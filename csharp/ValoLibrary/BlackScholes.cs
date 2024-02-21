using MathNet.Numerics.RootFinding;
using MathNet.Numerics;
using System;
using System.Diagnostics;

namespace ValoLibrary
{
    public class BlackScholes
    {
        public static double Price(string callPutFlag, double S, double sigma, double r, double K, double T)
        {
            double price = 0;
            double d1, d2;
            d1 = (1 / (sigma * Math.Sqrt(T))) * Math.Log(S / K) + (r + Math.Pow(sigma, 2)) * T;
            d2 = d1 - sigma * Math.Sqrt(T);

            if (callPutFlag == "c")
            {
                price = S * StatisticFormulas.Cfd(d1) - K * Math.Exp(-r * T) * StatisticFormulas.Cfd(d2);
            }

            else if (callPutFlag == "p")
            {
                price = -S * StatisticFormulas.Cfd(-d1) + K * Math.Exp(-r * T) * StatisticFormulas.Cfd(-d2);
            }

            return price;
        }
        public static double Delta(string callPutFlag, double S, double sigma, double r, double K, double T)
        {

            double d1 = (1 / (sigma * Math.Sqrt(T))) * Math.Log(S / K) + (r + Math.Pow(sigma, 2)) * T;

            if (callPutFlag == "c")
            {
                return StatisticFormulas.Cfd(d1);
            }
            else if (callPutFlag == "p")
            {
                return StatisticFormulas.Cfd(d1) - 1;
            }

            else
            {
                Console.WriteLine("ERROR : check call/put flag !");
                return 0;
            }
        }

        public static double Vega(string callPutFlag, double S, double sigma, double r, double K, double T)
        {

            double d1 = (1 / (sigma * Math.Sqrt(T))) * Math.Log(S / K) + (r + Math.Pow(sigma, 2)) * T;
            if (callPutFlag == "c" || callPutFlag == "p")
            {
                return StatisticFormulas.NormalDensity(d1, 0, 1) * S * Math.Sqrt(T);
            }

            else
            {
                Console.WriteLine("ERROR : check call/put flag !");
                return 0;
            }
        }

        public static double Gamma(string callPutFlag, double S, double sigma, double r, double K, double T)
        {
            double d1 = (1 / (sigma * Math.Sqrt(T))) * Math.Log(S / K) + (r + Math.Pow(sigma, 2)) * T;
            if (callPutFlag == "c" || callPutFlag == "p")
            {
                return StatisticFormulas.NormalDensity(d1, 0, 1) / (S * sigma * Math.Sqrt(T));
            }
            else
            {
                Console.WriteLine("ERROR : check call/put flag !");
                return 0;
            }
        }

        public static double Tetha(string callPutFlag, double S, double sigma, double r, double K, double T)
        {
            double d1 = (1 / (sigma * Math.Sqrt(T))) * Math.Log(S / K) + (r + Math.Pow(sigma, 2)) * T;
            double d2 = d1 - sigma * Math.Sqrt(T);
            if (callPutFlag == "c")
            {
                return -0.5 * S * StatisticFormulas.NormalDensity(d1, 0, 1) * sigma / Math.Sqrt(T)
                    - r * K * StatisticFormulas.Cfd(d2) * Math.Exp(-r * T);
            }
            else if (callPutFlag == "p")
            {
                return -0.5 * S * StatisticFormulas.NormalDensity(d1, 0, 1) * sigma / Math.Sqrt(T)
                    + r * K * StatisticFormulas.Cfd(-d2) * Math.Exp(-r * T);
            }
            else
            {
                Console.WriteLine("ERROR : check call/put flag !");
                return 0;
            }

        }

        public static double ImpliedVol(string callPutFlag, double S, double price, 
            double r, double K, double T)
        {
            double tolerance = 1e-6;
            int convergence = 100;
            double sigma;

            for (int i = 0; i < convergence; i++)
            {
                sigma = Math.Sqrt(2 * Math.PI / T) * price / S;
                double BS_price = Price(callPutFlag, S, sigma, r, K, T);

                double vega = Vega(callPutFlag,S, sigma, r,  K, T);                  

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
