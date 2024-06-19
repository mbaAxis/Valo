import matplotlib.pyplot as plt
import numpy as np
from Moteur.SVI_Interpolation import SVIModel


def g(params, x):
    a, b, rho, m, sig = params
    discr = np.sqrt((x - m) ** 2 + sig ** 2)
    w = a + b * (rho * (x - m) + discr)
    dw = b * rho + b * (x - m) / discr
    d2w = b * sig ** 2 / (discr ** 3)
    return (1 - (x * dw) / (2 * w)) ** 2 - ((dw ** 2) / 4) * (1 / w + 0.25) + d2w / 2
def test_calibration(curve,data,dataframe_name,k):
    method_name = "SLSQP"
    xx = len(data.Maturity.unique())
    nrows = int(np.ceil(np.sqrt(xx)))
    ncols = int(np.ceil(xx / nrows))
    fig, ax = plt.subplots(nrows, ncols, figsize=(12, 10), sharex=True, sharey=True)
    fig.suptitle('Calibration Results for ' + dataframe_name + ' puts', fontsize=16)
    last_params = np.zeros(5)
    for idx, ttm in enumerate(sorted(data.Maturity.unique())):
        if ttm < 0.0191 or idx >= nrows * ncols:
            continue
        else:
            moneyness = data[data.Maturity == ttm].Log_Moneyness
            itv = data[data.Maturity == ttm]['market_itv']
            a, b, rho, m, s = SVIModel().calibration(last_params,itv, moneyness, [0.000001, 0.09, 0.15, 0.09, 0.03], method_name, 20)
            last_params = np.array([a, b, rho, m, s])
            m_vec = np.linspace(min(moneyness), max(moneyness), 50)
            iv_vec = np.sqrt(SVIModel().SVI(m_vec, a, b, rho, m, s) / ttm)
            market_vol = data[data.Maturity == ttm].IV

            row = idx % nrows if xx != nrows * ncols else idx // ncols
            col = idx // nrows if xx != nrows * ncols else idx % ncols

            ax[row, col].scatter(moneyness, market_vol, label="Market IV")
            ax[row, col].plot(m_vec, iv_vec, label="Model IV", color='orange')
            ax[row, col].set_title("Maturity = {:.2f}".format(ttm), fontsize=10)
            ax[row, col].legend(loc="best")

    plt.xlabel("Log Moneyness", fontsize=12)
    plt.ylabel("Implied Volatility", fontsize=12)
    plt.tight_layout(rect=[0, 0.03, 1, 0.95])  # Adjust the space for the big title
    image_path = r'C:\Users\m.ben-el-ghoul\PycharmProjects\Autocall pricer V2 - Copie\plots\calibration\Calibration_plot-'+str(k)+'.png'
    plt.savefig(image_path)
    plt.show()
    #return fig
    #plt.show()

def test_spread_calendar_arbitrage(dataframe_name_list,list_data,list_params):
    if len(list_params) == 1:
        data = list_data[0]
        params = list_params[0]
        for i in range(params.shape[0]):
            ttm = params[i, 0]
            log_moneyness_forward = data[data.Maturity == ttm].Log_Moneyness
            if ttm < 0.0191:
                continue
            else:
                itv_vec = SVIModel().svi(log_moneyness_forward,params[i, 1:])
                plt.plot(log_moneyness_forward,itv_vec,label = str(round(ttm,5)))
                plt.xlabel("Log Moneyness Forward", fontsize=10)
                plt.ylabel("Model Implied Total Variance", fontsize=10)
                plt.title('No intersection - Free of calendar spread arbitrage ' + dataframe_name_list[0])
        plt.legend()
        image_path = r'C:\Users\m.ben-el-ghoul\PycharmProjects\Autocall pricer V2 - Copie\plots\Calendar-spread-plot.png'
        plt.savefig(image_path)
        plt.show()

    else :
        for idx, params in enumerate(list_params):
            data = list_data[idx]
            for i in range(params.shape[0]):
                ttm = params[i, 0]
                a, b, rho, m, s = params[i, 1:]
                if ttm < 0.0191:
                    continue
                else:
                    log_moneyness_forward = data[data.Maturity == ttm].Log_Moneyness
                    itv_vec = SVIModel().SVI(log_moneyness_forward, a, b, rho, m, s)
                    plt.plot(log_moneyness_forward,itv_vec,label=str(round(ttm, 5)))
                    #market_itv = data[data.Maturity == ttm].market_itv
                    #plt.scatter(log_moneyness_forward, market_itv, label=str(round(ttm, 5)))
                plt.xlabel("Log Moneyness Forward", fontsize=10)
                plt.ylabel("Model Implied Total Variance", fontsize=10)
                plt.legend()
                plt.title('No intersection - Free of calendar spread arbitrage ' + dataframe_name_list[idx])
            plt.savefig(r'C:\Users\m.ben-el-ghoul\PycharmProjects\Autocall pricer V2 - Copie\plots\calendar_spread\calendar-spread-'+str(idx+1)+'.png')
            plt.show()

