import numpy as np
import scipy as sp
from data_importation_preprocessing.data_getters import getters
from Moteur.SVI_Interpolation import SVIModel
from Moteur.vol_models import Volatility
import time
import joblib
from joblib import Parallel, delayed
from multiprocessing import Pool, cpu_count
from numba import jit

class path_generator:
    def qmc_rv_generator(self,n,N,L):
        '''
        Transforms sobol sequence to normal distribution, and correlates the normal random variables
        using the cholesky decomposition of the correlation matrix
        :return: array of correlated random variables
        '''
        sobol_seq = sp.stats.qmc.Sobol(n, scramble=True).random_base2(np.log2(N).astype(int))
        if n > 1:
            indep_rand_vec = sp.stats.norm.ppf(sobol_seq).reshape(n*N,)
            indep_rand_vec = indep_rand_vec.reshape(n, N)
            correlated_variables = np.matmul(L,indep_rand_vec)
            correlated_variables = correlated_variables.T.reshape(n, N)
            return correlated_variables
        else:
            return sp.stats.norm.ppf(sobol_seq).reshape(N,)

    def prepare_dividends(self, n , N, T, div_matrix,steps_per_year = 252):
        '''
        Prepares a matrix of shape(n,N,T*252) of dividends
        :return: matrix of dividends to apply directly to the diffusion
        '''
        full_years = int(T)
        fractional_year = T - full_years
        repeated_columns = [np.repeat(div_matrix[:, i], steps_per_year) for i in range(full_years)]
        fractional_steps = int(fractional_year * steps_per_year)
        if fractional_steps > 0 :
            repeated_columns.append(np.repeat(div_matrix[:, full_years], fractional_steps))
        if n == 1 :
            repeated_matrix = np.concatenate(repeated_columns).reshape(-1, div_matrix.shape[0]).T
            return repeated_matrix
        else :
            if full_years == 0:
                final_matrix = repeated_columns[0].reshape(n,N,fractional_steps)
                return final_matrix
            else :
                matrix = np.concatenate([repeated_columns[i].reshape(n,N,steps_per_year) for i in range(full_years)], axis=2)
                if fractional_steps >0:
                    final_matrix = np.concatenate((matrix, repeated_columns[-1].reshape(n, N, fractional_steps)),axis=2)
                    return final_matrix
                else:
                    return matrix

    def prepare_diffusion(self,n, N, T,init_stock,div_matrix):
        '''
        Prepares initial conditions for the matrix and the constant volatility to start the QMC process
        :return: correctly shaped matrix for stock prices diffusion, volatility and dividends
        '''
        nbr_steps = int(np.floor(252 * T))
        div_array = np.repeat(div_matrix, N)
        factors = np.array([1 - 0.2 * i for i in range(int(np.ceil(T)))])
        dividends = np.column_stack([div_array * factor for factor in factors])
        dividends = self.prepare_dividends(n, N, T, dividends)
        if n >1 :
            vol = np.repeat(np.full(n, 0.2), N).reshape(n, N, )
            S_matrix = np.zeros((n, N, nbr_steps +1))
            S_matrix[:, :, 0] = init_stock[:, None]
            return S_matrix, dividends, vol
        if n == 1 :
            S_matrix = np.zeros((N, nbr_steps +1))
            vol = np.repeat(np.full(n, 0.2), N).reshape(N, )
            S_matrix[:, 0] = np.repeat(init_stock, N)
            return S_matrix, dividends, vol



    def compute_r(self,step,T,curve,time_step):
        '''
        Computes the residual maturity and the corresponding riskless rate
        :param step: time of the diffusion
        :param curve : Nelson siegel svensson calibration
        '''
        residual_maturity = T - step * time_step * T
        r = curve(residual_maturity) / 100
        return r,residual_maturity
    def update_matrix(self,slice_before,i,n,N,r,dividends,vol,time_step,L):
        '''
        computes a specific slice of the stock prices matrix
        '''
        if n >1:
            return slice_before * np.exp(((r - (dividends[:, :, i]/slice_before) - (vol ** 2) / 2) * time_step + vol * np.sqrt(time_step) * self.qmc_rv_generator(n, N, L)))
        else :
            return slice_before * np.exp(((r - (dividends[:, i] / slice_before) - (vol ** 2) / 2) * time_step + vol * np.sqrt(time_step) * self.qmc_rv_generator(n, N, None)))

    def compute_vol_slice(self,n,N,vol_type,log_moneyness_forward, residual_maturity, list_params_matrix):
        '''
        computes the volatility array for a specific slice of stock prices matrix
        '''
        if vol_type == 'BS_IV':
            return Volatility().implied_vol(n, log_moneyness_forward, residual_maturity, list_params_matrix)[0].reshape(n,N)
        if vol_type == 'LV':
            return Volatility().compute_local_vol(n, log_moneyness_forward, residual_maturity,list_params_matrix).reshape(n,N)
    def stress_slice(self,arr,shock):
        '''
        Applies and up or down shock to an array of values
        '''
        return arr * (1.15 if shock == 'Up' else 0.85 if shock == 'Down' else 1)

    def generate_paths(self,n,paths, curve,div_matrix, N, T, init_stock,list_params_matrix, vol_type,greeks,shock):
        '''
        Main function to generate N paths for n stocks
        '''
        time_step = (1 / (T * 252))
        S_matrix, dividends, vol = self.prepare_diffusion(n, N, T,init_stock,div_matrix)
        if n > 1:
            corr_matrix = getters().get_corr_matrix(paths)
            L = np.linalg.cholesky(corr_matrix)
            for i in range(S_matrix.shape[2] - 1):
                r, residual_maturity = self.compute_r(i, T, curve, time_step)
                if (i == 0) and ((greeks == 'Delta') or (greeks == 'Gamma')):
                    S_matrix[:,:, 0] = self.stress_slice(S_matrix[:,:, 0], shock)
                if vol_type != 'CST':
                    log_moneyness_forward = np.log(S_matrix[:,:, i] / (S_matrix[:,:, i - 1 if i != 0 else i] * np.exp(r * residual_maturity)))
                    vol = self.compute_vol_slice(n, N, vol_type, log_moneyness_forward, residual_maturity,list_params_matrix)
                if (i == 0) and ((greeks == 'Vega') or (greeks == 'Vomma')):
                    vol = self.stress_slice(vol, shock)
                S_matrix[:,:, i + 1] = self.update_matrix(S_matrix[:,:, i], i, n, N, r, dividends, vol, time_step, L)
        if n == 1:
            if vol_type != 'CST':
                list_params_matrix = list_params_matrix[0]
            for i in range(S_matrix.shape[1] - 1):
                r, residual_maturity = self.compute_r(i, T, curve, time_step)
                if (i == 0) and ((greeks == 'Delta') or (greeks == 'Gamma')):
                    S_matrix[:, 0] = self.stress_slice(S_matrix[:, 0], shock)
                if vol_type != 'CST':
                    log_moneyness_forward = np.log(S_matrix[:, i] / (S_matrix[:, i - 1 if i != 0 else i] * np.exp(r * residual_maturity)))
                    vol = self.compute_vol_slice(n, N, vol_type, log_moneyness_forward, residual_maturity,list_params_matrix)
                if (i == 0) and((greeks == 'Vega') or (greeks == 'Vomma')):
                    vol = self.stress_slice(vol, shock)
                S_matrix[:, i + 1] = self.update_matrix(S_matrix[:, i], i, n, N, r, dividends, vol, time_step,None)
        return S_matrix





