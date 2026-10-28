using System;
using System.Collections.Generic;
using System.Text;

namespace HPImageViewer.Core.Persistence
{
    public class AimRectangleDesc : BoxDesc
    {
        public double OffsetX { get; set; } = 60;
        /// <summary>
        /// 
        /// </summary>
        public double OffsetY { get; set; } = 5;
        /// <summary>
        /// 矩形宽度
        /// </summary>
        public double RegionWidth { get; set; } = 40;
        /// <summary>
        /// 矩形高度
        /// </summary>
        public double RegionHeight { get; set; } = 80;
    }
}
