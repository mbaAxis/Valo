﻿using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Excel = Microsoft.Office.Interop.Excel;

namespace ValoLibrary
{
    //' This module contains utility functions that process dates
    //' Used by many others functions in other modules
    //' No documentation needed.
    public class UtilityDates
    {
        //Convert a period input as "2m" or "6M" or "5y" or "7Y" into a integer numberof months
        public static double MonthPeriod(string period, DateTime referenceDate = default)
        {
            double result;

            if (double.TryParse(period, out result))
            {
                return Math.Round((result - referenceDate.ToOADate()) / 365.25 * 12, 0);
            }

            if (DateTime.TryParse(period, out DateTime dateResult))
            {
                return Math.Round((dateResult.ToOADate() - referenceDate.ToOADate()) / 365.25 * 12, 0);
            }

            char datePostFix = period[period.Length - 1];

            switch (char.ToUpper(datePostFix))
            {
                case 'Y':
                    result = 12 * double.Parse(period.Substring(0, period.Length - 1));
                    break;
                case 'M':
                    result = double.Parse(period.Substring(0, period.Length - 1));
                    break;
                default:
                    Console.WriteLine($"Don't understand period");
                    result = double.NaN;
                    break;
            }

            return result;
        }

        //Convert a date under the format "3Y" or "6m" into a date as per Excel convetion
        //public static DateTime ConvertDate(DateTime paramDate, string maturityDate)
        //{
        //    int y = paramDate.Year;
        //    int m = paramDate.Month;
        //    int d = paramDate.Day;

        //    string datePostFix = maturityDate.Substring(maturityDate.Length  -1);

        //    switch (datePostFix.ToUpper())
        //    {
        //        case "Y":
        //            int years = int.Parse(maturityDate.Substring(0, maturityDate.Length - 1));
        //            return DateAndTime.DateSerial(y, m, d).AddYears(years);
        //        case "M":
        //            int months = int.Parse(maturityDate.Substring(0, maturityDate.Length - 1));
        //            return DateAndTime.DateSerial(y, m, d).AddMonths(months);
        //        default:
        //            if (!double.TryParse(maturityDate, out _))
        //            {
        //                Console.WriteLine($"Je ne comprends pas la date de maturité ({maturityDate}) : elle devrait être au format xY ou yM ou une date Excel régulière.");
        //                return DateTime.MinValue;
        //            }
        //            return DateTime.Parse(maturityDate);
        //    }
        //}


        //MODIF QUANTO

        public static DateTime ConvertDate(DateTime paramDate, object maturityDate)
        {
            int y = paramDate.Year;
            int m = paramDate.Month;
            int d = paramDate.Day;

            if (maturityDate is DateTime maturityDateTime)
            {
                return maturityDateTime;
            }

            string maturityString = maturityDate.ToString();
            string datePostFix = maturityString.Substring(maturityString.Length - 1, 1);

            switch (datePostFix.ToUpper())
            {
                case "Y":
                    return DateAndTime.DateSerial(y, m + 12 * int.Parse(maturityString.Substring(0, maturityString.Length - 1)), d);
                case "M":
                    return DateAndTime.DateSerial(y, m + int.Parse(maturityString.Substring(0, maturityString.Length - 1)), d);
                default:
                    /*if (!Utils.IsNumeric(maturityString))
                    {
                        Console.WriteLine($"Don't understand Maturity Date ({maturityString}): should be under the format xY or yM or a regular Excel Date");
                        return DateTime.MinValue; // You can change this to handle the error as needed
                    }*/
                    return DateTime.Parse(maturityString);
            }
        }

        public static double DurationYear(DateTime endDate, DateTime startDate)
        {
            double numberOfYear = 0;
            DateTime date2;
            Console.WriteLine("111111111111111111");
            do
            {
                numberOfYear++;
                date2 = DateAndTime.DateSerial((int)(startDate.Year + numberOfYear), startDate.Month, startDate.Day);
                Console.WriteLine("DurationYear startDate.Year=" + startDate.Year + " numberOfYear=" + numberOfYear + " endDate=" + endDate + " date2="+ date2);
            } while (date2 < endDate);
            Console.WriteLine("22222222222222222222");

            numberOfYear -= (date2 - endDate).Days / 365.0;

            return numberOfYear;
        }


