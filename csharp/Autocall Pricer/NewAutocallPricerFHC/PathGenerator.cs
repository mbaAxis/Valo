using System;
using System.Collections.Generic;
using System.Linq;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Math;
using DocumentFormat.OpenXml.Spreadsheet;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

// 693

public class PathGenerator
{
    public int n { get; }
    public int N { get; }
    public List<string> Paths { get; }
    public List<double> initDiv { get; }
    public Func<double, double> Curve { get; } 
    public string VolType { get; }
    public List<double[,]> ListParamsMatrix { get; }
    public double[] HestonParams { get; } 
    public double InitVol { get; private set; }
    public Matrix<double> RandomMatrix { get; set; } 

    // Constructor
    public PathGenerator(int n, int N, List<string> paths, List<double> initdiv, Func<double, double> curve, string volType, List<double[,]> listParamsMatrix, double[] hestonParams, Matrix<double> randomMatrix)
    {
        this.n = n;
        this.N = N;
        this.Paths = paths;
        this.initDiv = initdiv;
        this.Curve = curve;
        this.VolType = volType;
        this.ListParamsMatrix = listParamsMatrix;
        this.HestonParams = hestonParams;
        this.InitVol = 0;
        this.RandomMatrix = randomMatrix;
    }
    
    public static object StressSliceSpot(int n, object arr, string shock)
    {
        double factor = (shock == "Up") ? 0.1 : (shock == "Down") ? -0.1 : 0;
        double[] arrSlice = (double[])arr;
        return arrSlice.Select(value => value + factor).ToArray();
    }
    public static object StressSliceVol(int n, object arr, string shock)
    {
        double factor = (shock == "Up") ? 0.05 : (shock == "Down") ? -0.05 : 0;
        double[] arrSlice = (double[])arr;
        return arrSlice.Select(value => value + factor).ToArray();
    }
    public object ProcessRandomMatrix(Matrix<double> L, double T, Matrix<double> LHeston, Matrix<double> randomMat, bool heston = false)
    {
        int alpha = heston && this.n == 1 ? this.n + 1 : this.n;
        if (alpha > 1)
        {
            if (this.VolType != "SV")
            {
                var correlatedMatrix = randomMat * L;
                return correlatedMatrix;
            }
            else
            {
                double rho = this.HestonParams[4];
                Vector<double> randomVectorS = randomMat.Column(0);
                Vector<double> randomVectorV = randomMat.Column(1);
                var correlatedVector = rho * randomVectorS + Math.Sqrt(1 - rho * rho) * randomVectorV;
                Matrix<double> matrix = Matrix<double>.Build.Dense(randomVectorS.Count, 2);
                matrix.SetColumn(0, randomVectorS);
                matrix.SetColumn(1, correlatedVector);
                return matrix;
            }
        }
        else
        {
            double[] randomVariables = new double[this.RandomMatrix.RowCount];
            for (int j = 0; j < this.RandomMatrix.ColumnCount; j++)
            {
                for (int i = 0; i < this.RandomMatrix.RowCount; i++)
                {
                    randomVariables[i] = this.RandomMatrix[i, j];
                }
            }
            var correlatedVector = Vector<double>.Build.Dense(randomVariables);
            return correlatedVector;
        }


    }
    public object PrepareDividends(double T)
    {

        int timeSteps = Convert.ToInt16(T * 252.0 + 1);
        List<double[,]> divMatFinal = new List<double[,]>();
        for (int k = 0; k < this.initDiv.Count; k++)
        {
            int startQuotient = 0;
            double decreasAmount = 0;
            double[,] divMat = new double[this.N, Convert.ToInt16(T * 252.0 + 1)];
            for (int j = 0; j < Convert.ToInt16(T * 252.0 + 1); j++)
            {
                int quotient = j / Convert.ToInt16(252.0);
                if (quotient != startQuotient)
                {
                    decreasAmount = decreasAmount + 0.2;
                    startQuotient = quotient;
                }
                for (int i = 0; i < this.N; i++)
                {
                    divMat[i, j] = this.initDiv[0] - quotient * decreasAmount;
                }
            }
            divMatFinal.Add(divMat);
        }
        return this.n > 1 ? (object)divMatFinal : divMatFinal[0];
    }

