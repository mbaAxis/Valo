using System;
using System.Runtime.InteropServices;
namespace ValoLibrary
{
    [ComVisible(true)]

    public interface IUDF
    {
        double CallPrice(double K, double T, string underlying);
        double PutPrice(double K, double T, string underlying);
        double test();
        double Sensibility(double K, double T, string underlying);
    }
    [ComVisible(true)]
    [ProgId("ValoLibrary.UDF")]
    [Guid("122D3198-A973-4DCF-85A3-8A4C81C12733")]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class UDF : IUDF
    {          
        public double test() 
        { 
            return 7;
        }

        public double Sensibility(double K, double T, string underlying)
        {
            double S = GetData.GetSpot(underlying);
            double r = Calibration.GetRepo(underlying, T);
            double date = GetData.GetTime(underlying);
            double timeToMaturity = (T - date) / 360;
            double price = Calibration.interpolatePrice(K, T, underlying);
            double vol = Math.Abs(BlackScholes.ImpliedVol('c', S, price, r, K, timeToMaturity));
            double sensitivity = BlackScholes.Vega('c',S,vol,r,K, timeToMaturity);
            return sensitivity;
        }
        public double CallPrice(double K, double T, string underlying)
        {
            double S =  GetData.GetSpot(underlying);
            double r = Calibration.GetRepo(underlying, T);
            double date =  GetData.GetTime(underlying);
            double timeToMaturity = (T - date)/360;
            double price = Calibration.interpolatePrice(K, T,underlying);
            double vol = BlackScholes.ImpliedVol('c', S, price, r, K, timeToMaturity);
            double P = BlackScholes.Price('c', S, vol, r, K, timeToMaturity);
            return P;
        }
        public double PutPrice(double K, double T, string underlying)
        {
            double S = GetData.GetSpot(underlying);
            double r = Calibration.GetRepo(underlying, T);
            double date = GetData.GetTime(underlying);
            double timeToMaturity = (T - date) / 360;
            double price = Calibration.interpolatePrice(K, T, underlying);
            double vol = BlackScholes.ImpliedVol('p', S, price, r, K, timeToMaturity);
            double P = BlackScholes.Price('p', S, vol, r, K, timeToMaturity);
            return P;
        }
    }
}
