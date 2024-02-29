
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using static ValoLibrary.StrippingIRS;
using Excel = Microsoft.Office.Interop.Excel;
using Microsoft.VisualBasic;
using MathNet.Numerics.Distributions;
using Microsoft.Office.Interop.Excel;
using MathNet.Numerics.LinearAlgebra.Factorization;



namespace ValoLibrary
{
    public class StrippingCDS
    {
        public struct CDSCurve
        {
            public string CDSName;              // CDS Reference Entity name. For information only
            public double Recovery;             // CDS Recovery Rate
            public string Currency;             // CDS Currency
            public int NDates;                  // Nb of Date for CDS spread curve
            public string[] CurveDates;       // CDS spread date
            public double[] CDSSpread;          // CDS spread curve for the specified date
            public double[] StrippedDPandShocked;  // Default Probabilities derived from CDS spread curve
            public double[,] MonthlyDPandShocked;   // Monthly Default Probabilities derived from CDS spread curve
            public bool CDSdone;                // Control to check whether CDS has been correctly entered
        }

        public struct CDSCurveList
        {
            public DateTime CDSRollDate;        // Roll Date for CDS
            public int NumberOfCurves;
            public bool LastError;
            public CDSCurve[] Curves;
        }

        public static CDSCurveList CreditDefaultSwapCurves = new CDSCurveList();
        public static int LastCDSCurveID;

        private const double SpreadShock = 0.05;         // relative shock to apply on spread
        private const double MinSpreadShock = 0.0001;    // minimum shock to apply on spread


        public static int GetCDSCurveId(string CDSName)
        {

            int i;
            if (LastCDSCurveID >= 1 && LastCDSCurveID <= CreditDefaultSwapCurves.NumberOfCurves)
            {
                // Testez si CDSName est égal à la dernière recherche
                if (string.Equals(CreditDefaultSwapCurves.Curves[LastCDSCurveID - 1].CDSName, CDSName, StringComparison.OrdinalIgnoreCase))
                {
                    CreditDefaultSwapCurves.LastError = false;
                    return LastCDSCurveID;
                }
            }

            for (i = 0; i < CreditDefaultSwapCurves.NumberOfCurves; i++)
            {
                if (string.Equals(CreditDefaultSwapCurves.Curves[i].CDSName, CDSName, StringComparison.OrdinalIgnoreCase))
                {
                    LastCDSCurveID = i+1;
                    CreditDefaultSwapCurves.LastError = false;
                    return LastCDSCurveID;
                }
            }
            return -1;
        }

        public static bool StoreDP(DateTime paramDate, int cdsID, string CDSName,
            double RecoveryRate, string Currency, string[] CurveDates,
            double[] CDSCurve, double[] StrippedDP, DateTime CDSRollDate,
            double[,] MonthlyDP)
        {
            // Store CDS spread and default probability into memory to simplify functions parameters
            int CurveID;
            int i, NDates;
            int CDSRollDateOffset;
            int Scenario;

            NDates = CurveDates.Length;
            if (NDates != CDSCurve.Length)
            {
                Console.WriteLine( $"Curve Date and CDS Rates arrays do not have the same length. Fail to store curve {CDSName}. Called from: ");
                return false;
            }

            CreditDefaultSwapCurves.LastError = false;

            // Check if CDSName is a string or a number for name lookup afterwards
            if (UtilityDates.IsNumeric(CDSName))
            {
                if (!cdsID.Equals(CDSName))
                {
                    // if number, it has to be the same as the CDS ID
                    Console.WriteLine($"CDS Name has to be a string or, if a number, it must be equal to the CDS ID (CDS ID={cdsID}, CDS name:{CDSName}). CDS ID ({cdsID}) used. Called from ");
                    CDSName = cdsID.ToString();
                }
            }

            CurveID = cdsID;

            if (CurveID > CreditDefaultSwapCurves.NumberOfCurves || CreditDefaultSwapCurves.NumberOfCurves == 0)
            {
                // add a new curve
                CreditDefaultSwapCurves.NumberOfCurves = CurveID;
                Array.Resize(ref CreditDefaultSwapCurves.Curves, CreditDefaultSwapCurves.NumberOfCurves + 1);
            }

            //Store the data
            CreditDefaultSwapCurves.CDSRollDate = CDSRollDate;
            CreditDefaultSwapCurves.Curves[CurveID].CDSName = CDSName.ToUpper();
            CreditDefaultSwapCurves.Curves[CurveID].Recovery = RecoveryRate;
            CreditDefaultSwapCurves.Curves[CurveID].Currency = Currency;
            CreditDefaultSwapCurves.Curves[CurveID].NDates = NDates;

            CreditDefaultSwapCurves.Curves[CurveID].CurveDates = new string[NDates];
            CreditDefaultSwapCurves.Curves[CurveID].CDSSpread = new double[NDates];
            CreditDefaultSwapCurves.Curves[CurveID].StrippedDPandShocked = new double[2 * NDates];

            for (i = 0; i < NDates; i++)
            {
                CreditDefaultSwapCurves.Curves[CurveID].CurveDates[i] = CurveDates[i];
                CreditDefaultSwapCurves.Curves[CurveID].CDSSpread[i] = CDSCurve[i];
                CreditDefaultSwapCurves.Curves[CurveID].StrippedDPandShocked[i] = StrippedDP[i];
                CreditDefaultSwapCurves.Curves[CurveID].StrippedDPandShocked[NDates + i] = StrippedDP[NDates + i];
            }

            CDSRollDateOffset = MonthlyDP.GetLowerBound(0);
            CreditDefaultSwapCurves.Curves[CurveID].MonthlyDPandShocked = new double[121, 2];

            for (i = CDSRollDateOffset; i <= 120; i++)
            {
                for (Scenario = 0; Scenario <= 1; Scenario++)
                {
                    CreditDefaultSwapCurves.Curves[CurveID].MonthlyDPandShocked[i, Scenario] =MonthlyDP[i, Scenario];
                }
            }

            CreditDefaultSwapCurves.Curves[CurveID].CDSdone = true;
            return true;
        }


