using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Excel = Microsoft.Office.Interop.Excel;


namespace ValoLibrary
{
    public class UtilityLittleFunctions
    {
        public static double CallSpread(double S, double k1, double k2)
        {
            if (S < k1)
            {
                return 0;
            }
            else if (S < k2)
            {
                return S - k1;
            }
            else
            {
                return k2 - k1;
            }
        }

        public static double MaxPlus(double x)
        {
            return x > 0 ? x : 0;
        }

        public static double MinOf(double x, double y)
        {
            return x < y ? x : y;
        }

        public static double MaxOf(double x, double y)
        {
            return x < y ? y : x;
        }

        public static double MaxTab(double[] table, int size)
        {
            double maxVal = table[0];

            for (int i = 1; i < size; i++)
            {
                if (table[i] > maxVal)
                {
                    maxVal = table[i];
                }
            }

            return maxVal;
        }
    }
}
