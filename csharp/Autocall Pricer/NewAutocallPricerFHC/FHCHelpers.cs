using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using MathNet.Numerics.Distributions;
public class FHCHelpers
{
    private int n;
    private int N;
    private double notional;
    private int freqObs;
    private double BP;
    private double AT;
    private double coupon;
    private Func<double, double> curve;
    private int nbrHedges;
    public List<double[]> closingPricesList; 

    public FHCHelpers(int n, int N, double notional, int freqObs, double BP, double AT, double coupon, Func<double, double> curve, int nbrHedges, List<double[]> closingPricesList)
    {
        this.n = n;
        this.N = N;
        this.notional = notional;
        this.freqObs = freqObs;
        this.BP = BP;
        this.AT = AT;
        this.coupon = coupon;
        this.curve = curve;
        this.nbrHedges = nbrHedges;
        this.closingPricesList = closingPricesList;

    }

    public DataTable FillDeltaHedgeTable(GreeksComputations gc, PathGenerator pg, PayoffCalculator pc, double T, string FHCType, int stepSize = 252)
    {
        var FHCFixed = new FHC_FixedHedging(this.n, this.N, this.closingPricesList, this.notional, this.freqObs, this.BP, this.AT, this.coupon, this.curve, this.nbrHedges);
        var FHCLevelBased = new FHCMoveBasedHedging(this.n, this.N, this.closingPricesList, this.notional, this.freqObs, this.BP, this.AT, this.coupon, this.curve);
        DataTable deltaHedging = Constants.CreateDeltaHedgeTable(this.n);
        var sharesPurchasedDf = new List<double>();
        var optionsPurchasedDf = new List<double>();
        object result;
        if (FHCType == "Fixed")
        {
            result = FHCFixed.FHCMCFixedDeltaHedges(gc, pg, pc, T, stepSize);
        }
        else
        {
            result = FHCLevelBased.FHCMCMoveBasedDeltaHedges(gc, pg, pc, T, stepSize);
        }

        if (this.n > 1)
        {
            var (hedgingDatesList, hedgedSpotsList, deltas, sharesPurchased, costOfShares, cumCostOfSharesInRate, cumTransactionCosts) =
                ((List<int>, List<List<double>>, List<double[]>, List<double[]>, List<double[]>, List<double[]>, List<double[]>))result;
            for (int i = 0; i < deltas.Count; i++)
            {
                sharesPurchasedDf.Add(sharesPurchased[i].Sum());
                deltaHedging.Rows.Add(hedgingDatesList[i], hedgedSpotsList[i][0], hedgedSpotsList[i][1], deltas[i].Sum(), sharesPurchased[i].Sum(), sharesPurchasedDf.Take(i + 1).Sum(), costOfShares[i].Sum(), cumCostOfSharesInRate[i].Sum(), cumTransactionCosts[i].Sum());
            }
        }
        else
        {
            var (hedgingDatesList, deltas, hedgedSpotsList, sharesPurchased, costOfShares, cumCostOfSharesInRate, cumTransactionCosts) =
                 ((List<int>, List<double>, List<List<double>>, List<double>, List<double>, List<double>, List<double>))result;
            for (int i = 0; i < deltas.Count; i++)
            {

                sharesPurchasedDf.Add(sharesPurchased[i]);
                deltaHedging.Rows.Add(hedgingDatesList[i], hedgedSpotsList[i][0], deltas[i], sharesPurchasedDf[i], sharesPurchased.Take(i + 1).Sum(), costOfShares[i], cumCostOfSharesInRate[i], cumTransactionCosts[i]);
            }
        }
        return deltaHedging;
    }

