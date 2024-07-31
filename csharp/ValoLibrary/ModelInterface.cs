using MathNet.Numerics.Financial;
using Microsoft.Office.Core;
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
            if(lossRateUnit < 0.01)//Modification for Stochastic Recovery, if not, the lossRateUnit will be near 0 and lead to a theoretical infinite computation time
            {
                lossRateUnit = 0.01;
            }

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


        public static string[,] CDO(string maturity, double[] strikes, double[] correl, double[] spreadStandard, string pricingCurrency,
    int numberOfIssuer, string[] issuerList, double[] nominalIssuer, double spread, string cpnPeriod,
    string cpnConvention, string cpnLastSettle, double fxCorrel, double fxVol, double[] betaAdder,
    double[] recoveryIssuer = null, double isAmericanFloatLeg = 0, double isAmericanFixedLeg = 0,
    double withGreeks = 0, double withJtdVAL = 0, double withStochasticRecoveryVAL = 0, double[] hedgingCDS = null, double? lossUnitAmount = null,
    string integrationPeriod = "1m", double probMultiplier = 1, double dBeta = 0.1, int girrMonth = 0, string girrCurrency = null)
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
                withGreeks,withJtdVAL,withStochasticRecoveryVAL, hedgingCDS, (double)lossUnitAmount, integrationPeriod, null, probMultiplier, dBeta, girrMonth, girrCurrency);
        }
        public static string[,] CDS(string issuerIdParam, string maturity, double spread, double recoveryRate,double notional,
        string cpnPeriod, string cpnConvention, string cpnLastSettle, string pricingCurrency = null,
        double fxCorrel = 0, double fxVol = 0, double isAmericanFloatLeg = 0, double isAmericanFixedLeg = 0,
        double withGreeks = 0, double[] hedgingCds = null, string integrationPeriod = "1m", double probMultiplier = 1 , int girrMonth = 0 , string girrCurrency = null)
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

            if (double.IsNaN(recoveryRate)|| recoveryRate ==-1)
            {
                recoveryRate = StrippingCDS.CreditDefaultSwapCurves.Curves[Convert.ToInt32(issuerId)].Recovery;
            }

            return AmericanSwap(maturity, 1, issuerId, notional, recoveryRate, 0,spread, cpnLastSettle , cpnPeriod, cpnConvention,
                pricingCurrency, fxCorrel, fxVol, 0.0, 0.0, 0.0, isAmericanFloatLeg, isAmericanFixedLeg, withGreeks,0,0, hedgingCds, 1,
                integrationPeriod,null, probMultiplier,0.1,girrMonth, girrCurrency);
        }
        public static string[,] AmericanSwap(object maturity, int numberOfIssuer, object IssuerID, object nominalIssuer, object recoveryIssuer, object standardSpread,
    double inputSpread, object cpnLastSettle, string cpnPeriod, string cpnConvention,
    string pricingCurrency, double fxCorrel, double fxVol,
    object strikes, object correl, object betaAdder,
    double isAmericanFloatLegVal, double isAmericanFixedLegVal,
       double withGreeksVal, double withJtdVAl,double withStochasticRecoveryVAL, double[] HedgingCDS, double lossUnitAmount = 0.0, string integrationPeriod = "1m",
    DateTime[] cpnSchedule = null, double probMultiplier = 1, double dBeta = 0.1, int girrMonth = 0, string girrCurrency = null)
        {
            int i, j, k;

            double LossRate;
            double TrancheWidth;
            // MODIF QUANTO
            double CurrentTime;

            bool isAmericanFloatLeg = false;
            bool isAmericanFixedLeg = false;
            bool withGreeks = false;
            bool withJTD = false;
            bool withStochasticRecovery = false;

            if (withJtdVAl != 0)
            {
                withJTD = true;
            }
            if (withStochasticRecoveryVAL != 0)
            {
                withStochasticRecovery = true;
            }

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
                RiskFreeZC[i] = StrippingIRS.VbaGetRiskFreeZCVersion2(ParamDate, CurrentDate + "", ZC, ZCDate, false);
                if(i == girrMonth && String.Equals(pricingCurrency,girrCurrency))
                {
                    RiskFreeZC[i] = StrippingIRS.VbaGetRiskFreeZCVersion2(ParamDate, CurrentDate + "", ZC, ZCDate, true);
                }
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
            else if(IsCDO && withJTD)
            {
                x = new string[6 + numberOfIssuer + 1, 10];//Modification Jump-to-Default, columns are added
                x[6, 0] = "dPV"; // dPV/dCDS_PV
                x[6, 1] = "dHedge";
                x[6, 2] = "delta not. (Hedge Crncy)";
                x[6, 3] = "delta not. (Product CCY)";
                x[6, 4] = "dPV(dBeta)";
                x[6, 5] = "Name";
                x[6, 6] = "Jump to Default";
                x[6, 7] = "dHedge JTD";
                x[6, 8] = "delta not. (Hedge Crncy)";
                x[6, 9] = "delta not. (Product CCY)";
                x[5, 2] = "Leverage=";
                x[5, 7] = "Leverage=";
                for (i = 0; i <= 5; i++)
                {
                    for (j = 3; j <= 5; j++)
                    {
                        x[i, j] = 0 + "";
                    }
                }
            }
            else if (IsCDO)
            {
                x = new string[6 + numberOfIssuer + 1, 6];
                x[6, 0] = "dPV"; // dPV/dCDS_PV
                x[6, 1] = "dHedge";
                x[6, 2] = "delta not. (Hedge Crncy)";
                x[6, 3] = "delta not. (Product CCY)";
                x[6, 4] = "dPV(dBeta)";
                x[6, 5] = "Name";
                x[5, 2] = "Leverage=";
                for (i = 0; i <= 5; i++)
                {
                    for (j = 3; j <= 5; j++)
                    {
                        x[i, j] = 0 + "";
                    }
                }
            }
            else
            {
                x = new string[6 + numberOfIssuer + 1, 6];
                x[6, 0] = "dPV"; // dPV/dCDS_PV
                x[6, 1] = "dHedge";
                x[6, 2] = "delta not. (Hedge Crncy)";
                x[6, 3] = "delta not. (Product CCY)";
                x[6, 4] = "dPV(dBeta)";// ne devrait pas être là MODIF
                x[6, 5] = "Name";
                for (i = 0; i <= 5; i++)
                {
                    for (j = 2; j <= 5; j++)
                    {
                        x[i, j] = 0 + "";
                    }
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
                        EuropeanLow[i-1] = CDOModel.EuropeanCDOLossUnit(numberOfIssuer, lossUnitAmount, test0, DefaultProbability, ((double[])correl)[0], (double[])betaAdder, CurrentZC, (double[])nominalIssuer, (double[])recoveryIssuer,withStochasticRecovery, withGreeks, dBeta);
                        EuropeanHigh[i-1] = CDOModel.EuropeanCDOLossUnit(numberOfIssuer, lossUnitAmount, test1, DefaultProbability, ((double[])correl)[1], (double[])betaAdder, CurrentZC, (double[])nominalIssuer, (double[])recoveryIssuer,withStochasticRecovery, withGreeks, dBeta);
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

            if (!IsCDO)
            {
                x[4, 0] = (double)nominalIssuer * Double.Parse(x[4, 0]) + "";
            }
            double Leverage = 0;
            //object[] HedgingCDS = null;
            double val1=0;
            double val2=0;
            double val3=0;
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
                        if (!IsCDO)
                        {
                            val1 = HedgingCDS[0];
                        }
                        else
                        {
                            val1 = ((double[])standardSpread)[i - 1]/10000;
                        }
                        val2 = HedgingCDS[1];
                        val3 = HedgingCDS[2];

                        if (IsCDO)
                        {
                            hedging_cds = AmericanSwap(maturity, 1, j, 1.0, ThisCDS.Recovery,
                                                        0,val1,cpnLastSettle, cpnPeriod,
                                                        cpnConvention, CreditDefaultSwapCurves.Curves[j].Currency, 0.0, 0.0, 0.0, 0.0,
                                                       betaAdder, val2, val3, withGreeksVal,0,0, null, lossUnitAmount,
                                                        integrationPeriod, schedule, probMultiplier);
                        }
                        else
                        {
                            hedging_cds = AmericanSwap(maturity, 1, j, 1.0, ThisCDS.Recovery,0,
                                           val1, cpnLastSettle, cpnPeriod,
                                           cpnConvention, CreditDefaultSwapCurves.Curves[j].Currency, 0.0, 0.0, 0.0, 0.0,
                                            betaAdder, val2, val3, withGreeksVal,0,0, null, 1.0,
                                           integrationPeriod, schedule, probMultiplier);

                        }

                        //Npv of the CDS and the tranche before a default
                        x[6 + i, 1] = hedging_cds[7, 0];
                        if (IsCDO && withGreeks&& withJTD)
                        {
                            x[6 + i, 7] = hedging_cds[0, 0];
                        }


                        // Hedge in CD0 currency (delta CDO is in CDO currency unit) 
                        x[6 + i, 3] = "" + (Double.Parse(x[6 + i, 0]) / Double.Parse(hedging_cds[7, 0]));

                        // Hedge in CDS currency (delta CDO is in CDO currency unit => it has to be converted)

                        x[6 + i, 2] = "" + ((Double.Parse(x[6 + i, 3]) / (double)StrippingIRS.GetFXSpot(pricingCurrency)) * StrippingIRS.GetFXSpot(CreditDefaultSwapCurves.Curves[j].Currency));
                        x[6 + i, 5] = CreditDefaultSwapCurves.Curves[j].CDSName;

                        /////////////////::::addd
                        if (!IsCDO)
                        {
                            //x[4, 0] = (double)nominalIssuer * Double.Parse(x[4, 0]) + "";

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

            if (IsCDO && withJTD && withGreeks)
            {
                double[] recovery = (double[])recoveryIssuer;
                double defaultSpread = 1.50;//Modification Jump-to-Default, highest constant for the spread such that it compiles and let the possibility to simulate a default
                double[] shockedCurve = new double[9];
                string[] spreadCurveMaturity = { "3m", "6m", "1Y", "2Y", "3Y", "4Y", "5Y", "7Y", "10Y" };//MODIF JTD, temporaire
                string intensity = "Curvepoint";//MODIF JTD, temporaire
                double[] curve;
                string[,] hedging_cds;
                withJTD = false;
                Leverage = 0;
                for (i = 1; i <= numberOfIssuer; i++)
                {
                    cdsID = ((int[])CDSListID)[i - 1];
                    curve = StrippingCDS.CreditDefaultSwapCurves.Curves[cdsID].CDSSpread;

                    for (j = 0; j < curve.Length; j++)
                    {
                        shockedCurve[j] = defaultSpread;//MODIF ITRAXX 
                        //if (curve[j] != 0)
                        //{
                        //    shockedCurve[j] = defaultSpread;
                        //}
                        //else
                        //{
                        //    shockedCurve[j] = 0;
                        //}
                    }
                    StrippingCDS.StripDefaultProbability(cdsID, CreditDefaultSwapCurves.Curves[cdsID].CDSName, ParamDate, CDSRollDate, shockedCurve, spreadCurveMaturity, pricingCurrency, 0.4, false, intensity);
                    hedging_cds = AmericanSwap(maturity, 1, cdsID, 1.0, 0.25,
                            0, val1, cpnLastSettle, cpnPeriod, cpnConvention, pricingCurrency, 0.0, 0.0, 0.0, 0.0,
                           betaAdder, val2, val3, 0, 0, 0, null, lossUnitAmount,
                            integrationPeriod, schedule, probMultiplier);//Recovery fixed at 0.25%, see MAR 22.12 (same as non tranched MBS ?)
                    double oldRecovery = recovery[i-1];
                    recovery[i - 1] = 0.0;//Recovery fixed at 0, see MAR 22.11 for the DRC
                    string[,] test = AmericanSwap(maturity, numberOfIssuer, IssuerID, nominalIssuer, recovery, standardSpread, inputSpread, cpnLastSettle, cpnPeriod, cpnConvention, pricingCurrency, fxCorrel,
                        fxVol, strikes, correl, betaAdder, isAmericanFloatLegVal, isAmericanFixedLegVal, 0,0,0, HedgingCDS, lossUnitAmount, integrationPeriod, cpnSchedule, probMultiplier, dBeta);
                    recovery[i - 1] = oldRecovery;
                    x[6 + i, 6] =  double.Parse(test[0, 0])- double.Parse(x[0, 0]) + "";
                    //x[6 + i, 7] = double.Parse(hedging_cds[0, 0]) - double.Parse(x[6+i,7]) + "";
                    x[6 + i, 7] = 0.75 - double.Parse(x[6 + i, 7]) + "";//P&L CDS is just the payout if default ?? u dumb, 0.75 is the LGD
                    x[6 + i, 9] = "" + (Double.Parse(x[6 + i, 6]) / Double.Parse(x[6 + i, 7]));

                    // Hedge in CDS currency (delta CDO is in CDO currency unit => it has to be converted)
                    x[6 + i, 8] = "" + ((Double.Parse(x[6 + i, 9]) / StrippingIRS.GetFXSpot(pricingCurrency)) * StrippingIRS.GetFXSpot(CreditDefaultSwapCurves.Curves[cdsID].Currency));
                    Leverage += double.Parse(x[6 + i, 9]);
                    StrippingCDS.StripDefaultProbability(cdsID, StrippingCDS.CreditDefaultSwapCurves.Curves[cdsID].CDSName, ParamDate, CDSRollDate, curve, spreadCurveMaturity, pricingCurrency, 0.4, false, intensity);
                }
                x[5, 8] = Leverage / TrancheWidth + "";
                // Computation time
            }
            x[5, 0] = (DateTime.Now - StartTime) + "";
            x[5, 1] = (DateTime.Now - StartTime) + "";//MODIF, AJOUT
            return x;
        }
        public static double[,] CDSDeltaGIRR(string[] issuerName, double[] standardSpread, double[] recovery, double[] nominal,string cpnPeriod,
            string cpnConvention, string cpnLastSettle, string pricingCurrency, double[] hedgingCds, string integrationPeriod)
        {
            int lastIndice=issuerName.Length;
            for(int i = 0; i < issuerName.Length; i++)
            {
                if (issuerName[i]==""|| String.IsNullOrEmpty(issuerName[i]))
                {
                    lastIndice = i;
                    break;
                }
            }
            double additionalWeight = 1;
            if (pricingCurrency == "EUR" || pricingCurrency == "USD" || pricingCurrency == "GPB" || pricingCurrency == "AUD" || pricingCurrency == "JPY" || pricingCurrency == "SEK" || pricingCurrency == "CAD")//see 21.44
            {
                additionalWeight = Math.Sqrt(2);
            }
            double[] riskWeights = { 0.017, 0.017, 0.016, 0.013, 0.012, 0.011, 0.011, 0.011, 0.011, 0.011 };
            string[] tenors = { "3M", "6M", "1Y", "2Y", "3Y", "5Y", "10Y", "15Y", "20Y", "30Y" };
            double[,] results = new double[lastIndice, tenors.Length + 1];
            int[] months = { 3, 6, 12, 24, 36, 60, 120, 180, 240, 360 };
            double shockedGIRR = 0.0001;
            double[] sumSensitivities = new double[10];
            for (int i =  0; i < lastIndice; i++)
            {
                for(int j = 0; j < tenors.Length; j++)
                {
                    results[i,j] = -Double.Parse(CDS(issuerName[i], tenors[j], standardSpread[i] * shockedGIRR, recovery[i], nominal[i], cpnPeriod, cpnConvention, cpnLastSettle, pricingCurrency, 0, 0, 1, 1, 0, hedgingCds, integrationPeriod,1,0)[0,0]);
                    results[i, j] += Double.Parse(CDS(issuerName[i], tenors[j], standardSpread[i] * shockedGIRR, recovery[i], nominal[i], cpnPeriod, cpnConvention, cpnLastSettle, pricingCurrency, 0, 0, 1, 1, 0, hedgingCds, integrationPeriod, 1,months[j])[0, 0]);
                    results[i, j] /= shockedGIRR;
                    results[i, j] *= riskWeights[j] / additionalWeight;
                    sumSensitivities[j] += results[i, j];
                }
            }
            // Computation of K_b, see 21.4 for more information
            double[,] correlationMatrix = { { 1.0, 0.97, 0.914, 0.811, 0.719, 0.566, 0.4, 0.4, 0.4, 0.4 }, { 0.97, 1.0, 0.97, 0.914, 0.861, 0.763, 0.566, 0.419, 0.4, 0.4 },{0.914, 0.97, 1.0, 0.97, 0.942, 0.887, 0.763, 0.657, 0.566, 0.419}
            ,{0.811, 0.914, 0.97, 1.0, 0.985, 0.956, 0.887, 0.823, 0.763, 0.657},{0.719, 0.861, 0.942, 0.985, 1.0, 0.98, 0.932, 0.887, 0.844, 0.763},{0.566, 0.763, 0.887, 0.956, 0.98, 1.0, 0.97, 0.942, 0.914, 0.861},
            {0.4, 0.566, 0.763, 0.887, 0.932, 0.97, 1.0, 0.985, 0.97, 0.942},{0.4, 0.419, 0.657, 0.823, 0.887, 0.942, 0.985, 1.0, 0.99, 0.97},{0.4, 0.4, 0.566, 0.763, 0.844, 0.914, 0.97, 0.99, 1.0, 0.985},
            {0.4, 0.4, 0.419, 0.657, 0.763, 0.861, 0.942, 0.97, 0.985, 1.0}};
            double Kb = 0;
            for (int i = 0; i < tenors.Length; i++)
            {
                Kb += Math.Pow(sumSensitivities[i], 2);
                double s = 0;
                for (int j = 0; j < tenors.Length && j != i; j++)
                {
                    s += correlationMatrix[i, j] * sumSensitivities[i] * sumSensitivities[j];
                }
                Kb += s;
            }
            Kb = Math.Sqrt(Math.Max(0, Kb));
            results[0, 10] = Kb;
            return results;
        }
        public static double ImpliedCorrelation(double trancheSpread, string maturity, double[] strikes, double lowCorrel, double[] spreadStandard, string pricingCurrency,
    int numberOfIssuer, string[] issuerList, double[] nominalIssuer, double spread, string cpnPeriod,
    string cpnConvention, string cpnLastSettle, double fxCorrel, double fxVol, double[] betaAdder,
    double[] recoveryIssuer = null, double isAmericanFloatLeg = 0, double isAmericanFixedLeg = 0,
    double withGreeks = 0, double withJtdVAL = 0, double withStochasticRecoveryVAL = 0, double[] hedgingCDS = null, double? lossUnitAmount = null,
    string integrationPeriod = "1m", double probMultiplier = 1, double dBeta = 0.1)// Find the base correlation of the high strike tranche given the base correlation of the low strike tranche, with the spread given
        {
            int k = 0;
            double impliedHighCorrel = lowCorrel;
            double epsilon = 0.000001;
            double b = 1.0;
            double objectiveFunction = 0;
            double[] correl = { lowCorrel, (lowCorrel+b)/2 };
            do
            {
                k += 1;
                objectiveFunction = Double.Parse(CDO(maturity, strikes, correl, spreadStandard, pricingCurrency, numberOfIssuer, issuerList, nominalIssuer, spread, cpnPeriod,
                    cpnConvention, cpnLastSettle, fxCorrel, fxVol, betaAdder, recoveryIssuer, isAmericanFloatLeg, isAmericanFixedLeg, withGreeks,
                    withJtdVAL, withStochasticRecoveryVAL, hedgingCDS, lossUnitAmount, integrationPeriod, probMultiplier, dBeta, 0)[3, 0]);
                if(objectiveFunction>= trancheSpread)
                {
                    impliedHighCorrel = (impliedHighCorrel + b) / 2;
                }
                else
                {
                    b = (impliedHighCorrel + b)/2;
                }
                correl[1] = (impliedHighCorrel + b) / 2;
            }
            while (Math.Abs(objectiveFunction-trancheSpread)>epsilon || k==5000 );
            //Dichotomie



            return impliedHighCorrel;
        }
        public static double[] DeltaGIRR(string girrCurrency, string maturity, double[] strikes, double[] correl, double[] spreadStandard, string pricingCurrency,
    int numberOfIssuer, string[] issuerList, double[] nominalIssuer, double spread, string cpnPeriod,
    string cpnConvention, string cpnLastSettle, double fxCorrel, double fxVol, double[] betaAdder,
    double[] recoveryIssuer = null, double isAmericanFloatLeg = 0, double isAmericanFixedLeg = 0,
    double withGreeks = 0, double withJtdVAL = 0, double withStochasticRecoveryVAL = 0, double[] hedgingCDS = null, double? lossUnitAmount = null,
    string integrationPeriod = "1m", double probMultiplier = 1, double dBeta = 0.1)
        {
            int lastIndice = issuerList.Length;
            for (int i = 0; i < issuerList.Length; i++)//Au cas où dans la sélection de noms serait plus grande que les noms implémentés
            {
                if (issuerList[i] == "" || String.IsNullOrEmpty(issuerList[i]))
                {
                    lastIndice = i;
                    break;
                }
            }
            double additionalWeight = 1;
            if (pricingCurrency == "EUR" || pricingCurrency == "USD" || pricingCurrency == "GPB" || pricingCurrency == "AUD" || pricingCurrency == "JPY" || pricingCurrency == "SEK" || pricingCurrency == "CAD")//see 21.44
            {
                additionalWeight = Math.Sqrt(2);
            }
            double shockedGIRR = 0.0001;
            string[] tenors = { "3M", "6M", "1Y", "2Y", "3Y", "5Y", "10Y", "15Y", "20Y", "30Y" };
            int[] months = { 3, 6, 12, 24, 36, 60, 120, 180, 240, 360 };
            double[] riskWeights = { 0.017, 0.017, 0.016, 0.013, 0.012, 0.011, 0.011, 0.011, 0.011, 0.011 };
            double[] CDOresults = new double[tenors.Length];
            double[,] CDSresults = new double[lastIndice, tenors.Length];
            double[] nonShocked = new double[10];
            double[] shocked = new double[10];
            double[] results = new double[tenors.Length];
            for (int i = 0; i < tenors.Length; i++)
            {
                for (int j = 0; j < lastIndice; j++)
                {
                    CDSresults[j, i] = -Double.Parse(CDS(issuerList[j], maturity, spreadStandard[j] * shockedGIRR, recoveryIssuer[j], nominalIssuer[j], cpnPeriod, cpnConvention, cpnLastSettle, pricingCurrency, 0, 0, 1, 1, 0, hedgingCDS, integrationPeriod, 1, 0, girrCurrency)[0, 0]);
                    CDSresults[j, i] += Double.Parse(CDS(issuerList[j], maturity, spreadStandard[j] * shockedGIRR, recoveryIssuer[j], nominalIssuer[j], cpnPeriod, cpnConvention, cpnLastSettle, pricingCurrency, 0, 0, 1, 1, 0, hedgingCDS, integrationPeriod, 1, months[i], girrCurrency)[0, 0]);
                    CDSresults[j, i] /= shockedGIRR;
                    CDSresults[j, i] *= riskWeights[j] / additionalWeight;
                    results[i] -= CDSresults[j, i];
                }
                nonShocked[i] = Double.Parse(CDO(maturity, strikes, correl, spreadStandard, pricingCurrency, numberOfIssuer, issuerList, nominalIssuer, spread, cpnPeriod,
                    cpnConvention, cpnLastSettle, fxCorrel, fxVol, betaAdder, recoveryIssuer, isAmericanFloatLeg, isAmericanFixedLeg, withGreeks,
                    withJtdVAL, withStochasticRecoveryVAL, hedgingCDS, lossUnitAmount, integrationPeriod, probMultiplier, dBeta, 0, girrCurrency)[0, 0]);
                shocked[i] = Double.Parse(CDO(maturity, strikes, correl, spreadStandard, pricingCurrency, numberOfIssuer, issuerList, nominalIssuer, spread, cpnPeriod,
                    cpnConvention, cpnLastSettle, fxCorrel, fxVol, betaAdder, recoveryIssuer, isAmericanFloatLeg, isAmericanFixedLeg, withGreeks,
                    withJtdVAL, withStochasticRecoveryVAL, hedgingCDS, lossUnitAmount, integrationPeriod, probMultiplier, dBeta, months[i],girrCurrency)[0, 0]);
                CDOresults[i] = (shocked[i] - nonShocked[i]) / shockedGIRR;
                CDOresults[i] *= riskWeights[i] / additionalWeight;
                results[i] += CDOresults[i];
            }
            return results;
        }
        public static double[,] Girr(DateTime paramDate,string maturity, double[] strikes, double[] correl, double[] spreadStandard, string pricingCurrency,
    int numberOfIssuer, string[] issuerList, double[] nominalIssuer, double spread, string cpnPeriod,
    string cpnConvention, string cpnLastSettle, double fxCorrel, double fxVol, double[] betaAdder,
    double[] recoveryIssuer = null, double isAmericanFloatLeg = 0, double isAmericanFixedLeg = 0,
    double withGreeks = 0, double withJtdVAL = 0, double withStochasticRecoveryVAL = 0, double[] hedgingCDS = null, double? lossUnitAmount = null,
    string integrationPeriod = "1m", double probMultiplier = 1, double dBeta = 0.1)
        {
            StrippingIRS.IRCurveStore[] curvesList = StrippingIRS.CurveList;
            int lastIndice = 0;
            for(int i = 1;i< curvesList.Length;i++)
            {
                if (!String.Equals(curvesList[i].Currency, curvesList[i - 1].Currency))
                {
                    lastIndice++;
                }
            }
            int[] indice = new int[lastIndice+1];
            indice[0] = 0;
            lastIndice = 0;
            for (int i = 1; i < curvesList.Length; i++)
            {
                if (!String.Equals(curvesList[i].Currency, curvesList[i - 1].Currency))
                {
                    lastIndice++;
                    indice[lastIndice] = i;
                }
            }
            double[,] girrSensitivities = new double[curvesList.Length, 10];
            for(int i = 1;i<indice.Length;i++)
            {
                StrippingIRS.StripZC(paramDate, curvesList[indice[i]].Currency, curvesList[indice[i]].SwapRates, curvesList[indice[i]].CurveDates,
                    curvesList[indice[i]].SwapPeriod, curvesList[indice[i]].SwapBasis, curvesList[indice[i]].FXRate) ;//Strip toutes les autres courbes autre que la première currency
            }
            for (int i = 0; i < curvesList.Length; i++)
            {
                StrippingIRS.StripZC(paramDate, curvesList[i].Currency, curvesList[i].SwapRates, curvesList[i].CurveDates,
                    curvesList[i].SwapPeriod, curvesList[i].SwapBasis, curvesList[i].FXRate);
                for(int j = 1; j < indice.Length; j++)//on considère juste que les premières courbes de chaque currency sont celle de base qu'on veut tjrs
                {
                    if (i == indice[j])
                    {
                        StrippingIRS.StripZC(paramDate, curvesList[indice[j-1]].Currency, curvesList[indice[j-1]].SwapRates, curvesList[indice[j-1]].CurveDates,
                    curvesList[indice[j-1]].SwapPeriod, curvesList[indice[j-1]].SwapBasis, curvesList[indice[j-1]].FXRate);
                        break;
                    }
                }
                double[] results = DeltaGIRR(curvesList[i].Currency, maturity, strikes, correl, spreadStandard, pricingCurrency, numberOfIssuer, issuerList, nominalIssuer, spread,
                    cpnPeriod, cpnConvention, cpnLastSettle, fxCorrel, fxVol, betaAdder, recoveryIssuer, isAmericanFloatLeg, isAmericanFixedLeg, withGreeks, withJtdVAL, withStochasticRecoveryVAL,
                    hedgingCDS, lossUnitAmount, integrationPeriod, probMultiplier, dBeta);
                for(int j = 0; j < results.Length; j++)
                {
                    girrSensitivities[i, j] = results[j];
                }
            }
            double[] tenors = { 0.25, 0.5, 1, 2, 3, 5, 10, 15, 20, 30 };
            double[] Kb = new double[indice.Length];
            for(int b = 0; b < indice.Length; b++)
            {
                Kb[b] = 0;
                int limit = (b == indice.Length - 1) ? curvesList.Length : indice[b + 1];
                for (int j = 0; j < 10; j++)
                {
                    for(int i = indice[b];i< limit; i++)
                    {
                        for (int k = 0; k < 10; k++)
                        {
                            for (int l = indice[b]; l < limit; l++)
                            {
                                Kb[b] += girrSensitivities[i, j] * girrSensitivities[l, k];
                                Console.WriteLine("bucket: "+b+", curve: "+i+"vs curve: "+l+", tenor: " + tenors[j]+"vs tenor: " + tenors[k]);
                                if (l==i & k != j)//same bucket with different tenor and same curve
                                {
                                    Kb[b] *= Math.Max(Math.Exp(-0.03 * Math.Abs(tenors[k] - tenors[j]) / Math.Min(tenors[k], tenors[j])),0.4);
                                    Console.WriteLine(Math.Max(Math.Exp(-0.03 * Math.Abs(tenors[k] - tenors[j]) / Math.Min(tenors[k], tenors[j])), 0.4));
                                }
                                else if(l!=i & k != j)//same bucket with different tenor and different curve
                                {
                                    Kb[b] *= Math.Max(Math.Exp(-0.03 * Math.Abs(tenors[k] - tenors[j]) / Math.Min(tenors[k], tenors[j])), 0.4)*0.999;
                                    Console.WriteLine(Math.Max(Math.Exp(-0.03 * Math.Abs(tenors[k] - tenors[j]) / Math.Min(tenors[k], tenors[j])), 0.4));
                                }
                                else if(l!=i & k == j)//same bucket with same tenor and different curve
                                {
                                    Kb[b] *= 0.999;
                                }
                            }
                        }
                    }
                    Kb[b] = Math.Sqrt(Math.Max(0, Kb[b]));
                }
            }
            return girrSensitivities;
        }
    }
}
