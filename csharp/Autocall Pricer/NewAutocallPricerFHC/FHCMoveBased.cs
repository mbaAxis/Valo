using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.IO;
using MathNet.Numerics.LinearAlgebra;
using Accord.Math;
using System.Management.Instrumentation;
using ScottPlot.Colormaps;
using DocumentFormat.OpenXml.Office2010.CustomUI;
using Accord.Math.Distances;


public class FHCMoveBasedHedging
{
    public int n;
    public int N;
    public List<double[]> closingpricesList;
    public double notional;
    public int freqObs;
    public double BP;
    public double AT;
    public double coupon;
    public Func<double, double> curve;
    public int nbrHedges = 0;
    public double lastBinValue;
    public double lastFinalPayoff { get; private set; }

    public FHCMoveBasedHedging(int n, int N, List<double[]> closingpricesList, double notional, int freqObs, double BP, double AT, double coupon, Func<double, double> curve)
    {
        this.n = n;
        this.N = N;
        this.closingpricesList = closingpricesList;
        this.notional = notional;
        this.freqObs = freqObs;
        this.BP = BP;
        this.AT = AT;
        this.coupon = coupon;
        this.curve = curve;
    }
    public object FHCMCMoveBasedDeltaHedges(GreeksComputations gc, PathGenerator pg, PayoffCalculator pc, double T, int stepSize = 252)
    {
        List<int> obsDates = PayoffCalculator.ComputeObsDates(T, freqObs);
        var fhcHelpers = new FHCHelpers(this.n, this.N, this.notional, this.freqObs, this.BP, this.AT, this.coupon, this.curve, this.nbrHedges,this.closingpricesList);
        List<double> initSpotList = FHCHelpers.ExtractSpotList(this.closingpricesList,0);
        int positionCounter = 0, couponCounter = 0, oldPosition = 0;
        List<int> hedgingDatesList = new List<int> { 0 };
        if (this.n > 1) 
        {
            List<double[]> deltas = new List<double[]>();
            double[] initDeltasArr = (double[])fhcHelpers.ComputePriceAtHedgeDateFixed(gc, pg, pc, T, 0, obsDates, 0, initSpotList, "Delta");
            deltas.Add(initDeltasArr);
            double[] oldWeightDeltaHedgeArr = initDeltasArr;
            List<List<double>> hedgedSpotsList = new List<List<double>> { initSpotList };
            List<double[]> sharesPurchased = new List<double[]> { initDeltasArr };
            double[] initCostOfShares = new double[this.n];
            var (cumTransactionCosts, costOfShares, cumCostOfSharesInRate) = fhcHelpers.InitializeDeltaVectors(this.n, initDeltasArr, initSpotList);
            for (int i = 1; i < T * stepSize; i++)
            {
                if ((Math.Abs(this.closingpricesList[1][i] - this.closingpricesList[1][i - 1]) / this.closingpricesList[1][i - 1] > 0.02))
                {
                    int hedgeDate = i;
                    var (position, newOldPosition, newPositionCounter, newCouponCounter) = FHCHelpers.CheckPosition(obsDates, hedgeDate, couponCounter, positionCounter, oldPosition);
                    positionCounter = newPositionCounter;
                    couponCounter = newCouponCounter;
                    oldPosition = newOldPosition;
                    var checkBreakHedge = (ValueTuple<List<double>, bool>)fhcHelpers.CheckBreakHedge(closingpricesList, position, obsDates, oldPosition, positionCounter, T, couponCounter, AT, BP, coupon, notional);
                    List<double> finalPayoffsList = checkBreakHedge.Item1;
                    bool breakState = checkBreakHedge.Item2;
                    if (breakState)
                        break;
                    List<double> spotList  = FHCHelpers.ExtractSpotList(this.closingpricesList, hedgeDate);
                    double[] deltaArr = (double[])fhcHelpers.ComputePriceAtHedgeDateFixed(gc, pg, pc, T, couponCounter, obsDates, hedgeDate, spotList, "Delta");
                    double[] weightDeltaHedge = deltaArr;
                    hedgingDatesList.Add(hedgeDate);
                    deltas.Add(deltaArr);
                    hedgedSpotsList.Add(spotList);
                    double expTerm = Math.Exp(-0.01 * this.curve(hedgeDate / 252.0) * hedgeDate / 252);
                    var res = fhcHelpers.UpdateDeltaVectors(this.n, weightDeltaHedge, oldWeightDeltaHedgeArr, spotList, costOfShares, expTerm, cumCostOfSharesInRate);
                    sharesPurchased.Add(res.diffWeight);
                    costOfShares.Add(res.costShares);
                    cumTransactionCosts.Add((double[])fhcHelpers.UpdateTransactionCosts(cumTransactionCosts, spotList, weightDeltaHedge, oldWeightDeltaHedgeArr, expTerm));
                    cumCostOfSharesInRate.Add(res.costSharesAtHedgeDate);
                    oldWeightDeltaHedgeArr = weightDeltaHedge;
                }
            }
            this.nbrHedges = hedgingDatesList.Count;
            return (hedgingDatesList, hedgedSpotsList, deltas, sharesPurchased, costOfShares, cumCostOfSharesInRate, cumTransactionCosts);
        }
        else
        {
            double initDelta = (double)fhcHelpers.ComputePriceAtHedgeDateFixed(gc, pg, pc, T, 0, obsDates, 0, initSpotList, "Delta");
            double oldWeightDeltaHedge = initDelta;
            var deltasList = new List<double> { initDelta };
            var hedgedSpotsList = new List<List<double>> { initSpotList };
            var sharesPurchased = new List<double> { initDelta };
            var costOfShares = new List<double> { initDelta * initSpotList[0] };
            List<double> cumTransactionCosts = new List<double> { 0.01 * initSpotList[0] * initDelta };
            List<double> cumCostOfSharesInRate = new List<double> { initSpotList[0] * initDelta };
            for (int i = 1; i < T * stepSize; i++)
            {
                if ((Math.Abs(this.closingpricesList[0][i] - this.closingpricesList[0][i-1]) / this.closingpricesList[0][i-1] > 0.02))
                {
                    int hedgeDate = i;
                    var (position, newOldPosition, newPositionCounter, newCouponCounter) = FHCHelpers.CheckPosition(obsDates, hedgeDate, couponCounter, positionCounter, oldPosition);
                    positionCounter = newPositionCounter;
                    couponCounter = newCouponCounter;
                    oldPosition = newOldPosition;
                    var checkBreakHedge = (ValueTuple<List<double>, bool>)fhcHelpers.CheckBreakHedge(closingpricesList, position, obsDates, oldPosition, positionCounter, T, couponCounter, AT, BP, coupon, notional);
                    double finalPayoffsList = checkBreakHedge.Item1.Last();
                    bool breakState = checkBreakHedge.Item2;
                    
                    if (breakState)
                        break;

                    List<double> initSpot = new List<double> { closingpricesList[0][hedgeDate] };
                    double delta = (double)fhcHelpers.ComputePriceAtHedgeDateFixed(gc, pg, pc, T, couponCounter, obsDates, hedgeDate, initSpot, "Delta");
                    double discountFactor = Math.Exp(curve(hedgeDate / 252.0) / 100 * hedgeDate / 252.0);
                    double weightDeltaHedge = delta;
                    //Console.WriteLine($"Delta {delta}");
                    hedgingDatesList.Add(hedgeDate);
                    deltasList.Add(delta);
                    hedgedSpotsList.Add(initSpot);
                    double diffWeight = weightDeltaHedge - oldWeightDeltaHedge;
                    double costShares = diffWeight * initSpot[0];
                    sharesPurchased.Add(diffWeight);
                    costOfShares.Add(costShares);
                    cumTransactionCosts.Add((double)fhcHelpers.UpdateTransactionCosts(cumTransactionCosts, initSpot, weightDeltaHedge, oldWeightDeltaHedge, discountFactor));
                    double costSharesAtHedgeDate = cumCostOfSharesInRate.Last() * discountFactor + costOfShares.Last();
                    cumCostOfSharesInRate.Add(costSharesAtHedgeDate);
                    oldWeightDeltaHedge = weightDeltaHedge;
                    
                }
            }
            this.nbrHedges = hedgingDatesList.Count;
            return (hedgingDatesList, deltasList, hedgedSpotsList, sharesPurchased, costOfShares, cumCostOfSharesInRate, cumTransactionCosts);
        }
    }

   
    public object FHCMCMoveBasedDeltaVegaHedges(GreeksComputations gc, PathGenerator pg, PayoffCalculator pc, double T, int stepSize = 252)
    {
        List<int> hedgeDates = FHCHelpers.ComputeHedgeDates(T, nbrHedges);
        List<int> obsDates = PayoffCalculator.ComputeObsDates(T, freqObs);
        var fhcHelpers = new FHCHelpers(this.n, this.N, this.notional, this.freqObs, this.BP, this.AT, this.coupon, this.curve, this.nbrHedges, this.closingpricesList);
        List<double> initSpotList = FHCHelpers.ExtractSpotList(this.closingpricesList, 0);
        int positionCounter = 0, couponCounter = 0, oldPosition = 0;
        var hedgingDatesList = new List<int> { 0 };

