// Clean code for case n = 1. 
// change how you compute the delta or gamma, needs to be independant of the spot. 
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra;
using System.Globalization;
using System.Diagnostics;
using System.Linq;
using System.IO;
using ScottPlot.Colormaps;
using System.Text.Json;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Spreadsheet;
using Accord.Math;

namespace AutocallPricerFHC
{
    class TestFile
    {


        public class PricingResults
        {
            public double Price { get; set; }
            public double Delta { get; set; }
            public double Vega { get; set; }

            public double Gamma { get; set; }
            public double DeltaFHCClosed { get; set; }

            public double VegaFHCClosed { get; set; }

        }
        public static async Task Main(string[] args) {

            // Check the length of args
            if (args.Length < 8)
            {
                Console.WriteLine("Error: Not enough arguments provided.");
                return;
            }
            int n = int.Parse(args[0]);
            int N = int.Parse(args[1]);
            double T = double.Parse(args[2]);
            int freqObs = int.Parse(args[3]);
            double BP = double.Parse(args[4]);
            double AT = double.Parse(args[5]);
            double coupon = double.Parse(args[6]);
            int nbrHedges = int.Parse(args[7]);
            string hedgeType = args[8];
            double notional = double.Parse(args[10]);
            string stockname = args[11];
            string volType = args[9];
            //double notional = double.Parse(args[10], CultureInfo.InvariantCulture);
            //string hedgeType = args[8];
            //int n = int.Parse(args[0], CultureInfo.InvariantCulture);
            //int N = int.Parse(args[1], CultureInfo.InvariantCulture);
            //double T = double.Parse(args[2], CultureInfo.InvariantCulture);
            //int freqObs = int.Parse(args[3], CultureInfo.InvariantCulture);
            //double BP = double.Parse(args[4], CultureInfo.InvariantCulture);
            //double AT = double.Parse(args[5], CultureInfo.InvariantCulture);
            //double coupon = double.Parse(args[6], CultureInfo.InvariantCulture);
            //int nbrHedges = int.Parse(args[7], CultureInfo.InvariantCulture);
            //string stockname = "AIRBUS";

            Console.WriteLine($"Parsed Values: n={n}, N={N}, T={T}, freqObs={freqObs}, BP={BP}, AT={AT}, coupon={coupon}, nbrHedges={nbrHedges}, hedgeType={hedgeType}, volType = {volType}");

            string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string targetFolder = "Valo";
            int indexOfTargetFolder = currentDirectory.IndexOf(targetFolder, StringComparison.OrdinalIgnoreCase);
            string targetDirectory = currentDirectory.Substring(0, indexOfTargetFolder + targetFolder.Length);
            string pathRatesKeys = @"common\dataAutocall\data_and_keys";
            string dataPath = Path.Combine(targetDirectory, pathRatesKeys, "short term keys.xlsx");
            var ratesFetcher = new RatesFetcher();
            (List<double> maturities, List<double> rates) = await ratesFetcher.ProcessRatesKeys(dataPath);
            var nssModel = new NelsonSiegelSvenssonModel(maturities.ToArray(), rates.ToArray());
            List<string> pathsList = new List<string>();
            if (n == 1)
            {
                string pathData = @"common\dataAutocall\data_and_keys\" + stockname + "_25_04_2024.xlsx";
                pathsList.Add(Path.Combine(targetDirectory, pathData));
            }
            else
            {
                string pathData1 = @"common\dataAutocall\data_and_keys\AXA_25_04_2024.xlsx";
                string pathData2 = @"common\dataAutocall\data_and_keys\AIRBUS_25_04_2024.xlsx";
                pathsList.Add(Path.Combine(targetDirectory, pathData1));
                pathsList.Add(Path.Combine(targetDirectory, pathData2));
            }
            

            var result = Utility.RetrieveMarketData(pathsList, nssModel).GetAwaiter().GetResult();
            List<DataTable> dataList = result.DataList;
            List<double> initialSpotList = result.InitialSpotList;
            List<double> initialDividends = result.InitialDividends;
            List<string> tickers = result.Tickers;
            List<double[,]> listParamsMatrix = new List<double[,]>();
            var constants = new Constants();
            listParamsMatrix = constants.GetDummyParamMatrix(stockname);
            double[] hestonParams = new double[5];
            if (n == 1)
            {
                hestonParams = new double[] { 0.07184024202131313, 1.3182151985788433, 0.11146443297538572, 0.9629448114702667, -0.2668357302189678 };


                // Apply Heston only to Airbus stock, because lack of preprocessed data for AXA

                //string preprocessedDataPath = @"common\dataAutocall\data_and_keys\AIRBUS_preprocessed.xlsx"
                //DataTable marketData = Utility.LoadDataTableFromExcel(Path.Combine(targetDirectory, preprocessedDataPath));
                //HestonCalibrator calibrator = new HestonCalibrator(marketData);
                //double[] hestonParams = calibrator.Run();
            }


            bool heston = (volType == "SV");
            var qmcgenerator = new QMCGeneration();
            Matrix<double> randomMat = qmcgenerator.GenerateRandomMatrix(n, N, T, heston);
            var pathGenerator = new PathGenerator(n, N, pathsList, initialDividends, nssModel.GetRate, volType, listParamsMatrix, hestonParams, randomMat);
            var payoffCalculator = new PayoffCalculator(n, N, freqObs, BP, AT, coupon);
            List<int> obsDates = PayoffCalculator.ComputeObsDates(T, freqObs);
            int totalNbrObs = obsDates.Count;
            int k = 0;
            int smoothingFactor = 1000;
            double price = 0;
            Dictionary<double, double> probabilities = new Dictionary<double, double>();
            if (n == 1)
            {
                double[,] SMatrix = (double[,])pathGenerator.GeneratePaths(T, initialSpotList, null, null, -1);
                double[,] evalMatrix = (double[,])payoffCalculator.ComputeEvaluationMatrix(SMatrix, initialSpotList, obsDates);
                double[,] payoffMatrix = (double[,])payoffCalculator.ComputePayoff(evalMatrix, T, obsDates, totalNbrObs, k, smoothingFactor);
                probabilities = payoffCalculator.ComputeRedemptionProbability(payoffMatrix);
                price = PayoffCalculator.ComputePrice(n, notional, payoffMatrix, nssModel.GetRate, obsDates);
                Console.WriteLine($"price {price.ToString(CultureInfo.InvariantCulture)}");
                Console.WriteLine("starting computing delta gamma vega and price");
            }
            else
            {
                List<double[,]> SMatrix = (List<double[,]>)pathGenerator.GeneratePaths(T, initialSpotList, null, null, -1);
                List<double[,]> evalMatrix = (List<double[,]>)payoffCalculator.ComputeEvaluationMatrix(SMatrix, initialSpotList, obsDates);
                List<double[,]> payoffMatrix = (List<double[,]>)payoffCalculator.ComputePayoff(evalMatrix, T, obsDates, totalNbrObs, k, smoothingFactor);
                //Dictionary<double, double> probabilities = payoffCalculator.ComputeRedemptionProbability(payoffMatrix);
                price = PayoffCalculator.ComputePrice(n, notional, payoffMatrix, nssModel.GetRate, obsDates);
                Console.WriteLine($"price {price.ToString(CultureInfo.InvariantCulture)}");
                Console.WriteLine("starting computing delta gamma vega and price");
            }

            var greekscomputation = new GreeksComputations(n, N, pathsList, nssModel.GetRate, initialDividends, listParamsMatrix, hestonParams, volType,
                          freqObs, BP, AT, coupon, notional);
            double delta = 0;
            double vega = 0;
            double gamma = 0;
            if (n == 1)
            {
                if (hedgeType == "Delta-Vega")
                {
                    var deltaVega = (ValueTuple<double, double, double>)greekscomputation.ComputeGreeks(pathGenerator, payoffCalculator, initialSpotList, T, obsDates, totalNbrObs, 0, hedgeType, 0);
                    delta = deltaVega.Item1;
                    vega = deltaVega.Item2;
                    gamma = deltaVega.Item3;
                }
                if (hedgeType == "Delta")
                {
                    var deltaGamma = (ValueTuple<double, double>)greekscomputation.ComputeGreeks(pathGenerator, payoffCalculator, initialSpotList, T, obsDates, totalNbrObs, 0, hedgeType, 0);
                    delta = deltaGamma.Item1;
                    gamma = deltaGamma.Item2;
                }
                Console.WriteLine($"delta {delta.ToString(CultureInfo.InvariantCulture)}");
                Console.WriteLine($"gamma {gamma.ToString(CultureInfo.InvariantCulture)}");
                Console.WriteLine($"vega {vega.ToString(CultureInfo.InvariantCulture)}");
                Console.WriteLine("finished computing delta gamma vega and price");
            }
            else
            {
                if (hedgeType == "Delta-Vega")
                {
                    var deltaVega = (ValueTuple<double[], double[], double[]>)greekscomputation.ComputeGreeks(pathGenerator, payoffCalculator, initialSpotList, T, obsDates, totalNbrObs, 0, hedgeType, 0);
                    delta = deltaVega.Item1.Sum();
                    vega = deltaVega.Item2.Sum();
                    gamma = deltaVega.Item3.Sum();
                    for (int i = 0; i < n; i++)
                    {
                        Console.WriteLine($"delta for asset {i + 1} , {deltaVega.Item1[i]}");
                        Console.WriteLine($"vega for asset {i + 1} , {deltaVega.Item2[i]}");
                        Console.WriteLine($"gamma for asset {i + 1} , {deltaVega.Item3[i]}");

                    }
                }
                if (hedgeType == "Delta")
                {
                    var deltaGamma = (ValueTuple<double[], double[]>)greekscomputation.ComputeGreeks(pathGenerator, payoffCalculator, initialSpotList, T, obsDates, totalNbrObs, 0, hedgeType, 0);
                    delta = deltaGamma.Item1.Sum();
                    gamma = deltaGamma.Item2.Sum();
                }


            }

            var results = new PricingResults
            {
                Price = price,
                Delta = delta,
                Vega = vega,
                Gamma = gamma
            };
            

            //----------------------------------------------------------------------------
            List<string> historicalDataPaths = new List<string>();
            if (n == 1)
            {
                historicalDataPaths.Add(Path.Combine(targetDirectory, pathRatesKeys, "Historical" + stockname + ".xlsx"));
            }
            else
            {
                historicalDataPaths.Add(Path.Combine(targetDirectory, pathRatesKeys, "HistoricalAXA.xlsx"));
                historicalDataPaths.Add(Path.Combine(targetDirectory, pathRatesKeys, "HistoricalAIRBUS.xlsx"));
                
            }
            int nbrValues = (int)Math.Ceiling(252 * T);
            List<double[]> closingpricesList = new List<double[]>();
            for (int i = 0; i < n; i++)
            {
                var historicalData = Utility.LoadDataTableFromExcel(historicalDataPaths[i]);
                double[] closeValues = new double[nbrValues];
                closeValues = OptionDataProcessor.GetHistPricesFromDataTable(historicalData, "Close", nbrValues);
                closingpricesList.Add(closeValues);
            }

            var fhchelpers = new FHCHelpers(n, N, notional, freqObs, BP, AT, coupon, nssModel.GetRate, nbrHedges, closingpricesList);
            var fhcFixedHedging = new FHC_FixedHedging(n, N, closingpricesList, notional, freqObs, BP, AT, coupon, nssModel.GetRate, nbrHedges);

            //DataTable FixedDeltaHedging = fhchelpers.FillDeltaHedgeTable(greekscomputation, pathGenerator, payoffCalculator, T, "Fixed");

            Console.WriteLine("Testing Fixed FHC for Delta and Vega");
            DataTable FixedDeltaVegaHedging = fhchelpers.FillDeltaVegaHedgeTable(greekscomputation, pathGenerator, payoffCalculator, T, "Fixed");

            double sumFixedTransCosts = Utility.ComputeFHCSum(hedgeType, null, FixedDeltaVegaHedging);


            //Console.WriteLine("Testing Move Based FHC for Delta");
            var fhcMoveBasedHedging = new FHCMoveBasedHedging(n, N, closingpricesList, notional, freqObs, BP, AT, coupon, nssModel.GetRate);
            //DataTable MoveBasedDeltaHedging = fhchelpers.FillDeltaHedgeTable(greekscomputation, pathGenerator, payoffCalculator, T, "LevelBased");

            Console.WriteLine("Testing Move Based FHC for Delta and Vega");
            DataTable MoveBasedDeltaVegaHedging = fhchelpers.FillDeltaVegaHedgeTable(greekscomputation, pathGenerator, payoffCalculator, T, "LevelBased");


            var dataTables = new Dictionary<string, DataTable>
                    {
                        //{ "Fixed delta hedging", FixedDeltaHedging},
                        { "FixedDeltaVegaHedging",FixedDeltaVegaHedging },
                        //{ "Level based delta hedging",MoveBasedDeltaHedging},
                        { "LevelBasedDeltaVegaHedging",MoveBasedDeltaVegaHedging}
                    };
            Console.WriteLine("finished Testing Move Based FHC for Delta and Vega");
            Utility.ExportToOpenExcel(dataTables, Path.Combine(currentDirectory, "AutocallPricerFHC.xlsm"));
            Console.WriteLine("Finished exporting datatables");
            double sumLevelBasedTransCosts = Utility.ComputeFHCSum(hedgeType, null, MoveBasedDeltaVegaHedging);



            //Console.WriteLine("#####################################################################################");
            Console.WriteLine("starting 3rd approach");
            double rho = hestonParams[4];
            double sigmaS = Math.Sqrt(0.07184024202131313);
            double sigmaV = Math.Sqrt(0.07184024202131313);
            double ks = 0.01;
            double kv = 0.01;
            double lbda = 0.52;
            FHCClosedFormulae closedFormulae = new FHCClosedFormulae(n, N, freqObs, volType, rho, sigmaS, sigmaV, ks, kv, lbda, nssModel.GetRate, closingpricesList, BP, AT, coupon, notional, pathGenerator, payoffCalculator);
            var resultsClosedFHC = closedFormulae.ComputeFHCDeltaVega(T, closingpricesList, ks);
            Console.WriteLine("finishing 3rd approach");
            Console.WriteLine("starting writing in the txt file");

            string textFilePathResults = Path.Combine(currentDirectory, "results.txt");
            string textOutput = $"Price: {price}\nDelta: {delta}\nVega: {vega}\nGamma: {gamma}\nDeltaFHCClosed: {resultsClosedFHC.Item1}\nVegaFHCClosed: {resultsClosedFHC.Item2}";
            
            if (n == 1)
            {
                foreach (var pair in probabilities)
                {
                    textOutput += $"\nObservation date {pair.Key + 1.0}: {pair.Value}";
                }
            }
            textOutput += $"\nNbr hedges in Fixed: {FixedDeltaVegaHedging.Rows.Count}";
            textOutput += $"\nNbr hedges in Level Based: {MoveBasedDeltaVegaHedging.Rows.Count}";

            textOutput += $"\nNbr hedges in closed form: {closedFormulae.nbreffectiveHegde}";
            textOutput += $"\nNbr optimal hedges in closed form: {closedFormulae.optimalNs}";
            textOutput += $"\nFixed transaction costs: {sumFixedTransCosts}";
            textOutput += $"\nLevel Based transaction costs: {sumLevelBasedTransCosts}";
            textOutput += $"\nLast Value of the traded security in Fixed: {fhcFixedHedging.lastBinValue}";
            textOutput += $"\nLast Value of the traded security in Move Based: {fhcMoveBasedHedging.lastBinValue}";
            textOutput += $"\nLast Payoff: {fhcMoveBasedHedging.lastFinalPayoff}";
            File.WriteAllText(textFilePathResults, textOutput);
            ////"-------------------------------------------------------------------------------------------------------------------"

            Environment.Exit(0);





            // Output the final price

            // Testing the greeks computations
            //    Console.WriteLine("#####################################################################################");
            //var greekscomputation = new GreeksComputations(n, N, pathsList, nssModel.GetRate, initialDividends, listParamsMatrix, hestonParams, volType,
            //                freqObs, BP, AT, coupon, notional);

            ////string hedgeType = "Delta-Vega";
            //if (hedgeType == "Delta-Vega")
            //{
            //    var deltaVega = (ValueTuple<double, double>)greekscomputation.ComputeGreeks(pathGenerator, payoffCalculator, initialSpotList, T, obsDates, totalNbrObs, 0, hedgeType, 0);
            //    double delta = deltaVega.Item1;
            //    double vega = deltaVega.Item2;
            //    Console.WriteLine(price.ToString(CultureInfo.InvariantCulture));
            //    Console.WriteLine(delta.ToString(CultureInfo.InvariantCulture));
            //    Console.WriteLine(vega.ToString(CultureInfo.InvariantCulture));
            //    //Console.WriteLine("Delta and Vega computations");
            //    //Console.WriteLine($"delta: {delta.ToString(CultureInfo.InvariantCulture)}");
            //    //Console.WriteLine($"vega: {vega.ToString(CultureInfo.InvariantCulture)}");
            //}

            //hedgeType = "Delta";
            //if (hedgeType == "Delta")
            //{
            //    double delta = (double)greekscomputation.ComputeGreeks(pathGenerator, payoffCalculator, initialSpotList, T, obsDates, totalNbrObs, 0, hedgeType, 0);
            //    double vega = 0.0;
            //    //Console.WriteLine("Delta computations");
            //    //Console.WriteLine($"delta: {delta.ToString(CultureInfo.InvariantCulture)}");
            //    Console.WriteLine(price.ToString(CultureInfo.InvariantCulture));
            //    Console.WriteLine(delta.ToString(CultureInfo.InvariantCulture));
            //    Console.WriteLine(vega.ToString(CultureInfo.InvariantCulture));
            //}


            //    //Testing rates fetching, processing and Nelson Siegel Svensson calibration
            //    string pathRatesKeys = @"C:\Users\m.ben-el-ghoul\OneDrive - AXIS ALTERNATIVES\Documents\Valo\python\Autocall pricer and FHC computations\Autocall pricer V3\data_and_keys\short term keys.xlsx";
            //    var ratesFetcher = new RatesFetcher();
            //    (List<double> maturities, List<double> rates) = await ratesFetcher.ProcessRatesKeys(pathRatesKeys);
            //    var nssModel = new NelsonSiegelSvenssonModel(maturities.ToArray(), rates.ToArray());
            //    Console.WriteLine($"the rate for 10 years is , {nssModel.GetRate(0.021)}");


            //    //////"---------------------------------------------------------------------------------------------------------"

            //    int n = 1;
            //    int N = 5000;
            //    double T = 1;
            //    int freqObs = 4;
            //    double BP = 0.95;
            //    double AT = 1.01;
            //    double coupon = 0.1;
            //    int nbrHedges = 21;

            //    if (n > 1)
            //    {
            //        // Testing data Preprocessing , dividends fetching and initial spots extraction for n >1
            //        var pathsList = new List<string>{@"C:\Users\m.ben-el-ghoul\PycharmProjects\Autocall pricer V2\data and keys\AXA_25_04_2024.xlsx",
            //                                        @"C:\Users\m.ben-el-ghoul\PycharmProjects\Autocall pricer V2\data and keys\AIRBUS_25_04_2024.xlsx"};
            //        //DividendFetcher divFetcher = new DividendFetcher();
            //        Constants constants = new Constants();

            //        var result = Utility.RetrieveMarketData(pathsList, nssModel).GetAwaiter().GetResult();
            //        List<DataTable> dataList = result.DataList;
            //        List<double> initialSpotList = result.InitialSpotList;
            //        List<double> initialDividends = result.InitialDividends;
            //        List<string> tickers = result.Tickers;


            //        //for (int j = 0; j < n; j++)
            //        //{
            //        //    Console.WriteLine($"Ticker {j}, {tickers[j]}"); 
            //        //    Console.WriteLine($"Spot {j} ,{initialSpotList[j]} ");
            //        //    Console.WriteLine($"Dividend {j} ,{initialDividends[j]} ");
            //        //}

            //        //    //"-------------------------------------------------------------------------------------------------------------------"

            //        // Testing SVI calibration and generation of the parameters matrices. 
            //        List<double[,]> listParamsMatrix = new List<double[,]>();
            //        //listParamsMatrix = constants.GetDummyParamMatrix(n);
            //        var sviModel = new SVIModel(n, dataList);
            //        listParamsMatrix = sviModel.ParamsSkew();
            //        Utility.PrintMatrices(listParamsMatrix);

            //        //    //"-------------------------------------------------------------------------------------------------------------------"

            //        //    // Testing the spot matrices diffusion

            //        string volType = "LV";
            //        bool heston = new bool();
            //        if (volType == "SV")
            //        {
            //            heston = true;
            //        }
            //        if (volType != "SV")
            //        {
            //            heston = false;
            //        }
            //        var qmcgenerator = new QMCGeneration();
            //        Matrix<double> randomMat = qmcgenerator.GenerateRandomMatrix(n, N, T, heston);
            //        var pathGenerator = new PathGenerator(n, N, pathsList, initialDividends, nssModel.GetRate, volType, listParamsMatrix, null, randomMat);
            //        List<double[,]> SMatrixList = (List<double[,]>)pathGenerator.GeneratePaths(T, initialSpotList, null, null,-1);
            //        //Console.WriteLine("SMatrices");
            //        //Utility.PrintMatrices(SMatrixList);


            //        //    //"-------------------------------------------------------------------------------------------------------------------"

            //        //Testing the Payoff and price computations 
            //        var payoffCalculator = new PayoffCalculator(n, N, freqObs, BP, AT, coupon);
            //        List<int> obsDates = PayoffCalculator.ComputeObsDates(T, freqObs);
            //        //Console.WriteLine($"Observation dates,{string.Join(", ", obsDates)}");


            //        List<double[,]> evalMatrix = (List<double[,]>)payoffCalculator.ComputeEvaluationMatrix(SMatrixList, initialSpotList, obsDates);
            //        //Console.WriteLine("Evaluation matrices");
            //        //Utility.PrintMatrices(evalMatrix);


            //        int totalNbrObs = obsDates.Count;
            //        int k = 0;
            //        int smoothingFactor = 1000;
            //        List<double[,]> payoffMatrix = (List<double[,]>)payoffCalculator.ComputePayoff(evalMatrix, T, obsDates, totalNbrObs, k, smoothingFactor);
            //        //Console.WriteLine("Payoff Matrix");
            //        //Utility.PrintMatrices(payoffMatrix);
            //        double notional = 100;
            //        double price = PayoffCalculator.ComputePrice(n, notional, payoffMatrix, nssModel.GetRate, obsDates);
            //        Console.WriteLine($"Price {price}");




            //        ////    //"----------------------------------------------------------------------------------------------------------------------------------------"

            //        //// Testing greeks computations 
            //        var greekscomputation = new GreeksComputations(n, N, pathsList, nssModel.GetRate, initialDividends, listParamsMatrix, null, volType,
            //                        freqObs, BP, AT, coupon, notional);
            //        string hedgeType = "Delta-Vega";
            //        if (hedgeType == "Delta-Vega")
            //        {
            //            var deltaVega = (ValueTuple<double[], double[], double[]>)greekscomputation.ComputeGreeks(pathGenerator, payoffCalculator, initialSpotList, T, obsDates, totalNbrObs, 0, hedgeType, 0);
            //            double[] deltas = deltaVega.Item1;
            //            double[] vegas = deltaVega.Item2;
            //            double[] gammas = deltaVega.Item3;


            //            Console.WriteLine("Delta and Vega per asset");
            //            for (int i = 0; i < deltas.Length; i++)
            //            {
            //                Console.WriteLine($"delta for asset {i + 1} , {deltas[i]}");
            //                Console.WriteLine($"vega for asset {i + 1} , {vegas[i]}");
            //                Console.WriteLine($"gamma for asset {i + 1} , {gammas[i]}");

            //            }
            //            Console.WriteLine("Delta and Vega computations for LV");
            //            Console.WriteLine($"delta: {deltas.Sum()}");
            //            Console.WriteLine($"gamma: {gammas.Sum()}");
            //            Console.WriteLine($"vega: {vegas.Sum()}");
            //        }
            //        //hedgeType = "Delta";
            //        //if (hedgeType == "Delta")
            //        //{
            //        //    double[] delta = (double[])greekscomputation.ComputeGreeks(pathGenerator, payoffCalculator, initialSpotList, T, obsDates, totalNbrObs, 0, hedgeType, 0);
            //        //    for (int i = 0; i < delta.Length; i++)
            //        //    {
            //        //        Console.WriteLine($"delta for asset {i + 1},{delta[i]}");
            //        //    }
            //        //    Console.WriteLine("Delta computations for LV");
            //        //    Console.WriteLine($"delta: {delta.Sum()}");
            //        //}




            //        ////    //"----------------------------------------------------------------------------------------------------------------------------------------"

            //        //// Testing Future hedging costs computations


            //        List<string> historicalDataPaths = new List<string>();
            //        historicalDataPaths.Add(@"C:\Users\m.ben-el-ghoul\OneDrive - AXIS ALTERNATIVES\Documents\Valo\python\Autocall pricer and FHC computations\Autocall pricer V3\data_and_keys\HistoricalAxa.xlsx");
            //        historicalDataPaths.Add(@"C:\Users\m.ben-el-ghoul\OneDrive - AXIS ALTERNATIVES\Documents\Valo\python\Autocall pricer and FHC computations\Autocall pricer V3\data_and_keys\HistoricalAIRBUS.xlsx");
            //        int nbrValues = (int)Math.Ceiling(252 * T);
            //        List<double[]> closingpricesList = new List<double[]>();
            //        for (int i = 0; i < n; i++)
            //        {
            //            var historicalData = Utility.LoadDataTableFromExcel(historicalDataPaths[i]);
            //            double[] closeValues = new double[nbrValues];
            //            closeValues = OptionDataProcessor.GetHistPricesFromDataTable(historicalData, "Close", nbrValues);
            //            closingpricesList.Add(closeValues);
            //        }
            //        var fhchelpers = new FHCHelpers(n, N, notional, freqObs, BP, AT, coupon, nssModel.GetRate, nbrHedges, closingpricesList);
            //        //var fhcFixedHedging = new FHC_FixedHedging(n, N, closingpricesList, notional, freqObs, BP, AT, coupon, nssModel.GetRate, nbrHedges);
            //        //var fhcMoveBasedHedging = new FHCMoveBasedHedging(n, N, closingpricesList, notional, freqObs, BP, AT, coupon, nssModel.GetRate);

            //        //Console.WriteLine("Testing Fixed FHC for Delta");
            //        //DataTable FixedDeltaHedging = fhchelpers.FillDeltaHedgeTable(greekscomputation, pathGenerator, payoffCalculator, T, "Fixed");


            //        //Console.WriteLine("Testing Fixed FHC for Delta and Vega");
            //        //DataTable FixedDeltaVegaHedging = fhchelpers.FillDeltaVegaHedgeTable(greekscomputation, pathGenerator, payoffCalculator, T, "Fixed");


            //        //Console.WriteLine("Testing Move Based FHC for Delta");
            //        //DataTable MoveBasedDeltaHedging = fhchelpers.FillDeltaHedgeTable(greekscomputation, pathGenerator, payoffCalculator, T, "LevelBased");

            //        //Console.WriteLine("Testing Move Based FHC for Delta and Vega");
            //        //DataTable MoveBasedDeltaVegaHedging = fhchelpers.FillDeltaVegaHedgeTable(greekscomputation, pathGenerator, payoffCalculator, T, "LevelBased");


            //        //var dataTables = new Dictionary<string, DataTable>
            //        //        {
            //        //            { "Fixed delta hedging", FixedDeltaHedging},
            //        //            { "Fixed delta vega hedging",FixedDeltaVegaHedging },
            //        //            { "Move based delta hedging",MoveBasedDeltaHedging},
            //        //            { "Move based delta vega hedging",MoveBasedDeltaVegaHedging}
            //        //        };

            //        //Utility.ExporttoExcel(dataTables, @"C:\Users\m.ben-el-ghoul\source\repos\NewAutocallPricerFHC\HedgingFile.xlsx");

            //        ////"----------------------------------------------------------------------------------------------------------------------------------------"

            //        ////Testing Future hedging costs closed formulae

            //        //Console.WriteLine("#####################################################################################");
            //        //double rho = 0.25;
            //        //double sigmaS = Math.Sqrt(0.07184024202131313);
            //        //double sigmaV = Math.Sqrt(0.07184024202131313);
            //        //double ks = 0.01;
            //        //double kv = 0.01;
            //        //double lbda = 0.52;
            //        //FHCClosedFormulae closedFormulae = new FHCClosedFormulae(n, N, freqObs, volType, rho, sigmaS, sigmaV, ks, kv, lbda, nssModel.GetRate, closingpricesList, BP, AT, coupon, notional, pathGenerator, payoffCalculator);
            //        //var results = closedFormulae.ComputeFHCDeltaVega(T, closingpricesList, ks);
            //    }
            //    else
            //    {
            //        //Testing data Preprocessing , dividends fetching and initial spots extraction for n = 1


            //        Console.WriteLine("Testing the case n = 1");
            //        string stockName = "AXA";
            //        var pathsList = new List<string> { @"C:\Users\m.ben-el-ghoul\PycharmProjects\Autocall pricer V2\data and keys\"+stockName+"_25_04_2024.xlsx" };
            //        //DividendFetcher divFetcher = new DividendFetcher();
            //        var result = Utility.RetrieveMarketData(pathsList, nssModel).GetAwaiter().GetResult();
            //        List<DataTable> dataList = result.DataList;
            //        List<double> initialSpotList = result.InitialSpotList;
            //        //List<double> initialDividends = result.InitialDividends;
            //        double initDiv = 1;
            //        List<double> initialDividends = new List<double> { initDiv };
            //        List<string> tickers = result.Tickers;

            //        //"-------------------------------------------------------------------------------------------------------------------"

            //        // Testing SVI calibration and generation of the parameters matrices. 
            //        var constants = new Constants();
            //        List<double[,]> listParamsMatrix = constants.GetDummyParamMatrix(stockName);
            //        //var sviModel = new SVIModel(n, dataList);
            //        //List<double[,]> listParamsMatrix = sviModel.ParamsSkew();
            //        Utility.PrintMatrices(listParamsMatrix);

            //        //"-------------------------------------------------------------------------------------------------------------------"

            //        // Testing Heston Calibration 

            //        //DataTable marketData = Utility.LoadDataTableFromExcel(@"C:\Users\m.ben-el-ghoul\OneDrive - AXIS ALTERNATIVES\Documents\Autocall pricer V4\AIRBUS_preprocessed.xlsx");
            //        //HestonCalibrator calibrator = new HestonCalibrator(marketData);
            //        //double[] hestonParams = calibrator.Run();
            //        //Console.WriteLine(string.Join(", ", hestonParams));
            //        double[] hestonParams = new double[] { 0.07184024202131313, 1.3182151985788433, 0.11146443297538572, 0.9629448114702667, -0.2668357302189678 };

            //        //"-------------------------------------------------------------------------------------------------------------------"

            //        //Testing the diffusion
            //        string volType = "LV";
            //        bool heston = new bool();
            //        if (volType == "SV")
            //        {
            //            heston = true;
            //        }
            //        if (volType != "SV")
            //        {
            //            heston = false;
            //        }
            //        var qmcgenerator = new QMCGeneration();
            //        Matrix<double> randomMat = qmcgenerator.GenerateRandomMatrix(n, N, T, heston);
            //        var pathGenerator = new PathGenerator(n, N, pathsList, initialDividends, nssModel.GetRate, volType, listParamsMatrix, hestonParams, randomMat);
            //        Stopwatch stopwatch = new Stopwatch();
            //        stopwatch.Start();

            //        double[,] SMatrix = (double[,])pathGenerator.GeneratePaths(T, initialSpotList, null, null, -1);
            //        //Console.WriteLine("SMatrix");
            //        //Utility.PrintMatrix(SMatrix);
            //        //stopwatch.Stop();

            //        //    //"-------------------------------------------------------------------------------------------------------------------"

            //        //    // Testing the Payoff and price computations

            //        var payoffCalculator = new PayoffCalculator(n, N, freqObs, BP, AT, coupon);
            //        List<int> obsDates = PayoffCalculator.ComputeObsDates(T, freqObs);
            //        //Console.WriteLine("Observation dates");
            //        //Utility.PrintListInts(obsDates);

            //        double[,] evalMatrix = (double[,])payoffCalculator.ComputeEvaluationMatrix(SMatrix, initialSpotList, obsDates);
            //        //Console.WriteLine("Evaluation matrices");
            //        //Utility.PrintMatrix(evalMatrix);


            //        int totalNbrObs = obsDates.Count;
            //        int k = 0;
            //        int smoothingFactor = 1000;
            //        double[,] payoffMatrix = (double[,])payoffCalculator.ComputePayoff(evalMatrix, T, obsDates, totalNbrObs, k, smoothingFactor);
            //        //Console.WriteLine("Payoff Matrix");
            //        //Utility.PrintMatrix(payoffMatrix);


            //        double notional = 100;
            //        double price = PayoffCalculator.ComputePrice(n, notional, payoffMatrix, nssModel.GetRate, obsDates);
            //        Console.WriteLine("Price");
            //        Console.WriteLine(price);

            //        //"-------------------------------------------------------------------------------------------------------------------"

            //        // Testing the greeks computations
            //        Console.WriteLine("#####################################################################################");
            //        var greekscomputation = new GreeksComputations(n, N, pathsList, nssModel.GetRate, initialDividends, listParamsMatrix, hestonParams, volType,
            //                        freqObs, BP, AT, coupon, notional);
            //        string hedgeType = "Delta-Vega";
            //        if (hedgeType == "Delta-Vega")
            //        {
            //            var deltaVega = (ValueTuple<double,double, double>)greekscomputation.ComputeGreeks(pathGenerator, payoffCalculator, initialSpotList, T, obsDates, totalNbrObs, 0, hedgeType, 0);
            //            double delta = deltaVega.Item1;
            //            double vega = deltaVega.Item2;
            //            Console.WriteLine("Delta and Vega computations");
            //            Console.WriteLine($"delta: {delta}");
            //            Console.WriteLine($"vega: {vega}");
            //        }

            //        //hedgeType = "Delta";
            //        //if (hedgeType == "Delta")
            //        //{
            //        //    double delta = (double)greekscomputation.ComputeGreeks(pathGenerator, payoffCalculator, initialSpotList, T, obsDates, totalNbrObs, 0, hedgeType, 0);
            //        //    Console.WriteLine("Delta computations");
            //        //    Console.WriteLine($"delta: {delta}");
            //        //}

            //        ////    //"--------------------------------------------------------------------------------------------------------




            //        List<string> historicalDataPaths = new List<string>();
            //        historicalDataPaths.Add(@"C:\Users\m.ben-el-ghoul\OneDrive - AXIS ALTERNATIVES\Documents\Valo\python\Autocall pricer and FHC computations\Autocall pricer V3\data_and_keys\HistoricalAxa.xlsx");
            //        //historicalDataPaths.Add(@"C:\Users\m.ben-el-ghoul\OneDrive - AXIS ALTERNATIVES\Documents\Valo\python\Autocall pricer and FHC computations\Autocall pricer V3\data_and_keys\HistoricalAIRBUS.xlsx");
            //        int nbrValues = (int)Math.Ceiling(252 * T);
            //        List<double[]> closingpricesList = new List<double[]>();
            //        for (int i = 0; i < n; i++)
            //        {
            //            var historicalData = Utility.LoadDataTableFromExcel(historicalDataPaths[i]);
            //            double[] closeValues = new double[nbrValues];
            //            closeValues = OptionDataProcessor.GetHistPricesFromDataTable(historicalData, "Close", nbrValues);
            //            closingpricesList.Add(closeValues);
            //        }
            //        var fhchelpers = new FHCHelpers(n, N, notional, freqObs, BP, AT, coupon, nssModel.GetRate, nbrHedges, closingpricesList);
            //        var fhcFixedHedging = new FHC_FixedHedging(n, N, closingpricesList, notional, freqObs, BP, AT, coupon, nssModel.GetRate, nbrHedges);
            //        //var fhcMoveBasedHedging = new FHCMoveBasedHedging(n, N, closingpricesList, notional, freqObs, BP, AT, coupon, nssModel.GetRate);

            //        //Console.WriteLine("Testing Fixed FHC for Delta");
            //        //DataTable FixedDeltaHedging = fhchelpers.FillDeltaHedgeTable(greekscomputation, pathGenerator, payoffCalculator, T, "Fixed");


            //        //Console.WriteLine("Testing Fixed FHC for Delta and Vega");
            //        //DataTable FixedDeltaVegaHedging = fhchelpers.FillDeltaVegaHedgeTable(greekscomputation, pathGenerator, payoffCalculator, T, "Fixed");


            //        //Console.WriteLine("Testing Move Based FHC for Delta");
            //        //DataTable MoveBasedDeltaHedging = fhchelpers.FillDeltaHedgeTable(greekscomputation, pathGenerator, payoffCalculator, T, "LevelBased");

            //        //Console.WriteLine("Testing Move Based FHC for Delta and Vega");
            //        //DataTable MoveBasedDeltaVegaHedging = fhchelpers.FillDeltaVegaHedgeTable(greekscomputation, pathGenerator, payoffCalculator, T, "LevelBased");


            //        //var dataTables = new Dictionary<string, DataTable>
            //        //        {
            //        //            { "Fixed delta hedging", FixedDeltaHedging},
            //        //            { "Fixed delta vega hedging",FixedDeltaVegaHedging },
            //        //            { "Move based delta hedging",MoveBasedDeltaHedging},
            //        //            { "Move based delta vega hedging",MoveBasedDeltaVegaHedging}
            //        //        };

            //        //Utility.ExporttoExcel(dataTables, @"C:\Users\m.ben-el-ghoul\source\repos\NewAutocallPricerFHC\HedgingFile.xlsx");

            //        //Console.WriteLine("#####################################################################################");

            //        double rho = hestonParams[4];
            //        double sigmaS = Math.Sqrt(0.07184024202131313);
            //        double sigmaV = Math.Sqrt(0.07184024202131313);
            //        double ks = 0.01;
            //        double kv = 0.01;
            //        double lbda = 0.52;
            //        FHCClosedFormulae closedFormulae = new FHCClosedFormulae(n, N, freqObs, volType, rho, sigmaS, sigmaV, ks, kv, lbda, nssModel.GetRate, closingpricesList, BP, AT, coupon, notional, pathGenerator, payoffCalculator);
            //        var results = closedFormulae.ComputeFHCDeltaVega(T, closingpricesList, ks);
            //        //double fhcClosedVega = closedFormulae.ComputeFHCVega(T, closingpricesList, kv);

            //        //"-------------------------------------------------------------------------------------------------------------------"



            //    }

            //    Console.ReadLine();
            }
        }
    }


