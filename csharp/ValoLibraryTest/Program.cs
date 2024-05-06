using System.Net.WebSockets;
using System.Numerics;
using ValoLibrary;

////////////////////////////////////: OPtion//////////////////////////////////::::::
////=========================================================================================================
//double T = 1;
//double T2 = 44989;
//double S = 100;
//double strike = 100;
//double r = 0.05;
////double r = 0.967197916277398;
//double sigma = 0.2;
////double sigma = Math.Sqrt(2 * Math.PI / T) * 0.16 / S;
//string underlying = "CAC40";
//UDF exemple = new();
//string position1 = "long";
////string position2 = "short";
////double q = 0.306845697901433;


//double q = 0;
//double quantity = 2;




//string Flag = "call";
//string Flag1 = "put";
//double mc = exemple.GetMCEurOptionPrice(quantity, Flag, position1, S, sigma, r, strike, T);
//double mc2 = exemple.GetMCEurOptionPrice(quantity, Flag1, position1, S, sigma, r, strike, T);
//double prix11 = exemple.GetBSOptionPrice(quantity, Flag, position1, S, sigma, r, strike, T, q);
////double prix12 = exemple.GetBSOptionPrice(quantity, Flag1, position1, S, sigma, r, strike, T, q);
////double prix12 = exemple.GetBSOptionPrice(1, Flag, position1, S, sigma, r, strike, T, q);

////double delta = exemple.GetDeltaBS(quantity, Flag, position1, S, sigma, r, strike, T, q);
//double[,] sensi = exemple.GetSensiOptionBS(quantity, Flag, position1, S, sigma, r, strike, T, q);
//double[,] sensi2 = exemple.GetSensiOption(quantity, Flag, position1, strike, T2, underlying);
////double prix5= exemple.GetOptionPrice(Flag, position1, strike, T2, underlying, 0);

//Console.WriteLine("mc call =" + mc);
////Console.WriteLine("mc put =" + mc2);
//Console.WriteLine("BS call2 =" + prix11);
//Console.WriteLine("BS put2 =" + prix12);
//Console.WriteLine("BS call1 =" + prix12);
//Console.WriteLine("BS call12 =" + 2 * prix12);
//Console.WriteLine("delta =" + delta);

////Console.WriteLine("call =" + prix5);

//foreach (var kvp in sensi)
//{
//    Console.WriteLine($"sensi cal BS: {kvp}");
//}
//foreach (var sen in sensi2)
//{
//    Console.WriteLine($"{sen}");
//}

////===========================================portfolio======================
//double strike2 = 100;
//BSParameters[] params1 = [

//new BSParameters { quantity = quantity, optionFlag = Flag, position = position1, s = S, sigma = sigma, r = r, k = strike2, T = T, q = q },
//    new BSParameters { quantity = quantity, optionFlag = Flag, position = position1, s = S, sigma = sigma, r = r, k = strike2, T = T, q = q },

//];

//double portfolioPrice = exemple.GetBSOptionPortfolioPrice(params1);

//Console.WriteLine("Prix du portefeuille d'options BS : " + portfolioPrice);

//////////////////////////////////////////////////////////:::end Option////////////////////////////////////////////////////////////////////////

//Console.WriteLine("===================Test Utility Matrix====================");

//// Exemple d'utilisation de la fonction Choleski avec une matrice 3x3
//double[,] matrix = { { 4, 12, -16 }, { 12, 37, -43 }, { -16, -43, 98 } };
//int nStart = 0;
//int nEnd = 2;

//double[,] result = UtilityMatrix.Choleski(matrix, nStart, nEnd);
//Console.WriteLine("Resultant Choleski Matrix:");
//for (int i = nStart; i <= nEnd; i++)
//{
//    for (int j = nStart; j <= nEnd; j++)
//    {
//        Console.Write(result[i, j] + "\t");
//    }
//    Console.WriteLine();
//}
//Console.WriteLine("===================End Test Utility Matrix====================");

///===============================================================================================================================
//Console.WriteLine("===================Test Utility Dates====================");

//DateTime paramDate2 = new(2024, 3, 3);
//string period = "2y";
//double monthPeriod = UtilityDates.MonthPeriod(period, paramDate2);
//Console.WriteLine($"monthPeriod: {monthPeriod}");