        if (this.n > 1)
        {
            var (priceBinAObj, deltaBinAObj, vegaBinAObj) = fhcHelpers.DigitalOptionPriceAndGreeks(initSpotList, initSpotList, T, this.curve(T / 252) / 100, 0.2, 10, "call");
            var deltaBinAList = new List<double[]> { (double[])deltaBinAObj };
            var vegaBinAList = new List<double[]> { (double[])vegaBinAObj };
            var priceBinAList = new List<double[]> { (double[])priceBinAObj };
            var results = (ValueTuple<double[], double[]>)fhcHelpers.ComputePriceAtHedgeDateFixed(gc, pg, pc, T, 0, obsDates, 0, initSpotList, "Delta-Vega");
            double[] initDeltaArr = results.Item1;
            double[] initVegaArr = results.Item2;
           
            var (oldWeightDeltaHedge, oldWeightVegaHedge) = FHCHelpers.ComputeDeltaVegaWeights(this.n, deltaBinAList, vegaBinAList, initDeltaArr, initVegaArr);
            var deltas = new List<double[]> { initDeltaArr };
            var vegas = new List<double[]> { initVegaArr };
            
            var hedgedSpotsList = new List<List<double>> { initSpotList };
            var sharesPurchased = new List<double[]> { oldWeightDeltaHedge };
            var sharesPurchasedBinary = new List<double[]> { oldWeightVegaHedge };
            var (costOfShares, costOfSharesBinary, cumTransactionCosts, cumTransactionCostsBinary, cumCostOfSharesInRate, cumCostOfBinaryInRate) = fhcHelpers.InitializeDeltaVegaVectors(this.n, oldWeightDeltaHedge, oldWeightVegaHedge, initSpotList, priceBinAList);
            for (int i = 1; i < T * stepSize; i++)
            {
                if ( (Math.Abs(this.closingpricesList[1][i] - this.closingpricesList[1][i - 1]) / this.closingpricesList[1][i - 1] > 0.02))
                {
                    int hedgeDate = i;
                    var (position, newOldPosition, newPositionCounter, newCouponCounter) = FHCHelpers.CheckPosition(obsDates, hedgeDate, couponCounter, positionCounter, oldPosition);
                    positionCounter = newPositionCounter;
                    couponCounter = newCouponCounter;
                    oldPosition = newOldPosition;
                    var checkBreakHedge = (ValueTuple<List<double>, bool>)fhcHelpers.CheckBreakHedge(closingpricesList, position, obsDates, oldPosition, positionCounter, T, couponCounter, AT, BP, coupon, notional);
                    List<double> finalPayoffsList = checkBreakHedge.Item1;
                    bool breakState = checkBreakHedge.Item2;
                    if (breakState)
                        break;
                    List<double> spotList = FHCHelpers.ExtractSpotList(this.closingpricesList, hedgeDate);
                    var result = (ValueTuple<double[], double[]>)fhcHelpers.ComputePriceAtHedgeDateFixed(gc, pg, pc, T, couponCounter, obsDates, hedgeDate, spotList, "Delta-Vega");
                    double residualMaturity = (T - hedgeDate / 252.0);
                    double[] deltaArr = result.Item1;
                    double[] vegaArr = result.Item2;
                    double discountFactor = Math.Exp(curve(hedgeDate / 252.0) / 100 * hedgeDate / 252.0);
                    (priceBinAObj, deltaBinAObj, vegaBinAObj) = fhcHelpers.DigitalOptionPriceAndGreeks(hedgedSpotsList.Last(), hedgedSpotsList.Last(),T, this.curve(residualMaturity) / 100, 0.2, 10, "call");
                    deltaBinAList.Add((double[])deltaBinAObj);
                    vegaBinAList.Add((double[])vegaBinAObj);
                    priceBinAList.Add((double[])priceBinAObj);
                    var (weightDeltaHedge, weightVegaHedge) = FHCHelpers.ComputeDeltaVegaWeights(this.n, deltaBinAList, vegaBinAList, deltaArr, vegaArr);
                    hedgingDatesList.Add(hedgeDate);
                    deltas.Add(deltaArr);
                    vegas.Add(vegaArr);
                    hedgedSpotsList.Add(spotList);
                    var (cosnew, cobnew, weightDiff, weightDiffBin, cosR, cosB) = fhcHelpers.UpdateDeltaVegaVectors(this.n, weightDeltaHedge, oldWeightDeltaHedge,
        weightVegaHedge, oldWeightVegaHedge, hedgedSpotsList, priceBinAList, costOfShares,costOfSharesBinary, cumCostOfSharesInRate, cumCostOfBinaryInRate, discountFactor);
                    costOfShares.Add(cosnew);
                    costOfSharesBinary.Add(cobnew);
                    sharesPurchased.Add(weightDiff);
                    sharesPurchasedBinary.Add(weightDiffBin);
                    cumTransactionCosts.Add((double[])fhcHelpers.UpdateTransactionCosts(cumTransactionCosts, spotList, weightDeltaHedge, oldWeightDeltaHedge, discountFactor));
                    cumTransactionCostsBinary.Add((double[])fhcHelpers.UpdateTransactionCosts(cumTransactionCostsBinary, priceBinAList.Last().ToList(), weightVegaHedge, oldWeightVegaHedge, discountFactor));
                    cumCostOfSharesInRate.Add(cosR);
                    cumCostOfBinaryInRate.Add(cosB);
                    oldWeightDeltaHedge = weightDeltaHedge;
                    oldWeightVegaHedge = weightVegaHedge;
                }
            }
            this.nbrHedges = hedgingDatesList.Count;
            return (hedgingDatesList, hedgedSpotsList, deltas, vegas, sharesPurchased, costOfShares, cumCostOfSharesInRate, cumTransactionCosts,
                 sharesPurchasedBinary, costOfSharesBinary, cumCostOfBinaryInRate, cumTransactionCostsBinary);
        }
        else
        {
            
            var (priceBinAObj, deltaBinAObj, vegaBinAObj) = fhcHelpers.DigitalOptionPriceAndGreeks(initSpotList, initSpotList, T,this.curve(T) / 100, 0.2, 10, "call");
            var deltaBinAList = new List<double> { (double)deltaBinAObj };
            var vegaBinAList = new List<double> { (double)vegaBinAObj };
            var priceBinAList = new List<double> { (double)priceBinAObj };

            var results = (ValueTuple<double, double>)fhcHelpers.ComputePriceAtHedgeDateFixed(gc, pg, pc, T, 0, obsDates, 0, initSpotList, "Delta-Vega");
            double initDelta = results.Item1;
            double initVega = results.Item2;
            double oldWeightDeltaHedge = -(-initDelta + (initVega / vegaBinAList.Last()) * deltaBinAList.Last());
            double oldWeightVegaHedge = initVega / vegaBinAList.Last();
            var deltas = new List<double> { initDelta };
            var vegas = new List<double> { initVega };
            
            var hedgedSpotsList = new List<List<double>> { initSpotList };
            var sharesPurchased = new List<double> { oldWeightDeltaHedge };
            var costOfShares = new List<double> { oldWeightDeltaHedge * initDelta };
            List<double> cumTransactionCosts = new List<double> { 0.01 * Math.Abs(oldWeightDeltaHedge * initSpotList[0]) };
            List<double> cumTransactionCostsBinary = new List<double> {0.01 * Math.Abs(oldWeightVegaHedge * priceBinAList.Last()) };
            var sharesPurchasedBinary = new List<double> { oldWeightVegaHedge };
            var costOfSharesBinary = new List<double> { oldWeightVegaHedge * priceBinAList.Last() };
            List<double> cumCostOfSharesInRate = new List<double> { initSpotList[0] * oldWeightDeltaHedge};
            List<double> cumCostOfBinaryInRate = new List<double> { oldWeightVegaHedge  * priceBinAList.Last() };
            for (int i = 1; i < T * stepSize; i++)
            {
                if ((Math.Abs(this.closingpricesList[0][i] - this.closingpricesList[0][i-1]) / this.closingpricesList[0][i-1] > 0.02))
                {
                    int hedgeDate = i;
                    var (position, newOldPosition, newPositionCounter, newCouponCounter) = FHCHelpers.CheckPosition(obsDates, hedgeDate, couponCounter, positionCounter, oldPosition);
                    positionCounter = newPositionCounter;
                    couponCounter = newCouponCounter;
                    oldPosition = newOldPosition;
                    double residualMaturity = T - hedgeDate / 252;
                    var checkBreakHedge = (ValueTuple<List<double>, bool>)fhcHelpers.CheckBreakHedge(closingpricesList, position, obsDates, oldPosition, positionCounter, T, couponCounter, AT, BP, coupon, notional);
                    List<double> finalPayoffsList = checkBreakHedge.Item1;
                    bool breakState = checkBreakHedge.Item2;
                    
                    
                    if (breakState)
                    {
                        lastFinalPayoff = finalPayoffsList.Last();
                        Console.WriteLine($"inside the funct {lastFinalPayoff}");
                        break;
                    }
                        
                    List<double> spotList = FHCHelpers.ExtractSpotList(this.closingpricesList, i);
                    var result = (ValueTuple<double, double>)fhcHelpers.ComputePriceAtHedgeDateFixed(gc, pg, pc, T, couponCounter, obsDates, hedgeDate, spotList, "Delta-Vega");
                    double deltaArr = result.Item1;
                    double vegaArr = result.Item2;
                    double discountFactor = Math.Exp(curve(hedgeDate / 252.0) / 100 * hedgeDate / 252.0);

                    (priceBinAObj, deltaBinAObj, vegaBinAObj) = fhcHelpers.DigitalOptionPriceAndGreeks(spotList,spotList, residualMaturity, this.curve(residualMaturity)/100, 0.2, 10, "call");
                    
                    deltaBinAList.Add((double)deltaBinAObj);
                    vegaBinAList.Add((double)vegaBinAObj);
                    priceBinAList.Add((double)priceBinAObj);
                    
                    double weightVegaHedge = vegaArr / vegaBinAList.Last();
                    double weightDeltaHedge = -(-deltaArr + weightVegaHedge * deltaBinAList.Last());

                    double[] autocallGreeksMatrix = new double[] { deltaArr, vegaArr };
                    double[] hedgingInstrumentsGreeks = new double[] { deltaBinAList.Last(), vegaBinAList.Last() };

                    hedgingDatesList.Add(hedgeDate);
                    deltas.Add(deltaArr);
                    hedgedSpotsList.Add(spotList);
                    sharesPurchased.Add(weightDeltaHedge - oldWeightDeltaHedge);
                    costOfShares.Add((weightDeltaHedge - oldWeightDeltaHedge) * hedgedSpotsList.Last()[0]);
                    vegas.Add(vegaArr);
                    sharesPurchasedBinary.Add(weightVegaHedge - oldWeightVegaHedge);
                    costOfSharesBinary.Add((weightVegaHedge - oldWeightVegaHedge) * priceBinAList.Last());
                    double costSharesAtHedgeDate = cumCostOfSharesInRate.Last() * discountFactor + costOfShares.Last();
                    cumCostOfSharesInRate.Add(costSharesAtHedgeDate);
                    double costBinAtHedgeDate = cumCostOfBinaryInRate.Last() * discountFactor + costOfSharesBinary.Last();
                    cumCostOfBinaryInRate.Add(costBinAtHedgeDate);
                    cumTransactionCosts.Add((double)fhcHelpers.UpdateTransactionCosts(cumTransactionCosts, spotList, weightDeltaHedge, oldWeightDeltaHedge, discountFactor));
                    cumTransactionCostsBinary.Add((double)fhcHelpers.UpdateTransactionCosts(cumTransactionCostsBinary, priceBinAList, weightVegaHedge, oldWeightVegaHedge, discountFactor));
                    oldWeightDeltaHedge = weightDeltaHedge;
                    oldWeightVegaHedge = weightVegaHedge;
                }
                this.lastBinValue = priceBinAList.Last();
            }
            this.nbrHedges = hedgingDatesList.Count;
            return (hedgingDatesList, deltas, vegas, hedgedSpotsList, sharesPurchased, costOfShares, cumCostOfSharesInRate, cumTransactionCosts,
                 sharesPurchasedBinary, costOfSharesBinary, cumCostOfBinaryInRate, cumTransactionCostsBinary);
        }
    }
}
