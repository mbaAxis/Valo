using Accord.Math;
using System;
using System.Collections.Generic;
using System.Linq;


// Price tables

//for (int i = 0; i < testAT.Length; i++)
//{
//    for (int j = 0; j < testBP.Length; j++)
//    {

//        double at = testAT[i];
//        double bp = testBP[j];
//        var pc = new PayoffCalculator(n, N, freqObs, bp, at, coupon);
//        List<int> obdates = PayoffCalculator.ComputeObsDates(T, freqObs);
//        double[,] evalMat = (double[,])pc.ComputeEvaluationMatrix(SMatrix, initialSpotList, obdates);
//        int toNbrObs = obdates.Count;
//        int alp = 0;
//        int smF = 1000;
//        double[,] payoffMat = (double[,])pc.ComputePayoff(evalMat, T, obdates, toNbrObs, alp, smF);
//        Dictionary<double, double> probab = pc.ComputeRedemptionProbability(payoffMat);
//        double prc = PayoffCalculator.ComputePrice(n, notional, payoffMat, nssModel.GetRate, obdates);
//        priceDict.Add((at, bp), prc);
//    }

//}
//foreach (var entry in priceDict)
//{
//    // entry.Key.Item1 is 'at', entry.Key.Item2 is 'bp', entry.Value is the 'price'
//    Console.WriteLine($"AT: {entry.Key.Item1}, BP: {entry.Key.Item2}, Price: {entry.Value}");
//}



//    // Plotting An autocall Greeks

//    List<List<double>> spotList = new List<List<double>>();
//    List<double> gammas = new List<double>();
//    List<double> deltas = new List<double>();
//    List<double> vegas = new List<double>();
//    for (int i = -5; i < 6; i++)
//    {
//        List<double> spots = new List<double>();
//        spots.Add(100 + i * 10);
//        spotList.Add(spots);
//    }
//    foreach (var element in spotList)
//    {

//        Matrix<double> randMat = qmcgenerator.GenerateRandomMatrix(n, N, T, heston);
//        var pathGen = new PathGenerator(n, N, pathsList, initialDividends, nssModel.GetRate, volType, listParamsMatrix, hestonParams, randMat);
//        double[,] smat = (double[,])pathGen.GeneratePaths(T, element,null,null);
//        double[,] smatUp = (double[,])pathGen.GeneratePaths(T, element, "Delta", "Up");
//        double[,] smatDown = (double[,])pathGen.GeneratePaths(T, element, "Delta", "Down");
//        double[,] smatVUp = (double[,])pathGen.GeneratePaths(T, element, "Vega", "Up");
//        double[,] smatVDown = (double[,])pathGen.GeneratePaths(T, element, "Vega", "Down");
//        double[,] evalMatrixUp = (double[,])payoffCalculator.ComputeEvaluationMatrix(smatUp,element, obsDates);
//        double[,] evalMatrixDown = (double[,])payoffCalculator.ComputeEvaluationMatrix(smatDown, element, obsDates);
//        double[,] evalMat = (double[,])payoffCalculator.ComputeEvaluationMatrix(smat, element, obsDates);
//        double[,] evalMatrixVUp = (double[,])payoffCalculator.ComputeEvaluationMatrix(smatVUp, element, obsDates);
//        double[,] evalMatrixVDown = (double[,])payoffCalculator.ComputeEvaluationMatrix(smatVDown, element, obsDates);
//        double[,] payoffMatUp = (double[,])payoffCalculator.ComputePayoff(evalMatrixUp, T, obsDates, totalNbrObs, k, smoothingFactor);
//        double[,] payoffMatDown = (double[,])payoffCalculator.ComputePayoff(evalMatrixDown, T, obsDates, totalNbrObs, k, smoothingFactor);
//        double[,] payoffMat = (double[,])payoffCalculator.ComputePayoff(evalMat, T, obsDates, totalNbrObs, k, smoothingFactor);
//        double[,] payoffMatVUp = (double[,])payoffCalculator.ComputePayoff(evalMatrixVUp, T, obsDates, totalNbrObs, k, smoothingFactor);
//        double[,] payoffMatVDown = (double[,])payoffCalculator.ComputePayoff(evalMatrixVDown, T, obsDates, totalNbrObs, k, smoothingFactor);
//        double priceUp = PayoffCalculator.ComputePrice(n, notional, payoffMatUp, nssModel.GetRate, obsDates);
//        double prc = PayoffCalculator.ComputePrice(n, notional, payoffMat, nssModel.GetRate, obsDates);
//        double priceDown = PayoffCalculator.ComputePrice(n, notional, payoffMatDown, nssModel.GetRate, obsDates);
//        double priceVUp = PayoffCalculator.ComputePrice(n, notional, payoffMatVUp, nssModel.GetRate, obsDates);
//        double priceVDown = PayoffCalculator.ComputePrice(n, notional, payoffMatVDown, nssModel.GetRate, obsDates);
//        double delt = (priceUp-priceDown)/(2*element[0]*0.05);
//        double gamm = (priceUp + priceDown - 2*prc) / (2 * element[0] * 0.05);
//        double veg = 0;
//        if (volType == "LV")
//        {
//            veg = (priceVUp - priceVDown) / (2 * 0.05);
//        }
//        if (volType == "SV")
//        {
//            veg = (priceVUp - priceVDown) / (2 * Math.Pow(pathGen.InitVol, 2) * 0.0025) * 2 * pathGen.InitVol;
//        }
//        Console.WriteLine(delt);
//        deltas.Add(delt);
//        gammas.Add(gamm);
//        vegas.Add(veg);
//    }
//    double[] spotDoubles = new double[spotList.Count];
//    for (int i = 0; i < spotList.Count; i++)
//    {
//        spotDoubles[i] = spotList[i][0];
//    }
//    double[] deltasDouble = deltas.ToArray();
//    double[] gammasDouble = gammas.ToArray();
//    double[] vegasDouble = vegas.ToArray();

