using Accord.Math;
using DocumentFormat.OpenXml.Bibliography;
using System;





public class VanillaFHCClosed
{
    public double S;
    public double K;
    public double T;
    public double vol;
    public Func<double, double> curve;
    public double ks;
    public double sigmaS;
    public double lbda;

    public VanillaFHCClosed(double S, double K, double T, double vol, Func<double, double> curve, double ks, double sigmaS, double lbda)
    {
        this.S = S;
        this.K = K;
        this.T = T;
        this.vol = vol;
        this.curve = curve;
        this.ks = ks;
        this.sigmaS = sigmaS;
        this.lbda = lbda;
    }
    public (double,double) ComputeFrictionSlippage()
    {
        double r = this.curve(this.T/252);
        var bsPricer = new BlackScholesOptionPricing(this.S, this.K, r, 0, this.T);
        double price = bsPricer.CallPrice(this.vol);
        double delta = bsPricer.CallOptionDelta(this.vol);
        double gamma = bsPricer.OptionGamma(this.vol); 
        double vega = bsPricer.OptionVega(this.vol);
        double N = ComputeN();
        double frictionCosts = this.ks * Math.Sqrt(2 / Math.PI) * vega * Math.Sqrt(N/this.T);
        double varSlippage = Math.Sqrt(Math.PI / 2) * (Math.Pow(this.sigmaS, 4) * Math.Pow(this.T, 2)) / (2 * N) * Math.Pow((gamma * Math.Pow(this.S, 2)), 2);
        double stdSlippage = gamma * Math.Pow(this.S, 2) * Math.Pow(this.sigmaS, 2) * T / (Math.Sqrt(N * 2));
        return (frictionCosts, stdSlippage);
    }
    public double ComputeN()
    {
        double N = (this.lbda * Math.Sqrt(Math.PI) * this.sigmaS * this.T) / (2 * this.ks);
        return N;
    }
}