        public static double GetDefaultProbabilityQuanto(int issuerId,
            DateTime paramDate, string maturityDate, 
            string pricingCurrency = "", int scenario = 0, 
            double fXCorrel = 0, double fXVol = 0, 
            double currentTime = 1, double probMultiplier = 1)
        {
            int dateCounter, offset;
            DateTime nextDate, previousDate, prevPreviousDate, maturityDateX;
            double nextProbNoDef, previousProbNoDef, prevPreviousProbNoDef, prob, getdefaultprobabilitywithoutquanto, defaultBis;

            if (issuerId > CreditDefaultSwapCurves.NumberOfCurves)
            {
               
                Console.WriteLine($"Default Probability - Issuer {issuerId} out of range - probability set to 0 - called from ");
                return 0.0;
            }
            else if (!CreditDefaultSwapCurves.Curves[issuerId].CDSdone)
            {
                Console.WriteLine($"Default Probability - Issuer {issuerId} not defined - probability set to 0 - called from ");
                return 0.0;
            }

            string issuerCurrency = CreditDefaultSwapCurves.Curves[issuerId].Currency;
            DateTime cdsRollDate = CreditDefaultSwapCurves.CDSRollDate;
            double[,] defaultProb = CreditDefaultSwapCurves.Curves[issuerId].MonthlyDPandShocked;
            offset = defaultProb.GetLowerBound(0);

            maturityDateX = UtilityDates.ConvertDate(cdsRollDate, maturityDate);
            if (maturityDateX < paramDate)
            {
                Console.WriteLine($"Maturity Date prior to Parameter Date - Called from ");
                return 0.0;
            }

            previousProbNoDef = 1.0;
            previousDate = paramDate;
            prevPreviousProbNoDef = 1.0;
            prevPreviousDate = paramDate;

            if (string.IsNullOrEmpty(pricingCurrency))
            {
                pricingCurrency = issuerCurrency;
            }

            for (dateCounter = offset; dateCounter <= 120; dateCounter++)
            {
                nextDate = UtilityDates.ConvertDate(cdsRollDate, dateCounter + "m");
                if (nextDate < paramDate)
                {
                    nextDate = paramDate;
                }

                prob = defaultProb[dateCounter, scenario];

                if (prob != 0 && !double.IsNaN(prob))
                {
                    nextProbNoDef = 1.0 - prob;

                    if (nextDate > maturityDateX)
                    {
                        getdefaultprobabilitywithoutquanto = 1.0 - previousProbNoDef *
                            Math.Pow(nextProbNoDef / previousProbNoDef, (maturityDateX - previousDate).TotalDays / (nextDate - previousDate).TotalDays);

                        if (getdefaultprobabilitywithoutquanto <= 0)
                        {
                            return 0.0;
                        }
                        else if (issuerCurrency != pricingCurrency)
                        {
                            defaultBis = Normal.InvCDF(0, 1, getdefaultprobabilitywithoutquanto);
                            return UtilityBiNormal.NormalCumulativeDistribution(defaultBis + fXCorrel * fXVol * Math.Sqrt(currentTime));
                        }
                        else
                        {
                            return getdefaultprobabilitywithoutquanto* probMultiplier;
                        }
    

                    }

                    prevPreviousDate = previousDate;
                    prevPreviousProbNoDef = previousProbNoDef;
                    previousDate = nextDate;
                    previousProbNoDef = nextProbNoDef;
                }
            }

            // extrapolate at flat
            if (previousDate == paramDate)
            {
                Console.WriteLine($"Default probability for issuer id {issuerId} is empty - Called from ");
                return 0.0;
            }

            getdefaultprobabilitywithoutquanto = 1.0 - prevPreviousProbNoDef *
                Math.Pow((previousProbNoDef / prevPreviousProbNoDef), ((maturityDateX - prevPreviousDate).TotalDays / (previousDate - prevPreviousDate).TotalDays));

            if (issuerCurrency != pricingCurrency)
            {
                defaultBis = Normal.InvCDF(0, 1, getdefaultprobabilitywithoutquanto);
                return UtilityBiNormal.NormalCumulativeDistribution(defaultBis + fXCorrel * fXVol * Math.Sqrt(currentTime));
            }
            else
            {
                return getdefaultprobabilitywithoutquanto * probMultiplier;
            }
        }


