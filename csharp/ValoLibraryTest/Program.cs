using ValoLibrary;

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

//DateTime paramDate2 = DateTime.Now;
//string period = "2y";
//double monthPeriod = UtilityDates.MonthPeriod(period, paramDate2);
//Console.WriteLine($"monthPeriod: {monthPeriod}");

//string maturityDate = "2Y";
//DateTime convertedDate = UtilityDates.ConvertDate(paramDate2, maturityDate);
//Console.WriteLine($"convertedDate: {convertedDate}");

//DateTime startDate = new (2020, 1, 1);
//DateTime endDate = new (2024, 1, 1);
//double duration = UtilityDates.DurationYear(endDate, startDate);
//Console.WriteLine($"Durée en années : {duration}");

//DateTime paramDate2 = new (2022, 1, 1);
//string maturity = "1Y";
//DateTime cpnLastSettle = new (2022, 3, 30);
//string cpnPeriod = "3M";
//string cpnConvention = "LongFirst";

//DateTime[] schedule = UtilityDates.SwapSchedule(paramDate2, maturity, cpnLastSettle, cpnPeriod, cpnConvention);
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

//DateTime paramDate = new(2024,2, 28);
//string curveName = "JPY";
//double[] curve = {0.00978, 0.01034, 0.01104, 0.01207, 0.01309, 0.0141, 0.01509, 0.01604, 0.01698, 0.01787, 0.0212375, 0.0233875};  // Exemple de taux de courbe
//string[] curveMaturity = { "1Y", "2Y", "3Y", "4Y" , "5Y" , "6Y", "7Y" , "8Y", "9Y" , "10Y", "15Y" , "20Y"};  // Exemple de maturités de courbe
//int swapPeriod = 6;
//int swapBasis = 1;
//double fxSpot = 165.5616;

//double[] result = StrippingIRS.StripZC(paramDate, curveName, curve, curveMaturity, swapPeriod, swapBasis, fxSpot);

//// Affichage des résultats
//Console.WriteLine($"Stripped ZC for curve {curveName}:");

//if (result != null)
//{
//    // Affichage des résultats
//    Console.WriteLine($"Stripped ZC for curve {curveName}:");

//    for (int i = 0; i < result.Length; i++)
//    {
//        Console.WriteLine($"{result[i]}");
//    }
//}
//else
//{
//    Console.WriteLine("Erreur lors du calcul de Stripped ZC.");
//}

//Console.WriteLine("===================End Stripping IRS====================");


//Console.WriteLine("===================Test Stripping CDS====================");


int cdsID = 1;
string CDSName = "EUR";
DateTime ParamDate = DateTime.Now;
DateTime CDSRollDate = StrippingCDS.CDSRefDate(ParamDate);
double[] CDSCurve = { 0, 0.00133, 0.002, 0.0026, 0.00316, 0, 0.004, 0.0044, 0, 0048 };
string[] CurveMaturity = { "3M", "6M", "1Y", "2Y", "3Y", "4Y", "5Y", "7Y", "10Y" };
string CDSCurrency = "EUR";
double RecoveryRate = 0.4; // Par exemple, 40%
bool alterMode = false;
string intensity = "3M"; // Vous pouvez ajuster cette valeur en fonction de vos besoins

// Appel de la fonction à tester
double[] result = StrippingCDS.StripDefaultProbability(cdsID, CDSName, ParamDate, CDSRollDate, CDSCurve, CurveMaturity, CDSCurrency, RecoveryRate, alterMode, intensity);

// Vérification du résultat
if (result != null)
{
    Console.WriteLine("Résultat de la fonction StripDefaultProbability : ");
    for (int i = 0; i < result.Length; i++)
    {
        Console.WriteLine($"Result[{i}] = {result[i]}");
    }
}
else
{
    Console.WriteLine("La fonction StripDefaultProbability a renvoyé null. Vérifiez la console pour les détails d'erreur.");
}

Console.WriteLine("===================End Stripping CDS====================");


//Console.WriteLine("===================Test CDO MOdel====================");
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
//int numberOfIssuer = 1;

//double[] defaultProbability = { 0.1, 0.2, 0.3, 0 }; // Exemple de probabilités par défaut
//int[] lossUnitIssuer = { 1, 2, 3 }; // Exemple de pertes unitaires par émetteur
//int[] cumulLossUnitIssuer = { 1, 3, 6, 0 }; // Exemple de cumul des pertes unitaires par émetteur
//double[] betaVector = { 0.1, 0.2, 0.3, 0 }; // Exemple de vecteur bêta
//int? maxRequest = 2; // Vous pouvez ajuster cette valeur en fonction de vos besoins
//double[] inputThreshold = null; // Exemple de seuils d'entrée
//int? factorIndex = null; // Vous pouvez ajuster cette valeur en fonction de vos besoins
//bool withGreeks = false; // Vous pouvez ajuster cette valeur en fonction de vos besoins
//double dBeta = 0.1; // Vous pouvez ajuster cette valeur en fonction de vos besoins


//// Appel de la fonction à tester
//double[,] result = CDOModel.GetDefaultDistributionLossUnit(numberOfIssuer, defaultProbability, lossUnitIssuer, cumulLossUnitIssuer,
//    betaVector, maxRequest, inputThreshold, factorIndex, withGreeks, dBeta);

//// Vérification du résultat
//if (result != null)
//{
//    Console.WriteLine("Résultat de la fonction GetDefaultDistributionLossUnit : ");
//    for (int i = 0; i < result.GetLength(0); i++)
//    {
//        for (int j = 0; j < result.GetLength(1); j++)
//        {
//            Console.Write($"{result[i, j]} ");
//        }
//        Console.WriteLine();
//    }
//}
//else
//{
//    Console.WriteLine("La fonction GetDefaultDistributionLossUnit a renvoyé null. Vérifiez la console pour les détails d'erreur.");
//}

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
//   Console.WriteLine("La fonction GetDefaultDistributionLossUnit a renvoyé null. Vérifiez la console pour les détails d'erreur.");
//}
//Console.WriteLine("===================End CDO Model====================");

