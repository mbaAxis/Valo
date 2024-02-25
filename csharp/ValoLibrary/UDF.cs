using System;
using System.Collections.Generic;
using System.Data;
using System.Runtime.InteropServices;

namespace ValoLibrary
{
    [ComVisible(true)]
    [Guid("839187c8-9765-4e76-a508-61ec3dd1a504")]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface IUDF
    {
        // using user data
        double GetBSOptionPrice(string optionFlag, double s, double sigma, double r, double k, double T, double? q=null);
        double[,] GetSensiOptionBS(string optionFlag, double s, double sigma, double r, double k, double T, double? q=null);
        double GetBSOptionPortfolioPrice(int numberOfOptions, BSParameters[] options);


        // using market data
        double GetOptionPrice(string optionFlag, double k, double T, string underlying, double? q = null);        
        double[,] GetSensiOption(string optionFlag, double k, double T, string underlying, double? q = null);
        double GetOptionPortfolioPrice(int numberOfOptions, Parameters[] options);

        // using Monte Carlo
        double GetMCEurOptionPrice(string optionFlag, double s, double sigma, double r, double k, double T, double? q = null);
        double GetMCEurOptionPortfolioPrice(int numberOfOptions, BSParameters[] options);






    }

    [ComVisible(true)]
    [ProgId("ValoLibrary.UDF")]
    [Guid("14041acd-3ec6-4ba1-922b-87858755d0e7")]
    [ClassInterface(ClassInterfaceType.None)]
    public class UDF : IUDF
    {
        // using user data
        public double GetBSOptionPrice(string optionFlag, double s, double sigma, double r, double k, double T, double? q = null)
        {
            return BlackScholes.BSOptionPrice(optionFlag, s, sigma, r, k, T, q);
        }

        public double GetBSOptionPortfolioPrice(int numberOfOptions, BSParameters[] options)
        {
            return BlackScholes.BSOptionPortfolioPrice(numberOfOptions, options);
        }

        public double[,] GetSensiOptionBS(string optionFlag, double s, double sigma, double r, double k, double T, double? q = null)
        {
            return BlackScholes.SensiOptionBS(optionFlag, s, sigma, r, k, T, q);
        }

        //using market data

        public double GetOptionPrice(string optionFlag, double k, double T, string underlying, double? q = null)
        {
            return BlackScholesMD.OptionPrice(optionFlag, k, T, underlying, q); ;
        }

        public double[,] GetSensiOption(string optionFlag, double k, double T, string underlying, double? q = null)
        {
            return BlackScholesMD.SensiOption(optionFlag, k, T, underlying, q);
        }
        public double GetOptionPortfolioPrice(int numberOfOptions, Parameters[] options)
        {
            return BlackScholesMD.OptionPortfolioPrice(numberOfOptions, options);
        }

        //Using Monte Carlo

        public double GetMCEurOptionPrice(string optionFlag, double s, double sigma, double r, double k, double T, double? q = null)
        {
            return MonteCarlo.MCEurOptionPrice(optionFlag, s, sigma, r, k, T, q);
        }

        public double GetMCEurOptionPortfolioPrice(int numberOfOptions, BSParameters[] options)
        {
            return MonteCarlo.MCEurOptionPortfolioPrice(numberOfOptions, options);
        }

    }
  
}
