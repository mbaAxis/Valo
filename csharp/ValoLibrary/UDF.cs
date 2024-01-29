using System;
using System.Runtime.InteropServices;

namespace ValoLibrary
{
    [ComVisible(true)]
    [Guid("839187c8-9765-4e76-a508-61ec3dd1a504")]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface IUDF
    {
        double CallPrice(double K, double T, string underlying);
        double PutPrice(double K, double T, string underlying);
        double test();
        double Sensibility(double K, double T, string underlying);
    }

    [ComVisible(true)]
    [ProgId("ValoLibrary.UDF")]
    [Guid("14041acd-3ec6-4ba1-922b-87858755d0e7")]
    [ClassInterface(ClassInterfaceType.None)]
    public class UDF : IUDF
    {
        //public object BlackScholes{ get; private set; }

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
            double sensitivity = BlackScholes.Vega('c', S, vol, r, K, timeToMaturity);
            return sensitivity;
        }
        public double CallPrice(double K, double T, string underlying)
        {
            double S = GetData.GetSpot(underlying);
            double r = Calibration.GetRepo(underlying, T);
            double date = GetData.GetTime(underlying);
            double timeToMaturity = (T - date) / 360;
            double price = Calibration.interpolatePrice(K, T, underlying);
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