////DateTime maturityDate = new(2025, 3, 3);
//string maturityDate = "2y";//"2Y";
//DateTime convertedDate = UtilityDates.ConvertDate(paramDate2, maturityDate);
//Console.WriteLine($"convertedDate: {convertedDate}");

//DateTime startDate = new (2024, 3, 1);
//DateTime endDate = new(2026, 4, 1);
//double duration = UtilityDates.DurationYear(endDate, startDate);
//Console.WriteLine($"Durée en années : {duration}");

//DateTime paramDate3 = new(2024, 3, 3);
//DateTime maturity = new(2025, 3, 3);
//DateTime cpnLastSettle = new(2024, 3, 3);
//string cpnPeriod = "3M";
//string cpnConvention = "ShortFirst";

//DateTime[] schedule = UtilityDates.SwapSchedule(paramDate3, maturity, cpnLastSettle, cpnPeriod, cpnConvention);
//if (schedule != null)
//{
//    Console.WriteLine("Swap Schedule:");
//    foreach (DateTime date in schedule)
//    {
//        Console.WriteLine(date.ToString("yyyy-MM-dd"));
//    }
//}
//else
//{
//    Console.WriteLine("Error in Swap Schedule calculation.");
//}

//Console.WriteLine("===================End Test Utility Dates====================");


//Console.WriteLine("===================Test Stripping IRS====================");


//object curveName = 9;
//int getcurve = StrippingIRS.GetCurveId(curveName);
//Console.WriteLine("getcurvename = " + getcurve);

//================================================================


//DateTime paramDate1 = new(2024, 3, 1);

//double[]ZC = { 1.0, 1, 1 }; // Exemple de taux de zéro coupon

//string maturityDate = "5Y";
//string[] ZCDate = { "1Y", "2Y", "4Y" }; // Exemple de dates associées
////DateTime maturityDate = new(2029, 3, 1);
////DateTime[] ZCDate = { new(2025, 3, 1), new(2026, 3, 1), new(2028, 3, 1) }; // Exemple de dates associées
//double result1 = StrippingIRS.VbaGetRiskFreeZC(paramDate1, maturityDate, ZC, ZCDate);
//Console.WriteLine($"Le taux de zéro coupon à la date de maturité est : {result1}");

//string curveName = "EUR";

//int swapBasis = 2;
//int swapPeriod = 6;
//string[] curveDates = { "3M", "6M", "9M" };
//double[] swapRates = { 0.05, 0.06, 0.07 };
//double[] strippedZC = { 0.2, 0.3, 0.4 };
//double fxSpot = 0.4;
//bool result2 = StrippingIRS.VbaStoreZC(paramDate1, curveName, swapBasis, swapPeriod, curveDates, swapRates, strippedZC, fxSpot);
//Console.WriteLine("isVBASTRORE ?: " + result2);

//double result4 = StrippingIRS.GetFXSpot (curveName);
//Console.WriteLine("getfxspot =: " + result4);




//string curveName = "EUR";
//DateTime paramDate = new (2024, 3, 21); // DateTime(2024, 2, 16);
//double[] curve = { 0.00978, 0.01034, 0.01104, 0.01207, 0.01309, 0.0141, 0.01509, 0.01604, 0.01698, 0.01787, 0.0212375, 0.0233875 };
//double[] curve1 = { 0.04626, 0.04511, 0.04472, 0.044595, 0.0446379, 0.04479, 0.045035, 0.04529, 0.0455901, 0.0458856, 0.0471141, 0.04758 };// Exemple de taux de courbe
//string[] curveMaturity = { "1Y", "2Y", "3Y", "4Y", "5Y", "6Y", "7Y", "8Y", "9Y", "10Y", "15Y", "20Y" };  // Exemple de maturités de courbe
//int swapPeriod = 12; //6;
//int swapBasis = 4;//3;
//double fxSpot = 1; // 165.5616;

////Console.WriteLine("============================== 1");

//double[] result = StrippingIRS.StripZC(paramDate, curveName, curve1, curveMaturity, swapPeriod, swapBasis, fxSpot);


//if (result != null)
//{
//    // Affichage des résultats
//    Console.WriteLine($"Stripped ZC for curve {curveName}:");

//    for (int i = 0; i < result.Length; i++)
//    {
//        Console.WriteLine($"{result[i]}");
//    }
//}
////else
////{
////    Console.WriteLine("Erreur lors du calcul de Stripped ZC.");
////}
///
//UDF example = new UDF();
//DateTime cdsRollDate = StrippingCDS.CDSRefDate(paramDate);
//DateTime convertDate = example.GetConvertDate(paramDate, "3Y");


