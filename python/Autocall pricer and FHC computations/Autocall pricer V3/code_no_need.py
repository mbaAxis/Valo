'''def interpolate_derivative(self, x, ttm, list_params_matrix):
    a, b, rho, m, sig = list_params_matrix[:, 1], list_params_matrix[:, 2], list_params_matrix[:,
                                                                            3], list_params_matrix[:,
                                                                                4], list_params_matrix[:, 5]
    discr = np.sqrt((x - m) ** 2 + sig ** 2)
    dw = b * rho + b * (x - m) / discr
    d2w = b * sig ** 2 / (discr ** 3)
    x_axis = list_params_matrix[:, 0]
    x_axis = np.insert(x_axis, 0, 0)
    dw = np.insert(dw, 0, 0)
    d2w = np.insert(d2w, 0, 0)
    # cs = interp1d(x, y,fill_value="extrapolate")
    cs_1 = CubicSpline(x_axis, dw)
    cs_2 = CubicSpline(x_axis, d2w)
    first_deriv = cs_1(ttm)
    second_deriv = cs_2(ttm)
    return first_deriv, second_deriv'''


'''def SVI_derivatives(self, n, log_moneyness_forward_vec, ttm, list_params_matrix):
    final_1_deriv_list = []
    final_2_deriv_list = []
    if n == 1:
        list_params_matrix = list_params_matrix[0]
        for i in range(len(log_moneyness_forward_vec[0])):
            first_deriv, second_deriv = self.interpolate_derivative(log_moneyness_forward_vec[0][i], ttm,
                                                                    list_params_matrix)
            final_1_deriv_list.append(first_deriv)
            final_2_deriv_list.append(second_deriv)
        return np.array(final_1_deriv_list), np.array(final_2_deriv_list)
    else:
        for params_matrix, log_moneyness_forward in zip(list_params_matrix, log_moneyness_forward_vec):
            fst_deriv_list = []
            snd_deriv_list = []
            for i in range(len(log_moneyness_forward)):
                first_deriv, second_deriv = self.interpolate_derivative(log_moneyness_forward[i], ttm, params_matrix)
                fst_deriv_list.append(first_deriv)
                snd_deriv_list.append(second_deriv)
            final_1_deriv_list.append(fst_deriv_list)
            final_2_deriv_list.append(snd_deriv_list)
        return np.array(final_2_deriv_list), np.array(final_2_deriv_list)'''


'''def correlate_rv(self, n, N):
    if n > 1:
        indep_rand_vec = np.random.normal(loc=0, scale=1, size=n * N)
        corr_matrix = getters().get_corr_matrix(n)
        L = np.linalg.cholesky(corr_matrix)
        indep_rand_vec = indep_rand_vec.reshape(n, N, 1)
        indep_rand_vec = np.transpose(indep_rand_vec, (1, 0, 2))
        correlated_variables = np.dot(L, indep_rand_vec).transpose(1, 0, 2)
        correlated_variables = correlated_variables.T.reshape(n, N, 1)
        return correlated_variables
    else:
        return np.random.normal(loc=0, scale=1, size=n * N)'''


#implied vol n > 1
'''def interpolate_iv_multi(log_moneyness_forward,params_matrix, ttm):
    iv_list = []
    itv_list = []
    for i in range(len(log_moneyness_forward)):
        final_iv, interpolated_itv = self.interpolate(log_moneyness_forward[i], ttm, params_matrix)
        iv_list.append(final_iv)
        itv_list.append(interpolated_itv)
    return iv_list, itv_list

results = Parallel(n_jobs=-1,prefer="threads")(delayed(interpolate_iv_multi)(log_moneyness_forward,params_matrix,ttm) for params_matrix, log_moneyness_forward in zip(list_params_matrix, log_moneyness_forward_vec))
final_iv_list = [iv_list for iv_list, _ in results]
final_interpolated_itv_list = [itv_list for _, itv_list in results]
return np.array(final_iv_list),np.array(final_interpolated_itv_list)'''
'''for params_matrix, log_moneyness_forward in zip(list_params_matrix, log_moneyness_forward_vec):
    iv_list = []
    itv_list = []
    x = params_matrix[:,0]
    for i in range(len(log_moneyness_forward)):
        final_iv, interpolated_itv = self.interpolate(log_moneyness_forward[i],ttm, params_matrix,x)
        iv_list.append(final_iv)
        itv_list.append(interpolated_itv)
    final_iv_list.append(iv_list)
    final_interpolated_itv_list.append(itv_list)'''

