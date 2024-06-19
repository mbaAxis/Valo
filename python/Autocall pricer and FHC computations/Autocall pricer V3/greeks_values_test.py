import xlwings as xw
import numpy as np
import pandas as pd
from Moteur.SVI_Interpolation import SVIModel
from data_and_keys.constants import names_tickers
from data_importation_preprocessing.data_getters import getters, preprocess_data, get_dataframe_name
from Moteur.path_generation import path_generator
from Payoff import PayoffCalculator
from Moteur.greeks import GreeksComputations
import time
import warnings
warnings.filterwarnings("ignore")

def test_greeks_values():
    svi = SVIModel()
    curve = getters().get_riskless_rates()

    paths = []
    prefix = r'C:\Users\m.ben-el-ghoul\PycharmProjects\Autocall pricer\data and keys'
    wb = xw.Book.caller()  # Connect to the calling Excel instance
    main_worksheet = wb.sheets['Greeks_test']
    n = int(main_worksheet['B19'].value)
    if n == 1:
        vol_type = str(main_worksheet['B7'].value)
        N = 8192
        T = int(main_worksheet['B8'].value)
        stock = str(main_worksheet['B5'].value)
        path = prefix + '\\' + stock + '_25_04_2024.xlsx'
        paths.append(path)
        data_list = []
        dataframe_names_list = []
        dividends_matrix = getters().get_dividends(paths)
        for idx in range(n):
            path = paths[idx]
            data = pd.read_excel(path)
            div = dividends_matrix[idx]
            data_preprocessed = preprocess_data(data, curve, path, names_tickers, div)
            data_list.append(data_preprocessed)
            dataframe_names_list.append(get_dataframe_name(path))
        init_stock = data_list[0]['SPOT'][0]
        if vol_type == 'CST':
            list_params_matrix = None
        else:
            list_params_matrix = svi.params_skew(n, data_list)
        S_matrix = path_generator().generate_paths(n, paths, curve, dividends_matrix, N, T, init_stock,
                                                   list_params_matrix, vol_type, None, None)
        for i in range(4,13):
            start_time = time.time()
            print('line ' + str(i))
            AT = float(main_worksheet['D' + str(i)].value)
            BP = float(main_worksheet['E'+ str(i)].value)
            coupon = float(main_worksheet['F' + str(i)].value)
            freq_obs = int(main_worksheet['G'+ str(i)].value)
            AT = np.full(freq_obs, AT)
            notional = 100
            pc = PayoffCalculator(n, N, S_matrix, init_stock, T, freq_obs, BP, AT, coupon)
            eval_matrix, payoff_coupon_matrix, payoff_kg_matrix, state_matrix, payoff_matrix = pc.compute_payoff()
            price = pc.compute_price(n, notional, payoff_matrix, curve, T, freq_obs)
            print('price of Autocall', price)
            greeks_comp = GreeksComputations(n, paths, curve, dividends_matrix, N, T, init_stock, list_params_matrix, vol_type, price,
                 payoff_matrix, freq_obs, BP, AT, coupon, notional)
            delta, gamma, vega, vomma = greeks_comp.compute_greeks()
            print('delta', delta)
            print('gamma', gamma)
            print('vega', vega)
            print('vomma', vomma)

            main_worksheet['H'+str(i)].value = price
            main_worksheet['I'+str(i)].value = delta
            main_worksheet['J'+str(i)].value = gamma
            main_worksheet['K'+str(i)].value = vega
            main_worksheet['L'+str(i)].value = vomma
            end_time = time.time()
            main_worksheet['N' + str(i)].value = end_time - start_time
    if n> 1 :
        vol_type = str(main_worksheet['B26'].value)
        N = 8192
        T = int(main_worksheet['B25'].value)
        for i in range(n):
            stock = str(main_worksheet['A' +str(i+23)].value)
            path = prefix + '\\' + stock + '_25_04_2024.xlsx'
            paths.append(path)
        init_stock = []
        data_list = []
        dataframe_names_list = []
        dividends_matrix = getters().get_dividends(paths)
        for idx in range(n):
            path = paths[idx]
            data = pd.read_excel(path)
            init_stock.append(data['SPOT'][0])
            div = dividends_matrix[idx]
            data_preprocessed = preprocess_data(data, curve, path, names_tickers, div)
            data_list.append(data_preprocessed)
            dataframe_names_list.append(get_dataframe_name(path))
        init_stock = np.array(init_stock)
        if vol_type == 'CST':
            list_params_matrix = None
        else:
            list_params_matrix = svi.params_skew(n, data_list)
        S_matrix = path_generator().generate_paths(n, paths, curve, dividends_matrix, N, T, init_stock,
                                                   list_params_matrix, vol_type, None, None)
        for i in range(22,31):
            start_time = time.time()
            print('line ' + str(i))
            AT = float(main_worksheet['D' + str(i)].value)
            BP = float(main_worksheet['E' + str(i)].value)
            coupon = float(main_worksheet['F' + str(i)].value)
            freq_obs = int(main_worksheet['G' + str(i)].value)
            AT = np.full(freq_obs, AT)
            notional = 100
            pc = PayoffCalculator(n, N, S_matrix, init_stock, T, freq_obs, BP, AT, coupon)
            eval_matrix, payoff_coupon_matrix, payoff_kg_matrix, state_matrix, payoff_matrix = pc.compute_payoff()
            price = pc.compute_price(n, notional, payoff_matrix, curve, T, freq_obs)
            print('price of Autocall', price)
            greeks_comp = GreeksComputations(n, paths, curve, dividends_matrix, N, T, init_stock, list_params_matrix,vol_type, price,
                                             payoff_matrix, freq_obs, BP, AT, coupon, notional)
            delta, gamma, vega, vomma = greeks_comp.compute_greeks()
            print('delta', delta)
            print('gamma', gamma)
            print('vega', vega)
            print('vomma', vomma)
            main_worksheet['H'+str(i)].value = price
            main_worksheet['I'+str(i)].value = delta
            main_worksheet['J'+str(i)].value = gamma
            main_worksheet['K'+str(i)].value = vega
            main_worksheet['L'+str(i)].value = vomma
            end_time = time.time()
            main_worksheet['N' + str(i)].value = end_time - start_time

if __name__ == "__main__":
    xw.Book('Autocall pricer.xlsm').set_mock_caller()
    test_greeks_values()