//Console.WriteLine("===================End Stripping IRS====================");


Console.WriteLine("===================Test Stripping CDS====================");


string curveName = "EUR";
DateTime paramDate = new(2024, 04, 25) ;
double[] curve = { 0.04626, 0.04511, 0.04472, 0.044595, 0.0446379, 0.04479, 0.045035, 0.04529, 0.0455901, 0.0458856, 0.0471141, 0.04758 };// Exemple de taux de courbe
string[] curveMaturity = { "1Y", "2Y", "3Y", "4Y", "5Y", "6Y", "7Y", "8Y", "9Y", "10Y", "15Y", "20Y" };  // Exemple de maturités de courbe
int swapPeriod = 12;
int swapBasis = 4;
double fxSpot = 1;
StrippingIRS.StripZC(paramDate, curveName, curve, curveMaturity, swapPeriod, swapBasis, fxSpot);

int cdsID = 1;
string CDSName = "ABNAMRO_MMR.EUR.SU";
//DateTime ParamDate = new(2024, 03, 03);
DateTime CDSRollDate = StrippingCDS.CDSRefDate(paramDate);
double[] CDSCurve = { 0, 0.00133, 0.002, 0.0026, 0.00316, 0, 0.004, 0.0044, 0.0048 };
//bool vbaMontlyZC = StrippingIRS.VbaComputeMonthlyRiskyZC(curveName, paramDate, CDSRollDate);

//Console.WriteLine("id vbaMontlyZC = " + vbaMontlyZC);


string[] CurveMaturity = { "3M", "6M", "1Y", "2Y", "3Y", "4Y", "5Y", "7Y", "10Y" };
string CDSCurrency = "EUR";
double RecoveryRate = 0.4; // Par exemple, 40%
bool alterMode = false;
string intensity = "Curvepoint" ;//"3M"; // Vous pouvez ajuster cette valeur en fonction de vos besoins


// Appel de la fonction à tester

double[] result = StrippingCDS.StripDefaultProbability(cdsID, CDSName, paramDate, CDSRollDate, CDSCurve, CurveMaturity, CDSCurrency, RecoveryRate, alterMode, intensity);

Console.WriteLine("name = " + StrippingCDS.CreditDefaultSwapCurves.Curves[0].CDSName);
Console.WriteLine("name = "+StrippingCDS.CreditDefaultSwapCurves.Curves[1].CDSName);


// Vérification du résultat
if (result != null)
{
    Console.WriteLine("Résultat de la fonction StripDefaultProbability : ");
    for (int i = 0; i < result.Length; i++)
    {
        Console.WriteLine(result[i]);
    }
}
else
{
    Console.WriteLine("La fonction StripDefaultProbability a renvoyé null. Vérifiez la console pour les détails d'erreur.");
}
string getname = StrippingCDS.GetCDSName(cdsID);
//string getname2 = StrippingCDS.GetCDSName(CDSName);

Console.WriteLine("getname = " + getname);
//Console.WriteLine("getname = " + getname2);
Console.WriteLine("GETDPRO = " + StrippingCDS.GetDefaultProb(cdsID, "1M"));

//Console.WriteLine("GETDPRO2 = " + StrippingCDS.GetDefaultProb(getname, "1M"));

//Console.WriteLine("===================End Stripping CDS====================");



//Console.WriteLine("=================== Start Model Interface ====================");

//string issuerId = "ABNAMRO_MMR.EUR.SU";
//string maturity = "5Y";
//double spread = 0.01;
//double recoveryRate = 0.4;
//string pricingCurrency = "EUR";
//double fxCorrel = 0.0;
//double fxVol = 0.0;

//string cpnPeriod = "3M";
//string cpnConvention = "LongFirst";
//string cpnLastSettle = "";

//double isAmericanFloatLeg = 1; // 1 = true;
//double isAmericanFixedLeg = 1; // 1 = true;
//double withGreeks = 1; // 1 = true;

//string integrationPeriod = "1m";
//double probMultiplier = 1;

//double[] hedgingCds = {0 , 1, 1 }; // 1 = true


