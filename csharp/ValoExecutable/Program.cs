using System;
using ValoLibrary; // Ajoutez cette ligne pour utiliser les classes de la bibliothèque.

class Program
{
    static void Main()
    {
        // Utilisez les classes de la bibliothèque.
        //BlackScholes example = new BlackScholes();
        double price = BlackScholes.Price('c', "FTSE", 30, 0.5, 100, 44624);
        Console.WriteLine("hello Word");
        Console.WriteLine(price);
        Console.WriteLine("toto");
        Console.ReadKey();
    }
}