        //' Returns a list of coupon dates according to the maturity date of a swap
        //' and the First/Last Short/Long coupon convention
        public static DateTime[] GetSwapSchedule(DateTime paramDate, String maturity, String cpnLastSettle, string cpnPeriod, string cpnConvention)
        {
  
            return SwapSchedule(paramDate, maturity, cpnLastSettle, cpnPeriod, cpnConvention);
        }


        public static DateTime[] SwapSchedule(DateTime paramDate, String maturity, String cpnLastSettle, string cpnPeriod, string cpnConvention)
        {
            double freq;
            bool isFirst, isShort, continueIf;
            DateTime lastSettle, nextDate, currentDate, maturityDate;

            freq = MonthPeriod(cpnPeriod);
            isFirst = (cpnConvention.EndsWith("First"));
            isShort = (cpnConvention.StartsWith("Short"));

            maturityDate = ConvertDate(paramDate, maturity);


            if (cpnLastSettle == null || cpnLastSettle == "" || String.IsNullOrEmpty(cpnLastSettle))
            {
                lastSettle = paramDate;
            }
            else
            {
                lastSettle = DateTime.Parse(cpnLastSettle);
            }

            if (isFirst)
            {
                freq = - freq;   // backward
                currentDate = maturityDate;
            }
            else
            {
                currentDate = lastSettle;
            }

            int numberOfDates = 0;

            do
            {
                numberOfDates++;
                currentDate = DateAndTime.DateSerial(currentDate.Year, (int) (currentDate.Month + freq), currentDate.Day); // currentDate.AddMonths((int)freq);
                nextDate = DateAndTime.DateSerial(currentDate.Year, (int)(currentDate.Month + freq), currentDate.Day); //currentDate.AddMonths((int)freq);

                if (isFirst)
                {
                    continueIf = isShort ? (currentDate > lastSettle) : (nextDate >= lastSettle);
                }
                else
                {
                    continueIf = isShort ? (currentDate < maturityDate) : (nextDate <= maturityDate);
                }
            } while (continueIf);


            DateTime[] schedule = new DateTime[numberOfDates+1];

            int j;

            if (isFirst)
            {
                currentDate = maturityDate;
            }
            else
            {
                currentDate = lastSettle;
            }
            int i;
            for (i = 1; i <= numberOfDates; i++)
            {
                if (isFirst)
                {
                    j = numberOfDates - (i - 1) ;
                    
                }
                else
                {
                    j = i;
                    currentDate = DateAndTime.DateSerial(currentDate.Year, (int)(currentDate.Month + freq), currentDate.Day);// currentDate.AddMonths((int)freq);
                    nextDate = DateAndTime.DateSerial(currentDate.Year, (int)(currentDate.Month + freq), currentDate.Day); // currentDate.AddMonths((int)freq);

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

                if (isFirst) currentDate = DateAndTime.DateSerial(currentDate.Year, (int)(currentDate.Month + freq), currentDate.Day); // currentDate.AddMonths((int)freq);                             
            }

            if (schedule[0] == paramDate)
            {
                Console.WriteLine($"Coupon Settlement Date should be parameter date as time to maturity is a multiple of {cpnPeriod}");
                return null;
            }
            else
            {
                int xxx = isShort ? Math.Abs((int)freq) : Math.Abs((int)freq) * 2;
                Console.WriteLine("xxx = " + xxx);
                DateTime comparisonDate = DateAndTime.DateSerial(schedule[0].Year, schedule[0].Month - xxx, schedule[0].Day);// schedule[0].AddMonths(-xxx);

                if (comparisonDate > lastSettle)
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

    }
}

