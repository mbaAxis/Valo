import numpy as np
from Moteur.path_generation import path_generator
from Moteur.greeks import GreeksComputations
from Payoff import PayoffCalculator
from data_importation_preprocessing.data_getters import getters
import bisect
import matplotlib.pyplot as plt
loaded_matrix = np.load(r'C:\Users\m.ben-el-ghoul\PycharmProjects\Autocall pricer V3\matrix.npy')
params = np.load(r'C:\Users\m.ben-el-ghoul\PycharmProjects\Autocall pricer V3\params.npy')

class Autocall_delta_hedging:
    def __init__(self,n,N, paths,T,dividends_matrix,list_params_matrix,vol_type,notional,freq_obs, BP, AT, coupon,curve,nbr_hedges):
        self.n = n
        self.N = N
        self.paths = paths
        self.T = T
        self.dividends_matrix = dividends_matrix
        self.list_params_matrix = list_params_matrix
        self.vol_type = vol_type
        self.notional = notional
        self.freq_obs = freq_obs
        self.BP = BP
        self.AT = AT
        self.coupon = coupon
        self.curve = curve
        self.nbr_hedges = nbr_hedges
        self.obs_dates = PayoffCalculator.compute_obs_dates(self.T,self.freq_obs)
    def compute_payoff(self,evaluation_matrix,obs_dates_new,total_nbr_obs,k):
        AT = np.full(len(obs_dates_new),self.AT)
        state_matrix = np.ones((self.N,len(obs_dates_new)))
        payoff_coupon_matrix = np.zeros((self.N,len(obs_dates_new)))
        payoff_kg_matrix = np.zeros((self.N,len(obs_dates_new)))
        for obs in range(len(obs_dates_new) - 1):
            condition_1 = evaluation_matrix[:, obs] >= AT[obs]
            state_min = np.min(state_matrix[:, :], axis=1)
            payoff_coupon_matrix[:, obs] = np.where(condition_1, (self.T * (obs + 1 + k) / total_nbr_obs) * self.coupon * state_min,0)
            payoff_kg_matrix[:, obs] = np.where(condition_1, state_min, 0)
            state_matrix[:, obs] = np.where(condition_1, 0, state_matrix[:, obs])
        final_state_min = np.min(state_matrix[:, :], axis=1)
        payoff_coupon_matrix[:, -1], payoff_kg_matrix[:, -1] = PayoffCalculator.calculate_final_payoffs(self.T,evaluation_matrix[:, -1], final_state_min, AT[-1],self.BP,self.coupon)
        payoff_matrix = payoff_coupon_matrix + payoff_kg_matrix
        return evaluation_matrix, payoff_coupon_matrix, payoff_kg_matrix, state_matrix, payoff_matrix
    @staticmethod
    def compute_hedge_dates(T,nbr_hedges,step_size = 252):
        hedge_intervals = T * step_size / nbr_hedges
        hedge_dates = np.array([int(i * hedge_intervals) for i in range(1, int(np.ceil(T * step_size / hedge_intervals)))])
        return hedge_dates

    def compute_price_at_hedge_date(self,coupon_counter, position_counter,obs_dates,hedge_date,init_spot):
        position = bisect.bisect_left(obs_dates, hedge_date)
        if position != coupon_counter:
            position_counter = position
            coupon_counter = coupon_counter + 1
        obs_dates_new = [x for x in obs_dates - hedge_date if x > 0]
        matrix, matrix_up, matrix_down = [
            path_generator().generate_paths(self.n, self.paths, self.curve, self.dividends_matrix, self.N, (self.T - hedge_date / 252),init_spot,
                                            self.list_params_matrix, self.vol_type, 'Delta', shock) for shock in [None, 'Up', 'Down']]
        init_spot = matrix[:, int(self.T * 252 / self.nbr_hedges)].mean()
        spot_hedge_date = matrix[:,0].mean()
        eval_matrix, eval_matrix_up, eval_matrix_down = [mat[:, obs_dates_new] / matrix[:, 0][:, np.newaxis] for mat in
                                                         [matrix, matrix_up, matrix_down]]
        results = [self.compute_payoff(evaluation_matrix, obs_dates_new, len(obs_dates), coupon_counter)
                   for evaluation_matrix in [eval_matrix, eval_matrix_up, eval_matrix_down]]
        (_, _, _, sm, pm), (_, _, _, sm_up, pm_up), (_, _, _, sm_down, pm_down) = results
        price, price_up, price_down = [PayoffCalculator.compute_price(self.n, self.notional, payoff_matrix, self.curve, obs_dates_new) for payoff_matrix in [pm, pm_up, pm_down]]
        return coupon_counter, position_counter, price, price_up, price_down,spot_hedge_date,init_spot

    def initialize_portfolio(self, init_price, init_spot, init_delta):
        portfolio_value = init_price - init_spot * init_delta
        transaction_cost = 0
        return portfolio_value, transaction_cost, [portfolio_value], [transaction_cost]
    def FHC_MC_approach(self,S_matrix,T,init_price,init_delta,init_spot,nbr_hedges,ks = 0.001,step_size=252):
        hedge_dates = Autocall_delta_hedging.compute_hedge_dates(self.T,nbr_hedges)
        obs_dates = PayoffCalculator.compute_obs_dates(self.T, self.freq_obs)
        portfolio_value, transaction_cost, portfolio_values, transaction_costs = self.initialize_portfolio(init_price, init_spot, init_delta)
        portfolio_no_hedging = []
        portfolio_no_hedging.append(init_price)
        print(obs_dates)
        print(hedge_dates)
        position_counter, coupon_counter = 0,0
        init_spot = S_matrix[:, int(self.T * 252 / nbr_hedges)].mean()
        for hedge_date in hedge_dates:
            print(hedge_date)
            coupon_counter,position_counter, price, price_up, price_down,spot_hedge_date,init_spot = self.compute_price_at_hedge_date(coupon_counter,position_counter,obs_dates,hedge_date,init_spot)
            discount_factor = np.exp(self.curve(hedge_date / 252) / 100 * hedge_date / 252)
            print('price',price)
            print('price up', price_up)
            print('price down',price_down)
            delta = ((price_up - price_down)/(2*spot_hedge_date*0.15))
            print('delta',delta)
            portfolio_value = portfolio_value*discount_factor - (delta - init_delta) * spot_hedge_date
            transaction_cost = transaction_cost * discount_factor - ks * np.abs(delta - init_delta) * spot_hedge_date
            init_delta = delta
            transaction_costs.append(transaction_cost)
            portfolio_values.append(portfolio_value)
            portfolio_no_hedging.append(price)
        print('hedging cost', transaction_cost*(-1)*np.exp(-self.curve(self.T)/100 * self.T))
        return transaction_costs,portfolio_values,portfolio_no_hedging




