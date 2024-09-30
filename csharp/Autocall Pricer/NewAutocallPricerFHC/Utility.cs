using CsvHelper;
using OfficeOpenXml;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra;
using Python.Runtime;
using MathNet.Numerics.Distributions;
using System.Linq;
using Excel = Microsoft.Office.Interop.Excel;
using System.Runtime.InteropServices;
using MathNet.Numerics.Random;
using Microsoft.SqlServer.Server;
using System.Xml.Linq;
using DocumentFormat.OpenXml.Wordprocessing;

public class QMCGeneration
{
    public Matrix<double> GenerateRandomMatrix(int n, int N, double T, bool heston = false)
    {
        int tint = (int)T+1;
        double mean = 0.0;
        double stdDev = 1.0;
        int alpha = heston && n == 1 ? n + 1 : n;
        double[,] randomMatrix = new double[N * tint * 252, alpha];
        int rows = N * tint * 252;
        var randomSource = new MersenneTwister();
        var normalDist = new Normal(mean, stdDev, randomSource);
        for (int i = 0; i < alpha; i++)
        {
            var columnSamples = normalDist.Samples().Take(rows).ToArray();
            for (int j = 0; j < rows; j++)
            {
                randomMatrix[j, i] = columnSamples[j];
            }
        }
        return Matrix<double>.Build.DenseOfArray(randomMatrix);
    }
}
public static class Utility
{
    public static  double ComputeFHCSum(string hedgeType, DataTable dt1,  DataTable dt2)
    {
        if (hedgeType == "Delta")
        {
            //PrintDataTable(dt1);
            return Convert.ToDouble(dt1.Rows[dt1.Rows.Count - 1]["CumDiscTransCosShar"]);
        }
        else if (hedgeType == "Delta-Vega")
        {
            return Convert.ToDouble(dt2.Rows[dt2.Rows.Count - 1]["CumDiscTransCosShar"]) +
                Convert.ToDouble(dt2.Rows[dt2.Rows.Count - 1]["CumDiscTransCosOpt"]);
        }
        else
        {
            return 0;
        }

    }
    
