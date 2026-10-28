using System;
using System.Drawing;

namespace HPImageViewer.Core.Persistence
{
    [Serializable]
    public class ROIDesc
    {
        public ROIDesc()
        {
            Id=Guid.NewGuid();
        }
        public Guid Id { get; set; }
        public string Color { get; set; } = "#FF0000";
        public string PointColor { get; set; } = "#FF0000";
        public double StrokeThickness { get; set; } = 2;


    }
}
