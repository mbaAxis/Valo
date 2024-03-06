using MathNet.Numerics.RootFinding;
using MathNet.Numerics;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Collections.Specialized;

namespace ValoLibrary
{
    [ComVisible(true)]
    [ProgId("ValoLibrary.BSParameters")]
    [Guid("14041acd-3ec6-4ba1-922b-87858755d6e7")]
    [ClassInterface(ClassInterfaceType.None)]
    public class BSParameters
    {
        public string optionFlag;
        public string position;
        public double s;
        public double sigma;
        public double r;
        public double k;
        public double T;
        public double? q;
    }
    public class BlackScholes
    {

        public static double BSOptionPrice(string optionFlag, string position, double s, double sigma, double r, double k, double T, double? q = null)
        {
            double price = 0;
            double d1 = (Math.Log(s / k) + (r + (Math.Pow(sigma, 2) / 2)) * T) / (sigma * Math.Sqrt(T));
            double d2 = d1 - sigma * Math.Sqrt(T);

            double expQT = q.HasValue ? Math.Exp((double)(-q * T)) : 1.0;
            double expRT = Math.Exp(-r * T);

            if (optionFlag.ToLower() == "call")
            {
                if (position.ToLower() == "long")
                {
                    price = expQT * s * StatisticFormulas.Cfd(d1) - k * expRT * StatisticFormulas.Cfd(d2);
                }
                else if (position.ToLower() == "short")
                {
                    price = -(expQT * s * StatisticFormulas.Cfd(d1) - k * expRT * StatisticFormulas.Cfd(d2));
                }
            }
            else if (optionFlag.ToLower() == "put")
            {
                if (position.ToLower() == "long")
                {
                    price = -expQT * s * StatisticFormulas.Cfd(-d1) + k * expRT * StatisticFormulas.Cfd(-d2);

                }
                else if (position.ToLower() == "short")
                {
                    price = -(-expQT * s * StatisticFormulas.Cfd(-d1) + k * expRT * StatisticFormulas.Cfd(-d2));

                }
            }

            return price;
        }
        public static double DeltaBS(string optionFlag, string position, double s, double sigma, double r, double k, double T, double? q = null)
        {

            double delta = 0.0;
            double d1 = (Math.Log(s / k) + (r + (Math.Pow(sigma, 2) / 2)) * T) / (sigma * Math.Sqrt(T));
            double expQT = q.HasValue ? Math.Exp((double)(-q * T)) : 1.0;


            if (optionFlag.ToLower() == "call")
            {
                if (position.ToLower() == "long")
                {
                    delta = expQT * StatisticFormulas.Cfd(d1);
                }
                else if (position.ToLower() == "short")
                {
                    delta = -(expQT * StatisticFormulas.Cfd(d1));
                }
            }

            else if (optionFlag.ToLower() == "put")
            {
                if (position.ToLower() == "long")
                {
                    delta = expQT * (StatisticFormulas.Cfd(d1) - 1);

                }
                else if (position.ToLower() == "short")
                {
                    delta = -(expQT * (StatisticFormulas.Cfd(d1) - 1));

                }
            }

            else
            {
                Console.WriteLine("ERROR : check call/put flag !");
                return 0;
            }

            return delta;
        }

        public static double GammaBS(string optionFlag, string position, double s, double sigma, double r, double k, double T, double? q = null)
        {
            double gamma = 0.0;
            double d1 = (Math.Log(s / k) + (r + (Math.Pow(sigma, 2) / 2)) * T) / (sigma * Math.Sqrt(T));
            double expQT = q.HasValue ? Math.Exp((double)(-q * T)) : 1.0;


            if (optionFlag.ToLower() == "call" || optionFlag.ToLower() == "put")
            {
                if (position.ToLower() == "long")
                {
                    gamma = expQT * StatisticFormulas.NormalDensity(d1, 0, 1) / (s * sigma * Math.Sqrt(T));
                }
                else if (position.ToLower() == "short")
                {
                    gamma = -(expQT * StatisticFormulas.NormalDensity(d1, 0, 1) / (s * sigma * Math.Sqrt(T)));

                }

            }

            else
            {
                Console.WriteLine("ERROR : check call/put flag !");
                return 0;
            }

            return gamma;
        }

        public static double ThetaBS(string optionFlag, string position, double s, double sigma, double r, double k, double T, double? q = null)
        {
            double theta = 0.0;
            double d1 = (Math.Log(s / k) + (r + (Math.Pow(sigma, 2) / 2)) * T) / (sigma * Math.Sqrt(T));
            double d2 = d1 - sigma * Math.Sqrt(T);

            double expQT = q.HasValue ? Math.Exp((double)(-q * T)) : 1.0;
            double expRT = Math.Exp(-r * T);

            if (optionFlag.ToLower() == "call")
            {
                if (position.ToLower() == "long")
                {
                    theta = (expQT * s * StatisticFormulas.NormalDensity(d1, 0, 1) * sigma) / (2 * Math.Sqrt(T))
                  - (r * k * StatisticFormulas.Cfd(d2) * expRT) + (q.GetValueOrDefault() * expQT * s * StatisticFormulas.Cfd(d2));
                }
                else if (position.ToLower() == "short")
                {
                    theta = -((expQT * s * StatisticFormulas.NormalDensity(d1, 0, 1) * sigma) / (2 * Math.Sqrt(T))
               - (r * k * StatisticFormulas.Cfd(d2) * expRT) + (q.GetValueOrDefault() * expQT * s * StatisticFormulas.Cfd(d2)));

                }

            }
            else if (optionFlag.ToLower() == "put")
            {
                if (position.ToLower() == "long")
                {
                    theta = (expQT * s * StatisticFormulas.NormalDensity(d1, 0, 1) * sigma) / (2 * Math.Sqrt(T))
                   + (r * k * StatisticFormulas.Cfd(d2) * expRT) - (q.GetValueOrDefault() * expQT * s * StatisticFormulas.Cfd(d2));
                }
                else if (position.ToLower() == "short")
                {
                    theta = -((expQT * s * StatisticFormulas.NormalDensity(d1, 0, 1) * sigma) / (2 * Math.Sqrt(T))
                   + (r * k * StatisticFormulas.Cfd(d2) * expRT) - (q.GetValueOrDefault() * expQT * s * StatisticFormulas.Cfd(d2)));
                }
            }

            else
            {
                Console.WriteLine("ERROR : check call/put flag !");
                return 0;
            }

            return theta;
        }

