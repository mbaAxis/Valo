using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Excel = Microsoft.Office.Interop.Excel;

namespace ValoLibrary
{
    public class UtilityDates
    {
        public static double MonthPeriod(dynamic period, DateTime referenceDate = default)
        {
            string datePostFix;

            if (IsNumeric(period))
            {
                return Math.Round((double)(period - referenceDate) / 365.25 * 12, 0);
            }

            if (IsDate(period))
            {
                return Math.Round((double)(period - referenceDate) / 365.25 * 12, 0);
            }

            datePostFix = period.Substring(period.Length - 1, 1);

            switch (datePostFix.ToUpper())
            {
                case "Y":
                    return 12 * double.Parse(period.Substring(0, period.Length - 1));
                case "M":
                    return double.Parse(period.Substring(0, period.Length - 1));
                default:
                    Console.WriteLine($"Don't understand period - Called from "+ datePostFix);
                    return double.NaN;
            }
        }

        public static DateTime ConvertDate(dynamic paramDate, dynamic maturityDate)
        {
            string datePostFix;
            int y, m, d;

            y = paramDate.Year;
            m = paramDate.Month;
            d = paramDate.Day;

            datePostFix = maturityDate.Substring(maturityDate.Length - 1, 1);

            switch (datePostFix.ToUpper())
            {
                case "Y":
                    return new DateTime(y, m + 12 * int.Parse(maturityDate.Substring(0, maturityDate.Length - 1)), d);
                case "M":
                    return new DateTime(y, m + int.Parse(maturityDate.Substring(0, maturityDate.Length - 1)), d);
                default:
                    if (!IsNumeric(maturityDate - paramDate))
                    {
                        Console.WriteLine($"Don't understand Maturity Date ({maturityDate}): should be under the format xY or yM or a regular Excel Date - Called from " + datePostFix);
                        return DateTime.MinValue;
                    }
                    return maturityDate;
            }
        }

        public static double DurationYear(DateTime endDate, DateTime startDate)
        {
            int numberOfYear = 0;
            DateTime date2;

            do
            {
                numberOfYear++;
                date2 = DateAndTime.DateSerial(startDate.Year + numberOfYear, startDate.Month, startDate.Day);
            } while (date2 < endDate);

            numberOfYear += - (date2 - endDate).Days / 365;

            return numberOfYear;
        }

        public static DateTime[] GetSwapSchedule(dynamic paramDate, dynamic maturity, dynamic cpnLastSettle, dynamic cpnPeriod, dynamic cpnConvention)
        {
            DateTime[] maturityDates = SwapSchedule(paramDate, maturity, cpnLastSettle, cpnPeriod, cpnConvention);
            return maturityDates;
        }

        public static DateTime[] SwapSchedule(dynamic paramDate, dynamic maturity, dynamic cpnLastSettle, dynamic cpnPeriod, dynamic cpnConvention)
        {
            int freq, numberOfDates, i, j;
            DateTime currentDate, nextDate;
            bool isFirst, isShort;
            DateTime[] schedule;

            freq = (int)MonthPeriod(cpnPeriod);
            isFirst = cpnConvention.EndsWith("First", StringComparison.OrdinalIgnoreCase);
            isShort = cpnConvention.StartsWith("Short", StringComparison.OrdinalIgnoreCase);

            DateTime maturityDate = ConvertDate(paramDate, maturity);

            DateTime lastSettle;
            if (cpnLastSettle == null || cpnLastSettle == DBNull.Value || string.IsNullOrWhiteSpace(cpnLastSettle.ToString()))
            {
                lastSettle = paramDate;
            }
            else
            {
                lastSettle = cpnLastSettle;
            }

            if (isFirst)
            {
                freq = -freq; // backward
                currentDate = maturityDate;
            }
            else
            {
                currentDate = lastSettle;
            }

            numberOfDates = 0;

            bool continueIf;

            do
            {
                numberOfDates++;
                currentDate = new DateTime(currentDate.Year, currentDate.Month + freq, currentDate.Day);
                nextDate = new DateTime(currentDate.Year, currentDate.Month + freq, currentDate.Day);

                if (isFirst)
                {
                    if (isShort)
                    {
                        continueIf = (currentDate > lastSettle);
                    }
                    else
                    {
                        continueIf = (nextDate >= lastSettle);
                    }
                }
                else
                {
                    if (isShort)
                    {
                        continueIf = (currentDate < maturityDate);
                    }
                    else
                    {
                        continueIf = (nextDate <= maturityDate);
                    }
                }
            } while (continueIf);

            schedule = new DateTime[numberOfDates + 1];
            if (isFirst)
            {
                currentDate = maturityDate;
            }
            else
            {
                currentDate = lastSettle;
            }

            for (i = 1; i <= numberOfDates; i++)
            {
                if (isFirst)
                {
                    j = numberOfDates - i + 1;
                }
                else
                {
                    j = i;
                    currentDate = new DateTime(currentDate.Year, currentDate.Month + freq, currentDate.Day);
                    nextDate = new DateTime(currentDate.Year, currentDate.Month + freq, currentDate.Day);

                    if (isShort)
                    {
                        if (currentDate > maturityDate)
                        {
                            currentDate = maturityDate;
                        }
                    }
                    else
                    {
                        if (nextDate > maturityDate)
                        {
                            currentDate = maturityDate;
                        }
                    }
                }
                schedule[j] = currentDate;
                if (isFirst) currentDate = new DateTime(currentDate.Year, currentDate.Month + freq, currentDate.Day);
            }

            if (schedule[1] == paramDate)
            {
                Console.WriteLine($"Coupon Settlement Date should be parameter date as time to maturity is a multiple of {cpnPeriod}");
                return null;
            }
            else
            {
                int xxx;

                if (isShort)
                {
                    xxx = Math.Abs(freq);
                }
                else
                {
                    xxx = Math.Abs(freq) * 2;
                }

                if (schedule[1].AddMonths(-xxx) > lastSettle)
                {
                    Console.WriteLine($"Coupon Settlement Date should be parameter date as time to maturity is a multiple of {cpnPeriod}");
                    return null;
                }
            }
            schedule[0] = lastSettle;

            return schedule;
        }

        public static bool IsNumeric(object expression)
        {
            return double.TryParse(expression.ToString(), out _);
        }

        public static bool IsDate(object expression)
        {
            return DateTime.TryParse(expression.ToString(), out _);
        }
    }
}
