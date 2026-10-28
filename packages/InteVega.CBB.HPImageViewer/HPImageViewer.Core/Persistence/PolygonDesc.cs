using System.Collections.Generic;
using HPImageViewer.Core.Primitives;

namespace HPImageViewer.Core.Persistence
{
    public class PolygonDesc : ROIDesc
    {
        public List<Point> Vertices { get; set; } = new List<Point>();
        ///// <summary>
        ///// 是否封闭图形,默认封闭
        ///// </summary>
        public bool IsClosed { get; set; } = true;
        /// <summary>
        /// 能否移动
        /// </summary>
        public bool IsMove { get; set; } = true;
        public override string ToString()
        {
            string str = "";
            foreach (var vertice in Vertices)
            {
                str += $"{vertice}" + "; ";
            }
            return str;
        }
    }
}
