using MathNet.Numerics.Distributions;
using MathNet.Numerics.Optimization;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using Python.Runtime;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra;
using System.Globalization;
using System.Data;
using System.IO;
using Microsoft.SqlServer.Server;


//namespace MyApplication
//{

public class BlackScholesOptionPricing
{
    public double S { get; set; }
    public double K { get; set; }
    public double r { get; set; }
    public double d { get; set; }
    public double T { get; set; }

    public BlackScholesOptionPricing(double S, double K, double r, double d, double T)
    {
        this.S = S;
        this.K = K;
        this.r = r;
        this.d = d;
        this.T = T;
    }

    public double d1(double sigma)
    {
        return (Math.Log(S / K) + (r - d + sigma * sigma / 2) * T) / (sigma * Math.Sqrt(T));
    }

    public double d2(double sigma)
    {
        return (Math.Log(S / K) + (r - d - sigma * sigma / 2) * T) / (sigma * Math.Sqrt(T));
    }

    public double CallPrice(double sigma)
    {
        return S * Math.Exp(-d * T) * Normal.CDF(0, 1, d1(sigma)) - K * Math.Exp(-r * T) * Normal.CDF(0, 1, d2(sigma));
    }

    public double PutPrice(double sigma)
    {
        return K * Math.Exp(-r * T) * Normal.CDF(0, 1, -d2(sigma)) - S * Math.Exp(-d * T) * Normal.CDF(0, 1, -d1(sigma));
    }

    public double ImpliedVolatility(double marketPrice, char optionFlag, int iterations = 100, double initialSigma = 0.2)
    {
        double sigmaEst = initialSigma;
        for (int i = 0; i < iterations; i++)
        {
            double vega = S * Math.Exp(-d * T) * Math.Sqrt(T) * Normal.PDF(0, 1, d1(sigmaEst));
            double priceDiff = (optionFlag == 'C' ? CallPrice(sigmaEst) : PutPrice(sigmaEst)) - marketPrice;
            sigmaEst -= priceDiff / vega;
        }
        return sigmaEst;
    }
    public double CallOptionDelta(double sigma)
    {
        double dOne = d1(sigma);
        return Normal.CDF(0, 1, dOne);
    }
    public double OptionGamma(double sigma)
    {
        double dOne = d1(sigma);
        return Normal.PDF(0, 1, dOne) / (this.S * sigma * Math.Sqrt(this.T));
    }
    public double OptionVega(double sigma)
    {
        double dOne = d1(sigma);
        return this.S * Normal.PDF(0, 1, dOne) * Math.Sqrt(this.T);
    }
}

public class NelsonSiegelSvenssonModel
{
    private double[] _maturities;
    private double[] _rates;
    private double _beta0;
    private double _beta1;
    private double _beta2;
    private double _beta3;
    private double _tau0;
    private double _tau1;

    public NelsonSiegelSvenssonModel(double[] maturities, double[] rates, double tau0 = 2.0, double tau1 = 5.0)
    {
        if (maturities == null || maturities.Length == 0)
            throw new ArgumentException("Maturities cannot be null or empty.");

        if (rates == null || rates.Length == 0)
            throw new ArgumentException("Rates cannot be null or empty.");

        if (maturities.Length != rates.Length)
            throw new ArgumentException("Maturities and rates must have the same length.");

        _maturities = maturities;
        _rates = rates;
        _tau0 = tau0;
        _tau1 = tau1;
        Calibrate();
    }

    private void Calibrate()
    {
        var initialGuess = Vector<double>.Build.Dense(new[] { _tau0, _tau1 });
        Func<Vector<double>, double> objectiveFunctionTau = parameters =>
        {
            double tau0 = parameters[0];
            double tau1 = parameters[1];

            var (beta0, beta1, beta2, beta3) = FitBetas(tau0, tau1);

            double[] predictedRates = _maturities.Select(t => NelsonSiegelSvensson(t, beta0, beta1, beta2, beta3, tau0, tau1)).ToArray();
            if (predictedRates.Length == 0)
                throw new InvalidOperationException("Predicted rates are empty.");

            double mse = _rates.Zip(predictedRates, (actual, predicted) => Math.Pow(actual - predicted, 2)).Average();
            return mse;
        };

        var optimizerTau = new NelderMeadSimplex(1e-3, 100000);
        var objectiveTau = ObjectiveFunction.Value(objectiveFunctionTau);
        var resultTau = optimizerTau.FindMinimum(objectiveTau, initialGuess);
        var parametersTauOptimized = resultTau.MinimizingPoint;

        _tau0 = parametersTauOptimized[0];
        _tau1 = parametersTauOptimized[1];

        (_beta0, _beta1, _beta2, _beta3) = FitBetas(_tau0, _tau1);
    }

