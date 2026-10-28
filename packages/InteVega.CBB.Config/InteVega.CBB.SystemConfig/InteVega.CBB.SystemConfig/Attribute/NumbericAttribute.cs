using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InteVega.CBB.SystemConfig
{
    public class NumbericAttribute: Attribute
    {
        public double Minimum { get; set; }
        public double Maximum { get; set; }
        public int DecimalPlaces { get; set; }
        public NumbericAttribute() { }
        public NumbericAttribute(double minimum, double maximum,int decimalPlaces)
        {
            Minimum = minimum;
            Maximum = maximum;
            DecimalPlaces = decimalPlaces;
        }
    }
}
