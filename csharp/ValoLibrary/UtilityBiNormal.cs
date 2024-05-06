using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Excel = Microsoft.Office.Interop.Excel;

namespace ValoLibrary
{
    public class UtilityBiNormal
    {
        public static double BivariateNormalDistribution(double a1, double b1, double rho)
        {
            double denominator;
            double rholow;
            double rhohigh;

            if (rho > 0.9999)
            {
                return NormalCumulativeDistribution(Math.Min(a1, b1));
            }
            else if (rho < -0.9999)
            {
                return Math.Max(0, NormalCumulativeDistribution(a1) - NormalCumulativeDistribution(-b1));
            }
            else
            {
                if (a1 * b1 * rho <= 0)
                {
                    if (a1 <= 0 && b1 <= 0 && rho <= 0)
                    {
                        return BivariateNormalDistributionTwo(a1, b1, rho);
                    }
                    else if (a1 <= 0 && b1 * rho >= 0)
                    {
                        return NormalCumulativeDistribution(a1) - BivariateNormalDistributionTwo(a1, -b1, -rho);
                    }
                    else if (b1 <= 0 && rho >= 0)
                    {
                        return NormalCumulativeDistribution(b1) - BivariateNormalDistributionTwo(-a1, b1, -rho);
                    }
                    else
                    {
                        return NormalCumulativeDistribution(a1) + NormalCumulativeDistribution(b1) - 1 + BivariateNormalDistributionTwo(-a1, -b1, rho);
                    }
                }
                else
                {
                    denominator = Math.Sqrt(a1 * a1 - 2 * rho * a1 * b1 + b1 * b1);
                    rholow = (rho * a1 - b1) * Math.Sign(a1) / denominator;
                    rhohigh = (rho * b1 - a1) * Math.Sign(b1) / denominator;
                    return BivariateNormalDistribution(a1, 0, rholow) + BivariateNormalDistribution(b1, 0, rhohigh) - (1 - Math.Sign(a1) * Math.Sign(b1)) / 4;
                }
            }
        }

        public static double NormalCumulativeDistribution(double x)
        {
            double[] a = { 0.31938153, -0.356563782, 1.781477937, -1.821255978, 1.330274429 };
            if (x < -7)
            {
                return NormalDensityFunction(x) / Math.Sqrt(1 + x * x);
            }
            else if (x > 7)
            {
                return 1 - NormalCumulativeDistribution(-x);
            }
            else
            {
                double result = 0.2316419;
                //double T = 1 / (1 + result * Math.Abs(x));
                result = 1 / (1 + result * Math.Abs(x));
                double c=0;
                for (int i = 0; i < a.Length; i++)
                {
                    c += a[i] * Math.Pow(result, i + 1);
                }
                //result = 1 / (1 + result);
                result = 1 - NormalDensityFunction(x) *c;
                return (x <= 0) ? 1 - result : result;
            }
        }

        public static double MyNormal(double x, double Mean, double Stdev)
        {
            double y, T, xi;
            if ((x - Mean) >= 0)
            {
                xi = (x - Mean) / Stdev;
            }
            else
            {
                xi = -(x - Mean) / Stdev;
            }

            T = 1 / (1 + 0.2316419 * xi);
            y = 1 - (0.31938153 * T - 0.356563782 * T * T + 1.781477937 * T * T * T - 1.821255978 * T * T * T * T + 1.330274429 * T * T * T * T * T) * Math.Exp(-xi * xi / 2) / Math.Sqrt(2 * Math.PI);

            return (x - Mean > 0) ? y : 1 - y;
        }

        public static double NormalDensityFunction(double x)
        {
            return 0.398942280401433 * Math.Exp(-x * x * 0.5);
        }

        public static double FF(double x, double y, double at, double bt, double rho)
        {
            return Math.Exp(at * (2 * x - at) + bt * (2 * y - bt) + 2 * rho * (x - at) * (y - bt));
        }

        public static double BivariateNormalDistributionTwo(double a, double b, double rho)
        {
            double[] tableau1 = { 0.325303, 0.4211071, 0.1334425, 0.006374323 };
            double[] tableau2 = { 0.1337764, 0.6243247, 1.3425378, 2.2626645 };

            double sq = 1 / Math.Sqrt(2 * (1 - rho * rho));
            double at = a * sq;
            double bt = b * sq;

            double result = 0;

            for (int i = 0; i < tableau1.Length; i++)
            {
                for (int j = 0; j < tableau2.Length; j++)
                {
                    result += tableau1[i] * tableau1[j] * FF(tableau2[i], tableau2[j], at, bt, rho);
                }
            }

            return result * Math.Sqrt(1 - rho * rho) / Math.PI;
        }

        public static double g(double x, double y, double r, double b)
        {
            double Z = x * (x - 2 * r * y) + y * y;
            return Math.Exp(b * Z);
        }
    }
}
