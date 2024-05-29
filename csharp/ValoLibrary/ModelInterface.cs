using Microsoft.Office.Interop.Excel;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using static ValoLibrary.StrippingCDS;

namespace ValoLibrary
{
    public class ModelInterface
    {
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

        
        public static string[,] CDO(string maturity, double[] strikes, double[] correl,double[] spreadStandard, string pricingCurrency,
    int numberOfIssuer, string[] issuerList, double[] nominalIssuer, double spread, string cpnPeriod,
    string cpnConvention, string cpnLastSettle, double fxCorrel, double fxVol, double[] betaAdder,
    double[] recoveryIssuer = null, double isAmericanFloatLeg = 0, double isAmericanFixedLeg = 0,
    double withGreeks = 0, double[] hedgingCDS = null, double? lossUnitAmount = null,
    string integrationPeriod = "1m", double probMultiplier = 1, double dBeta = 0.1)
        {
            int i;
            double[] recoveryRate;
            int curveId;

            curveId = StrippingIRS.GetCurveId(pricingCurrency);
            int[] vbaIssuerList = new int[issuerList.Length];

            if (curveId == -1)
            {
                if (!StrippingIRS.InterestRateCurves.LastError)
                {
                    Console.WriteLine($"CDO Pricing: Curve {pricingCurrency} was not stripped - Called from : {Environment.StackTrace}");
                    StrippingIRS.InterestRateCurves.LastError = true;
                }
                return null;
            }

            if (numberOfIssuer > issuerList.Length)
            {
                Console.WriteLine($"CDO Pricing: Not enough Issuers specified compared to the indicated number of issuer - Called from: {Environment.StackTrace}");
                return null;
            }
            else if (issuerList == null)
            {
                Console.WriteLine($"CDO Pricing: No Issuers specified - Called from: {Environment.StackTrace}");
                return null;
            }
            else
            {
                for (i = 0; i < numberOfIssuer; i++)
                {
                    if (issuerList.Length <= i)
                    {
                        Console.WriteLine($"CDO Pricing: No Issuers specified in position {i} - Called from: {Environment.StackTrace}");
                        return null;
                    }
                    else
                    {
                        if (!int.TryParse(issuerList[i],out _))
                        {
                            vbaIssuerList[i] = StrippingCDS.GetCDSCurveId(issuerList[i]);
                        }
                        else
                        {
                            vbaIssuerList[i] = int.Parse(issuerList[i]);
                        }

                        if (vbaIssuerList[i] == -1)
                        {
                            Console.WriteLine($"CDO Pricing: Position {i}: IssuerID {issuerList[i]} not recognized - Called from: {Environment.StackTrace}");
                            return null;

                        }

                        if (vbaIssuerList[i] > StrippingCDS.CreditDefaultSwapCurves.NumberOfCurves)
                        {
                            Console.WriteLine($"CDO Pricing: Position {i}: IssuerID ({issuerList[i]}) exceeds range of defined issuer - Called from: {Environment.StackTrace}");
                            return null;
                        }
                        else if (!StrippingCDS.CreditDefaultSwapCurves.Curves[vbaIssuerList[i]].CDSdone)
                        {
                            Console.WriteLine($"CDO Pricing: Position {i}: IssuerID ({issuerList[i]}) CDS curve not stripped - Called from: {Environment.StackTrace}");
                            return null;
                        }
                    }
                }
            }

            if (recoveryIssuer == null || recoveryIssuer.Length == 0)
            {
                recoveryRate = new double[(int)numberOfIssuer];
                for (i = 0; i < numberOfIssuer; i++)
                {
                    recoveryRate[i] = CreditDefaultSwapCurves.Curves[vbaIssuerList[i]].Recovery;
                }
            }
            else
            {
                recoveryRate = new double[numberOfIssuer];
                for (i = 0; i < numberOfIssuer; i++)
                {
                    if (recoveryIssuer == null || recoveryIssuer.Length == 0)
                    {
                        recoveryRate[i] = CreditDefaultSwapCurves.Curves[vbaIssuerList[i]].Recovery;
                    }
                    else
                    {
                        recoveryRate[i] = recoveryIssuer[i];
                    }
                }
            }

            if (!lossUnitAmount.HasValue || lossUnitAmount == null)
            {
                lossUnitAmount = LossUnit(numberOfIssuer, nominalIssuer, recoveryRate, 0.0001);
            }

            if (betaAdder == null || betaAdder.Length == 0)
            {
                betaAdder = new double[(int)numberOfIssuer];
                for (i = 0; i < numberOfIssuer; i++)
                {
                    betaAdder[i] = 0;
                }
            }

            return AmericanSwap(maturity,
                numberOfIssuer, vbaIssuerList, nominalIssuer, recoveryRate,spreadStandard,
                spread, cpnLastSettle, cpnPeriod, cpnConvention,
                pricingCurrency, fxCorrel, fxVol,
                strikes, correl, betaAdder,
                isAmericanFloatLeg, isAmericanFixedLeg,
                withGreeks, hedgingCDS, (double)lossUnitAmount, integrationPeriod, null, probMultiplier, dBeta);
        }
        public static double[] CDOtest(string maturity, double[] strikes)
        {
            return strikes;
        }
    //    public static string[,] CDOTEST(string maturity, double[] strikes, double[] correl, string pricingCurrency,
    //int numberOfIssuer, string[] issuerList, double[] nominalIssuer, double spread, string cpnPeriod,
    //string cpnConvention, string cpnLastSettle, double fxCorrel, double fxVol, double[] betaAdder,
    //double[] recoveryIssuer = null, double isAmericanFloatLeg = 0, double isAmericanFixedLeg = 0,
    //double withGreeks = 0, double[] hedgingCDS = null, double? lossUnitAmount = null,
    //string integrationPeriod = "1m", double probMultiplier = 1, double dBeta = 0.1)
    //    {
    //        int i;
    //        double[] recoveryRate;

