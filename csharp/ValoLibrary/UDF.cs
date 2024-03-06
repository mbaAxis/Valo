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
        double GetBSOptionPrice(string optionFlag, string position, double s, double sigma, double r, double k, double T, double? q = null);
        double[,] GetSensiOptionBS(string optionFlag, string position, double s, double sigma, double r, double k, double T, double? q = null);
        double GetBSOptionPortfolioPrice(int numberOfOptions, BSParameters[] options);
        double GetDeltaBS(string optionFlag, string position, double s, double sigma, double r, double k, double T, double? q = null);
        double GetGammaBS(string optionFlag, string position, double s, double sigma, double r, double k, double T, double? q = null);
        double GetThetaBS(string optionFlag, string position, double s, double sigma, double r, double k, double T, double? q = null);
        double GetVegaBS(string optionFlag, string position, double s, double sigma, double r, double k, double T, double? q = null);





        ///========================================================================================================================================================
        // using market data
        double GetOptionPrice(string optionFlag, string position, double k, double T, string underlying, double? q = null);
        double[,] GetSensiOption(string optionFlag, string position, double k, double T, string underlying, double? q = null);
        double GetOptionPortfolioPrice(int numberOfOptions, Parameters[] options);
        double GetDelta(string optionFlag, string position, double k, double T, string underlying, double? q = null);
        double GetGamma(string optionFlag, string position, double k, double T, string underlying, double? q = null);
        double GetTheta(string optionFlag, string position, double k, double T, string underlying, double? q = null);
        double GetVega(string optionFlag, string position, double k, double T, string underlying, double? q = null);


        ///========================================================================================================================================================

        // using Monte Carlo
        double GetMCEurOptionPrice(string optionFlag, string position, double s, double sigma, double r, double k, double T, double? q = null);
        double GetMCEurOptionPortfolioPrice(int numberOfOptions, BSParameters[] options);

        ////===============================================================
        double[] GetStripZC(DateTime paramDate, string curveName, double[] curve, string[] curveMaturity, int swapPeriod, int swapBasis, double fxSpot);


        double[] GetStripDefaultProbability(int cdsID, string CDSName, DateTime ParamDate,
            DateTime CDSRollDate, double[] CDSCurve, string[] CurveMaturity,
            string CDSCurrency, double RecoveryRate, bool alterMode, string intensity);

        object GetGetDefaultProb(int issuer, string maturityDate, int scenario = 0, double probMultiplier = 1);




    }

    [ComVisible(true)]
    [ProgId("ValoLibrary.UDF")]
    [Guid("14041acd-3ec6-4ba1-922b-87858755d0e7")]
    [ClassInterface(ClassInterfaceType.None)]
    public class UDF : IUDF
    {
        // using user data
        public double GetBSOptionPrice(string optionFlag, string position, double s, double sigma, double r, double k, double T, double? q = null)
        {
            return BlackScholes.BSOptionPrice(optionFlag, position, s, sigma, r, k, T, q);
        }

        public double GetBSOptionPortfolioPrice(int numberOfOptions, BSParameters[] options)
        {
            return BlackScholes.BSOptionPortfolioPrice(numberOfOptions, options);
        }

        public double[,] GetSensiOptionBS(string optionFlag, string position, double s, double sigma, double r, double k, double T, double? q = null)
        {
            return BlackScholes.SensiOptionBS(optionFlag, position, s, sigma, r, k, T, q);
        }

        public double GetDeltaBS(string optionFlag, string position, double s, double sigma, double r, double k, double T, double? q = null)
        {
            return BlackScholes.DeltaBS(optionFlag, position, s, sigma, r, k, T, q);
        }
        public double GetGammaBS(string optionFlag, string position, double s, double sigma, double r, double k, double T, double? q = null)
        {
            return BlackScholes.GammaBS(optionFlag, position, s, sigma, r, k, T, q);
        }

        public double GetThetaBS(string optionFlag, string position, double s, double sigma, double r, double k, double T, double? q = null)
        {
            return BlackScholes.ThetaBS(optionFlag, position, s, sigma, r, k, T, q);
        }
        public double GetVegaBS(string optionFlag, string position, double s, double sigma, double r, double k, double T, double? q = null)
        {
            return BlackScholes.VegaBS(optionFlag, position, s, sigma, r, k, T, q);
        }


        ///=======================================================================================================================================================


        //using market data

        public double GetOptionPrice(string optionFlag, string position, double k, double T, string underlying, double? q = null)
        {
            return BlackScholesMD.OptionPrice(optionFlag, position, k, T, underlying, q); ;
        }

        public double[,] GetSensiOption(string optionFlag, string position, double k, double T, string underlying, double? q = null)
        {
            return BlackScholesMD.SensiOption(optionFlag, position, k, T, underlying, q);
        }
        public double GetOptionPortfolioPrice(int numberOfOptions, Parameters[] options)
        {
            return BlackScholesMD.OptionPortfolioPrice(numberOfOptions, options);
        }

        public double GetDelta(string optionFlag, string position, double k, double T, string underlying, double? q = null)
        {
            return BlackScholesMD.Delta(optionFlag, position, k, T, underlying, q);
        }
        public double GetGamma(string optionFlag, string position, double k, double T, string underlying, double? q = null)
        {
            return BlackScholesMD.Gamma(optionFlag, position, k, T, underlying, q);
        }

        public double GetTheta(string optionFlag, string position, double k, double T, string underlying, double? q = null)
        {
            return BlackScholesMD.Theta(optionFlag, position, k, T, underlying, q);
        }

        public double GetVega(string optionFlag, string position, double k, double T, string underlying, double? q = null)
        {
            return BlackScholesMD.Vega(optionFlag, position, k, T, underlying, q);
        }

        ///==================================================================================================================================================

        //Using Monte Carlo

        public double GetMCEurOptionPrice(string optionFlag, string position, double s, double sigma, double r, double k, double T, double? q = null)
        {
            return MonteCarlo.MCEurOptionPrice(optionFlag, position, s, sigma, r, k, T, q);
        }

        public double GetMCEurOptionPortfolioPrice(int numberOfOptions, BSParameters[] options)
        {
            return MonteCarlo.MCEurOptionPortfolioPrice(numberOfOptions, options);
        }
        //////////////////////////////////////:===========================================
        ///

        public double[] GetStripZC(DateTime paramDate, string curveName, double[] curve,
            string[] curveMaturity, int swapPeriod, int swapBasis, double fxSpot)
        {
            return StrippingIRS.StripZC(paramDate, curveName, curve,
            curveMaturity, swapPeriod, swapBasis, fxSpot);
        }

        public  double[] GetStripDefaultProbability(int cdsID, string CDSName, DateTime ParamDate,
            DateTime CDSRollDate, double[] CDSCurve, string[] CurveMaturity,
            string CDSCurrency, double RecoveryRate, bool alterMode, string intensity)
        {
            return StrippingCDS.StripDefaultProbability(cdsID, CDSName, ParamDate,
             CDSRollDate, CDSCurve, CurveMaturity,
             CDSCurrency, RecoveryRate, alterMode, intensity);
        }


        public object GetGetDefaultProb(int issuer, string maturityDate, int scenario = 0, double probMultiplier = 1)
        {
            return StrippingCDS.GetDefaultProb(issuer, maturityDate, scenario = 0, probMultiplier = 1);
        }



    }

}
