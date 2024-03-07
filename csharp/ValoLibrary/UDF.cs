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
        double GetBSOptionPrice(double quantity, string optionFlag, string position, double s, double sigma, double r, double k, double T, double? q = null);
        double[,] GetSensiOptionBS(double quantity, string optionFlag, string position, double s, double sigma, double r, double k, double T, double? q = null);
        double GetBSOptionPortfolioPrice(BSParameters[] options);
        double GetDeltaBS(double quantity, string optionFlag, string position, double s, double sigma, double r, double k, double T, double? q = null);
        double GetGammaBS(double quantity, string optionFlag, string position, double s, double sigma, double r, double k, double T, double? q = null);
        double GetThetaBS(double quantity, string optionFlag, string position, double s, double sigma, double r, double k, double T, double? q = null);
        double GetVegaBS(double quantity, string optionFlag, string position, double s, double sigma, double r, double k, double T, double? q = null);





        ///========================================================================================================================================================
        // using market data
        double GetOptionPrice(double quantity, string optionFlag, string position, double k, double T, string underlying, double? q = null);
        double[,] GetSensiOption(double quantity, string optionFlag, string position, double k, double T, string underlying, double? q = null);
        double GetOptionPortfolioPrice( Parameters[] options);
        double GetDelta(double quantity, string optionFlag, string position, double k, double T, string underlying, double? q = null);
        double GetGamma(double quantity, string optionFlag, string position, double k, double T, string underlying, double? q = null);
        double GetTheta(double quantity, string optionFlag, string position, double k, double T, string underlying, double? q = null);
        double GetVega(double quantity, string optionFlag, string position, double k, double T, string underlying, double? q = null);


        ///========================================================================================================================================================

        // using Monte Carlo
        double GetMCEurOptionPrice(double quantity, string optionFlag, string position, double s, double sigma, double r, double k, double T, double? q = null);
        double GetMCEurOptionPortfolioPrice(BSParameters[] options);

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
        public double GetBSOptionPrice(double quantity, string optionFlag, string position, double s, double sigma, double r, double k, double T, double? q = null)
        {
            return BlackScholes.BSOptionPrice(quantity, optionFlag, position, s, sigma, r, k, T, q);
        }

        public double GetBSOptionPortfolioPrice(BSParameters[] options)
        {
            return BlackScholes.BSOptionPortfolioPrice(options);
        }

        public double[,] GetSensiOptionBS(double quantity, string optionFlag, string position, double s, double sigma, double r, double k, double T, double? q = null)
        {
            return BlackScholes.SensiOptionBS(quantity,optionFlag, position, s, sigma, r, k, T, q);
        }

        public double GetDeltaBS(double quantity, string optionFlag, string position, double s, double sigma, double r, double k, double T, double? q = null)
        {
            return BlackScholes.DeltaBS(quantity, optionFlag, position, s, sigma, r, k, T, q);
        }
        public double GetGammaBS(double quantity, string optionFlag, string position, double s, double sigma, double r, double k, double T, double? q = null)
        {
            return BlackScholes.GammaBS( quantity, optionFlag, position, s, sigma, r, k, T, q);
        }

        public double GetThetaBS(double quantity, string optionFlag, string position, double s, double sigma, double r, double k, double T, double? q = null)
        {
            return BlackScholes.ThetaBS( quantity, optionFlag, position, s, sigma, r, k, T, q);
        }
        public double GetVegaBS(double quantity, string optionFlag, string position, double s, double sigma, double r, double k, double T, double? q = null)
        {
            return BlackScholes.VegaBS(quantity, optionFlag, position, s, sigma, r, k, T, q);
        }


        ///=======================================================================================================================================================


        //using market data

        public double GetOptionPrice(double quantity, string optionFlag, string position, double k, double T, string underlying, double? q = null)
        {
            return BlackScholesMD.OptionPrice(quantity, optionFlag, position, k, T, underlying, q); ;
        }

        public double[,] GetSensiOption(double quantity, string optionFlag, string position, double k, double T, string underlying, double? q = null)
        {
            return BlackScholesMD.SensiOption(quantity, optionFlag, position, k, T, underlying, q);
        }
        public double GetOptionPortfolioPrice(Parameters[] options)
        {
            return BlackScholesMD.OptionPortfolioPrice(options);
        }

        public double GetDelta(double quantity, string optionFlag, string position, double k, double T, string underlying, double? q = null)
        {
            return BlackScholesMD.Delta(quantity, optionFlag, position, k, T, underlying, q);
        }
        public double GetGamma(double quantity, string optionFlag, string position, double k, double T, string underlying, double? q = null)
        {
            return BlackScholesMD.Gamma(quantity, optionFlag, position, k, T, underlying, q);
        }

        public double GetTheta(double quantity, string optionFlag, string position, double k, double T, string underlying, double? q = null)
        {
            return BlackScholesMD.Theta(quantity, optionFlag, position, k, T, underlying, q);
        }

        public double GetVega(double quantity, string optionFlag, string position, double k, double T, string underlying, double? q = null)
        {
            return BlackScholesMD.Vega(quantity, optionFlag, position, k, T, underlying, q);
        }

        ///==================================================================================================================================================

        //Using Monte Carlo

        public double GetMCEurOptionPrice(double quantity, string optionFlag, string position, double s, double sigma, double r, double k, double T, double? q = null)
        {
            return MonteCarlo.MCEurOptionPrice( quantity, optionFlag, position, s, sigma, r, k, T, q);
        }

        public double GetMCEurOptionPortfolioPrice(BSParameters[] options)
        {
            return MonteCarlo.MCEurOptionPortfolioPrice(options);
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
