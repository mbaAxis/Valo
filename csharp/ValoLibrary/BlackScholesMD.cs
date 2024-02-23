using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValoLibrary
{
    public class BlackScholesMD
    {
        public static double OptionPrice(string optionFlag, double K, double T, string underlying, double? q = null)
        {
            double S = GetData.GetSpot(underlying);
            double r = Calibration.GetRepo(underlying, T);
            q = Calibration.GetDividend(underlying, T);
            double date = GetData.GetTime(underlying);
            double timeToMaturity = (T - date) / 365;
            double price = Calibration.InterpolatePrice(K, T, underlying);
            double vol = BlackScholes.ImpliedVol(optionFlag, S, price, r, K, timeToMaturity, q);
            double P = BlackScholes.BSOptionPrice(optionFlag, S, vol, r, K, timeToMaturity, q);
            return P;
        }

        public static double Delta(string optionFlag, double K, double T, string underlying, double? q = null)
        {
            double S = GetData.GetSpot(underlying);
            double r = Calibration.GetRepo(underlying, T);
            q = q  ?? Calibration.GetDividend(underlying, T);
            double date = GetData.GetTime(underlying);
            double timeToMaturity = (T - date) / 365;
            double price = Calibration.InterpolatePrice(K, T, underlying);
            double vol = Math.Abs(BlackScholes.ImpliedVol(optionFlag, S, price, r, K, timeToMaturity, q));
            double sensitivity = BlackScholes.DeltaBS(optionFlag, S, vol, r, K, timeToMaturity, q);
            return sensitivity;
        }
        public static double Gamma(string optionFlag, double K, double T, string underlying, double?q = null)
        {
            double S = GetData.GetSpot(underlying);
            double r = Calibration.GetRepo(underlying, T);
            q = q ?? Calibration.GetDividend(underlying, T);
            double date = GetData.GetTime(underlying);
            double timeToMaturity = (T - date) / 365;
            double price = Calibration.InterpolatePrice(K, T, underlying);
            double vol = Math.Abs(BlackScholes.ImpliedVol(optionFlag, S, price, r, K, timeToMaturity, q));
            double sensitivity = BlackScholes.GammaBS(optionFlag, S, vol, r, K, timeToMaturity, q);
            return sensitivity;
        }
        public static double Theta(string optionFlag, double K, double T, string underlying, double? q = null)
        {
            double S = GetData.GetSpot(underlying);
            double r = Calibration.GetRepo(underlying, T);
            q = q ?? Calibration.GetDividend(underlying, T);
            double date = GetData.GetTime(underlying);
            double timeToMaturity = (T - date) / 365;
            double price = Calibration.InterpolatePrice(K, T, underlying);
            double vol = Math.Abs(BlackScholes.ImpliedVol(optionFlag, S, price, r, K, timeToMaturity, q));
            double sensitivity = BlackScholes.ThetaBS(optionFlag, S, vol, r, K, timeToMaturity, q);
            return sensitivity;
        }

        public static double Vega(string optionFlag, double K, double T, string underlying, double? q = null)
        {
            double S = GetData.GetSpot(underlying);
            double r = Calibration.GetRepo(underlying, T);
            q = q ?? Calibration.GetDividend(underlying, T);
            double date = GetData.GetTime(underlying);
            double timeToMaturity = (T - date) / 365;
            double price = Calibration.InterpolatePrice(K, T, underlying);
            double vol = Math.Abs(BlackScholes.ImpliedVol(optionFlag, S, price, r, K, timeToMaturity, q));
            double sensitivity = BlackScholes.VegaBS(optionFlag, S, vol, r, K, timeToMaturity, q);
            return sensitivity;
        }

        public static double Rho(string optionFlag, double K, double T, string underlying, double? q= null)
        {
            double S = GetData.GetSpot(underlying);
            double r = Calibration.GetRepo(underlying, T);
            q = q ?? Calibration.GetDividend(underlying, T);
            double date = GetData.GetTime(underlying);
            double timeToMaturity = (T - date) / 365;
            double price = Calibration.InterpolatePrice(K, T, underlying);
            double vol = Math.Abs(BlackScholes.ImpliedVol(optionFlag, S, price, r, K, timeToMaturity, q));
            double sensitivity = BlackScholes.RhoBS(optionFlag, S, vol, r, K, timeToMaturity, q);
            return sensitivity;
        }

        public static double[,] SensiOption(string optionFlag, double K, double T, string underlying, double? q = null)
        {
            double[,] sensitivities = new double[5, 1]; // Tableau 2D pour simuler une colonne

            sensitivities[0, 0] = Delta(optionFlag, K, T, underlying, q);
            sensitivities[1, 0] = Gamma(optionFlag, K, T, underlying, q);
            sensitivities[2, 0] = Theta(optionFlag, K, T, underlying, q);
            sensitivities[3, 0] = Vega(optionFlag, K, T, underlying, q);
            sensitivities[4, 0] = Rho(optionFlag, K, T, underlying, q);



            return sensitivities;
        }



        ////// option portfolio
        public static double PortfolioOptionPrice(List<OptionParameters> optionParameters)
        {
            double totalPrice = 0.0;

            foreach (var parameters in optionParameters)
            {
                double S = GetData.GetSpot(parameters.Underlying);
                double r = Calibration.GetRepo(parameters.Underlying, parameters.Expiry);
                double date = GetData.GetTime(parameters.Underlying);
                double timeToMaturity = (parameters.Expiry - date) / 365;
                double price = Calibration.InterpolatePrice(parameters.Strike, parameters.Expiry, parameters.Underlying);
                double vol = BlackScholes.ImpliedVol(parameters.OptionType, S, price, r, parameters.Strike, timeToMaturity);
                double optionPrice = BlackScholes.BSOptionPrice(parameters.OptionType, S, vol, r, parameters.Strike, timeToMaturity);

                totalPrice += optionPrice;
            }

            return totalPrice;
        }

        public class OptionParameters
        {
            public string Underlying;
            public double Strike;
            public double Expiry;
            public string OptionType; // "c" pour call, "p" pour put, par exemple

        }


    }
}