    private (double, double, double, double) FitBetas(double tau0, double tau1)
    {
        int n = _maturities.Length;
        var matrix = DenseMatrix.OfArray(new double[n, 4]);

        for (int i = 0; i < n; i++)
        {
            double t = _maturities[i];
            matrix[i, 0] = 1.0;
            matrix[i, 1] = (1 - Math.Exp(-t / tau0)) / (t / tau0);
            matrix[i, 2] = matrix[i, 1] - Math.Exp(-t / tau0);
            matrix[i, 3] = (t / tau1) * Math.Exp(-t / tau1);
        }

        var y = DenseVector.OfArray(_rates);
        var matrixT = matrix.Transpose();
        var betas = (matrixT * matrix).Inverse() * matrixT * y;

        return (betas[0], betas[1], betas[2], betas[3]);
    }

    private double NelsonSiegelSvensson(double t, double beta0, double beta1, double beta2, double beta3, double tau0, double tau1)
    {
        double term1 = (1 - Math.Exp(-t / tau0)) / (t / tau0);
        double term2 = term1 - Math.Exp(-t / tau0);
        double term3 = (t / tau1) * Math.Exp(-t / tau1);
        return beta0 + beta1 * term1 + beta2 * term2 + beta3 * term3;
    }

    public double GetRate(double maturity)
    {

        return NelsonSiegelSvensson(maturity, _beta0, _beta1, _beta2, _beta3, _tau0, _tau1);
    }