//String[,] result3 = ModelInterface.CDS(issuerId, maturity, spread, recoveryRate, cpnPeriod, cpnConvention, cpnLastSettle, pricingCurrency,
// fxCorrel, fxVol, isAmericanFloatLeg, isAmericanFixedLeg, withGreeks, hedgingCds, integrationPeriod, probMultiplier);



//// Affichage du résultat
//if (result3 != null)
//{
//    Console.WriteLine("Résultat de la fonction StripDefaultProbability : ");
//    int rows = result3.GetLength(0) - 1;
//    int columns = result3.GetLength(1);

//    for (int i = 0; i < rows; i++)
//    {
//        Console.Write($"{i} | \t\t");

//        for (int j = 0; j < columns; j++)
//        {
//            if (j + 1 == columns)
//            {
//                Console.Write($"{result3[i, j]}\t");
//            }
//            else
//            {
//                if (i == rows - 2)
//                {
//                    Console.Write($"{result3[i, j]}\t\t\t -> \t");
//                }
//                else
//                {
//                    Console.Write($"{result3[i, j]}\t -> \t");
//                }                
//            }

//        }
//        Console.WriteLine("\n______________________________________________________________________________________________________________________________________________");
//    }
//}
//else
//{
//    Console.WriteLine("La fonction StripDefaultProbability a renvoyé null. Vérifiez la console pour les détails d'erreur.");
//}

//Console.WriteLine("=================== End Model Interface ====================");


//Console.WriteLine("===================Test CDO Model====================");

//int numberOfIssuer = 5;
//double[] defaultProbKnowingFactor = { 0.1, 0.2, 0.3, 0.4, 0.5 }; // Exemple de probabilités par défaut
//int? maxRequest = 3; // Vous pouvez ajuster cette valeur en fonction de vos besoins
//bool withGreeks = false; // Vous pouvez ajuster cette valeur en fonction de vos besoins

//// Appel de la fonction à tester
//double[,] result2 = CDOModel.Recursion(numberOfIssuer, defaultProbKnowingFactor, maxRequest, withGreeks);

//// Vérification du résultat
//if (result2 != null)
//{
//    Console.WriteLine("Résultat de la fonction Recursion : ");
//    for (int i = 0; i < result2.GetLength(0); i++)
//    {
//        for (int j = 0; j < result2.GetLength(1); j++)
//        {
//            Console.Write($"{result2[i, j]} ");
//        }
//        Console.WriteLine();
//    }
//}
//else
//{
//    Console.WriteLine("La fonction Recursion a renvoyé null. Vérifiez la console pour les détails d'erreur.");
//}
//====================================================================================================================================================================
//int numberOfIssuer = 2;
//double[] defaultProbKnowingfFactor = { 0.1, 0.2, 0.3 }; // Exemple de probabilités par défaut
//int? maxRequest = 2; // Vous pouvez ajuster cette valeur en fonction de vos besoins
//bool withGreeks = false; // Vous pouvez ajuster cette valeur en fonction de vos besoins
//double[] lossUnitIssuer = { 1, 2, 3 };
//int[] cumulLossUnitIssuer = { 1, 2, 3 };

//// Appel de la fonction à tester
//double[,] result3 = CDOModel.RecursionLossUnit(numberOfIssuer, defaultProbKnowingfFactor, lossUnitIssuer, cumulLossUnitIssuer, maxRequest, withGreeks);

//// Vérification du résultat
//if (result3 != null)
//{
//    Console.WriteLine("Résultat de la fonction GetDefaultDistribution : ");
//    for (int i = 0; i < result3.GetLength(0); i++)
//    {
//        for (int j = 0; j < result3.GetLength(1); j++)
//        {
//            Console.Write($"{result3[i, j]} ");
//        }
//        Console.WriteLine();
//    }
//}
//else
//{
//    Console.WriteLine("La fonction GetDefaultDistribution a renvoyé null. Vérifiez la console pour les détails d'erreur.");
//}

/////============================================================================================
int numberOfIssuer = 1;

double[] defaultProbability = { 0.1, 0.2, 0.3, 0 }; // Exemple de probabilités par défaut
int[] lossUnitIssuer = { 1, 2, 3 }; // Exemple de pertes unitaires par émetteur
int[] cumulLossUnitIssuer = { 1, 3, 6, 0 }; // Exemple de cumul des pertes unitaires par émetteur
double[] betaVector = { 0.1, 0.2, 0.3, 0 }; // Exemple de vecteur bêta
int? maxRequest = 2; // Vous pouvez ajuster cette valeur en fonction de vos besoins
double[] inputThreshold = null; // Exemple de seuils d'entrée
int? factorIndex = null; // Vous pouvez ajuster cette valeur en fonction de vos besoins
bool withGreeks = false; // Vous pouvez ajuster cette valeur en fonction de vos besoins
double dBeta = 0.1; // Vous pouvez ajuster cette valeur en fonction de vos besoins