def test_butterfly_arbitrage(dataframe_name_list,data_list,list_params):
    log_moneyness = np.linspace(-0.4, 0.6, 50)
    if len(list_params) == 1 :
        params = list_params[0]
        slice_count = params.shape[0]  # Number of slices for the current parameter matrix
        for i in range(slice_count):
            g_list_non_arbitrage = [g(params[i, 1:], x) for x in log_moneyness]
            plt.plot(log_moneyness, g_list_non_arbitrage, label = str(round(params[i,0],4)))
            plt.title(dataframe_name_list[0] + ' No butterfly arbitrage')
        plt.axhline(y = 0)
        plt.legend()
        image_path = r'C:\Users\m.ben-el-ghoul\PycharmProjects\Autocall pricer V2 - Copie\plots\Butterfly_plot.png'
        plt.savefig(image_path)
        plt.show()
    else :
        for idx, params in enumerate(list_params):
            slice_count = params.shape[0]  # Number of slices for the current parameter matrix
            for i in range(slice_count):
                g_list_non_arbitrage = [g(params[i, 1:], x) for x in log_moneyness]
                plt.plot(log_moneyness, g_list_non_arbitrage, label=str(round(params[i, 0], 4)))
                plt.title(dataframe_name_list[idx] + ' No butterfly arbitrage')
            plt.axhline(y=0)
            plt.legend()
            plt.savefig(r'C:\Users\m.ben-el-ghoul\PycharmProjects\Autocall pricer V2 - Copie\plots\butterfly\butterfly-' + str(idx + 1) + '.png')
            plt.show()

def plot_volatility_surface(n,vol_type,list_params,data_list,dataframe_names_list):
    for idx, data in enumerate(data_list):
        fig = plt.figure()
        ax = fig.add_subplot(111, projection='3d')
        min_ttm = min(data.Maturity)
        max_ttm = max(data.Maturity)
        min_logm = min(data.Log_Moneyness)
        max_logm = max(data.Log_Moneyness)
        logm = np.linspace(min_logm, max_logm, 100)
        ttm = np.linspace(min_ttm, max_ttm, 100)
        v_market = np.zeros((ttm.shape[0], logm.shape[0]))
        if vol_type == 'LV':
            for i in range(ttm.shape[0]):
                t = ttm[i]
                v_market[i,:] = SVIModel().compute_local_vol(1, logm, t, list_params[idx])[0]
            logm_grid, ttm_grid = np.meshgrid(logm, ttm)
            surf = ax.plot_surface(logm_grid, ttm_grid, v_market, cmap='viridis')

            # Set labels and title
            ax.set_xlabel('Log Moneyness')
            ax.set_ylabel('Time-to-Maturity')
            ax.set_zlabel('Local Volatility')
            ax.set_title('Volatility Surface ' + dataframe_names_list[idx])
            fig.colorbar(surf)
            if n == 1 :
                plt.savefig(r'C:\Users\m.ben-el-ghoul\PycharmProjects\Autocall pricer V2 - Copie\plots\vol_surf-' + str(idx + 1) + '.png')
            else :
                plt.savefig(r'C:\Users\m.ben-el-ghoul\PycharmProjects\Autocall pricer V2 - Copie\plots\vol_surf\vol_surf-' + str(idx + 1) + '.png')
            plt.show()
        if vol_type == 'BS_IV':
            for i in range(ttm.shape[0]):
                t = ttm[i]
                v_market[i, :] = SVIModel().implied_vol(1, logm, t, list_params[idx])[0]
            logm_grid, ttm_grid = np.meshgrid(logm, ttm)
            surf = ax.plot_surface(logm_grid, ttm_grid, v_market, cmap='viridis')

            # Set labels and title
            ax.set_xlabel('Log Moneyness')
            ax.set_ylabel('Time-to-Maturity')
            ax.set_zlabel('Implied Volatility')
            ax.set_title('Volatility Surface ' + dataframe_names_list[idx])
            fig.colorbar(surf)
            if n == 1:
                plt.savefig(r'C:\Users\m.ben-el-ghoul\PycharmProjects\Autocall pricer V2 - Copie\plots\vol_surf-' + str(
                    idx + 1) + '.png')
            else:
                plt.savefig(
                    r'C:\Users\m.ben-el-ghoul\PycharmProjects\Autocall pricer V2 - Copie\plots\vol_surf\vol_surf-' + str(
                        idx + 1) + '.png')
            plt.show()






def plot_stock_path(S_mat,n,T):
    x_axis = np.linspace(0,T,T*252)
    for k in range(S_mat.shape[0]):
        print(S_mat.shape)
        plt.plot(x_axis,S_mat[k,:])
    plt.show()