        public static double GetCDS_PV(DateTime paramDate, DateTime CDSRollDate, double[,] CDSFloatingLeg, double[,] CDSCouponLeg,
            double[,] AmericanLessEuropeanLeg, double[,] RiskyZC, double[] ZC, int PreviousCalcMonth, int NextCalcMonth,
            double LossRate, double CDSSpread, int Scenario, double[,] MonthlyDP)
        {
            double DailyProbNoDefIncrease, ProbNoDefaultPreviousMonth, ProbNoDefaultNextMonth;
            double InitialProbNoDefault, CurrentProbNoDefault, NextProbNoDefault;
            int CouponCounter, NextCouponMonth, MonthCounter;
            double SumOfNotionalDT;
            int y, m, d;
            DateTime NextDate, PreviousDate, InitialDate, PreviousCouponDate;
            int DT, offsetzc;

            offsetzc = 1 * 0;

            ProbNoDefaultPreviousMonth = RiskyZC[PreviousCalcMonth, Scenario] / ZC[PreviousCalcMonth + offsetzc];
            ProbNoDefaultNextMonth = RiskyZC[NextCalcMonth, Scenario] / ZC[NextCalcMonth + offsetzc];

            CurrentProbNoDefault = ProbNoDefaultPreviousMonth;
            InitialProbNoDefault = ProbNoDefaultPreviousMonth;

            y = CDSRollDate.Year;
            m = CDSRollDate.Month;
            d = CDSRollDate.Day;
            

            if (PreviousCalcMonth == RiskyZC.GetLowerBound(0))
            {
                PreviousDate = paramDate;
                InitialDate = paramDate;
                

            }
            else
            {
                PreviousDate = DateAndTime.DateSerial(y, m + PreviousCalcMonth, d);
                InitialDate = DateAndTime.DateSerial(y, m + PreviousCalcMonth, d);
                if (PreviousDate < paramDate)
                {
                    PreviousDate = paramDate;
                    InitialDate = paramDate;
                }
            }
            NextDate = DateAndTime.DateSerial(y, m + NextCalcMonth, d);
            if (NextDate < paramDate)
            {
                NextDate = paramDate;
                DailyProbNoDefIncrease = 1;
            }
            else
            {
                DailyProbNoDefIncrease = Math.Pow(ProbNoDefaultNextMonth / ProbNoDefaultPreviousMonth, 1.0 / (NextDate - InitialDate).Days);
            }

            PreviousCouponDate = PreviousDate;
            NextCouponMonth = PreviousCalcMonth;

            for (CouponCounter = 1; CouponCounter <= (NextCalcMonth - PreviousCalcMonth) / 3; CouponCounter++)
            {
                NextCouponMonth = PreviousCalcMonth + CouponCounter * 3;

                AmericanLessEuropeanLeg[NextCouponMonth, Scenario] = AmericanLessEuropeanLeg[NextCouponMonth - 3, Scenario];
                SumOfNotionalDT = 0;

                for (MonthCounter = 2; MonthCounter >= 0; MonthCounter--)
                {
                    int j = NextCouponMonth - MonthCounter;
                    NextDate = DateAndTime.DateSerial(y, m + j, d);

                    if (NextDate < paramDate)
                    {
                        NextDate = paramDate;
                    }

                    double totalDays = (NextDate - PreviousDate).TotalDays;
                    DT = (int)((totalDays / 2.0 - (PreviousCouponDate - PreviousDate).TotalDays) / 360.0);

                    if (NextDate > InitialDate)
                    {
                        NextProbNoDefault = InitialProbNoDefault * Math.Pow(DailyProbNoDefIncrease, (NextDate - InitialDate).TotalDays);
                    }
                    else
                    {
                        NextProbNoDefault = 1;
                    }

                    RiskyZC[j, Scenario] = NextProbNoDefault * ZC[j + offsetzc];

                    if (PreviousDate == paramDate)
                    {
                        DateTime Date1 = DateAndTime.DateSerial(y, m + j - 1, d);
                        DateTime Date2 = DateAndTime.DateSerial(y, m + j, d);

                        SumOfNotionalDT += Math.Sqrt(ZC[j + offsetzc] * ZC[j - 1 + offsetzc]) * DT * (CurrentProbNoDefault - NextProbNoDefault);
                    }
                    else
                    {
                        SumOfNotionalDT += Math.Sqrt(ZC[j + offsetzc] * ZC[j - 1 + offsetzc]) * DT * (CurrentProbNoDefault - NextProbNoDefault);
                    }

                    AmericanLessEuropeanLeg[NextCouponMonth, Scenario] +=
                        (ZC[j + offsetzc] - RiskyZC[j, Scenario]) * (1 - ZC[j + offsetzc] / ZC[j + offsetzc - 1]);

                    PreviousDate = NextDate;
                    CurrentProbNoDefault = NextProbNoDefault;
                    MonthlyDP[j, Scenario] = 1 - NextProbNoDefault;
                }

                CDSFloatingLeg[NextCouponMonth, Scenario] =
                    ZC[NextCouponMonth + offsetzc] -
                    RiskyZC[NextCouponMonth, Scenario] +
                    AmericanLessEuropeanLeg[NextCouponMonth, Scenario];

                NextDate = DateAndTime.DateSerial(y, m + NextCouponMonth, d);
                if (NextDate < paramDate)
                {
                    NextDate = paramDate;
                }

                CDSCouponLeg[NextCouponMonth, Scenario] =
                    CDSCouponLeg[NextCouponMonth - 3, Scenario] +
                    RiskyZC[NextCouponMonth, Scenario] * (NextDate - PreviousCouponDate).TotalDays / 360.0 +
                    SumOfNotionalDT;

                PreviousCouponDate = DateAndTime.DateSerial(y, m + NextCouponMonth, d);
                if (PreviousCouponDate < paramDate)
                {
                    PreviousCouponDate = paramDate;
                }
            }

            return LossRate * CDSFloatingLeg[NextCalcMonth, Scenario] - CDSSpread * CDSCouponLeg[NextCouponMonth, Scenario];
        }


