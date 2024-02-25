using System.Diagnostics;
using System.Runtime.Intrinsics.X86;
using ValoLibrary;
using static ValoLibrary.UDF;
using static ValoLibrary.BlackScholes;


//=========================================================================================================
double T = 1;
double T2 = 44989;
double S = 100;
double strike = 100;
double r = 0.05;
//double r = 0.967197916277398;
double sigma = 0.2;
//double sigma = Math.Sqrt(2 * Math.PI / T) * 0.16 / S;
string underlying = "CAC40";
UDF exemple = new();
string position1 = "long";
string position2 = "short";
//double q = 0.306845697901433;
double q = 0.05;



string Flag = "call";
string Flag1 = "put";

double mc = exemple.GetMCEurOptionPrice(Flag, position2, S, sigma, r, strike, T);
double prix = exemple.GetBSOptionPrice(Flag, position1, S, sigma, r, strike, T, q);
double prix3 = exemple.GetBSOptionPrice(Flag1, position2, S, sigma, r, strike, T, q);

double delta = exemple.GetDeltaBS(Flag, position1, S, sigma, r, strike, T, q);
//double[,] sensi = exemple.GetSensiOptionBS(Flag, S, sigma, r, strike, T, q);
////double[,] sensi2 = exemple.GetSensiOption(Flag, strike, T2, underlying);

//double prix5= exemple.GetOptionPrice(Flag, position1, strike, T2, underlying, 0);

Console.WriteLine("mc call =" + mc);
Console.WriteLine("BS call ="+ prix);
Console.WriteLine("BS put =" + prix3);
Console.WriteLine("delta =" + delta);

//Console.WriteLine("call =" + prix5);

//foreach (var kvp in sensi)
//{
//    Console.WriteLine($"sensi cal BS: {kvp}");
//}
//foreach (var sen in sensi2)
//{
//    Console.WriteLine($"{sen}");
//}

//===========================================portfolio======================

BSParameters[] params1 = new BSParameters[2];
params1[0] = new BSParameters { optionFlag = Flag, position = position1, s = S, sigma = sigma, r = r, k = strike, T = T, q = q };
params1[1] = new BSParameters { optionFlag = Flag1, position = position2, s = S, sigma = sigma, r = r, k = strike, T = T, q = q };

double portfolioPrice = MonteCarlo.MCEurOptionPortfolioPrice(2, params1);

Console.WriteLine("Prix du portefeuille d'options BS : " + portfolioPrice);

//List<Parameters2> options = new List<Parameters2>
//{
//    new Parameters2 { optionFlag = "call", s = S, sigma = sigma, r = r, strike = strike, T = T, q = q },
//    new Parameters2 { optionFlag = "put", s = 100, sigma = 0.2, r = r, strike = strike, T = T, q = q }
//    // Ajoutez d'autres options au besoin
//};

//int numberOfOptions = 2;

//double portfolioDelta = exemple.GetDeltaBSPortfolio(numberOfOptions, options);
//Console.WriteLine("Portfolio Delta: " + portfolioDelta);


