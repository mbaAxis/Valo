using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValoLibrary 
{
    public class Calibration 
    {   
        public static double GetRepo(string underlying, double T)
        {
            var data = GetData.Data(underlying);
            var repoRates = data["repoRates"];
            var ts = PosMaturitiesToInterpol(T);
            var i_t1 = ts[0];
            var i_t2 = ts[1];
            var t1 = System.Convert.ToDouble(repoRates.GetValue(i_t1, 1));
            var t2 = System.Convert.ToDouble(repoRates.GetValue(i_t2, 1));
            var r1 = System.Convert.ToDouble(repoRates.GetValue(i_t1, 2));
            var r2 = System.Convert.ToDouble(repoRates.GetValue(i_t2, 2));
            return StatisticFormulas.linearInterpol(T, t1, t2, r1, r2);
        }

        public static double GetDiv(string underlying, double T)
        {
            var data = GetData.Data(underlying);
            var dividends = data["dividends"];
            var ts = PosMaturitiesToInterpol(T);
            var i_t1 = ts[0];
            var i_t2 = ts[1];
            var t1 = System.Convert.ToDouble(dividends.GetValue(i_t1, 1));
            var t2 = System.Convert.ToDouble(dividends.GetValue(i_t2, 1));
            var d1 = System.Convert.ToDouble(dividends.GetValue(i_t1, 2));
            var d2 = System.Convert.ToDouble(dividends.GetValue(i_t2, 2));
            return StatisticFormulas.linearInterpol(T, t1, t2, d1, d2);
        }

        public static int[] PosStrikesToInterpol(double k)
        {
            //var exampleIndex = "FTSE";
            var strikes = GetData.Data("FTSE")["strikes"];
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
        public static int[] PosMaturitiesToInterpol(double T)
        {

            var maturities = GetData.Data("FTSE")["maturities"];
            int i = 1;
            while (System.Convert.ToDouble(maturities.GetValue(i, 1)) < T)
            {
                i = i + 1;
            }
            int[] Ts = new int[2];
            Ts[0] = i - 1;
            Ts[1] = i;
            return Ts;
        }
        public static double interpolatePrice(double K, double T, string underlying)
        {
            var posStrikesInterval = PosStrikesToInterpol(K);
            var posMaturitiesInterval = PosMaturitiesToInterpol(T);
            var data = GetData.Data(underlying);
            var prices = data["prices"];
            var maturities = data["maturities"];
            var strikes = data["strikes"];
            int i1_k = posStrikesInterval[0];
            int i2_k = posStrikesInterval[1];
            int i1_t = posMaturitiesInterval[0];
            int i2_t = posMaturitiesInterval[1];

            var k1 = System.Convert.ToDouble(strikes.GetValue(1, i1_k));
            var k2 = System.Convert.ToDouble(strikes.GetValue(1, i2_k));
            var t1 = System.Convert.ToDouble(maturities.GetValue(i1_t, 1));
            var t2 = System.Convert.ToDouble(maturities.GetValue(i2_t, 1));

            double p1 = StatisticFormulas.linearInterpol(K, k1, k2, System.Convert.ToDouble(prices.GetValue(i1_t, i1_k)),
                System.Convert.ToDouble(prices.GetValue(i1_t, i2_k)));
            double p2 = StatisticFormulas.linearInterpol(K, k1, k2, System.Convert.ToDouble(prices.GetValue(i2_t, i1_k)),
                System.Convert.ToDouble(prices.GetValue(i2_t, i2_k)));
            double interpolatedPrice = StatisticFormulas.linearInterpol(T, t1, t2, p1, p2);

            return interpolatedPrice;
        }
    }
}