        public static double GetCDS_PV_3m(DateTime paramDate, DateTime cdsRollDate, double[,] cdsFloatingLeg, double[,] cdsCouponLeg,
                                           double[,] americanLessEuropeanLeg, double[,] riskyZC, double[] zc, int previousCalcMonth,
                                           int nextCalcMonth, double lossRate, double cdsSpread, int scenario, double[] previousDefaultIntensity,
                                           double[] shiftDefaultIntensity, double[,] monthlyDP)
        {
            double dailyProbNoDefIncrease, probNoDefaultPreviousMonth;
            double initialProbNoDefault, currentProbNoDefault, nextProbNoDefault;
            int couponCounter, nextCouponMonth, monthCounter;
            double sumOfNotionalDT;
            int y, m, d, j;
            DateTime nextDate, previousDate, initialDate, previousCouponDate;
            double dt, offsetZC;

            offsetZC = 1 * 0;

            probNoDefaultPreviousMonth = riskyZC[previousCalcMonth, scenario] / zc[previousCalcMonth + (int)offsetZC];
            currentProbNoDefault = probNoDefaultPreviousMonth;
            initialProbNoDefault = probNoDefaultPreviousMonth;

            double prevDefaultIntensity;
            double curDefaultIntensity;
            DateTime prevPreviousDate;
            double prevPreviousNoDefProb;

            y = cdsRollDate.Year;
            m = cdsRollDate.Month;
            d = cdsRollDate.Day;

            if (previousCalcMonth == 0)
            {
                previousDate = paramDate;
                initialDate = paramDate;
            }
            else
            {
                previousDate = DateAndTime.DateSerial(y, m + previousCalcMonth, d);
                initialDate = DateAndTime.DateSerial(y, m + previousCalcMonth, d);
                if (previousDate < paramDate)
                {
                    previousDate = paramDate;
                    initialDate = paramDate;
                }
            }

            if (previousDate == paramDate)
            {
                prevDefaultIntensity = 0;
            }
            else
            {
                prevPreviousNoDefProb = riskyZC[previousCalcMonth - 1, scenario] / zc[previousCalcMonth - 1 + (int)offsetZC];
                prevPreviousDate = DateAndTime.DateSerial(y, m + previousCalcMonth - 1, d);
                if (prevPreviousDate < paramDate) prevPreviousDate = paramDate;
                prevDefaultIntensity = -Math.Log(probNoDefaultPreviousMonth / prevPreviousNoDefProb) *
                                        360.0 / (previousDate - prevPreviousDate).TotalDays;
                if (prevDefaultIntensity < 0) prevDefaultIntensity = 0;
            }

            curDefaultIntensity = prevDefaultIntensity;
            nextDate = new DateTime(y, m + nextCalcMonth, d);
            if (nextDate < paramDate)
            {
                nextDate = paramDate;
                dailyProbNoDefIncrease = 1;
            }

            previousCouponDate = previousDate;
            nextCouponMonth = previousCalcMonth;

            for (couponCounter = 1; couponCounter <= (nextCalcMonth - previousCalcMonth) / 3; couponCounter++)
            {
                nextCouponMonth = previousCalcMonth + couponCounter * 3;
                americanLessEuropeanLeg[nextCouponMonth, scenario] = americanLessEuropeanLeg[nextCouponMonth - 3, scenario];
                sumOfNotionalDT = 0;
                curDefaultIntensity += shiftDefaultIntensity[scenario];

                for (monthCounter = 2; monthCounter >= 0; monthCounter--)
                {
                    j = nextCouponMonth - monthCounter;
                    nextDate = DateAndTime.DateSerial(y, m + (int)j, d);

                    if (nextDate < paramDate)
                    {
                        nextDate = paramDate;
                    }

                    double totalDays = (nextDate - previousDate).TotalDays;
                    dt = (int)((totalDays / 2.0 - (previousCouponDate - previousDate).TotalDays) / 360.0);

                    if (nextDate > initialDate)
                    {
                        nextProbNoDefault = currentProbNoDefault * Math.Exp(-curDefaultIntensity * (nextDate - previousDate).TotalDays / 360.0);
                    }
                    else
                    {
                        nextProbNoDefault = 1;
                    }

                    riskyZC[j, scenario] = nextProbNoDefault * zc[j + (int)offsetZC];

                    if (previousDate == paramDate)
                    {

                        DateTime Date1 = DateAndTime.DateSerial(y, m + j - 1, d);
                        DateTime Date2 = DateAndTime.DateSerial(y, m + j, d);

                        sumOfNotionalDT = sumOfNotionalDT +
                            Math.Pow(zc[j + (int)offsetZC] * zc[(int)j - 1 + (int)offsetZC], 0.5) * dt *
                            (currentProbNoDefault - nextProbNoDefault);
                    }
                    else
                    {
                        sumOfNotionalDT = sumOfNotionalDT +
                            Math.Pow(zc[j + (int)offsetZC] * zc[j - 1 + (int)offsetZC], 0.5) * dt *
                            (currentProbNoDefault - nextProbNoDefault);
                    }

                    americanLessEuropeanLeg[nextCouponMonth, scenario] =
                        americanLessEuropeanLeg[nextCouponMonth, scenario] +
                        (zc[j + (int)offsetZC] - riskyZC[j, scenario]) *
                        (1.0 - zc[j + (int)offsetZC] / zc[j + (int)offsetZC - 1]);

                    previousDate = nextDate;
                    currentProbNoDefault = nextProbNoDefault;
                    monthlyDP[j, scenario] = 1 - nextProbNoDefault;
                }

                cdsFloatingLeg[nextCouponMonth, scenario] =
                    zc[nextCouponMonth + (int)offsetZC] - riskyZC[nextCouponMonth, scenario] +
                    americanLessEuropeanLeg[nextCouponMonth, scenario];

                nextDate = DateAndTime.DateSerial(y, m + nextCouponMonth, d);
                if (nextDate < paramDate)
                {
                    nextDate = paramDate;
                }

                cdsCouponLeg[nextCouponMonth, scenario] = cdsCouponLeg[nextCouponMonth - 3, scenario] +
                    riskyZC[nextCouponMonth, scenario] * (nextDate - previousCouponDate).TotalDays / 360.0 + sumOfNotionalDT;

                previousCouponDate = new DateTime(y, m + nextCouponMonth, d);
                if (previousCouponDate < paramDate)
                {
                    previousCouponDate = paramDate;
                }
            }


            return lossRate * cdsFloatingLeg[nextCalcMonth, scenario] -
                   cdsSpread * cdsCouponLeg[nextCouponMonth, scenario];
        }


