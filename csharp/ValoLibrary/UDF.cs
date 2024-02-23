using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ValoLibrary
{
    [ComVisible(true)]
    [Guid("839187c8-9765-4e76-a508-61ec3dd1a504")]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface IUDF
    {
        // using user data
        double GetBSOptionPrice(string optionFlag, double S, double sigma, double r, double K, double T, double? q=null);
        double[,] GetSensiOptionBS(string optionFlag, double S, double sigma, double r, double K, double T, double? q=null);

        // using market data
        double GetOptionPrice(string optionFlag, double K, double T, string underlying, double? q = null);        
        double[,] GetSensiOption(string optionFlag, double K, double T, string underlying, double? q = null);

        // using Monte Carlo
        double GetMCEurOptionPrice(string optionFlag, double S0, double sigma, double r, double K, double T, double? q = null);

    }

    [ComVisible(true)]
    [ProgId("ValoLibrary.UDF")]
    [Guid("14041acd-3ec6-4ba1-922b-87858755d0e7")]
    [ClassInterface(ClassInterfaceType.None)]
    public class UDF : IUDF
    {
        // using user data
        public double GetBSOptionPrice(string optionFlag, double S, double sigma, double r, double K, double T, double? q=null)
        {
            return BlackScholes.BSOptionPrice(optionFlag, S, sigma, r, K, T, q);
        }

        public double[,] GetSensiOptionBS(string optionFlag, double S, double sigma, double r, double K, double T, double? q= null)
        {
            return BlackScholes.SensiOptionBS(optionFlag, S, sigma, r, K, T, q);
        }

        //using market data

        public double GetOptionPrice(string optionFlag, double K, double T, string underlying, double? q = null)
        {
            return BlackScholesMD.OptionPrice(optionFlag, K, T, underlying,q); ;
        }

        public double[,] GetSensiOption(string optionFlag, double K, double T, string underlying, double? q = null)
        {
            return BlackScholesMD.SensiOption(optionFlag, K, T, underlying, q);
        }

        //Using Monte Carlo

        public double GetMCEurOptionPrice(string optionFlag, double S0, double sigma, double r, double K, double T, double? q = null)
        {
            return MonteCarlo.MCEurOptionPrice(optionFlag, S0, sigma, r, K, T, q);
        }






    }
}
