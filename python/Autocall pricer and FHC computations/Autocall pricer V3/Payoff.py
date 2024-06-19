import numpy as np


class PayoffCalculator:
    def __init__(self, n, N, stocks_matrix, init_stock, T, freq_obs, BP, AT, coupon,obs_dates):
        self.n = n
        self.N = N
        self.stocks_matrix = stocks_matrix
        self.init_stock = init_stock
        self.T = T
        self.freq_obs = freq_obs
        self.BP = BP
        self.AT = AT
        self.coupon = coupon
        self.obs_dates = obs_dates
        self.evaluation_matrix = self.compute_evaluation_matrix()

    @staticmethod
    def compute_obs_dates(T, freq_obs, step_size=252):
        '''
        computes the observation dates based on the maturity of the autocall and the frequency of observations
        '''
        obs_dates = []
        obs_date = int(np.floor(252 * T))
        while obs_date > 1:
            obs_dates.insert(0, obs_date)
            obs_date -= int(step_size / freq_obs)
        return obs_dates

    def compute_evaluation_matrix(self):
        '''
        computes the evaluation matrix that we'll use later to for the payoff
        :return : matrix whose columns are the stock prices from the diffused matrix at the observation dates
        '''
        obs_dates = self.obs_dates
        if self.n > 1:
            init_stock = np.repeat(self.init_stock, [self.N for _ in range(self.n)]).reshape(self.n,self.N, 1)
            evaluation_matrix = self.stocks_matrix[:, :, obs_dates] / self.init_stock
            #evaluation_matrix = self.stocks_matrix[:, :, obs_dates] / self.stocks_matrix[:, :,0]
        else:
            evaluation_matrix = self.stocks_matrix[:, obs_dates] / np.tile(self.init_stock,self.N)[:, np.newaxis]
            #evaluation_matrix = self.stocks_matrix[:, obs_dates] / np.tile(self.stocks_matrix[0,0],self.N)[:, np.newaxis]
        return evaluation_matrix

    def compute_payoff(self):
        '''
        computes the payoff for a single or multi asset
        '''
        AT = np.full(len(self.obs_dates), self.AT)
        if self.n > 1:
            return self.compute_payoff_multi(AT)
        else:
            return self.compute_payoff_single(AT)

    def compute_payoff_multi(self, AT):
        state_matrix = np.ones((self.n, self.N, len(self.obs_dates)))
        payoff_coupon_matrix = np.zeros((self.n, self.N, len(self.obs_dates)))
        payoff_kg_matrix = np.zeros((self.n, self.N, len(self.obs_dates)))

        for obs in range(len(self.obs_dates) - 1):
            condition_1 = self.evaluation_matrix[:, :, obs] >= AT[obs]
            state_min = np.min(state_matrix[:, :, :], axis=2)
            payoff_coupon_matrix[:, :, obs] = np.where(condition_1, (self.T * (obs + 1) / len(self.obs_dates)) * self.coupon * state_min, 0)
            payoff_kg_matrix[:, :, obs] = np.where(condition_1, state_min, 0)
            state_matrix[:, :, obs] = np.where(condition_1, 0, state_matrix[:, :, obs])

        final_state_min = np.min(state_matrix[:, :, :], axis=2)
        payoff_coupon_matrix[:, :, -1], payoff_kg_matrix[:, :, -1] = self.calculate_final_payoffs(
            self.evaluation_matrix[:, :, -1], final_state_min, AT[-1], self.BP)

        payoff_matrix = payoff_coupon_matrix + payoff_kg_matrix
        return self.evaluation_matrix, payoff_coupon_matrix, payoff_kg_matrix, state_matrix, payoff_matrix

    def compute_payoff_single(self, AT):
        state_matrix = np.ones((self.N, len(self.obs_dates)))
        payoff_coupon_matrix = np.zeros((self.N, len(self.obs_dates)))
        payoff_kg_matrix = np.zeros((self.N, len(self.obs_dates)))
        for obs in range(len(self.obs_dates) - 1):
            condition_1 = self.evaluation_matrix[:, obs] >= AT[obs]
            state_min = np.min(state_matrix[:, :], axis=1)
            payoff_coupon_matrix[:, obs] = np.where(condition_1, (self.T * (obs + 1) / len(self.obs_dates)) * self.coupon * state_min, 0)
            payoff_kg_matrix[:, obs] = np.where(condition_1, state_min, 0)
            state_matrix[:, obs] = np.where(condition_1, 0, state_matrix[:, obs])

        final_state_min = np.min(state_matrix[:, :], axis=1)
        payoff_coupon_matrix[:, -1], payoff_kg_matrix[:, -1] = self.calculate_final_payoffs(
            self.evaluation_matrix[:, -1], final_state_min, AT[-1], self.BP)
        payoff_matrix = payoff_coupon_matrix + payoff_kg_matrix
        return self.evaluation_matrix, payoff_coupon_matrix, payoff_kg_matrix, state_matrix, payoff_matrix
    def calculate_final_payoffs(self, evaluation_final, state_min, AT_final, BP):
        '''
        computes the payoff at the last observation dates
        '''
        comparison_AT = evaluation_final >= AT_final
        comparison_BP = (evaluation_final < AT_final) & (evaluation_final >= BP)
        payoff_coupon_final = np.where(comparison_AT, self.T * self.coupon * state_min, 0)
        payoff_kg_final = np.where(comparison_AT, state_min, 0)
        payoff_kg_final = np.where(comparison_BP, state_min, payoff_kg_final)
        payoff_kg_final = np.where(~comparison_AT & ~comparison_BP, state_min, payoff_kg_final)
        payoff_coupon_final = np.where(~comparison_AT & ~comparison_BP, -(1 - evaluation_final) * state_min, payoff_coupon_final)
        return payoff_coupon_final, payoff_kg_final
    @staticmethod
    def compute_price(n, notional, matrice, curve, T, freq_obs):
        '''
        discounts the payoffs to compute the prices
        :return:
        '''
        obs_dates = PayoffCalculator.compute_obs_dates(T, freq_obs)
        if n > 1:
            price = [np.mean(matrice[:, :, i]) * np.exp(-0.01 * curve(obs_dates[i] / 252) * obs_dates[i] / 252) for i in
                     range(len(obs_dates))]
        else:
            price = [np.mean(matrice[:, i]) * np.exp(-0.01 * curve(obs_dates[i] / 252) * obs_dates[i] / 252) for i in
                     range(len(obs_dates))]
        return notional * np.array(price).sum()

