using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Office2010.ExcelAc;
using DocumentFormat.OpenXml.Office2019.Excel.RichData2;
using DocumentFormat.OpenXml.VariantTypes;
using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;

public class Volatility
{
    public (double[], double[,]) PrepareInterpolation(double[] logmArr, double[,] paramsMatrix)
    {
        int rows = paramsMatrix.GetLength(0);
        var paramsMatrixMatrix = Matrix<double>.Build.DenseOfArray(paramsMatrix);
        var logmArrVector = Vector<double>.Build.Dense(logmArr);
        var columnVector = paramsMatrixMatrix.Column(4);
        var logmArrMatrix = logmArrVector.ToColumnMatrix();
        var resultMatrix = Matrix<double>.Build.Dense(logmArr.Length, paramsMatrix.GetLength(0));
        double[] column = new double[rows];
        for (int i = 0; i < rows; i++)
        {
            column[i] = paramsMatrix[i, 4];
        }
        for (int i = 0; i < logmArr.Length; i++)
        {
            for (int j = 0; j < rows; j++)
            {
                resultMatrix[i, j] = logmArr[i] - column[j];
            }
        }
        double[,] resultMatrixArray = resultMatrix.ToArray();
        double[,] logmOperationMatrix = resultMatrixArray;
        double[,] finalResultsMatrix = new double[paramsMatrix.GetLength(0), logmArr.Length];
        int cols = logmOperationMatrix.GetLength(1);
        double[,] result = new double[logmOperationMatrix.GetLength(0), cols];
        for (int j = 0; j < cols; j++)
        {
            for (int i = 0; i < logmOperationMatrix.GetLength(0); i++)
            {
                double term1 = paramsMatrix[j, 1];
                double term2 = paramsMatrix[j, 2];
                double term3 = paramsMatrix[j, 3];
                double term5 = paramsMatrix[j, 5];
                double logMoneynessValue = logmOperationMatrix[i, j];
                result[i, j] = term1 + term2 * (term3 * logMoneynessValue +
                              Math.Sqrt(Math.Pow(logMoneynessValue, 2) + Math.Pow(term5, 2)));

            }
        }
        var newMatrix = Matrix<double>.Build.Dense(result.GetLength(0), result.GetLength(1) + 1);
        for (int i = 0; i < result.GetLength(0); i++)
        {
            newMatrix[i, 0] = 0.0;

            for (int j = 0; j < result.GetLength(1); j++)
            {
                newMatrix[i, j + 1] = result[i, j];
            }
        }
        double[,] finalResult = newMatrix.ToArray();
        double[] ttmArray = new double[paramsMatrix.GetLength(0) + 1];
        ttmArray[0] = 0;
        for (int i = 0; i < paramsMatrix.GetLength(0); i++)
        {
            ttmArray[i + 1] = paramsMatrix[i, 0];
        }
        return (ttmArray, finalResult);
    }

    public (double[], double[]) ImpliedVol(int n, double[] logMoneynessForwardVec, double ttm,double[,] paramsMatrix)
    {
        List<double[]> logMoneynessForwardVecList = new List<double[]>();
        if (n == 1)
        {
            logMoneynessForwardVecList.Add(logMoneynessForwardVec);
        }
        List<double[]> allItvInterpolated = new List<double[]>();
        
        foreach (var logmArr in logMoneynessForwardVecList)
        {
            var preparedArrays = PrepareInterpolation(logmArr, paramsMatrix);
            double[] x = preparedArrays.Item1;
            double[,] resultMatrix = preparedArrays.Item2;
            List<double> interpolatedValuesList = new List<double>();
            for (int row = 0; row < resultMatrix.GetLength(0); row++)
            {
                double[] rowValues = Utility.GetRowFromMatrix(resultMatrix, row);
                interpolatedValuesList.Add(Interp1dLinearExtrapolate(x, rowValues, ttm));
            }
            double[] interpolatedValues = interpolatedValuesList.ToArray();
            allItvInterpolated.Add(interpolatedValues);
        }
        double[] finalItvInterpolatedValues = allItvInterpolated[0];
        double[] finalIvInterpolatedValues = finalItvInterpolatedValues.Select(itv => Math.Sqrt(itv / ttm)).ToArray();
        return (finalIvInterpolatedValues, finalItvInterpolatedValues);
    }