    //        int curveId;
    //        object vbaIssuerList;

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

    //        if (numberOfIssuer > issuerList.Length)
    //        {
    //            Console.WriteLine($"CDO Pricing: Not enough Issuers specified compared to the indicated number of issuer - Called from: {Environment.StackTrace}");
    //            return null;
    //        }
    //        else if (issuerList == null)
    //        {
    //            Console.WriteLine($"CDO Pricing: No Issuers specified - Called from: {Environment.StackTrace}");
    //            return null;
    //        }
    //        else
    //        {
    //            for (i = 0; i < numberOfIssuer; i++)
    //            {
    //                if (issuerList.Length <= i)
    //                {
    //                    Console.WriteLine($"CDO Pricing: No Issuers specified in position {i} - Called from: {Environment.StackTrace}");
    //                    return null;
    //                }
    //                else
    //                {
    //                    if (!UtilityDates.IsNumeric(issuerList[i].ToString()))
    //                    {
    //                        ((int[])vbaIssuerList)[i] = StrippingCDS.GetCDSCurveId(issuerList[i].ToString());
    //                    }
    //                    else
    //                    {
    //                        ((string[])vbaIssuerList)[i] = issuerList[i];
    //                    }

    //                    if (((double[])vbaIssuerList)[i] == -1)
    //                    {
    //                        Console.WriteLine($"CDO Pricing: Position {i}: IssuerID {issuerList[i]} not recognized - Called from: {Environment.StackTrace}");
    //                        return null;

    //                    }

    //                    if (((int[])vbaIssuerList)[i] > StrippingCDS.CreditDefaultSwapCurves.NumberOfCurves)
    //                    {
    //                        Console.WriteLine($"CDO Pricing: Position {i}: IssuerID ({issuerList[i]}) exceeds range of defined issuer - Called from: {Environment.StackTrace}");
    //                        return null;
    //                    }
    //                    else if (!StrippingCDS.CreditDefaultSwapCurves.Curves[((int[])vbaIssuerList)[i]].CDSdone)
    //                    {
    //                        Console.WriteLine($"CDO Pricing: Position {i}: IssuerID ({issuerList[i]}) CDS curve not stripped - Called from: {Environment.StackTrace}");
    //                        return null;
    //                    }
    //                }
    //            }
    //        }

    //        if (recoveryIssuer == null || recoveryIssuer.Length == 0)
    //        {
    //            recoveryRate = new double[(int)numberOfIssuer];
    //            for (i = 0; i < numberOfIssuer; i++)
    //            {
    //                recoveryRate[i] = CreditDefaultSwapCurves.Curves[((int[])vbaIssuerList)[i]].Recovery;
    //            }
    //        }
    //        else
    //        {
    //            recoveryRate = new double[numberOfIssuer];
    //            for (i = 0; i < numberOfIssuer; i++)
    //            {
    //                if (recoveryIssuer == null || recoveryIssuer.Length == 0)
    //                {
    //                    recoveryRate[i] = CreditDefaultSwapCurves.Curves[((int[])vbaIssuerList)[i]].Recovery;
    //                }
    //                else
    //                {
    //                    recoveryRate[i] = recoveryIssuer[i];
    //                }
    //            }
    //        }

