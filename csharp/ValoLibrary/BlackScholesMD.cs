using Microsoft.Office.Interop.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValoLibrary
{
    public class Parameters
    {
        public string optionFlag;
        public string position;
        public double strike;
        public double maturity;
        public string underlying;
        public double? dividend;
    }
    public class BlackScholesMD
    {
        public static double OptionPrice(string optionFlag, string position, double k, double T, string underlying, double? q = null)
        {
            double s = GetData.GetSpot(underlying);
            double r = Calibration.GetRepo(underlying, T);
            q =  q ?? Calibration.GetDividend(underlying, T);
            double date = GetData.GetTime(underlying);
            double timeToMaturity = (T - date) / 365;
            double price = Calibration.InterpolatePrice(k, T, underlying);
            double vol = BlackScholes.ImpliedVol(optionFlag, position, s, price, r, k, timeToMaturity, q);
            double P = BlackScholes.BSOptionPrice(optionFlag, position, s, vol, r, k, timeToMaturity, q);
            return P;
        }

        public static double Delta(string optionFlag, string position, double k, double T, string underlying, double? q = null)
        {
            double s = GetData.GetSpot(underlying);
            double r = Calibration.GetRepo(underlying, T);
            q = q  ?? Calibration.GetDividend(underlying, T);
            double date = GetData.GetTime(underlying);
            double timeToMaturity = (T - date) / 365;
            double price = Calibration.InterpolatePrice(k, T, underlying);
            double vol = Math.Abs(BlackScholes.ImpliedVol(optionFlag, position, s, price, r, k, timeToMaturity, q));
            double sensitivity = BlackScholes.DeltaBS(optionFlag, position, s, vol, r, k, timeToMaturity, q);
            return sensitivity;
        }
        public static double Gamma(string optionFlag, string position, double k, double T, string underlying, double?q = null)
        {
            double s = GetData.GetSpot(underlying);
            double r = Calibration.GetRepo(underlying, T);
            q = q ?? Calibration.GetDividend(underlying, T);
            double date = GetData.GetTime(underlying);
            double timeToMaturity = (T - date) / 365;
            double price = Calibration.InterpolatePrice(k, T, underlying);
            double vol = Math.Abs(BlackScholes.ImpliedVol(optionFlag, position, s, price, r, k, timeToMaturity, q));
            double sensitivity = BlackScholes.GammaBS(optionFlag, position, s, vol, r, k, timeToMaturity, q);
            return sensitivity;
        }
        public static double Theta(string optionFlag, string position, double k, double T, string underlying, double? q = null)
        {
            double s = GetData.GetSpot(underlying);
            double r = Calibration.GetRepo(underlying, T);
            q = q ?? Calibration.GetDividend(underlying, T);
            double date = GetData.GetTime(underlying);
            double timeToMaturity = (T - date) / 365;
            double price = Calibration.InterpolatePrice(k, T, underlying);
            double vol = Math.Abs(BlackScholes.ImpliedVol(optionFlag, position, s, price, r, k, timeToMaturity, q));
            double sensitivity = BlackScholes.ThetaBS(optionFlag, position, s, vol, r, k, timeToMaturity, q);
            return sensitivity;
        }

        public static double Vega(string optionFlag, string position, double k, double T, string underlying, double? q = null)
        {
            double s = GetData.GetSpot(underlying);
            double r = Calibration.GetRepo(underlying, T);
            q = q ?? Calibration.GetDividend(underlying, T);
            double date = GetData.GetTime(underlying);
            double timeToMaturity = (T - date) / 365;
            double price = Calibration.InterpolatePrice(k, T, underlying);
            double vol = Math.Abs(BlackScholes.ImpliedVol(optionFlag, position, s, price, r, k, timeToMaturity, q));
            double sensitivity = BlackScholes.VegaBS(optionFlag, position, s, vol, r, k, timeToMaturity, q);
            return sensitivity;
        }

        public static double Rho(string optionFlag, string position, double k, double T, string underlying, double? q= null)
        {
            double s = GetData.GetSpot(underlying);
            double r = Calibration.GetRepo(underlying, T);
            q = q ?? Calibration.GetDividend(underlying, T);
            double date = GetData.GetTime(underlying);
            double timeToMaturity = (T - date) / 365;
            double price = Calibration.InterpolatePrice(k, T, underlying);
            double vol = Math.Abs(BlackScholes.ImpliedVol(optionFlag, position, s, price, r, k, timeToMaturity, q));
            double sensitivity = BlackScholes.RhoBS(optionFlag, position, s, vol, r, k, timeToMaturity, q);
            return sensitivity;
        }

        public static double[,] SensiOption(string optionFlag, string position, double k, double T, string underlying, double? q = null)
        {
            double[,] sensitivities = new double[5, 1]; // Tableau 2D pour simuler une colonne

            sensitivities[0, 0] = Delta(optionFlag, position, k, T, underlying, q);
            sensitivities[1, 0] = Gamma(optionFlag, position, k, T, underlying, q);
            sensitivities[2, 0] = Theta(optionFlag, position, k, T, underlying, q);
            sensitivities[3, 0] = Vega(optionFlag, position, k, T, underlying, q);
            sensitivities[4, 0] = Rho(optionFlag, position, k, T, underlying, q);



            return sensitivities;
        }



        ////// option portfolio
        ///
        public static double OptionPortfolioPrice(int numberOfOptions, Parameters[] options)
        {
            if (options.Length != numberOfOptions)
            {
                throw new ArgumentException("Le nombre d'options ne correspond pas à la valeur spécifiée.");
            }

            double portfolioPrice = 0;

            foreach (Parameters option in options)
            {
                double optionPrice = OptionPrice(option.optionFlag, option.position, option.strike, option.maturity, option.underlying, option.dividend);

                portfolioPrice += optionPrice;
            }

            return portfolioPrice;
        }

        


    }
   


}