//    Utility.ScatterPlot2D(spotDoubles, deltasDouble,"Spot","Delta", "Delta function of spots");
//    Utility.ScatterPlot2D(spotDoubles, gammasDouble, "Spot", "Gamma", "Gammas function of spots");
//    Utility.ScatterPlot2D(spotDoubles, vegasDouble, "Spot", "Vega", "Vega function of spots");


////// printing the diffusion Matrix 
////double stepSize = 1.0 / 252;
////double[] linspace = Enumerable.Range(0, (int)(T / stepSize) * 252 + 1)
////                 .Select(i => i * stepSize)
////                 .ToArray();
////var myPlot = new ScottPlot.Plot();
////for (int i = 0; i < N; i++)
////{
////    double[] arraySpots = new double[(int)Math.Ceiling(252 * T) + 1];
////    for (int j = 0; j < (int)Math.Ceiling(252 * T) + 1; j++)
////    {
////        arraySpots[j] = SMatrix[i, j];
////    }

////    myPlot.Add.ScatterLine(linspace, arraySpots);
////}
////myPlot.Axes.Bottom.Label.Text = "Time";
////myPlot.Axes.Left.Label.Text = "Spot";
////myPlot.Axes.Title.Label.Text = "Diffusion Matrix" + volType;
////myPlot.SavePng("Diffusion Matrix.png", 400, 300);
////    // Display the elapsed time in milliseconds
////    Console.WriteLine($"Time taken: {stopwatch.ElapsedMilliseconds} ms");
///


//double stepSize = 1.0 / 252;
//double[] linspace = Enumerable.Range(0, (int)(T / stepSize) * 252 + 1)
//                     .Select(i => i * stepSize)
//                     .ToArray();
//var myPlot = new ScottPlot.Plot();
//double[,] SMatrix = SMatrixList[1];
//for (int i = 0; i < N; i++)
//{
//    double[] arraySpots = new double[(int)Math.Ceiling(252 * T) + 1];
//    for (int j = 0; j < (int)Math.Ceiling(252 * T) + 1; j++)
//    {
//        arraySpots[j] = SMatrix[i, j];
//    }

//    myPlot.Add.ScatterLine(linspace, arraySpots);
//}
//myPlot.Axes.Bottom.Label.Text = "Time";
//myPlot.Axes.Left.Label.Text = "Spot";
//myPlot.Axes.Title.Label.Text = "Diffusion Matrix" + volType;
//myPlot.SavePng("Diffusion Matrix.png", 400, 300);


//Console.WriteLine("starting the plots");
//// Plotting An autocall Greeks

//List<List<double>> spotList = new List<List<double>>();
//List<double> gammas = new List<double>();
//List<double> deltas = new List<double>();
//List<double> vegas = new List<double>();
////for (int i = -5; i < 6; i++)
//double[] spotss = new double[] { 600, 620, 640, 680, 720, 760, 800, 830, 860, 890, 900, 940, 980, 1000 };
//foreach(var spot in spotss )
//{
//    List<double> spots = new List<double>();
//    spots.Add( spot);
//    spotList.Add(spots);
//}
////foreach (var element in spotList)
////{

