using System;
using System.Linq;
using System.Numerics;
using MathNet.Numerics.Optimization;
using MathNet.Numerics.LinearAlgebra;
using Accord.Math;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data;
using Accord.Math.Optimization;
using MathNetVector = MathNet.Numerics.LinearAlgebra.Vector<double>;


public class SVIModel
{
    public int n { get; private set; }
    public List<DataTable> ListData { get; private set; }
    public string MethodName { get; private set; }
    public int MaxIter { get; private set; }
    public double Epsilon { get; private set; }

    public SVIModel(int nValue, List<DataTable> listData, string methodName = "SLSQP", int maxIter = 50, double epsilon = 1e-12)
    {
        n = nValue;
        ListData = listData;
        MethodName = methodName;
        MaxIter = maxIter;
        Epsilon = epsilon;
    }
    public static double[] G(double[] parameters, double[] x)
    {
        /*
        Density function of SVI
        :param parameters: SVI parameters (a, b, rho, m, sig)
        :param x: log moneyness forward vector
        :return: array of values to respect a constraint in the minimization
        */

        double a = parameters[0];
        double b = parameters[1];
        double rho = parameters[2];
        double m = parameters[3];
        double sig = parameters[4];
        int logMoneynessLength = x.Length;
        double[] results = new double[logMoneynessLength];

        for (int i = 0; i < logMoneynessLength; i++)
        {
            double xi = x[i];
            double discr = Math.Sqrt(Math.Pow(xi - m, 2) + Math.Pow(sig, 2));
            double w = a + b * (rho * (xi - m) + discr);
            double dw = b * rho + b * (xi - m) / discr;
            double d2w = b * Math.Pow(sig, 2) / Math.Pow(discr, 3);

            results[i] = Math.Pow(1 - (xi * dw) / (2 * w), 2)
                         - (Math.Pow(dw, 2) / 4) * (1 / w + 0.25)
                         + d2w / 2;
        }

        return results;
    }
    public static double Svi(double logMoneynessForward, double[] parameters)
    {
        /*
        SVI model function
        :param logMoneynessForward: log-moneyness forward
        :param parameters: SVI parameters (a, b, rho, m, s)
        :return: SVI value
        */
        double a = parameters[0];
        double b = parameters[1];
        double rho = parameters[2];
        double m = parameters[3];
        double s = parameters[4];

        return a + b * (rho * (logMoneynessForward - m) + Math.Sqrt(Math.Pow(logMoneynessForward - m, 2) + Math.Pow(s, 2)));
    }

    public static double MsLoss(double[] parameters, double[] logMoneynessForward, double[] marketItv)
    {
        /*
        Mean Squared Loss function
        :param parameters: SVI parameters
        :param logMoneynessForward: log-moneyness forward array
        :param marketItv: market implied total variance
        :return: loss value
        */
        return logMoneynessForward
            .Select((t, i) => Math.Pow(Svi(t, parameters) - marketItv[i], 2))
            .Sum();
    }

    private double CalendarSpreadArbitrage(double[] parameters, double[] lastParams, double[] logMoneynessForward)
    {
        double constraint = 0.0;
        for (int i = 0; i < logMoneynessForward.Length; i++)
        {
            constraint += Svi(logMoneynessForward[i], parameters) - Svi(logMoneynessForward[i], lastParams);
        }
        return constraint;
    }

    private double ButterflyArbitrage(double[] parameters, double[] logMoneynessForward)
    {
        double constraint = 0.0;
        for (int i = 0; i < logMoneynessForward.Length; i++)
        {
            constraint += Math.Abs(Svi(logMoneynessForward[i], parameters) - 1e-4);
        }
        return constraint;
    }
    public double[] Calibration(double[] lastParams, double[] marketItv, double[] logMoneynessForward, double[] initParams)
    {
        double optRmse = 1;
        var solver = new Cobyla(numberOfVariables: initParams.Length)
        {
            MaxIterations = MaxIter
        };

        Func<double[], double> objectiveFunction = parameters =>
        {
            double loss = MsLoss(parameters, logMoneynessForward, marketItv);
            double constraintPenalty = 0.0;
            constraintPenalty += Math.Max(0, -CalendarSpreadArbitrage(parameters, lastParams, logMoneynessForward));
            return loss + 1e4 * constraintPenalty;
        };

        solver.Function = objectiveFunction;

        for (int i = 1; i <= MaxIter; i++)
        {
            bool success = solver.Minimize(initParams);
            double aOpt = solver.Solution[0];
            double bOpt = solver.Solution[1];
            double rhoOpt = solver.Solution[2];
            double mOpt = solver.Solution[3];
            double sOpt = solver.Solution[4];
            initParams = new[] { aOpt, bOpt, rhoOpt, mOpt, sOpt };
            double optRmse1 = MsLoss(initParams, logMoneynessForward, marketItv);
            if (i > 1 && Math.Abs(optRmse - optRmse1) < Epsilon)
            {
                break;
            }
            optRmse = optRmse1;
        }

        return initParams;
    }

