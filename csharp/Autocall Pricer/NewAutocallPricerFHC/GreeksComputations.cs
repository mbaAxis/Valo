
using DocumentFormat.OpenXml.Bibliography;
using ScottPlot.Colormaps;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Media.Animation;

public class GreeksComputations
{
    public int n;
    public int N;
    public List<string> Paths;
    public Func<double, double> curve;
    public List<double> dividendsMatrix;
    public List<double[,]> listParamsMatrix;
    public double[] hestonParams;
    public string volType;
    public int freqObs;
    public double BP;
    public double AT;
    public double coupon;
    public double notional;
    public double? price_S_up;
    public double? price_vol_up;

    public GreeksComputations(int n, int N, List<string> Paths, Func<double, double> curve, List<double> dividendsMatrix,
                              List<double[,]> listParamsMatrix, double[] hestonParams, string volType, int freqObs, double BP,
                              double AT, double coupon, double notional)
    {
        this.n = n;
        this.Paths = Paths;
        this.curve = curve;
        this.dividendsMatrix = dividendsMatrix;
        this.N = N;
        this.listParamsMatrix = listParamsMatrix;
        this.hestonParams = hestonParams;
        this.volType = volType;
        this.freqObs = freqObs;
        this.BP = BP;
        this.AT = AT;
        this.coupon = coupon;
        this.notional = notional;
        this.price_S_up = null;
        this.price_vol_up = null;
    }

    public object GeneratePrices(PathGenerator pg, PayoffCalculator pc, List<double> initSpotList, double T, List<int> obsDates,
                                   int totalNbrObs, int couponCounter, int hedgeDate, string shiftType, string shiftDirection,double assetToShift)
    {
        double price = new double(); 
        if (this.n > 1)
        {
            List<double[,]> shiftedMatrix = (List<double[,]>)pg.GeneratePaths(T - hedgeDate / 252.0, initSpotList, shiftType, shiftDirection, assetToShift);
            List<double[,]> evaluationMatrix = (List<double[,]>)pc.ComputeEvaluationMatrix(shiftedMatrix, initSpotList, obsDates);
            List<double[,]> payoffMatrix = (List<double[,]>)pc.ComputePayoff(evaluationMatrix, T, obsDates, totalNbrObs, couponCounter);
            price = PayoffCalculator.ComputePrice(n, notional, payoffMatrix, curve, obsDates);
        }
        else
        {
            double[,] shiftedMatrix = (double[,])pg.GeneratePaths(T - hedgeDate / 252.0, initSpotList, shiftType, shiftDirection,-1);
            double[,] evaluationMatrix = (double[,])pc.ComputeEvaluationMatrix(shiftedMatrix, initSpotList, obsDates);
            double[,] payoffMatrix = (double[,])pc.ComputePayoff(evaluationMatrix, T, obsDates, totalNbrObs, couponCounter);
            price = PayoffCalculator.ComputePrice(n, notional, payoffMatrix, curve, obsDates);
        }
        return price;
    }