    public double[] ComputeLocalVol(int n, double[] logMoneynessForwardVec, double ttm,double[,] ParamsMatrix)
    {
        double epsilonTime = 1e-4;
        double epsilonLogMoneyness = 1e-4;
        Func<double, double, double[]> computeIvEpsilon = (epsTime, epsLogMoneyness) =>
            ImpliedVol(n, logMoneynessForwardVec.Select(x => x + epsLogMoneyness).ToArray(), ttm + epsTime, ParamsMatrix).Item2;

        var consts = ImpliedVol(n, logMoneynessForwardVec, ttm, ParamsMatrix).Item2;
        var constPlusEpsLogM = computeIvEpsilon(0.0, epsilonLogMoneyness);
        var constMinusEpsLogM = computeIvEpsilon(0.0, -epsilonLogMoneyness);
        var svIt = (computeIvEpsilon(epsilonTime, 0.0).Zip(computeIvEpsilon(-epsilonTime, 0.0), (a, b) => a - b).ToArray()).Select(x => x / (2 * epsilonTime)).ToArray();
        var svIm = (constPlusEpsLogM.Zip(constMinusEpsLogM, (a, b) => a - b).ToArray()).Select(x => x / (2 * epsilonLogMoneyness)).ToArray();
        var svImm = (constPlusEpsLogM.Zip(consts.Zip(constMinusEpsLogM, (a, b) => 2 * a - b), (a, b) => a - b).ToArray()).Select(x => x / Math.Pow(epsilonLogMoneyness, 2)).ToArray();
        return svIt.Zip(RegularizeG(DenomDupire(consts, svIm, svImm, logMoneynessForwardVec)), (a, b) => Math.Sqrt(a / b)).ToArray();
    }
    public static double Interp1dLinearExtrapolate(double[] x, double[] y, double ttm)
    {
        if (ttm <= x[0])
        {
            double slope = (y[1] - y[0]) / (x[1] - x[0]);
            return y[0] + slope * (ttm - x[0]);
        }
        else if (ttm >= x[x.Length - 1])
        {
            double slope = (y[y.Length - 1] - y[y.Length - 2]) / (x[x.Length - 1] - x[x.Length - 2]);
            return y[y.Length - 1] + slope * (ttm - x[x.Length - 1]);
        }
        else
        {
            int idx = System.Array.BinarySearch(x, ttm);
            if (idx < 0)
            {
                idx = ~idx - 1;
            }
            double x0 = x[idx];
            double x1 = x[idx + 1];
            double y0 = y[idx];
            double y1 = y[idx + 1];
            return y0 + (y1 - y0) * (ttm - x0) / (x1 - x0);
        }
    }

    public static double[] DenomDupire(double[] constant, double[] SVIm, double[] SVImm, double[] logMoneynessForwardVec)
    {
        double[] result = new double[SVIm.Length];

        for (int i = 0; i < SVIm.Length; i++)
        {
            double term1 = 1 - 0.5 * logMoneynessForwardVec[i] * SVIm[i] / constant[i];
            double term2 = 0.25 * SVIm[i] * SVIm[i] * (0.25 + 1.0 / constant[i]);
            result[i] = (term1 * term1) - term2 + 0.5 * SVImm[i];
        }

        return result;
    }

    public static double[] RegularizeG(double[] arr)
    {
        double meanPositive = arr.Where(x => x > 0).DefaultIfEmpty(0).Average();
        for (int i = 0; i < arr.Length; i++)
        {
            if (arr[i] < 0)
            {
                arr[i] = meanPositive;
            }
        }
        return arr;
    }
}

