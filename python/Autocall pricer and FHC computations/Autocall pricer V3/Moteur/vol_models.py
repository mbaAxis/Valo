import numpy as np
from numba import jit
from Moteur.SVI_Interpolation import SVIModel

@jit(nopython=True) #Decorator to speed up the interpolation
def interp1d_linear_extrapolate(x, y, ttm):
    '''
    Linear Interpolation and exptrapolation to replace interp1d
    :param x: market maturities
    :param y: market implied total variances
    :param ttm: maturity to evaluate the interpolation
    :return: itv value corresponding to ttm
    '''
    if ttm <= x[0]:
        # Extrapolate to the left
        slope = (y[1] - y[0]) / (x[1] - x[0])
        return y[0] + slope * (ttm - x[0])
    elif ttm >= x[-1]:
        # Extrapolate to the right
        slope = (y[-1] - y[-2]) / (x[-1] - x[-2])
        return y[-1] + slope * (ttm - x[-1])
    else:
        # Interpolate
        idx = np.searchsorted(x, ttm) - 1
        x0, x1 = x[idx], x[idx + 1]
        y0, y1 = y[idx], y[idx + 1]
        return y0 + (y1 - y0) * (ttm - x0) / (x1 - x0)
@jit(nopython=True)
def denom_dupire(const, SVIm, SVImm, log_moneyness_forward_vec):
    '''
    The denominator of the identity linking Dupire local vol to BS implied vol
    :param: Local vol, 1st and 2nd derivative of local vol
    :return: array of values
    '''
    return (1 - 0.5 * log_moneyness_forward_vec * SVIm/const)**2 - 0.25 * SVIm**2 * (0.25 + 1/const) + 0.5 * SVImm

def regularize_g(arr):
    '''
    g is sometimes negative when extrapolating. This function makes it positive
    '''
    if arr.ndim == 1:
        arr[arr < 0] = np.mean(arr[arr > 0])
    else:
        for i in range(arr.shape[0]):
            arr[i][arr[i] < 0] = np.mean(arr[i][arr[i] > 0])
    return arr
class Volatility:

    def prepare_interpolation(self, logm_arr,params_matrix):
        '''
        prepares the arrays of interpolation
        :return: x : array of maturities
                result: matrix whose arrays are the itv of one log moneyness and all maturities
        '''
        x = params_matrix[:, 0]
        log_moneyness_forward_m = (logm_arr - params_matrix[:, 4][:, np.newaxis]).T
        result = params_matrix[:, 1] + params_matrix[:, 2] * (params_matrix[:, 3] * (log_moneyness_forward_m) +
                                                              np.sqrt((log_moneyness_forward_m) ** 2 + params_matrix[:,5] ** 2))

        result = np.insert(result, 0, 0,axis=1)  # each line in results are the ITV of one log moneyness for all maturities
        x = np.insert(x, 0, 0)
        return x, result

    def implied_vol(self, n, log_moneyness_forward_vec, ttm, list_params_matrix):
        '''
        Interpolates the maturities and the itv and then computes the itv at a specific ttm
        :return: vectors of shape (n*N,) or (n,N,) of IVs and ITVs.
        '''
        if n == 1:
            list_params_matrix = [list_params_matrix]
            log_moneyness_forward_vec = [log_moneyness_forward_vec]
        all_itv_interpolated = []
        for logm_arr, params_matrix in zip(log_moneyness_forward_vec, list_params_matrix):
            x,result = self.prepare_interpolation(logm_arr,params_matrix)
            interpolated_values = np.array([interp1d_linear_extrapolate(x, row, ttm) for row in result])
            all_itv_interpolated.append(interpolated_values)
        if n == 1:
            final_itv_interpolated_values = np.array(all_itv_interpolated).reshape(log_moneyness_forward_vec[0].shape[0],)
        else:
            final_itv_interpolated_values = np.vstack(all_itv_interpolated)
        final_iv_interpolated_values = np.sqrt(final_itv_interpolated_values / ttm)
        return final_iv_interpolated_values, final_itv_interpolated_values

    def compute_local_vol(self, n, log_moneyness_forward_vec, ttm, list_params_matrix):
        '''
        applies finite difference on itv arrays to compute derivatives and local volatility
        :return: Array of local volatilities
        '''
        epsilon_time = 1e-4
        epsilon_log_moneyness = 1e-4
        def compute_iv_epsilon(eps_time=0.0, eps_log_moneyness=0.0):
            return self.implied_vol(n, log_moneyness_forward_vec + eps_log_moneyness, ttm + eps_time,list_params_matrix)[1]
        const = self.implied_vol(n, log_moneyness_forward_vec, ttm, list_params_matrix)[1]
        const_plus_epsilon_logm = compute_iv_epsilon(eps_log_moneyness=epsilon_log_moneyness)
        const_minus_epsilon_logm = compute_iv_epsilon(eps_log_moneyness=-epsilon_log_moneyness)
        SVIt = (compute_iv_epsilon(eps_time=epsilon_time) - compute_iv_epsilon(eps_time=-epsilon_time)) / (2 * epsilon_time)
        SVIm = (const_plus_epsilon_logm - const_minus_epsilon_logm) / (2 * epsilon_log_moneyness)
        SVImm = (const_plus_epsilon_logm - 2 * const + const_minus_epsilon_logm) / (epsilon_log_moneyness ** 2)
        return np.sqrt(SVIt / regularize_g(denom_dupire(const, SVIm, SVImm, log_moneyness_forward_vec)))