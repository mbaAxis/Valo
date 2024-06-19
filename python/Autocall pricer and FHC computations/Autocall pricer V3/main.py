import xlwings as xw
import numpy as np
import pandas as pd
from tests_plots.plots import test_spread_calendar_arbitrage, test_butterfly_arbitrage,plot_volatility_surface
from Moteur.SVI_Interpolation import SVIModel
from data_and_keys.constants import names_tickers
from data_importation_preprocessing.data_getters import getters, preprocess_data, get_dataframe_name
from Moteur.path_generation import path_generator
from Payoff import PayoffCalculator
from Moteur.greeks import GreeksComputations
import time
import os
import matplotlib.pyplot as plt
import warnings
from tests_plots.plots import test_calibration
warnings.filterwarnings("ignore")


def main():
    svi = SVIModel()
    curve = getters().get_riskless_rates()
    start_time = time.time()
    data_list = []
    dataframe_names_list = []
    dividends_matrix = getters().get_dividends(paths)
    print(dividends_matrix)
    for idx in range(n):
        path = paths[idx]
        data = pd.read_excel(path)
        div = dividends_matrix[idx]
        data_preprocessed = preprocess_data(data, curve, path, names_tickers, div)
        data_list.append(data_preprocessed)
        dataframe_names_list.append(get_dataframe_name(path))
    if n == 1 :
        init_stock = data_list[0]['SPOT'][0]
    if n >1 :
        init_stock = []
        for data in data_list :
            init_stock.append(data['SPOT'][0])
        init_stock = np.array(init_stock)
    print('stock prices', init_stock)
    print('div yield', np.array(dividends_matrix)/init_stock)
    print('interest rate', curve(0))
    if vol_type == 'CST':
        list_params_matrix = None
    else:
        list_params_matrix = svi.params_skew(n, data_list)
        # test_butterfly_arbitrage(dataframe_names_list, data_list, list_params_matrix)
        #test_spread_calendar_arbitrage(dataframe_names_list, data_list, list_params_matrix)
    S_matrix = path_generator().generate_paths(n, paths, curve, dividends_matrix, N, T, init_stock,
                                               list_params_matrix, vol_type, None, None)
    print(S_matrix.shape)


    np.save('matrix.npy', S_matrix)
    np.save('params.npy', list_params_matrix)
    obs_dates = PayoffCalculator.compute_obs_dates(T,freq_obs)
    pc = PayoffCalculator(n, N, S_matrix, init_stock, T, freq_obs, BP, AT, coupon,obs_dates)
    notional = 100
    evaluation_matrix, payoff_coupon_matrix, payoff_kg_matrix, state_matrix, payoff_matrix = pc.compute_payoff()
    print('eval matrix')
    print(evaluation_matrix)
    print('payoff_coupon_matrix')
    print(payoff_coupon_matrix)
    print('payoff_kg_matrix')
    print(payoff_kg_matrix)
    print('payoff matrix')
    print(payoff_matrix)

    price = pc.compute_price(n, notional, payoff_matrix, curve, T, freq_obs)
    print('price of Autocall', price)

    delta, gamma, vega, vomma = GreeksComputations(n, paths, curve, dividends_matrix, N, T, init_stock, list_params_matrix, vol_type, price, payoff_matrix,
        freq_obs, BP, AT, coupon, notional,obs_dates).compute_greeks()

    print('delta', delta)
    print('gamma', gamma)
    print('vega', vega)
    print('vomma', vomma)
    end_time = time.time()
    print(end_time - start_time)


if __name__ == "__main__":

    N = 8192
    #n = int(input('number of stocks: '))
    n = 1

    prefix = r'C:\Users\m.ben-el-ghoul\PycharmProjects\Autocall pricer\data and keys'
    paths = []
    for i in range(n):
        stock = str(input('Enter stock name: '))
        stock = stock.upper()
        path = prefix + '\\' + stock + '_25_04_2024.xlsx'
        paths.append(path)
    #vol_type = str(input('vol type: ')).upper()
    vol_type = 'LV'

    AT = 1
    BP = 0.5
    coupon = 0.1
    freq_obs = 4
    T = 1
    print('my delta is higher because SG is computing sticky delta which tends to be lower than delta when the iv surface has downward then upward slopes')
    main()

#Pricing with constant vol is almost equal
#Pricing with local vol is sometimes lower than cst , sometimes higher

# To DO
# Read on stochastic local volatility
# Read on FHC
