from tests_plots.plots import test_calibration,test_spread_calendar_arbitrage
import numpy as np
import matplotlib.pyplot as plt
import xlwings as xw

def test_calibration_to_excel(wb,curve,data,dataframe_name,cell='A1'):
    test_calibration(curve, data, dataframe_name)
    #wb = xw.Book('Autocall pricer.xlsm')
    sheet = wb.sheets('Calibration_Plots')
    image_path = r'C:\Users\m.ben-el-ghoul\PycharmProjects\Autocall pricer V2 - Copie\plots\Calibration\Calibration_plot.png'
    sheet.pictures.add(image_path, name='Calibration', update=True,
                       left=sheet.range(cell).left, top=sheet.range(cell).top, scale=1)

def test_spread_calendar_to_excel(wb,dataframe_name_list,list_data,list_params,cell='A25'):
    test_spread_calendar_arbitrage(dataframe_name_list,list_data,list_params)
    #wb = xw.Book('Autocall pricer.xlsm')
    sheet_arb = wb.sheets('Calibration_plots')
    if len(dataframe_name_list) == 1:
        image_path = r'C:\Users\m.ben-el-ghoul\PycharmProjects\Autocall pricer V2 - Copie\plots\Calendar-spread-plot.png'
        sheet_arb.pictures.add(image_path, name='Calendar-spread-plot', update=True,
                           left=sheet_arb.range(cell).left, top=sheet_arb.range(cell).top, scale=1)