    public (object SMatrix, object vol) PrepareDiffusion(double T, List<double> initSpotList)
    {
        int nbrSteps = (int)Math.Ceiling(252 * T);
        List<double[]> volList = new List<double[]>();
        List<double[,]> SMatrixList = new List<double[,]>();
        for (int i = 0; i < this.n; i++)
        {
            double[] volItem = Enumerable.Repeat(0.25, this.N).ToArray();
            double[,] SMatrix = new double[this.N, nbrSteps + 1];
            double initSpot = initSpotList[i];
            for (int j = 0; j < this.N; j++)
            {
                SMatrix[j, 0] = initSpot;
            }
            volList.Add(volItem);
            SMatrixList.Add(SMatrix);
        }
        return this.n > 1 ? ((object)SMatrixList, (object)volList) : ((object)SMatrixList[0], (object)volList[0]); 
    }
    public (double r, double residualMaturity) ComputeR(int step, double T, double timeStep)
    {
        double residualMaturity = T - step * timeStep;
        double r = this.Curve(residualMaturity) / 100;
        return (r, residualMaturity);
    }
    public object ComputeVolSlice(double[] logMoneynessForward, double residualMaturity, double[,] paramsMatrix)
    {

        var volatility = new Volatility();
        if (this.VolType == "BS_IV")
        {
            var result = volatility.ImpliedVol(1, logMoneynessForward, residualMaturity, paramsMatrix);
            return result.Item1;
        }
        else if (this.VolType == "LV")
        {
            return volatility.ComputeLocalVol(1, logMoneynessForward, residualMaturity, paramsMatrix);
        }
        else
        {
            throw new InvalidOperationException("Unknown volatility type");
        }
    }


    public (double[,], double[,]) PrepareHestonDiffusion(double T, List<double> initSpotList)
    {
        double initSpot = initSpotList[0];
        double v0 = HestonParams[0];
        double kappa = HestonParams[1];
        double theta = HestonParams[2];
        double sigma = HestonParams[3];
        double rho = HestonParams[4];
        this.InitVol = Math.Sqrt(v0);
        int nbrSteps = (int)Math.Ceiling(252 * T);
        double[,] SMatrix = new double[this.N, nbrSteps + 1];
        double[,] VMatrix = new double[this.N, nbrSteps + 1];
        for (int i = 0; i < this.N; i++)
        {
            SMatrix[i, 0] = initSpot;
            VMatrix[i, 0] = v0;
        }
        return (SMatrix, VMatrix);
    }

    public double[] UpdateMatrix(double[] sliceBefore, int i, double r, double[] dividends, double[] Vol, double timeStep, Matrix<double> L, double T, int k, Vector<double> randCol, bool antithetic = false)
    {
        Vector<double> rv = randCol.SubVector(i * this.N, this.N);
        if (antithetic == true)
        {
            rv = rv * -1;
        }
        double[] resultdoub = new double[this.N];
        for (int a = 0; a < this.N; a++)
        {
            resultdoub[a] = sliceBefore[a] * Math.Exp((r - (dividends[a]/sliceBefore[a]) - Vol[a] * Vol[a] * 0.5) * timeStep + Vol[a] * Math.Sqrt(timeStep) * rv[a]);
        }
        return resultdoub;
    }

