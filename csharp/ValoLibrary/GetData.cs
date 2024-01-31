using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Office.Interop.Excel;
using _Excel = Microsoft.Office.Interop.Excel;

namespace ValoLibrary
{
    public class GetData
    {
        //public static Dictionary<string, Array> DATA = Data("FTSE");
        public static string _filePath = @"C:\Users\l.baduet\OneDrive - AXIS ALTERNATIVES\Documents\mon pricing\projet\Valo\common\MyInputs.xlsm";
        
        
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
        public static Dictionary<string, Array> Data(string underlying)
        {
            int sheet = 1;
            _Application excel = new _Excel.Application();
            var workbooks = excel.Workbooks;
            var wb = workbooks.Open(_filePath);
            Worksheet ws = (Worksheet)wb.Worksheets[sheet];
            var used_range = ws.UsedRange;
            var reference = used_range.Find(underlying);
            var pricesMatrix = ws.Range[reference.Offset[3, 1], reference.Offset[18, 15]].Cells.Value2;
            var repoRates = ws.Range[reference.Offset[2, 18], reference.Offset[18, 19]].Cells.Value2;
            var dividends = ws.Range[reference.Offset[2, 21], reference.Offset[18, 22]].Cells.Value2;

            //var spot = ws.Range[reference.Offset[1, 0], reference.Offset[1, 0]].Cells.Value2;
            //var date = ws.Range[reference.Offset[-1, 0], reference.Offset[0, -1]].Cells.Value2;

            var maturities = ws.Range[reference.Offset[3, 0], reference.Offset[18, 0]].Cells.Value2;
            var strikes = ws.Range[reference.Offset[2, 1], reference.Offset[2, 16]].Cells.Value2;

            //ReleaseMemory( ref excel, ref wb, ref ws, ref used_range);

            var data = new Dictionary<string, Array>();
            data["prices"] = (Array)pricesMatrix;
            data["repoRates"] = (Array)repoRates;
            data["dividends"] = (Array)dividends;

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