# Example usage
# Ensure to define all required variables and imports before running the example usage
# n, N, S_matrix, T, init_price, init_delta, init_spot, nbr_hedges, freq_obs, BP, AT, coupon, step_size
# auto_call = DeltaHedgingAutoCall(...)
# obs_dates, hedge_dates, portfolio_values, transaction_costs = auto_call.run_hedging_strategy()


#paths = r'C:\Users\m.ben-el-ghoul\PycharmProjects\Autocall pricer\data and keys\AIRBUS_25_04_2024.xlsx'
'''vol_type = 'LV'
list_params_matrix = params
dividends_matrix = np.array([1])
n = 1
N = 8192
S_matrix = loaded_matrix
init_spot = S_matrix[0,0]
freq_obs = 4
BP = 0.5
AT = 1
coupon = 0.1
T = 1
nbr_hedges = 11
init_price = 96.64042793081664
init_delta = 0.03228550160293015
curve = getters().get_riskless_rates()
notional = 100
ks = 0.01
transaction_costs,portfolio_values,portfolio_no_hedging = Autocall_delta_hedging(n,N,T,freq_obs,BP,AT,coupon).FHC_MC_approach(S_matrix,T,init_price,init_delta,init_spot,nbr_hedges,step_size=252)
#a,b = FHC_MC_approach(n,N,S_matrix,T,init_price,init_delta,init_spot,nbr_hedges,freq_obs,BP,AT,coupon)
hedge_dates = Autocall_delta_hedging.compute_hedge_dates(T,nbr_hedges)
hedge_dates = np.insert(hedge_dates,0,0)
plt.plot(hedge_dates, np.array(portfolio_values), label = 'Hedged')
plt.plot(hedge_dates, np.array(portfolio_no_hedging), label = 'No hedge')
plt.legend()
plt.show()'''
#Façon brute ( comme dans la thése ) avec des frequences de hedge bien determiné
#Façon brute ( comme dans la thése ) avec des limites sur les sensi
#Façon BNP .

# what i have done so far
    # managed to compute the autocall prices at each hedging date
    #verify the results with less timesteps and more paths ( or even create a matrix of your own )
# Next
    # evaluate delta at each hedging date


#After rehedging change the init stock price

# In this approach at each hedging date a spot matrix is generated, since the diffusion happens for dates in the future
# we don't know which paths are autocalled so we don't kill any path.



# Payoff vs price