////    Matrix<double> randMat = qmcgenerator.GenerateRandomMatrix(n, N, T, heston);
////    var pathGen = new PathGenerator(n, N, pathsList, initialDividends, nssModel.GetRate, volType, listParamsMatrix, hestonParams, randMat);
////    double[,] smat = (double[,])pathGen.GeneratePaths(T, element, null, null);
////    double[,] smatUp = (double[,])pathGen.GeneratePaths(T, element, "Delta", "Up");
////    double[,] smatDown = (double[,])pathGen.GeneratePaths(T, element, "Delta", "Down");
////    //double[,] smatVUp = (double[,])pathGen.GeneratePaths(T, element, "Vega", "Up");
////    //double[,] smatVDown = (double[,])pathGen.GeneratePaths(T, element, "Vega", "Down");
////    double[,] evalMatrixUp = (double[,])payoffCalculator.ComputeEvaluationMatrix(smatUp, element, obsDates);
////    double[,] evalMatrixDown = (double[,])payoffCalculator.ComputeEvaluationMatrix(smatDown, element, obsDates);
////    double[,] evalMat = (double[,])payoffCalculator.ComputeEvaluationMatrix(smat, element, obsDates);
////    //double[,] evalMatrixVUp = (double[,])payoffCalculator.ComputeEvaluationMatrix(smatVUp, element, obsDates);
////    //double[,] evalMatrixVDown = (double[,])payoffCalculator.ComputeEvaluationMatrix(smatVDown, element, obsDates);
////    double[,] payoffMatUp = (double[,])payoffCalculator.ComputePayoff(evalMatrixUp, T, obsDates, totalNbrObs, k, smoothingFactor);
////    double[,] payoffMatDown = (double[,])payoffCalculator.ComputePayoff(evalMatrixDown, T, obsDates, totalNbrObs, k, smoothingFactor);
////    double[,] payoffMat = (double[,])payoffCalculator.ComputePayoff(evalMat, T, obsDates, totalNbrObs, k, smoothingFactor);
////    //double[,] payoffMatVUp = (double[,])payoffCalculator.ComputePayoff(evalMatrixVUp, T, obsDates, totalNbrObs, k, smoothingFactor);
////    //double[,] payoffMatVDown = (double[,])payoffCalculator.ComputePayoff(evalMatrixVDown, T, obsDates, totalNbrObs, k, smoothingFactor);
////    double priceUp = PayoffCalculator.ComputePrice(n, notional, payoffMatUp, nssModel.GetRate, obsDates);
////    double prc = PayoffCalculator.ComputePrice(n, notional, payoffMat, nssModel.GetRate, obsDates);
////    double priceDown = PayoffCalculator.ComputePrice(n, notional, payoffMatDown, nssModel.GetRate, obsDates);
////    //double priceVUp = PayoffCalculator.ComputePrice(n, notional, payoffMatVUp, nssModel.GetRate, obsDates);
////    //double priceVDown = PayoffCalculator.ComputePrice(n, notional, payoffMatVDown, nssModel.GetRate, obsDates);
////    double delt = (priceUp - priceDown) / (2 * element[0] * 0.05);
////    //double gamm = (priceUp + priceDown - 2 * prc) / (2 * element[0] * 0.05);
////    //double veg = 0;
////    //if (volType == "LV")
////    //{
////    //    veg = (priceVUp - priceVDown) / (2 * 0.05);
////    //}
////    //if (volType == "SV")
////    //{
////    //    veg = (priceVUp - priceVDown) / (2 * Math.Pow(pathGen.InitVol, 2) * 0.0025) * 2 * pathGen.InitVol;
////    //}
////    Console.WriteLine(delt);
////    deltas.Add(delt);
////    //gammas.Add(gamm);
////    //vegas.Add(veg);
////}
////double[] spotDoubles = new double[spotList.Count];
////for (int i = 0; i < spotList.Count; i++)
////{
////    spotDoubles[i] = spotList[i][0];
////}
////double[] deltasDouble = deltas.ToArray();
//////double[] gammasDouble = gammas.ToArray();
//////double[] vegasDouble = vegas.ToArray();

////Utility.ScatterPlot2D(spotDoubles, deltasDouble, "Spot", "Delta", "Delta function of spots");
//////Utility.ScatterPlot2D(spotDoubles, gammasDouble, "Spot", "Gamma", "Gammas function of spots");
//////Utility.ScatterPlot2D(spotDoubles, vegasDouble, "Spot", "Vega", "Vega function of spots");

////Console.WriteLine("Plotting ended");