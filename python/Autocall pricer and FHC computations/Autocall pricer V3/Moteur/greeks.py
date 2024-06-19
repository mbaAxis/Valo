from Moteur.path_generation import path_generator
from Payoff import PayoffCalculator
import numpy as np

class GreeksComputations:

    def __init__(self, n, paths, curve, dividends_matrix, N, T, init_stock, list_params_matrix, vol_type, price,
                 payoff_matrix, freq_obs, BP, AT, coupon, notional,obs_dates):
        self.n = n
        self.paths = paths
        self.curve = curve
        self.dividends_matrix = dividends_matrix
        self.N = N
        self.T = T
        self.init_stock = init_stock
        self.list_params_matrix = list_params_matrix
        self.vol_type = vol_type
        self.price = price
        self.payoff_matrix = payoff_matrix
        self.freq_obs = freq_obs
        self.BP = BP
        self.AT = AT
        self.coupon = coupon
        self.notional = notional
        self.obs_dates = obs_dates

    def generate_prices(self, shift_type, shift_direction):
        '''
        generates stressed stock price matrices and compute their prices
        '''
        shifted_matrix = path_generator().generate_paths(
            self.n, self.paths, self.curve, self.dividends_matrix, self.N, self.T,
            self.init_stock, self.list_params_matrix, self.vol_type, shift_type, shift_direction
        )
        pc = PayoffCalculator(self.n, self.N, shifted_matrix, self.init_stock, self.T,
                         self.freq_obs, self.BP, self.AT, self.coupon, self.obs_dates)
        _,_,_,_,payoff_matrix = pc.compute_payoff()
        if self.n > 1:
            prices = [pc.compute_price(1, self.notional, payoff_matrix[i], self.curve,self.obs_dates) for i in range(payoff_matrix.shape[0])]
            return np.array(prices)
        else :
            price = pc.compute_price(1, self.notional, payoff_matrix, self.curve,self.obs_dates )
            return np.array(price)


    def compute_greeks(self):
        '''
        applies finite difference to stressed prices to compute greeks
        '''
        price_spot_up, price_spot_down, price_vol_up, price_vol_down = [self.generate_prices(greek, direction) for greek in
                                                                ['Delta', 'Gamma'] for direction in ['Up', 'Down']]
        print('S up', price_spot_up)
        print('S down', price_spot_down)
        print('Vol up', price_vol_up)
        print('Vol down', price_vol_down)
        deltas = (price_spot_up - price_spot_down) / (2 * self.init_stock * 0.1)
        gammas = (price_spot_up - 2 * self.price + price_spot_down) / (self.init_stock * 0.1) ** 2
        vegas = (price_vol_up - price_vol_down) / (2 * self.init_stock * 0.1)
        vommas = (price_vol_up - 2 * self.price + price_vol_down) / (self.init_stock * 0.1) ** 2

        if self.n == 1:
            return deltas, gammas, vegas, vommas
        else:
            return deltas.sum(), gammas.sum(), vegas.sum(), vommas.sum()




