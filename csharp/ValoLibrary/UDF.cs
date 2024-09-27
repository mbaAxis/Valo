using System;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using System.Runtime.InteropServices;
using Excel = Microsoft.Office.Interop.Excel;
using Microsoft.Office.Core;

namespace ValoLibrary
{
    [ComVisible(true)]
    [Guid("839187c8-9765-4e76-a508-61ec3dd1a504")]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface IUDF
    {
        // function test 
        string GetWelcome(string s);
        // using user data
        double GetBSOptionPrice(double quantity, string optionFlag, string position, double s, double sigma,
            double r, double k, double T, double? q = null);
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
        double GetOptionPortfolioPrice(Parameters[] options);
        double GetDelta(double quantity, string optionFlag, string position, double k, double T, string underlying, double? q = null);
        double GetGamma(double quantity, string optionFlag, string position, double k, double T, string underlying, double? q = null);
        double GetTheta(double quantity, string optionFlag, string position, double k, double T, string underlying, double? q = null);
        double GetVega(double quantity, string optionFlag, string position, double k, double T, string underlying, double? q = null);


        ///========================================================================================================================================================

        // using Monte Carlo
        double GetMCEurOptionPrice(double quantity, string optionFlag, string position, double s, double sigma, double r, double k, double T, double? q = null);
        double GetMCEurOptionPortfolioPrice(BSParameters[] options);

        //===============================================================
        double[] GetStripZC(DateTime paramDate, string curveName, double[] curve, string[] curveMaturity, int swapPeriod, int swapBasis, double fxSpot);


        double[] GetStripDefaultProbability(int cdsID, string CDSName, DateTime ParamDate,
            DateTime CDSRollDate, double[] CDSCurve, string[] CurveMaturity,
            string CDSCurrency, double RecoveryRate, bool alterMode, string intensity);


        DateTime GetConvertDate(DateTime paramDate, object maturityDate);
        DateTime GetCDSRefDate(DateTime paramDate, bool isSingleNameConvention = true);
        string GetGetCDSName(int cdsID);
        double GetGetDefaultProb(int issuer, string maturityDate, int scenario = 0, double probMultiplier = 1);



        //////======================================ModelInterface====================================

        string[,] GetCDS(string issuerIdParam, string maturity, double spread, double recoveryRate,double notional,
       string cpnPeriod, string cpnConvention, string cpnLastSettle, string pricingCurrency = null,
       double fxCorrel = 0, double fxVol = 0, double isAmericanFloatLeg = 0, double isAmericanFixedLeg = 0,
       double withGreeks = 0, double[] hedgingCds = null, string integrationPeriod = "1m", double probMultiplier = 1, int[] girrMonth = null, double shock = 0,string girrCurrency = null);

        string[,] GetCDO(string maturity, double[] strikes, double[] correl,double[] spreadStandard, string pricingCurrency,
        int numberOfIssuer, string[] issuerList, double[] nominalIssuer, double spread, string cpnPeriod,
        string cpnConvention, string cpnLastSettle, double fxCorrel, double fxVol, double[] betaAdder,
        double[] recoveryIssuer = null, double isAmericanFloatLeg = 0, double isAmericanFixedLeg = 0,
        double withGreeks = 0,double withJtdVAL = 0,double withStochasticRecoveryVAL = 0, double[] hedgingCDS = null, double? lossUnitAmount = null,
        string integrationPeriod = "1m", double probMultiplier = 1, double dBeta = 0.1, int[] girrMonth = null, double shock = 0, string girrCurrency = null);

        double GetImpliedCorrelation(double upfront, double trancheSpread, string maturity, double[] strikes, double lowCorrel, double[] spreadStandard, string pricingCurrency,
    int numberOfIssuer, string[] issuerList, double[] nominalIssuer, double spread, string cpnPeriod,
    string cpnConvention, string cpnLastSettle, double fxCorrel, double fxVol, double[] betaAdder,
    double[] recoveryIssuer = null, double isAmericanFloatLeg = 0, double isAmericanFixedLeg = 0);
        //============================================== FRTB ===========================================

        double[] GetDeltaCSRCDOProxy(string[] names, string[] ratings, double[] sectors, string maturity, double[] strikes, double[] correl, double[] spreadStandard,
          string pricingCurrency, int numberOfIssuer, string[] issuerList, double[] nominalIssuer, double spread, string cpnPeriod,
          string cpnConvention, string cpnLastSettle, double fxCorrel, double fxVol, double[] betaAdder,
          double[] recoveryIssuer = null, double isAmericanFloatLeg = 0, double isAmericanFixedLeg = 0, double withStochasticRecoveryVAL = 0, double[] hedgingCDS = null, double? lossUnitAmount = null,
          string integrationPeriod = "1m", double probMultiplier = 1, double dBeta = 0.1);
        double[,] GetDeltaCSRCDO(string[] names, string[] ratings, double[] sectors, DateTime paramDate, DateTime CDSRollDate, bool alterMode, string intensity, string maturity, double[] strikes, double[] correl, double[] spreadStandard,
           string pricingCurrency, int numberOfIssuer, string[] issuerList, double[] nominalIssuer, double spread, string cpnPeriod,
           string cpnConvention, string cpnLastSettle, double fxCorrel, double fxVol, double[] betaAdder,
           double[] recoveryIssuer = null, double isAmericanFloatLeg = 0, double isAmericanFixedLeg = 0, double withStochasticRecoveryVAL = 0);
        double[,] GetDeltaCSRCDS(string[] names, string[] ratings, double[] sectors, DateTime paramDate, DateTime CDSRollDate, bool alterMode, string intensity, string maturity,
            double[] spreadStandard, int numberOfIssuer, string[] issuerList, double[] nominalIssuer, double[] recoveryIssuer,string cpnPeriod, string cpnConvention, string cpnLastSettle);
        double[,] GetcurvatureCSR(double floor, string riskClass, double[,] deltaSensitivities, string[] names, string[] ratings, double[] sectors, DateTime paramDate, DateTime CDSRollDate,
            bool alterMode, string intensity, string maturity, double[] strikes, double[] correl, double[] spreadStandard, string pricingCurrency, int numberOfIssuer, string[] issuerList,
            double[] nominalIssuer, double spread, string cpnPeriod, string cpnConvention, string cpnLastSettle, double fxCorrel, double fxVol, double[] betaAdder,
            double[] recoveryIssuer = null, string correlationScenario = "MEDIUM", double isAmericanFloatLeg = 0, double isAmericanFixedLeg = 0, double withStochasticRecoveryVAL = 0);
        double GetcurvatureCSRPROXY(double[] deltaSensitivities, string[] names, string[] ratings, double[] sectors, string maturity, double[] strikes, double[] correl, double[] spreadStandard, string pricingCurrency, int numberOfIssuer, string[] issuerList,
            double[] nominalIssuer, double spread, string cpnPeriod, string cpnConvention, string cpnLastSettle, double fxCorrel, double fxVol, double[] betaAdder,
            double[] recoveryIssuer = null, double isAmericanFloatLeg = 0, double isAmericanFixedLeg = 0, double withStochasticRecoveryVAL = 0,
            double? lossUnitAmount = null, string integrationPeriod = "1m", double probMultiplier = 1, double dBeta = 0.1);
        //======================================Stripping IRS====================================
        void GetStoreCurve(string[] currency, double[] swapBasis, double[] FXRate, double[] swapPeriod, string[] curveNames, double[,] swapRates,
            string[] curveDates, string[] typeOfCurve);
        int[] GetBucketCompute(int numberOfIssuer, string[] issuerName, string[] ratings, double[] sectors);

    }




