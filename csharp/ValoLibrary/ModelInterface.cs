using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ValoLibrary.StrippingCDS;

namespace ValoLibrary
{
    public class ModelInterface
    {

        //    public static object CDO(string maturity, double[] strikes, double[] correl, string pricingCurrency,
        //int numberOfIssuer, string[] issuerList, double[] nominalIssuer, double spread, string cpnPeriod,
        //string cpnConvention, string cpnLastSettle, double fxCorrel, double fxVol, double[] betaAdder,
        //double? recoveryIssuer = null, bool isAmericanFloatLeg = false, bool isAmericanFixedLeg = false,
        //bool withGreeks = false, string hedgingCDS = null, double? lossUnitAmount = null,
        //string integrationPeriod = "1m", double probMultiplier = 1, double dBeta = 0.1)
        //    {
        //        int i;
        //        double[] recoveryRate;

        //        // Check Interest Rate Curve
        //        int curveId;
        //        string[] vbaIssuerList;

        //        curveId = StrippingIRS.GetCurveId(pricingCurrency);
        //        vbaIssuerList = new string[issuerList.Length];

        //        if (curveId == -1)
        //        {
        //            if (!StrippingIRS.InterestRateCurves.LastError)
        //            {
        //                Console.WriteLine($"CDO Pricing: Curve {pricingCurrency} was not stripped - Called from : {Environment.StackTrace}");
        //                StrippingIRS.InterestRateCurves.LastError = true;
        //            }
        //            return null;
        //        }

        //        // Check Portfolio of Issuer
        //        if (numberOfIssuer > issuerList.Length)
        //        {
        //            return $"CDO Pricing: Not enough Issuers specified compared to the indicated number of issuer - Called from: {Environment.StackTrace}";
        //        }
        //        else if (issuerList == null)
        //        {
        //            return $"CDO Pricing: No Issuers specified - Called from: {Environment.StackTrace}";
        //        }
        //        else
        //        {
        //            for (i = 0; i < numberOfIssuer; i++)
        //            {
        //                if (issuerList.Length <= i)
        //                {
        //                    Console.WriteLine($"CDO Pricing: No Issuers specified in position {i} - Called from: {Environment.StackTrace}");
        //                    return $"CDO Pricing: No Issuers specified in position {i} - Called from: {Environment.StackTrace}";
        //                }
        //                else
        //                {
        //                    if (!UtilityDates.IsNumeric(issuerList[i].ToString()))
        //                    {
        //                        vbaIssuerList[i] = StrippingCDS.GetCDSCurveId(issuerList[i].ToString());
        //                    }
        //                    else
        //                    {
        //                        vbaIssuerList[i] = issuerList[i];
        //                    }

        //                    if (vbaIssuerList[i] == -1+"")
        //                    {
        //                        return $"CDO Pricing: Position {i}: IssuerID {issuerList[i]} not recognized - Called from: {Environment.StackTrace}";
        //                    }

        //                    if (vbaIssuerList[i] > StrippingCDS.CreditDefaultSwapCurves.NumberOfCurves)
        //                    {
        //                        return $"CDO Pricing: Position {i}: IssuerID ({issuerList[i]}) exceeds range of defined issuer - Called from: {Environment.StackTrace}";
        //                    }
        //                    else if (!StrippingCDS.CreditDefaultSwapCurves.Curves[vbaIssuerList[i]].CDSdone)
        //                    {
        //                        return $"CDO Pricing: Position {i}: IssuerID ({issuerList[i]}) CDS curve not stripped - Called from: {Environment.StackTrace}";
        //                    }
        //                }
        //            }
        //        }

        //        if (!recoveryIssuer.HasValue || recoveryIssuer == null)
        //        {
        //            recoveryRate = new double[(int)numberOfIssuer];
        //            for (i = 0; i < numberOfIssuer; i++)
        //            {
        //                recoveryRate[i] = CreditDefaultSwapCurves.Curves[vbaIssuerList[i]].Recovery;
        //            }
        //        }
        //        else
        //        {
        //            recoveryRate = new double[numberOfIssuer];
        //            for (i = 0; i < numberOfIssuer; i++)
        //            {
        //                if (!recoveryIssuer.HasValue || recoveryIssuer == null)
        //                {
        //                    recoveryRate[i] = CreditDefaultSwapCurves.Curves[vbaIssuerList[i]].Recovery;
        //                }
        //                else
        //                {
        //                    recoveryRate[i] = (double)recoveryIssuer;
        //                }
        //            }
        //        }

