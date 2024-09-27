using System;
using System.Collections.Generic;
using System.Data;

public class Constants
{
    // Dictionary to store names and tickers
    public Dictionary<string, string> NamesTickers { get; private set; }

    // List to store paths
    public List<string> Paths { get; private set; }

    public double[,] Matrix1 { get; private set; }
    public double[,] Matrix2 { get; private set; }

    // Constructor to initialize the data
    public Constants()
    {
        // Initialize the dictionary with names and tickers
        NamesTickers = new Dictionary<string, string>
        {
            { "AXA_15_05_2024", "CS.PA" },
            { "AXA_25_04_2024", "CS.PA" },
            { "AIRBUS_25_04_2024", "AIR.PA" },
            { "BNP Paribas_25_04_2024", "BNP.PA" },
            { "CAPGEMINI_25_04_2024", "CAP.PA" },
            { "ENGIE_25_04_2024", "ENGI.PA" },
            { "LVMH_25_04_2024", "MC.PA" },
            { "ORANGE_25_04_2024", "ORA.PA" }
        };

        // Initialize the list with paths
        Paths = new List<string>
        {
            @"C:\Users\m.ben-el-ghoul\PycharmProjects\Autocall pricer V2\data and keys\AXA_25_04_2024.xlsx",
            @"C:\Users\m.ben-el-ghoul\PycharmProjects\Autocall pricer V2\data and keys\AIRBUS_25_04_2024.xlsx",
            @"C:\Users\m.ben-el-ghoul\PycharmProjects\Autocall pricer V2\data and keys\BNP Paribas_25_04_2024.xlsx",
            @"C:\Users\m.ben-el-ghoul\PycharmProjects\Autocall pricer V2\data and keys\CAPGEMINI_25_04_2024.xlsx",
            @"C:\Users\m.ben-el-ghoul\PycharmProjects\Autocall pricer V2\data and keys\ENGIE_25_04_2024.xlsx",
            @"C:\Users\m.ben-el-ghoul\PycharmProjects\Autocall pricer V2\data and keys\LVMH_25_04_2024.xlsx",
            @"C:\Users\m.ben-el-ghoul\PycharmProjects\Autocall pricer V2\data and keys\ORANGE_25_04_24.xlsx"
        };


    }

    // Method to get the ticker for a given name
    public string GetTicker(string name)
    {
        return NamesTickers.ContainsKey(name) ? NamesTickers[name] : null;
    }

    // Method to get all paths
    public List<string> GetPaths()
    {
        return Paths;
    }

    public static DataTable CreateDeltaHedgeTable(int n)
    {
        var deltaHedging = new DataTable();
        deltaHedging.Columns.Add("HedgeDate", typeof(int));
        if (n == 1)
        {
            deltaHedging.Columns.Add("HedgedSpot", typeof(double));
        }
        else
        {
            deltaHedging.Columns.Add("HedgedSpotAsset1", typeof(double));
            deltaHedging.Columns.Add("HedgedSpotAsset2", typeof(double));
        }
        deltaHedging.Columns.Add("Delta", typeof(double));
        deltaHedging.Columns.Add("SharesPurchased", typeof(double));
        deltaHedging.Columns.Add("SharesHeld", typeof(double));
        deltaHedging.Columns.Add("CostSharesPurchased", typeof(double));
        deltaHedging.Columns.Add("CumCosSharPurchasedInRate", typeof(double));
        deltaHedging.Columns.Add("CumDiscTransCosShar", typeof(double));
        return deltaHedging;
    }
    public static DataTable CreateDeltaVegaHedgeTable(int n)
    {
        var deltaVegaHedging = new DataTable();
        deltaVegaHedging.Columns.Add("HedgeDate", typeof(int));
        if (n == 1)
        {
            deltaVegaHedging.Columns.Add("StockPrice", typeof(double));
        }
        else
        {
            deltaVegaHedging.Columns.Add("HedgedSpotAsset1", typeof(double));
            deltaVegaHedging.Columns.Add("HedgedSpotAsset2", typeof(double));
        }
        deltaVegaHedging.Columns.Add("Delta", typeof(double));
        deltaVegaHedging.Columns.Add("Vega", typeof(double));
        deltaVegaHedging.Columns.Add("SharesPurchased", typeof(double));
        deltaVegaHedging.Columns.Add("OptionsPurchased", typeof(double));
        deltaVegaHedging.Columns.Add("SharesHeld", typeof(double));
        deltaVegaHedging.Columns.Add("OptionsHeld", typeof(double));
        deltaVegaHedging.Columns.Add("CostSharesPurchased", typeof(double));
        deltaVegaHedging.Columns.Add("CostOptionsPurchased", typeof(double));
        deltaVegaHedging.Columns.Add("CumCosSharPurchasedInRate", typeof(double));
        deltaVegaHedging.Columns.Add("CumCosOptPurchasedInRate", typeof(double));
        deltaVegaHedging.Columns.Add("CumDiscTransCosShar", typeof(double));
        deltaVegaHedging.Columns.Add("CumDiscTransCosOpt", typeof(double));
        return deltaVegaHedging;
    }

