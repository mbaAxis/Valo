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

        public static double ImpliedVol(string callPutFlag, double S, double price, double r, double K, double T)
        {
            const double tolerance = 1e-6;
            const int convergence = 100;
            int i = 0;

            double vol = Math.Sqrt(2 * Math.PI / T) * price / S;
            double BS_price = Price(callPutFlag, S, vol, r, K, T);
            while (Math.Abs(price - BS_price) > tolerance)
            {
                if (i < convergence)
                {
                    i = i + 1;
                    double vega = Vega(callPutFlag, S, vol, r, K, T);
                    vol = vol - (BS_price - price) / vega;
                }
                break;
            }

            return Math.Sqrt(2 * Math.PI / T) * price / S;


        }
    }
}
