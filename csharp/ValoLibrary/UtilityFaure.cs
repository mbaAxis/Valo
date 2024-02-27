using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Excel = Microsoft.Office.Interop.Excel;

namespace ValoLibrary
{
    public class UtilityFaure
    {
        public static double[,] UniformRandomize(int Iter, int Size)
        {
            double[] table = {
                -3.14159, 1, 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47,
                53, 59, 61, 67, 71, 73, 79, 83, 89, 97, 101, 103, 107, 109, 113, 127,
                131, 137, 139, 149, 151, 157, 163, 167, 173, 179, 181, 191, 193, 197, 199, 211,
                223, 227, 229, 233, 239, 241, 251, 257, 263, 269, 271, 277, 281, 283, 293, 307,
                311, 313, 317, 331, 337, 347, 349, 353, 359, 367, 373, 379, 383, 389, 397, 401,
                409, 419, 421, 431, 433, 439, 443, 449, 457, 461, 463, 467, 479, 487, 491, 499,
                503, 509, 521, 523, 541, 547, 557, 563, 569, 571, 577, 587, 593, 599, 601, 607
            };

            int First = 0;
            for (int j = 1; j <= 112; j++)
            {
                if (table[j] >= Size)
                {
                    First = (int)table[j];
                    break;
                }
            }

            int jVal = 0;
            do
            {
                jVal++;
            } while (Math.Pow(First, jVal) <= Iter);

            jVal--;

            double[,] c1 = new double[Iter + 1, jVal + 1];
            double[,] c2 = new double[Iter + 1, jVal + 1];
            double[,] c3 = new double[Iter + 1, Size + 1];

            for (int n = 1; n <= Iter; n++)
            {
                double FirstTime = Math.Pow(First, jVal);
                int Flag = n;

                for (int l = 0; l <= jVal; l++)
                {
                    c1[n, jVal - l] = (int)(Flag / FirstTime);
                    Flag = Flag % (int)FirstTime;
                    FirstTime = FirstTime / First;
                }

                c3[n, 1] = 0;
                FirstTime = 1.0 / First;

                for (int l = 0; l <= jVal; l++)
                {
                    c3[n, 1] += c1[n, l] * FirstTime;
                    FirstTime = FirstTime / First;
                }
            }

            for (int i = 2; i <= Size; i++)
            {
                for (int n = 1; n <= Iter; n++)
                {
                    for (int l = 0; l <= jVal; l++)
                    {
                        c2[n, l] = 0;

                        double sigma = 1.0;
                        int m = l;

                        c2[n, l] += sigma * c1[n, m];

                        for (m = l + 1; m <= jVal; m++)
                        {
                            sigma = sigma * m / (m - l);
                            c2[n, l] += sigma * c1[n, m];
                        }

                        c2[n, l] = c2[n, l] % First;
                    }

                    c3[n, i] = 0;
                    double FirstTime = 1.0 / First;

                    for (int m = 0; m <= jVal; m++)
                    {
                        c3[n, i] += c2[n, m] * FirstTime;
                        FirstTime = FirstTime / First;
                    }
                }

                for (int n = 1; n <= Iter; n++)
                {
                    for (int l = 0; l <= jVal; l++)
                    {
                        c1[n, l] = c2[n, l];
                    }
                }
            }

            return c3;
        }
    }
}