    public DataTable FillDeltaVegaHedgeTable(GreeksComputations gc, PathGenerator pg, PayoffCalculator pc, double T, string FHCType, int stepSize = 252)
    {
        var FHCFixed = new FHC_FixedHedging(this.n, this.N, this.closingPricesList, this.notional, this.freqObs, this.BP, this.AT, this.coupon, this.curve, this.nbrHedges);
        var FHCLevelBased = new FHCMoveBasedHedging(this.n, this.N, this.closingPricesList, this.notional, this.freqObs, this.BP, this.AT, this.coupon, this.curve);
        DataTable deltaVegaHedging = Constants.CreateDeltaVegaHedgeTable(this.n);
        var sharesPurchasedDf = new List<double>();
        var optionsPurchasedDf = new List<double>();
        object result;
        if (FHCType == "Fixed")
        {
            result = FHCFixed.FHCMCFixedDeltaVegaHedges(gc, pg, pc, T, stepSize);
        }
        else
        {
            result = FHCLevelBased.FHCMCMoveBasedDeltaVegaHedges(gc, pg, pc, T, stepSize);
        }
        
        if (this.n > 1)
        {
            var (hedgeDates, hedgedSpotsList, deltas, vegas, sharesPurchased, costOfShares, cumCostOfSharesInRate, cumTransactionCosts,
                 sharesPurchasedBinary, costOfSharesBinary, cumCostOfBinaryInRate, cumTransactionCostsBinary) =
                ((List<int>, List<List<double>>, List<double[]>, List<double[]>, List<double[]>, List<double[]>, List<double[]>, List<double[]>,
                 List<double[]>, List<double[]>, List<double[]>, List<double[]>))result;
            for (int i = 0; i < deltas.Count; i++)
            {

                sharesPurchasedDf.Add(sharesPurchased[i].Sum());
                optionsPurchasedDf.Add(sharesPurchasedBinary[i].Sum());
                deltaVegaHedging.Rows.Add(hedgeDates[i], hedgedSpotsList[i][0], hedgedSpotsList[i][1], deltas[i].Sum(), vegas[i].Sum(), sharesPurchased[i].Sum(), sharesPurchasedBinary[i].Sum(), sharesPurchasedDf.Take(i + 1).Sum(), optionsPurchasedDf.Take(i + 1).Sum(), costOfShares[i].Sum(),
                    costOfSharesBinary[i].Sum(), cumCostOfSharesInRate[i].Sum(), cumCostOfBinaryInRate[i].Sum(), cumTransactionCosts[i].Sum(), cumTransactionCostsBinary[i].Sum());
            }
        }
        else
        {
            var (hedgeDates, deltas, vegas, hedgedSpotsList, sharesPurchased, costOfShares, cumCostOfSharesInRate, cumTransactionCosts,
                 sharesPurchasedBinary, costOfSharesBinary, cumCostOfBinaryInRate, cumTransactionCostsBinary) =
                ((List<int>, List<double>, List<double>, List<List<double>>, List<double>, List<double>, List<double>, List<double>,
                 List<double>, List<double>, List<double>, List<double>))result;
            for (int i = 0; i < deltas.Count; i++)
            {

                sharesPurchasedDf.Add(sharesPurchased[i]);
                optionsPurchasedDf.Add(sharesPurchasedBinary[i]);
                deltaVegaHedging.Rows.Add(hedgeDates[i], hedgedSpotsList[i][0], deltas[i], vegas[i], sharesPurchasedDf[i], optionsPurchasedDf[i], sharesPurchasedDf.Take(i + 1).Sum(),
                    optionsPurchasedDf.Take(i + 1).Sum(), costOfShares[i], costOfSharesBinary[i], cumCostOfSharesInRate[i], cumCostOfBinaryInRate[i], cumTransactionCosts[i], cumTransactionCostsBinary[i]);
            }
        }
        return deltaVegaHedging;
    }
    public (object price, object delta, object vega) DigitalOptionPriceAndGreeks(object spot, object strike, double residMat, double r, double sigma, double payoff, string optionType = "call")
    {
        List<double> spotList = (List<double>)spot;
        List<double> strikeList = (List<double>)strike;
        double[] price = new double[this.n];
        double[] delta = new double[this.n];
        double[] vega = new double[this.n];
        for (int i = 0; i < this.n; i++)
        {
            double S = spotList[i];
            double K = strikeList[i];
            double d2 = (Math.Log(S / K) + (r - 0.5 * Math.Pow(sigma, 2)) * residMat) / (sigma * Math.Sqrt(residMat));
            double d1 = (Math.Log(S / K) + (r + 0.5 * Math.Pow(sigma, 2)) * residMat) / (sigma * Math.Sqrt(residMat));
            double pdf_d2 = Normal.PDF(0, 1, d2);
            price[i] = Math.Exp(-r * residMat) * payoff * (optionType == "call" ? Normal.CDF(0, 1, d2) : Normal.CDF(0, 1, -d2));
            delta[i] = (optionType == "call" ? pdf_d2 / (S * sigma * Math.Sqrt(residMat)) : -pdf_d2 / (S * sigma * Math.Sqrt(residMat))) * payoff * Math.Exp(-r * residMat);
            vega[i] = -(d1 * pdf_d2 / sigma) * payoff * Math.Exp(-r * residMat);
        }
        if (this.n > 1)
        {
            return (price, delta, vega);
        }
        else
        {
            return (price[0], delta[0], vega[0]);
        }
    }

