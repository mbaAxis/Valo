using MathNet.Numerics.RootFinding;
using MathNet.Numerics;
using System;
using System.Diagnostics;

namespace ValoLibrary
{
    public class BlackScholes
    {

        public static double BSOptionPrice(string optionFlag, double S, double sigma, double r, double K, double T, double? q = null)
        {
            double price = 0;
            double d1 = (Math.Log(S / K) + (r + (Math.Pow(sigma, 2) / 2)) * T) / (sigma * Math.Sqrt(T));
            double d2 = d1 - sigma * Math.Sqrt(T);

            double expQT = q.HasValue ? Math.Exp((double)(-q * T)) : 1.0;
            double expRT = Math.Exp(-r * T);

            if (optionFlag.ToLower() == "call")
            {
                price = expQT * S * StatisticFormulas.Cfd(d1) - K * expRT * StatisticFormulas.Cfd(d2);
            }
            else if (optionFlag.ToLower() == "put")
            {
                price = -expQT * S * StatisticFormulas.Cfd(-d1) + K * expRT * StatisticFormulas.Cfd(-d2);
            }

            return price;
        }
        public static double DeltaBS(string optionFlag, double S, double sigma, double r, double K, double T, double? q=null)
        {

            double d1 = (Math.Log(S / K) + (r + (Math.Pow(sigma, 2) / 2)) * T) / (sigma * Math.Sqrt(T));
            double expQT = q.HasValue ? Math.Exp((double)(-q * T)) : 1.0;


            if (optionFlag.ToLower() == "call")
            {
                return expQT * StatisticFormulas.Cfd(d1);
            }
            else if (optionFlag.ToLower() == "put")
            {
                return expQT * (StatisticFormulas.Cfd(d1) - 1);
            }

            else
            {
                Console.WriteLine("ERROR : check call/put flag !");
                return 0;
            }
        }

        public static double GammaBS(string optionFlag, double S, double sigma, double r, double K, double T, double? q = null)
        {
            double d1 = (Math.Log(S / K) + (r + (Math.Pow(sigma, 2) / 2)) * T) / (sigma * Math.Sqrt(T));
            double expQT = q.HasValue ? Math.Exp((double)(-q * T)) : 1.0;


            if (optionFlag.ToLower() == "call" || optionFlag.ToLower() == "put")
            {
                return expQT * StatisticFormulas.NormalDensity(d1, 0, 1) / (S * sigma * Math.Sqrt(T));
            }
            else
            {
                Console.WriteLine("ERROR : check call/put flag !");
                return 0;
            }
        }

        public static double ThetaBS(string optionFlag, double S, double sigma, double r, double K, double T, double? q = null)
        {
            double d1 = (Math.Log(S / K) + (r + (Math.Pow(sigma, 2) / 2)) * T) / (sigma * Math.Sqrt(T));
            double d2 = d1 - sigma * Math.Sqrt(T);

            double expQT = q.HasValue ? Math.Exp((double)(-q * T)) : 1.0;
            double expRT = Math.Exp(-r * T);

            if (optionFlag.ToLower() == "call")
            {
                return (expQT * S * StatisticFormulas.NormalDensity(d1, 0, 1) * sigma) / (2* Math.Sqrt(T))
                   - (r * K * StatisticFormulas.Cfd(d2) * expRT) + (q.GetValueOrDefault() * expQT * S * StatisticFormulas.Cfd(d2));
            }
            else if (optionFlag.ToLower() == "put")
            {
                return (expQT * S * StatisticFormulas.NormalDensity(d1, 0, 1) * sigma) / (2 * Math.Sqrt(T))
                   + (r * K * StatisticFormulas.Cfd(d2) * expRT) - (q.GetValueOrDefault() * expQT * S * StatisticFormulas.Cfd(d2));
            }
            else
            {
                Console.WriteLine("ERROR : check call/put flag !");
                return 0;
            }

        }

        public static double VegaBS(string optionFlag, double S, double sigma, double r, double K, double T, double? q = null)
        {

            double d1 = (Math.Log(S / K) + (r + (Math.Pow(sigma, 2) / 2)) * T) / (sigma * Math.Sqrt(T));
            double expQT = q.HasValue ? Math.Exp((double)(-q * T)) : 1.0;

            if (optionFlag.ToLower() == "call" || optionFlag.ToLower() == "put")
            {
                return expQT * StatisticFormulas.NormalDensity(d1, 0, 1) * S * Math.Sqrt(T);
            }

            else
            {
                Console.WriteLine("ERROR : check call/put flag !");
                return 0;
            }
        }

        public static double RhoBS(string optionFlag, double S, double sigma, double r, double K, double T, double? q = null)
        {

            double d1 = (Math.Log(S / K) + (r + (Math.Pow(sigma, 2) / 2)) * T) / (sigma * Math.Sqrt(T));
            double d2 = d1 - sigma * Math.Sqrt(T);

            if (optionFlag.ToLower() == "call") 
            {
                return K*T*Math.Exp(-r*T)*StatisticFormulas.Cfd(d2);
            }

            else if (optionFlag.ToLower() == "put")
            {
                return -K * T * Math.Exp(-r * T) * StatisticFormulas.Cfd(-d2);
            }

            else
            {
                Console.WriteLine("ERROR : check call/put flag !");
                return 0;
            }
        }

        public static double[,] SensiOptionBS(string optionFlag, double S, double sigma, double r, double K, double T, double? q=null)
        {
            double[,] sensitivities = new double[5, 1]; // Tableau 2D pour simuler une colonne

            sensitivities[0, 0] = BlackScholes.DeltaBS(optionFlag, S, sigma, r, K, T, q);
            sensitivities[1, 0] = BlackScholes.GammaBS(optionFlag, S, sigma, r, K, T, q);            
            sensitivities[2, 0] = BlackScholes.ThetaBS(optionFlag, S, sigma, r, K, T, q);
            sensitivities[3, 0] = BlackScholes.VegaBS(optionFlag, S, sigma, r, K, T, q);
            sensitivities[4, 0] = BlackScholes.RhoBS(optionFlag, S, sigma, r, K, T, q);

            return sensitivities;
        }

        public static double ImpliedVol(string optionFlag, double S, double price, double r, double K, double T, double? q=null)
        {
            double tolerance = 1e-6;
            int convergence = 100;
            double sigma;
            sigma = Math.Sqrt(2 * Math.PI / T) * price / S;


            for (int i = 0; i < convergence; i++)
            {
                double BS_price = BSOptionPrice(optionFlag.ToLower(), S, sigma, r, K, T, q);

                double vega = VegaBS(optionFlag.ToLower(),S, sigma, r,  K, T, q);                  

                if (Math.Abs(BS_price - price)> tolerance)
                {
                    return sigma;
                }

                sigma -= (BS_price - price) / vega;

                // Clamp sigma to avoid negative or extreme values
                sigma = Math.Max(sigma, 0.001);
                sigma = Math.Min(sigma, 5.0);
            }

            // If maxIterations reached without convergence, return NaN or throw an exception
            return double.NaN;
        }






    }
}
