using System;
using System.Collections.Generic;
using System.Text;

namespace HPImageViewer.Core.Persistence
{
    public class AimXYSDesc : AimRectangleDesc
    {
        /// <summary>
        /// 是否是X方向
        /// </summary>
        public bool IsX { get; set; }

        public bool IsClockWise { get; set; }
        //public double OffsetX { get; set; } = 60;
        //public double OffsetY { get; set; } = 5;
        //public double RegionWidth { get; set; } = 40;
        //public double RegionHeight { get; set; } = 80;
        // public List<(double, double)> PointList { get; set; } = new List<(double, double)>();
    }
}
