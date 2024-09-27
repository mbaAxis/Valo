using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Vml;
using DocumentFormat.OpenXml.Vml.Office;
using ScottPlot.Hatches;
using System;
using System.Collections.Generic;
using System.Linq;


public class FHCClosedFormulae
{
    private int n;
    private int N;
    private int freqObs;
    public string volType;
    public double rho;
    public double sigmaS;
    public double sigmaV;
    public double Ks;
    public double Kv;
    public double lbda;
    public Func<double, double> Curve { get; }
    public List<double[]> closingPricesList;
    private double BP;
    private double AT;
    private double coupon;
    public double notional;
    public PayoffCalculator _payoffCalculator;
    public PathGenerator _pathGenerator;
    public int nbreffectiveHegde;
    public int optimalNs;


    public FHCClosedFormulae(int n, int N, int freqObs, string volType, double rho, double sigmaS, double sigmaV, double Ks, double Kv, double lbda,
        Func<double, double> curve, List<double[]> closingPricesList, double BP, double AT, double coupon, double notional, PathGenerator pathGenerator, PayoffCalculator payoffCalculator)
    {
        this.n = n;
        this.N = N;
        this.freqObs = freqObs;
        this.volType = volType;
        this.rho = rho;
        this.sigmaS = sigmaS;
        this.sigmaV = sigmaV;
        this.Ks = Ks;
        this.Kv = Kv;
        this.lbda = lbda;
        this.Curve = curve;
        this.BP = BP;
        this.AT = AT;
        this.coupon = coupon;
        this.notional = notional;
        this.closingPricesList = closingPricesList;
        _pathGenerator = pathGenerator;
        _payoffCalculator = payoffCalculator;
        this.nbreffectiveHegde = 0;
        this.optimalNs = 0;
    }
    static double[,] ReshapeArray(double[] originalArray, int rows, int columns)
    {
        if (originalArray.Length != rows * columns)
        {
            throw new ArgumentException("The total number of elements does not match the specified dimensions.");
        }
        double[,] reshapedArray = new double[rows, columns];
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                reshapedArray[i, j] = originalArray[i * columns + j];
            }
        }

        return reshapedArray;
    }
    public object ExtractEvalPaths(object matrix, int index, List<int> obsDates, double divisor)
    {
        int[] columnsToExtract = (int[])obsDates.ToArray();
        List<double> initSpotsList = new List<double>();
        if (this.n > 1)
        {
            List<double[,]> listSpotMatrices = (List<double[,]>)matrix;
            List<double[,]> evalPaths = new List<double[,]>();  
            for (int i = 0; i < listSpotMatrices.Count; i++)
            {
                initSpotsList.Add(listSpotMatrices[i][index, 0]);
                double[] extractedValues = columnsToExtract.Select(columnIndex => listSpotMatrices[i][index, columnIndex]).ToArray();
                double[] evalPath = extractedValues.Select(value => value / initSpotsList[i]).ToArray();
                double[,] evalPathMatrix = ReshapeArray(evalPath, 1, evalPath.GetLength(0));
                evalPaths.Add(evalPathMatrix);
            }
            return evalPaths;
        }
        else
        {
            double[,] matrice = (double[,])matrix;
            initSpotsList.Add(matrice[index, 0]);
            double[] extractedValues = columnsToExtract.Select(columnIndex => matrice[index, columnIndex]).ToArray();
            double[] evalPath = extractedValues.Select(value => value / divisor).ToArray();
            double[,] evalPathMatrix = ReshapeArray(evalPath, 1, evalPath.GetLength(0));
            return evalPathMatrix;
        }
    }

    public object ComputePrice(object ep, double T, List<int> obsDates, int totalNbrObs)
    {
        if (this.n > 1)
        {
            List<double[,]> payoffMatList = (List<double[,]>)_payoffCalculator.ComputePayoff(ep, T, obsDates, totalNbrObs);
            double price = PayoffCalculator.ComputePrice(this.n, this.notional, payoffMatList, Curve, obsDates);
            return price;
        }
        else
        {
            double[,] payoffMat = (double[,])_payoffCalculator.ComputePayoff(ep, T, obsDates, totalNbrObs);
            double price = PayoffCalculator.ComputePrice(this.n, notional, payoffMat, Curve, obsDates);
            return price;
        }
    }

    public object ComputeExpectationAj(PathGenerator pg, double T, double rho, double sigmaS, double sigmaV, int couponCounter,
        List<int> obsDates, int totalNbrObs, object generatedMatrices,object matrix, string comp)
    {
        double nbrNullGammaVanna = 0;
        double nbrNullVolgaVanna = 0;
        double AperHedgeDate = 0;
        double initVol = pg.InitVol;
        if (this.n > 1)
        {
            List<double[,]> matrixNeutral = (List<double[,]>)matrix;
            List<List<List<double[,]>>> generatedMatricesList = (List<List<List<double[,]>>>)generatedMatrices;
            for (int k = 0; k < this.N; k++)
            {
                int index = k;
                List<List<double[,]>> extractedEvalPathsAllMatrices = new List<List<double[,]>>();
                List<List<double>> initSpots = new List<List<double>>();
                for (int i = 0; i < generatedMatricesList.Count; i++)
                {
                    List<List<double[,]>> matrixList = generatedMatricesList[i];
                    for (int j = 0; j < matrixList.Count; j++)
                    {

                        List<double[,]> matrixStressedNeutral = matrixList[j];
                        List<double> spots = new List<double>();
                        for (int a = 0; a < matrixStressedNeutral.Count; a++)
                        {
                            spots.Add(matrixStressedNeutral[a][k, 0]);
                        }
                        List<double[,]> evalPaths = (List<double[,]>)ExtractEvalPaths(matrixStressedNeutral, k, obsDates, 0);
                        
                        extractedEvalPathsAllMatrices.Add(evalPaths);
                        initSpots.Add(spots);
                    }
                }
                List<double> pricesAllMatricesOnePath = new List<double>();
                for (int i = 0; i < extractedEvalPathsAllMatrices.Count; i++)
                {
                    List<double[,]> evalPaths = extractedEvalPathsAllMatrices[i];
                    double price = (double)ComputePrice(evalPaths, T, obsDates, totalNbrObs);
                    pricesAllMatricesOnePath.Add(price);
                }
                double[] prSUpVUpList = new double[2];
                double[] prSDownVDownList = new double[2];
                double[] prSUpVDownList = new double[2];
                double[] prSDownVUpList = new double[2];
                double[] prSUpList = new double[2];
                double[] prSDownList = new double[2];
                double[] prVUpList = new double[2];
                double[] prVDownList = new double[2];
                List<double[]> priceLists = new List<double[]>
                {
                    prSUpVUpList,
                    prSDownVDownList,
                    prSUpVDownList,
                    prSDownVUpList,
                    prSUpList,
                    prSDownList,
                    prVUpList,
                    prVDownList
                };
                List<double> initSpotsNeutral = new List<double>();
                for (int a = 0; a < matrixNeutral.Count; a++)
                {
                    initSpotsNeutral.Add(matrixNeutral[a][k, 0]);
                }
                List<double[,]> evalPathsNeutral = (List<double[,]>)ExtractEvalPaths(matrixNeutral, k, obsDates, 0);
                double priceNeutral = (double)ComputePrice(evalPathsNeutral, T, obsDates, totalNbrObs);
                for (int i = 0; i < priceLists.Count; i++)
                {
                    int alpha = 2 * i;
                    priceLists[i][0] = pricesAllMatricesOnePath[alpha];
                    priceLists[i][1] = pricesAllMatricesOnePath[alpha + 1];
                }
                double[] gammaPath = new double[this.n]; // each of these double[] will contain the gamma of the autocall wrt to one asset; they need to be summed up later
                double[] vegaPath = new double[this.n]; // should the greeks be computed with neutral init spots or stressed init spots. 
                double[] vannaPath = new double[this.n];
                double[] volgaPath = new double[this.n];
                double[] cashGammaperPathperDate = new double[this.n];
                double[] cashVannaperPathperDate = new double[this.n];
                double[] cashVolgaperPathperDate = new double[this.n];
                for (int i = 0; i < this.n; i++)
                {
                    gammaPath[i] = ((prSUpList[i] + prSDownList[i] - 2 * priceNeutral) / Math.Pow(0.1, 2))/100;
                    volgaPath[i] = (prVUpList[i] + prVDownList[i] - 2 * priceNeutral) / (Math.Pow(0.05, 2));
                    vegaPath[i] = (prVUpList[i] - prVDownList[i]) / (2 * 0.05);
                    vannaPath[i] = (prSUpVUpList[i] - prSUpVDownList[i] - prSDownVUpList[i] + prSDownVDownList[i]) / (4 * 0.05 * 0.1);
                    cashGammaperPathperDate[i] = gammaPath[i] * Math.Pow(initSpotsNeutral[i], 2);
                    cashVannaperPathperDate[i] = vannaPath[i] * initSpotsNeutral[i] * pg.InitVol;
                    cashVolgaperPathperDate[i] = volgaPath[i] * Math.Pow(pg.InitVol, 2);
                }
                if (gammaPath.Sum() == 0 && vannaPath.Sum() == 0)
                {
                    nbrNullGammaVanna += 1;
                }
                if (volgaPath.Sum() == 0 && vannaPath.Sum() == 0)
                {
                    nbrNullVolgaVanna += 1;
                }
                if (comp == "gv")
                {
                    double AperPathperHedgeDate = Math.Pow(sigmaS, 2) * Math.Pow(cashGammaperPathperDate.Sum(), 2)
                        + Math.Pow(sigmaV, 2) * Math.Pow(cashVannaperPathperDate.Sum(), 2) + 2 * rho * sigmaV * sigmaS *
                        cashVannaperPathperDate.Sum() * cashGammaperPathperDate.Sum();
                    AperHedgeDate += Math.Sqrt(AperPathperHedgeDate);
                }
                if (comp == "vv")
                {
                    double AperPathperHedgeDate = Math.Pow(sigmaS, 2) * Math.Pow(cashVolgaperPathperDate.Sum(), 2)
                        + Math.Pow(sigmaV, 2) * Math.Pow(cashVannaperPathperDate.Sum(), 2) + 2 * rho * sigmaV * sigmaS *
                        cashVannaperPathperDate.Sum() * cashVolgaperPathperDate.Sum();
                    AperHedgeDate += Math.Sqrt(AperPathperHedgeDate);
                }
            }
            if (comp == "gv")
            {
                return AperHedgeDate / (this.N - nbrNullGammaVanna);
            }
            else
            {
                return AperHedgeDate / (this.N - nbrNullVolgaVanna);
            }
        }
        else
        {
            List<double[,]> generatedMatricesList = (List<double[,]>)generatedMatrices;
            for (int k = 0; k < this.N; k++)
            {
                int index = k;
                double cashGammaperPathperDate = new double();
                double cashVannaperPathperDate = new double();
                double cashVolgaperPathperDate = new double();
                double gammaPath = new double();
                double vegaPath = new double();
                double vannaPath = new double();
                double volgaPath = new double();
                List<double> pricesAllMatrices = new List<double>();
                List<double[,]> extractedEvalPathsAllMatrices = new List<double[,]>();
                List<double> initSpotsList = new List<double>();
                for (int i = 0; i < generatedMatricesList.Count; i++)
                {
                    double[,] mat = generatedMatricesList[i];
                    for (int j = 0; j < this.n; j++)
                    {
                        initSpotsList.Add(mat[k, 0]);
                    }

                    double[,] extractedEvalPaths = (double[,])ExtractEvalPaths(generatedMatricesList[i], index, obsDates, generatedMatricesList[0][k, 0]);
                    extractedEvalPathsAllMatrices.Add(extractedEvalPaths);
                }
                for (int i = 0; i < extractedEvalPathsAllMatrices.Count; i++)
                {
                    double[,] evalPaths = extractedEvalPathsAllMatrices[i];
                    double pricePath = (double)ComputePrice(evalPaths, T, obsDates, totalNbrObs);
                    pricesAllMatrices.Add(pricePath);
                }
                double price = pricesAllMatrices[0];
                double prSUpVUpList = pricesAllMatrices[1];
                double prSDownVDownList = pricesAllMatrices[2];
                double prSUpVDownList = pricesAllMatrices[3];
                double prSDownVUpList = pricesAllMatrices[4];
                double prSUpList = pricesAllMatrices[5];
                double prSDownList = pricesAllMatrices[6];
                double prVUpList = pricesAllMatrices[7];
                double prVDownList = pricesAllMatrices[8];
                gammaPath = ((prSUpList + prSDownList - 2 * price) / Math.Pow(0.1, 2)) / 100;
                if (this.volType == "SV")
                {
                    volgaPath = ((prVUpList + prVDownList - 2 * price) / (Math.Pow(Math.Pow(pg.InitVol, 2) * 0.0025, 2))) * 4 * Math.Pow(pg.InitVol, 2);
                    vegaPath = ((prVUpList - prVDownList) / (2 * Math.Pow(pg.InitVol, 2) * 0.0025)) * 2 * pg.InitVol;
                    vannaPath = (prSUpVUpList - prSUpVDownList - prSDownVUpList + prSDownVDownList) / (4 * (Math.Pow(pg.InitVol, 2) * 0.0025) * 0.1) * 2 * pg.InitVol;
                }
                if (this.volType != "SV")
                {
                    volgaPath = (prVUpList + prVDownList - 2 * price) / (Math.Pow(0.05, 2));
                    vegaPath = (prVUpList - prVDownList) / (2 * 0.05);
                    vannaPath = (prSUpVUpList - prSUpVDownList - prSDownVUpList + prSDownVDownList) / (4 * 0.05 * 0.1);
                }
                cashGammaperPathperDate = gammaPath * Math.Pow(initSpotsList[0], 2);
                cashVannaperPathperDate = vannaPath * initSpotsList[0] * pg.InitVol;
                cashVolgaperPathperDate = volgaPath * Math.Pow(pg.InitVol, 2);
                if (gammaPath == 0 && vannaPath == 0)
                {
                    nbrNullGammaVanna += 1;
                }
                if (volgaPath == 0 && vannaPath == 0)
                {
                    nbrNullVolgaVanna += 1;
                }
                if (comp == "gv")
                {
                    double AperPathperHedgeDate = Math.Pow(sigmaS, 2) * Math.Pow(cashGammaperPathperDate, 2)
                        + Math.Pow(sigmaV, 2) * Math.Pow(cashVannaperPathperDate, 2) + 2 * rho * sigmaV * sigmaS *
                        cashVannaperPathperDate * cashGammaperPathperDate;
                    AperHedgeDate += Math.Sqrt(AperPathperHedgeDate);

                }
                if (comp == "vv")
                {
                    double AperPathperHedgeDate = Math.Pow(sigmaS, 2) * Math.Pow(cashVolgaperPathperDate, 2)
                        + Math.Pow(sigmaV, 2) * Math.Pow(cashVannaperPathperDate, 2) + 2 * rho * sigmaV * sigmaS *
                        cashVannaperPathperDate * cashVolgaperPathperDate;
                    AperHedgeDate += Math.Sqrt(AperPathperHedgeDate);
                }
            }
            if (comp == "gv")
            {
                return AperHedgeDate / (this.N- nbrNullGammaVanna);
            }
            else
            {
                return AperHedgeDate / (this.N- nbrNullVolgaVanna);
            }
        }

    }

    public double OptimalNbrHedges(double lbda, double omega, double T, double ks)
    {
        double Ns = lbda * omega * Math.Sqrt(T * Math.PI) / (2 * ks);
        this.optimalNs = (int)Math.Ceiling(Ns);
        return Ns;
    }

    public (double,double) CombinedGammaVanna(double Ns, List<double[]> closingpricesList, double T)
    {
        double combGammaVanna = 0;
        double combVannaVolga = 0;
        int NsInt = (int)Ns;
        List<int> hedgeDates = FHCHelpers.ComputeHedgeDates(T, NsInt);
        List<int> obsDates = PayoffCalculator.ComputeObsDates(T, this.freqObs);
        int totalNbrObs = obsDates.Count;
        int oldPosition = 0; int couponCounter = 0; int positionCounter = 0; int position = 0;
        var fhcHelpers = new FHCHelpers(this.n, this.N, this.notional, this.freqObs, this.BP, this.AT, this.coupon, this.Curve, NsInt,this.closingPricesList);

        for (int i = 1; i < hedgeDates.Count; i++)
        {
            int hedgeDate = hedgeDates[i];
            double hedgeDateCurve = (double)hedgeDate / 252;
            double r = this.Curve(hedgeDateCurve) / 100;
            var result = FHCHelpers.CheckPosition(obsDates, hedgeDate, couponCounter, positionCounter, oldPosition);
            (position, oldPosition, positionCounter, couponCounter) = result;
            var checkBreakHedge = (ValueTuple<List<double>, bool>)fhcHelpers.CheckBreakHedge(closingpricesList, position, obsDates, oldPosition, positionCounter, T, couponCounter, AT, BP, coupon, notional);
            List<double> finalPayoffsList = checkBreakHedge.Item1;
            bool breakState = checkBreakHedge.Item2;
            if (breakState)
                break;
            this.nbreffectiveHegde += 1;
            if (this.n > 1)
            {
                List<double> initSpotsList = FHCHelpers.ExtractSpotList(this.closingPricesList, hedgeDate);
                List<double[,]> matrix = (List<double[,]>)_pathGenerator.GeneratePaths(T - hedgeDate / 252, initSpotsList, null, null,-1);
                var pathTypesAndLists = new Dictionary<string, List<List<double[,]>>>()
                    {
                        { "Vanna_Up", new List<List<double[,]>>() },
                        { "Vanna_Down", new List<List<double[,]>>()  },
                        { "Vanna_Up_down", new List<List<double[,]>>()  },
                        { "Vanna_Down_up", new List<List<double[,]>>()  },
                        { "Delta_Up", new List<List<double[,]>>()  },
                        { "Delta_Down", new List<List<double[,]>>()  },
                        { "Vega_Up", new List<List<double[,]>>() },
                        { "Vega_Down", new List < List < double[,] > >() }
                    };
                for (int k = 0; k < this.n; k++)
                {
                    foreach (var entry in pathTypesAndLists)
                    {
                        string[] splitType = entry.Key.Split('_');
                        string pathType = splitType[0];
                        string direction = splitType[1];
                        entry.Value.Add((List<double[,]>)_pathGenerator.GeneratePaths(T - hedgeDate / 252, initSpotsList, pathType, direction, k));
                    }
                }
                List<List<List<double[,]>>> generatedMatricesList = new List<List<List<double[,]>>>();
                foreach (var entry in pathTypesAndLists)
                {
                    generatedMatricesList.Add(entry.Value);
                }
                List<int> obsDatesNew = (List<int>)obsDates.Select(x => x - hedgeDate).Where(x => x > 0).ToList();
                position = Array.BinarySearch(obsDates.ToArray(), hedgeDate);
                position = position < 0 ? ~position : position;
                if (position != couponCounter)
                {
                    oldPosition = positionCounter;
                    positionCounter = position;
                    couponCounter = couponCounter + 1;
                }
                double AperHedgeDateDelta = (double)ComputeExpectationAj(_pathGenerator, T, this.rho, this.sigmaS, this.sigmaV, couponCounter,
                        obsDates, totalNbrObs, generatedMatricesList,matrix, "gv");
                double AperHedgeDateVega = (double)ComputeExpectationAj(_pathGenerator, T, this.rho, this.sigmaS, this.sigmaV, couponCounter,
                        obsDates, totalNbrObs, generatedMatricesList, matrix, "vv");
                combGammaVanna += Math.Exp(-r * hedgeDate / 252) * AperHedgeDateDelta;
                combVannaVolga += Math.Exp(-r * hedgeDate / 252) * AperHedgeDateVega;
            }
            else
            {
                List<double> initSpotsList = new List<double>();
                for (int k = 0; k < closingpricesList.Count; k++)
                {
                    double[] closingPricesHist = closingpricesList[k];
                    initSpotsList.Add(closingPricesHist[hedgeDate]);
                }
                string[] pathtypesList ={"null_null","Vanna_Up","Vanna_Down","Vanna_Up_down","Vanna_Down_up","Delta_Up","Delta_Down","Vega_Up","Vega_Down"};
                List<double[,]> generatedMatricesList = new List<double[,]>();
                foreach (var pathtype in pathtypesList)
                {
                    string[] splitType = pathtype.Split('_');
                    string pathType = splitType[0];
                    string direction = splitType[1];
                    generatedMatricesList.Add((double[,])_pathGenerator.GeneratePaths(T - hedgeDate / 252, initSpotsList, pathType, direction, -1));
                }
                List<int> obsDatesNew = (List<int>)obsDates.Select(x => x - hedgeDate).Where(x => x > 0).ToList();
                position = Array.BinarySearch(obsDates.ToArray(), hedgeDate);
                position = position < 0 ? ~position : position;
                if (position != couponCounter)
                {
                    oldPosition = positionCounter;
                    positionCounter = position;
                    couponCounter = couponCounter + 1;
                }
                double AperHedgeDateGV = (double)ComputeExpectationAj(_pathGenerator, T, this.rho, this.sigmaS, this.sigmaV, couponCounter,
                        obsDatesNew, totalNbrObs, generatedMatricesList,null, "gv");
                double AperHedgeDateVV = (double)ComputeExpectationAj(_pathGenerator, T, this.rho, this.sigmaS, this.sigmaV, couponCounter,
                        obsDatesNew, totalNbrObs, generatedMatricesList, null, "vv");

                combGammaVanna += Math.Exp(-r * hedgeDate / 252) * AperHedgeDateGV;
                combVannaVolga += Math.Exp(-r * hedgeDate / 252) * AperHedgeDateVV;
            }
        }
        return (combGammaVanna / Ns, combVannaVolga /Ns);
    }
    

    public (double,double) ComputeFHCDeltaVega(double T, List<double[]> closingpricesList, double ks)
    {
        double Ns = OptimalNbrHedges(lbda, 1, T, ks);
        (double combGamVan , double combVolVan ) = CombinedGammaVanna(Ns, closingpricesList, T);
        double resultDeltaHedge = combGamVan * (Math.Sqrt((this.lbda * this.Ks) / (this.sigmaS * Math.Sqrt(T))) * this.sigmaS * T / Math.Pow(Math.PI, 0.25));
        double resultVegaHedge = combVolVan * (Math.Sqrt((this.lbda * this.Kv) / (this.sigmaV * Math.Sqrt(T))) * this.sigmaV * T / Math.Pow(Math.PI, 0.25));
        Console.WriteLine($"The transaction costs for hedging the delta is {resultDeltaHedge} ");
        Console.WriteLine($"The transaction costs for hedging the vega is {resultVegaHedge} ");
        return (resultDeltaHedge,resultVegaHedge);
    }
}