        public static double VegaBS(string optionFlag, string position, double s, double sigma, double r, double k, double T, double? q = null)
        {
            double vega = 0.0;
            double d1 = (Math.Log(s / k) + (r + (Math.Pow(sigma, 2) / 2)) * T) / (sigma * Math.Sqrt(T));
            double expQT = q.HasValue ? Math.Exp((double)(-q * T)) : 1.0;

            if (optionFlag.ToLower() == "call" || optionFlag.ToLower() == "put")
            {
                if (position.ToLower() == "long")
                {
                    vega = expQT * StatisticFormulas.NormalDensity(d1, 0, 1) * s * Math.Sqrt(T);
                }
                else if (position.ToLower() == "short")
                {
                    vega = -(expQT * StatisticFormulas.NormalDensity(d1, 0, 1) * s * Math.Sqrt(T));
                }
            }

            else
            {
                Console.WriteLine("ERROR : check call/put flag !");
                return 0;
            }

            return vega;
        }

        public static double RhoBS(string optionFlag, string position, double s, double sigma, double r, double k, double T, double? q = null)
        {
            double rho = 0.0;
            double d1 = (Math.Log(s / k) + (r + (Math.Pow(sigma, 2) / 2)) * T) / (sigma * Math.Sqrt(T));
            double d2 = d1 - sigma * Math.Sqrt(T);

            if (optionFlag.ToLower() == "call")
            {
                if (position.ToLower() == "lonng")
                {
                    rho = k * T * Math.Exp(-r * T) * StatisticFormulas.Cfd(d2);
                }
                else if (position.ToLower() == "short")
                {
                    rho = -(k * T * Math.Exp(-r * T) * StatisticFormulas.Cfd(d2));
                }

            }

            else if (optionFlag.ToLower() == "put")
            {

                if (position.ToLower() == "lonng")
                {
                    rho = -k * T * Math.Exp(-r * T) * StatisticFormulas.Cfd(-d2); ;
                }
                else if (position.ToLower() == "short")
                {
                    rho = -(-k * T * Math.Exp(-r * T) * StatisticFormulas.Cfd(-d2));
                }

            }

            else
            {
                Console.WriteLine("ERROR : check call/put flag !");
                return 0;
            }

            return rho;
        }

        public static double[,] SensiOptionBS(string optionFlag, string position, double s, double sigma, double r, double k, double T, double? q = null)
        {
            double[,] sensitivities = new double[5, 1]; // Tableau 2D pour simuler une colonne

            sensitivities[0, 0] = BlackScholes.DeltaBS(optionFlag, position, s, sigma, r, k, T, q);
            sensitivities[1, 0] = BlackScholes.GammaBS(optionFlag, position, s, sigma, r, k, T, q);
            sensitivities[2, 0] = BlackScholes.ThetaBS(optionFlag, position, s, sigma, r, k, T, q);
            sensitivities[3, 0] = BlackScholes.VegaBS(optionFlag, position, s, sigma, r, k, T, q);
            sensitivities[4, 0] = BlackScholes.RhoBS(optionFlag, position, s, sigma, r, k, T, q);

            return sensitivities;
        }

        public static double ImpliedVol(string optionFlag, string position, double s, double price, double r, double k, double T, double? q = null)
        {
            double tolerance = 1e-6;
            int convergence = 100;
            double sigma;
            sigma = Math.Sqrt(2 * Math.PI / T) * price / s;


            for (int i = 0; i < convergence; i++)
            {
                double BS_price = BSOptionPrice(optionFlag, position, s, sigma, r, k, T, q);

                double vega = VegaBS(optionFlag, position, s, sigma, r, k, T, q);

                if (Math.Abs(BS_price - price) > tolerance)
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

        ////////////option portfolio price/////////////////////
        public static double BSOptionPortfolioPrice(int numberOfOptions, BSParameters[] options)
        {
            if (options.Length != numberOfOptions)
            {
                throw new ArgumentException("Le nombre d'options ne correspond pas à la valeur spécifiée.");
            }

            double portfolioPrice = 0;

            foreach (BSParameters option in options)
            {
                double optionPrice = BSOptionPrice(option.optionFlag, option.position, option.s, option.sigma, option.r,
                    option.k, option.T, option.q);

                portfolioPrice += optionPrice;
            }

            return portfolioPrice;
        }
    }
}

