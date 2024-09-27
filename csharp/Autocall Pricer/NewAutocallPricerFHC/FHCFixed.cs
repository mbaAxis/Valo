using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using Accord.Math;
using DocumentFormat.OpenXml.Drawing.Charts;

public class FHC_FixedHedging
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
    public int nbrHedges;
    public int effectiveNbrHedgingEvents;
    public double lastBinValue;
    public FHCHelpers fhcHelpers;

    public FHC_FixedHedging(int n, int N, List<double[]> closingpricesList, double notional, int freqObs, double BP, double AT, double coupon, Func<double, double> curve, int nbrHedges)
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
        this.nbrHedges = nbrHedges;
        this.fhcHelpers = new FHCHelpers(this.n, this.N, this.notional, this.freqObs, this.BP, this.AT, this.coupon, this.curve, this.nbrHedges, this.closingpricesList);
        
    }

    public object FHCMCFixedDeltaHedges(GreeksComputations gc, PathGenerator pg, PayoffCalculator pc, double T, int stepSize = 252)
    {

        List<int> hedgeDates = FHCHelpers.ComputeHedgeDates(T, nbrHedges);
        List<int> obsDates = PayoffCalculator.ComputeObsDates(T, freqObs);
        List<double> initSpotList = FHCHelpers.ExtractSpotList(this.closingpricesList, 0);
        int positionCounter = 0, couponCounter = 0, oldPosition = 0;
        List<double[]> deltas = new List<double[]>();
        var fhcHelpers = new FHCHelpers(this.n, this.N, this.notional, this.freqObs, this.BP, this.AT, this.coupon, this.curve, this.nbrHedges, this.closingpricesList);
        if (this.n > 1)
        {
            double[] initDeltasArr = (double[])fhcHelpers.ComputePriceAtHedgeDateFixed(gc, pg, pc, T, 0, obsDates, 0, initSpotList, "Delta");
            deltas.Add(initDeltasArr);
            double[] oldWeightDeltaHedgeArr = initDeltasArr;
            List<int> hedgingDatesList = new List<int> { 0 };
            List<List<double>> hedgedSpotsList = new List<List<double>> { initSpotList };
            List<double[]> sharesPurchased = new List<double[]> { initDeltasArr };
            double[] initCostOfShares = new double[this.n];
            var (cumTransactionCosts, costOfShares, cumCostOfSharesInRate) = fhcHelpers.InitializeDeltaVectors(n, initDeltasArr, initSpotList);
            for (int i = 1; i < hedgeDates.Count; i++)
            {
                int hedgeDate = hedgeDates[i];
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
                double[] deltaArr = (double[])fhcHelpers.ComputePriceAtHedgeDateFixed(gc, pg, pc, T, couponCounter, obsDates, hedgeDate, spotList, "Delta");
                double discountFactor = Math.Exp(curve(hedgeDate / 252.0) / 100 * hedgeDate / 252.0);
                double[] weightDeltaHedge = deltaArr;
                hedgingDatesList.Add(hedgeDate);
                deltas.Add(deltaArr);
                hedgedSpotsList.Add(spotList);
                double expTerm = Math.Exp(-0.01 * this.curve(hedgeDate / 252.0) * hedgeDate / 252);
                var res = fhcHelpers.UpdateDeltaVectors(this.n, weightDeltaHedge, oldWeightDeltaHedgeArr, spotList, costOfShares, expTerm, cumCostOfSharesInRate);
                sharesPurchased.Add(res.diffWeight);
                costOfShares.Add(res.costShares);
                cumTransactionCosts.Add((double[])fhcHelpers.UpdateTransactionCosts(cumTransactionCosts, spotList, weightDeltaHedge, oldWeightDeltaHedgeArr, discountFactor));
                cumCostOfSharesInRate.Add(res.costSharesAtHedgeDate);
                oldWeightDeltaHedgeArr = weightDeltaHedge;
            }
            this.effectiveNbrHedgingEvents = hedgingDatesList.Count;
            return (hedgeDates, hedgedSpotsList, deltas, sharesPurchased, costOfShares, cumCostOfSharesInRate, cumTransactionCosts);
        }
        else
        {
            double initDelta = (double)fhcHelpers.ComputePriceAtHedgeDateFixed(gc, pg, pc, T, 0, obsDates, 0, initSpotList, "Delta");
            double oldWeightDeltaHedge = initDelta;
            var deltasList = new List<double> { initDelta };
            var hedgingDatesList = new List<int> { 0 };
            var hedgedSpotsList = new List<List<double>> { initSpotList };
            var sharesPurchased = new List<double> { initDelta };
            var costOfShares = new List<double> { initSpotList[0] * initDelta };
            List<double> cumCostOfSharesInRate = new List<double> { initSpotList[0] * initDelta };
            var cumTransactionCosts = new List<double> { 0.01* initSpotList[0] * initDelta };
            for (int i = 1; i < hedgeDates.Count; i++)
            {
                var hedgeDate = hedgeDates[i];
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
                double discountFactor = Math.Exp(-this.curve(hedgeDate / 252.0) / 100 * hedgeDate / 252.0);
                double weightDeltaHedge = delta;
                hedgingDatesList.Add(hedgeDate);
                deltasList.Add(delta);
                hedgedSpotsList.Add(initSpot);
                double diffWeight = weightDeltaHedge - oldWeightDeltaHedge;
                double costShares = diffWeight * initSpot[0];
                sharesPurchased.Add(diffWeight);
                costOfShares.Add(costShares);
                cumTransactionCosts.Add((double)fhcHelpers.UpdateTransactionCosts(cumTransactionCosts, initSpot, weightDeltaHedge, oldWeightDeltaHedge, discountFactor));
                double costSharesAtHedgeDate = cumCostOfSharesInRate[i - 1] * Math.Exp(-0.01 * this.curve(hedgeDate/252.0) * hedgeDate / 252) + costOfShares[i];
                cumCostOfSharesInRate.Add(costSharesAtHedgeDate);
                oldWeightDeltaHedge = weightDeltaHedge;
            }
            this.effectiveNbrHedgingEvents = hedgingDatesList.Count;
            return (hedgeDates, deltasList, hedgedSpotsList, sharesPurchased, costOfShares, cumCostOfSharesInRate, cumTransactionCosts);
        }
    }
    public object FHCMCFixedDeltaVegaHedges(GreeksComputations gc, PathGenerator pg, PayoffCalculator pc, double T, int stepSize = 252)
    {
        List<int> hedgeDates = FHCHelpers.ComputeHedgeDates(T, nbrHedges);
        List<int> obsDates = PayoffCalculator.ComputeObsDates(T, freqObs);
        var fhcHelpers = new FHCHelpers(this.n, this.N, this.notional, this.freqObs, this.BP, this.AT, this.coupon, this.curve, this.nbrHedges,this.closingpricesList);
        List<double> initSpotList = FHCHelpers.ExtractSpotList(this.closingpricesList, 0);
        int positionCounter = 0, couponCounter = 0, oldPosition = 0;
        var hedgingDatesList = new List<int> { 0 };
        var hedgedSpotsList = new List<List<double>> { initSpotList };
        if (this.n > 1)
        {
            
            var (priceBinAObj, deltaBinAObj, vegaBinAObj) = fhcHelpers.DigitalOptionPriceAndGreeks(initSpotList, initSpotList,T, this.curve(T/252) / 100, 0.2, 10, "call");
            var deltaBinAList = new List<double[]> { (double[])deltaBinAObj };
            var vegaBinAList = new List<double[]> { (double[])vegaBinAObj };
            var priceBinAList = new List<double[]> { (double[])priceBinAObj };
            var results = (ValueTuple<double[], double[]>)fhcHelpers.ComputePriceAtHedgeDateFixed(gc, pg, pc, T, 0, obsDates, 0, initSpotList, "Delta-Vega");
            double[] initDeltaArr = results.Item1;
            double[] initVegaArr = results.Item2;
           

            var (oldWeightDeltaHedge, oldWeightVegaHedge) = FHCHelpers.ComputeDeltaVegaWeights(this.n, deltaBinAList, vegaBinAList, initDeltaArr, initVegaArr);
            var deltas = new List<double[]> { initDeltaArr };
            var vegas = new List<double[]> { initVegaArr };
            var sharesPurchased = new List<double[]> { oldWeightDeltaHedge };
            var sharesPurchasedBinary = new List<double[]> { oldWeightVegaHedge };

            var (costOfShares, costOfSharesBinary, cumTransactionCosts, cumTransactionCostsBinary, cumCostOfSharesInRate, cumCostOfBinaryInRate) = fhcHelpers.InitializeDeltaVegaVectors(this.n, oldWeightDeltaHedge, oldWeightVegaHedge, initSpotList, priceBinAList);
            for (int i = 1; i < hedgeDates.Count(); i++)
            {
                int hedgeDate = hedgeDates[i];
                double residualMaturity = (T - hedgeDate / 252.0);
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
        weightVegaHedge, oldWeightVegaHedge, hedgedSpotsList, priceBinAList, costOfShares, costOfSharesBinary, cumCostOfSharesInRate, cumCostOfBinaryInRate, discountFactor);
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
            this.effectiveNbrHedgingEvents = hedgingDatesList.Count;
            return (hedgeDates,hedgedSpotsList, deltas, vegas, sharesPurchased, costOfShares, cumCostOfSharesInRate, cumTransactionCosts,
                 sharesPurchasedBinary, costOfSharesBinary, cumCostOfBinaryInRate, cumTransactionCostsBinary);

        }
        else
        {
            var (priceBinAObj, deltaBinAObj, vegaBinAObj) = fhcHelpers.DigitalOptionPriceAndGreeks(initSpotList, initSpotList, T, this.curve(T / 252) / 100, 0.2, 10, "call");
            var deltaBinAList = new List<double> { (double)deltaBinAObj };
            var vegaBinAList = new List<double> { (double)vegaBinAObj };
            var priceBinAList = new List<double> {(double)priceBinAObj };
            
            var results = (ValueTuple<double, double>)fhcHelpers.ComputePriceAtHedgeDateFixed(gc, pg, pc, T, 0, obsDates, 0, initSpotList, "Delta-Vega");
            double initDelta = results.Item1;
            double initVega = results.Item2;
            
            double oldWeightDeltaHedge = -(-initDelta + (initVega / vegaBinAList.Last()) * deltaBinAList.Last());
            double oldWeightVegaHedge = initVega / vegaBinAList.Last();
            var deltas = new List<double> { initDelta };
            var vegas = new List<double> { initVega };
           
            var sharesPurchased = new List<double> { oldWeightDeltaHedge };
            var costOfShares = new List<double> { oldWeightDeltaHedge * initSpotList[0]};
            List<double> cumTransactionCosts = new List<double> {0.01 * Math.Abs(oldWeightDeltaHedge * initSpotList[0])};
            List<double> cumTransactionCostsBinary = new List<double> { 0.01 * Math.Abs(oldWeightVegaHedge * priceBinAList.Last()) };
            var sharesPurchasedBinary = new List<double> { oldWeightVegaHedge };
            var costOfSharesBinary = new List<double> { oldWeightVegaHedge * priceBinAList.Last() };
            List<double> cumCostOfSharesInRate = new List<double> { initSpotList[0] * oldWeightDeltaHedge };
            List<double> cumCostOfBinaryInRate = new List<double> { oldWeightVegaHedge * priceBinAList.Last()};

            for (int i = 1; i < hedgeDates.Count(); i++)
            {
                
                int hedgeDate = hedgeDates[i];
                double discountFactor = Math.Exp(-0.01 * this.curve(hedgeDate / 252.0) * hedgeDate / 252.0);
                var (position, newOldPosition, newPositionCounter, newCouponCounter) = FHCHelpers.CheckPosition(obsDates, hedgeDate, couponCounter, positionCounter, oldPosition);
                positionCounter = newPositionCounter;
                couponCounter = newCouponCounter;
                oldPosition = newOldPosition;

                double residualMaturity = (T - hedgeDate / 252.0);
                var checkBreakHedge = (ValueTuple<List<double>, bool>)fhcHelpers.CheckBreakHedge(closingpricesList, position, obsDates, oldPosition, positionCounter, T, couponCounter, AT, BP, coupon, notional);
                List<double> finalPayoffsList = checkBreakHedge.Item1;
                bool breakState = checkBreakHedge.Item2;
                if (breakState)
                    break;

                List<double>  spotList = FHCHelpers.ExtractSpotList(closingpricesList, i);
                
                var result = (ValueTuple<double, double>)fhcHelpers.ComputePriceAtHedgeDateFixed(gc, pg, pc, T, couponCounter, obsDates, hedgeDate, spotList, "Delta-Vega");
                double deltaArr = result.Item1;
                double vegaArr = result.Item2;
               
                (priceBinAObj, deltaBinAObj, vegaBinAObj) = fhcHelpers.DigitalOptionPriceAndGreeks(spotList, spotList, residualMaturity, this.curve(residualMaturity)/100, 0.2, 10, "call");

                deltaBinAList.Add((double)deltaBinAObj);
                vegaBinAList.Add((double)vegaBinAObj);
                priceBinAList.Add((double)priceBinAObj);
                double weightVegaHedge = vegaArr / vegaBinAList.Last();
                double weightDeltaHedge = -(-deltaArr + weightVegaHedge * deltaBinAList.Last());
                hedgingDatesList.Add(hedgeDate);
                deltas.Add(deltaArr);
                hedgedSpotsList.Add(spotList);
                sharesPurchased.Add(weightDeltaHedge - oldWeightDeltaHedge);
                costOfShares.Add((weightDeltaHedge - oldWeightDeltaHedge) * hedgedSpotsList.Last()[0]);
                vegas.Add(vegaArr);
                sharesPurchasedBinary.Add(weightVegaHedge - oldWeightVegaHedge);
                costOfSharesBinary.Add((weightVegaHedge - oldWeightVegaHedge) * priceBinAList.Last());

                double costSharesAtHedgeDate = cumCostOfSharesInRate[i-1] *discountFactor + costOfShares[i];
                double costBinAtHedgeDate = cumCostOfBinaryInRate[i-1] * discountFactor + costOfSharesBinary[i];

                cumCostOfSharesInRate.Add(costSharesAtHedgeDate);
                cumCostOfBinaryInRate.Add(costBinAtHedgeDate);
                cumTransactionCosts.Add((double)fhcHelpers.UpdateTransactionCosts(cumTransactionCosts, spotList, weightDeltaHedge, oldWeightDeltaHedge, discountFactor));
                cumTransactionCostsBinary.Add((double)fhcHelpers.UpdateTransactionCosts(cumTransactionCostsBinary, priceBinAList, weightVegaHedge, oldWeightVegaHedge, discountFactor));

                this.lastBinValue = priceBinAList.Last(); 
                oldWeightDeltaHedge = weightDeltaHedge;
                oldWeightVegaHedge = weightVegaHedge;
                
            }


            return (hedgeDates, deltas, vegas, hedgedSpotsList, sharesPurchased, costOfShares, cumCostOfSharesInRate, cumTransactionCosts,
                 sharesPurchasedBinary, costOfSharesBinary, cumCostOfBinaryInRate, cumTransactionCostsBinary);
        }

    }
}
