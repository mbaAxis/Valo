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

        public static object CDO(string maturity, double[] strikes, double[] correl, string pricingCurrency,
    int numberOfIssuer, string[] issuerList, double[] nominalIssuer, double spread, string cpnPeriod,
    string cpnConvention, string cpnLastSettle, double fxCorrel, double fxVol, double[] betaAdder,
    double[] recoveryIssuer = null, bool isAmericanFloatLeg = false, bool isAmericanFixedLeg = false,
    bool withGreeks = false, string hedgingCDS = null, double? lossUnitAmount = null,
    string integrationPeriod = "1m", double probMultiplier = 1, double dBeta = 0.1)
        {
            int i;
            double[] recoveryRate;

            int curveId;
            object vbaIssuerList;

            curveId = StrippingIRS.GetCurveId(pricingCurrency);
            vbaIssuerList = new string[issuerList.Length];

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
                return $"CDO Pricing: Not enough Issuers specified compared to the indicated number of issuer - Called from: {Environment.StackTrace}";
            }
            else if (issuerList == null)
            {
                return $"CDO Pricing: No Issuers specified - Called from: {Environment.StackTrace}";
            }
            else
            {
                for (i = 0; i < numberOfIssuer; i++)
                {
                    if (issuerList.Length <= i)
                    {
                        return $"CDO Pricing: No Issuers specified in position {i} - Called from: {Environment.StackTrace}";
                    }
                    else
                    {
                        if (!UtilityDates.IsNumeric(issuerList[i].ToString()))
                        {
                            ((int[])vbaIssuerList)[i] = StrippingCDS.GetCDSCurveId(issuerList[i].ToString());
                        }
                        else
                        {
                            ((string[])vbaIssuerList)[i] = issuerList[i];
                        }

                        if (((double[])vbaIssuerList)[i] == -1)
                        {
                            return $"CDO Pricing: Position {i}: IssuerID {issuerList[i]} not recognized - Called from: {Environment.StackTrace}";
                        }

                        if (((int[])vbaIssuerList)[i] > StrippingCDS.CreditDefaultSwapCurves.NumberOfCurves)
                        {
                            return $"CDO Pricing: Position {i}: IssuerID ({issuerList[i]}) exceeds range of defined issuer - Called from: {Environment.StackTrace}";
                        }
                        else if (!StrippingCDS.CreditDefaultSwapCurves.Curves[((int[])vbaIssuerList)[i]].CDSdone)
                        {
                            return $"CDO Pricing: Position {i}: IssuerID ({issuerList[i]}) CDS curve not stripped - Called from: {Environment.StackTrace}";
                        }
                    }
                }
            }

            if (recoveryIssuer == null || recoveryIssuer.Length == 0)
            {
                recoveryRate = new double[(int)numberOfIssuer];
                for (i = 0; i < numberOfIssuer; i++)
                {
                    recoveryRate[i] = CreditDefaultSwapCurves.Curves[((int[])vbaIssuerList)[i]].Recovery;
                }
            }
            else
            {
                recoveryRate = new double[numberOfIssuer];
                for (i = 0; i < numberOfIssuer; i++)
                {
                    if (recoveryIssuer == null || recoveryIssuer.Length == 0)
                        {
                        recoveryRate[i] = CreditDefaultSwapCurves.Curves[((int[])vbaIssuerList)[i]].Recovery;
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

            if (betaAdder == null || betaAdder.Length ==0)
            {
                betaAdder = new double[(int)numberOfIssuer];
                for (i = 0; i < numberOfIssuer; i++)
                {
                    betaAdder[i] = 0;
                }
            }

            return AmericanSwap(maturity,
                numberOfIssuer, vbaIssuerList, nominalIssuer, recoveryRate,
                spread, cpnLastSettle, cpnPeriod, cpnConvention,
                pricingCurrency, fxCorrel, fxVol,
                strikes, correl, betaAdder,
                isAmericanFloatLeg, isAmericanFixedLeg,
                withGreeks, hedgingCDS, (double)lossUnitAmount, integrationPeriod, null, probMultiplier, dBeta);
        }

        public static object CDS(object issuerIdParam, string maturity, double spread, double recoveryRate,
        string cpnPeriod, string cpnConvention, string cpnLastSettle, string pricingCurrency = null,
        double fxCorrel = 0, double fxVol = 0, bool isAmericanFloatLeg = false, bool isAmericanFixedLeg = false,
        bool withGreeks = false, object hedgingCds = null, string integrationPeriod = "1m", double probMultiplier = 1)
        {
            //DateTime paramDate = new DateTime(2024, 03, 03);
            //DateTime CDSRollDate = StrippingCDS.CDSRefDate(paramDate);
            //int cdsID = 1;
            //string CDSName = CreditDefaultSwapCurves.Curves[cdsID].CDSName; 

            //string[] CurveDates = CreditDefaultSwapCurves.Curves[cdsID].CurveDates;
            //double[] CDSCurve = CreditDefaultSwapCurves.Curves[cdsID].CDSSpread;
            //double[] StrippedDP = CreditDefaultSwapCurves.Curves[cdsID].StrippedDPandShocked;
            //double [,] MonthlyDP = CreditDefaultSwapCurves.Curves[cdsID].MonthlyDPandShocked;

            //if (StoreDP(paramDate, cdsID,CDSName, recoveryRate,pricingCurrency, CurveDates, CDSCurve, StrippedDP, CDSRollDate, MonthlyDP))
            //{
            //    Console.WriteLine("iiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiitrue");
            //}

            int issuerId;

            if (!Utils.IsNumeric(issuerIdParam))
            {
                issuerId = StrippingCDS.GetCDSCurveId((string) issuerIdParam) - 1;
            }
            else
            {
                issuerId = (int) issuerIdParam - 1;
            }
            

            Console.WriteLine("okkkkkk11 issuerId = " + issuerId);
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
                pricingCurrency, fxCorrel, fxVol, 0.0, 0.0, 0.0, isAmericanFloatLeg, isAmericanFixedLeg, withGreeks, hedgingCds, 1,
                integrationPeriod,null, probMultiplier);
        }

        public static object AmericanSwap(object maturity, int numberOfIssuer, object IssuerID, object nominalIssuer, object recoveryIssuer,
    double inputSpread, object cpnLastSettle, string cpnPeriod, string cpnConvention,
    string pricingCurrency, double fxCorrel, double fxVol,
    object strikes, object correl, object betaAdder,
    bool isAmericanFloatLeg = false, bool isAmericanFixedLeg = false,
    bool withGreeks = false, object HedgingCDS = null,
    double lossUnitAmount = 0.0, string integrationPeriod = "1m",
    DateTime[] cpnSchedule = null, double probMultiplier = 1, double dBeta = 0.1)
        {
            Console.WriteLine("Commencons fffff =========================================================================================");
            int i, j, k;

            double LossRate;
            double TrancheWidth;
            // MODIF QUANTO
            double CurrentTime;
           

            int CurveID;
            DateTime ParamDate, StartTime;

            Console.WriteLine("Commencons HHHHHHHHHH =========================================================================================");

            CurveID = StrippingIRS.GetCurveId(pricingCurrency);
            Console.WriteLine("11111111 ========================================================================================= CurveID = "+ CurveID);
            if (CurveID == -1)
            {
                if (StrippingIRS.InterestRateCurves.LastError == false)
                {
                    Console.WriteLine("Curve " + pricingCurrency + " was not stripped - Called from : ");
                    StrippingIRS.InterestRateCurves.LastError = true;
                }
                return null;
            }
            Console.WriteLine("2222222 ========================================================================================= CurveID = " + CurveID);

            double[] ZC;
            string[] ZCDate;
            ParamDate = StrippingIRS.InterestRateCurves.Curves[CurveID].ParamDate;
            ZCDate = StrippingIRS.InterestRateCurves.Curves[CurveID].CurveDates;
            ZC = StrippingIRS.InterestRateCurves.Curves[CurveID].StrippedZC;

            Console.WriteLine("3333333 ========================================================================================= CurveID = " + CurveID);

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

            Console.WriteLine("44444444 ========================================================================================= CurveID = " + CurveID);

            // Is this a CDO or a CDS

            bool IsCDO;
            Console.WriteLine("numberOfIssueré" + numberOfIssuer);

            if (numberOfIssuer > 1)
            {
                Console.WriteLine("ZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZ");

                IsCDO = true;
                LossRate = 1;
            }
            else
            {
                IsCDO = false;
                //LossRate = 1 - Convert.ToDouble(recoveryIssuer[0]);

                LossRate = 1 - (double)recoveryIssuer;
            }

            Console.WriteLine("5555555 ========================================================================================= CurveID = " + CurveID);

            DateTime CDSRollDate;
            CDSRollDate = StrippingCDS.CreditDefaultSwapCurves.CDSRollDate;
            DateTime[]schedule;

            if (cpnSchedule == null)
            {
                maturity = UtilityDates.ConvertDate(CDSRollDate, (string)maturity);
                if (cpnLastSettle != null && cpnLastSettle != "")
                {
                    cpnLastSettle = (DateTime)cpnLastSettle;
                }

                Console.WriteLine("OOOOOOOOOOOOOOOO ========================================================================================= cpnPeriod = ");
                schedule = UtilityDates.SwapSchedule(ParamDate, maturity+"", cpnLastSettle, cpnPeriod, cpnConvention);
            }
            else
            {
                schedule = cpnSchedule;
            }

            Console.WriteLine("+++++++++++ ========================================================================================= cpnPeriod = ");

            int NumberOfDates = schedule.Length;

            DateTime[] ScheduleIntermed;
            int CouponDateCounter;
            DateTime PreviousCouponDate;
            object NextCouponDate;
            double PreviousNumberOfIntegrationDates;

            double NumberOfIntegrationDates = 0;
            double[] NumberofIntegrationDateOnCouponDate = null;

            DateTime[] ScheduleIntegration = null;
            double IntegrationDateCounter;
           
            Array.Resize(ref ScheduleIntegration, (int)NumberOfIntegrationDates + 1);
            Array.Resize(ref NumberofIntegrationDateOnCouponDate, NumberOfDates + 1);

            ScheduleIntegration[0] = schedule[0];
            NumberofIntegrationDateOnCouponDate[0] = 0;

            Console.WriteLine("KKKKKKKKKKK ========================================================================================= " );

            for (CouponDateCounter = 1; CouponDateCounter < NumberOfDates; CouponDateCounter++) // <
            {
                PreviousCouponDate = (DateTime)schedule[CouponDateCounter - 1];
                NextCouponDate = (DateTime)schedule[CouponDateCounter];
                ScheduleIntermed = UtilityDates.SwapSchedule(PreviousCouponDate, NextCouponDate+"", PreviousCouponDate+"", integrationPeriod, "ShortFirst");
                PreviousNumberOfIntegrationDates = NumberOfIntegrationDates;
                NumberOfIntegrationDates = (int)PreviousNumberOfIntegrationDates + ScheduleIntermed.Length;
                NumberofIntegrationDateOnCouponDate[(int)CouponDateCounter] = NumberOfIntegrationDates;
                Array.Resize(ref ScheduleIntegration, (int) NumberOfIntegrationDates + 1);
                for (IntegrationDateCounter = 0; IntegrationDateCounter < ScheduleIntermed.Length; IntegrationDateCounter++)
                {
                    ScheduleIntegration[(int)PreviousNumberOfIntegrationDates + (int)IntegrationDateCounter] = ScheduleIntermed[(int)IntegrationDateCounter];
                }
            }

            NumberOfIntegrationDates = ScheduleIntegration.Length;

            Console.WriteLine("NumberOfIntegrationDates ========================================================================================= " + NumberOfIntegrationDates);

            //'
            //' Compute the risk free zc at schedule dates
            //'

            object CurrentDate;

            double[] RiskFreeZC = new double[(int)NumberOfIntegrationDates + 1];
            RiskFreeZC[0] = 1;
            for (i = 1; i < NumberOfIntegrationDates; i++) // <=
            {
                CurrentDate = ScheduleIntegration[i];
                RiskFreeZC[i] = StrippingIRS.VbaGetRiskFreeZC(ParamDate, CurrentDate+"", ZC, ZCDate);
            }

            object[] EuropeanLow = null;
            object[] EuropeanHigh = null;
            double[,] European;
            double[,] dProb = null;

            Console.WriteLine("IsCDO " + IsCDO);

            if (IsCDO)
            {

                EuropeanLow = new object[(int)NumberOfIntegrationDates];
                EuropeanHigh = new object[(int)NumberOfIntegrationDates];
                Console.WriteLine("strike =========" + strikes);
                TrancheWidth = ((double[])strikes)[1] - ((double[])strikes)[0];
            }
            else
            {
                TrancheWidth = 1;
            }

           
            

            if (withGreeks)
            {
                European = new double[(int)NumberOfIntegrationDates, 2 * numberOfIssuer ];
                dProb = new double[(int)NumberOfIntegrationDates, numberOfIssuer];
            }
            else
            {
                European = new double[(int)NumberOfIntegrationDates, 1];
            }



            object CDSListID;// = new object[0];
            

            if (IssuerID == null)
            {
                CDSListID  = 1;
            }
            else
            {
                //CDSListID = (object[])IssuerID;
                CDSListID = IssuerID ;
            }

            Console.WriteLine("11111111111111111111111111111111111111111111111111111111111111111===========");

            //'
            //' Dimension the ouput array depending on whether greeks are requested or not
            //'

            object[,] x;
            if (!withGreeks)
            {
                x = new object[6, 2];
            }
            else
            {
                x = new object[6 + numberOfIssuer + 2, 6];
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
                        x[i, j] = 0;
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
            double[] DefaultProbability = new double[numberOfIssuer];


            for (int g = (int)NumberOfIntegrationDates; g >= LastDateWhenEuroCDONeeded; g--)
            {
                i = g - 1;
                Console.WriteLine("position g = " + i + " NumberOfIntegrationDates = " + NumberOfIntegrationDates);
                CurrentDate = ScheduleIntegration[i];
                if ((DateTime)CurrentDate <= ParamDate)
                {
                    Console.WriteLine("aaaaaaaaaaaaaaaaaaaaa = " + i);
                    European[i, 0] = 0;
                    if (withGreeks)
                    {
                        for (j = 0; j < numberOfIssuer; j++)
                        {
                            European[i, j + 1] = 0;
                            European[i, numberOfIssuer - 1 + j + 1] = 0; // -1
                        }
                    }
                    Console.WriteLine("ccccccccccccccccccccc = " + i);
                }
                else
                {
                    Console.WriteLine("dddddddddddddddddddddd = " + i);

                    CurrentZC = RiskFreeZC[i];
                    Console.WriteLine("eeeeeeeeeeeeeeeeeeeeeeeeeee = " + i);
                    // modif QUANTO
                    CurrentTime = UtilityDates.DurationYear((DateTime)CurrentDate, ParamDate);
                    Console.WriteLine("fffffffffffffffffffffffffffffffffffff = " + i);
                    string IssuerCurrency;
                    // end
                    if (IsCDO)
                    {
                        Console.WriteLine("ggggggggggggggggggggggggggggggggggg = " + i);
                        for (j = 0; j < numberOfIssuer; j++)
                        {
                            cdsID = ((int[])CDSListID)[j];
                            ThisCDS = StrippingCDS.CreditDefaultSwapCurves.Curves[cdsID];
                            IssuerCurrency = ThisCDS.Currency;
                            DefaultProbability[j] = StrippingCDS.GetDefaultProbabilityQuanto(cdsID, ParamDate, CurrentDate+"", pricingCurrency, 0, fxCorrel, fxVol, CurrentTime, probMultiplier);

                            // If DefaultProbability(j) = "Error Def Prob" Then
                            if (!UtilityDates.IsNumeric(DefaultProbability[j]))
                            {
                                return null;
                            }
                            if (withGreeks)
                            {
                                dProb[i,j] = StrippingCDS.GetDefaultProbabilityQuanto(cdsID, ParamDate, CurrentDate+"", pricingCurrency, 1, fxCorrel, fxVol, CurrentTime, probMultiplier) - DefaultProbability[j];
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("hhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhh = " + i);
                        // modif QUANTO
                        IssuerCurrency = StrippingCDS.CreditDefaultSwapCurves.Curves[(int)CDSListID].Currency;

                        DefaultProbability[0] = StrippingCDS.GetDefaultProbabilityQuanto((int)CDSListID, ParamDate, CurrentDate+"", pricingCurrency, 0, fxCorrel, fxVol, CurrentTime, probMultiplier);
                        
                        if (withGreeks)
                        {
                            Console.WriteLine("on entreeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee = ");

                            dProb[i, 0] = StrippingCDS.GetDefaultProbabilityQuanto((int)CDSListID, ParamDate, CurrentDate+"", pricingCurrency, 1, fxCorrel, fxVol, CurrentTime, probMultiplier) - DefaultProbability[0];
                        }
                    }

                    Console.WriteLine("inexxxxxxxxxxxxxx = ");

                    if (IsCDO)
                    {
                        EuropeanLow[i] = CDOModel.EuropeanCDOLossUnit(numberOfIssuer, lossUnitAmount, (double[])strikes, DefaultProbability, ((double[])correl)[0], (double[])betaAdder, CurrentZC, (double[])nominalIssuer, (double[])recoveryIssuer, withGreeks, dBeta);
                        EuropeanHigh[i] = CDOModel.EuropeanCDOLossUnit(numberOfIssuer, lossUnitAmount, (double[])strikes, DefaultProbability, ((double[])correl)[1], (double[])betaAdder, CurrentZC, (double[])nominalIssuer, (double[])recoveryIssuer, withGreeks, dBeta);
                        European[i, 0] = ((double[,])EuropeanHigh[i])[0,0] - ((double[,])EuropeanLow[i])[0, 0];
                        
                        if (withGreeks)
                        {
                            for (j = 0; j < numberOfIssuer; j++)
                            {
                                European[i, j + 1] = European[i, 1] + (((double[,])EuropeanHigh[i])[1 + j, 0] - ((double[,])EuropeanLow[i])[1 + j, 1]) * dProb[i, j];
                                European[i, numberOfIssuer + j + 1] = European[i, 1] + (((double[,])EuropeanHigh[i])[1 + j + numberOfIssuer, 1] - ((double[,])EuropeanLow[i])[1 + j + numberOfIssuer, 1]);
                            }
                        }
                    }

                    else
                    {
                        CurrentZC = RiskFreeZC[i] * LossRate;
                        European[i, 0] = DefaultProbability[0] * CurrentZC;
                        if (withGreeks)
                        {
                            // compute dCDS
                            European[i, 1] = European[i, 0] + CurrentZC * dProb[i, 0];
                            European[i, 2] = 0;
                        }
                    }

                    Console.WriteLine("huhuhuhhuhuhuh = " + i);
                }
            }


            Console.WriteLine("00000000000000000000000000000000000000000");
            // -----------------------------------------------------------------------
            // FLOAT LEG
            // -----------------------------------------------------------------------
            // Compute the First term of the float leg. i.e. the European CDS/CDO at maturity

            x[0, 0] = European[(int)NumberOfIntegrationDates, 1];
            if (withGreeks)
            {
                for (j = 0; j < numberOfIssuer; j++)
                {
                    // store of the variation of the european tranche protection
                    x[6 + j, 0] = European[(int)NumberOfIntegrationDates, 1 + j];
                    x[6 + j, 4] = European[(int)NumberOfIntegrationDates, 1 + numberOfIssuer + j];
                }
            }

            // If American Float leg then compute the integration of other terms
            if (isAmericanFloatLeg)
            {
                for (i = 0; i < NumberOfIntegrationDates; i++)
                {
                    // Adjust the american float leg
                    if (ScheduleIntegration[i] <= ParamDate)
                    {
                        // nothing to do
                    }
                    else
                    {
                        double Financing = (1 - RiskFreeZC[i] / (double) RiskFreeZC[i - 1]);
                        x[1, 0] = (double)x[1, 0] + European[i, 1] * Financing;
                        if (withGreeks)
                        {
                            for (j = 0; j < numberOfIssuer; j++)
                            {
                                // store of the variation of the european tranche protection
                                x[6 + j, 0] = (double)x[6 + j, 0]  + European[i, 1 + j] * Financing;
                                x[6 + j, 4] = (double)x[6 + j, 4] + European[i, 1 + numberOfIssuer + j] * Financing;
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

            double InputSpread = 1.0;
            double Spread;
            if (double.IsNaN(InputSpread))
            {
                Spread = 1.0;
            }
            else
            {
                Spread = InputSpread;
            }

            // Need to compute the change of BPV only if american fixed leg, and if spread <> 0
            int Lastj = 1;
            bool IsAmericanfixedleg = false;

            if (withGreeks && IsAmericanfixedleg && Spread != 0)
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

            for (j = 0; j < Lastj; j++)
            {
                // Initialization
                bpv[j] = 0;
                double PreviousProbNoDef = 1;

                // compute sum of npv of 1 bp
                for ( i = 0; i < NumberOfDates; i++)
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
    
                                this_bpv = (ScheduleIntegration[k] - ScheduleIntegration[k - 1]).TotalDays / 360.0 * (double) RiskFreeZC[(int)NumberofIntegrationDateOnCouponDate[i]];

                                // Risky Coupon if american leg
                                if (IsAmericanfixedleg)
                                {
                                    double NextProbNoDef = (1.0 - European[k, j] / (double) RiskFreeZC[k] / (double) TrancheWidth / (double) LossRate);
                                    this_bpv = this_bpv * (NextProbNoDef + 0.5 * PreviousProbNoDef * (1.0 - NextProbNoDef / (double) PreviousProbNoDef));
                                    PreviousProbNoDef = NextProbNoDef;
                                }

                                // Add all the coupon payment
                                bpv[j] += this_bpv;
                            }
                        }
                        else
                        {
                            // For a CDS Only
                            // Coupon is calculated up to Credit Event Date
                            // and is paid on Credit Event Date

                            this_bpv = (schedule[i] - schedule[i]).TotalDays / 360.0 * (double) RiskFreeZC[(int)NumberofIntegrationDateOnCouponDate[i]];

                            // reduction of bpv due to Credit Event in case of american fixed leg

                            double DefaultDayCountFraction;
                            if (IsAmericanfixedleg)
                            {
                                this_bpv = this_bpv * (1.0 - European[(int)NumberofIntegrationDateOnCouponDate[i], j] / (double) RiskFreeZC[(int)NumberofIntegrationDateOnCouponDate[i]] / (double) TrancheWidth / (double) LossRate);

                                for (k = (int)NumberofIntegrationDateOnCouponDate[i] ; k < NumberofIntegrationDateOnCouponDate[i]; k++)
                                {
                                    DateTime Date1 = ScheduleIntegration[k];
                                    DateTime Date2 = ScheduleIntegration[k];

                                    // Default is assumed to occur at mid integration period
                                    //DefaultDayCountFraction = (int)((Date2 + Date1).TotalDays / 2 - schedule[i - 1].TotalDays) / 360.0;

                                    DefaultDayCountFraction = (int)((Date2 - Date1).TotalDays / 2 + (double) Date1.Subtract(schedule[i]).TotalDays) / 360.0;

                                    double NextProbNoDef = (1.0 - European[k, j] / (double) RiskFreeZC[k] / (double) TrancheWidth / (double) LossRate);
                                    double Accrued_bpv = DefaultDayCountFraction * (-NextProbNoDef + PreviousProbNoDef) * Math.Sqrt(RiskFreeZC[k] * RiskFreeZC[k]);
                                    this_bpv += Accrued_bpv;
                                    PreviousProbNoDef = NextProbNoDef;
                                }
                            }

                            // Add all the coupon payment
                            bpv[j] += this_bpv;
                        }
                    }
                }
            }

            // Store the basis point value
            x[4, 0] = bpv[0];

            // Compute the ATMSpread
            x[3, 0] = (double)x[1, 0] / (double) TrancheWidth / (double)x[4, 0];
            if (double.IsNaN(InputSpread))
            {
                Spread = (double)x[3, 0];
            }

            // Store the NPV of the fixed leg
            x[2, 0] = (double)x[4, 0] * TrancheWidth * Spread;

            // Store the NPV of the CDS/CDO (dirty, i.e. inclusive of next coupon)
            x[0, 0] = (double)x[1, 0] - (double)x[2, 0];

            double Leverage = 0;
            //object[] HedgingCDS = null;
            if (withGreeks)
            {
                for (i = 0; i < numberOfIssuer; i++)
                {
                    // Change of float leg
                    x[6 + i, 0] = (double)x[6 + i, 0] - (double)x[1, 0];
                    x[6 + i, 4] = (double)x[6 + i, 4] - (double)x[1, 0];

                    // Change of fixed leg
                    if (Spread != 0 && IsAmericanfixedleg)
                    {
                        x[6 + i, 0] = (double)x[6 + i, 0] - Spread * TrancheWidth * (bpv[i] - bpv[0]);
                        if (IsCDO)
                        {
                            x[6 + i, 4] = (double)x[6 + i, 4] - Spread * TrancheWidth * (bpv[i + numberOfIssuer] - bpv[0]);
                        }
                    }

                    if (HedgingCDS != null)
                    {
                        j = (IsCDO) ? ((int[])CDSListID)[i] : ((int[])CDSListID)[0];
                        ThisCDS = CreditDefaultSwapCurves.Curves[j];

                        double[,] hedging_cds;
                        if (IsCDO)
                        {
                            hedging_cds = (double[,])AmericanSwap(maturity, 1, j, 1.0,ThisCDS.Recovery,
                                                        ((double[])HedgingCDS)[0], cpnLastSettle, cpnPeriod,
                                                        cpnConvention, CreditDefaultSwapCurves.Curves[j].Currency, 0.0, 0.0, 0.0, 0.0,
                                                       betaAdder, ((bool[])HedgingCDS)[1], ((bool[])HedgingCDS)[2], withGreeks, 0.0, lossUnitAmount,
                                                        integrationPeriod, schedule, probMultiplier);
                        }
                        else
                        {
                            hedging_cds = (double[,])AmericanSwap(maturity, 1, j, 1.0, ThisCDS.Recovery,
                                                        ((double[])HedgingCDS)[0], cpnLastSettle, cpnPeriod,
                                                        cpnConvention, CreditDefaultSwapCurves.Curves[j].Currency, 0.0, 0.0, 0.0, 0.0,
                                                         betaAdder, ((bool[])HedgingCDS)[1], ((bool[])HedgingCDS)[2], withGreeks, 0.0, 1.0,
                                                        integrationPeriod, schedule, probMultiplier);

                        }

                        x[6 + i, 1] = hedging_cds[7, 0];

                        // Hedge in CD0 currency (delta CDO is in CDO currency unit)
                        x[6 + i, 3] = (double)x[6 + i, 0] / hedging_cds[7, 0];

                        // Hedge in CDS currency (delta CDO is in CDO currency unit => it has to be converted)
                        x[6 + i, 2] = ((double)x[6 + i, 3] / (double) StrippingIRS.GetFXSpot(pricingCurrency)) * StrippingIRS.GetFXSpot(CreditDefaultSwapCurves.Curves[j].Currency);
                        x[6 + i, 5] = CreditDefaultSwapCurves.Curves[j].CDSName;

                        if (IsCDO)
                        {
                            Leverage += (double)x[6 + i, 3];
                        }
                    }
                    
                }

                if (IsCDO)
                {
                    Leverage /= (double) TrancheWidth;
                    x[5, 3] = Leverage;
                }
            }

            // Results in percentage of the tranche
            foreach (int ii in new int[] { 0, 1, 2 })
            {
                x[ii, 1] = (double)x[ii, 0] / (double) TrancheWidth;
            }

            foreach (int ii in new int[] { 3, 4 })
            {
                x[ii, 1] = x[ii, 0];
            }

            // Computation time
            x[5, 0] = DateTime.Now - StartTime;
            x[5, 1] = DateTime.Now - StartTime;

            return x;
        }
    }
}
