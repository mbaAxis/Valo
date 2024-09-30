using Accord.Math.Wavelets;

using Flurl.Util;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;

public class PayoffCalculator
{
    private int n;
    private int N;
    private int freqObs;
    private double BP;
    private double AT;
    private double coupon;

    public PayoffCalculator(int n, int N, int freqObs, double BP, double AT, double coupon)
    {
        this.n = n;
        this.N = N;
        this.freqObs = freqObs;
        this.BP = BP;
        this.AT = AT;
        this.coupon = coupon;
    }

    public static List<int> ComputeObsDates(double T, int freqObs, int stepSize = 252)
    {// computes the observation dates
        var obsDates = new List<int>();
        int obsDate = (int)Math.Floor(252 * T);
        while (obsDate > 1)
        {
            obsDates.Insert(0, obsDate);
            obsDate -= stepSize / freqObs;
        }
        return obsDates;
    }

    public Dictionary<double,double> ComputeRedemptionProbability(object matrix)
    {// computes the probability that an autocall will be redeemed at a certain observation date
        Dictionary<double, double> probabilities = new Dictionary<double, double>();
        if (this.n > 1)
        {
            return null;
        }
        else
        {
            double[,] payMatrix = (double[,])matrix;
            for (int i = 0;i<payMatrix.GetLength(1); i++)
            {
                double redeemed = 0;
                double obdate = i;
                for (int j = 0; j < payMatrix.GetLength(0); j++)
                {
                    if (payMatrix[j,i]!= 0)
                    {
                        redeemed += 1;
                    }
                }
                probabilities[obdate] = redeemed / payMatrix.GetLength(0);
            }
            return probabilities;
        }
    }

    public object ComputeEvaluationMatrix(object matrix, List<double> initSpotList, List<int> obsDates)
    {
        List<double[,]> evaluationMatrixList = new List<double[,]>();
        List<double[,]> sMatrixList = new List<double[,]>();
        if (this.n > 1)
        {
            sMatrixList = (List<double[,]>)matrix;
        }
        else
        {
            sMatrixList.Add((double[,])matrix);
        }
        for (int i = 0; i < this.n; i++)
        { 
                double[,] sMatrix = sMatrixList[i];
                double initSpot = initSpotList[i];
                double[,] evalMatrix = new double[sMatrix.GetLength(0), obsDates.Count];
                for (int j = 0; j < sMatrix.GetLength(0); j++)
                {
                    for (int k = 0; k < obsDates.Count; k++)
                    {
                        int obs = obsDates[k];
                        evalMatrix[j, k] = sMatrix[j, obs] / initSpot;
                    }
                }
                evaluationMatrixList.Add(evalMatrix);
        }
        return this.n > 1 ? (object)evaluationMatrixList : evaluationMatrixList[0];
    }

    public object ComputePayoff(object evaluationMatrixList, double T, List<int> obsDates, int totalNbrObs, int k = 0, double smoothingFactor = 1000)
    {
        double[] ATArray = Enumerable.Repeat(AT, obsDates.Count).ToArray();

        if (n > 1)
        {
            List<double[,]> evaluationMatrix = (List<double[,]>)evaluationMatrixList;
            List<double[,]> resultPayoffsList = new List<double[,]>();
            for (int i = 0; i < this.n; i++)
            {
                double[,] evalMat = evaluationMatrix[i];
                double[,] resultPayoff = ComputePayoffSingle(evalMat, T, obsDates, totalNbrObs, ATArray, k, smoothingFactor);
                resultPayoffsList.Add(resultPayoff);
            }
            return resultPayoffsList;
        }
        else
        {
            double[,] evaluationMatrix = (double[,])evaluationMatrixList;
            return ComputePayoffSingle(evaluationMatrix, T, obsDates, totalNbrObs, ATArray, k, smoothingFactor);
        }
    }
    private double[,] ComputePayoffSingle(double[,] evaluationMatrix, double T, List<int> obsDates, int totalNbrObs, double[] AT, int k, double smoothingFactor)
    {
        double[,] stateMatrix = Initialize2DArray(evaluationMatrix.GetLength(0), obsDates.Count, 1.0);
        double[,] payoffCouponMatrix = Initialize2DArray(evaluationMatrix.GetLength(0), obsDates.Count, 0.0);
        double[,] payoffKgMatrix = Initialize2DArray(evaluationMatrix.GetLength(0), obsDates.Count, 0.0);
        for (int obs = 0; obs < obsDates.Count - 1; obs++)
        {
            double[] condition1 = new double[evaluationMatrix.GetLength(0)];
            for (int i = 0; i < evaluationMatrix.GetLength(0); i++)
            {
                condition1[i] = (evaluationMatrix[i, obs] >= AT[obs]) ? 1 : 0;
            }
            
            double[] stateMin = GetRowMinima(stateMatrix);
            for (int i = 0; i < evaluationMatrix.GetLength(0); i++)
            {
                payoffCouponMatrix[i, obs] = (T * (obs + 1 + k) / totalNbrObs) * this.coupon * stateMin[i] * condition1[i];
                payoffKgMatrix[i, obs] = stateMin[i] * condition1[i];
                stateMatrix[i, obs] *= 1 - condition1[i];
            }
        }
        double[] finalStateMin = GetRowMinima(stateMatrix);
        double[] lastColEval = new double[evaluationMatrix.GetLength(0)];
        for (int i = 0; i < evaluationMatrix.GetLength(0); i++)
        {
            lastColEval[i] = evaluationMatrix[i, obsDates.Count - 1];
        }
        (var payoffCouponFinal, var payoffKgFinal) = CalculateFinalPayoffs(T, lastColEval, finalStateMin, AT.Last(), BP, coupon, smoothingFactor);

        for (int i = 0; i < evaluationMatrix.GetLength(0); i++)
        {
            payoffCouponMatrix[i, obsDates.Count - 1] = payoffCouponFinal[i];
            payoffKgMatrix[i, obsDates.Count - 1] = payoffKgFinal[i];

        }
        var payoffMatrix = AddMatrices(payoffCouponMatrix, payoffKgMatrix);
        return payoffMatrix;
    }

