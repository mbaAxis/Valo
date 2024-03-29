using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ValoLibrary
{
    public class MonteCarlo
    {

        public static double MCEurOptionPrice(double quantity, string optionType, string position, double s, double sigma, double r, double k, double T, double? q = null)
        {

            // Vérifier si K est négatif
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

                int numberSimulation = 10000000;
                double payOff;
                double spotPrice;
                List<double> samples = new List<double>();
                Random random = new Random();
                double multiplier = (position.ToLower() == "long") ? 1 : -1;




                for (int i = 0; i < numberSimulation; i++)
                {
                    spotPrice = s * Math.Exp((r - q.GetValueOrDefault() - sigma * sigma / 2) * T + sigma * Math.Sqrt(T) * random.NextGaussian());

                    if (optionType.ToLower() == "call")
                    {
                        payOff = multiplier * Math.Max(spotPrice - k, 0);
                    }
                    else if (optionType.ToLower() == "put")
                    {
                        payOff = multiplier * Math.Max(k - spotPrice, 0);
                    }
                    else
                    {
                        throw new ArgumentException("Invalid option type. Supported types: 'call' or 'put'");
                    }

                    samples.Add(payOff);
                }

                return quantity * samples.Average() * Math.Exp(-r * T);
            }


            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return 0.0; // You can choose an appropriate value to return in case of an error
            }
        }

        ////////////option portfolio price/////////////////////
        public static double MCEurOptionPortfolioPrice(BSParameters[] options)
        {
            try
            {
                if (options == null || options.Length < 1)
                {
                    throw new ArgumentException("The option portfolio must contain at least one option.");
                }

                double portfolioPrice = 0;

                foreach (BSParameters option in options)
                {
                    double optionPrice = MCEurOptionPrice(option.quantity, option.optionFlag, option.position, option.s, option.sigma, option.r,
                        option.k, option.T, option.q);

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
