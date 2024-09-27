using OfficeOpenXml;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using System.Xml;
using System.Data;
using YahooFinanceApi;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Statistics;
using MathNet.Numerics.LinearAlgebra.Double;





public class RatesFetcher
{
    private static readonly HttpClient _httpClient = new HttpClient();
    static RatesFetcher()
    {
        ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.Commercial;
    }

    public List<string> ReadKeysFromExcel(string filePath)
    {
        var keys = new List<string>();

        using (var package = new ExcelPackage(new FileInfo(filePath)))
        {
            var worksheet = package.Workbook.Worksheets.First();
            var rowCount = worksheet.Dimension.Rows;

            for (int row = 2; row <= rowCount; row++)
            {
                var key = worksheet.Cells[row, 1].Text.Trim();
                if (!string.IsNullOrWhiteSpace(key))
                {
                    keys.Add(key);
                }
            }
        }

        return keys;
    }
    public async Task<string> FetchDataAsync(string key)
    {
        string entrypoint = "https://sdw-wsrest.ecb.europa.eu/service/data/";
        string dbId = ExtractDbId(key);
        string keyRemainder = ExtractKeyRemainder(key);
        string requestUrl = $"{entrypoint}{dbId}/{keyRemainder}?format=genericdata";

        var parameters = new Dictionary<string, string>
        {
            { "startPeriod", "2024-04-25" },
            { "endPeriod", "2024-04-25" }
        };

        var response = await _httpClient.GetAsync(BuildUrlWithParameters(requestUrl, parameters));
        response.EnsureSuccessStatusCode();
        var responseData = await response.Content.ReadAsStringAsync();
        return responseData;
    }

    private string ExtractDbId(string key)
    {
        var parts = key.Split('.');
        return parts.Length > 0 ? parts[0] : string.Empty;
    }

    private string ExtractKeyRemainder(string key)
    {
        var parts = key.Split('.');
        return parts.Length > 1 ? string.Join(".", parts.Skip(1)) : string.Empty;
    }

    private string BuildUrlWithParameters(string url, Dictionary<string, string> parameters)
    {
        var query = string.Join("&", parameters.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
        return $"{url}&{query}";
    }

    public Dictionary<string, string> ProcessResponseData(string responseData)
    {
        var results = new Dictionary<string, string>();

        var xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(responseData);

        var nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
        nsmgr.AddNamespace("message", "http://www.sdmx.org/resources/sdmxml/schemas/v2_1/message");
        nsmgr.AddNamespace("common", "http://www.sdmx.org/resources/sdmxml/schemas/v2_1/common");
        nsmgr.AddNamespace("generic", "http://www.sdmx.org/resources/sdmxml/schemas/v2_1/data/generic");

        XmlNode obsValueNode = xmlDoc.SelectSingleNode("//generic:ObsValue", nsmgr);
        if (obsValueNode != null)
        {
            string obsValue = obsValueNode.Attributes["value"].Value;
            results["ObsValue"] = obsValue;
        }
        else
        {
            results["ObsValue"] = "ObsValue node not found.";
        }

        XmlNode dataTypeFmNode = xmlDoc.SelectSingleNode("//generic:Value[@id='DATA_TYPE_FM']", nsmgr);
        if (dataTypeFmNode != null)
        {
            string dataTypeFmValue = dataTypeFmNode.Attributes["value"].Value;
            string extractedValue;

            if (dataTypeFmValue.EndsWith("Y"))
            {
                extractedValue = dataTypeFmValue.Substring(0, dataTypeFmValue.Length - 1);
                string withoutPrefix = dataTypeFmValue.Replace("SR_", "");
                string numericPartFinal = withoutPrefix.Replace("Y", "");
                extractedValue = numericPartFinal;
            }
            else if (dataTypeFmValue.EndsWith("M"))
            {
                extractedValue = dataTypeFmValue.Substring(0, dataTypeFmValue.Length - 1);
                string withoutPrefix = dataTypeFmValue.Replace("SR_", "");
                string numericPartFinal = withoutPrefix.Replace("M", "");
                double numericValue = double.Parse(numericPartFinal);
                extractedValue = (numericValue / 12).ToString();
            }
            else
            {
                extractedValue = dataTypeFmValue;
            }
            results["DATA_TYPE_FM"] = extractedValue;
        }
        else
        {
            results["DATA_TYPE_FM"] = "DATA_TYPE_FM node not found.";
        }

        return results;
    }
    public async Task<Dictionary<string, string>> ActualRates(List<string> keys)
    {
        var aggregatedResults = new Dictionary<string, string>();
        var tasks = new List<Task<string>>();

        foreach (var key in keys)
        {
            tasks.Add(FetchDataAsync(key));
        }

        var responses = await Task.WhenAll(tasks);

        foreach (var responseData in responses)
        {
            var results = ProcessResponseData(responseData);
            if (results.ContainsKey("DATA_TYPE_FM") && results.ContainsKey("ObsValue"))
            {
                var key = results["DATA_TYPE_FM"];
                var value = results["ObsValue"];
                aggregatedResults[key] = value;
            }
        }
        return aggregatedResults;
    }
    public async Task<(List<double>, List<double>)> ProcessRatesKeys(string pathRatesKeys)
    {
        var keys = ReadKeysFromExcel(pathRatesKeys);
        var actualRates = await ActualRates(keys);
        var maturities = new List<double>();
        var rates = new List<double>();
        var cultureInfo = new CultureInfo("en-US");
        foreach (var kvp in actualRates)
        {
            if (double.TryParse(kvp.Key, NumberStyles.Any, cultureInfo, out double maturity))
            {
                if (double.TryParse(kvp.Value, NumberStyles.Any, cultureInfo, out double rate))
                {
                    maturities.Add(maturity);
                    rates.Add(rate);
                }
            }
        }
        return (maturities, rates);
    }
}
public class AssetsCorrelationMatrixCalculator
{
    public List<string> _paths;

