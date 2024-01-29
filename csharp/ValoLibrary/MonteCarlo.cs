using System;
using System.Collections.Generic;
using System.Linq;

namespace ValoLibrary
{
    internal class MonteCarlo
    {
        public static double Price(char callPutFlag, double S0, double sigma, double r, double K, double T)
        {
            int numberSimulation = 100000;
            double payOff = 0;
            double S = 0;
            List<double> samples = new List<double>();
            if (callPutFlag == 'c')
            {
                for (int i = 0; i < numberSimulation; i++)
                {
                    S = S0 * Math.Exp((r - sigma * sigma / 2) * T + sigma * Math.Sqrt(T) * StatisticFormulas.RandomNormal(0, 1));
                    payOff = Math.Max(S - K, 0);
                    samples.Add(payOff);
                }
            }

            else
            {
                for (int i = 0; i < numberSimulation; i++)
                {
                    S = S0 * Math.Exp((r - sigma * sigma / 2) * T + sigma * Math.Sqrt(T) * StatisticFormulas.RandomNormal(0, 1));
                    payOff = Math.Max(K - S, 0);
                    samples.Add(payOff);
                }
            }
            return samples.Average() * Math.Exp(-r * T);
        }
    }
}