        public static double[] StripDefaultProbability(int cdsID, string CDSName, DateTime ParamDate,
            DateTime CDSRollDate, double[] CDSCurve, string[] CurveMaturity,
            string CDSCurrency, double RecoveryRate, bool alterMode, string intensity)
        {
            int CurveID;
            int CDSRollDateOffset;
            int ScenarioNumber = 1;
            double[] ZC;

            // Placeholder for the definition of the StrippingIRS.InterestRateCurves class and related methods
            // Replace the placeholder with the actual class definition

            CurveID = StrippingIRS.GetCurveId(CDSCurrency);

            Console.WriteLine("CurveID" + CurveID);
            if (CurveID == -1)
            {
                
                if (StrippingIRS.InterestRateCurves.LastError == false)
                {
                    Console.WriteLine("iici 1");

                    Console.WriteLine($"Curve {CDSCurrency} was not stripped - Called from : ");
                    StrippingIRS.InterestRateCurves.LastError = true;
                    Console.WriteLine("iici 2");
                }
                return null;
            }

            Console.WriteLine("iici 3");

            // Compute ZC for Risky Curve if not done yet
            if (!StrippingIRS.InterestRateCurves.Curves[CurveID].IsMonthlyRollZCCalculated)
            {
                if (!StrippingIRS.VbaComputeMonthlyRiskyZC(CDSCurrency, ParamDate, CDSRollDate))
                {
                    if (StrippingIRS.InterestRateCurves.LastError == false)
                    {
                        Console.WriteLine($"Monthly ZC for curve {CDSCurrency} was not computed - called from : ");
                        StrippingIRS.InterestRateCurves.LastError = true;
                    }
                    return null;
                }
            }

            // Other variable declarations
            double[,] RiskyZC, CDSFloatingLeg, CDSCouponLeg, AmericanLessEuropeanLeg, FullDefaultProb;
            double CDSSpread, LossRate;
            int CDSCurvePointNumber, CurvePointCounter, PreviousCalcMonth, NextCalcMonth;

            //CurveID = Convert.ToInt32(cdsID);
            CDSRollDateOffset = (StrippingIRS.InterestRateCurves.Curves[CurveID].MonthlyRollZC).GetLowerBound(0);
            ZC = StrippingIRS.InterestRateCurves.Curves[CurveID].MonthlyRollZC;

            // Initialize arrays
            RiskyZC = new double[121, 2];
            CDSFloatingLeg = new double[121, 2];
            CDSCouponLeg = new double[121, 2];
            AmericanLessEuropeanLeg = new double[121, 2];
            FullDefaultProb = new double[121, 2];

            RiskyZC[CDSRollDateOffset, 0] = 1; // initial curve
            RiskyZC[CDSRollDateOffset, 1] = 1; // shocked curve
            double dRiskyZC = -0.0001; // shock on Risky ZC used to compute the derivatives of a CDS PV against a ZC

            double[] PreviousDefaultIntensity = new double[2];
            double[] ShiftDefaultIntensity = new double[2];
            PreviousDefaultIntensity[0] = 1;
            ShiftDefaultIntensity[1] = 0.000001; // initial and shocked curve
            double dShiftDefaultIntensity = 0.000000001; // shock on Default Intensity Shift used to compute the derivatives of a CDS PV

            FullDefaultProb[CDSRollDateOffset, 0] = 0; // initial curve
            FullDefaultProb[CDSRollDateOffset, 1] = 0; // shocked curve

            // PV of the floating leg of the American CDS
            CDSFloatingLeg[CDSRollDateOffset, 0] = 0; // initial curve
            CDSFloatingLeg[CDSRollDateOffset, 1] = 0; // shocked curve

            // PV of the Coupon Leg of standard CDS with normalized spread at 100%
            CDSCouponLeg[CDSRollDateOffset, 0] = 0; // initial curve
            CDSCouponLeg[CDSRollDateOffset, 1] = 0; // shocked curve

            // Difference of the PV of the floating leg of the American CDS less the PV of the floating leg of the European CDS
            AmericanLessEuropeanLeg[CDSRollDateOffset, 0] = 0; // initial curve
            AmericanLessEuropeanLeg[CDSRollDateOffset, 1] = 0; // shocked curve

            LossRate = 1 - Convert.ToDouble(RecoveryRate);

            CDSCurvePointNumber = CDSCurve.Length;
            if (CurveMaturity.Length != CDSCurvePointNumber)
            {
                Console.WriteLine($"CDS Curve and CDS Curve maturity do not contain the same number of data - Called from : ");
                return null;
            }

            PreviousCalcMonth = CDSRollDateOffset;
            ScenarioNumber = 1;
            int SpreadValueCount = 0;
            if (UtilityDates.MonthPeriod(CurveMaturity[CDSCurvePointNumber], CDSRollDate) > 120)

            {
                    Console.WriteLine("Not possible to calibrate risky ZC on curve with maturity beyond 10Y - Called from");
             
            }

            for (CurvePointCounter = 1; CurvePointCounter <= CDSCurvePointNumber; CurvePointCounter++)
            {
                CDSSpread = CDSCurve[CurvePointCounter];
                if (CDSSpread != 0)
                {
                    string NextMaturity = CurveMaturity[CurvePointCounter];
                    if (NextMaturity == null)
                    {
                        Console.WriteLine("Maturity undefined in CurveMaturity argument nb ");
                        return null;
                    }

                    NextCalcMonth = (int)UtilityDates.MonthPeriod(CurveMaturity[CurvePointCounter], CDSRollDate);
                    
                    if (UtilityDates.ConvertDate(CDSRollDate, NextCalcMonth + "M") <= ParamDate)
                    {
                        Console.WriteLine("No spread shall be given for duration lower than Parameter Date in CurveMaturity argument nb \" & CurvePointCounter & Chr(13) & \"Spread assumed to be 0\"");
                        return null;
                    }

                    SpreadValueCount ++;
                    if (SpreadValueCount == 1 || intensity != "3M")
                    {
                        double NextRiskyZC;
                        // Newton iteration to solve for the next Risky ZC
                        for (int Scenario = 0; Scenario < ScenarioNumber; Scenario++)
                        {
                            if (Scenario == 1)
                            {
                                double dSpread = UtilityLittleFunctions.MaxOf(SpreadShock * CDSSpread, MinSpreadShock);
                                CDSSpread += dSpread;
                                if (UtilityDates.ConvertDate(CDSRollDate, NextCalcMonth + "M") <= ParamDate)
                                {
                                    NextRiskyZC = RiskyZC[NextCalcMonth, 0];
                                }
                                else
                                {
                                    NextRiskyZC = RiskyZC[NextCalcMonth, 0] * Math.Exp(-dSpread / (1 - RecoveryRate) * NextCalcMonth / 12);
                                }
                            }
                            else
                            {
                                NextRiskyZC = RiskyZC[PreviousCalcMonth, 0] / ZC[PreviousCalcMonth] * ZC[NextCalcMonth];
                            }

                            double dPV_dRiskyZC;
                            double CDS_PV;
                            do
                            {
                                RiskyZC[NextCalcMonth, Scenario] = NextRiskyZC;
                                CDS_PV = GetCDS_PV(ParamDate, CDSRollDate, CDSFloatingLeg, CDSCouponLeg, AmericanLessEuropeanLeg, RiskyZC, ZC, PreviousCalcMonth, NextCalcMonth, LossRate, CDSSpread, Scenario, FullDefaultProb);

                                if (CDS_PV == 0)
                                {
                                    break;
                                }

                                RiskyZC[NextCalcMonth, Scenario] = NextRiskyZC + dRiskyZC;
                                dPV_dRiskyZC = (GetCDS_PV(ParamDate, CDSRollDate, CDSFloatingLeg, CDSCouponLeg, AmericanLessEuropeanLeg, RiskyZC, ZC, PreviousCalcMonth, NextCalcMonth, LossRate, CDSSpread, Scenario, FullDefaultProb) - CDS_PV) / dRiskyZC;

                                double AdjustRiskyZC;

                                if (CDS_PV == 0)
                                {
                                    AdjustRiskyZC = 0;
                                    break;
                                }
                                else
                                {
                                    AdjustRiskyZC = -CDS_PV / dPV_dRiskyZC;
                                }

                                if (Math.Abs(CDS_PV) < 0.00001 * UtilityLittleFunctions.MinOf(1, CDSCouponLeg[NextCalcMonth, Scenario]))
                                {
                                    break;
                                }

                                NextRiskyZC += AdjustRiskyZC;
                            } while (true);

                            RiskyZC[NextCalcMonth, Scenario] = NextRiskyZC;
                            CDS_PV = GetCDS_PV(ParamDate, CDSRollDate, CDSFloatingLeg, CDSCouponLeg, AmericanLessEuropeanLeg, RiskyZC, ZC, PreviousCalcMonth, NextCalcMonth, LossRate, CDSSpread, Scenario, FullDefaultProb);

                            if (NextRiskyZC > (RiskyZC[PreviousCalcMonth, Scenario] / ZC[PreviousCalcMonth + 1] * ZC[NextCalcMonth + 1]))
                            {
                                if (alterMode == true)
                                {
                                    Console.WriteLine($"Negative Default Probability for CDS Calibration at {NextCalcMonth} - Called from : ");
                                    Console.WriteLine("Continue to compute anyway");
                                }
                            }
                        }
                    }
                    else
                    {
                        // Default intensity constant over 3 months with a constant shift between CDS curve spread values
                        for (int Scenario = 0; Scenario < ScenarioNumber; Scenario++)
                        {
                            if (Scenario == 1)
                            {
                                double dSpread = UtilityLittleFunctions.MaxOf(SpreadShock * CDSSpread, MinSpreadShock);
                                CDSSpread += dSpread;
                            }

                            double CDS_PV;
                            double dPV_dRiskyZC;
                            double AdjustShiftDefaultIntensity;
                            do
                            {
                                CDS_PV = GetCDS_PV_3m(ParamDate, CDSRollDate, CDSFloatingLeg, CDSCouponLeg, AmericanLessEuropeanLeg, RiskyZC, ZC, PreviousCalcMonth, NextCalcMonth, LossRate, CDSSpread, Scenario, PreviousDefaultIntensity, ShiftDefaultIntensity, FullDefaultProb);

                                if (Math.Abs(CDS_PV) < 0.00001 * UtilityLittleFunctions.MinOf(1, CDSCouponLeg[NextCalcMonth, Scenario]))
                                {
                                    break;
                                }

                                ShiftDefaultIntensity[Scenario] += dShiftDefaultIntensity;
                                dPV_dRiskyZC = (GetCDS_PV_3m(ParamDate, CDSRollDate, CDSFloatingLeg, CDSCouponLeg, AmericanLessEuropeanLeg, RiskyZC, ZC, PreviousCalcMonth, NextCalcMonth, LossRate, CDSSpread, Scenario, PreviousDefaultIntensity, ShiftDefaultIntensity, FullDefaultProb) - CDS_PV) / dShiftDefaultIntensity;

                                AdjustShiftDefaultIntensity = -CDS_PV / dPV_dRiskyZC;
                                ShiftDefaultIntensity[Scenario] += AdjustShiftDefaultIntensity - dShiftDefaultIntensity;
                            } while (true);

                            CDS_PV = GetCDS_PV_3m(ParamDate, CDSRollDate, CDSFloatingLeg, CDSCouponLeg, AmericanLessEuropeanLeg, RiskyZC, ZC, PreviousCalcMonth, NextCalcMonth, LossRate, CDSSpread, Scenario, PreviousDefaultIntensity, ShiftDefaultIntensity, FullDefaultProb);

                            if (FullDefaultProb[NextCalcMonth, Scenario] < FullDefaultProb[NextCalcMonth - 1, Scenario])
                            {
                                if (alterMode == true)
                                {
                                    Console.WriteLine($"Negative Default Probability for CDS Calibration at {NextCalcMonth} - Called from : ");
                                    Console.WriteLine("Continue to compute anyway");
                                }
                            }
                        }
                    }

                    PreviousCalcMonth = NextCalcMonth;
                }
            }

            double[] Res = new double[CDSCurvePointNumber * 2];

            for (int Scenario = 0; Scenario < ScenarioNumber; Scenario++)
            {
                for (CurvePointCounter = 0; CurvePointCounter < CDSCurvePointNumber; CurvePointCounter++)
                {
                    CDSSpread = CDSCurve[CurvePointCounter];
                    if (CDSSpread != 0)
                    {
                        NextCalcMonth = (int)UtilityDates.MonthPeriod(CurveMaturity[CurvePointCounter].ToString() , CDSRollDate);
                        Res[CurvePointCounter + CDSCurvePointNumber * Scenario] = 1 - RiskyZC[NextCalcMonth, Scenario] / ZC[NextCalcMonth];
                    }
                    else
                    {
                        Res[CurvePointCounter + CDSCurvePointNumber * Scenario] = 0;
                    }
                }
            }

            if (StoreDP(ParamDate, cdsID, CDSName, RecoveryRate, CDSCurrency, CurveMaturity, CDSCurve, Res, CDSRollDate, FullDefaultProb))
            {
                return Res;
            }
            else
            {
                Console.WriteLine($"Problem while storing Curves {CDSName} - Called from : ");
                return null;
            }
        }