        //        if (!lossUnitAmount.HasValue || lossUnitAmount == null)
        //        {
        //            lossUnitAmount = LossUnit(numberOfIssuer, nominalIssuer, recoveryRate, 0.0001);
        //        }

        //        if (!betaAdder.Any())
        //        {
        //            betaAdder = new double[(int)numberOfIssuer];
        //            for (i = 0; i < numberOfIssuer; i++)
        //            {
        //                betaAdder[i] = 0;
        //            }
        //        }

        //        return AmericanSwap(maturity,
        //            numberOfIssuer, vbaIssuerList, nominalIssuer, recoveryRate,
        //            spread, cpnLastSettle, cpnPeriod, cpnConvention,
        //            pricingCurrency, fxCorrel, fxVol,
        //            strikes, correl, betaAdder,
        //            isAmericanFloatLeg, isAmericanFixedLeg,
        //            withGreeks, hedgingCDS, (double)lossUnitAmount, integrationPeriod, null, probMultiplier, dBeta);
        //    }

        public static double ProxyPGCD(double a, double b, double precision = 0.0001)
        {
            if (b > a)
            {
                return ProxyPGCD(b, a);
            }
            else if (Math.Abs(b) <= Math.Abs(a) * precision)
            {
                return a;
            }
            else
            {
                return ProxyPGCD(b, a - Math.Floor(a / b) * b);
            }
        }

        public static double AmountUnit(double numberOfNames, double[] amounts, double optionalPrecision = 0.0001)
        {
            double amountUnit = amounts[0];

            for (int i = 1; i < numberOfNames; i++)
            {
                double lossAmount = amounts[i];
                amountUnit = ProxyPGCD(amountUnit, lossAmount, optionalPrecision);
            }

            return amountUnit;
        }

        public static double LossUnit(double numberOfNames, double[] nominals, double[] recoveries, double optionalPrecision = 0.0001)
        {
            double[] lossRates = new double[(int)numberOfNames];

            double nominalUnit = AmountUnit(numberOfNames, nominals, optionalPrecision);
            for (int i = 0; i < numberOfNames; i++)
            {
                lossRates[i] = 1.0 - recoveries[i];
            }

            double lossRateUnit = AmountUnit(numberOfNames, lossRates, optionalPrecision);

            double lossUnit = nominalUnit * lossRateUnit;
            double lossAmount = nominals[0] * (1.0 - recoveries[0]);
            long lossNumber = (long)Math.Round(lossAmount / lossUnit);
            long lossPGCD = lossNumber;

            for (int i = 1; i < numberOfNames; i++)
            {
                lossAmount = nominals[i] * (1.0 - recoveries[i]);
                lossNumber = (long)Math.Round(lossAmount / lossUnit);
                lossPGCD = (long)ProxyPGCD(lossPGCD, lossNumber);
            }

            lossUnit *= lossPGCD;

            return lossUnit;
        }

        public static object GetLossUnit(double numberOfIssuer, double[] issuerList, double[] nominalIssuer, double[] recoveryIssuer = null)
        {
            int i;


            // Check Portfolio of Issuer
            int lowerBound = issuerList.GetLowerBound(0);
            int upperBound = issuerList.GetUpperBound(0);
            double[] vbaIssuerList = new double[upperBound - lowerBound + 1];