    public (double[], double[]) UpdateMatricesHeston(double[] slcSBefore, double[] slcVBefore, double r, Matrix<double> LHeston,
                                        double T, int index, double timestep, Matrix<double> randomMat, bool antithetic = false)
    {
        
        var slcSBeforeVec = Vector<double>.Build.Dense(slcSBefore);
        var slcVBeforeVec = Vector<double>.Build.Dense(slcVBefore);
        double v0 = HestonParams[0];
        double kappa = HestonParams[1];
        double theta = HestonParams[2];
        double sigma = HestonParams[3];
        double rho = HestonParams[4];

        var rv0 = randomMat.SubMatrix(index * this.N, this.N, 0, 1).Column(0);
        var rv1 = randomMat.SubMatrix(index * this.N, this.N, 1, 1).Column(0);
        if (antithetic == true)
        {
            rv0 = -1 * rv0;
            rv1 = -1 * rv1;
        }
        Vector<double> Vi = slcVBeforeVec + kappa * (theta - slcVBeforeVec.Map(x => Math.Max(x, 0))) * timestep +
            sigma * (slcVBeforeVec.Map(x => Math.Max(x, 0)).Map(x => Math.Sqrt(x * timestep))).PointwiseMultiply(rv1)
            + 0.25 * Math.Pow(sigma, 2) * (rv1.PointwiseMultiply(rv1) - 1) * timestep;
        Vector<double> Si = slcSBeforeVec + r * slcSBeforeVec * timestep + slcSBeforeVec.PointwiseMultiply(slcVBeforeVec.Map(x => Math.Max(x, 0)).Map(x => Math.Sqrt(x * timestep))).PointwiseMultiply(rv0) +
            0.5 * (slcSBeforeVec.PointwiseMultiply(slcVBeforeVec.Map(x => Math.Max(x, 0)))).PointwiseMultiply(rv0.PointwiseMultiply(rv0) - 1) * timestep;
        return (Si.ToArray(), Vi.ToArray());
    }

