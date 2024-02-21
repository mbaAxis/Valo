using ValoLibrary;
using static ValoLibrary.UDF;


//double T = 44989;
//double strike = 100;
//string underlying = "CAC40";
//double r = 0.97;
//double r = Calibration.GetRepo(underlying, T);
//double d = Calibration.GetDiv(underlying, T);
//double vol = BlackScholes.ImpliedVol("c", 100, 53.765, r, strike, T);

//double inter = Calibration.interpolatePrice(strike, T, "FTSE");
//Console.WriteLine("interpolate price is: = " + inter);
//Console.WriteLine("Repo est:");
//Console.WriteLine("div 5 ans est:" + d);
//Console.WriteLine("repo 5 ans est:" + r);
//Console.WriteLine("vol est:" + vol);
////Console.WriteLine("vol2 est:" + vol2);
///
//Console.WriteLine("================== Test UDF.CallPrice ============");
//UDF exemple = new();
//double price2 = exemple.CallPrice(strike, T, underlying);
//Console.WriteLine(price2);
//Console.WriteLine("================== End Test UDF.CallPrice =========");



//=========================================================================================================
double T = 1;
double strike = 100;
double r = 0.97;
UDF exemple = new();

Dictionary<string, double> sensitivities = exemple.SensitivitiesBS("c", 100, 0.2, r, strike, T);
foreach (var kvp in sensitivities)
{
    Console.WriteLine($"{kvp.Key}: {kvp.Value}");
}


Console.WriteLine("next");
double T1 = 44989;
Dictionary<string, double> sensi = exemple.Sensitivities("c", strike, T1, "CAC40");
foreach (var kvp in sensi)
{
    Console.WriteLine($"{kvp.Key}: {kvp.Value}");
}

////// option portefolio
/////
//List<OptionParameters> optionPortfolio = new List<OptionParameters>
//{
//    new OptionParameters { Underlying = "CAC40", Strike = 100, Expiry = T, OptionType = "C" },
//    new OptionParameters { Underlying = "CAC40", Strike = 100, Expiry = T, OptionType = "P" },
//    // Ajoutez d'autres options au besoin
//};

//// Appelez la fonction pour obtenir le prix total du portefeuille
//double portfolioPrice = UDF.PortfolioOptionPrice(optionPortfolio);

//// Faites quelque chose avec le prix total du portefeuille
//Console.WriteLine("Prix total du portefeuille : " + portfolioPrice);