    public List<double[,]> GetDummyParamMatrix(string stockName)
    {
        List<double[,]> listParamsMatrix = new List<double[,]>();
        double[,] matrixAirbus = new double[,]
                    {
                        { 1.69863014e-01, 1.00000000e-05, 9.86514202e-02, 4.00368697e-01, 1.06272875e-01, 9.20916804e-02 },
                        { 6.71232877e-01, 1.00000000e-05, 1.43590168e-01, 4.17177675e-02, 2.16197836e-01, 2.27495450e-01 },
                        { 1.16986301e+00, 1.00000000e-05, 1.95759666e-01, 9.27897430e-02, 2.74197828e-01, 3.57943692e-01 },
                        { 1.67123288e+00, 2.03832804e-02, 1.82558865e-01, -8.88629812e-02, 3.19403666e-01, 4.05698753e-01 }
                    };
        double[,] matrixAxa = new double[,]
                    {
                        { 1.69863014e-01,  1.00000000e-05,  2.67489425e-01,  5.26558100e-01, -2.22179026e-02,  8.16941060e-02 },
                        { 2.52054795e-01,  1.00000000e-05,  2.83029603e-01,  4.63175573e-01, -1.75439869e-02,  8.57687875e-02 },
                        { 4.21917808e-01,  9.03746839e-03,  2.85076628e-01,  4.57492593e-01,  3.67780156e-03,  7.67833781e-02 },
                        { 6.71232877e-01,  1.30935697e-02,  3.12151138e-01,  3.75402423e-01,  2.82998724e-02,  9.62638485e-02 },
                        { 9.17808219e-01,  3.39582913e-02,  2.80359360e-01,  5.20544865e-01,  7.13197468e-02,  7.61075594e-02 },
                        { 1.16986301e+00,  1.00000000e-05,  4.69086530e-01,  3.52432864e-01,  3.62236062e-02,  2.22933082e-01 }
                    };

        if (stockName == "AIRBUS")
        {
            listParamsMatrix.Add(matrixAirbus);
            return listParamsMatrix;
        }

        else if (stockName == "AXA")
        {
            listParamsMatrix.Add(matrixAxa);
            return listParamsMatrix;
        }
        else 
        {
            listParamsMatrix.Add(matrixAxa);
            listParamsMatrix.Add(matrixAirbus);
            return listParamsMatrix;
        }
    }
}




//List<double[]> pricesAllMatrices = new List<double[]>();
//List<List<double[,]>> extractedEvalPathsAllMatrices = new List<List<double[,]>>();
//List<double> initSpotsList = new List<double>();
//for (int j=0;j<this.n; j++)
//{
//    for (int i = 0; i < generatedMatricesList.Count; i++)
//    {
//        List<List<double[,]>> matList = generatedMatricesList[i]; // this returns 4 matrices (n=2) one stressed one neutral one neutral one stressed
//        for (int j = 0; j < this.n; j++)
//        {
//            for (int a = 0; a <matList.Count; a++)
//            {
//                initSpotsList.Add(matList[j][a][k, 0]);
//            }
//        }
//        List<double[,]> extractedEvalPaths = (List<double[,]>)ExtractEvalPaths(generatedMatricesList[i], index, obsDates, 0.0);
//        extractedEvalPathsAllMatrices.Add(extractedEvalPaths);
//    }
//    for (int i = 0; i < extractedEvalPathsAllMatrices.Count; i++)
//    {
//        List<double[,]> evalPaths = extractedEvalPathsAllMatrices[i];
//        double[] prices = (double[])ComputePrice(evalPaths, T, obsDates, totalNbrObs);

//        pricesAllMatrices.Add(prices);
//    }
//    double[] price = pricesAllMatrices[0];
//    double[] prSUpVUpList = pricesAllMatrices[1];
//    double[] prSDownVDownList = pricesAllMatrices[2];
//    double[] prSUpVDownList = pricesAllMatrices[3];
//    double[] prSDownVUpList = pricesAllMatrices[4];
//    double[] prSUpList = pricesAllMatrices[5];
//    double[] prSDownList = pricesAllMatrices[6];
//    double[] prVUpList = pricesAllMatrices[7];
//    double[] prVDownList = pricesAllMatrices[8];

//for (int i = 0; i < this.n; i++)
//{
//    gammaPath[i] = (prSUpList[i] + prSDownList[i] - 2 * price[i]) / Math.Pow((initSpotsList[i] * 0.05), 2);
//    if (this.volType == "SV")
//    {
//        volgaPath[i] = (prVUpList[i] + prVDownList[i] - 2 * price[i]) / (Math.Pow(Math.Pow(pg.InitVol, 2) * 0.0025, 2)) * 4 * Math.Pow(pg.InitVol, 2);
//        vegaPath[i] = (prVUpList[i] - prVDownList[i]) / (2 * (Math.Pow(pg.InitVol, 2) * 0.0025)) * 2 * pg.InitVol;
//        vannaPath[i] = (prSUpVUpList[i] - prSUpVDownList[i] - prSDownVUpList[i] + prSDownVDownList[i]) / (4 * (Math.Pow(pg.InitVol, 2) * 0.0025) * (initSpotsList[i] * 0.1)) * 2 * pg.InitVol;
//    }
//    if (this.volType != "SV")
//    {
//        volgaPath[i] = (prVUpList[i] + prVDownList[i] - 2 * price[i]) / (Math.Pow(0.05, 2));
//        vegaPath[i] = (prVUpList[i] - prVDownList[i]) / (2 * 0.05);
//        vannaPath[i] = (prSUpVUpList[i] - prSUpVDownList[i] - prSDownVUpList[i] + prSDownVDownList[i]) / (4 * 0.05 * (initSpotsList[i] * 0.05));
//    }
//    cashGammaperPathperDate[i] = gammaPath[i] * Math.Pow(initSpotsList[i], 2);
//    cashVannaperPathperDate[i] = vannaPath[i] * initSpotsList[i] * pg.InitVol;
//    cashVolgaperPathperDate[i] = volgaPath[i] * Math.Pow(pg.InitVol, 2);

//}