    public (double[,], double[,]) SpotStressing(double[,] SMatrix, double[,] SMatrixAnti,string shock)
    {
        double[] firstSlice = Enumerable.Range(0, SMatrix.GetLength(0)).Select(row => SMatrix[row, 0]).ToArray();
        double[] stressedArray = (double[])StressSliceSpot(1, firstSlice, shock);
        for (int j = 0; j < SMatrix.GetLength(0); j++)
        {
            SMatrix[j, 0] = stressedArray[j];
            SMatrixAnti[j, 0] = stressedArray[j];
        }
        return (SMatrix, SMatrixAnti);
    }
    public (double[,], double[,])VarianceStressing(double[,] VMatrix, double[,] VMatrixAnti, string shock)
    {
        double factor = (shock == "Up") ? 1.0025 : (shock == "Down" ? 0.9975 : 1);
        double[] firstSlice = Enumerable.Range(0, VMatrix.GetLength(0)).Select(row => VMatrix[row, 0]).ToArray();
        double[] stressedArray = firstSlice.Select(x => x * factor).ToArray();
        for (int j = 0; j < VMatrix.GetLength(0); j++)
        {
            VMatrix[j, 0] = stressedArray[j];
            VMatrixAnti[j, 0] = stressedArray[j];
        }
        return (VMatrix, VMatrixAnti);
    }
    public double[] ComputeLogMoneyness(int i,double r, double residualMaturity, double[,] SMatrix)
    {
        int sliceIndex = (i != 0 ? i - 1 : i);
        double[] actualArray = Utility.GetColumnFromMatrix(SMatrix,i);
        double[]  prevArray = Utility.GetColumnFromMatrix(SMatrix, sliceIndex);
        var currentVector = Vector<double>.Build.DenseOfArray(actualArray.Cast<double>().ToArray());
        var previousVector = Vector<double>.Build.DenseOfArray(prevArray.Cast<double>().ToArray());
        double expTerm = Math.Exp(r * residualMaturity);
        var denominatorVector = previousVector * expTerm;
        Vector<double> resultVector = (currentVector.PointwiseDivide(denominatorVector)).PointwiseLog();
        double[] logMoneynessForward = resultVector.ToArray();
        return logMoneynessForward;
    }
    public object GeneratePaths(double T, List<double> initSpotList, string greeks, string shock, double assetToShift)
    {
        double timeStep = 1.0 / 252;
        if (this.n > 1)
        {
            var (diffusionSMatrixList,volList) = PrepareDiffusion(T, initSpotList);
            List<double[,]> SMatrix3DList = (List<double[,]>)diffusionSMatrixList;
            List<double[]> VolList = (List<double[]>)volList;

            List<double[,]> DividendsList = (List<double[,]>)PrepareDividends(T);
            var assetCorrMatrix = new AssetsCorrelationMatrixCalculator(this.Paths);
            Matrix<double> correlationMatrix = assetCorrMatrix.GetCorrMatrixAsync().GetAwaiter().GetResult();
            Matrix<double> L = correlationMatrix.Cholesky().Factor;
            List<double[,]> finalSmatrixList = new List<double[,]>();
            Matrix<double> randomMat = (Matrix<double>)ProcessRandomMatrix(L, T, null, this.RandomMatrix, false);
            for (int k = 0; k < this.n; k++)
            {
                List<double[,]> SMatrixAntiList = new List<double[,]>();
                Vector<double> randColMat = randomMat.Column(k);
                double[,] SMatrix = SMatrix3DList[k];
                double[,] divMatrix = DividendsList[k];
                double[,] paramsMatrix = this.ListParamsMatrix[k];
                int SMatrixRows = SMatrix.GetLength(0); 
                int SMatrixColumns = SMatrix.GetLength(1);
                double[,] SMatrixAnti = (double[,])SMatrix.Clone();

                for (int i = 0; i < SMatrixColumns - 1; i++)
                {
                    var (r, residualMaturity) = ComputeR(i, T, timeStep);
                    if (i == 0 && (greeks == "Delta" || greeks == "Gamma") && assetToShift == k)
                    {
                        (SMatrix,SMatrixAnti) = SpotStressing(SMatrix,SMatrixAnti, shock);
                    }
                    double[] logMoneynessForward = ComputeLogMoneyness(i, r, residualMaturity, SMatrix);
                    double[] vol = (double[])ComputeVolSlice(logMoneynessForward, residualMaturity, paramsMatrix);
                    if (i == 0)
                    {
                        this.InitVol = vol.Average();
                    }
                    if ((greeks == "Vega" || greeks == "Vomma") && VolType != "SV" && assetToShift==k)
                    {
                        vol = (double[])StressSliceVol(1, vol, shock);
                    }
                    if (greeks == "Vanna" && assetToShift == k)
                    {
                        if (shock == "Up_down")
                        {
                            if(i == 0)
                            {
                                (SMatrix, SMatrixAnti) = SpotStressing(SMatrix, SMatrixAnti, "Up");
                            }
                            vol = (double[])StressSliceVol(1, vol, "Down");
                            
                        }
                        else if (shock == "Down_up")
                        {
                            if (i == 0)
                            {
                                (SMatrix, SMatrixAnti) = SpotStressing(SMatrix, SMatrixAnti, "Down");
                            }
                            vol = (double[])StressSliceVol(1, vol, "Up");
                        }
                        else
                        {
                            if (i == 0)
                            {
                                (SMatrix, SMatrixAnti) = SpotStressing(SMatrix, SMatrixAnti, shock);
                            }
                            vol = (double[])StressSliceVol(1, vol, shock);
                        }
                    }
                    double[] slcBefore = Utility.GetColumnFromMatrix(SMatrix, i);
                    double[] slcBeforeAnti = Utility.GetColumnFromMatrix(SMatrixAnti, i);
                    double[] divSlc = Utility.GetColumnFromMatrix(divMatrix, i); 
                    double[] newSMatrixSlice = UpdateMatrix(slcBefore, i, r, divSlc, vol, timeStep, L, T, k, randColMat);
                    double[] newSMatrixSliceAnti = UpdateMatrix(slcBeforeAnti, i, r, divSlc, vol, timeStep, L, T, k, randColMat, true);
                    for (int j = 0; j < this.N; j++)
                    {
                        SMatrix[j, i + 1] = newSMatrixSlice[j];
                        SMatrixAnti[j, i + 1] = newSMatrixSliceAnti[j]; 
                    }
                    
                }
                //double[,] result = new double[SMatrix.GetLength(0), SMatrix.GetLength(1)];
                //for (int i = 0; i < SMatrix.GetLength(0); i++)
                //{
                //    for (int j = 0; j < SMatrix.GetLength(1); j++)
                //    {
                //        result[i, j] = (SMatrix[i, j] + SMatrixAnti[i, j]) / 2;
                //    }
                //}
                //finalSmatrixList.Add(result);
                finalSmatrixList.Add(SMatrix);
            }
            return finalSmatrixList;
        }
        else
        {
            Matrix<double> randomMat = Matrix<double>.Build.Dense(this.RandomMatrix.RowCount, this.RandomMatrix.ColumnCount);
            Vector<double> randomVec = Vector<double>.Build.Dense(this.RandomMatrix.RowCount);
            double[,] SMatrix = null;
            double[,] SMatrixAnti = null;
            double[,] VMatrix = null;
            double[,] VMatrixAnti = null;
            Matrix<double> LHeston = null;
            double[,] divMatrix = null;
            double[] vol = null;

            if (this.VolType != "SV")
            {
                var (diffusionSMatrix,voldiff) = PrepareDiffusion(T, initSpotList);
                SMatrix = (double[,])diffusionSMatrix;
                vol = (double[]) voldiff;
                divMatrix = (double[,])PrepareDividends(T);
                SMatrixAnti = SMatrix;
                randomVec = (Vector<double>)ProcessRandomMatrix(null, T, null, this.RandomMatrix, false);
            }
            else
            {
                (SMatrix, VMatrix) = PrepareHestonDiffusion(T, initSpotList);
                SMatrixAnti = SMatrix;
                VMatrixAnti = VMatrix;
                double rho = this.HestonParams[4];
                Matrix<double> cov = DenseMatrix.OfArray(new double[,]
                           {
                                { 1.0, rho },
                                { rho, 1.0 }
                           });
                LHeston = cov.Cholesky().Factor;
                randomMat = (Matrix<double>)ProcessRandomMatrix(null, T, LHeston, this.RandomMatrix, true);
            }
            int SMatrixRows = SMatrix.GetLength(0);
            int SMatrixColumns = SMatrix.GetLength(1);
            for (int i = 0; i < SMatrix.GetLength(1) - 1; i++)
            {
                var (r, residualMaturity) = ComputeR(i, T, timeStep);
                if (i == 0 && (greeks == "Delta" || greeks == "Gamma"))
                {
                    (SMatrix, SMatrixAnti) = SpotStressing(SMatrix, SMatrixAnti, shock);
                }
                if (this.VolType == "LV" || this.VolType == "BS_IV")
                {
                    double[] logMoneynessForward = ComputeLogMoneyness(i,r, residualMaturity,SMatrix);
                    var volVec = ComputeVolSlice(logMoneynessForward, residualMaturity, this.ListParamsMatrix[0]);
                    vol = (double[])volVec;
                    if (i == 0)
                    {
                        this.InitVol = vol.Average();
                    }
                    if (greeks == "Vega" || greeks == "Vomma")
                    {
                        vol = (double[])StressSliceVol(this.n, vol, shock);
                    }
                }

                if (i == 0 && (greeks == "Vega" || greeks == "Vomma") && VolType == "SV")
                {
                    (VMatrix, VMatrixAnti) = VarianceStressing(VMatrix, VMatrixAnti, shock);
                }
                if (greeks == "Vanna")
                {
                    if (shock == "Up_down")
                    {
                        if(i == 0)
                        {
                            (SMatrix, SMatrixAnti) = SpotStressing(SMatrix, SMatrixAnti, "Up");

                        }
                        
                        if (VolType != "SV")
                        {
                            vol = (double[])StressSliceVol(this.n, vol, "Down");
                        }
                        if (VolType == "SV" && i == 0)
                        {
                            (VMatrix, VMatrixAnti) = VarianceStressing(VMatrix, VMatrixAnti, "Down");
                        }
                    }
                    else if (shock == "Down_up")
                    {
                        if (i == 0)
                        {
                            (SMatrix, SMatrixAnti) = SpotStressing(SMatrix, SMatrixAnti, "Down");

                        }
                        if (VolType != "SV")
                        {
                            vol = (double[])StressSliceVol(this.n, vol, "Up");
                        }
                        if (VolType == "SV" && i == 0)
                        {
                            (VMatrix, VMatrixAnti) = VarianceStressing(VMatrix, VMatrixAnti, "Up");
                        }
                    }
                    else
                    {
                        if (i == 0)
                        {
                            (SMatrix, SMatrixAnti) = SpotStressing(SMatrix, SMatrixAnti, shock);
                        }
                        if (VolType != "SV")
                        {
                            vol = (double[])StressSliceVol(this.n, vol, shock);
                        }
                        if (VolType == "SV" && i == 0)
                        {
                            (VMatrix, VMatrixAnti) = VarianceStressing(VMatrix, VMatrixAnti, shock);
                        }
                    }
                }
                if (VolType != "SV")
                {
                    double[] slcBefore = Utility.GetColumnFromMatrix(SMatrix, i);
                    double[] slcBeforeAnti = Utility.GetColumnFromMatrix(SMatrixAnti, i);
                    double[] divSlc = Utility.GetColumnFromMatrix(divMatrix, i);
                    double[] newSMatrixSlice = UpdateMatrix(slcBefore, i, r,divSlc, vol, timeStep, null, T, 0, randomVec);
                    double[] newSMatrixSliceAnti = UpdateMatrix(slcBeforeAnti, i, r, divSlc, vol, timeStep, null, T, 0, randomVec, true);
                    for (int j = 0; j < SMatrixRows; j++)
                    {
                        SMatrix[j, i + 1] = newSMatrixSlice[j];
                        SMatrixAnti[j, i + 1] = newSMatrixSliceAnti[j];
                    }
                }
                else if (VolType == "SV")
                {
                    double[] slcBefore = Utility.GetColumnFromMatrix(SMatrix, i);
                    double[] slcBeforeAnti = Utility.GetColumnFromMatrix(SMatrixAnti, i);
                    double[] varSlc = Utility.GetColumnFromMatrix(VMatrix, i);
                    double[] varSlcAnti = Utility.GetColumnFromMatrix(VMatrixAnti, i);
                    (double[] slcS, double[] slcV) = UpdateMatricesHeston(slcBefore, varSlc, r, LHeston, T, i, timeStep, randomMat);
                    (double[] slcSAnti, double[] slcVAnti) = UpdateMatricesHeston(slcBeforeAnti, varSlcAnti, r, LHeston, T, i, timeStep, randomMat, true);

                    for (int j = 0; j < SMatrixRows; j++)
                    {
                        SMatrix[j, i + 1] = slcS[j];
                        VMatrix[j, i + 1] = slcV[j];
                        SMatrixAnti[j, i + 1] = slcSAnti[j];
                        VMatrixAnti[j, i + 1] = slcVAnti[j];
                    }
                }
            }
            double[,] result = new double[SMatrix.GetLength(0), SMatrix.GetLength(1)];
            for (int i = 0; i < SMatrix.GetLength(0); i++)
            {
                for (int j = 0; j < SMatrix.GetLength(1); j++)
                {
                    result[i, j] = (SMatrix[i, j] + SMatrixAnti[i, j]) / 2;
                }
            }
            return result;
            //return SMatrix;
        }
    }
}