    [ComVisible(true)]
    [ProgId("ValoLibrary.UDF")]
    [Guid("14041acd-3ec6-4ba1-922b-87858755d0e7")]
    [ClassInterface(ClassInterfaceType.None)]
    public class UDF : IUDF
    {
        public string GetWelcome(string s)
        {
            if(String.Equals(s,"")|| String.IsNullOrEmpty(s))
            {
                return "ok";
            }
            return s;
        }  

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
            return BlackScholes.SensiOptionBS(quantity, optionFlag, position, s, sigma, r, k, T, q);
        }

        public double GetDeltaBS(double quantity, string optionFlag, string position, double s, double sigma, double r, double k, double T, double? q = null)
        {
            return BlackScholes.DeltaBS(quantity, optionFlag, position, s, sigma, r, k, T, q);
        }
        public double GetGammaBS(double quantity, string optionFlag, string position, double s, double sigma, double r, double k, double T, double? q = null)
        {
            return BlackScholes.GammaBS(quantity, optionFlag, position, s, sigma, r, k, T, q);
        }

        public double GetThetaBS(double quantity, string optionFlag, string position, double s, double sigma, double r, double k, double T, double? q = null)
        {
            return BlackScholes.ThetaBS(quantity, optionFlag, position, s, sigma, r, k, T, q);
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
            return MonteCarlo.MCEurOptionPrice(quantity, optionFlag, position, s, sigma, r, k, T, q);
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

        public DateTime GetConvertDate(DateTime paramDate, object maturityDate)
        {
            return UtilityDates.ConvertDate(paramDate, maturityDate);
        }

        public DateTime GetCDSRefDate(DateTime paramDate, bool isSingleNameConvention = true)
        {
            return StrippingCDS.CDSRefDate(paramDate);
        }

        public string GetGetCDSName(int cdsID)
        {
            return StrippingCDS.GetCDSName(cdsID);
        }

        public double[] GetStripDefaultProbability(int cdsID, string CDSName, DateTime ParamDate,
            DateTime CDSRollDate, double[] CDSCurve, string[] CurveMaturity,
            string CDSCurrency, double RecoveryRate, bool alterMode, string intensity)
        {
            return StrippingCDS.StripDefaultProbability(cdsID, CDSName, ParamDate,
             CDSRollDate, CDSCurve, CurveMaturity,
             CDSCurrency, RecoveryRate, alterMode, intensity);
        }


        public double GetGetDefaultProb(int issuer, string maturityDate, int scenario = 0, double probMultiplier = 1)
        {
            return StrippingCDS.GetDefaultProb(issuer, maturityDate, scenario = 0, probMultiplier = 1);
        }

        // ===================================Model Interface========================================,

        public string[,] GetCDS(string issuerIdParam, string maturity, double spread, double recoveryRate, double notional,
        string cpnPeriod, string cpnConvention, string cpnLastSettle, string pricingCurrency = null,
        double fxCorrel = 0, double fxVol = 0, double isAmericanFloatLeg = 0, double isAmericanFixedLeg = 0,
        double withGreeks = 0, double[] hedgingCds = null, string integrationPeriod = "1m", double probMultiplier = 1,int[] girrMonth =null, double shock = 0, string girrCurrency = null)
        {
            return ModelInterface.CDS(issuerIdParam, maturity, spread, recoveryRate,notional,
             cpnPeriod, cpnConvention, cpnLastSettle, pricingCurrency,
             fxCorrel, fxVol, isAmericanFloatLeg, isAmericanFixedLeg,
             withGreeks, hedgingCds, integrationPeriod, probMultiplier, girrMonth,shock, girrCurrency);

        }

        public string[,] GetCDO(string maturity, double[] strikes, double[] correl,double[] spreadStandard, string pricingCurrency,
           int numberOfIssuer, string[] issuerList, double[] nominalIssuer, double spread, string cpnPeriod,
           string cpnConvention, string cpnLastSettle, double fxCorrel, double fxVol, double[] betaAdder,
           double[] recoveryIssuer = null, double isAmericanFloatLeg = 0, double isAmericanFixedLeg = 0,
           double withGreeks = 0, double withJtdVAL = 0,double withStochasticRecoveryVAL= 0, double[] hedgingCDS = null, double? lossUnitAmount = null,
           string integrationPeriod = "1m", double probMultiplier = 1, double dBeta = 0.1, int[] girrMonth = null, double shock = 0,string girrCurrency = null)
        {
            lossUnitAmount=null;
            return ModelInterface.CDO(maturity, strikes, correl, spreadStandard, pricingCurrency, numberOfIssuer, issuerList, nominalIssuer,
    spread, cpnPeriod, cpnConvention, cpnLastSettle, fxCorrel, fxVol, betaAdder, recoveryIssuer, isAmericanFloatLeg,
    isAmericanFixedLeg, withGreeks,withJtdVAL,withStochasticRecoveryVAL, hedgingCDS, lossUnitAmount, integrationPeriod, probMultiplier, dBeta, girrMonth,shock,girrCurrency);
        }

        public double GetImpliedCorrelation(double upfront, double trancheSpread, string maturity, double[] strikes, double lowCorrel, double[] spreadStandard, string pricingCurrency,
    int numberOfIssuer, string[] issuerList, double[] nominalIssuer, double spread, string cpnPeriod,
    string cpnConvention, string cpnLastSettle, double fxCorrel, double fxVol, double[] betaAdder,
    double[] recoveryIssuer = null, double isAmericanFloatLeg = 0, double isAmericanFixedLeg = 0)
        {
            return ModelInterface.ImpliedCorrelation(upfront, trancheSpread,maturity,strikes,lowCorrel,spreadStandard,pricingCurrency,numberOfIssuer,issuerList,
                nominalIssuer,spread,cpnPeriod,cpnConvention,cpnLastSettle,fxCorrel,fxVol,betaAdder,recoveryIssuer,isAmericanFloatLeg,isAmericanFixedLeg);
        }
        // ===================================Stripping IRS========================================
        public void GetStoreCurve(string[] currency, double[] swapBasis, double[] FXRate, double[] swapPeriod, string[] curveNames, double[,] swapRates,
            string[] curveDates, string[] typeOfCurve)
        {
            int[] intswapBasis = swapBasis.Select(d => (int)d).ToArray();
            int[] intswapPeriod = swapPeriod.Select(d => (int)d).ToArray();
            StrippingIRS.StoreCurve(currency, intswapBasis, FXRate, intswapPeriod,curveNames,swapRates,curveDates,typeOfCurve);
        }
        public int[] GetBucketCompute(int numberOfIssuer, string[] issuerName, string[] ratings, double[] sectors)
        {
            return FRTB.BucketCompute(numberOfIssuer,issuerName, ratings, sectors);
        }

        //============================================FRTB==============================================
        public double[] GetDeltaCSRCDOProxy(string[] names, string[] ratings, double[] sectors, string maturity, double[] strikes, double[] correl, double[] spreadStandard,
  string pricingCurrency, int numberOfIssuer, string[] issuerList, double[] nominalIssuer, double spread, string cpnPeriod,
  string cpnConvention, string cpnLastSettle, double fxCorrel, double fxVol, double[] betaAdder,
  double[] recoveryIssuer = null, double isAmericanFloatLeg = 0, double isAmericanFixedLeg = 0, double withStochasticRecoveryVAL = 0, double[] hedgingCDS = null, double? lossUnitAmount = null,
  string integrationPeriod = "1m", double probMultiplier = 1, double dBeta = 0.1)
        {
            lossUnitAmount = null;
            return FRTB.DeltaCSRCDOProxy(names, ratings, sectors, maturity, strikes, correl, spreadStandard, pricingCurrency, numberOfIssuer, issuerList, nominalIssuer,
                spread, cpnPeriod, cpnConvention, cpnLastSettle, fxCorrel, fxVol, betaAdder, recoveryIssuer, isAmericanFloatLeg, isAmericanFixedLeg, withStochasticRecoveryVAL,
                hedgingCDS, lossUnitAmount, integrationPeriod, probMultiplier, dBeta);
        }
        public double[,] GetDeltaCSRCDO(string[] names, string[] ratings, double[] sectors, DateTime paramDate, DateTime CDSRollDate, bool alterMode, string intensity, string maturity, double[] strikes, double[] correl, double[] spreadStandard,
           string pricingCurrency, int numberOfIssuer, string[] issuerList, double[] nominalIssuer, double spread, string cpnPeriod,
           string cpnConvention, string cpnLastSettle, double fxCorrel, double fxVol, double[] betaAdder,
           double[] recoveryIssuer = null, double isAmericanFloatLeg = 0, double isAmericanFixedLeg = 0, double withStochasticRecoveryVAL = 0)
        {
            return FRTB.DeltaCSRCDO(names, ratings, sectors, paramDate, CDSRollDate, alterMode, intensity, maturity, strikes, correl, spreadStandard, pricingCurrency,
                numberOfIssuer, issuerList, nominalIssuer, spread, cpnPeriod, cpnConvention, cpnLastSettle, fxCorrel, fxVol, betaAdder, recoveryIssuer, isAmericanFloatLeg,
                isAmericanFixedLeg, withStochasticRecoveryVAL);
        }
        public double[,] GetDeltaCSRCDS(string[] names, string[] ratings, double[] sectors, DateTime paramDate, DateTime CDSRollDate, bool alterMode, string intensity, string maturity,
            double[] spreadStandard, int numberOfIssuer, string[] issuerList, double[] nominalIssuer,double[] recoveryIssuer, string cpnPeriod, string cpnConvention, string cpnLastSettle)
        {
            return FRTB.DeltaCSRCDS(names, ratings, sectors, paramDate, CDSRollDate, alterMode, intensity, maturity, spreadStandard,
                numberOfIssuer, issuerList, nominalIssuer, recoveryIssuer,cpnPeriod, cpnConvention, cpnLastSettle);
        }
        public double[,] GetcurvatureCSR(double floor, string riskClass, double[,] deltaSensitivities, string[] names, string[] ratings, double[] sectors, DateTime paramDate, DateTime CDSRollDate,
            bool alterMode, string intensity, string maturity, double[] strikes, double[] correl, double[] spreadStandard, string pricingCurrency, int numberOfIssuer, string[] issuerList,
            double[] nominalIssuer, double spread, string cpnPeriod, string cpnConvention, string cpnLastSettle, double fxCorrel, double fxVol, double[] betaAdder,
            double[] recoveryIssuer = null, string correlationScenario = "MEDIUM", double isAmericanFloatLeg = 0, double isAmericanFixedLeg = 0, double withStochasticRecoveryVAL = 0)
        {
            return FRTB.curvatureCSR(floor, riskClass, deltaSensitivities, names, ratings, sectors, paramDate, CDSRollDate, alterMode, intensity, maturity, strikes, correl,
                spreadStandard, pricingCurrency, numberOfIssuer, issuerList, nominalIssuer, spread, cpnPeriod, cpnConvention, cpnLastSettle, fxCorrel, fxVol, betaAdder,
                recoveryIssuer, correlationScenario, isAmericanFloatLeg, isAmericanFixedLeg, withStochasticRecoveryVAL);
        }
        public double GetcurvatureCSRPROXY(double[] deltaSensitivities, string[] names, string[] ratings, double[] sectors, string maturity, double[] strikes, double[] correl, double[] spreadStandard, string pricingCurrency, int numberOfIssuer, string[] issuerList,
            double[] nominalIssuer, double spread, string cpnPeriod, string cpnConvention, string cpnLastSettle, double fxCorrel, double fxVol, double[] betaAdder,
            double[] recoveryIssuer = null, double isAmericanFloatLeg = 0, double isAmericanFixedLeg = 0, double withStochasticRecoveryVAL = 0,
            double? lossUnitAmount = null, string integrationPeriod = "1m", double probMultiplier = 1, double dBeta = 0.1)
        {
            lossUnitAmount = null;
            return FRTB.curvatureCSRPROXY(deltaSensitivities, names, ratings, sectors, maturity, strikes, correl, spreadStandard, pricingCurrency, numberOfIssuer, issuerList,
                nominalIssuer, spread, cpnPeriod, cpnConvention, cpnLastSettle, fxCorrel, fxVol, betaAdder, recoveryIssuer, isAmericanFloatLeg, isAmericanFixedLeg,
                withStochasticRecoveryVAL, lossUnitAmount, integrationPeriod, probMultiplier, dBeta);
        }
    }
}