    public static List<int> ComputeHedgeDates(double T, int nbrHedges, int stepSize = 252)
    {
        double hedgeIntervals = T * stepSize / nbrHedges;
        List<int> hedgeDates = new List<int>();
        int maxHedgeDate = (int)Math.Ceiling(T * stepSize / (double)hedgeIntervals);
        for (int j = 0; j < maxHedgeDate; j++)
        {
            int hedgeDate = (int)(j * hedgeIntervals);
            hedgeDates.Add(hedgeDate);
        }
        return hedgeDates;
    }

    public object UpdateTransactionCosts(object transactionCostsObj, List<double> spot, object newWeightObj, object oldWeightObj, double discountFactor)
    {
        if (this.n > 1)
        {
            List<double[]> transactionCosts = (List<double[]>)transactionCostsObj;
            double[] newWeight = (double[])newWeightObj;
            double[] oldWeight = (double[])oldWeightObj;
            double[] updatedTransCosts = new double[this.n];
            for (int i = 0; i < this.n; i++)
            {
                updatedTransCosts[i] = transactionCosts.Last()[i] * discountFactor + 0.01 * Math.Abs(newWeight[i] - oldWeight[i]) * spot[i];
            }
            return (double[])updatedTransCosts;
        }
        else
        {
            List<double> transactionCosts = (List<double>)transactionCostsObj;
            double newWeight = (double)newWeightObj;
            double oldWeight = (double)oldWeightObj;
            double updatedTransCosts = new double();
            updatedTransCosts = transactionCosts.Last() * discountFactor + 0.01 * Math.Abs(newWeight - oldWeight) * spot[0];
            return (double)updatedTransCosts;
        }

    }

    public object ComputePriceAtHedgeDateFixed(GreeksComputations gc, PathGenerator pg, PayoffCalculator pc,
                                                                                                  double T, int couponCounter, List<int> obsDates,
                                                                                                  int hedgeDate, List<double> initSpotList, string hedgeType)
    {
        List<int> obsDatesNew = (List<int>)obsDates.Select(x => x - hedgeDate).Where(x => x > 0).ToList();
        int totalNbrObs = obsDates.Count;

        var results = gc.ComputeGreeks(pg, pc, initSpotList, T, obsDatesNew, totalNbrObs, couponCounter, hedgeType, hedgeDate);
        if (this.n > 1)
        {
            if (hedgeType == "Delta-Vega")
            {
                var deltavega = (ValueTuple<double[], double[], double[]>)results;
                return (deltavega.Item1, deltavega.Item2);
            }
            else
            {
                var deltaGamma = (double[])results;
                return deltaGamma;
            }
        }
        else
        {
            if (hedgeType == "Delta-Vega")
            {
                var deltavega = (ValueTuple<double, double ,double>)results;
                return (deltavega.Item1, deltavega.Item2);
            }
            else
            {
                var deltaGamma = (double)results;
                return deltaGamma;
            }
        }
    }

    public static (int position, int oldPosition, int positionCounter, int couponCounter) CheckPosition(List<int> obsDates, int hedgeDate, int couponCounter, int positionCounter, int oldPosition)
    {
        int position = Array.BinarySearch(obsDates.ToArray(), hedgeDate);
        position = position < 0 ? ~position : position;
        if (position != couponCounter)
        {
            oldPosition = positionCounter;
            positionCounter = position;
            couponCounter += 1;
        }
        return (position, oldPosition, positionCounter, couponCounter);
    }

