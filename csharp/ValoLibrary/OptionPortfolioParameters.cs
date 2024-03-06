using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValoLibrary
{
    internal class OptionPortfolioParameters
    {
        public string OptionFlag { get; set; }
        public double S { get; set; }
        public double sigma { get; set; }
        public double r { get; set; }
        public double K { get; set; }
        public double T { get; set; }
        public double? q { get; set; }
    }
}
