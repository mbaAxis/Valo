using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Office.Interop.Excel;

namespace ValoLibrary
{
    public class Utils
    {
        public static void SelectionCalculate()
        {
            // Récupérer l'application Excel en cours d'exécution
            Application excelApp = (Application) System.Runtime.InteropServices.Marshal.GetActiveObject("Excel.Application");

            // Calculer la sélection en cours
            excelApp.Selection.Calculate();
        }

        public static void CurrentRegionCalculate()
        {
            // Récupérer l'application Excel en cours d'exécution
            Application excelApp = (Application)System.Runtime.InteropServices.Marshal.GetActiveObject("Excel.Application");

            // Enregistrer la sélection en cours
            Range currentSelection = excelApp.Selection;

            // Sélectionner la région actuelle
            currentSelection.CurrentRegion.Select();

            // Calculer la sélection
            excelApp.Selection.Calculate();

            // Re-sélectionner la sélection d'origine
            currentSelection.Select();
        }

        public static bool IsNumeric(object value)
        {
            return value is int || value is double || value is float || value is long || value is short || value is byte;
        }

    }
}