    public (double[], double[]) CalculateFinalPayoffs(double T, double[] evaluationFinal, double[] stateMin, double ATFinal, double BP, double coupon, double smoothingFactor)
    {
        double[] conditionAT = new double[this.N];
        double[] conditionBP = new double[this.N];
        double[] payoffCouponFinal = new double[this.N];
        double[] payoffKgFinal = new double[this.N];

        for (int i = 0; i < evaluationFinal.Length; i++)
        {
            if (evaluationFinal[i] >= ATFinal)
            {

                payoffCouponFinal[i] = T * coupon * stateMin[i];
                payoffKgFinal[i] = stateMin[i];
            }
            if (evaluationFinal[i] < ATFinal && evaluationFinal[i] >= BP)
            {

                payoffCouponFinal[i] = 0;
                payoffKgFinal[i] = 1 * stateMin[i];
            }
            if (evaluationFinal[i] < BP)
            {

                payoffCouponFinal[i] = 0;
                payoffKgFinal[i] = evaluationFinal[i] * stateMin[i];
            }

        }
        return (payoffCouponFinal, payoffKgFinal);
    }

    public static double[] Sigmoid(double[] x, double k)
    {
        return x.Select(val => 1.0 / (1.0 + Math.Exp(-k * val))).ToArray();
    }

    public static double[,] Sigmoid(double[,] x, double k)
    {
        int rows = x.GetLength(0);
        int cols = x.GetLength(1);
        double[,] result = new double[rows, cols];
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                result[i, j] = 1.0 / (1.0 + Math.Exp(-k * x[i, j]));
            }
        }
        return result;
    }

    public static double ComputePrice(int n, double notional, object matrice, Func<double, double> curve, List<int> obsDates)
    {
        double price = 0.0;
        if (n > 1)
        {
            List<double[,]> payoffMatrixList = (List<double[,]>)matrice;
            for (int i = 0; i < obsDates.Count; i++)
            {
                List<double> columnValues = new List<double>();
                foreach (var matrix in payoffMatrixList)
                {
                    for (int j = 0; j < matrix.GetLength(0); j++)
                    {
                        columnValues.Add(matrix[j, i]);
                    }
                }
                double mean = columnValues.Average();
                price += mean * Math.Exp(-0.01 * curve(obsDates[i] / 252.0) * obsDates[i] / 252.0);
            }
        }
        else
        {
            double[,] payoffMatrix = (double[,])matrice;
            for (int i = 0; i < obsDates.Count; i++)
            {
                List<double> columnValues = new List<double>();
                for (int j = 0; j < payoffMatrix.GetLength(0); j++)
                {
                    columnValues.Add(payoffMatrix[j, i]);
                }
                double mean = columnValues.Average();
                price += mean * Math.Exp(-0.01 * curve(obsDates[i] / 252.0) * obsDates[i] / 252.0);
            }
        }
        return price*100;
    }
    private static double[,] Initialize2DArray(int rows, int cols, double value)
    {
        double[,] array = new double[rows, cols];
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                array[i, j] = value;
            }
        }
        return array;
    }

    public double[] GetRowMinima(double[,] matrix)
    {
        int rows = matrix.GetLength(0);
        int cols = matrix.GetLength(1);
        double[] rowMinima = new double[rows];

        for (int i = 0; i < rows; i++)
        {
            double min = matrix[i, 0];
            for (int j = 1; j < cols; j++)
            {
                if (matrix[i, j] < min)
                {
                    min = matrix[i, j];
                }
            }
            rowMinima[i] = min;
        }

        return rowMinima;
    }
    private static double[,] AddMatrices(double[,] a, double[,] b)
    {
        int rows = a.GetLength(0);
        int cols = a.GetLength(1);
        double[,] result = new double[rows, cols];
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                result[i, j] = a[i, j] + b[i, j];
            }
        }
        return result;
    }
}