    public List<double[,]> ParamsSkew(int numberParameters = 5)
    {
        var resultList = new List<double[,]>();
        for (int i = 0; i < n; i++)
        {
            var data = ListData[i];
            var sortedTtmVec = data.AsEnumerable()
                .Select(row => row.Field<object>("Maturity"))
                .Where(value => value != DBNull.Value)
                .Select(value => Convert.ToDouble(value))
                .Distinct()
                .OrderBy(ttm => ttm)
                .ToArray();
            var paramsMatrix = new double[sortedTtmVec.Length, numberParameters + 1];

            for (int j = 0; j < sortedTtmVec.Length; j++)
            {
                paramsMatrix[j, 0] = sortedTtmVec[j];
            }

            var paramsList = new List<double[]>();
            var lastParams = new double[5];
            foreach (var ttm in sortedTtmVec)
            {
                var subset = data.AsEnumerable()
                .Where(row =>
                {
                    object value = row.Field<object>("Maturity");
                    if (value != DBNull.Value)
                    {
                        try
                        {
                            double numericValue = Convert.ToDouble(value);
                            return numericValue == ttm;
                        }
                        catch (FormatException)
                        {
                            return false;
                        }
                    }
                    return false;
                }).ToList();
                var logMoneynessForward = subset.Select(row => row.Field<double>("Log_Moneyness")).ToArray();
                var marketItv = subset.Select(row => row.Field<double>("market_itv")).ToArray();
                var initParams = new double[] { 0.5 * marketItv.Min(), 0.1, -0.5, 0.1, 0.1 };
                var calibratedParams = Calibration(lastParams, marketItv, logMoneynessForward, initParams);
                paramsList.Add(calibratedParams);
                lastParams = calibratedParams;
            }
            for (int k = 0; k < sortedTtmVec.Length; k++)
            {
                double[] array = paramsList[k];
                for (int j = 0; j < numberParameters; j++)
                {
                    paramsMatrix[k, j + 1] = array[j];
                }
            }
            resultList.Add(paramsMatrix);
        }
        return resultList;
    }
}


public class StopIterationException : Exception
{
    public StopIterationException() : base("Iteration has been stopped.") { }
    public StopIterationException(string message) : base(message) { }
    public StopIterationException(string message, Exception inner) : base(message, inner) { }
}

public class HestonCalibrator
{
    
    private DataTable data;
    public double S0 { get; private set; }
    public double[] r { get; private set; }
    public double[] K { get; private set; }
    public double[] T { get; private set; }
    public double[] P { get; private set; }
    public string OptionFlag { get; private set; }
    public Dictionary<string, (double x0, double[] lbub)> Params { get; private set; }
    public double[] X0 { get; private set; }
    public (double, double)[] Bnds { get; private set; }
    public double BestErr { get; private set; }
    public double[] BestParams { get; private set; }
    public bool IsGlobalOptimization { get; private set; }
    public HestonCalibrator(DataTable data)
    {
        this.data = data;
        var marketData = GetMarketData();
        S0 = marketData.S0;
        r = marketData.r;
        K = marketData.K;
        T = marketData.T;
        P = marketData.P;
        OptionFlag = "P";
        Params = new Dictionary<string, (double x0, double[] lbub)>
        {
            {"v0", (0.03843822476489981, new[] { 1e-3, 1.0 })},
            {"kappa", (1.8892855530063508, new[] { 1e-3, 1e3 })},
            {"theta", (0.12208180452649998, new[] { 1e-3, 1.0 })},
            {"sigma", (0.7318189328705834, new[] { 1e-3, 1.0 })},
            {"rho", (- 0.09009001815979012, new[] { -1.0, 1.0 })}
        };


        X0 = Params.Values.Select(p => p.x0).ToArray();
        Bnds = Params.Values.Select(p => (p.lbub[0], p.lbub[1])).ToArray();
        BestErr = double.PositiveInfinity;
        BestParams = (double[])X0.Clone();
    }

