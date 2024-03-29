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
        public double quantity;
        public string optionFlag;
        public string position;
        public double strike;
        public double maturity;
        public string underlying;
        public double? dividend;
    }
    public class BlackScholesMD
    {
        public static double OptionPrice(double quantity, string optionFlag, string position, double k, double T, string underlying, double? q = null)
        {
            try
            {
                if (k < 0 || T < 0)
                {
                    throw new ArgumentException("The strike or maturity cannot be negative.");
                }
                if (T == 0)
                {
                    Console.WriteLine("Exception: You should put a positive Maturity");
                    return 0.0;
                }
                double s = GetData.GetSpot(underlying);
                double r = Calibration.GetRepo(underlying, T);
                q = q ?? Calibration.GetDividend(underlying, T);
                double date = GetData.GetTime(underlying);
                double timeToMaturity = (T - date) / 365;
                double price = Calibration.InterpolatePrice(k, T, underlying);
                double vol = BlackScholes.ImpliedVol(quantity, optionFlag, position, s, price, r, k, timeToMaturity, q);
                double P = BlackScholes.BSOptionPrice(quantity, optionFlag, position, s, vol, r, k, timeToMaturity, q);
                return P;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return 0.0; // You can choose an appropriate value to return in case of an error
            }
        }

        public static double Delta(double quantity, string optionFlag, string position, double k, double T, string underlying, double? q = null)
        {
            try
            {
                if (k < 0 || T < 0)
                {
                    throw new ArgumentException("The strike or maturity cannot be negative.");
                }
                if (T == 0)
                {
                    Console.WriteLine("Exception: You should put a positive Maturity");
                    return 0.0;
                }
                double s = GetData.GetSpot(underlying);
                double r = Calibration.GetRepo(underlying, T);
                q = q ?? Calibration.GetDividend(underlying, T);
                double date = GetData.GetTime(underlying);
                double timeToMaturity = (T - date) / 365;
                double price = Calibration.InterpolatePrice(k, T, underlying);
                double vol = Math.Abs(BlackScholes.ImpliedVol(quantity, optionFlag, position, s, price, r, k, timeToMaturity, q));
                double sensitivity = BlackScholes.DeltaBS(quantity, optionFlag, position, s, vol, r, k, timeToMaturity, q);
                return sensitivity;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return 0.0; // You can choose an appropriate value to return in case of an error
            }
        }
        public static double Gamma(double quantity, string optionFlag, string position, double k, double T, string underlying, double? q = null)
        {
            try
            {
                if (k < 0 || T < 0)
                {
                    throw new ArgumentException("The strike or maturity cannot be negative.");
                }
                if (T == 0)
                {
                    Console.WriteLine("Exception: You should put a positive Maturity");
                    return 0.0;
                }
                double s = GetData.GetSpot(underlying);
                double r = Calibration.GetRepo(underlying, T);
                q = q ?? Calibration.GetDividend(underlying, T);
                double date = GetData.GetTime(underlying);
                double timeToMaturity = (T - date) / 365;
                double price = Calibration.InterpolatePrice(k, T, underlying);
                double vol = Math.Abs(BlackScholes.ImpliedVol(quantity, optionFlag, position, s, price, r, k, timeToMaturity, q));
                double sensitivity = BlackScholes.GammaBS(quantity, optionFlag, position, s, vol, r, k, timeToMaturity, q);
                return sensitivity;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return 0.0; // You can choose an appropriate value to return in case of an error
            }
        }
        public static double Theta(double quantity, string optionFlag, string position, double k, double T, string underlying, double? q = null)
        {
            try
            {
                if (k < 0 || T < 0)
                {
                    throw new ArgumentException("The strike or maturity cannot be negative.");
                }
                if (T == 0)
                {
                    Console.WriteLine("Exception: You should put a positive Maturity");
                    return 0.0;
                }
                double s = GetData.GetSpot(underlying);
                double r = Calibration.GetRepo(underlying, T);
                q = q ?? Calibration.GetDividend(underlying, T);
                double date = GetData.GetTime(underlying);
                double timeToMaturity = (T - date) / 365;
                double price = Calibration.InterpolatePrice(k, T, underlying);
                double vol = Math.Abs(BlackScholes.ImpliedVol(quantity, optionFlag, position, s, price, r, k, timeToMaturity, q));
                double sensitivity = BlackScholes.ThetaBS(quantity, optionFlag, position, s, vol, r, k, timeToMaturity, q);
                return sensitivity;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return 0.0; // You can choose an appropriate value to return in case of an error
            }
        }

        public static double Vega(double quantity, string optionFlag, string position, double k, double T, string underlying, double? q = null)
        {
            try
            {
                if (k < 0 || T < 0)
                {
                    throw new ArgumentException("The strike or maturity cannot be negative.");
                }
                if (T == 0)
                {
                    Console.WriteLine("Exception: You should put a positive Maturity");
                    return 0.0;
                }

                double s = GetData.GetSpot(underlying);
                double r = Calibration.GetRepo(underlying, T);
                q = q ?? Calibration.GetDividend(underlying, T);
                double date = GetData.GetTime(underlying);
                double timeToMaturity = (T - date) / 365;
                double price = Calibration.InterpolatePrice(k, T, underlying);
                double vol = Math.Abs(BlackScholes.ImpliedVol(quantity, optionFlag, position, s, price, r, k, timeToMaturity, q));
                double sensitivity = BlackScholes.VegaBS(quantity, optionFlag, position, s, vol, r, k, timeToMaturity, q);
                return sensitivity;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return 0.0; // You can choose an appropriate value to return in case of an error
            }
        }

        //public static double Rho(double quantity, string optionFlag, string position, double k, double T, string underlying, double? q = null)
        //{
        //    double s = GetData.GetSpot(underlying);
        //    double r = Calibration.GetRepo(underlying, T);
        //    q = q ?? Calibration.GetDividend(underlying, T);
        //    double date = GetData.GetTime(underlying);
        //    double timeToMaturity = (T - date) / 365;
        //    double price = Calibration.InterpolatePrice(k, T, underlying);
        //    double vol = Math.Abs(BlackScholes.ImpliedVol(quantity, optionFlag, position, s, price, r, k, timeToMaturity, q));
        //    double sensitivity = BlackScholes.RhoBS(quantity, optionFlag, position, s, vol, r, k, timeToMaturity, q);
        //    return sensitivity;
        //}

        public static double[,] SensiOption(double quantity, string optionFlag, string position, double k, double T, string underlying, double? q = null)
        {
            double[,] sensitivities = new double[4, 1]; // Tableau 2D pour simuler une colonne

            sensitivities[0, 0] = Delta(quantity, optionFlag, position, k, T, underlying, q);
            sensitivities[1, 0] = Gamma(quantity, optionFlag, position, k, T, underlying, q);
            sensitivities[2, 0] = Theta(quantity, optionFlag, position, k, T, underlying, q);
            sensitivities[3, 0] = Vega(quantity, optionFlag, position, k, T, underlying, q);

            return sensitivities;
        }



        ////// option portfolio
        ///
        public static double OptionPortfolioPrice(Parameters[] options)
        {
            try
            {


                if (options == null || options.Length < 1)
                {
                    throw new ArgumentException("The option portfolio must contain at least one option.");
                }
                double portfolioPrice = 0;

                foreach (Parameters option in options)
                {
                    double optionPrice = OptionPrice(option.quantity, option.optionFlag, option.position, option.strike, option.maturity, option.underlying, option.dividend);

                    portfolioPrice += optionPrice;
                }

                return portfolioPrice;
            }
            catch (Exception ex)
            {

                Console.WriteLine($"An error occurred: {ex.Message}");
                return 0.0; // You can choose an appropriate value to return in case of an error
            }
        }



    }

}
