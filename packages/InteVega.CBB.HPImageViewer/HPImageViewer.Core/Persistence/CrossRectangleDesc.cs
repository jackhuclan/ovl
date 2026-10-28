using System;
namespace HPImageViewer.Core.Persistence
{
    public class CrossRectangleDesc : BoxDesc
    {
        [Obsolete("该方法已过时，请使用 BandPercent 代替。")]
        public double BandLength { get; set; } = 40;
        public double ColumnWidth { get; set; }

        //public List<(double, double)> PointList { get; set; } = new List<(double, double)>();
    }
}
