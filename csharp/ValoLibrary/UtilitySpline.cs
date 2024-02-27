using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Excel = Microsoft.Office.Interop.Excel;


namespace ValoLibrary
{
    public class UtilitySpline
    {
        public static double[] SGSpline(double[] x, int n, double[] abscissa_0, double[] ordinate_0, int Size)
        {
            double[] ordinate2 = new double[Size];
            double[] Output = new double[n + 1];

            Output[0] = 0;
            FirstSpline(abscissa_0, ordinate_0, Size, 1e30, 1e30, ordinate2);

            for (int i = 1; i <= n; i++)
            {
                double ordinate_i;
                Splinter(abscissa_0, ordinate_0, ordinate2, Size, x[i - 1], out ordinate_i);
                Output[i] = ordinate_i;
            }

            return Output;
        }

        public static void FirstSpline(double[] abscissa, double[] ordinate, int n, double suplim1, double suplimn, double[] ordinate2)
        {
            int MaxNumber = 500;
            double[] table = new double[MaxNumber];

            if (suplim1 > 9.9E+29)
            {
                ordinate2[0] = 0;
                table[0] = 0;
            }
            else
            {
                ordinate2[0] = -0.5;
                table[0] = (3 / (abscissa[1] - abscissa[0])) * ((ordinate[1] - ordinate[0]) / (abscissa[1] - abscissa[0]) - suplim1);
            }

            for (int i = 1; i < n - 1; i++)
            {
                double sigma = (abscissa[i] - abscissa[i - 1]) / (abscissa[i + 1] - abscissa[i - 1]);
                double p = sigma * ordinate2[i - 1] + 2;
                ordinate2[i] = (sigma - 1) / p;
                table[i] = (6 * ((ordinate[i + 1] - ordinate[i]) / (abscissa[i + 1] - abscissa[i]) - (ordinate[i] - ordinate[i - 1]) / (abscissa[i] - abscissa[i - 1])) / (abscissa[i + 1] - abscissa[i - 1]) - sigma * table[i - 1]) / p;
            }

            double qn;
            double un;

            if (suplimn > 9.9E+29)
            {
                qn = 0;
                un = 0;
            }
            else
            {
                qn = 0.5;
                un = (3 / (abscissa[n - 1] - abscissa[n - 2])) * (suplimn - (ordinate[n - 1] - ordinate[n - 2]) / (abscissa[n - 1] - abscissa[n - 2]));
            }

            ordinate2[n - 1] = (un - qn * table[n - 2]) / (qn * ordinate2[n - 2] + 1);

            for (int k = n - 2; k >= 0; k--)
            {
                ordinate2[k] = ordinate2[k] * ordinate2[k + 1] + table[k];
            }
        }

        public static void Splinter(double[] xa, double[] ya, double[] ordinate2a, int n, double abscissa, out double ordinate)
        {
            int k2 = 0;
            int k1 = n - 1;

            while (k1 - k2 > 1)
            {
                int k = (k1 + k2) / 2;
                if (xa[k] > abscissa)
                {
                    k1 = k;
                }
                else
                {
                    k2 = k;
                }
            }

            double h = xa[k1] - xa[k2];
            if (h == 0)
            {
                Console.WriteLine("bad xa input in splinter");
            }

            double a = (xa[k1] - abscissa) / h;
            double b = (abscissa - xa[k2]) / h;
            ordinate = a * ya[k2] + b * ya[k1] + ((Math.Pow(a, 3) - a) * ordinate2a[k2] + (Math.Pow(b, 3) - b) * ordinate2a[k1]) * (Math.Pow(h, 2)) / 6;
        }
    }
}

