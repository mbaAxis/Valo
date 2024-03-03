﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Excel = Microsoft.Office.Interop.Excel;
using Microsoft.VisualBasic;
using MathNet.Numerics.LinearAlgebra.Factorization;
using Microsoft.Office.Interop.Excel;
using Microsoft.VisualBasic.Devices;
using System.Net;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices.ComTypes;
using System.Diagnostics.Eventing.Reader;

namespace ValoLibrary
{
    public class StrippingIRS
    {

        public struct IRCurve
        {
            public DateTime ParamDate;
            public string CurveName;
            public int NDates;
            public int SwapBasis;
            public int SwapPeriod;
            public string[] CurveDates;
            public double[] SwapRates;
            public double[] StrippedZC;
            public double[] MonthlyZC;
            public bool IsMonthlyRollZCCalculated;
            public double[] MonthlyRollZC;
            public double FXRate;
        }

        public struct IRCurveList
        {
            public int NumberOfCurves;
            public bool LastError;
            public IRCurve[] Curves;
        }

        public static IRCurveList InterestRateCurves = new IRCurveList();

        public static int GetCurveId(object curveName)
        {
            if (Utils.IsNumeric(curveName))
            {
                int curveId = Convert.ToInt32(curveName);
                if (curveId >= 1 && curveId <= InterestRateCurves.NumberOfCurves)
                {
                    InterestRateCurves.LastError = false;
                    return curveId;
                }
                else
                {
                    return -1;
                }
            }

            for (int i = 1; i <= InterestRateCurves.NumberOfCurves; i++)
            {
                if (string.Equals(InterestRateCurves.Curves[i].CurveName, curveName.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    InterestRateCurves.LastError = false;
                    return i;
                }
            }

            return -1;
        }

        //public static double VbaGetRiskFreeZC(DateTime paramDate, string maturityDate,
        //    double[] ZC, string[] ZCDate)
        //{
        //    DateTime maturityDateX = UtilityDates.ConvertDate(paramDate, maturityDate);

        //    if (maturityDateX < paramDate)
        //    {
        //        return 1.0;
        //    }

        //    double lastZC = 1.0;
        //    DateTime lastDate = paramDate;

        //    for (int dateCounter = 0; dateCounter < ZC.Length; dateCounter++)
        //    {
        //        if (!string.IsNullOrEmpty(ZCDate[dateCounter]) && ZC[dateCounter] != 0)
        //        {
        //            DateTime nextDate = UtilityDates.ConvertDate(paramDate, ZCDate[dateCounter]);

        //            if (nextDate >= maturityDateX)
        //            {
        //                return lastZC * Math.Pow(ZC[dateCounter] / lastZC, (maturityDateX - lastDate).TotalDays / (nextDate - lastDate).TotalDays);
        //            }
        //            else
        //            {
        //                lastZC = ZC[dateCounter];
        //                lastDate = nextDate;
        //            }
        //        }
        //    }

        //    // Extrapolate at flat rate
        //    return Math.Pow(lastZC, (maturityDateX - paramDate).TotalDays / (lastDate - paramDate).TotalDays);
        //}

        public static double VbaGetRiskFreeZC(DateTime paramDate, string maturityDate, double[] ZC, string[] ZCDate)
        {
            DateTime maturityDateX = UtilityDates.ConvertDate(paramDate, maturityDate);

            if (maturityDateX < paramDate)
            {
                return 1.0;
            }

            double lastZC = 1.0;
            DateTime lastDate = paramDate;

            for (int dateCounter = 1; dateCounter < ZC.Length; dateCounter++)
            {
                if (ZCDate[dateCounter] != "" && ZCDate[dateCounter] != null && ZC[dateCounter] != 0)
                {
                    DateTime nextDate = UtilityDates.ConvertDate(paramDate, ZCDate[dateCounter]);

                    if (nextDate >= maturityDateX)
                    {
                        return lastZC * Math.Pow(ZC[dateCounter] / lastZC, (maturityDateX - lastDate).Days / (nextDate - lastDate).Days);
                    }
                    else
                    {
                        lastZC = ZC[dateCounter];
                        lastDate = nextDate;
                    }
                }
            }

            // Extrapoler à un taux constant
            return Math.Pow(lastZC, (maturityDateX - paramDate).Days / (lastDate - paramDate).Days);
        }


        public static bool VbaStoreZC(DateTime paramDate, string curveName,
            int swapBasis, int swapPeriod, string[] curveDates, double[] swapRates,
            double[] strippedZC, double fxSpot)
        {
            int curveId;

            int nDates = curveDates.Length;

            if (nDates != swapRates.Length)
            {
                string errorMessage = $"Curve Date and Swap Rates arrays do not have the same length. Fail to store curve {curveName}";
                Console.WriteLine(errorMessage);
                return false;
            }

            InterestRateCurves.LastError = false;

            curveId = GetCurveId(curveName);
            Console.WriteLine(curveId);
            if (curveId == -1)
            {
                // add a new curvee
                InterestRateCurves.NumberOfCurves++;
                Array.Resize(ref InterestRateCurves.Curves, InterestRateCurves.NumberOfCurves+1);
                curveId = InterestRateCurves.NumberOfCurves;
            }

            

            // Store the data
            //InterestRateCurves.Curves = new IRCurve[nDates+1];

            InterestRateCurves.Curves[curveId].CurveName = curveName?.ToString()?.ToUpper();
            InterestRateCurves.Curves[curveId].ParamDate = paramDate;
            InterestRateCurves.Curves[curveId].SwapBasis = swapBasis;
            InterestRateCurves.Curves[curveId].SwapPeriod = swapPeriod;
            InterestRateCurves.Curves[curveId].NDates = nDates;
            InterestRateCurves.Curves[curveId].FXRate = fxSpot;

            //InterestRateCurves.Curves(CurveID).CurveDates = CurveDates
            InterestRateCurves.Curves[curveId].CurveDates = new string[nDates];
            InterestRateCurves.Curves[curveId].SwapRates = new double[nDates];
            InterestRateCurves.Curves[curveId].StrippedZC = new double[nDates];

            int months = 0;
            for (int i = 0; i < nDates; i++)
            {
                InterestRateCurves.Curves[curveId].CurveDates[i] = curveDates[i];
                if (curveDates[i] != "")
                {
                    //months = (int)UtilityDates.MonthPeriod($"{i - 1}M", InterestRateCurves.Curves[curveId].CurveDates[i]);
                    months =(int)UtilityDates.MonthPeriod(curveDates[i]);
                }
                InterestRateCurves.Curves[curveId].SwapRates[i] = swapRates[i];
                InterestRateCurves.Curves[curveId].StrippedZC[i] = strippedZC[i];
            }
            Array.Resize(ref InterestRateCurves.Curves[curveId].MonthlyZC, months + 1);


            //InterestRateCurves.Curves[curveId].MonthlyZC = new double[months + 1];
            InterestRateCurves.Curves[curveId].MonthlyZC[1] = 1;

            for (int i = 2; i < months + 1; i++)
            {
                InterestRateCurves.Curves[curveId].MonthlyZC[i] = VbaGetRiskFreeZC(paramDate, $"{i - 1}M", InterestRateCurves.Curves[curveId].StrippedZC, InterestRateCurves.Curves[curveId].CurveDates);
            }

            InterestRateCurves.Curves[curveId].IsMonthlyRollZCCalculated = false;
            return true;
        }


        public static double[] VbaGetSwapPVandDerivatives(DateTime paramDate, double[] floatingLeg, double[] fixedLeg, double[] riskFreeZC,
            int previousCurvePoint, int curvePointCounter, double rate, int previousCalcMonth, int nextCalcMonth, int swapPeriod, int swapBasis, WorksheetFunction wsf
            )
        {
            double currentZC, fwdZC, nextZC;
            double[] res = new double[2];
            int y, m, d;
            DateTime nextDate, previousDate;
            int couponMonth, countCoupon;
            double fixedLegDerivatives;

            y = paramDate.Year;
            m = paramDate.Month;
            d = paramDate.Day;

            previousDate = DateAndTime.DateSerial(y, m + previousCalcMonth, d);
            
            currentZC = riskFreeZC[previousCurvePoint];
            nextZC = riskFreeZC[curvePointCounter];

            fixedLegDerivatives = 0;
            countCoupon = 0;

            floatingLeg[curvePointCounter] = 1 - nextZC;
            fixedLeg[curvePointCounter] = fixedLeg[previousCurvePoint];

            fwdZC = Math.Pow((double) (nextZC / currentZC), swapPeriod / (double) (nextCalcMonth - previousCalcMonth));

            int i;

            for (i = previousCalcMonth + swapPeriod; i <= nextCalcMonth; i = i + swapPeriod)
            {
                couponMonth = i;
                countCoupon = countCoupon + 1;
                currentZC = currentZC * fwdZC;
                nextDate = DateAndTime.DateSerial(y, m + couponMonth, d);

                double fixedLegInc = wsf.YearFrac(previousDate, nextDate, swapBasis);

                fixedLegInc = fixedLegInc * currentZC;
                fixedLeg[curvePointCounter] = fixedLeg[curvePointCounter] + fixedLegInc;
                fixedLegDerivatives = fixedLegDerivatives + fixedLegInc * countCoupon;
                previousDate = nextDate;
            }
  
            res[0] = floatingLeg[curvePointCounter] - rate * fixedLeg[curvePointCounter];
            res[1] = -1 - rate * fixedLegDerivatives / (double) swapPeriod / (double) nextZC;

            return res;
        }
        
        public static bool VbaComputeMonthlyRiskyZC(string curveName, DateTime paramDate, DateTime cdsRollDate)
        {
            int months;
            int curveId = GetCurveId(curveName);

            if (curveId == -1)
            {
                if (InterestRateCurves.LastError == false)
                {
                    Console.WriteLine($"Curve {curveName} was not stripped.");
                    InterestRateCurves.LastError = true;
                }

                return false;
            }

            DateTime testDate;
            int zcCdsDateOffset = 0;

            if (cdsRollDate > paramDate)
            {
                do
                {
                    zcCdsDateOffset -= 3;
                    testDate = UtilityDates.ConvertDate(cdsRollDate, zcCdsDateOffset + "m");
                } while (testDate <= paramDate);
            }

            months = InterestRateCurves.Curves[curveId - 1].MonthlyZC.Length - 1;
            Array.Resize(ref InterestRateCurves.Curves[curveId - 1].MonthlyRollZC, months);

            int cdsRollDateYear = cdsRollDate.Year;
            int cdsRollDateMonth = cdsRollDate.Month;
            int cdsRollDateDay = cdsRollDate.Day;

            for (int i = zcCdsDateOffset; i < months; i++)
            {
                DateTime zcDate = DateAndTime.DateSerial(cdsRollDateYear, cdsRollDateMonth + i, cdsRollDateDay);

                if (zcDate <= paramDate)
                {
                    zcDate = paramDate;
                    InterestRateCurves.Curves[curveId - 1].MonthlyRollZC[i] = 1;
                }
                else
                {
                    InterestRateCurves.Curves[curveId - 1].MonthlyRollZC[i] =
                        VbaGetRiskFreeZC(paramDate, zcDate+"",
                            InterestRateCurves.Curves[curveId - 1].StrippedZC,
                            InterestRateCurves.Curves[curveId - 1].CurveDates);
                }
            }

            InterestRateCurves.Curves[curveId - 1].IsMonthlyRollZCCalculated = true;
            return true;
        }
        public static double GetFXSpot(object curveName)
        {
            int curveId = GetCurveId(curveName);

            if (curveId == -1)
            {
                if (InterestRateCurves.LastError == false)
                {
                    Console.WriteLine($"Curve {curveName} was not stripped.");
                    InterestRateCurves.LastError = true;
                }

                return 1;
            }

            return InterestRateCurves.Curves[curveId].FXRate;
        }
        
        public static double[] StripZC(DateTime paramDate, string curveName, double[] curve,
            string[] curveMaturity, int swapPeriod, int swapBasis, double fxSpot)
        {
            var excel = new Application();
            WorksheetFunction wsf = excel.WorksheetFunction;

            double nextRiskFreeZC;
            double[] floatingLeg, fixedLeg, riskFreeZC;
            double rate;
            int curvePointNumber, curvePointCounter, previousCurvePoint;
            DateTime previousDate;
            int previousCalcMonth, nextCalcMonth;
            string nextMaturity;

            curvePointNumber = curve.Length;
            if (curveMaturity.Length != curvePointNumber)
            {
                Console.WriteLine("Rate Curve and Curve Maturity do not contain the same number of data.");
                return null;
            }

            riskFreeZC = new double[curvePointNumber + 1];
            floatingLeg = new double[curvePointNumber + 1];
            fixedLeg = new double[curvePointNumber + 1];

            riskFreeZC[0] = 1;
            floatingLeg[0] = 0;
            fixedLeg[0] = 0;

            previousCurvePoint = 0;
            previousCalcMonth = 0;
            previousDate = paramDate;

            int i;

            for (i = 0; i < curvePointNumber; i=i+1)
            {
                curvePointCounter = i;
                rate = curve[curvePointCounter];

                if (!double.IsNaN(rate) && !double.IsInfinity(rate))
                {
                    nextMaturity = curveMaturity[curvePointCounter];
                    if (nextMaturity == "" || nextMaturity == null)
                    {
                        Console.WriteLine($"\"Maturity undefined in CurveMaturity argument nb {curvePointCounter}");
                        return null;
                    }


                    nextCalcMonth = (int)UtilityDates.MonthPeriod(nextMaturity);

                    if (nextCalcMonth % 12 != 0)
                    {
                        Console.WriteLine("The Interest Rate Stripping Function can only take points at multiples of 12 months.");
                        return null;
                    }

                    DateTime nextDate = UtilityDates.ConvertDate(paramDate, nextMaturity);
                    DateTime nextMaturityDate;

                    if (DateTime.TryParse(nextMaturity, out nextMaturityDate))
                    {

                        if (nextMaturityDate <= previousDate)
                        {
                            Console.WriteLine("Interest Rate Stripping Function: rate curve dates in input must be sorted.");
                            return null;
                        }
                    }

                    if (previousCalcMonth == 0)
                    {
                        nextRiskFreeZC = 1.0;
                    }
                    else
                    {
                        nextRiskFreeZC = Math.Pow(riskFreeZC[previousCurvePoint], (nextDate - paramDate).Days / (double) (previousDate - paramDate).Days);

                    }

                    do
                    {
                        riskFreeZC[curvePointCounter+1] = nextRiskFreeZC;
                        var swapPvAndDerivatives = VbaGetSwapPVandDerivatives(paramDate, floatingLeg, fixedLeg, riskFreeZC,
                            previousCurvePoint, curvePointCounter+1, rate, previousCalcMonth, nextCalcMonth, swapPeriod, swapBasis, wsf
                           );
                        var adjustRiskFreeZC = -swapPvAndDerivatives[0] / swapPvAndDerivatives[1];
                        nextRiskFreeZC += adjustRiskFreeZC;

                        if (Math.Abs(swapPvAndDerivatives[0]) < 0.000001)
                        {
                            break;
                        }
                    } while (true);
                    riskFreeZC[curvePointCounter+1] = nextRiskFreeZC;

                    if (nextRiskFreeZC > riskFreeZC[previousCurvePoint])
                    {
                        Console.WriteLine($"Negative Forward rate at {nextMaturity} in range.");
                        Console.WriteLine("Continue to compute anyway");
                    }

                    previousCurvePoint = curvePointCounter+1;
                    previousCalcMonth = nextCalcMonth;
                    previousDate = nextDate;
                }
                else
                {
                    riskFreeZC[curvePointCounter+1] = double.NaN;
                }
            }

            double[] res = new double[curvePointNumber];

            int j;
            for (j = 1; j < curvePointNumber + 1; j = j + 1)
            {
                res[j-1] = riskFreeZC[j];
            }

            if (VbaStoreZC(paramDate, curveName, swapBasis, swapPeriod, curveMaturity, curve, riskFreeZC, fxSpot))
            {
                return res;
            }
            else
            {
                Console.WriteLine($"Problem while storing Curves {curveName}");
                return null;
            }

        }


        public static double GetYearFraction(DateTime startDate, DateTime endDate, int swapBasis)
        {
            // Calculer la différence en jours entre les deux dates
            int daysDifference = (endDate - startDate).Days;

            switch (swapBasis)
            {
                case 0:
                    // Act/360 : 30E/360
                    //return daysDifference / 360.5;
                    return daysDifference / 365.0;
                case 1:
                    // Act/365 : 30E/365
                    return daysDifference / 365.25;
                default:
                    throw new ArgumentException("SwapBasis non pris en charge.");
            }
        }
        public static double GetZC(string maturityDate, string curveName)
        {
            int curveID = GetCurveId(curveName);

            if (curveID == -1)
            {
                if (!InterestRateCurves.LastError)
                {
                    Console.WriteLine($"Curve {curveName} was not stripped - Called from");
                    InterestRateCurves.LastError = true;
                }
                return 0.0; // You may want to adjust the return value based on your requirements
            }

            string[] zcDates = InterestRateCurves.Curves[curveID - 1].CurveDates;
            double[] zc = InterestRateCurves.Curves[curveID - 1].StrippedZC;
            DateTime paramDate = InterestRateCurves.Curves[curveID - 1].ParamDate;

            return VbaGetRiskFreeZC(paramDate, maturityDate, zc, zcDates);
        }
        public static string GetCurveName(int curveId)
        {
            if (curveId >= 1 || curveId <= InterestRateCurves.NumberOfCurves)
            {
                InterestRateCurves.LastError = false;
                return InterestRateCurves.Curves[curveId - 1].CurveName;
            }
            else
            {
                return "Curve ID missing";
            }
        }


    }
}