            if (numberOfIssuer > issuerList.Length)
            {
                return $"GetLossUnit: Not enough Issuers specified compared to the indicated number of issuer - Called from: {Environment.StackTrace}";
            }
            else if (issuerList == null)
            {
                return $"GetLossUnit: No Issuers specified  - Called from: {Environment.StackTrace}";
            }
            else
            {
                for (i = 0; i < numberOfIssuer; i++)
                {
                    if (issuerList == null || issuerList.Length < i)
                    {
                        return $"GetLossUnit: No Recovery Rate specified for Issuer in position {i} - Called from: {Environment.StackTrace}";
                    }
                    else
                    {
                        if (!UtilityDates.IsNumeric(issuerList[i]))
                        {
                            vbaIssuerList[i] = StrippingCDS.GetCDSCurveId(issuerList[i].ToString());
                        }
                        else
                        {
                            vbaIssuerList[i] = issuerList[i];
                        }

                        if (vbaIssuerList[i] == -1)
                        {
                            return $"GetLossUnit: Position {i}: IssuerID {issuerList[i]} not recognised - Called from: {Environment.StackTrace}";
                        }

                        if (vbaIssuerList[i] > StrippingCDS.CreditDefaultSwapCurves.NumberOfCurves)
                        {
                            return $"GetLossUnit: Position {i}: IssuerID ({issuerList[i]}) exceeds range of defined issuer - Called from: {Environment.StackTrace}";
                        }
                        else if (!StrippingCDS.CreditDefaultSwapCurves.Curves[(int)vbaIssuerList[i]].CDSdone)
                        {
                            return $"GetLossUnit: Position {i}: IssuerID ({issuerList[i]}) CDS curve not stripped - Called from: {Environment.StackTrace}";
                        }
                    }
                }
            }


            double[] recoveryRate;
            if (recoveryIssuer == null || recoveryIssuer.Length == 0)
            {
                recoveryRate = new double[(int)numberOfIssuer];
                for (i = 0; i < numberOfIssuer; i++)
                {
                    recoveryRate[i] = StrippingCDS.CreditDefaultSwapCurves.Curves[(int)vbaIssuerList[i]].Recovery;
                }
            }
            else
            {
                recoveryRate = new double[(int)numberOfIssuer];
                for (i = 0; i < numberOfIssuer; i++)
                {
                    if (recoveryIssuer == null || recoveryIssuer.Length == 0)
                    {
                        recoveryRate[i] = StrippingCDS.CreditDefaultSwapCurves.Curves[(int)vbaIssuerList[i]].Recovery;
                    }
                    else
                    {
                        recoveryRate[i] = recoveryIssuer[i];
                    }
                }
            }

            return LossUnit(numberOfIssuer, nominalIssuer, recoveryRate, 0.0001);
        }

        public static object CDS(string issuerId, string maturity, double spread, double recoveryRate,
        string cpnPeriod, string cpnConvention, string cpnLastSettle, string pricingCurrency = null,
        double fxCorrel = 0, double fxVol = 0, bool isAmericanFloatLeg = false, bool isAmericanFixedLeg = false,
        bool withGreeks = false, string hedgingCds = null, string integrationPeriod = "1m", double probMultiplier = 1)
        {
            if (!int.TryParse(issuerId, out int _))
            {
                _ = StrippingCDS.GetCDSCurveId(issuerId);
            }

            if (Convert.ToDouble(issuerId) > CreditDefaultSwapCurves.NumberOfCurves)
            {
                return $"CDS - Issuer {issuerId} out of range - probability set to 0 - called from {Environment.StackTrace}";
            }
            else if (!CreditDefaultSwapCurves.Curves[Convert.ToInt32(issuerId)].CDSdone)
            {
                return $"CDS - Issuer {issuerId} not defined - probability set to 0 - called from {Environment.StackTrace}";
            }



            if (pricingCurrency == null || string.IsNullOrEmpty(pricingCurrency))
            {
                pricingCurrency = StrippingCDS.CreditDefaultSwapCurves.Curves[Convert.ToInt32(issuerId)].Currency;
            }

            int curveId = StrippingIRS.GetCurveId(pricingCurrency);
            if (curveId == -1)
            {
                if (StrippingIRS.InterestRateCurves.LastError == false)
                {
                    Console.WriteLine($"CDS Pricing: Curve {pricingCurrency} was not stripped - Called from : {Environment.StackTrace}");
                    StrippingIRS.InterestRateCurves.LastError = true;
                }
                return null;
            }

            if (double.IsNaN(recoveryRate))
            {
                recoveryRate = StrippingCDS.CreditDefaultSwapCurves.Curves[Convert.ToInt32(issuerId)].Recovery;
            }

            return AmericanSwap(maturity, 1, issuerId, 1, recoveryRate, spread, cpnLastSettle, cpnPeriod, cpnConvention,
                pricingCurrency, fxCorrel, fxVol, 0, 0, 0, isAmericanFloatLeg, isAmericanFixedLeg, withGreeks, hedgingCds, 1,
                integrationPeriod, null, probMultiplier);
        }








    }
}