    //        if (!lossUnitAmount.HasValue || lossUnitAmount == null)
    //        {
    //            lossUnitAmount = LossUnit(numberOfIssuer, nominalIssuer, recoveryRate, 0.0001);
    //        }

    //        if (betaAdder == null || betaAdder.Length == 0)
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
        public static string[,] CDS(string issuerIdParam, string maturity, double spread, double recoveryRate,double notional,
        string cpnPeriod, string cpnConvention, string cpnLastSettle, string pricingCurrency = null,
        double fxCorrel = 0, double fxVol = 0, double isAmericanFloatLeg = 0, double isAmericanFixedLeg = 0,
        double withGreeks = 0, double[] hedgingCds = null, string integrationPeriod = "1m", double probMultiplier = 1)
        {

            int issuerId;

            if (!Utils.IsNumeric(issuerIdParam))
            {
                issuerId = StrippingCDS.GetCDSCurveId((string) issuerIdParam) ;
            }
            else
            {
                issuerId = (int) Double.Parse(issuerIdParam); // update
            }
            
            if (Convert.ToDouble(issuerId) > CreditDefaultSwapCurves.NumberOfCurves)
            {
                Console.WriteLine($"CDS - Issuer {issuerId} out of range - probability set to 0 - called from {Environment.StackTrace}");
                return null;
            }
            else if (!CreditDefaultSwapCurves.Curves[Convert.ToInt32(issuerId)].CDSdone)
            {

                Console.WriteLine($"CDS - Issuer {issuerId} not defined - probability set to 0 - called from {Environment.StackTrace}");
                return null;
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

            return AmericanSwap(maturity, 1, issuerId, notional, recoveryRate, 0,spread, cpnLastSettle , cpnPeriod, cpnConvention,
                pricingCurrency, fxCorrel, fxVol, 0.0, 0.0, 0.0, isAmericanFloatLeg, isAmericanFixedLeg, withGreeks, hedgingCds, 1,
                integrationPeriod,null, probMultiplier);
        }
        public static string[,] AmericanSwap(object maturity, int numberOfIssuer, object IssuerID, object nominalIssuer, object recoveryIssuer, object standardSpread,
    double inputSpread, object cpnLastSettle, string cpnPeriod, string cpnConvention,
    string pricingCurrency, double fxCorrel, double fxVol,
    object strikes, object correl, object betaAdder,
    double isAmericanFloatLegVal, double isAmericanFixedLegVal,
       double withGreeksVal, double[] HedgingCDS, double lossUnitAmount = 0.0, string integrationPeriod = "1m",
    DateTime[] cpnSchedule = null, double probMultiplier = 1, double dBeta = 0.1)
        {
            int i, j, k;

            double LossRate;
            double TrancheWidth;
            // MODIF QUANTO
            double CurrentTime;

            bool isAmericanFloatLeg = false;
            bool isAmericanFixedLeg = false;
            bool withGreeks = false;

            if (isAmericanFloatLegVal != 0)
            {
                isAmericanFloatLeg = true;
            }

            if (isAmericanFixedLegVal != 0)
            {
                isAmericanFixedLeg = true;
            }

            if (withGreeksVal != 0)
            {
                withGreeks = true;
            }


            int CurveID;
            DateTime ParamDate, StartTime;


            CurveID = StrippingIRS.GetCurveId(pricingCurrency);
            if (CurveID == -1)
            {
                if (StrippingIRS.InterestRateCurves.LastError == false)
                {
                    Console.WriteLine("Curve " + pricingCurrency + " was not stripped - Called from : ");
                    StrippingIRS.InterestRateCurves.LastError = true;
                }
                return null;
            }

            double[] ZC;
            string[] ZCDate;
            ParamDate = StrippingIRS.InterestRateCurves.Curves[CurveID].ParamDate;
            ZCDate = StrippingIRS.InterestRateCurves.Curves[CurveID].CurveDates;
            ZC = StrippingIRS.InterestRateCurves.Curves[CurveID].StrippedZC;

            if (string.IsNullOrEmpty(integrationPeriod))
            {
                integrationPeriod = "1m";
            }

            if (probMultiplier == 0)
            {
                withGreeks = false;
            }
            // Store the computation beginning time

            StartTime = DateTime.Now;


            // Is this a CDO or a CDS

            bool IsCDO;

            if (numberOfIssuer > 1)
            {
                IsCDO = true;
                LossRate = 1;
            }
            else
            {
                IsCDO = false;
                //LossRate = 1 - Convert.ToDouble(recoveryIssuer[0]);

                LossRate = 1 - (double)recoveryIssuer;
            }

            DateTime CDSRollDate;
            CDSRollDate = StrippingCDS.CreditDefaultSwapCurves.CDSRollDate;
            DateTime[] schedule;

            if (cpnSchedule == null || cpnSchedule.Length <= 0)
            {
                maturity = UtilityDates.ConvertDate(CDSRollDate, maturity);
                schedule = UtilityDates.SwapSchedule(ParamDate, maturity + "", cpnLastSettle + "", cpnPeriod, cpnConvention);

            }
            else
            {
                schedule = cpnSchedule;
            }


            int NumberOfDates = schedule.Length-1;

            DateTime[] ScheduleIntermed;
            int CouponDateCounter;
            DateTime PreviousCouponDate;
            DateTime NextCouponDate;
            int PreviousNumberOfIntegrationDates;

            int NumberOfIntegrationDates = 0;
            double[] NumberofIntegrationDateOnCouponDate = null;

            DateTime[] ScheduleIntegration = null;
            int IntegrationDateCounter;

            Array.Resize(ref ScheduleIntegration, NumberOfIntegrationDates + 1);
            Array.Resize(ref NumberofIntegrationDateOnCouponDate, NumberOfDates + 1);

            ScheduleIntegration[0] = schedule[0];
            NumberofIntegrationDateOnCouponDate[0] = 0;

            int index;
            for (index = 1; index <= NumberOfDates; index++) // 
            {
                CouponDateCounter = index;
                PreviousCouponDate = schedule[CouponDateCounter - 1];
                NextCouponDate = schedule[CouponDateCounter];
                ScheduleIntermed = UtilityDates.SwapSchedule(PreviousCouponDate, NextCouponDate + "", PreviousCouponDate + "", integrationPeriod, "ShortFirst");
                PreviousNumberOfIntegrationDates = NumberOfIntegrationDates;
                NumberOfIntegrationDates = PreviousNumberOfIntegrationDates + ScheduleIntermed.GetUpperBound(0);
                NumberofIntegrationDateOnCouponDate[CouponDateCounter] = NumberOfIntegrationDates;


                Array.Resize(ref ScheduleIntegration, NumberOfIntegrationDates + 1);
                for (IntegrationDateCounter = 0; IntegrationDateCounter < ScheduleIntermed.Length; IntegrationDateCounter++)
                {
                    ScheduleIntegration[PreviousNumberOfIntegrationDates + IntegrationDateCounter] = ScheduleIntermed[IntegrationDateCounter];
                }
            }

            NumberOfIntegrationDates = ScheduleIntegration.Length-1;


            //'
            //' Compute the risk free zc at schedule dates
            //'

            object CurrentDate;

            double[] RiskFreeZC = new double[(int)NumberOfIntegrationDates + 1];
            RiskFreeZC[0] = 1;
            for (i = 1; i <= NumberOfIntegrationDates; i++) // <=
            {
                CurrentDate = ScheduleIntegration[i];
                RiskFreeZC[i] = StrippingIRS.VbaGetRiskFreeZC(ParamDate, CurrentDate + "", ZC, ZCDate);
            }

            object[] EuropeanLow = null;
            object[] EuropeanHigh = null;
            double[,] European;
            double[,] dProb = null;

            if (IsCDO)
            {

                EuropeanLow = new object[(int)NumberOfIntegrationDates];
                EuropeanHigh = new object[(int)NumberOfIntegrationDates];
                TrancheWidth = ((double[])strikes)[1] - ((double[])strikes)[0];
            }
            else
            {
                TrancheWidth = 1;
            }




            if (withGreeks)
            {
                European = new double[(int)NumberOfIntegrationDates, 2 * numberOfIssuer + 1 ]; // 1 //European = new double[(int)NumberOfIntegrationDates, 2 * numberOfIssuer + 1 + 1];
                dProb = new double[(int)NumberOfIntegrationDates, numberOfIssuer]; // 1 //dProb = new double[(int)NumberOfIntegrationDates, numberOfIssuer + 1];
            }
            else
            {
                European = new double[(int)NumberOfIntegrationDates, 1 ]; // 1 //European = new double[(int)NumberOfIntegrationDates, 1 + 1];
            }



            object CDSListID;// = new object[0];


            if (IssuerID == null)
            {
                CDSListID = 1;
            }
            else
            {
                //CDSListID = (object[])IssuerID;
                CDSListID = IssuerID;
            }


            //'
            //' Dimension the ouput array depending on whether greeks are requested or not
            //'noml

            string[,] x;
            if (!withGreeks)
            {
                x = new string[6, 2];
            }
            else
            {
                x = new string[6 + numberOfIssuer + 1, 6];
                x[6, 0] = "dPV"; // dPV/dCDS_PV
                x[6, 1] = "dHedge";
                x[6, 2] = "delta not. (Hedge Crncy)";
                x[6, 3] = "delta not. (Product CCY)";
                x[6, 4] = "dPV(dBeta)";
                x[6, 5] = "Name";
                for (i = 0; i <= 5; i++)
                {
                    for (j = 2; j <= 5; j++)
                    {
                        x[i, j] = 0 + "";
                    }
                }
                if (IsCDO)
                {
                    x[5, 2] = "Leverage=";
                }
            }

            //'
            //' Compute the series of European CDO
            //'
            //' Only at maturity if both leg are not American.
            //' For each date of the schedule otherwise

            //NumberOfIntegrationDates -= 1; // add new // MODIF, ORIGINAL : la ligne est active


            double LastDateWhenEuroCDONeeded;
            if (isAmericanFloatLeg || isAmericanFixedLeg)
            {
                LastDateWhenEuroCDONeeded = 1;
            }
            else
            {
                LastDateWhenEuroCDONeeded = NumberOfIntegrationDates;
            }

            int cdsID;
            CDSCurve ThisCDS;

            double CurrentZC;
            double[] DefaultProbability = new double[numberOfIssuer ]; 

            for (int g = (int)NumberOfIntegrationDates; g >= LastDateWhenEuroCDONeeded; g--) //NumberOfIntegrationDates
            {
                //i = g - 1;
                i = g;
                CurrentDate = ScheduleIntegration[i];
                if ((DateTime)CurrentDate <= ParamDate)
                {
                    European[i, 1] = 0;
                    if (withGreeks)
                    {
                        for (j = 1; j <= numberOfIssuer; j++)
                        {
                            European[i-1, j] = 0;
                            European[i-1, numberOfIssuer + j] = 0; 
                        }
                    }
                }
                else
                {

                    CurrentZC = RiskFreeZC[i];
                    // modif QUANTO
                    CurrentTime = UtilityDates.DurationYear((DateTime)CurrentDate, ParamDate);

                    string IssuerCurrency;
                    // end
                    if (IsCDO)
                    {
                        for (j = 1; j <= numberOfIssuer; j++)
                        {
                            cdsID = ((int[])CDSListID)[j-1];
                            ThisCDS = StrippingCDS.CreditDefaultSwapCurves.Curves[cdsID];
                            IssuerCurrency = ThisCDS.Currency;
                            DefaultProbability[j-1] = StrippingCDS.GetDefaultProbabilityQuanto(cdsID, ParamDate, CurrentDate + "", pricingCurrency, 0, fxCorrel, fxVol, CurrentTime, probMultiplier);

                            // If DefaultProbability(j) = "Error Def Prob" Then
                            if (!UtilityDates.IsNumeric(DefaultProbability[j-1]))
                            {
                                return null;
                            }
                            if (withGreeks)
                            {
                                dProb[i-1, j-1] = StrippingCDS.GetDefaultProbabilityQuanto(cdsID, ParamDate, CurrentDate + "", pricingCurrency, 1, fxCorrel, fxVol, CurrentTime, probMultiplier) - DefaultProbability[j-1];
                            }
                        }
                    }
                    else
                    {
                        // modif QUANTO c
                        IssuerCurrency = StrippingCDS.CreditDefaultSwapCurves.Curves[(int)CDSListID].Currency;

                        DefaultProbability[0] = StrippingCDS.GetDefaultProbabilityQuanto((int)CDSListID, ParamDate, CurrentDate + "", pricingCurrency, 0, fxCorrel, fxVol, CurrentTime, probMultiplier);

                        if (withGreeks)
                        {
                            dProb[i-1, 0] = StrippingCDS.GetDefaultProbabilityQuanto((int)CDSListID, ParamDate, CurrentDate + "", pricingCurrency, 1, fxCorrel, fxVol, CurrentTime, probMultiplier) - DefaultProbability[0];
                        }
                    }

                    if (IsCDO)
                    {
                        double[] test0 = { ((double[])strikes)[0] };
                        double[] test1 = { ((double[])strikes)[1] };
                        EuropeanLow[i-1] = CDOModel.EuropeanCDOLossUnit(numberOfIssuer, lossUnitAmount, test0, DefaultProbability, ((double[])correl)[0], (double[])betaAdder, CurrentZC, (double[])nominalIssuer, (double[])recoveryIssuer, withGreeks, dBeta);
                        EuropeanHigh[i-1] = CDOModel.EuropeanCDOLossUnit(numberOfIssuer, lossUnitAmount, test1, DefaultProbability, ((double[])correl)[1], (double[])betaAdder, CurrentZC, (double[])nominalIssuer, (double[])recoveryIssuer, withGreeks, dBeta);
                        European[i-1, 0] = (double)((object[,])EuropeanHigh[i - 1])[0, 1] - (double)((object[,])EuropeanLow[i - 1])[0, 1];
                        if (withGreeks)
                        {
                            for (j = 1; j <= numberOfIssuer; j++)
                            {
                                European[i-1, j] = European[i-1, 0] + ((double)((object[,])EuropeanHigh[i - 1])[j, 1]- (double)((object[,])EuropeanLow[i - 1])[j, 1]) * dProb[i-1,j-1];
                                European[i-1, numberOfIssuer + j] = European[i-1, 0] + (double)((object[,])EuropeanHigh[i - 1])[j+numberOfIssuer, 1]- (double)((object[,])EuropeanLow[i - 1])[j+numberOfIssuer, 1];
                            }
                        }
                    }

                    else
                    {
                        CurrentZC = RiskFreeZC[i] * LossRate;
                        European[i-1, 0] = DefaultProbability[0] * CurrentZC;
                        if (withGreeks)
                        {
                            // compute dCDS
                            European[i-1, 1] = European[i-1, 0] + CurrentZC * dProb[i-1, 0];
                            European[i-1, 2] = 0;
                        }
                    }

                }
            }


            // -----------------------------------------------------------------------
            // FLOAT LEG
            // -----------------------------------------------------------------------
            // Compute the First term of the float leg. i.e. the European CDS/CDO at maturity

            x[1, 0] = European[(int)NumberOfIntegrationDates-1, 0] + "";

            if (withGreeks)
            {
                for (j = 1; j <= numberOfIssuer; j++)
                {
                    // store of the variation of the european tranche protection
                    x[6 + j, 0] = European[(int)NumberOfIntegrationDates-1,  j] + "";
                    x[6 + j, 4] = European[(int)NumberOfIntegrationDates-1,  numberOfIssuer + j] + "";
                }
            }

            // If American Float leg then compute the integration of other terms
            if (isAmericanFloatLeg)
            {
                for (i = 1; i <= NumberOfIntegrationDates; i++)
                {
                    // Adjust the american float leg
                    if (ScheduleIntegration[i] <= ParamDate)
                    {
                        // nothing to do
                    }
                    else
                    {
                        double Financing = (1 - RiskFreeZC[i] / (double)RiskFreeZC[i - 1]);
                        x[1, 0] = (Double.Parse(x[1, 0]) + European[i-1, 0] * Financing) + "";
                        if (withGreeks)
                        {
                            for (j = 1; j <= numberOfIssuer; j++)
                            {
                                // store of the variation of the european tranche protection
                                x[6 + j, 0] = (Double.Parse(x[6 + j, 0]) + European[i-1,  j] * Financing) + "";
                                x[6 + j, 4] = (Double.Parse(x[6 + j, 4]) + European[i-1,  numberOfIssuer + j] * Financing) + "";
                            }
                        }
                    }
                }
            }


            // -----------------------------------------------------------------------
            // FIXED LEG
            //
            // First compute the value of risky basis point (BPV: Basis Point Value)
            // -----------------------------------------------------------------------
            //
            // If the Spread is missing in the input, then force it to 100% for now so as to force greeks computation
            //

            double InputSpread = inputSpread;
            double Spread;
            if (double.IsNaN(InputSpread) || InputSpread == 0.0)
            {
                Spread = 1.0;
            }
            else
            {
                Spread = InputSpread;
            }

            // Need to compute the change of BPV only if american fixed leg, and if spread <> 0
            int Lastj = 1;
            // bool IsAmericanfixedleg = false;

            if (withGreeks && isAmericanFixedLeg && Spread != 0)
            {
                if (IsCDO)
                {
                    Lastj += numberOfIssuer * 2;
                }
                else
                {
                    Lastj += numberOfIssuer;
                }
            }

            double[] bpv = new double[Lastj];
            double this_bpv;

            for (j = 1; j <= Lastj; j++)
            {
                // Initialization
                bpv[j-1] = 0;
                double PreviousProbNoDef = 1;

                // compute sum of npv of 1 bp
                for (i = 1; i < NumberOfDates; i++)
                {
                    if (schedule[i] <= ParamDate)
                    {
                        // nothing to do
                    }
                    else
                    {
                        if (IsCDO)
                        {
                            // For a CDO Only
                            // Coupon is calculated up to Credit Event Date
                            // and is paid on Coupon Payment Date
                            for (k = (int)NumberofIntegrationDateOnCouponDate[i - 1] + 1; k <= NumberofIntegrationDateOnCouponDate[i]; k++)
                            {
                                this_bpv = (ScheduleIntegration[k] - ScheduleIntegration[k - 1]).Days / 360.0 * (double)RiskFreeZC[(int)NumberofIntegrationDateOnCouponDate[i]];

                                // Risky Coupon if american leg
                                if (isAmericanFixedLeg)
                                {
                                    double NextProbNoDef = (1.0 - European[k-1, j-1] / (double)RiskFreeZC[k] / (double)TrancheWidth / (double)LossRate);
                                    this_bpv = this_bpv * (NextProbNoDef + 0.5 * PreviousProbNoDef * (1.0 - NextProbNoDef / (double)PreviousProbNoDef));
                                    PreviousProbNoDef = NextProbNoDef;
                                }

                                // Add all the coupon payment
                                bpv[j-1] += this_bpv;
                            }
                        }
                        else
                        {
                            // For a CDS Only
                            // Coupon is calculated up to Credit Event Date
                            // and is paid on Credit Event Date

                            this_bpv = (schedule[i] - schedule[i - 1]).Days / 360.0 * RiskFreeZC[(int)NumberofIntegrationDateOnCouponDate[i]];

                            // reduction of bpv due to Credit Event in case of american fixed leg
                            double DefaultDayCountFraction;
                            if (isAmericanFixedLeg)
                            {
                                this_bpv = this_bpv * (1.0 - European[(int)NumberofIntegrationDateOnCouponDate[i]-1, j-1] / (double)RiskFreeZC[(int)NumberofIntegrationDateOnCouponDate[i]] / (double)TrancheWidth / (double)LossRate);

                                for (k = (int)NumberofIntegrationDateOnCouponDate[i - 1] + 1; k <= NumberofIntegrationDateOnCouponDate[i]; k++)
                                {
                                    DateTime Date1 = ScheduleIntegration[k - 1];
                                    DateTime Date2 = ScheduleIntegration[k];


                                    // Default is assumed to occur at mid integration period
                                    DateTime dTmp = new DateTime((long)((Date2.Ticks + Date1.Ticks) / 2.0));

                                    DefaultDayCountFraction = (dTmp - schedule[i - 1]).Days / 360.0;
                                    double NextProbNoDef = (1.0 - European[k-1, j-1] / (double)RiskFreeZC[k] / (double)TrancheWidth / (double)LossRate);
                                    double Accrued_bpv = DefaultDayCountFraction * (-NextProbNoDef + PreviousProbNoDef) * Math.Sqrt(RiskFreeZC[k] * RiskFreeZC[k - 1]);
                                    this_bpv += Accrued_bpv;
                                    PreviousProbNoDef = NextProbNoDef;
                                }
                            }

                            // Add all the coupon payment
                            bpv[j-1] += this_bpv;
                        }
                    }
                }
            }

            // Store the basis point value
            x[4, 0] = bpv[0] + "";

            // Compute the ATMSpread
            x[3, 0] = "" + (Double.Parse(x[1, 0]) / (double)TrancheWidth / Double.Parse(x[4, 0]));
            if (double.IsNaN(InputSpread) || InputSpread == 0.0)
            {
                Spread = Double.Parse(x[3, 0]);
            }

            // Store the NPV of the fixed leg
            x[2, 0] = ""  + Double.Parse(x[4, 0]) * TrancheWidth * Spread;

            if (!IsCDO)
            {
                x[2,0]=""+(Double)nominalIssuer* Double.Parse(x[4, 0]) * TrancheWidth * Spread;
            }

            double Leverage = 0;
            //object[] HedgingCDS = null;
            if (withGreeks)
            {
                for (i = 1; i <= numberOfIssuer; i++)
                {
                    // Change of float leg

                    x[6 + i, 0] = (Double.Parse(x[6 + i, 0]) - Double.Parse(x[1, 0])) + "";
                    x[6 + i, 4] = (Double.Parse(x[6 + i, 4]) - Double.Parse(x[1, 0])) + "";

                    // Change of fixed leg
                    if (Spread != 0 && isAmericanFixedLeg)
                    {
                        x[6 + i, 0] = (Double.Parse(x[6 + i, 0]) - Spread * TrancheWidth * (bpv[i ] - bpv[0])) + "";
                        if (IsCDO)
                        {
                            x[6 + i, 4] = (Double.Parse(x[6 + i, 4]) - Spread * TrancheWidth * (bpv[i + numberOfIssuer ] - bpv[0])) + "";
                        }
                    }



                    if (HedgingCDS != null)
                    {
                        j = (IsCDO) ? ((int[])CDSListID)[i-1] : (int)CDSListID;
                        ThisCDS = CreditDefaultSwapCurves.Curves[j];

                        string[,] hedging_cds;
                        double val1;
                        if (!IsCDO)
                        {
                            val1 = HedgingCDS[0];
                        }
                        else
                        {
                            val1 = ((double[])standardSpread)[i - 1]/10000;
                        }
                        double val2 = HedgingCDS[1];
                        double val3 = HedgingCDS[2];

                        if (IsCDO)
                        {
                            hedging_cds = AmericanSwap(maturity, 1, j, 1.0, ThisCDS.Recovery,
                                                        0,val1,cpnLastSettle, cpnPeriod,
                                                        cpnConvention, CreditDefaultSwapCurves.Curves[j].Currency, 0.0, 0.0, 0.0, 0.0,
                                                       betaAdder, val2, val3, withGreeksVal, null, lossUnitAmount,
                                                        integrationPeriod, schedule, probMultiplier);
                        }
                        else
                        {
                            hedging_cds = AmericanSwap(maturity, 1, j, 1.0, ThisCDS.Recovery,0,
                                           val1, cpnLastSettle, cpnPeriod,
                                           cpnConvention, CreditDefaultSwapCurves.Curves[j].Currency, 0.0, 0.0, 0.0, 0.0,
                                            betaAdder, val2, val3, withGreeksVal, null, 1.0,
                                           integrationPeriod, schedule, probMultiplier);

                        }


                        x[6 + i, 1] = hedging_cds[7, 0];

                        // Hedge in CD0 currency (delta CDO is in CDO currency unit) 
                        x[6 + i, 3] = "" + (Double.Parse(x[6 + i, 0]) / Double.Parse(hedging_cds[7, 0]));

                        // Hedge in CDS currency (delta CDO is in CDO currency unit => it has to be converted)

                        x[6 + i, 2] = "" + ((Double.Parse(x[6 + i, 3]) / (double)StrippingIRS.GetFXSpot(pricingCurrency)) * StrippingIRS.GetFXSpot(CreditDefaultSwapCurves.Curves[j].Currency));
                        x[6 + i, 5] = CreditDefaultSwapCurves.Curves[j].CDSName;

                        /////////////////::::addd
                        if (!IsCDO)
                        {
                            x[4, 0] = (double)nominalIssuer * Double.Parse(x[4, 0]) + "";

                            for (i = 1; i <= numberOfIssuer; i++)
                            {
                                x[6 + i, 0] = (double)nominalIssuer * Double.Parse(x[6 + i, 0]) + "";
                                x[6 + i, 1] = (double)nominalIssuer * Double.Parse(x[6 + i, 1]) + "";
                                x[6 + i, 2] = (double)nominalIssuer * Double.Parse(x[6 + i, 2]) + "";
                                x[6 + i, 3] = (double)nominalIssuer * Double.Parse(x[6 + i, 3]) + "";
                                x[6 + i, 4] = (double)nominalIssuer * Double.Parse(x[6 + i, 4]) + "";
                                x[6 + i, 5] = x[6 + i, 5];

                            }
                        }



                        if (IsCDO)
                        {
                            Leverage += Double.Parse(x[6 + i, 3]);
                        }
                    }
                }
                if (IsCDO)
                {
                    Leverage /= (double)TrancheWidth;
                    x[5, 3] = "" + Leverage;
                }
            }
            // add
            // Store the NPV of the floated leg
            if (!IsCDO)
            {
                x[1, 0] = "" + (Double)nominalIssuer * (Double.Parse(x[1, 0]));
            }

            // Store the NPV of the CDS/CDO (dirty, i.e. inclusive of next coupon)
            x[0, 0] = "" + (Double.Parse(x[1, 0]) - Double.Parse(x[2, 0]));
            for (i = 0; i <= 2; i++)//MODIF AJOUT
            {
                x[i, 1] = Double.Parse(x[i, 0]) / TrancheWidth + "";
            }
            for (i = 3; i <= 4; i++)
            {
                x[i, 1] = x[i, 0];
            }
            // Computation time
            x[5, 0] = (DateTime.Now - StartTime) + "";
            x[5, 1] = (DateTime.Now - StartTime) + "";//MODIF, AJOUT


            return x;
        }
    }
}
