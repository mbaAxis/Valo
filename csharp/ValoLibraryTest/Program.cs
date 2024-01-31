using ValoLibrary;

// See https://aka.ms/new-console-template for more information
double T = 44989;
double strike = 100;
string underlying = "CAC40";
double r = Calibration.GetRepo(underlying, T);
double d = Calibration.GetDiv(underlying, T);
double vol = BlackScholes.ImpliedVol("c", 100, 53.765, 0.97, strike, T);
//double vol2 = BlackScholes.CalculateCallOptionImpliedVolatility("c", 100, strike, T, 0.97, 53.765) ;

double inter = Calibration.interpolatePrice(strike, T, "FTSE");
Console.WriteLine("interpolate price is: = " + inter);
Console.WriteLine("Repo est:");
Console.WriteLine("div 5 ans est:" + d);
Console.WriteLine("repo 5 ans est:" + r);
Console.WriteLine("vol est:" + vol);
//Console.WriteLine("vol2 est:" + vol2);

Console.WriteLine("================== Test UDF.CallPrice ============");
UDF exemple = new();
double price2 = exemple.CallPrice(strike, T, underlying);
Console.WriteLine(price2);
Console.WriteLine("================== End Test UDF.CallPrice =========");