    public static void ExportToOpenExcel(Dictionary<string, DataTable> dataTables, string filePath)
    {
        Excel.Application excelApp = null;
        Excel.Workbook workbook = null;

        try
        {
            // Get the running instance of Excel
            excelApp = (Excel.Application)Marshal.GetActiveObject("Excel.Application");

            // Find the workbook by file path
            foreach (Excel.Workbook wb in excelApp.Workbooks)
            {
                if (wb.FullName.Equals(filePath, StringComparison.CurrentCultureIgnoreCase))
                {
                    workbook = wb;
                    break;
                }
            }

            // If the workbook is not open, throw an exception
            if (workbook == null)
            {
                throw new Exception("Workbook not found. Make sure the Excel file is open.");
            }

            // Add data tables to the workbook
            foreach (var kvp in dataTables)
            {
                string sheetName = kvp.Key;
                DataTable dataTable = kvp.Value;

                // Check if the sheet already exists; if not, add a new one
                Excel.Worksheet worksheet = null;
                try
                {
                    worksheet = (Excel.Worksheet)workbook.Sheets[sheetName];
                }
                catch
                {
                    // Sheet does not exist, create a new one
                    worksheet = (Excel.Worksheet)workbook.Sheets.Add(After: workbook.Sheets[workbook.Sheets.Count]);
                    worksheet.Name = sheetName;
                }

                // Load data from DataTable to Excel sheet
                for (int i = 0; i < dataTable.Columns.Count; i++)
                {
                    worksheet.Cells[1, i + 1] = dataTable.Columns[i].ColumnName;
                }

                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    for (int j = 0; j < dataTable.Columns.Count; j++)
                    {
                        worksheet.Cells[i + 2, j + 1] = dataTable.Rows[i][j];
                    }
                }
            }

            // Save the changes to the workbook
            workbook.Save();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            // Release COM objects
            if (workbook != null)
            {
                Marshal.ReleaseComObject(workbook);
            }
            if (excelApp != null)
            {
                Marshal.ReleaseComObject(excelApp);
            }
            workbook = null;
            excelApp = null;
            GC.Collect();
        }
    }
    public static void ExporttoExcel(Dictionary<string, DataTable> dataTables, string filePath)
    {
        using (var package = new ExcelPackage())
        {
            foreach (var kvp in dataTables)
            {
                string sheetName = kvp.Key;
                DataTable dataTable = kvp.Value;
                var worksheet = package.Workbook.Worksheets.Add(sheetName);
                worksheet.Cells["A1"].LoadFromDataTable(dataTable, true);
            }
            FileInfo fi = new FileInfo(filePath);
            package.SaveAs(fi);
        }
    }

    public static void ScatterPlot2D(double[] xData,double[] yData,string xlabel, string ylabel, string title)
    {
        var myPlot = new ScottPlot.Plot();
        myPlot.Add.ScatterLine(xData,yData);
        myPlot.Axes.Bottom.Label.Text = xlabel;
        myPlot.Axes.Left.Label.Text = ylabel;
        myPlot.Axes.Title.Label.Text = title;
        myPlot.SavePng(title+".png", 400, 300);
    }

    public static async Task<(List<DataTable> DataList, List<double> InitialSpotList, List<double> InitialDividends, List<string> Tickers)> RetrieveMarketData(List<string> pathsList, NelsonSiegelSvenssonModel nssModel)
    {
        var dataList = new List<DataTable> { };
        List<double> initialSpotList = new List<double>();
        List<string> tickers = new List<string>();
        List<double> initialDividends = new List<double>();
        for (int idx = 0; idx < pathsList.Count; idx++)
        {
            string filePath = pathsList[idx];
            DataTable data = Utility.LoadDataTableFromExcel(filePath);
            var dataPreprocessed = OptionDataProcessor.PreprocessData(data, nssModel, 0);
            dataList.Add(dataPreprocessed);
            double spotValue = Convert.ToDouble(dataPreprocessed.Rows[0]["SPOT"]);
            initialSpotList.Add(spotValue);
            string dfName = Utility.GetDataFrameName(filePath);
            var constants = new Constants();
            string ticker = constants.GetTicker(dfName);
            tickers.Add(ticker);
            //DividendFetcher divFetcher = new DividendFetcher();
            //double div = await divFetcher.GetDividendsAsync(ticker);
            double div = 1;
            initialDividends.Add(div);
        }
        return (dataList, initialSpotList, initialDividends, tickers);
    }
    public static List<double[]> ProcessHistoricalClosingPrices(List<string> historicalDataPaths)
    {
        List<double[]> closingPricesList = new List<double[]>();
        for (int i = 0; i < historicalDataPaths.Count; i++)
        {
            var historicalData = Utility.LoadDataTableFromExcel(historicalDataPaths[i]);
            double[] closeValues = new double[historicalData.Rows.Count];
            for (int k = 0; k < closeValues.Length; k++)
            {
                closeValues[k] = OptionDataProcessor.GetDoubleFromDataTable(historicalData, k, "Close");
            }
            closingPricesList.Add(closeValues);
        }
        return closingPricesList;
    }

    public static List<int> PrintListInts(List<int> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            Console.Write(list[i]);
        }
        return list;
    }
    public static double[,] Transpose(double[,] matrix)
    {
        int rows = matrix.GetLength(0);
        int cols = matrix.GetLength(1);
        double[,] transposed = new double[cols, rows];

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                transposed[j, i] = matrix[i, j];
            }
        }

        return transposed;
    }
    public static double[] GetRowFromMatrix(double[,] matrix, int row)
    {
        int columns = matrix.GetLength(1);
        double[] rowValues = new double[columns];
        for (int j = 0; j < columns; j++)
        {
            rowValues[j] = matrix[row, j];
        }
        return rowValues;
    }

    public static double[] GetColumnFromMatrix(double[,] matrix, int column)
    {
        int rows = matrix.GetLength(0);
        double[] columnValues = new double[rows];
        for (int j = 0; j < rows; j++)
        {
            columnValues[j] = matrix[j, column];
        }
        return columnValues;
    }

    public static void PrintDataTable(DataTable table)
    {
        foreach (DataColumn column in table.Columns)
        {
            Console.Write($"{column.ColumnName}\t");
        }
        Console.WriteLine();
        foreach (DataRow row in table.Rows)
        {
            {
                foreach (var item in row.ItemArray)
                {
                    Console.Write($"  {item}\t");
                }
                Console.WriteLine();
            }
        }
    }
    public static DataTable LoadDataTableFromCsv(string filePath)
    {
        var dataTable = new DataTable();

        using (var reader = new StreamReader(filePath))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            using (var dr = new CsvDataReader(csv))
            {
                dataTable.Load(dr);
            }
        }

        return dataTable;
    }
    public static DataTable LoadDataTableFromExcel(string filePath)
    {
        var dataTable = new DataTable();

        using (var package = new ExcelPackage(new FileInfo(filePath)))
        {
            var worksheet = package.Workbook.Worksheets[0];
            foreach (var headerCell in worksheet.Cells[1, 1, 1, worksheet.Dimension.End.Column])
            {
                dataTable.Columns.Add(headerCell.Text);
            }
            for (int rowIndex = 2; rowIndex <= worksheet.Dimension.End.Row; rowIndex++)
            {
                var row = dataTable.NewRow();
                for (int colIndex = 1; colIndex <= worksheet.Dimension.End.Column; colIndex++)
                {
                    row[colIndex - 1] = worksheet.Cells[rowIndex, colIndex].Text;
                }
                dataTable.Rows.Add(row);
            }
        }

        return dataTable;
    }
    public static string GetDataFrameName(string path)
    {
        string filename = Path.GetFileName(path);
        string filenameWithoutExtension = Path.GetFileNameWithoutExtension(filename);
        int endIndex = filenameWithoutExtension.LastIndexOf('\\');
        string dataframeName = endIndex != -1 ? filenameWithoutExtension.Substring(endIndex + 1) : filenameWithoutExtension;

        return dataframeName;
    }
    public static double[,] Reshape(this double[] array, int rows, int cols)
    {
        if (array.Length != rows * cols)
        {
            throw new ArgumentException("Array length does not match the specified dimensions.");
        }

        double[,] result = new double[rows, cols];
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                result[i, j] = array[i * cols + j];
            }
        }

        return result;
    }
    public static void PrintMatrices(List<double[,]> listOfMatrices)
    {
        for (int matrixIndex = 0; matrixIndex < listOfMatrices.Count; matrixIndex++)
        {
            double[,] matrix = listOfMatrices[matrixIndex];
            Console.WriteLine($"Matrix {matrixIndex + 1}:");

            PrintMatrix(matrix);
        }
    }

    public static void PrintMatrix(double[,] matrix)
    {
        int rows = matrix.GetLength(0);
        int cols = matrix.GetLength(1);

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                Console.Write($"{matrix[i, j],6:F7} ");
            }
            Console.WriteLine();
        }
        Console.WriteLine();
    }
}