// Appel de la fonction à tester
double[,] results = CDOModel.GetDefaultDistributionLossUnit(numberOfIssuer, defaultProbability, lossUnitIssuer, cumulLossUnitIssuer,
    betaVector, maxRequest, inputThreshold, factorIndex, withGreeks, dBeta);
Console.WriteLine("résultat cdf :"+ UtilityBiNormal.NormalCumulativeDistribution(1));
// Vérification du résultat
if (result != null)
{
    Console.WriteLine("Résultat de la fonction GetDefaultDistributionLossUnit : ");
    for (int i = 0; i < results.GetLength(0); i++)
    {
        for (int j = 0; j < results.GetLength(1); j++)
        {
            Console.Write($"resultat + {results[i, j]} ");
        }
        Console.WriteLine();
    }
}
else
{
    Console.WriteLine("La fonction GetDefaultDistributionLossUnit a renvoyé null. Vérifiez la console pour les détails d'erreur.");
}

////////////================================================================================================

//int numberOfIssuer2 = 3;
//double lossUnitAmount = 100;
//double[] strikes = { 0.1, 0.4, 0.6 };
//double[] defaultProbability2 = { 0.01, 0.02, 0.03 };
//double correl = 0.005;
//double[] betaAdder = { 0.1, 0.2, 0.3 };
//double zC = 1;
//double[] nominalIssuer = { 10000, 15000, 20000.0 };
//double[] recoveryIssuer = { 0.4, 0.3, 0.2 };
//bool withGreeks2 = false;
//double dBeta2 = 0.1;

//// Appeler la fonction
//var result2 = CDOModel.EuropeanCDOLossUnit(numberOfIssuer2, lossUnitAmount, strikes, defaultProbability2, correl,
//    betaAdder, zC, nominalIssuer, recoveryIssuer, withGreeks2, dBeta2);

//// Afficher les résultats
//Console.WriteLine("Results:");

//if (result2 != null)
//{
//    for (int i = 0; i < result2.GetLength(0); i++)
//    {
//        for (int j = 0; j < result2.GetLength(1); j++)
//        {
//            Console.Write(result2[i, j] + "\t");
//        }
//        Console.WriteLine();
//    }

//}
//else
//{
//    Console.WriteLine("La fonction GetDefaultDistributionLossUnit a renvoyé null. Vérifiez la console pour les détails d'erreur.");
//}
//Console.WriteLine("===================End CDO Model====================");




//test function 
//UDF test = new UDF();
//double exemple = test.Getadd(4, 8);
//Console.WriteLine(exemple);
//Console.WriteLine(test.GetWelcome());

//string curveName1 = "EUR";
//DateTime paramDate1 = new(2024, 03, 1);
//double[] curve1 = { 0.04626, 0.04511, 0.04472, 0.044595, 0.0446379, 0.04479, 0.045035, 0.04529, 0.0455901, 0.0458856, 0.0471141, 0.04758 };// Exemple de taux de courbe
//double[] curve2 = { 0.00978, 0.01034, 0.01104, 0.01207, 0.01309, 0.0141, 0.01509, 0.01604, 0.01698, 0.01787, 0.0212375, 0.0233875 };//JPY
//double[] curve3 = { 0.045525, 0.04448, 0.045115, 0.046085, 0.047005, 0.0478, 0.048495, 0.049075, 0.04961, 0.05008, 0.051595, 0.052305 };//USD