#implied vol mono asset
# log_moneyness_forward_vec = [log_moneyness_forward_vec]
'''def interpolate_iv_mono(logm, params_matrix):
    final_iv, interpolated_itv = self.interpolate(logm, ttm, params_matrix)
    return final_iv, interpolated_itv
results = Parallel(n_jobs=-1,prefer= "threads")(delayed(interpolate_iv_mono)(logm,list_params_matrix[0]) for logm in log_moneyness_forward_vec[0])
for final_iv, interpolated_itv in results:
    final_iv_list.append(final_iv)
    final_interpolated_itv_list.append(interpolated_itv)
return np.array(final_iv_list), np.array(final_interpolated_itv_list)'''
'''for i in range(len(log_moneyness_forward_vec[0])):
    final_iv, interpolated_itv = self.interpolate(log_moneyness_forward_vec[0][i], ttm, params_matrix,x)
    final_iv_list.append(final_iv)
    final_interpolated_itv_list.append(interpolated_itv)
return np.array(final_iv_list), np.array(final_interpolated_itv_list)'''

#interpolate function
'''def interpolate(self, logm, ttm, list_params_matrix, x):
    y = self.SVI(logm, list_params_matrix[:, 1], list_params_matrix[:, 2],
                 list_params_matrix[:, 3],
                 list_params_matrix[:, 4],
                 list_params_matrix[:, 5])  # vector of implied variances needed for interpolation
    # x = list_params_matrix[:, 0]
    x = np.insert(x, 0, 0)
    y = np.insert(y, 0, 0)
    cs = interp1d(x, y, fill_value="extrapolate")
    # cs = CubicSpline(x, y)
    interpolated_itv = cs(ttm)
    final_iv = np.array(np.sqrt(interpolated_itv / ttm))
    return final_iv, interpolated_itv'''

from Moteur.SVI_Interpolation import SVIModel
import numpy as np
import pandas as pd
from data_and_keys.constants import names_tickers
from data_importation_preprocessing.data_getters import getters,preprocess_data,get_dataframe_name
from Moteur.path_generation import path_generator
from tests_plots.plots import test_calibration,test_butterfly_arbitrage,test_spread_calendar_arbitrage,plot_stock_path,plot_volatility_surface
from Moteur.curve_module import curve
import joblib
from joblib import Parallel, delayed
import time
import warnings
warnings.filterwarnings("ignore")
import matplotlib.pyplot as plt
from Payoff import compute_payoff,compute_price
from Moteur.greeks import greeks_computations
import xlwings as xw


svi = SVIModel()
curve = getters().get_riskless_rates()







#def main(paths,AT):
start_time = time.time()
paths = []
prefix = r'C:\Users\m.ben-el-ghoul\PycharmProjects\Autocall pricer\data and keys'

N = 4
n = 1
#vol_type = input('Enter vol type ')
AT = 1
BP = 0.8
coupon = 0.06
freq_obs = 3
T = 1

AT = np.full(freq_obs, AT)

wb = xw.books.active

main_worksheet = wb.sheets['Main']

vol_type = str(main_worksheet['C3'].value)
print(vol_type)

for i in range(n):
    # stock = input('Enter stock ' + str(i + 1) + ' ticker:')
    stock = str(main_worksheet['C4'].value)
    path = prefix + '\\' + stock + '_25_04_2024.xlsx'
    paths.append(path)


if n == 1:
    data_list = []
    dataframe_names_list = []
    dividends_matrix = getters().get_dividends(paths)
    for idx in range(n):
        path = paths[idx]
        data = pd.read_excel(path)
        div = dividends_matrix[idx]
        data_preprocessed = preprocess_data(data,curve,path,names_tickers,div)
        data_list.append(data_preprocessed)
        dataframe_names_list.append(get_dataframe_name(path))
        #test_calibration(curve, data_list[idx], dataframe_names_list[idx])
    if vol_type == 'CST':
        list_params_matrix = None
    else:
        list_params_matrix = svi.params_skew(n, data_list)
        #print(list_params_matrix)
        #test_butterfly_arbitrage(dataframe_names_list, data_list, list_params_matrix)
        #test_spread_calendar_arbitrage(dataframe_names_list, data_list, list_params_matrix)
    init_stock = data_list[0]['SPOT'][0]
    S_matrix = path_generator().generate_paths(n,paths,curve, dividends_matrix, N, T, init_stock, list_params_matrix,vol_type,None,None)
    print(S_matrix)
    print(S_matrix.shape)
    #S_matrix = S_matrix.reshape(N,T*252)
    #x_axis = np.linspace(0,T,T*252)
    #for i in range(S_matrix.shape[0]):
        #plt.plot(x_axis, S_matrix[i,:])
    #plt.xlabel('Time')
    #plt.ylabel('Stock price')
    #plt.title('Stock price for ' + dataframe_names_list[0])
    #plt.show()
    #if vol_type == 'BS_IV':
        #plot_volatility_surface(n, vol_type, list_params_matrix, dataframe_names_list[0])
    notional = 100
    eval_matrix, payoff_coupon_matrix, payoff_kg_matrix, state_matrix, payoff_matrix=compute_payoff(n, S_matrix, init_stock, T, N, freq_obs, BP, AT, coupon)
    price = compute_price(n,notional, payoff_matrix,curve, T, freq_obs)
    print('price of Autocall', price)
    delta, gamma, vega,vomma = greeks_computations().finite_diff_greeks(n, paths, curve, dividends_matrix, N, T, init_stock,list_params_matrix, vol_type,price,payoff_matrix,freq_obs, BP, AT,coupon)
    print('delta', delta)
    print('gamma', gamma)
    print('vega', vega)
    print('vomma', vomma)
    main_worksheet['E2'].value = price

