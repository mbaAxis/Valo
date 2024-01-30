using ValoLibrary;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("================== Lancement des tests ================");


Console.WriteLine("================== Test BlackScholes.Price ============");
double price = BlackScholes.Price('c', 100, 0.2, 0.05, 100, 1);
Console.WriteLine(price);
Console.WriteLine("================== En Test BlackScholes.Price =========");

Console.WriteLine("================== Test Calibration ============");
double T = 3;
int[] exe = Calibration.PosMaturitiesToInterpol(T);
int[] exe2 = Calibration.PosStrikesToInterpol(120);
Console.WriteLine("exe est:");
for (int k = 0; k < exe.Length; k++)
{
    Console.WriteLine(exe[k]);
}

Console.WriteLine("Ks est:");
for (int i = 0; i < exe2.Length; i++)
{
    Console.WriteLine(exe2[i] + " et la taille: " + exe2.Length);
}

double r = Calibration.GetRepo("FTSE", T);
Console.WriteLine("Repo est:");
Console.WriteLine(r);

Console.WriteLine("================== End Test Calibration =========");

Console.WriteLine("================== Test Get Data =========");

// Supposez que votre fichier Excel est situé à _filePath et contient des données pour l'actif "FTSE"
string underlying = "CAC40";
var data = GetData.Data(underlying);

// Affichage des données
Console.WriteLine($"Prices Matrix for {underlying}:");
PrintArray(data["prices"]);

Console.WriteLine($"Repo Rates for {underlying}:");
PrintArray(data["repoRates"]);

Console.WriteLine($"Dividends for {underlying}:");
PrintArray(data["dividends"]);

Console.WriteLine($"Maturities for {underlying}:");
PrintArray(data["maturities"]);

Console.WriteLine($"Strikes for {underlying}:");
PrintArray(data["strikes"]);

static void PrintArray(Array array)
{
for (int i = 1; i <= array.GetLength(0); i++)
{
    for (int j = 1; j <= array.GetLength(1); j++)
    {
        Console.Write($"{array.GetValue(i, j)}\t");
    }
    Console.WriteLine();
}
Console.WriteLine();
}
Console.WriteLine("================== End Test Get Data =========");

//Console.WriteLine("================== Test UDF.CallPrice ============");
//UDF exemple = new();
//double price2 = exemple.CallPrice(100, 1, "FTSE");
//Console.WriteLine(price2);
//Console.WriteLine("================== End Test UDF.CallPrice =========");
