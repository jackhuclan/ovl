using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace HPImageViewer.Core.Persistence
{
    public class QuadRectangleDesc : BoxDesc
    {
        [Obsolete("该方法已过时，请使用 BandPercent 代替。")]
        public double BandLength { get; set; }

        public double BandPercent { get; set; }
    }
}