        public static string GetCDSName(int cdsID)
        {
            int CDS_ID;
            if (!int.TryParse(cdsID.ToString(), out CDS_ID))
            {
                CDS_ID = GetCDSCurveId(cdsID.ToString());
            }
            else
            {
                CDS_ID = cdsID;
            }

            if (CDS_ID < 0 || CDS_ID > CreditDefaultSwapCurves.NumberOfCurves)
            {
                return $"CDS Name - Issuer {cdsID} out of range - called from {Environment.StackTrace}";
            }

            if (!CreditDefaultSwapCurves.Curves[CDS_ID - 1].CDSdone)
            {
                return $"CDS Name - Issuer {cdsID} not defined - called from {Environment.StackTrace}";
            }

            return CreditDefaultSwapCurves.Curves[CDS_ID - 1].CDSName;
        }

        public static string GetCDSCurrency(int cdsID)
        {
            int cds_Id;

            if (!int.TryParse(cdsID.ToString(), out cds_Id))
            {
                cds_Id = GetCDSCurveId(cdsID.ToString());
            }
            else
            {
                cds_Id = cdsID;
            }

            if (cds_Id < 0 || cds_Id > CreditDefaultSwapCurves.NumberOfCurves)
            {
                return $"CDS Currency - Issuer {cds_Id} out of range - called from {Environment.StackTrace}";
            }

            if (!CreditDefaultSwapCurves.Curves[cds_Id - 1].CDSdone)
            {
                return $"CDS Currency - Issuer {cds_Id} not defined - called from {Environment.StackTrace}";
            }

            return CreditDefaultSwapCurves.Curves[cds_Id - 1].Currency;


        }