    public static async Task<NelsonSiegelSvenssonModel> CalibrateNSSModelAsync(string filePath)
    {
        var ratesFetcher = new RatesFetcher();
        var keys = ratesFetcher.ReadKeysFromExcel(filePath);
        var actualRates = await ratesFetcher.ActualRates(keys);
        var maturities = new List<double>();
        var rates = new List<double>();

        foreach (KeyValuePair<string, string> kvp in actualRates)
        {
            string cleanKey = kvp.Key.Trim().Replace("SR_", "").Replace("Y", "").Replace("M", "");
            if (double.TryParse(cleanKey, NumberStyles.Any, CultureInfo.InvariantCulture, out double maturity))
            {
                string cleanValue = kvp.Value.Trim();
                if (double.TryParse(cleanValue, NumberStyles.Any, CultureInfo.InvariantCulture, out double rate))
                {
                    rates.Add(rate);
                    maturities.Add(maturity);
                }
            }
        }

        double[] maturitiesArray = maturities.ToArray();
        double[] ratesArray = rates.ToArray();

        return new NelsonSiegelSvenssonModel(maturitiesArray, ratesArray);
    }
}
public class OptionDataProcessor
{
    public static DataTable PreprocessData(DataTable data, NelsonSiegelSvenssonModel curve, double div)
    {

        var filteredData = data.AsEnumerable()
                                .Where(row => row.Field<string>("OPTION FLAG") == "P")
                                .CopyToDataTable();

        if (filteredData.Rows.Count == 0)
        {
            throw new InvalidOperationException("No data available after filtering.");
        }

        double S = GetDoubleFromDataTable(filteredData, 0, "SPOT");
        double d = div / S;

        var logMoneynessForward = filteredData.AsEnumerable()
                                                .Select(row =>
                                                {
                                                    try
                                                    {
                                                        double strike = GetDoubleFromDataTable(filteredData, row.Table.Rows.IndexOf(row), "STRIKE");
                                                        double mat = GetDoubleFromDataTable(filteredData, row.Table.Rows.IndexOf(row), "MAT");
                                                        double rate = curve.GetRate(mat);
                                                        return Math.Log(strike / S) + (rate / 100) * mat;
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        return double.NaN;
                                                    }
                                                })
                                                .ToList();

        var impliedVols = filteredData.AsEnumerable()
                                        .Select(row =>
                                        {
                                            try
                                            {
                                                double K = GetDoubleFromDataTable(filteredData, row.Table.Rows.IndexOf(row), "STRIKE");
                                                double T = GetDoubleFromDataTable(filteredData, row.Table.Rows.IndexOf(row), "MAT");
                                                double marketPrice = GetDoubleFromDataTable(filteredData, row.Table.Rows.IndexOf(row), "PRICE");
                                                double r = curve.GetRate(T) / 100;
                                                var optionPricing = new BlackScholesOptionPricing(S, K, r, d, T);
                                                return optionPricing.ImpliedVolatility(marketPrice, 'P');
                                            }
                                            catch (Exception ex)
                                            {
                                                return double.NaN;
                                            }
                                        })
                                        .ToArray();
        var impliedTotalVariances = new double[filteredData.Rows.Count];
        for (int i = 0; i < filteredData.Rows.Count; i++)
        {
            try
            {
                double mat = GetDoubleFromDataTable(filteredData, i, "MAT");
                double iv = impliedVols[i];

                if (double.IsNaN(iv))
                {
                    throw new InvalidOperationException($"Implied volatility is NaN for MAT: {mat}");
                }

                impliedTotalVariances[i] = mat * Math.Pow(iv, 2);
            }
            catch (Exception ex)
            {
                impliedTotalVariances[i] = double.NaN;
            }
        }

        var processedData = filteredData.Copy();
        processedData.Columns.Add("market_itv", typeof(double));
        processedData.Columns.Add("IV", typeof(double));
        processedData.Columns.Add("Log_Moneyness", typeof(double));
        for (int i = 0; i < processedData.Rows.Count; i++)
        {
            processedData.Rows[i]["market_itv"] = impliedTotalVariances[i];
            processedData.Rows[i]["IV"] = impliedVols[i];
            processedData.Rows[i]["Log_Moneyness"] = logMoneynessForward[i];
        }

        processedData.Columns["MAT"].ColumnName = "Maturity";
        var cleanedData = processedData.AsEnumerable()
                                        .Where(row => !double.IsNaN(row.Field<double>("IV")) && row.Field<double>("IV") >= 0)
                                        .CopyToDataTable();

        cleanedData.Columns.Add("rate", typeof(double));
        foreach (DataRow row in cleanedData.Rows)
        {
            try
            {
                row["rate"] = curve.GetRate(GetDoubleFromDataTable(cleanedData, row.Table.Rows.IndexOf(row), "Maturity")) / 100;
            }
            catch (Exception ex)
            {
                row["rate"] = double.NaN;
            }
        }

        return cleanedData;
    }
    public static double[] GetHistPricesFromDataTable(DataTable table, string columnName, int numberOfValues)
    {
        if (numberOfValues <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(numberOfValues), "Number of values must be greater than zero.");
        }
        if (!table.Columns.Contains(columnName))
        {
            throw new ArgumentException("Column not found.", nameof(columnName));
        }
        int startRowIndex = table.Rows.Count - numberOfValues;
        if (startRowIndex < 0)
        {
            startRowIndex = 0;
        }
        List<double> result = new List<double>();
        for (int i = startRowIndex; i < table.Rows.Count && result.Count < numberOfValues; i++)
        {
            object value = table.Rows[i][columnName];

            if (value == DBNull.Value)
            {
                continue;
            }

            double doubleValue;

            if (value is double)
            {
                doubleValue = (double)value;
            }
            else if (value is string strValue && double.TryParse(strValue, out double parsedValue))
            {
                doubleValue = parsedValue;
            }
            else
            {
                continue;
            }

            result.Add(doubleValue);
        }
        return result.ToArray();
    }

    public static double GetDoubleFromDataTable(DataTable table, int rowIndex, string columnName)
    {
        if (rowIndex < 0 || rowIndex >= table.Rows.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(rowIndex), "Row index is out of range.");
        }

        if (!table.Columns.Contains(columnName))
        {
            throw new ArgumentException("Column not found.", nameof(columnName));
        }

        object value = table.Rows[rowIndex][columnName];

        if (value == DBNull.Value)
        {
            throw new InvalidCastException($"Value in column '{columnName}' is DBNull.");
        }

        if (value is double doubleValue)
        {
            return doubleValue;
        }

        if (value is string strValue && double.TryParse(strValue, out double parsedValue))
        {
            return parsedValue;
        }

        throw new InvalidCastException($"Cannot cast value to double: {value}");
    }

}