if n > 1:
    init_stock = []
    data_list = []
    dataframe_names_list = []
    dividends_matrix = getters().get_dividends(paths)
    for idx in range(n):
        path = paths[idx]
        data = pd.read_excel(path)
        init_stock.append(data['SPOT'][0])
        div = dividends_matrix[idx]
        data_preprocessed = preprocess_data(data, curve,path, names_tickers,div)
        data_list.append(data_preprocessed)
        dataframe_names_list.append(get_dataframe_name(path))
        #test_calibration(curve, data_list[idx], dataframe_names_list[idx])

    init_stock = np.array(init_stock)
    if vol_type == 'CST':
        list_params_matrix = None
    else :
        list_params_matrix = svi.params_skew(n, data_list)
        #print(list_params_matrix)
        #test_butterfly_arbitrage(dataframe_names_list,data_list,list_params_matrix)
        #test_spread_calendar_arbitrage(dataframe_names_list,data_list,list_params_matrix)
    S_matrix = path_generator().generate_paths(n,paths, curve,dividends_matrix, N, T, init_stock, list_params_matrix,vol_type,None,None)

    '''for i in range(S_matrix.shape[0]):
        S_mat = S_matrix[i,:,:]
        S_mat = S_mat.reshape(N,T*252)
        x_axis = np.linspace(0, T, T * 252)
        for j in range(S_mat.shape[0]):
            plt.plot(x_axis, S_mat[j, :])
        plt.xlabel('Time')
        plt.ylabel('Stock price')
        plt.title('Stock price for ' + dataframe_names_list[i])
        plt.show()
    if vol_type == 'BS_IV':
        for i in range(len(list_params_matrix)):
            plot_volatility_surface(1, vol_type, [list_params_matrix[i]], dataframe_names_list[i])'''

    notional = 100
    eval_matrix, payoff_coupon_matrix, payoff_kg_matrix, state_matrix, payoff_matrix = compute_payoff(n, S_matrix,
                                                                                                      init_stock, T, N,
                                                                                                      freq_obs, BP, AT,coupon)

    price = compute_price(n,notional, payoff_matrix,curve, T, freq_obs)
    print('price', price)

    delta, gamma, vega, voma = greeks_computations().finite_diff_greeks(n, paths, curve, dividends_matrix, N, T, init_stock,list_params_matrix, vol_type,price,payoff_matrix,freq_obs, BP, AT,coupon)
    print('delta', delta)
    print('gamma', gamma)
    print('vega', vega)
    print('voma', voma)
end_time = time.time()
print(end_time - start_time)


'''delta= greeks_computations().finite_diff_greeks(n, paths, curve, dividends_matrix, N, T,
                                                                  init_stock, list_params_matrix, vol_type,
                                                                  price, freq_obs, BP, AT, coupon)
print(delta)'''


#if __name__ == "__main__":




#paths = []
#prefix = r'C:\Users\m.ben-el-ghoul\PycharmProjects\Autocall pricer\data and keys'
'''n = int(input('Enter number of assets '))
vol_type = input('Enter vol type ')
AT = float(input('Enter autocall barrier '))
BP = float(input('Enter barrier put '))
coupon = float(input('Enter coupon value '))
freq_obs = int(input('Enter the frequencey of observation of the Autocall '))
T = int(input('Enter the maturity of the autocall '))'''

'''N = 4
n = 1
#vol_type = input('Enter vol type ')
AT = 1
BP = 0.8
coupon = 0.06
freq_obs = 3
T = 1
AT = np.full(freq_obs, AT)'''
'''for i in range(n):
    #stock = input('Enter stock ' + str(i + 1) + ' ticker:')
    stock = str(main_worksheet['B3'].value)
    path = prefix + '\\' + stock + '_25_04_2024.xlsx'
    paths.append(path)'''

'''start_time = time.time()
main(paths,AT)
end_time = time.time()
print(end_time - start_time)'''