        public static object[,] GetMonthlyDP(int issuer, int scenario = 0)
        {
            int size;
            object defaultProb;
            int offset;
            int i;

            if (!int.TryParse(issuer.ToString(), out int issuerId))
            {
                issuerId = GetCDSCurveId(issuer.ToString());
            }

            if (issuer > CreditDefaultSwapCurves.NumberOfCurves)
            {
                return new object[,] { { $"Default Probability - Issuer {issuer} out of range - probability set to 0 - called from {Environment.StackTrace}" } };
            }
            else if (!CreditDefaultSwapCurves.Curves[issuer].CDSdone)
            {
                return new object[,] { { $"Default Probability - Issuer {issuer} not defined - probability set to 0 - called from {Environment.StackTrace}" } };
            }

            defaultProb = CreditDefaultSwapCurves.Curves[issuer].MonthlyDPandShocked;
            size = ((Array)defaultProb).GetUpperBound(0) - ((Array)defaultProb).GetLowerBound(0) + 1;
            offset = ((Array)defaultProb).GetLowerBound(0);

            object[,] res = new object[size, 2];

            for (i = 0; i < size; i++)
            {
                res[i, 0] = i - 1 + offset;

                if (((Array)defaultProb).GetValue(i - 1 + offset, scenario) != null)
                {
                    res[i, 1] = ((Array)defaultProb).GetValue(i - 1 + offset, scenario);
                }
                else
                {
                    res[i, 1] = "";
                }
            }

