using System;
using System.Collections.Generic;
using System.Text;

namespace HPImageViewer.Core.Persistence
{
    public class CrosshairsDesc : ROIDesc
    {
        public CrosshairsDesc() { }
        public CrosshairsDesc(double centerX,double centerY) {
            CenterX = centerX;
            CenterY = centerY;
        }

        public double CenterX { get; set; }

        public double CenterY { get; set; }

        public double Size { get; set; } = 30d;
        public double Angle { get; set; }
        public bool IsMove { get; set; } = false;

        public double ActualX { get; set; }
        public double ActualY { get; set; }
    }
}
