import pandas as pd
from data_and_keys.constants import names_tickers,paths
import numpy as np
import nelson_siegel_svensson
from nelson_siegel_svensson import calibrate
import joblib
from joblib import Parallel, delayed
from nelson_siegel_svensson import NelsonSiegelSvenssonCurve
import scipy.stats as st
import yfinance as yf
import warnings
warnings.filterwarnings("ignore")
import os
import re
import requests
import csv
import io
import time
import math
from scipy.optimize import newton, bisect

class BlackScholesOptionPricing:
    def __init__(self, S, K, r, d, T):
        self.S = S
        self.K = K
        self.r = r
        self.d = d
        self.T = T
    def d1(self, sigma):
        return (np.log(self.S / self.K) + (self.r - self.d + sigma ** 2 / 2) * self.T) / (sigma * np.sqrt(self.T))

    def d2(self, sigma):
        return (np.log(self.S / self.K) + (self.r - self.d - sigma ** 2 / 2) * self.T) / (sigma * np.sqrt(self.T))

    def call_price(self, sigma):
        return self.S * np.exp(-self.d * self.T) * st.norm.cdf(self.d1(sigma)) - self.K * np.exp(-self.r * self.T) * st.norm.cdf(self.d2(sigma))

    def put_price(self, sigma):
        return self.K * np.exp(-self.r * self.T) * st.norm.cdf(-self.d2(sigma)) - self.S * np.exp(-self.d * self.T) * st.norm.cdf(-self.d1(sigma))

    def implied_volatility(self, market_price, option_flag, it=100, initial_sigma=0.2):
        sigma_est = initial_sigma
        for _ in range(it):
            vega = self.S * np.exp(-self.d * self.T) * np.sqrt(self.T) * st.norm.pdf(self.d1(sigma_est))
            price_diff = self.call_price(sigma_est) - market_price if option_flag == 'C' else self.put_price(sigma_est) - market_price
            sigma_est -= price_diff / vega
        return sigma_est