    public (double S0, double[] r, double[] K, double[] T, double[] P) GetMarketData()
    {

        double S0 = Convert.ToDouble(data.Rows[0]["SPOT"]);
        double[] r = data.AsEnumerable()
                     .Select(row => Convert.ToDouble(row.Field<string>("rate")))
                     .ToArray();
        double[] K = data.AsEnumerable()
                         .Select(row => Convert.ToDouble(row.Field<string>("STRIKE")))
                         .ToArray();
        double[] T = data.AsEnumerable()
                         .Select(row => Convert.ToDouble(row.Field<string>("Maturity")))
                         .ToArray();
        double[] P = data.AsEnumerable()
                         .Select(row => Convert.ToDouble(row.Field<string>("PRICE")))
                         .ToArray();
        return (S0, r, K, T, P);
    }


    public static Complex Integrand(Complex x, double T, double v0, double kappa, double theta, double sigma, double rho)
    {
        Complex i = Complex.ImaginaryOne;

        Complex b = 2 * (i * x * rho * sigma + kappa) / (sigma * sigma);
        Complex eta = Complex.Sqrt(b * b + 4 * (x * x - i * x) / (sigma * sigma));
        Complex g = 0.5 * (b - eta);
        Complex h = (b - eta) / (b + eta);
        double q = 0.5 * sigma * sigma * T;

        Complex firstTerm = 2 * kappa * theta * (q * g - Complex.Log((1 - h * Complex.Exp(-eta * q)) / (1 - h))) / (sigma * sigma);
        Complex secondTerm = v0 * g * ((1 - Complex.Exp(-eta * q)) / (1 - h * Complex.Exp(-eta * q)));

        Complex H_hat = Complex.Exp(firstTerm + secondTerm);
        return H_hat;
    }

    public static double[] HestonPriceLewisApproach(
        double S0,
        double[] K,
        double[] r,
        double[] T,
        double v0,
        double kappa,
        double theta,
        double sigma,
        double rho,
        string optionFlag)
    {
        double[] prices = new double[K.Length];
        var umax = 100.0;
        var N = 100000;
        var dk = umax / N;
        for (int i = 0; i < K.Length; i++)
        {
            double X = Math.Log(S0 / K[i]) + r[i] * T[i];
            Complex P = Complex.Zero;
            for (int n = 0; n < N; n++)
            {
                Complex k = n * dk + Complex.ImaginaryOne / 2;
                Complex integrandValue = Integrand(k, T[i], v0, kappa, theta, sigma, rho);
                P += dk * (integrandValue * Complex.Exp(-Complex.ImaginaryOne * k * X) / (k * k - Complex.ImaginaryOne * k));
            }
            double callPrice = S0 - K[i] * Math.Exp(-r[i] * T[i]) * (P.Real / Math.PI);
            prices[i] = optionFlag == "C" ? callPrice : callPrice - S0 + K[i] * Math.Exp(-r[i] * T[i]);
        }
        return prices;
    }

    public double SqErr(double[] x)
    {

        double v0 = x[0];
        double kappa = x[1];
        double theta = x[2];
        double sigma = x[3];
        double rho = x[4];
        double[] prices = HestonPriceLewisApproach(S0, K, r, T, v0, kappa, theta, sigma, rho, OptionFlag);
        double err = prices.Zip(P, (p, marketP) => Math.Pow(marketP - p, 2)).Sum() / P.Length;
        if (err < 1.0)
        {
            BestErr = err;
            BestParams = (double[])x.Clone();
            throw new StopIterationException("Error threshold reached. Stopping optimization.");
        }
        if (err < BestErr)
        {
            BestErr = err;
            BestParams = (double[])x.Clone();
        }
        return err;
    }

    public double[] Run()
    {
        try
        {

            var x0 = MathNetVector.Build.DenseOfArray(X0);
            var lowerBounds = MathNetVector.Build.DenseOfArray(Bnds.Select(b => b.Item1).ToArray());
            var upperBounds = MathNetVector.Build.DenseOfArray(Bnds.Select(b => b.Item2).ToArray());
            Func<MathNetVector, double> objectiveFunction = x =>
            {
                double[] parameters = x.ToArray();
                return SqErr(parameters);
            };
            var objective = ObjectiveFunction.Value(objectiveFunction);
            var optimizer = new NelderMeadSimplex(1e-5, 100);
            var result = optimizer.FindMinimum(objective, x0);
        }
        catch (StopIterationException ex)
        {
            Console.WriteLine(ex.Message);
        }
        double[] bestParams = new double[] { BestParams[0], BestParams[1], BestParams[2], BestParams[3], BestParams[4] };
        return bestParams;
    }
}



