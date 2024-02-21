using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ValoLibrary
{
    [ComVisible(true)]
    [Guid("839187c8-9765-4e76-a508-61ec3dd1a504")]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface IUDF
    {
        // Version Naive
        double BS(string callPutFlag, double S, double sigma, double r, double K, double T);
        double DeltaBS(string callPutFlag, double S, double sigma, double r, double K, double T);
        double GammaBS(string callPutFlag, double S, double sigma, double r, double K, double T);
        double VegaBS(string callPutFlag, double S, double sigma, double r, double K, double T);
        double TethaBS(string callPutFlag, double S, double sigma, double r, double K, double T);

        Dictionary<string, double> SensitivitiesBS(string callPutFlag, double S, double sigma, double r, double K, double T);


        // version Améliorée
        double CallPutPrice(string callPutFlag, double K, double T, string underlying);

        double DeltaSensi(string callPutFlag, double K, double T, string underlying);
        double GammaSensi(string callPutFlag, double K, double T, string underlying);
        double TethaSensi(string callPutFlag, double K, double T, string underlying);
        double VegaSensi(string callPutFlag, double K, double T, string underlying);
        Dictionary<string, double> Sensitivities(string callPutFlag, double K,
           double T, string underlying);



    }



    [ComVisible(true)]
    [ProgId("ValoLibrary.UDF")]
    [Guid("14041acd-3ec6-4ba1-922b-87858755d0e7")]
    [ClassInterface(ClassInterfaceType.None)]
    public class UDF : IUDF
    {
        //public object BlackScholes{ get; private set; }

        // version Naive
        public double BS(string callPutFlag, double S, double sigma, double r, double K, double T)
        {
            double P = BlackScholes.Price(callPutFlag, S, sigma, r, K, T);
            return P;
        }

        public double DeltaBS(string callPutFlag, double S, double sigma, double r, double K, double T)
        {
            double delta = BlackScholes.Delta(callPutFlag, S, sigma, r, K, T);
            return delta;
        }
        public double GammaBS(string callPutFlag, double S, double sigma, double r, double K, double T)
        {
            double gamma = BlackScholes.Gamma(callPutFlag, S, sigma, r, K, T);
            return gamma;
        }

        public double VegaBS(string callPutFlag, double S, double sigma, double r, double K, double T)
        {
            double vega = BlackScholes.Vega(callPutFlag, S, sigma, r, K, T);
            return vega;
        }

        public double TethaBS(string callPutFlag, double S, double sigma, double r, double K, double T)
        {
            double tetha = BlackScholes.Tetha(callPutFlag, S, sigma, r, K, T);
            return tetha;
        }

        public  Dictionary<string, double> SensitivitiesBS(string callPutFlag, double S, double sigma, double r, double K, double T)
        {
            Dictionary<string, double> sensitivities = new Dictionary<string, double>();

            sensitivities["DeltaBS"] = BlackScholes.Delta(callPutFlag, S, sigma, r, K, T);
            sensitivities["GammaBS"] = BlackScholes.Gamma(callPutFlag, S, sigma, r, K, T);
            sensitivities["VegaBS"] = BlackScholes.Vega(callPutFlag, S, sigma, r, K, T);
            sensitivities["ThetaBS"] = BlackScholes.Tetha(callPutFlag, S, sigma, r, K, T);

            return sensitivities;
        }




    //Version Ameliorée

        public double CallPutPrice(string callPutFlag, double K, double T, string underlying)
        {
            double S = GetData.GetSpot(underlying);
            double r = Calibration.GetRepo(underlying, T);
            double date = GetData.GetTime(underlying);
            double timeToMaturity = (T - date) / 360;
            double price = Calibration.InterpolatePrice(K, T, underlying);
            double vol = BlackScholes.ImpliedVol(callPutFlag, S, price, r, K, timeToMaturity);
            double P = BlackScholes.Price(callPutFlag, S, vol, r, K, timeToMaturity);
            return P;
        }
        //public double PutPrice(double K, double T, string underlying)
        //{
        //    double S = GetData.GetSpot(underlying);
        //    double r = Calibration.GetRepo(underlying, T);
        //    double date = GetData.GetTime(underlying);
        //    double timeToMaturity = (T - date) / 360;
        //    double price = Calibration.InterpolatePrice(K, T, underlying);
        //    double vol = BlackScholes.ImpliedVol("p", S, price, r, K, timeToMaturity);
        //    double P = BlackScholes.Price("p", S, vol, r, K, timeToMaturity);
        //    return P;
        //}
        public double VegaSensi(string callPutFlag, double K, double T, string underlying)
        {
            double S = GetData.GetSpot(underlying);
            double r = Calibration.GetRepo(underlying, T);
            double date = GetData.GetTime(underlying);
            double timeToMaturity = (T - date) / 360;
            double price = Calibration.InterpolatePrice(K, T, underlying);
            double vol = Math.Abs(BlackScholes.ImpliedVol(callPutFlag, S, price, r, K, timeToMaturity));
            double sensitivity = BlackScholes.Vega(callPutFlag, S, vol, r, K, timeToMaturity);
            return sensitivity;
        }
        public double DeltaSensi(string callPutFlag, double K, double T, string underlying)
        {
            double S = GetData.GetSpot(underlying);
            double r = Calibration.GetRepo(underlying, T);
            double date = GetData.GetTime(underlying);
            double timeToMaturity = (T - date) / 360;
            double price = Calibration.InterpolatePrice(K, T, underlying);
            double vol = Math.Abs(BlackScholes.ImpliedVol(callPutFlag, S, price, r, K, timeToMaturity));
            double sensitivity = BlackScholes.Delta(callPutFlag, S, vol, r, K, timeToMaturity);
            return sensitivity;
        }
        public double GammaSensi(string callPutFlag, double K, double T, string underlying)
        {
            double S = GetData.GetSpot(underlying);
            double r = Calibration.GetRepo(underlying, T);
            double date = GetData.GetTime(underlying);
            double timeToMaturity = (T - date) / 360;
            double price = Calibration.InterpolatePrice(K, T, underlying);
            double vol = Math.Abs(BlackScholes.ImpliedVol(callPutFlag, S, price, r, K, timeToMaturity));
            double sensitivity = BlackScholes.Gamma(callPutFlag, S, vol, r, K, timeToMaturity);
            return sensitivity;
        }
        public double TethaSensi(string callPutFlag, double K, double T, string underlying)
        {
            double S = GetData.GetSpot(underlying);
            double r = Calibration.GetRepo(underlying, T);
            double date = GetData.GetTime(underlying);
            double timeToMaturity = (T - date) / 360;
            double price = Calibration.InterpolatePrice(K, T, underlying);
            double vol = Math.Abs(BlackScholes.ImpliedVol(callPutFlag, S, price, r, K, timeToMaturity));
            double sensitivity = BlackScholes.Tetha(callPutFlag, S, vol, r, K, timeToMaturity);
            return sensitivity;
        }

        public Dictionary<string, double> Sensitivities(string callPutFlag, double K,
            double T, string underlying)
        {
            Dictionary<string, double> sensitivities = new Dictionary<string, double>();

            // Ajouter les sensibilités à la liste
            
            sensitivities["Delta"]= DeltaSensi(callPutFlag, K, T, underlying);
            sensitivities["Gamma"]= GammaSensi(callPutFlag, K, T, underlying);
            sensitivities["Vega"] = VegaSensi(callPutFlag, K, T, underlying);
            sensitivities["Theta"]= TethaSensi(callPutFlag, K, T, underlying);

            return sensitivities;
        }


        ////// option portfolio
        public static double PortfolioOptionPrice(List<OptionParameters> optionParameters)
        {
            double totalPrice = 0.0;

            foreach (var parameters in optionParameters)
            {
                double S = GetData.GetSpot(parameters.Underlying);
                double r = Calibration.GetRepo(parameters.Underlying, parameters.Expiry);
                double date = GetData.GetTime(parameters.Underlying);
                double timeToMaturity = (parameters.Expiry - date) / 360;
                double price = Calibration.InterpolatePrice(parameters.Strike, parameters.Expiry, parameters.Underlying);
                double vol = BlackScholes.ImpliedVol(parameters.OptionType, S, price, r, parameters.Strike, timeToMaturity);
                double optionPrice = BlackScholes.Price(parameters.OptionType, S, vol, r, parameters.Strike, timeToMaturity);

                totalPrice += optionPrice;
            }

            return totalPrice;
        }

        public class OptionParameters
        {
            public string Underlying;
            public double Strike;
            public double Expiry;
            public string OptionType; // "c" pour call, "p" pour put, par exemple

            // Ajoutez d'autres propriétés si nécessaire, comme la quantité d'options dans le portefeuille, etc.
        }



    }
}
