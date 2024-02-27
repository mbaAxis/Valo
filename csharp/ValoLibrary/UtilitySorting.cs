using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Excel = Microsoft.Office.Interop.Excel;


namespace ValoLibrary
{
    public class UtitilySorting
    {
        public static string ChangeDatas(ref int[,] table, int low, int up, int size)
        {
            int inter;
            for (int j = 0; j < size; j++)
            {
                inter = table[low, j];
                table[low, j] = table[up, j];
                table[up, j] = inter;
            }
            return "OK";
        }

        public static string DuplicateDatas(int[,] dataIn, int i, int[] dataOut, int n)
        {
            for (int j = 0; j < n; j++)
            {
                dataOut[j] = dataIn[i, j];
            }
            return "OK";
        }

        public static string DataSort(int[,] data, int inf, int sup, int size)
        {
            int low = inf;
            int up = sup;
            int mid;
            int[] temporaryData = new int[size];
            int[] dataBuffer = new int[size];

            if (sup <= inf) return "OK";
            mid = (inf + sup) / 2;

            DuplicateDatas(data, mid, temporaryData, size);

            while (low <= up)
            {
                while (data[low, 0] < temporaryData[0])
                {
                    low++;
                    if (low == sup) break;
                }

                while (temporaryData[0] < data[up, 0])
                {
                    up--;
                    if (up == inf) break;
                }

                if (low <= up)
                {
                    ChangeDatas(ref data, low, up, size);
                    low++;
                    up--;
                }
            }

            if (inf < up)
            {
                DataSort(data, inf, up, size);
            }

            if (low < sup)
            {
                DataSort(data, low, sup, size);
            }

            return "OK";
        }

        public static int[,] TemporarySortData(int[,] data)
        {
            int[,] table = new int[12, 2];
            for (int i = 0; i < 12; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    table[i, j] = data[i, j];
                }
            }

            DataSort(table, 0, data.GetLength(0) - 1, 2);

            return table;
        }
    }
}
