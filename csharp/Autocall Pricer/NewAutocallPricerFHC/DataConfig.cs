using System.Collections.Generic;

public static class DataConfig
{

    public static readonly Dictionary<string, string> NamesTickers = new Dictionary<string, string>
    {
        { "AXA_15_05_2024", "CS.PA" },
        { "AXA_25_04_2024", "CS.PA" },
        { "AIRBUS_25_04_2024", "AIR.PA" },
        { "BNP Paribas_25_04_2024", "BNP.PA" },
        { "CAPGEMINI_25_04_2024", "CAP.PA" },
        { "ENGIE_25_04_2024", "ENGI.PA" },
        { "LVMH_25_04_2024", "MC.PA" },
        { "ORANGE_25_04_2024", "ORA.PA" }
    };

    public static readonly string[] Paths = new string[]
    {
        @"C:\Users\m.ben-el-ghoul\PycharmProjects\Autocall pricer V2\data and keys\AXA_25_04_2024.xlsx",
        @"C:\Users\m.ben-el-ghoul\PycharmProjects\Autocall pricer V2\data and keys\AIRBUS_25_04_2024.xlsx",
        @"C:\Users\m.ben-el-ghoul\PycharmProjects\Autocall pricer V2\data and keys\BNP Paribas_25_04_2024.xlsx",
        @"C:\Users\m.ben-el-ghoul\PycharmProjects\Autocall pricer V2\data and keys\CAPGEMINI_25_04_2024.xlsx",
        @"C:\Users\m.ben-el-ghoul\PycharmProjects\Autocall pricer V2\data and keys\ENGIE_25_04_2024.xlsx",
        @"C:\Users\m.ben-el-ghoul\PycharmProjects\Autocall pricer V2\data and keys\LVMH_25_04_2024.xlsx",
        @"C:\Users\m.ben-el-ghoul\PycharmProjects\Autocall pricer V2\data and keys\ORANGE_25_04_24.xlsx"
    };
}