    public (List<double> finalPayoffsList, bool isBreak) CheckBreakHedge(List<double[]> closingPricesList, int position, List<int> obsDates, int oldPosition,
                                                                      int positionCounter, double T, int couponCounter, double AT, double BP,
                                                                      double coupon, double notional)
    {
        List<double> finalPayoffsList = new List<double>();
        bool isBreakHedge = false;
        for (int i = 0; i < this.n; i++)
        {
            if (position < obsDates.Count - 1)
            {
                if (oldPosition != positionCounter && closingPricesList[i][obsDates[oldPosition]] / closingPricesList[i][0] > AT)
                {

                    double finalPayoff = (1 + ((T * (oldPosition + couponCounter) / obsDates.Count) * coupon)) * notional;
                    isBreakHedge = isBreakHedge || true;
                    finalPayoffsList.Add(finalPayoff);
                }
                //else
                //{
                //    finalPayoffsList.Add(0);
                //}
            }
            else if (position == obsDates.Count - 1)
            {
                double ratio = closingPricesList[i][obsDates[oldPosition]] / closingPricesList[i][0];
                if (oldPosition != positionCounter && ratio > AT)
                {

                    double finalPayoff = (1 + ((T * (oldPosition + 1) / obsDates.Count) * coupon))*notional;
                    isBreakHedge = isBreakHedge || true;
                    finalPayoffsList.Add(finalPayoff);

                }
                if (ratio < AT)
                {

                    double finalPayoff;
                    if (closingPricesList[i][obsDates.Last()] / closingPricesList[i][0] >= AT)
                    {
                        finalPayoff = (1 + coupon) * notional;
                        
                    }
                    else if (BP < closingPricesList[i][obsDates.Last()] / closingPricesList[i][0] && closingPricesList[i][obsDates.Last()] / closingPricesList[i][0] < AT)
                    {
                        finalPayoff = notional;
                        
                    }
                    else
                    {
                        finalPayoff = (closingPricesList[i][obsDates.Last()] / closingPricesList[i][0]) * notional;
                        
                    }
                    isBreakHedge = isBreakHedge || false;
                    finalPayoffsList.Add(finalPayoff);
                }
            }
        }
        if (finalPayoffsList.Count == 0)
        {
            finalPayoffsList.Add(0);
        }
        return (finalPayoffsList, isBreakHedge);

    }
    public static List<double> ExtractSpotList(List<double[]>closingpricesList, int index)
    {
        List<double> initSpotList = new List<double>();
        for (int i = 0; i < closingpricesList.Count; i++)
        {
            initSpotList.Add(closingpricesList[i][index]);
        }
        return initSpotList;
    }

    public (List<double[]> cumTransactionCosts, List<double[]> costOfShares, List<double[]> cumCostOfSharesInRate) InitializeDeltaVectors(int n, double[] weightDeltaHedge, List<double> spotList)
    {
        List<double[]> cumCostOfSharesInRate = new List<double[]>();
        var cumTransactionCosts = new List<double[]>();
        List<double[]> costOfShares = new List<double[]>();
        double[] initCostOfShares = new double[n];
        double[] cts = new double[n];
        for (int i = 0; i < this.n; i++)
        {
            initCostOfShares[i] = weightDeltaHedge[i] * spotList[i];
            cts[i] = 0.01 * spotList[i] * weightDeltaHedge[i];
        }
        cumTransactionCosts.Add(cts);
        costOfShares.Add(initCostOfShares);
        cumCostOfSharesInRate.Add(initCostOfShares);
        return (cumTransactionCosts, costOfShares, cumCostOfSharesInRate);
    }