    public object ComputeGreeks(PathGenerator pg, PayoffCalculator pc, List<double> initSpotList, double T,
                                                            List<int> obsDates, int totalNbrObs, int couponCounter, string hedgeType, int hedgeDate)
    {
      
        (double, double, double, double) ComputeStressedPrices(double k)
        {
            double priceSpotUp = new double();
            double priceSpotDown = new double();
            double priceVolUp = new double();
            double priceVolDown = new double();
            string[] greeks = new[] { "Delta", "Vega" };
            string[] directions = new[] { "Up", "Down" };
            List<double> prices = new List<double>();
            for (int i = 0; i < greeks.Length; i++)
            {
                for (int j = 0; j < directions.Length; j++)
                {
                    int index = 2 * i + j;
                    prices.Add((double)GeneratePrices(pg, pc, initSpotList, T, obsDates, totalNbrObs, couponCounter, hedgeDate, greeks[i], directions[j], k));
                }
            }
            priceSpotUp = prices[0];
            priceSpotDown = prices[1];
            priceVolUp = prices[2];
            priceVolDown = prices[3];
            return (priceSpotUp,priceSpotDown, priceVolUp, priceVolDown) ;
        }
        if (this.n > 1)
        {
            double[] deltas = new double[this.n];
            double[] vegas = new double[this.n];
            double[] gammas = new double[this.n];
        
            double price = (double)GeneratePrices(pg, pc, initSpotList, T, obsDates, totalNbrObs, couponCounter, hedgeDate, null, null, -1);
            for (int k = 0; k<this.n; k++)
            {
               
                if (hedgeType == "Delta-Vega")
                {
                    (double priceSpotUp, double priceSpotDown, double priceVolUp, double priceVolDown) = ComputeStressedPrices(k);
                    deltas[k] = ((priceSpotUp - priceSpotDown) / (2 * 0.1)) / 10;
                    gammas[k] = ((priceSpotUp + priceSpotDown - 2 * price) / Math.Pow(0.1, 2)) / 100;
                    vegas[k] = (priceVolUp - priceVolDown) / (2 * 0.05);
                }
                else if (hedgeType == "Delta")
                {
                    
                    List<double> prices = new List<double>();
                    double priceSpotUp = (double)GeneratePrices(pg, pc, initSpotList, T, obsDates, totalNbrObs, couponCounter, hedgeDate, "Delta", "Up",k);
                    prices.Add(priceSpotUp);
                    double priceSpotDown = (double)GeneratePrices(pg, pc, initSpotList, T, obsDates, totalNbrObs, couponCounter, hedgeDate, "Delta", "Down",k);
                    prices.Add(priceSpotDown);
                    deltas[k] = ((priceSpotUp - priceSpotDown) / (2 * 0.1)) / 10;
                }
            }
            if (hedgeType == "Delta-Vega")
            {
                return (deltas, vegas, gammas);
            }
            else
            {
                return deltas;
            }

        }
        else
        {
            
            if (hedgeType == "Delta-Vega")
            {
                (double priceSpotUp, double priceSpotDown, double priceVolUp, double priceVolDown) = ComputeStressedPrices(-1);
                double deltas = ((priceSpotUp - priceSpotDown) / (2 * 0.1))/10;
                double price = (double)GeneratePrices(pg, pc, initSpotList, T, obsDates, totalNbrObs, couponCounter, hedgeDate, null, null, -1);
                double gamma = ((priceSpotUp + priceSpotDown - 2 * price) / Math.Pow(0.1, 2))/100;
                double vegas = new double();
                if (this.volType != "SV")
                {
                    vegas = (priceVolUp - priceVolDown) / (2 * 0.05);
                }
                else
                {
                    vegas = (priceVolUp - priceVolDown) / (2 * Math.Pow(pg.InitVol, 2) * 0.0025) * 2 * pg.InitVol;
                }
                return (deltas, vegas, gamma);
            }
            else if (hedgeType == "Delta")
            {
                double price = (double)GeneratePrices(pg, pc, initSpotList, T, obsDates, totalNbrObs, couponCounter, hedgeDate, null, null, -1);
                double[] prices = new double[2];
                double priceSpotUp = (double)GeneratePrices(pg, pc, initSpotList, T, obsDates, totalNbrObs, couponCounter, hedgeDate, "Delta", "Up",-1);
                prices[0] = priceSpotUp;
                double priceSpotDown = (double)GeneratePrices(pg, pc, initSpotList, T, obsDates, totalNbrObs, couponCounter, hedgeDate, "Delta", "Down", -1);
                prices[1] = priceSpotDown;
                double delta = ((priceSpotUp - priceSpotDown) / (2 * 0.1))/10; // Delta is in  % of notional per 1 unit change in spot unit(euros).
                double gamma = ((priceSpotUp + priceSpotDown - 2 * price) / Math.Pow(0.1, 2))/100; // gamma is in % of notional per 1 unit change in spot price
                return delta;
            }
        }
        return null;
    }
}