    public AssetsCorrelationMatrixCalculator(List<string> paths)
    {
        _paths = paths;
    }

    private bool IsPositiveDefinite(Matrix<double> matrix)
    {
        try
        {
            matrix.Cholesky();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private double FindSmallestEpsilonToRegularize(Matrix<double> matrix, double epsilonMin = 1e-10, double epsilonMax = 1.0, double tol = 1e-8)
    {
        if (epsilonMin >= epsilonMax)
            throw new ArgumentException("Invalid range: epsilonMin must be less than epsilonMax");

        while (epsilonMax - epsilonMin > tol)
        {
            double epsilonMid = (epsilonMin + epsilonMax) / 2.0;
            var identityMatrix = Matrix<double>.Build.DenseIdentity(matrix.RowCount);
            var perturbedMatrix = matrix + epsilonMid * identityMatrix;
            if (IsPositiveDefinite(perturbedMatrix))
            {
                epsilonMax = epsilonMid;
            }
            else
            {
                epsilonMin = epsilonMid;
            }
        }
        return epsilonMax;
    }

    private Matrix<double> HandleCorrMatrix(Matrix<double> matrix)
    {
        if (IsPositiveDefinite(matrix))
        {
            return matrix;
        }
        else
        {
            double epsilon = FindSmallestEpsilonToRegularize(matrix);
            var identityMatrix = Matrix<double>.Build.DenseIdentity(matrix.RowCount);
            matrix = matrix + epsilon * identityMatrix;
            return matrix;
        }
    }



    private async Task<Vector<double>> RetrieveHistPricesAsync(string path)
    {
        string stockName = Utility.GetDataFrameName(path);
        if (DataConfig.NamesTickers.TryGetValue(stockName, out string ticker))
        {
            //var historicalData = await Yahoo.GetHistoricalAsync("AIR.PA", DateTime.Now.AddDays(-10), DateTime.Now, Period.Daily);
            //double[] closingPrices = historicalData.Select(quote => (double)quote.Close).ToArray();
            //return Vector<double>.Build.DenseOfArray(closingPrices);
            double[] clsprices = new double[] { 33.2, 34.6, 30.5 };
            return Vector<double>.Build.DenseOfArray(clsprices);
        }
        else
        {
            return null;
        }
    }
    public async Task<Matrix<double>> GetCorrMatrixAsync()
    {
        var tasks = _paths.Select(path => RetrieveHistPricesAsync(path));
        //var historicalPrices = await Task.WhenAll(tasks);
        //double[][] priceArray = historicalPrices.Select(v => v.ToArray()).ToArray();
        //var correlationMatrix = Correlation.PearsonMatrix(priceArray);
        //var correlationMatrixMathNet = DenseMatrix.OfArray(correlationMatrix.ToArray());
        //return HandleCorrMatrix(correlationMatrixMathNet);
        double[,] array = new double[2, 2]
            {
                { 1, 0.3 },
                { 0.3, 1}
            };
        Matrix<double> correlationMatrixMathNet = DenseMatrix.OfArray(array);
        return correlationMatrixMathNet;
    }
}

//public class DividendFetcher
//{
//    //Method to fetch dividends for a list of tickers
//    public async Task<double> GetDividendsAsync(string ticker)
//    {
//        double dividend = await FetchDividendAsync(ticker);
//        return dividend;
//    }

//    //Method to fetch the most recent dividend for a given ticker
//    private async Task<double> FetchDividendAsync(string ticker)
//    {
//        try
//        {

//            var dividends = await Yahoo.GetDividendsAsync(ticker, DateTime.Now.AddYears(-1), DateTime.Now);
//            return (double)dividends.First().Dividend;
//        }
//        catch (Exception ex)
//        {
//            Console.WriteLine($"Error fetching data for {ticker}: {ex.Message}");
//            return 0.0;
//        }
//    }
//}