//string[] curveMaturity1 = { "1Y", "2Y", "3Y", "4Y", "5Y", "6Y", "7Y", "8Y", "9Y", "10Y", "15Y", "20Y" };  // Exemple de maturités de courbe
//int swapPeriod1 = 12;
//int swapBasis1 = 4;
//double fxSpot1 = 1;
//double[] a = StrippingIRS.StripZC(paramDate1, curveName1, curve1, curveMaturity1, swapPeriod1, swapBasis1, fxSpot1);
//string[] bond =new string[360];
//string[] irs = new string[360];
//double value;
//DateTime maturityDateX;
//double t;
//for (int i = 1; i <=360; i++)
//{
//    maturityDateX = UtilityDates.ConvertDate(paramDate, i+"m");
//    t = UtilityDates.DurationYear(maturityDateX, paramDate);
//    value = StrippingIRS.VbaGetRiskFreeZC(paramDate1, i + "m", a, curveMaturity1);
//    bond[i-1]= value.ToString().Replace(',','.');
//    irs[i-1]= (-Math.Log(value) / t).ToString().Replace(',','.');
//}
//for( int i = 1;i <= 360; i++)
//{
//    Console.Write(bond[i-1]+",");
//}
//Console.WriteLine(bond);
//Console.WriteLine("-----------------------------------------------");
//Console.WriteLine(irs);



//DateTime parameterDate = new DateTime(2024, 4, 25);
//DateTime cdsRollDate = new DateTime(2024, 6, 20);
//int[] issuer = { 1, 2, 3 };
//string[] spreadCurveMaturity = { "3m", "6m", "1Y", "2Y", "3Y", "4Y", "5Y", "7Y", "10Y" };
//double[,] CDScurve = {{ 0, 0.13, 0.20, 0.26, 0.32, 0, 0.400, 0.44, 0.48 },{ 0, 0.08, 0.11, 0.16, 0.23, 0, 0.36, 0.43, 0.53 },{ 0, 0.06, 0.10, 0.18, 0.27, 0, 0.40, 0.51, 0.62 } };
//double[] c1 = { 0.0, 0.00133, 0.0020, 0.0026 , 0.00316   ,0.0 , 0.004,   0.0044,  0.0048};
//double[] c2 = { 0.0, 0.0008, 0.0011, 0.0016, 0.0023, 0.0, 0.0036, 0.0043, 0.0053 };
//double[] c3 = { 0.0, 0.0006, 0.0010, 0.0018, 0.0027, 0.0, 0.0040, 0.0051, 0.0062 };

//string maturity = "5Y";
//double[] strikes = { 0.0, 3.0 };
//double[] correl = { 0.4, 0.5 };
//string pricingCurrency = "EUR";
//int numberOfIssuer = 3;
//string[] issuerList = { "ABNAMRO_MMR.EUR.SU","Accor_MMR.EUR.SU","Adecco_MMR.EUR.SU"};
//double[] nominalIssuer = { 1.0, 1.0, 1.0};
//double spread = 0.05;
//string cpnPeriod = "3m";
//string cpnConvention = "LongFirst";
//string cpnLastSettle = "";
//double fxCorrel = -0.05;
//double fxVol = 0.1;
//double[] betaAdder = {0.0,0.0,0.0 };
//double[] recoveryIssuer = { 0.4, 0.4, 0.4 };
//double isAmericanFloatLeg = -1;
//double isAmericanFixedLeg = -1;
//double withGreeks = -1;
//double[] hedgingCDS = {0.0,-1.0,-1.0};
//double lossUnitAmount = 0.0;
//string integrationPeriod = "1m";
//double dBeta = 0.01;

////double[] test = StrippingCDS.StripDefaultProbability(1, issuerList[0], parameterDate, cdsRollDate, c1, CurveMaturity, pricingCurrency, 0.4,false,"Curve points");
//double[] test1 = StrippingCDS.StripDefaultProbability(2, issuerList[1], parameterDate, cdsRollDate, c2, CurveMaturity, pricingCurrency, 0.4, false, intensity);
//double[] test2 = StrippingCDS.StripDefaultProbability(3, issuerList[2], parameterDate, cdsRollDate, c3, CurveMaturity, pricingCurrency, 0.4, false, intensity);

//int u = StrippingIRS.GetCurveId(pricingCurrency);
//Console.WriteLine(u);
//Console.WriteLine(StrippingCDS.GetCDSCurveId(issuerList[0]));
//string[,] cdot = ModelInterface.CDO(maturity, strikes, correl, pricingCurrency, 3, issuerList, nominalIssuer,
//    spread, cpnPeriod, cpnConvention, cpnLastSettle, fxCorrel, fxVol, betaAdder, recoveryIssuer, isAmericanFloatLeg,
//    isAmericanFixedLeg, withGreeks, hedgingCDS, lossUnitAmount, integrationPeriod, 1, dBeta);