            return res;
        }

        public static object GetDefaultProb(int issuer, string maturityDate, int scenario = 0, double probMultiplier = 1)
        {
            DateTime paramDate;
            if (!int.TryParse(issuer.ToString(), out int issuerInt))
            {
                issuerInt = GetCDSCurveId(issuer.ToString());
            }

            if (issuer > CreditDefaultSwapCurves.NumberOfCurves)
            {
                return $"Default Probability - Issuer {issuer} out of range - probability set to 0 - called from ";
            }
            else if (!CreditDefaultSwapCurves.Curves[issuer].CDSdone)
            {
                return $"Default Probability - Issuer {issuer} not defined - probability set to 0 - called from ";
            }

            paramDate = StrippingIRS.InterestRateCurves.Curves[StrippingIRS.InterestRateCurves.NumberOfCurves].ParamDate;
            return GetDefaultProbabilityQuanto(issuer, paramDate, maturityDate, null, scenario, 0, 0, 1, probMultiplier);
        }


        public static double GetRecoveryRate(int id)
        {
            if (id > CreditDefaultSwapCurves.NumberOfCurves)
            {
                Console.WriteLine($"Recovery Rate - Issuer {id} out of range - recovery set to 0 - called from ");
                return 0.0;
            }
            else if (!CreditDefaultSwapCurves.Curves[id].CDSdone)
            {
                Console.WriteLine($"RecoveryTable Issuer {id} not loaded - recovery set to 0 - called from ");
                return 0.0;
            }
            else
            {
                return CreditDefaultSwapCurves.Curves[id].Recovery;
            }
        }
        public static DateTime CDSRefDate(DateTime currentDate, bool isSingleNameConvention = true)
        {
            int d = currentDate.Day;
            int m = currentDate.Month + (d >= 20 ? 1 : 0);
            int y = currentDate.Year;

            int m2, y2;

            if (isSingleNameConvention)
            {
                m2 = 3 + 3 * ((m - 1) / 3);
                y2 = y;
            }
            else
            {
                switch (m)
                {
                    case int n when (n >= 1 && n <= 3) || n == 13:
                        m2 = 12;
                        y2 = y - 1;
                        break;
                    case int n when n >= 4 && n <= 9:
                        m2 = 6;
                        y2 = y;
                        break;
                    case int n when n >= 10 && n <= 12:
                        m2 = 12;
                        y2 = y;
                        break;
                    default:
                        throw new ArgumentException("Invalid month");
                }
            }

            return new DateTime(y2, m2, 20);
        }
    }


}
