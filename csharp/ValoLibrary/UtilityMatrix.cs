using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Excel = Microsoft.Office.Interop.Excel;


namespace ValoLibrary
{
    public class UtilityMatrix
    {
        public static double[] MatrixTimesVector(double[,] l, double[] x, int NStart, int NEnd)
        {
            double[] y = new double[NEnd + 1];

            for (int i = NStart; i <= NEnd; i++)
            {
                double Z = 0;
                for (int j = NStart; j <= i; j++)
                {
                    Z += l[i, j] * x[j];
                }
                y[i] = Z;
            }

            return y;
        }

        public static double[,] Choleski(double[,] S, int NStart, int NEnd)
        {
            double[,] l = new double[NEnd + 1, NEnd + 1];
            double x = Math.Sqrt(S[NStart, NStart]);
            l[NStart, NStart] = x;

            for (int i = NStart + 1; i <= NEnd; i++)
            {
                x = S[i, NStart] / l[NStart, NStart];
                l[i, NStart] = x;
            }

            for (int i = NStart + 1; i <= NEnd; i++)
            {
                x = S[i, i];
                for (int j = NStart; j <= i; j++)
                {
                    x -= l[i, j] * l[i, j];
                }

                if (x < 0)
                {
                    Console.WriteLine("Matrix non positive - Called From ");
                    // You might want to handle this error in an appropriate way in your application
                }

                x = Math.Sqrt(x);
                l[i, i] = x;

                for (int j = i + 1; j <= NEnd; j++)
                {
                    x = S[j, i];
                    for (int k = NStart; k <= i - 1; k++)
                    {
                        x -= l[i, k] * l[j, k];
                    }
                    x /= l[i, i];
                    l[j, i] = x;
                }
            }

            return l;
        }
    }
}
