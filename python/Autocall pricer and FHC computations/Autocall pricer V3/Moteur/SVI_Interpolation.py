import numpy as np
import scipy.optimize as opt
from scipy.interpolate import CubicSpline, interp1d
import joblib
from joblib import Parallel, delayed
from numba import jit
import time
from multiprocessing import Pool, cpu_count
import warnings
warnings.filterwarnings("ignore")

@jit(nopython = True)
def g(params, x):
    '''
    Density function of SVI
    :param params: SVI parameters
    :param x: log moneyness forward vec
    :return: array of values to respect a constraint in the minimization
    '''
    a, b, rho, m, sig = params
    discr = np.sqrt((x - m) ** 2 + sig ** 2)
    w = a + b * (rho * (x - m) + discr)
    dw = b * rho + b * (x - m) / discr
    d2w = b * sig ** 2 / (discr ** 3)
    return (1 - (x * dw) / (2 * w)) ** 2 - ((dw ** 2) / 4) * (1 / w + 0.25) + d2w / 2
class SVIModel:
    def __init__(self, method_name='SLSQP', maxiter=50, epsilon=1e-12):
        self.method_name = method_name
        self.maxiter = maxiter
        self.epsilon = epsilon

    def svi(self, log_moneyness_forward,params):
        a, b, rho, m, s = params
        return a + b * (rho * (log_moneyness_forward - m) + np.sqrt((log_moneyness_forward - m) ** 2 + s ** 2))

    def ms_loss(self, params, log_moneyness_forward, market_itv):
        return np.sum((self.svi(log_moneyness_forward,params) - market_itv) ** 2)

    def calendar_spread_arbitrage(self, params, last_params, log_moneyness_forward):
        return self.svi(log_moneyness_forward,params) - self.svi(log_moneyness_forward, last_params)

    def butterfly_arbitrage(self, params, log_moneyness_forward):
        return g(params, log_moneyness_forward) - 1e-4

    def calibration(self, last_params, market_itv, log_moneyness_forward, init_params):
        opt_rmse = 1
        bounds = [(1e-5, np.max(market_itv)), (0.01, 1), (-1 + 1e-12, 1 - 1e-12),
                  (2 * np.min(log_moneyness_forward), 2 * np.max(log_moneyness_forward)), (0.01, 1)]

        for i in range(1, self.maxiter + 1):
            inequality_constraint1 = {'type': 'ineq', 'fun': self.calendar_spread_arbitrage,
                                      'args': (last_params, log_moneyness_forward)}
            inequality_constraint2 = {'type': 'ineq', 'fun': self.butterfly_arbitrage,
                                      'args': (log_moneyness_forward,)}
            constraints_list = [inequality_constraint1, inequality_constraint2]

            result = opt.minimize(self.ms_loss, init_params, args=(log_moneyness_forward, market_itv),
                                    method=self.method_name,constraints=constraints_list,
                                    bounds=bounds, tol=1e-8)
            a_opt, b_opt, rho_opt, m_opt, s_opt = result.x
            init_params = [a_opt, b_opt, rho_opt, m_opt, s_opt]
            opt_rmse1 = self.ms_loss(init_params, log_moneyness_forward, market_itv)
            if i > 1 and (abs(opt_rmse - opt_rmse1) < self.epsilon):
                break
            opt_rmse = opt_rmse1
        return np.array([a_opt, b_opt, rho_opt, m_opt, s_opt])

    def params_skew(self, n, list_data, number_parameters=5):
        def params_single_data(i):
            data = list_data[i]
            sorted_ttm_vec = np.sort(np.unique(np.array(data.Maturity.values)))
            params_matrix = np.zeros((len(sorted_ttm_vec), number_parameters + 1))
            params_matrix[:, 0] = sorted_ttm_vec
            params_list = []
            last_params = np.zeros(5)
            for ttm in sorted_ttm_vec:
                subset = data[data.Maturity == ttm]
                log_moneyness_forward = subset['Log_Moneyness'].values
                market_itv = subset['market_itv'].values
                init_params = np.array([0.5 * np.min(np.array(market_itv)), 0.1, -0.5, 0.1, 0.1])
                params_list.append(self.calibration(last_params, market_itv, log_moneyness_forward, init_params))
                last_params = params_list[-1]
            params_matrix[:, 1:] = np.array(params_list)
            return params_matrix
        result = Parallel(n_jobs = -1,prefer='threads')(delayed(params_single_data)(i) for i in range(n))
        return result
