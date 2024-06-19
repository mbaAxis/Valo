import numpy as np
from Moteur.path_generation import path_generator
from Moteur.greeks import GreeksComputations
from Payoff import PayoffCalculator
from data_importation_preprocessing.data_getters import getters
import bisect
loaded_matrix = np.load(r'C:\Users\m.ben-el-ghoul\PycharmProjects\Autocall pricer V3\matrix.npy')
params = np.load(r'C:\Users\m.ben-el-ghoul\PycharmProjects\Autocall pricer V3\params.npy')
def price_eval_matrix(N,obs_dates,evaluation_matrix,AT,total_nbr_obs,k):
    state_matrix = np.ones((evaluation_matrix.shape[0], len(obs_dates)))
    payoff_coupon_matrix = np.zeros((evaluation_matrix.shape[0], len(obs_dates)))
    payoff_kg_matrix = np.zeros((evaluation_matrix.shape[0], len(obs_dates)))

    for obs in range(len(obs_dates) - 1):
        condition_1 = evaluation_matrix[:, obs] >= AT[obs]
        state_min = np.min(state_matrix[:, :], axis=1)
        payoff_coupon_matrix[:, obs] = np.where(condition_1, (T * (obs + 1+ k) / total_nbr_obs) * coupon * state_min, 0)
        payoff_kg_matrix[:, obs] = np.where(condition_1, state_min, 0)
        state_matrix[:, obs] = np.where(condition_1, 0, state_matrix[:, obs])

    final_state_min = np.min(state_matrix[:, :], axis=1)
    payoff_coupon_matrix[:, -1], payoff_kg_matrix[:, -1] = calculate_final_payoffs(
        evaluation_matrix[:, -1], final_state_min, AT[-1], BP)
    payoff_matrix = payoff_coupon_matrix + payoff_kg_matrix
    return evaluation_matrix, payoff_coupon_matrix, payoff_kg_matrix, state_matrix, payoff_matrix

def calculate_final_payoffs(evaluation_final, state_min, AT_final, BP):
    '''
    computes the payoff at the last observation dates
    '''
    comparison_AT = evaluation_final >= AT_final
    comparison_BP = (evaluation_final < AT_final) & (evaluation_final >= BP)
    payoff_coupon_final = np.where(comparison_AT,T * coupon * state_min, 0)
    payoff_kg_final = np.where(comparison_AT, state_min, 0)
    payoff_kg_final = np.where(comparison_BP, state_min, payoff_kg_final)
    payoff_kg_final = np.where(~comparison_AT & ~comparison_BP, state_min, payoff_kg_final)
    payoff_coupon_final = np.where(~comparison_AT & ~comparison_BP, -(1 - evaluation_final) * state_min, payoff_coupon_final)
    return payoff_coupon_final, payoff_kg_final
def compute_payoff(n,N,evaluation_matrix,obs_dates,AT,total_nbr_obs,k):
    '''
    computes the payoff for a single or multi asset
    '''
    AT = np.full(len(obs_dates),AT)
    return price_eval_matrix(N,obs_dates,evaluation_matrix,AT,total_nbr_obs,k)

def compute_price(n, notional, matrice, curve,obs_dates_new):
    '''
    discounts the payoffs to compute the prices
    :return:
    '''
    if n > 1:
        price = [np.mean(matrice[:, :, i]) * np.exp(-0.01 * curve(obs_dates_new[i] / 252) * obs_dates_new[i] / 252) for i in
                 range(len(obs_dates_new))]
    else:
        price = [np.mean(matrice[:, i]) * np.exp(-0.01 * curve(obs_dates_new[i] / 252) * obs_dates_new[i] / 252) for i in
                 range(len(obs_dates_new))]
    return notional * np.array(price).sum()
def count_nbr_hedges(array1, array2):
    array = np.zeros_like(array2)
    i, count = 0, 0
    for j, elem in enumerate(array2):
        while i < len(array1) and array1[i] < elem:
            count += 1
            i += 1
        array[j] = count
    result = np.zeros_like(array)
    result[0] = array[0]
    result[1:] = array[1:] - array[:-1]
    return result
def get_active_paths(payoff_matrix):
    non_zero_rows = ~np.all(payoff_matrix == 0, axis=1)
    payoff_matrix= payoff_matrix[non_zero_rows]
    return payoff_matrix

def FHC_MC_approach(n,N,S_matrix,T,init_price,init_delta,init_spot,nbr_hedges,freq_obs,BP,AT,coupon,step_size=252):
    hedge_intervals = T*step_size/nbr_hedges
    hedge_dates = np.array([int(i*hedge_intervals) for i in range(1,int(np.ceil(T*step_size/hedge_intervals)))])
    obs_dates = PayoffCalculator.compute_obs_dates(T, freq_obs)
    prev_pos = 0
    called_paths = []
    for hedge_date in hedge_dates:
        init_spot = S_matrix[:,int(hedge_intervals)].mean()
        S_matrix = path_generator().generate_paths(n, paths, curve, dividends_matrix, N, (T - hedge_date / 252), init_spot,
                                            list_params_matrix, vol_type, None, None)
        print(S_matrix)
        print(S_matrix.shape)
        obs_dates_new = [x for x in obs_dates - hedge_date if x > 0]
        position = bisect.bisect_left(obs_dates, hedge_date)
        if position != prev_pos:
            called_paths.append(np.where(S_matrix[:,obs_dates_new[0]] > init_spot))
            print(called_paths)
            prev_pos = position
    return  2,3


paths = r'C:\Users\m.ben-el-ghoul\PycharmProjects\Autocall pricer\data and keys\AIRBUS_25_04_2024.xlsx'
vol_type = 'LV'
list_params_matrix = params
dividends_matrix = np.array([1])
n = 1
N = 32
S_matrix = loaded_matrix
init_spot = S_matrix[0,0]
freq_obs = 4
BP = 0.5
AT = 1
coupon = 0.1
T = 1
nbr_hedges = 11
init_price = 100.07375021055141
init_delta = 0.013648274404524261
curve = getters().get_riskless_rates()
notional = 100
ks = 0.01
np.set_printoptions(threshold=1000)
a,b = FHC_MC_approach(n,N,S_matrix,T,init_price,init_delta,init_spot,nbr_hedges,freq_obs,BP,AT,coupon)



#Façon brute ( comme dans la thése ) avec des frequences de hedge bien determiné
#Façon brute ( comme dans la thése ) avec des limites sur les sensi
#Façon BNP .

# what i have done so far
    # managed to compute the autocall prices at each hedging date
    #verify the results with less timesteps and more paths ( or even create a matrix of your own )
# Next
    # evaluate delta at each hedging date


#After rehedging change the init stock price

# In this approach, a spot matrices ( stressed ) are produced from the beginning and at each hedging date
# we extract what is left from the initial matrices and price. Since we have only one matrix ( and two stressed ) we can be aware
# of what happened in the past and kill the paths that have been called


# The problem is a lot of paths are called from the begining so that in the end i have nothing left to compute delta
# Payoff matrix is Nan


# Look how to replicate payoffs when not martingale
# how to delta hedge something is not a martingale