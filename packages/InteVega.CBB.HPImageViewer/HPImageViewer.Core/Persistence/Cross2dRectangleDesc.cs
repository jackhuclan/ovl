using System;
using System.Collections.Generic;
using System.Text;

namespace HPImageViewer.Core.Persistence
{
    public class Cross2dRectangleDesc : CrossRectangleDesc
    {
        public double IntervalLength_X { get; set; } = 40;
        public double IntervalLength_Y { get; set; } = 40;
        public double BandPercent { get; set; }

        public double PointLength { get; set; }
        /// <summary>
        /// 小矩形位置
        /// </summary>
        public double SmallRectPositionPercent { get; set; } = 0.5;

        public List<(double, double)> RactPointList { get; set; }


    }
}