class getters:
    def get_riskless_rates(self):
        '''
        Connects to BCE API and recover AAA bonds of the euro area yield rates for different maturities
        :return: a dictionnary whose keys are maturities and values are rates.
        NB : these rates corresponds to the date of 25/04/2024, since it is the pricing date of the data we will use
        as test.
        '''
        # Hiding the code because I always use the same rates

        #keys_df = pd.read_excel(r'C:\Users\m.ben-el-ghoul\PycharmProjects\Autocall pricer V2\data_and_keys\short term keys.xlsx')
        #keys_list = keys_df['keys'].tolist()
        def fetch_data(i):
            #entrypoint = "https://sdw-wsrest.ecb.europa.eu/service/data/"
            #keyComponent_list = re.findall(r"(\w+)\.", i)
            #db_id = keyComponent_list[0]
            #keyRemainder = '.'.join(re.findall(r"\.(\w+)", i))
            '''requestUrl = entrypoint + db_id + "/" + keyRemainder + "?format=genericdata"
            parameters = {
                'startPeriod': '2024-04-25',
                'endPeriod': '2024-04-25'
            }
            response = requests.get(requestUrl, params=parameters, headers={'Accept': 'text/csv'})
            assert response.status_code == 200, f"Expected response code 200, got {response.status_code} for {requestUrl}. Check your URL!"
            response_data = response.text
            data_dict_list = []
            reader = csv.DictReader(io.StringIO(response_data))
            for row in reader:
                data_dict_list.append(row)
            return data_dict_list

        final_list = Parallel(n_jobs=-1,prefer="threads")(delayed(fetch_data)(i) for i in keys_list)
        rates_dict = {}

        def process_data(dict_r):
            key = dict_r['DATA_TYPE_FM'].split('_')[1:][0]
            if 'Y' in key:
                key = float(dict_r['DATA_TYPE_FM'].split('_')[1:][0][:-1])
            elif 'M' in key:
                key = float(dict_r['DATA_TYPE_FM'].split('_')[1:][0][:-1]) / 12
            value = float(dict_r['OBS_VALUE'])
            return key, value

        results = Parallel(n_jobs=-1,prefer="threads")(delayed(process_data)(l[0]) for l in final_list)

        for key, value in results:
            rates_dict[key] = value'''

        rates_dict = {10.0: 2.6968869031,
                     1.0: 3.3368550717,
                     2.0: 2.9873046997,
                     0.25: 3.7258869368,
                     3.0: 2.7756859322,
                     4.0: 2.6580181479,
                     5.0: 2.6031485928,
                     0.5: 3.5818096039,
                     6.0: 2.5891222919,
                     7.0: 2.6005690372,
                     8.0: 2.6268233911,
                     0.75: 3.452559747,
                     9.0: 2.660572905}
        t, y = np.array(list(rates_dict.items())).T
        curve,_ = nelson_siegel_svensson.calibrate.calibrate_nss_ols(t, y, tau0=(2.0, 5.0))
        return curve

    def get_corr_matrix(self,paths):
        '''
        extracts historical asset prices to compute correlation
        handles it if not definite positive.
        :return: correlation matrix of the assets
        '''
        def is_positive_definite(matrix):
            try:
                np.linalg.cholesky(matrix)
                return True
            except np.linalg.LinAlgError:
                return False

        def find_smallest_epsilon_to_regularize(matrix, epsilon_min=1e-10, epsilon_max=1.0, tol=1e-8):
            assert epsilon_min < epsilon_max, "Invalid range: epsilon_min must be less than epsilon_max"
            while epsilon_max - epsilon_min > tol:
                epsilon_mid = (epsilon_min + epsilon_max) / 2.0
                perturbed_matrix = matrix + epsilon_mid * np.eye(matrix.shape[0])
                if is_positive_definite(perturbed_matrix):
                    epsilon_max = epsilon_mid
                else:
                    epsilon_min = epsilon_mid
            return epsilon_max

        def handle_corr_matrix(matrix):
            if is_positive_definite(matrix):
                pass
            else:
                epsilon = find_smallest_epsilon_to_regularize(matrix)
                matrix = matrix + epsilon * np.eye(matrix.shape[0])
            return matrix

        def retrieve_hist_prices(path):
            stock_name = get_dataframe_name(path)
            ticker = names_tickers[stock_name]
            stock_data = yf.download(ticker, period='10d', interval='1d', progress=False)
            stock_data = np.array(stock_data['Close'].values)
            return stock_data

        historical_prices = Parallel(n_jobs=-1, prefer="processes")(delayed(retrieve_hist_prices)(path) for path in paths)
        historical_prices = np.vstack(historical_prices)
        correlation_matrix = np.corrcoef(historical_prices, rowvar=True)
        correlation_matrix = handle_corr_matrix(correlation_matrix)
        return correlation_matrix


    def get_dividends(self,paths):
        '''
        :return: market dividend for each ticker
        '''
        def process_div(path):
            stock_name = get_dataframe_name(path)
            ticker = names_tickers[stock_name]
            dividends = yf.Ticker(ticker).dividends.tail(1).values
            return float(dividends)
        dividends_matrix = Parallel(n_jobs = -1, prefer="threads")(delayed(process_div)(path) for path in paths)
        return np.array(dividends_matrix)


def preprocess_data(data,curve,path,names_tickers,div):
    '''
    This function returns preprocessed data and adds a column IV and a column ITV(implied total variance)
    :param data: One of the datasets of euro next
    :return: preprocessed data
    '''
    data = data[data['OPTION FLAG']=='P']
    data.reset_index(inplace=True, drop = True)
    S = data['SPOT'][0]
    d = div / S
    option_flag = data['OPTION FLAG'][0]
    log_moneyness_forward = np.log(data['STRIKE'] / S) + (curve(data['MAT']) / 100) * data['MAT']
    implied_vols = [
        BlackScholesOptionPricing(S, K, r, d, T).implied_volatility(market_price, option_flag)
        for K, T, market_price, r in zip(data['STRIKE'], data['MAT'], data['PRICE'], curve(data['MAT']) / 100)
    ]
    implied_total_variances = data['MAT'] * np.square(implied_vols)
    data = data.assign(
        market_itv=implied_total_variances,
        IV=implied_vols,
        Log_Moneyness=log_moneyness_forward
    ).rename(columns={'MAT': 'Maturity'}).dropna().query('IV >= 0').reset_index(drop=True)
    return data


def get_dataframe_name(path):
    '''
    gets the assets names from the dataframes, usefull for plots
    :return: stocks name
    '''
    filename = os.path.basename(path)
    filename_without_extension = os.path.splitext(filename)[0]
    end_index = filename_without_extension.rfind('\\')
    dataframe_name = filename_without_extension[end_index + 1:] if end_index != -1 else filename_without_extension
    return dataframe_name

