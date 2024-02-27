using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Excel = Microsoft.Office.Interop.Excel;
using Microsoft.VisualBasic;

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
            public DateTime[] CurveDates;
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

        public static string GetCurveName(int curveId)
        {
            if (curveId >= 1 && curveId <= InterestRateCurves.NumberOfCurves)
            {
                return InterestRateCurves.Curves[curveId - 1].CurveName;
            }
            else
            {
                return "Curve ID missing";
            }
        }

        public static int GetCurveId(string curveName)
        {
            if (int.TryParse(curveName, out int numericCurveId))
            {
                if (numericCurveId >= 1 && numericCurveId <= InterestRateCurves.NumberOfCurves)
                {
                    return numericCurveId;
                }
                else
                {
                    return -1;
                }
            }

            for (int i = 0; i < InterestRateCurves.NumberOfCurves; i++)
            {
                if (string.Equals(InterestRateCurves.Curves[i].CurveName, curveName, StringComparison.OrdinalIgnoreCase))
                {
                    return i + 1;
                }
            }

            return -1;
        }

        public static bool VbaStoreZC(DateTime paramDate, string curveName, int swapBasis, int swapPeriod,
            DateTime[] curveDates, double[] swapRates, double[] strippedZC, double fxSpot)
        {
            int curveId;
            int nDates = curveDates.Length;

            if (nDates != swapRates.Length)
            {
                Console.WriteLine($"Curve Date and Swap Rates arrays do not have the same length. Fail to store curve {curveName}");
                return false;
            }

            InterestRateCurves.LastError = false;
            curveId = GetCurveId(curveName);

            if (curveId == -1)
            {
                InterestRateCurves.NumberOfCurves++;
                Array.Resize(ref InterestRateCurves.Curves, InterestRateCurves.NumberOfCurves);
                curveId = InterestRateCurves.NumberOfCurves;
            }

            InterestRateCurves.Curves[curveId - 1].CurveName = curveName.ToUpper();
            InterestRateCurves.Curves[curveId - 1].ParamDate = paramDate;
            InterestRateCurves.Curves[curveId - 1].SwapBasis = swapBasis;
            InterestRateCurves.Curves[curveId - 1].SwapPeriod = swapPeriod;
            InterestRateCurves.Curves[curveId - 1].NDates = nDates;
            InterestRateCurves.Curves[curveId - 1].FXRate = fxSpot;
            InterestRateCurves.Curves[curveId - 1].CurveDates = curveDates;
            InterestRateCurves.Curves[curveId - 1].SwapRates = swapRates;
            InterestRateCurves.Curves[curveId - 1].StrippedZC = strippedZC;

            int months = 0;

            for (int i = 0; i < nDates; i++)
            {
                InterestRateCurves.Curves[curveId - 1].CurveDates[i] = curveDates[i];

                if (curveDates[i] != DateTime.MinValue && !DateTime.Equals(curveDates[i], DateTime.MinValue))
                {
                    months = (int) UtilityDates.MonthPeriod(curveDates[i]);
                }

                InterestRateCurves.Curves[curveId - 1].SwapRates[i] = swapRates[i];
                InterestRateCurves.Curves[curveId - 1].StrippedZC[i] = strippedZC[i];
            }

            Array.Resize(ref InterestRateCurves.Curves[curveId - 1].MonthlyZC, months + 1);
            InterestRateCurves.Curves[curveId - 1].MonthlyZC[0] = 1;

            for (int i = 1; i <= months; i++)
            {
                InterestRateCurves.Curves[curveId - 1].MonthlyZC[i] =
                    VbaGetRiskFreeZc(paramDate, (i - 1) + "M", InterestRateCurves.Curves[curveId - 1].StrippedZC,
                        InterestRateCurves.Curves[curveId - 1].CurveDates);
            }

            InterestRateCurves.Curves[curveId - 1].IsMonthlyRollZCCalculated = false;

            return true;
        }

        public static bool VbaComputeMonthlyRiskyZC(string curveName, DateTime paramDate, DateTime cdsRollDate)
        {
            int months;
            int curveId = GetCurveId(curveName);

            if (curveId == -1)
            {
                if (!InterestRateCurves.LastError)
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
                } while (testDate > paramDate);
            }

            months = InterestRateCurves.Curves[curveId - 1].MonthlyZC.Length - 1;
            Array.Resize(ref InterestRateCurves.Curves[curveId - 1].MonthlyRollZC, months);

            int cdsRollDateYear = cdsRollDate.Year;
            int cdsRollDateMonth = cdsRollDate.Month;
            int cdsRollDateDay = cdsRollDate.Day;

            for (int i = zcCdsDateOffset; i < months; i++)
            {
                DateTime zcDate = new DateTime(cdsRollDateYear, cdsRollDateMonth + i, cdsRollDateDay);

                if (zcDate <= paramDate)
                {
                    zcDate = paramDate;
                    InterestRateCurves.Curves[curveId - 1].MonthlyRollZC[i] = 1;
                }
                else
                {
                    InterestRateCurves.Curves[curveId - 1].MonthlyRollZC[i] =
                        VbaGetRiskFreeZc(paramDate, zcDate + "",
                            InterestRateCurves.Curves[curveId - 1].StrippedZC,
                            InterestRateCurves.Curves[curveId - 1].CurveDates);
                }
            }

            InterestRateCurves.Curves[curveId - 1].IsMonthlyRollZCCalculated = true;
            return true;
        }

        public static double[] StripZC(DateTime paramDate, string curveName, double[] curve, DateTime[] curveMaturity, int swapPeriod, int swapBasis, double fxSpot)
        {
            double nextRiskFreeZC;
            double[] floatingLeg, fixedLeg, riskFreeZC, forwardRates;
            double rate;
            int curvePointNumber, curvePointCounter, previousCurvePoint;
            DateTime previousDate, nextDate, maturityDate;
            int previousCalcMonth, nextCalcMonth;
            DateTime nextMaturity;
            char yearOrMonth;

            curvePointNumber = curve.Length;
            if (curveMaturity.Length != curvePointNumber)
            {
                Console.WriteLine("Rate Curve and Curve Maturity do not contain the same number of data.");
                return null;
            }

            riskFreeZC = new double[curvePointNumber];
            forwardRates = new double[curvePointNumber];
            floatingLeg = new double[curvePointNumber + 1];
            fixedLeg = new double[curvePointNumber + 1];

            riskFreeZC[0] = 1;
            floatingLeg[0] = 0;
            fixedLeg[0] = 0;

            previousCurvePoint = 0;
            previousCalcMonth = 0;
            previousDate = paramDate;

            for (curvePointCounter = 1; curvePointCounter <= curvePointNumber; curvePointCounter++)
            {
                rate = curve[curvePointCounter - 1];

                if (!double.IsNaN(rate) && !double.IsInfinity(rate))
                {
                    nextMaturity = curveMaturity[curvePointCounter - 1];

                    yearOrMonth = nextMaturity.ToString()[nextMaturity.ToString().Length - 1]; ;

                    if (yearOrMonth != 'y' && yearOrMonth != 'Y' && yearOrMonth != 'm' && yearOrMonth != 'M')
                    {
                        Console.WriteLine($"CurveMaturity argument should end with y, Y, m, or M at index {curvePointCounter}");
                        return null;
                    }

                    nextCalcMonth = (int) UtilityDates.MonthPeriod(nextMaturity);

                    if (nextCalcMonth % 12 != 0)
                    {
                        Console.WriteLine("The Interest Rate Stripping Function can only take points at multiples of 12 months.");
                        return null;
                    }

                    maturityDate = UtilityDates.ConvertDate(paramDate, nextMaturity);

                    if (nextMaturity <= previousDate)
                    {
                        Console.WriteLine("Interest Rate Stripping Function: rate curve dates in input must be sorted.");
                        return null;
                    }

                    if (previousCalcMonth == 0)
                    {
                        nextRiskFreeZC = 1;
                    }
                    else
                    {
                        nextRiskFreeZC = Math.Pow(riskFreeZC[previousCurvePoint], (maturityDate - paramDate).TotalDays / (previousDate - paramDate).TotalDays);
                    }

                    do
                    {
                        riskFreeZC[curvePointCounter - 1] = nextRiskFreeZC;
                        var swapPvAndDerivatives = VbaGetSwapPVandDerivatives(paramDate, floatingLeg, fixedLeg, riskFreeZC, previousCurvePoint, curvePointCounter, rate, previousCalcMonth, nextCalcMonth, swapPeriod, swapBasis);
                        var adjustRiskFreeZC = -swapPvAndDerivatives[0] / swapPvAndDerivatives[1];
                        nextRiskFreeZC += adjustRiskFreeZC;

                        if (Math.Abs(swapPvAndDerivatives[0]) < 0.000001)
                        {
                            break;
                        }
                    } while (true);

                    riskFreeZC[curvePointCounter - 1] = nextRiskFreeZC;

                    if (nextRiskFreeZC > riskFreeZC[previousCurvePoint])
                    {
                        Console.WriteLine($"Negative Forward rate at {nextMaturity} in range.");
                        Console.WriteLine("Continue to compute anyway");
                    }

                    previousCurvePoint = curvePointCounter - 1;
                    previousCalcMonth = nextCalcMonth;
                    previousDate = maturityDate;
                }
                else
                {
                    riskFreeZC[curvePointCounter - 1] = double.NaN;
                }
            }

            double[] result = new double[curvePointNumber];

            for (int i = 0; i < curvePointNumber; i++)
            {
                result[i] = riskFreeZC[i];
            }

            if (VbaStoreZC(paramDate, curveName, swapBasis, swapPeriod, curveMaturity, curve, riskFreeZC, fxSpot))
            {
                return result;
            }
            else
            {
                Console.WriteLine($"Problem while storing Curves {curveName}");
                return null;
            }
        }

        public static double GetFXSpot(string curveName)
        {
            int curveId = GetCurveId(curveName);

            if (curveId == -1)
            {
                if (!InterestRateCurves.LastError)
                {
                    Console.WriteLine($"Curve {curveName} was not stripped.");
                    InterestRateCurves.LastError = true;
                }

                return 1;
            }

            return InterestRateCurves.Curves[curveId - 1].FXRate;
        }

        public static double VbaGetRiskFreeZc(DateTime paramDate, string maturityDate, double[] zc, DateTime[] zcDate)
        {
            DateTime maturityDateX = UtilityDates.ConvertDate(paramDate, maturityDate+"");

            if (maturityDateX < paramDate)
            {
                return 1;
            }

            double lastZC = 1;
            DateTime lastDate = paramDate;

            for (int i = 0; i < zc.Length; i++)
            {
                if (!double.IsNaN(zc[i]) && zcDate[i] != null && zcDate[i] != DateTime.MinValue)
                {
                    DateTime nextDate = UtilityDates.ConvertDate(paramDate, zcDate[i]);

                    if (nextDate >= maturityDateX)
                    {
                        return lastZC * Math.Pow(zc[i] / lastZC, (maturityDateX - lastDate).TotalDays / (nextDate - lastDate).TotalDays);
                    }
                    else
                    {
                        lastZC = zc[i];
                        lastDate = nextDate;
                    }
                }
            }

            return Math.Pow(lastZC, (maturityDateX - paramDate).TotalDays / (lastDate - paramDate).TotalDays);
        }

        public static double GetZC(string maturityDate, string curveName)
        {
            int curveId = GetCurveId(curveName);

            if (curveId == -1)
            {
                if (!InterestRateCurves.LastError)
                {
                    Console.WriteLine($"Curve {curveName} was not stripped.");
                    InterestRateCurves.LastError = true;
                }

                return 0;
            }

            double[] zc = InterestRateCurves.Curves[curveId - 1].StrippedZC;
            DateTime[] zcDate = InterestRateCurves.Curves[curveId - 1].CurveDates;
            DateTime paramDate = InterestRateCurves.Curves[curveId - 1].ParamDate;

            return VbaGetRiskFreeZc(paramDate, maturityDate, zc, zcDate);
        }

        public static double[] VbaGetSwapPVandDerivatives(DateTime paramDate, double[] floatingLeg, double[] fixedLeg, double[] riskFreeZC,
            int previousCurvePoint, int curvePointCounter, double rate, int previousCalcMonth, int nextCalcMonth, int swapPeriod, int swapBasis)
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

            previousDate = DateAndTime.DateSerial(y,m+ previousCalcMonth, d);
            currentZC = riskFreeZC[previousCurvePoint];
            nextZC = riskFreeZC[curvePointCounter - 1];

            fixedLegDerivatives = 0;
            countCoupon = 0;

            floatingLeg[curvePointCounter] = 1 - nextZC;
            fixedLeg[curvePointCounter] = fixedLeg[previousCurvePoint];

            fwdZC = Math.Pow(nextZC / currentZC, swapPeriod / (nextCalcMonth - previousCalcMonth));

            for (couponMonth = previousCalcMonth + swapPeriod; couponMonth <= nextCalcMonth; couponMonth += swapPeriod)
            {
                countCoupon++;
                currentZC *= fwdZC;
                nextDate = new DateTime(y, m + couponMonth, d);

                double fixedLegIncTemp = GetYearFraction(previousDate, nextDate, swapBasis) * currentZC;
                fixedLeg[curvePointCounter] += fixedLegIncTemp;
                fixedLegDerivatives += fixedLegIncTemp * countCoupon;

                previousDate = nextDate;
            }

            res[0] = floatingLeg[curvePointCounter] - rate * fixedLeg[curvePointCounter];
            res[1] = -1 - rate * fixedLegDerivatives / swapPeriod / nextZC;

            return res;
        }

        public static double GetYearFraction(DateTime startDate, DateTime endDate, int basis)
        {
            return DateAndTime.DateDiff(DateInterval.Day, startDate, endDate, FirstDayOfWeek.Sunday, FirstWeekOfYear.Jan1) / 365.0;
        }

    }
}