    public (double[] diffWeight, double[] costShares, double[] costSharesAtHedgeDate) UpdateDeltaVectors(int n,double[]weightDeltaHedge, double[]oldWeightDeltaHedgeArr, List<double> spotList, List<double[]> costOfShares,double expTerm, List<double[]> cumCostOfSharesInRate)
    {
        double[] diffWeight = new double[n];
        double[] costShares = new double[n];
        double[] costSharesAtHedgeDate = new double[n];
        for (int j = 0; j < n; j++)
        {
            diffWeight[j] = weightDeltaHedge[j] - oldWeightDeltaHedgeArr[j];
            costShares[j] = (weightDeltaHedge[j] - oldWeightDeltaHedgeArr[j]) * spotList[j];
            costSharesAtHedgeDate[j] = cumCostOfSharesInRate.Last()[j] * expTerm + costOfShares.Last()[j];
        }
        return (diffWeight, costShares, costSharesAtHedgeDate);
    }
    public (List<double[]> costOfShares, List<double[]> costOfSharesBinary, List<double[]> cumTransactionCosts, List<double[]> cumTransactionCostsBinary,
        List<double[]> cumCostOfSharesInRate, List<double[]> cumCostOfBinaryInRate) InitializeDeltaVegaVectors(int n, double[] weightDeltaHedge,double[] weightVegaHedge, List<double> spotList, List<double[]> priceBinAList)
    {
        var costOfShares = new List<double[]> { };
        var costOfSharesBinary = new List<double[]> { };
        List<double[]> cumTransactionCosts = new List<double[]>();
        List<double[]> cumTransactionCostsBinary = new List<double[]>();
        List<double[]> cumCostOfSharesInRate = new List<double[]>();
        List<double[]> cumCostOfBinaryInRate = new List<double[]>();
        double[] cos = new double[this.n];
        double[] cob = new double[this.n];
        double[] cts = new double[this.n];
        double[] ctb = new double[this.n];
        for (int i = 0; i < this.n; i++)
        {
            cos[i] = weightDeltaHedge[i] * spotList[i];
            cob[i] = weightVegaHedge[i] * priceBinAList.Last()[i];
            cts[i] = 0.01 * Math.Abs(weightDeltaHedge[i] * spotList[i]);
            ctb[i] = 0.01 * Math.Abs(weightVegaHedge[i] * priceBinAList.Last()[i]);
        }
        costOfShares.Add(cos);
        costOfSharesBinary.Add(cob);
        cumTransactionCosts.Add(cts);
        cumTransactionCostsBinary.Add(ctb);
        cumCostOfSharesInRate.Add(costOfShares[0]);
        cumCostOfBinaryInRate.Add(costOfSharesBinary[0]);
        return (costOfShares,costOfSharesBinary,cumTransactionCosts,cumTransactionCostsBinary,cumCostOfSharesInRate,cumCostOfBinaryInRate); 
    }

    public (double[] cosnew,double[] cobnew,double[] weightDiff,double[] weightDiffBin,double[] cosR, double[] cosB) UpdateDeltaVegaVectors(int n, double[] weightDeltaHedge, double[] oldWeightDeltaHedge, 
        double[] weightVegaHedge,double[] oldWeightVegaHedge, List<List<double>> hedgedSpotsList, List<double[]> priceBinAList,List<double[]> costOfShares, 
        List<double[]> costOfSharesBinary, List<double[]> cumCostOfSharesInRate, List<double[]> cumCostOfBinaryInRate, double discountFactor)
    {
        double[] weightDiff = new double[this.n];
        double[] weightDiffBin = new double[this.n];
        double[] costOfSharesBin = new double[this.n];
        double[] cosR = new double[this.n];
        double[] cosB = new double[this.n];
        double[] cosnew = new double[this.n];
        double[] cobnew = new double[this.n];
        for (int j = 0; j < this.n; j++)
        {
            weightDiff[j] = weightDeltaHedge[j] - oldWeightDeltaHedge[j];
            weightDiffBin[j] = weightVegaHedge[j] - oldWeightVegaHedge[j];
            cosnew[j] = weightDiff[j] * hedgedSpotsList.Last()[j];
            cobnew[j] = weightDiffBin[j] * priceBinAList.Last()[j];
            cosR[j] = cumCostOfSharesInRate.Last()[j] * discountFactor + cosnew[j];
            cosB[j] = cumCostOfBinaryInRate.Last()[j] * discountFactor + cobnew[j];
        }
        return (cosnew,cobnew,weightDiff,weightDiffBin,cosR, cosB);
    }

    public static (double[] weightDeltaHedge, double[] weightVegaHedge) ComputeDeltaVegaWeights(int n ,List<double[]> deltaBinAList, List<double[]> vegaBinAList, double[] deltaArr, double[] vegaArr)
    {
        double[] weightVegaHedge = new double[n];
        double[] weightDeltaHedge = new double[n];
        for (int j = 0; j < n; j++)
        {
            weightVegaHedge[j] = vegaArr[j] / vegaBinAList.Last()[j];
            weightDeltaHedge[j] = -(-deltaArr[j] + weightVegaHedge[j] * deltaBinAList.Last()[j]);
        }
        return (weightDeltaHedge, weightVegaHedge);
    }
}

