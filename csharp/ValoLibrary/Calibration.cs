using Microsoft.Office.Interop.Excel;
using System;

namespace ValoLibrary
{
    public class Calibration
    {
        public static double GetRepo(string underlying, double T)
        {
            var data = GetData.Data(underlying);
            var repoRates = data["riskFreeRates"];

            var ts = PosMaturitiesToInterpol(underlying, T);
            var i_t1 = ts[0];
            var i_t2 = ts[1];
            var t1 = Convert.ToDouble(repoRates.GetValue(i_t1, 1));
            var t2 = Convert.ToDouble(repoRates.GetValue(i_t2, 1));
            var r1 = Convert.ToDouble(repoRates.GetValue(i_t1, 2));
            var r2 = Convert.ToDouble(repoRates.GetValue(i_t2, 2));
            return StatisticFormulas.LinearInterpol(T, t1, t2, r1, r2);
        }

        public static double GetDividend(string underlying, double T)
        {
            var data = GetData.Data(underlying);
            var dividends = data["dividends"];

            var ts = PosMaturitiesToInterpol(underlying, T);
            var i_t1 = ts[0];
            var i_t2 = ts[1];
            //add

            double t1 = Convert.ToDouble(dividends.GetValue(i_t1, 1));
            double t2 = Convert.ToDouble(dividends.GetValue(i_t2, 1));
            double d1 = Convert.ToDouble(dividends.GetValue(i_t1, 2));
            double d2 = Convert.ToDouble(dividends.GetValue(i_t2, 2));
            return StatisticFormulas.LinearInterpol(T, t1, t2, d1, d2);
        }

        public static int[] PosStrikesToInterpol(string underlying, double k)
        {
            //var exampleIndex = "FTSE";
            var strikes = GetData.Data(underlying)["strikes"];
            int i = 1;
            while (System.Convert.ToDouble(strikes.GetValue(1, i)) < k)
            {
                i = i + 1;
            }
            int[] Ks = new int[2];
            Ks[0] = i - 1;
            Ks[1] = i;
            return Ks;
        }
        public static int[] PosMaturitiesToInterpol(string underlying, double T)
        {
            var maturities = GetData.Data(underlying)["maturities"];
            int i = 1;
            //while (System.Convert.ToDouble(maturities.GetValue(i, 1)) < T)

            while (System.Convert.ToDouble(maturities.GetValue(i, 1)) < T)
            {
                i = i + 1;
            }
            int[] Ts = new int[2];
            Ts[0] = i - 1;
            Ts[1] = i;
            return Ts;
        }
        public static double InterpolatePrice(double K, double T, string underlying)
        {
            var posStrikesInterval = PosStrikesToInterpol(underlying, K);
            var posMaturitiesInterval = PosMaturitiesToInterpol(underlying, T);
            var data = GetData.Data(underlying);
            var prices = data["prices"];
            var maturities = data["maturities"];
            var strikes = data["strikes"];
            int i1_k = posStrikesInterval[0];
            int i2_k = posStrikesInterval[1];
            int i1_t = posMaturitiesInterval[0];
            int i2_t = posMaturitiesInterval[1];

            var k1 = Convert.ToDouble(strikes.GetValue(1, i1_k));
            var k2 = Convert.ToDouble(strikes.GetValue(1, i2_k));
            var t1 = Convert.ToDouble(maturities.GetValue(i1_t, 1));
            var t2 = Convert.ToDouble(maturities.GetValue(i2_t, 1));

            double p1 = StatisticFormulas.LinearInterpol(K, k1, k2, Convert.ToDouble(prices.GetValue(i1_t, i1_k)),
                Convert.ToDouble(prices.GetValue(i1_t, i2_k)));
            double p2 = StatisticFormulas.LinearInterpol(K, k1, k2, Convert.ToDouble(prices.GetValue(i2_t, i1_k)),
                Convert.ToDouble(prices.GetValue(i2_t, i2_k)));
            double interpolatedPrice = StatisticFormulas.LinearInterpol(T, t1, t2, p1, p2);

            return interpolatedPrice;
        }
    }
}
