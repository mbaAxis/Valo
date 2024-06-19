import numpy as np
from Moteur.path_generation import path_generator
from Moteur.greeks import GreeksComputations
from Payoff import PayoffCalculator
from data_importation_preprocessing.data_getters import getters
import bisect
loaded_matrix = np.load(r'C:\Users\m.ben-el-ghoul\PycharmProjects\Autocall pricer V3\matrix.npy')
params = np.load(r'C:\Users\m.ben-el-ghoul\PycharmProjects\Autocall pricer V3\params.npy')
def price_eval_matrix(N,obs_dates,evaluation_matrix,AT,total_nbr_obs,k):
    state_matrix = np.ones((N, len(obs_dates)))
    payoff_coupon_matrix = np.zeros((N, len(obs_dates)))
    payoff_kg_matrix = np.zeros((N, len(obs_dates)))

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
    print('hedge intervals',hedge_intervals)
    hedge_dates = np.array([int(i*hedge_intervals) for i in range(1,int(np.ceil(T*step_size/hedge_intervals)))])
    portfolio_value = init_price-init_spot*init_delta
    transaction_cost = 0
    obs_dates = PayoffCalculator.compute_obs_dates(T, freq_obs)
    print(obs_dates)
    total_nbr_obs = len(obs_dates)
    nbr_obs_bef_hedge = count_nbr_hedges(obs_dates,hedge_dates)
    sm, sm_up, sm_down = [np.ones((N,total_nbr_obs)) for i in range(3)]
    active, active_up, active_down = [np.min(mat[:, :], axis=1) for mat in [sm, sm_up, sm_down]]
    prev_pos, k = [0,0]
    transaction_costs = []
    portfolio_values = []
    transaction_costs.append(transaction_cost)
    portfolio_values.append(portfolio_value)
    print('first slice from original matrix', S_matrix[:,63]/init_spot)
    print('aaaa')
    S_matrix_up,S_matrix_down = [path_generator().generate_paths(n, paths, curve, dividends_matrix, N,T,init_spot,
                                                   list_params_matrix, vol_type, 'Delta', shock)
                                                    for shock in ['Up','Down']]
    for hedge_date in hedge_dates[:-1]:
        discount_factor = np.exp(curve(hedge_date/252)/100 * hedge_date/252)
        print(hedge_date)
        obs_dates_new=[x for x in obs_dates-hedge_date if x > 0]
        position = bisect.bisect_left(obs_dates, hedge_date)
        if position != prev_pos :
            active, active_up, active_down = [np.min(mat[:, :], axis=1) for mat in [sm, sm_up, sm_down]]
            prev_pos = position
            k = k+1
        matrix,matrix_up, matrix_down = [mat[:,hedge_date:] for mat in [S_matrix,S_matrix_up,S_matrix_down]]
        hedge_spot = matrix[:,0]
        eval_mat, eval_mat_up, eval_mat_down = [mat[:,obs_dates_new]/matrix[:,0][:, np.newaxis]*act[:, np.newaxis] for mat,act in zip(
            [matrix,matrix_up,matrix_down],[active,active_up,active_down])]
        results = [compute_payoff(n, N, evaluation_matrix, obs_dates_new, AT, total_nbr_obs, k)
                   for evaluation_matrix in [eval_mat, eval_mat_up, eval_mat_down]]
        (_, _, _, sm, pm),(_, _, _, sm_up, pm_up), (_, _, _, sm_down, pm_down) = results
        pm,pm_up,pm_down = [get_active_paths(payoff_matrix) for payoff_matrix in [pm,pm_up,pm_down]]
        '''print('up')
        for col in range(pm_up.shape[1]):
            print('Returns from observation date '+ str(obs_dates_new[col]),np.sum(pm_up[:, col]))
        print('normal')
        for col in range(pm.shape[1]):
            print('Returns from observation date ' + str(obs_dates_new[col]), np.sum(pm[:, col]))
        print('down')
        for col in range(pm_down.shape[1]):
            print('Returns from observation date ' + str(obs_dates_new[col]), np.sum(pm_down[:, col]))'''
        print('number of paths left in normal ',pm.shape[0])
        print('number of paths left in up ',pm_up.shape[0])
        print('number of paths left in down ', pm_down.shape[0])
        price,price_up,price_down = [compute_price(n, notional, payoff_mat, curve, obs_dates_new) for payoff_mat
                                     in [pm,pm_up,pm_down]]
        print('price', price)
        print('price up', price_up)
        print('price_down', price_down)
        delta = (price_up-price_down)/(2*init_spot*0.15)
        sm,sm_up,sm_down = [mat*act[:,np.newaxis] for mat, act in zip([sm,sm_up,sm_down],[active,active_up,active_down])]
        # Computing the delta around the hedging date
        print('delta',delta)
        portfolio_value = portfolio_value*discount_factor - (delta - init_delta)*hedge_spot.mean()
        transaction_cost = transaction_cost*discount_factor-ks*np.abs((delta - init_delta))*hedge_spot.mean()
        init_delta = delta
        transaction_costs.append(transaction_cost)
        portfolio_values.append(portfolio_value)
    print(transaction_costs)
    print(portfolio_values)
    print('hedging cost', transaction_cost * (-1) * np.exp(-curve(T) / 100 * T))
    return transaction_costs, portfolio_values



paths = r'C:\Users\m.ben-el-ghoul\PycharmProjects\Autocall pricer\data and keys\AIRBUS_25_04_2024.xlsx'
vol_type = 'LV'
list_params_matrix = params
dividends_matrix = np.array([1])
n = 1
N = 512
S_matrix = loaded_matrix
init_spot = S_matrix[0,0]
freq_obs = 4
BP = 0.5
AT = 1
coupon = 0.1
T = 1
nbr_hedges = 11
init_price = 100.77952015479252
init_delta = 0.010976173342844592
curve = getters().get_riskless_rates()
notional = 100
ks = 0.01
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