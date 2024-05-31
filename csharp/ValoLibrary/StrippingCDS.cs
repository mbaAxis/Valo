
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
using System.Runtime.CompilerServices;



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


        private static double[,] RiskyZC;
        private static double[,] cdsFloatingLeg;
        private static double[,] CDSCouponLeg;
        private static double[,] AmericanLessEuropeanLeg;
        private static double[,] FullDefaultProb;



        public static int GetCDSCurveId(string CDSName)
        {


            if (LastCDSCurveID >= 1 && LastCDSCurveID <= CreditDefaultSwapCurves.NumberOfCurves)
            {
                // Testez si CDSName est égal à la dernière recherche
                if (string.Equals(CreditDefaultSwapCurves.Curves[LastCDSCurveID - 1].CDSName, CDSName, StringComparison.OrdinalIgnoreCase))
                {
                    CreditDefaultSwapCurves.LastError = false;
                    return LastCDSCurveID-1;//MODIF JTD
                }
            }

            if (CreditDefaultSwapCurves.Curves != null && CreditDefaultSwapCurves.Curves.Length > CreditDefaultSwapCurves.NumberOfCurves)
            {
                CreditDefaultSwapCurves.NumberOfCurves = CreditDefaultSwapCurves.Curves.Length;
            }


            for (int i = 0; i < CreditDefaultSwapCurves.NumberOfCurves; i++)
            {
                if (CreditDefaultSwapCurves.Curves != null && i<CreditDefaultSwapCurves.Curves.Length)
                {
                    if (string.Equals(CreditDefaultSwapCurves.Curves[i].CDSName, CDSName, StringComparison.OrdinalIgnoreCase))
                    {
                        LastCDSCurveID = i;//i+1 initialement sinon incrément d'un en trop
                        CreditDefaultSwapCurves.LastError = false;
                        return LastCDSCurveID;
                    }
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

            //add

            //CreditDefaultSwapCurves.NumberOfCurves++;   
            //CurveID = CreditDefaultSwapCurves.Curves.Length - 1;

            // end add


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


            CDSRollDateOffset = MonthlyDP.GetLowerBound(0) - 3;

            CreditDefaultSwapCurves.Curves[CurveID].MonthlyDPandShocked = new double[121 - CDSRollDateOffset, 2];

            for (i = 0; i <= 120 - CDSRollDateOffset; i++)
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
            double[,]   defaultProb = CreditDefaultSwapCurves.Curves[issuerId].MonthlyDPandShocked;


            DateTime testDate;
            offset = 0;
            if (cdsRollDate > paramDate)
            {
                do
                {
                    offset -= 3;
                    testDate = UtilityDates.ConvertDate(cdsRollDate, offset + "m");
                } while (testDate > paramDate);
            }

            //offset = defaultProb.GetLowerBound(0);

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

                prob = defaultProb[dateCounter - offset, scenario]; // offset update

                if(prob!=null && !double.IsNaN(prob))
                {
                    nextProbNoDef = 1.0 - prob;

                    if (nextDate > maturityDateX)
                    {
                        getdefaultprobabilitywithoutquanto = 1.0 - previousProbNoDef *
                            Math.Pow(nextProbNoDef / (double) previousProbNoDef, (maturityDateX - previousDate).Days / (double) (nextDate - previousDate).Days);

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
                            return getdefaultprobabilitywithoutquanto * probMultiplier;
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
                Math.Pow((previousProbNoDef / (double) prevPreviousProbNoDef), ((maturityDateX - prevPreviousDate).Days / (double) (previousDate - prevPreviousDate).Days));
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

        public static double GetCDS_PV(DateTime paramDate, DateTime CDSRollDate, double[] ZC, int PreviousCalcMonth, int NextCalcMonth,
            double LossRate, double CDSSpread, int Scenario, int zcCdsDateOffset)
        {
            double DailyProbNoDefIncrease, ProbNoDefaultPreviousMonth, ProbNoDefaultNextMonth;
            double InitialProbNoDefault, CurrentProbNoDefault, NextProbNoDefault;
            int CouponCounter, NextCouponMonth, MonthCounter;
            double SumOfNotionalDT;
            int y, m, d;
            DateTime NextDate, PreviousDate, InitialDate, PreviousCouponDate;
            double DT;
            int offsetzc;

            offsetzc = 1 * 0;

            //ProbNoDefaultPreviousMonth = (RiskyZC[PreviousCalcMonth, Scenario] / (double) ZC[PreviousCalcMonth + offsetzc]);
            ProbNoDefaultPreviousMonth = (RiskyZC[PreviousCalcMonth - zcCdsDateOffset, Scenario] / (double) ZC[PreviousCalcMonth - zcCdsDateOffset + offsetzc]);
            //ProbNoDefaultNextMonth = (RiskyZC[NextCalcMonth, Scenario] / (double) ZC[NextCalcMonth + offsetzc]); 
            ProbNoDefaultNextMonth = (RiskyZC[NextCalcMonth - zcCdsDateOffset, Scenario] / (double) ZC[NextCalcMonth - zcCdsDateOffset + offsetzc]);


            CurrentProbNoDefault = ProbNoDefaultPreviousMonth;
            InitialProbNoDefault = ProbNoDefaultPreviousMonth;

            y = CDSRollDate.Year;
            m = CDSRollDate.Month;
            d = CDSRollDate.Day;
            

            //if (PreviousCalcMonth == RiskyZC.GetLowerBound(0))
            if (PreviousCalcMonth == zcCdsDateOffset)
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
                double testvar;
                testvar= (double)(NextDate - InitialDate).Days;
                DailyProbNoDefIncrease = Math.Pow( (ProbNoDefaultNextMonth / (double) ProbNoDefaultPreviousMonth), 1.0 / (double) (NextDate - InitialDate).Days);
            }

            PreviousCouponDate = PreviousDate;
            NextCouponMonth = PreviousCalcMonth;

            for (CouponCounter = 1; CouponCounter <= (NextCalcMonth - PreviousCalcMonth) / 3.0; CouponCounter++)
            {
                //NextCouponMonth = PreviousCalcMonth + CouponCounter * 3;
                NextCouponMonth = PreviousCalcMonth + CouponCounter * 3;
                
                AmericanLessEuropeanLeg[NextCouponMonth - zcCdsDateOffset, Scenario] = AmericanLessEuropeanLeg[NextCouponMonth -zcCdsDateOffset - 3, Scenario];
                SumOfNotionalDT = 0;

                for (MonthCounter = 2; MonthCounter >= 0; MonthCounter--)
                {
                    int j = NextCouponMonth - MonthCounter;
                    NextDate = DateAndTime.DateSerial(y, m + j, d);

                    if (NextDate < paramDate)
                    {
                        NextDate = paramDate;
                    }

                    /*double totalDays = (NextDate - PreviousDate).Days;
                    DT = (int)((totalDays / 2.0 - (PreviousCouponDate - PreviousDate).Days) / 360.0);*/

                    DateTime tmp = new DateTime((long) ((NextDate.Ticks + PreviousDate.Ticks) / 2));

                    DT = ((tmp - PreviousCouponDate).Days / 360.0);

                    if (NextDate > InitialDate)
                    {
                        NextProbNoDefault = InitialProbNoDefault * Math.Pow(DailyProbNoDefIncrease, (double) (NextDate - InitialDate).Days);
                    }
                    else
                    {
                        NextProbNoDefault = 1;
                    }

                    RiskyZC[j - zcCdsDateOffset, Scenario] = NextProbNoDefault * ZC[j - zcCdsDateOffset + offsetzc];


                    if (PreviousDate == paramDate)
                    {
                        DateTime Date1 = DateAndTime.DateSerial(y, m + j - 1, d);
                        DateTime Date2 = DateAndTime.DateSerial(y, m + j , d);

                        SumOfNotionalDT += Math.Sqrt(ZC[j -zcCdsDateOffset + offsetzc] * ZC[j - zcCdsDateOffset - 1 + offsetzc]) * DT * (CurrentProbNoDefault - NextProbNoDefault);
                    }
                    else
                    {
                        SumOfNotionalDT += Math.Sqrt(ZC[j - zcCdsDateOffset + offsetzc] * ZC[j - zcCdsDateOffset - 1 + offsetzc]) * DT * (CurrentProbNoDefault - NextProbNoDefault);
                    }

                    AmericanLessEuropeanLeg[NextCouponMonth - zcCdsDateOffset, Scenario] +=
                        (ZC[j - zcCdsDateOffset + offsetzc] - RiskyZC[j - zcCdsDateOffset, Scenario]) * (1 - ZC[j - zcCdsDateOffset + offsetzc] / (double) ZC[j - zcCdsDateOffset + offsetzc - 1]);

                    PreviousDate = NextDate;
                    CurrentProbNoDefault = NextProbNoDefault;
                    FullDefaultProb[j - zcCdsDateOffset, Scenario] = 1 - NextProbNoDefault;
                    if (Double.IsInfinity(NextProbNoDefault))//MODIF ICHAK JTD
                    {
                        FullDefaultProb[j - zcCdsDateOffset, Scenario] = 1;
                    }
                    else
                    {
                        FullDefaultProb[j - zcCdsDateOffset, Scenario] = 1 - NextProbNoDefault;
                    }

                }


                cdsFloatingLeg[NextCouponMonth - zcCdsDateOffset, Scenario] =
                    ZC[NextCouponMonth - zcCdsDateOffset + offsetzc] -
                    RiskyZC[NextCouponMonth - zcCdsDateOffset, Scenario] +
                    AmericanLessEuropeanLeg[NextCouponMonth - zcCdsDateOffset, Scenario];



                NextDate = DateAndTime.DateSerial(y, m + NextCouponMonth, d);
                if (NextDate < paramDate)
                {
                    NextDate = paramDate;
                }


                CDSCouponLeg[NextCouponMonth - zcCdsDateOffset, Scenario] =
                    CDSCouponLeg[NextCouponMonth - zcCdsDateOffset - 3, Scenario] +
                    RiskyZC[NextCouponMonth - zcCdsDateOffset, Scenario] * (NextDate - PreviousCouponDate).Days / 360.0 +
                    SumOfNotionalDT;


                PreviousCouponDate = DateAndTime.DateSerial(y, m + NextCouponMonth, d);
                if (PreviousCouponDate < paramDate)
                {
                    PreviousCouponDate = paramDate;
                }
            }
            return LossRate * cdsFloatingLeg[NextCalcMonth - zcCdsDateOffset, Scenario] - CDSSpread * CDSCouponLeg[NextCouponMonth - zcCdsDateOffset, Scenario]; 
        }

        public static double GetCDS_PV_3m(DateTime paramDate, DateTime cdsRollDate, double[] zc, int previousCalcMonth,
                                           int nextCalcMonth, double lossRate, double cdsSpread, int scenario, double[] previousDefaultIntensity,
                                           double[] shiftDefaultIntensity, int zcCdsDateOffset)
        {
            double dailyProbNoDefIncrease, probNoDefaultPreviousMonth;
            double initialProbNoDefault, currentProbNoDefault, nextProbNoDefault;
            int couponCounter, nextCouponMonth, monthCounter;
            double sumOfNotionalDT;
            int y, m, d, j;
            DateTime nextDate, previousDate, initialDate, previousCouponDate;
            double dt;

            int offsetZC;

            offsetZC = 1 * 0;

            //probNoDefaultPreviousMonth = (RiskyZC[previousCalcMonth, scenario] / (double) zc[previousCalcMonth + (int)offsetZC]); 
            probNoDefaultPreviousMonth = (RiskyZC[previousCalcMonth- zcCdsDateOffset, scenario] / (double) zc[previousCalcMonth - zcCdsDateOffset + (int)offsetZC]); 
            currentProbNoDefault = probNoDefaultPreviousMonth;
            initialProbNoDefault = probNoDefaultPreviousMonth;

            double prevDefaultIntensity;
            double curDefaultIntensity;
            DateTime prevPreviousDate;
            double prevPreviousNoDefProb;

            y = cdsRollDate.Year;
            m = cdsRollDate.Month;
            d = cdsRollDate.Day;

            //if (previousCalcMonth == 0)
            if (previousCalcMonth == zcCdsDateOffset)
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
                //prevPreviousNoDefProb = (RiskyZC[previousCalcMonth - 1, scenario] / (double) zc[previousCalcMonth - 1 + (int)offsetZC]);
                prevPreviousNoDefProb = (RiskyZC[previousCalcMonth - zcCdsDateOffset - 1, scenario] / (double) zc[previousCalcMonth - zcCdsDateOffset - 1 + (int)offsetZC]);
                prevPreviousDate = DateAndTime.DateSerial(y, m + previousCalcMonth - 1, d);
                if (prevPreviousDate < paramDate) prevPreviousDate = paramDate;
                prevDefaultIntensity = -Math.Log(probNoDefaultPreviousMonth / (double) prevPreviousNoDefProb) * 360.0 / (double) (previousDate - prevPreviousDate).Days;
                if (prevDefaultIntensity < 0) prevDefaultIntensity = 0;
            }


            curDefaultIntensity = prevDefaultIntensity;
            nextDate = DateAndTime.DateSerial(y, m + nextCalcMonth, d);
            if (nextDate < paramDate)
            {
                nextDate = paramDate;
                dailyProbNoDefIncrease = 1;
            }

            previousCouponDate = previousDate;
            nextCouponMonth = previousCalcMonth;

            for (couponCounter = 1; couponCounter <= (nextCalcMonth - previousCalcMonth) / 3.0; couponCounter++)
            {
                //nextCouponMonth = previousCalcMonth + couponCounter * 3;
                nextCouponMonth = previousCalcMonth + couponCounter * 3;
                AmericanLessEuropeanLeg[nextCouponMonth - zcCdsDateOffset, scenario] = AmericanLessEuropeanLeg[nextCouponMonth - zcCdsDateOffset - 3, scenario];
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

                    /*double totalDays = (double) (nextDate - previousDate).Days;
                    dt = (int)((totalDays / 2.0 - (previousCouponDate - previousDate).Days) / 360.0);*/

                    DateTime tmp = new DateTime((long)((nextDate.Ticks + previousDate.Ticks) / 2));
                    dt = ((tmp - previousCouponDate).Days / 360.0);

                    if (nextDate > initialDate)
                    {
                        nextProbNoDefault = currentProbNoDefault * Math.Exp(-curDefaultIntensity * (nextDate - previousDate).Days / 360.0);
                    }
                    else
                    {
                        nextProbNoDefault = 1;
                    }

                    RiskyZC[j - zcCdsDateOffset, scenario] = nextProbNoDefault * zc[j - zcCdsDateOffset + (int)offsetZC];
                    if (previousDate == paramDate)
                    {

                        DateTime Date1 = DateAndTime.DateSerial(y, m + j - 1, d);
                        DateTime Date2 = DateAndTime.DateSerial(y, m + j, d);

                        sumOfNotionalDT = sumOfNotionalDT +
                            Math.Pow(zc[j - zcCdsDateOffset + (int)offsetZC] * zc[(int)j - 1 - zcCdsDateOffset + (int)offsetZC], 0.5) * dt *
                            (currentProbNoDefault - nextProbNoDefault);
                    }
                    else
                    {
                        sumOfNotionalDT = sumOfNotionalDT +
                            Math.Pow(zc[j - zcCdsDateOffset + (int)offsetZC] * zc[j - 1 - zcCdsDateOffset + (int)offsetZC], 0.5) * dt *
                            (currentProbNoDefault - nextProbNoDefault);
                    }

                    AmericanLessEuropeanLeg[nextCouponMonth - zcCdsDateOffset, scenario] =
                        AmericanLessEuropeanLeg[nextCouponMonth - zcCdsDateOffset, scenario] +
                        (zc[j - zcCdsDateOffset + (int)offsetZC] - RiskyZC[j - zcCdsDateOffset, scenario]) *
                        (1.0 - zc[j - zcCdsDateOffset + (int)offsetZC] / (double) zc[j - zcCdsDateOffset + (int)offsetZC - 1]);

                    previousDate = nextDate;
                    currentProbNoDefault = nextProbNoDefault;
                    FullDefaultProb[j - zcCdsDateOffset, scenario] = 1 - nextProbNoDefault;
                }

                cdsFloatingLeg[nextCouponMonth - zcCdsDateOffset, scenario] = zc[nextCouponMonth - zcCdsDateOffset + (int)offsetZC] - RiskyZC[nextCouponMonth - zcCdsDateOffset, scenario] + AmericanLessEuropeanLeg[nextCouponMonth - zcCdsDateOffset, scenario];

                nextDate = DateAndTime.DateSerial(y, m + nextCouponMonth, d);
                if (nextDate < paramDate)
                {
                    nextDate = paramDate;
                }

                CDSCouponLeg[nextCouponMonth - zcCdsDateOffset, scenario] = CDSCouponLeg[nextCouponMonth - zcCdsDateOffset - 3, scenario] + RiskyZC[nextCouponMonth - zcCdsDateOffset, scenario] * (nextDate - previousCouponDate).Days / 360.0 + sumOfNotionalDT;

                previousCouponDate = DateAndTime.DateSerial(y, m + nextCouponMonth, d);
                if (previousCouponDate < paramDate)
                {
                    previousCouponDate = paramDate;
                }
            }
            return lossRate * cdsFloatingLeg[nextCalcMonth - zcCdsDateOffset, scenario] - cdsSpread * CDSCouponLeg[nextCouponMonth - zcCdsDateOffset, scenario]; 
        }


        public static double[] StripDefaultProbability(int cdsID, string CDSName, DateTime ParamDate,
            DateTime CDSRollDate, double[] CDSCurve, string[] CurveMaturity,
            string CDSCurrency, double RecoveryRate, bool alterMode, string intensity)
        {
            int CurveID;
            int CDSRollDateOffset;
            int ScenarioNumber = 1;
            double[] ZC;

            CurveID = StrippingIRS.GetCurveId(CDSCurrency);

            if (CurveID == -1)
            {
                if (StrippingIRS.InterestRateCurves.LastError == false)
                {
                    Console.WriteLine($"Curve {CDSCurrency} was not stripped - Called from : ");
                    StrippingIRS.InterestRateCurves.LastError = true;
                }
                //return null;
                return new double[] { CurveID + 1000000  };
            }

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
                    //return null;
                    return new double[] { CurveID + 1000 };
                }
            }

            

            // Other variable declarations

            double CDSSpread, LossRate;
            int CDSCurvePointNumber, CurvePointCounter, PreviousCalcMonth, NextCalcMonth;

            //CurveID = Convert.ToInt32(cdsID);
            DateTime testDate;
            int zcCdsDateOffset = 0;
            if (CDSRollDate > ParamDate)
            {
                do
                {
                    zcCdsDateOffset -= 3;
                    testDate = UtilityDates.ConvertDate(CDSRollDate, zcCdsDateOffset + "m");
                } while (testDate > ParamDate);
            }

            //CDSRollDateOffset = (StrippingIRS.InterestRateCurves.Curves[CurveID].MonthlyRollZC).GetLowerBound(0);
            CDSRollDateOffset = 0;
            ZC = StrippingIRS.InterestRateCurves.Curves[CurveID].MonthlyRollZC; 

            // Initialize arrays
            RiskyZC = new double[121 - zcCdsDateOffset, 2];
            cdsFloatingLeg = new double[121 - zcCdsDateOffset, 2];
            CDSCouponLeg = new double[121 - zcCdsDateOffset, 2];
            AmericanLessEuropeanLeg = new double[121 - zcCdsDateOffset, 2];
            FullDefaultProb = new double[121 - zcCdsDateOffset, 2];


            RiskyZC[CDSRollDateOffset, 0] = 1; // initial curve
            RiskyZC[CDSRollDateOffset, 1] = 1; // shocked curve
            double dRiskyZC = -0.0001; // shock on Risky ZC used to compute the derivatives of a CDS PV against a ZC

            double[] PreviousDefaultIntensity = new double[2];
            double[] ShiftDefaultIntensity = new double[2];
            //PreviousDefaultIntensity[0] = 1;
            ShiftDefaultIntensity[0] = 0.000001; // initial and shocked curve ShiftDefaultIntensity[1]
            ShiftDefaultIntensity[1] = 0.000001; // initial and shocked curve ShiftDefaultIntensity[1]
            double dShiftDefaultIntensity = 0.000000001; // shock on Default Intensity Shift used to compute the derivatives of a CDS PV

            FullDefaultProb[CDSRollDateOffset, 0] = 0; // initial curve
            FullDefaultProb[CDSRollDateOffset, 1] = 0; // shocked curve

            // PV of the floating leg of the American CDS
            cdsFloatingLeg[CDSRollDateOffset, 0] = 0; // initial curve
            cdsFloatingLeg[CDSRollDateOffset, 1] = 0; // shocked curve

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

            //PreviousCalcMonth = CDSRollDateOffset;
            PreviousCalcMonth = CDSRollDateOffset + zcCdsDateOffset;
            ScenarioNumber = 1;
            int SpreadValueCount = 0;
            if (UtilityDates.MonthPeriod(CurveMaturity[CDSCurvePointNumber-1], CDSRollDate) > 120)
            {
                 Console.WriteLine("Not possible to calibrate risky ZC on curve with maturity beyond 10Y - Called from");
            }
            for (CurvePointCounter = 0; CurvePointCounter < CDSCurvePointNumber; CurvePointCounter++)
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

                    NextCalcMonth = (int) UtilityDates.MonthPeriod(CurveMaturity[CurvePointCounter], CDSRollDate);

                    if (UtilityDates.ConvertDate(CDSRollDate, NextCalcMonth + "M") <= ParamDate)
                    {
                        Console.WriteLine("No spread shall be given for duration lower than Parameter Date in CurveMaturity argument nb \" & CurvePointCounter & Chr(13) & \"Spread assumed to be 0\"");
                        return null;
                    }

                    SpreadValueCount++;
             
                    if (SpreadValueCount == 1 || intensity != "3M")
                    {
                        double NextRiskyZC;
                        // Newton iteration to solve for the next Risky ZC
                        for (int Scenario = 0; Scenario <= ScenarioNumber; Scenario++)
                        {

                            if (Scenario == 1)
                            {
                                double dSpread = MinSpreadShock;
                                CDSSpread += dSpread;
                                if (UtilityDates.ConvertDate(CDSRollDate, NextCalcMonth + "M") <= ParamDate)
                                {
                                    NextRiskyZC = RiskyZC[NextCalcMonth - zcCdsDateOffset, 0];
                                }
                                else
                                {    // res2 faux
                                    NextRiskyZC = RiskyZC[NextCalcMonth - zcCdsDateOffset, 0] * Math.Exp(-dSpread / (double)(1 - RecoveryRate) * NextCalcMonth / 12.0);
                                }
                            }
                            else
                            {
                                NextRiskyZC = RiskyZC[PreviousCalcMonth - zcCdsDateOffset, 0] / (double)ZC[PreviousCalcMonth - zcCdsDateOffset] * ZC[NextCalcMonth - zcCdsDateOffset];
                            }


                            int k = 0;
                            double dPV_dRiskyZC;
                            double CDS_PV;
                            do
                            {
                                RiskyZC[NextCalcMonth - zcCdsDateOffset, Scenario] = NextRiskyZC;
                                CDS_PV = GetCDS_PV(ParamDate, CDSRollDate, ZC, PreviousCalcMonth, NextCalcMonth, LossRate, CDSSpread, Scenario, zcCdsDateOffset);
                                k += 1;
                                if (CDS_PV == 0||Double.IsNaN(CDS_PV))//MODIF Ichak, jump to default
                                {
                                    break;
                                }
                               
                                RiskyZC[NextCalcMonth - zcCdsDateOffset, Scenario] = NextRiskyZC + dRiskyZC;
                                dPV_dRiskyZC = (GetCDS_PV(ParamDate, CDSRollDate, ZC, PreviousCalcMonth, NextCalcMonth, LossRate, CDSSpread, Scenario, zcCdsDateOffset) - CDS_PV) / (double) dRiskyZC;
                               
                                double AdjustRiskyZC;

                                if (CDS_PV == 0)
                                {
                                    AdjustRiskyZC = 0.0;
                                    break;
                                }
                                else
                                {
                                    
                                    AdjustRiskyZC =  (-CDS_PV / (double) dPV_dRiskyZC);
                                }

                                if (Math.Abs(CDS_PV) < 0.00001 * UtilityLittleFunctions.MinOf(1, CDSCouponLeg[NextCalcMonth - zcCdsDateOffset, Scenario])) 
                                {
                                   
                                    break;
                                }

                                NextRiskyZC += AdjustRiskyZC;
                            } while (true);
                            
                            RiskyZC[NextCalcMonth - zcCdsDateOffset, Scenario] = NextRiskyZC; 
                            CDS_PV = GetCDS_PV(ParamDate, CDSRollDate, ZC, PreviousCalcMonth, NextCalcMonth, LossRate, CDSSpread, Scenario, zcCdsDateOffset);

                            if (NextRiskyZC > (RiskyZC[PreviousCalcMonth- zcCdsDateOffset, Scenario] / (double) (ZC[PreviousCalcMonth + 1- zcCdsDateOffset] * ZC[NextCalcMonth - zcCdsDateOffset + 1])))
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
                        
                        for (int Scenario = 0; Scenario <= ScenarioNumber; Scenario++) 
                        {
                            if (Scenario == 1)
                            {
                                double dSpread = MinSpreadShock;
                                CDSSpread += dSpread;
                            }


                            double CDS_PV;
                            double dPV_dRiskyZC;
                            double AdjustShiftDefaultIntensity;

                            do
                            {                   
                                CDS_PV = GetCDS_PV_3m(ParamDate, CDSRollDate, ZC, PreviousCalcMonth, NextCalcMonth, LossRate, CDSSpread, Scenario, PreviousDefaultIntensity, ShiftDefaultIntensity, zcCdsDateOffset);


                                if (Math.Abs(CDS_PV) < 0.00001 * UtilityLittleFunctions.MinOf(1, CDSCouponLeg[NextCalcMonth - zcCdsDateOffset, Scenario]))
                                {
                                    break;
                                }

                                ShiftDefaultIntensity[Scenario] += dShiftDefaultIntensity;
                                dPV_dRiskyZC = (GetCDS_PV_3m(ParamDate, CDSRollDate, ZC, PreviousCalcMonth, NextCalcMonth, LossRate, CDSSpread, Scenario, PreviousDefaultIntensity, ShiftDefaultIntensity, zcCdsDateOffset) - CDS_PV) / (double) dShiftDefaultIntensity;

                                AdjustShiftDefaultIntensity = -CDS_PV / (double) dPV_dRiskyZC;
                                ShiftDefaultIntensity[Scenario] += AdjustShiftDefaultIntensity - dShiftDefaultIntensity;
                            } while (true);

                            CDS_PV = GetCDS_PV_3m(ParamDate, CDSRollDate, ZC, PreviousCalcMonth, NextCalcMonth, LossRate, CDSSpread, Scenario, PreviousDefaultIntensity, ShiftDefaultIntensity, zcCdsDateOffset);

                            if (FullDefaultProb[NextCalcMonth- zcCdsDateOffset, Scenario] < FullDefaultProb[NextCalcMonth - zcCdsDateOffset - 1, Scenario]) 
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

            for (int Scenario = 0; Scenario <= ScenarioNumber; Scenario++)
            {
                for (CurvePointCounter = 0; CurvePointCounter < CDSCurvePointNumber; CurvePointCounter++)
                {

                    CDSSpread = CDSCurve[CurvePointCounter];
                    if (CDSSpread != 0)
                    {
                        NextCalcMonth = (int)UtilityDates.MonthPeriod(CurveMaturity[CurvePointCounter], CDSRollDate);
                        if(Double.IsNaN(RiskyZC[NextCalcMonth - zcCdsDateOffset, Scenario])||Double.IsInfinity(RiskyZC[NextCalcMonth - zcCdsDateOffset, Scenario]))//MODIF JUMP TO DEFAULT ichak
                        {
                            Res[CurvePointCounter + CDSCurvePointNumber * Scenario] = 1.0;
                        }
                        else
                        {
                            Res[CurvePointCounter + CDSCurvePointNumber * Scenario] = 1.0 - RiskyZC[NextCalcMonth - zcCdsDateOffset, Scenario] / (double)ZC[NextCalcMonth - zcCdsDateOffset];
                        }
                    }
                    else
                    {
                        Res[CurvePointCounter + CDSCurvePointNumber * Scenario] = 0.0;
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

            return DateAndTime.DateSerial(y2, m2, 20);
        }
        
        public static string GetCDSName(int cdsID)
        {
            int CDS_ID;
            if (!UtilityDates.IsNumeric(cdsID))
            {
                CDS_ID = GetCDSCurveId(cdsID.ToString())-1; // update
            }
            else
            {
                CDS_ID =(int)cdsID;
            }

            Console.WriteLine("CDS_ID =" + CDS_ID);

            if (CDS_ID < 0 || CDS_ID > CreditDefaultSwapCurves.NumberOfCurves)
            {
                return $"CDS Name - Issuer {cdsID} out of range - called from {Environment.StackTrace}";
            }

            if (!CreditDefaultSwapCurves.Curves[CDS_ID].CDSdone)
            {
                return $"CDS Name - Issuer {cdsID} not defined - called from {Environment.StackTrace}";
            }

            return CreditDefaultSwapCurves.Curves[CDS_ID].CDSName;
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

        public static double GetDefaultProb(int issuer, string maturityDate, int scenario = 0, double probMultiplier = 1)
        {
            DateTime paramDate;
            if (!UtilityDates.IsNumeric(issuer))
            {
                issuer = GetCDSCurveId(issuer.ToString()) - 1; // update
            }


            if ((int)issuer > CreditDefaultSwapCurves.NumberOfCurves)
            {

                Console.WriteLine( $"Default Probability - Issuer {issuer} out of range - probability set to 0 - called from ");
                return 0;

            }

           
            else if (!CreditDefaultSwapCurves.Curves[(int)issuer].CDSdone)
            {
                Console.WriteLine($"Default Probability - Issuer {issuer} out of range - probability set to 0 - called from ");
                return 0;
            }

            //Console.WriteLine("paramDate avant = " + paramDate);

            paramDate = StrippingIRS.InterestRateCurves.Curves[StrippingIRS.InterestRateCurves.NumberOfCurves-1].ParamDate;

            Console.WriteLine("paramDate avant = " + paramDate);
            return GetDefaultProbabilityQuanto((int)issuer, paramDate, maturityDate, null, scenario, 0, 0, 1, probMultiplier);
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
       
    }


}
