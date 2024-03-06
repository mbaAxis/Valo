using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Office.Interop.Excel;
using _Excel = Microsoft.Office.Interop.Excel;

namespace ValoLibrary
{
    public class GetData
    {

        public static string _filePath = @"C:\Users\l.baduet\OneDrive - AXIS ALTERNATIVES\Documents\mon pricing\projet\Valo\common\MyInputs.xlsm";

        //public static string _filePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), @"..\..\..\..\..\..\Valo\common\MyInputs.xlsm"));
        public static object Cells { get; private set; }

        // To release Excel from RAM in order to be able to charge the data from excel.
        public static void ReleaseMemory(ref _Application excel, ref Workbook wb, ref Worksheet ws,
            ref _Excel.Range usedRange)
        {
            if (excel is null)
            {
                throw new ArgumentNullException(nameof(excel));
            }

            if (wb is null)
            {
                throw new ArgumentNullException(nameof(wb));
            }

            if (ws is null)
            {
                throw new ArgumentNullException(nameof(ws));
            }

            if (usedRange is null)
            {
                throw new ArgumentNullException(nameof(usedRange));
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            Marshal.ReleaseComObject(usedRange);
            Marshal.ReleaseComObject(ws);
            wb.Close();
            Marshal.ReleaseComObject(wb);
            excel.Quit();
            Marshal.ReleaseComObject(excel);
        }

        private const int rowsPM1 = 3;
        private const int columnsPM1 = 1;
        private const int rowsPM2 = 18;
        private const int columnsPM2 = 15;


        private const int rowsRFR1 = 2;
        private const int columnsRFR1 = 18;
        private const int rowsRFR2 = 18;
        private const int columnsRFR2 = 19;

        private const int rowsD1 = 2;
        private const int columnsD1 = 21;
        private const int rowsD2 = 18;
        private const int columnsD2 = 22;

        private const int rowsM1 = 3;
        private const int columnsM1 = 0;
        private const int rowsM2 = 18;
        private const int columnsM2 = 0;

        private const int rowsK1 = 2;
        private const int columnsK1 = 1;
        private const int rowsK2 = 2;
        private const int columnsK2 = 16;

        public static Dictionary<string, Array> Data(string underlying)
        {
            int sheet = 1;
            _Application excel = new _Excel.Application();
            var workbooks = excel.Workbooks;
            var wb = workbooks.Open(_filePath);
            Worksheet ws = (Worksheet)wb.Worksheets[sheet];
            var used_range = ws.UsedRange;
            var reference = used_range.Find(underlying);
            var pricesMatrix = ws.Range[reference.Offset[rowsPM1, columnsPM1], reference.Offset[rowsPM2, columnsPM2]].Cells.Value2;
            var riskFreeRates = ws.Range[reference.Offset[rowsRFR1, columnsRFR1], reference.Offset[rowsRFR2, columnsRFR2]].Cells.Value2;
            var dividends = ws.Range[reference.Offset[rowsD1, columnsD1], reference.Offset[rowsD2, columnsD2]].Cells.Value2;
            var maturities = ws.Range[reference.Offset[rowsM1, columnsM1], reference.Offset[rowsM2, columnsM2]].Cells.Value2;
            var strikes = ws.Range[reference.Offset[rowsK1, columnsK1], reference.Offset[rowsK2, columnsK2]].Cells.Value2;

            var data = new Dictionary<string, Array>();
            data["prices"] = (Array)pricesMatrix;
            data["riskFreeRates"] = (Array)riskFreeRates;
            data["dividends"] = (Array)dividends;


            //var spot = ws.Range[reference.Offset[1, 0], reference.Offset[1, 0]].Cells.Value2;
            //var date = ws.Range[reference.Offset[-1, 0], reference.Offset[0, -1]].Cells.Value2;
            //data["spot"] = spot;
            //data["date"] = date;

            data["maturities"] = (Array)maturities;
            data["strikes"] = (Array)strikes;
            ReleaseMemory(ref excel, ref wb, ref ws, ref used_range);
            return data;
        }
        public static double GetSpot(string underlying)
        {
            // further investigate why commented code generate error
            if (underlying == "CAC40") { return 100; }
            else if (underlying == "FTSE") { return 150; }
            else if (underlying == "SP500") { return 125; }
            else return -1;

            //int sheet = 1;
            //_Application excel = new _Excel.Application();
            //var workbooks = excel.Workbooks;
            //var wb = workbooks.Open(_filePath);
            //Worksheet ws = wb.Worksheets[sheet];
            //var used_range = ws.UsedRange;
            //var reference = used_range.Find(underlying);
            //double spot = ws.Range[reference.Offset[0, 1]].Cells.Value2;
            //ReleaseMemory(ref excel, ref wb, ref ws, ref used_range);
            //return spot;
        }
        public static double GetTime(string underlying)
        {
            // to further investigate: commented code generate error
            return 44624;
            //int sheet = 1;
            //_Application excel = new _Excel.Application();
            //var workbooks = excel.Workbooks;
            //var wb = workbooks.Open(_filePath);
            //Worksheet ws = wb.Worksheets[sheet];
            //var used_range = ws.UsedRange;
            //var reference = used_range.Find(underlying);
            //double date = ws.Range[reference.Offset[-1, 1]].Cells.Value2;
            //ReleaseMemory(ref excel, ref wb, ref ws, ref used_range);
            //return date;
        }
    }
}
