using System;
using System.Collections.Generic;
using System.Linq;

namespace ValoLibrary
{
    public class MonteCarlo
    {
        //public static double MCEurOptionPrice(string optionFlag, double S0, double sigma, double r, double K, double T, double?q = null)
        //{
        //    int numberSimulation = 100000;
        //    double payOff;
        //    double S;
        //    List<double> samples = new List<double>();
            

        //    if (optionFlag.ToLower() == "call")
        //    {
        //        for (int i = 0; i < numberSimulation; i++)
        //        {
        //            S = S0 * Math.Exp((r - q.GetValueOrDefault() - sigma * sigma / 2) * T + sigma * Math.Sqrt(T) * StatisticFormulas.RandomNormal(0, 1));
        //            payOff = Math.Max(S - K, 0);
        //            samples.Add(payOff);
        //        }
        //    }

        //    else
        //    {
        //        for (int i = 0; i < numberSimulation; i++)
        //        {
        //            S = S0 * Math.Exp((r - q.GetValueOrDefault() -  sigma * sigma / 2) * T + sigma * Math.Sqrt(T) * StatisticFormulas.RandomNormal(0, 1));
        //            payOff = Math.Max(K - S, 0);
        //            samples.Add(payOff);
        //        }
        //    }
        //    return samples.Average() * Math.Exp(-r  * T);
            

        //}

        public static double MCEurOptionPrice(string optionFlag, double s, double sigma, double r, double K, double T, double? q = null)
        {
            int numberSimulation = 100000;
            double payOff;
            double S;
            List<double> samples = new List<double>();
            Random random = new Random();

            if (optionFlag.ToLower() == "call")
            {
                for (int i = 0; i < numberSimulation; i++)
                {
                    S = s * Math.Exp((r - q.GetValueOrDefault() - sigma * sigma / 2) * T + sigma * Math.Sqrt(T) * random.NextGaussian());
                    payOff = Math.Max(S - K, 0);
                    samples.Add(payOff);
                }
            }
            else
            {
                for (int i = 0; i < numberSimulation; i++)
                {
                    S = s * Math.Exp((r - q.GetValueOrDefault() - sigma * sigma / 2) * T + sigma * Math.Sqrt(T) * random.NextGaussian());
                    payOff = Math.Max(K - S, 0);
                    samples.Add(payOff);
                }
            }

            return samples.Average() * Math.Exp(-r * T);
        }


        ////////////option portfolio price/////////////////////
        public static double MCEurOptionPortfolioPrice(int numberOfOptions, BSParameters[] options)
        {
            if (options.Length != numberOfOptions)
            {
                throw new ArgumentException("Le nombre d'options ne correspond pas à la valeur spécifiée.");
            }

            double portfolioPrice = 0;

            foreach (BSParameters option in options)
            {
                double optionPrice = MCEurOptionPrice(option.optionFlag, option.s, option.sigma, option.r,
                    option.k, option.T, option.q);

                portfolioPrice += optionPrice;
            }

            return portfolioPrice;
        }




    }
}
