using HPImageViewer.Core.Persistence;
using HPImageViewer.Core.Primitives;
using HPImageViewer.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Point2 = System.Windows.Point;

namespace HPImageViewer.Rendering.ROIRenders
{
    internal class Cross2dRectangleRender : BoxRender
    {
        public Cross2dRectangleRender(Cross2dRectangleDesc cross2dRectangleDesc) : base(cross2dRectangleDesc)
        {
        }
        protected override void OnROIRender(RenderContext renderContext)
        {
            var brush = Brush;
            brush.Freeze();
            var fillBrush = IsSelected ? FillBrush : null;
            if (fillBrush?.IsFrozen == false)
            {
                fillBrush.Freeze();
            }
            //renderContext.DrawingContext.DrawRegularRectangle(Brushes.Transparent, new Pen(FillBrush, ROIDesc.StrokeThickness)
            //{
            //    DashStyle = new DashStyle(new double[] { 4, 4 }, 0)
            //}, RenderTransform.ToDevice(Rect));


            var pen = new Pen(brush, 1);
            DashStyle dashStyle = new DashStyle();
            dashStyle.Offset = 0;
            dashStyle.Dashes = new DoubleCollection();
            dashStyle.Dashes.Add(2);
            dashStyle.Dashes.Add(5);
            pen.DashStyle = dashStyle;

            renderContext.DrawingContext.DrawRegularRectangle(Brushes.Transparent, pen, RenderTransform.ToDevice(MathUtil.GetNormalizedRectangle(Point1, Point2)));
            renderContext.DrawingContext.DrawRegularRectangle(Brushes.Transparent, pen, RenderTransform.ToDevice(MathUtil.GetNormalizedRectangle(Point2, Point3)));
            renderContext.DrawingContext.DrawRegularRectangle(Brushes.Transparent, pen, RenderTransform.ToDevice(MathUtil.GetNormalizedRectangle(Point3, Point4)));
            renderContext.DrawingContext.DrawRegularRectangle(Brushes.Transparent, pen, RenderTransform.ToDevice(MathUtil.GetNormalizedRectangle(Point4, Point5)));
            renderContext.DrawingContext.DrawRegularRectangle(Brushes.Transparent, pen, RenderTransform.ToDevice(MathUtil.GetNormalizedRectangle(Point5, Point6)));
            renderContext.DrawingContext.DrawRegularRectangle(Brushes.Transparent, pen, RenderTransform.ToDevice(MathUtil.GetNormalizedRectangle(Point6, Point7)));
            renderContext.DrawingContext.DrawRegularRectangle(Brushes.Transparent, pen, RenderTransform.ToDevice(MathUtil.GetNormalizedRectangle(Point7, Point8)));
            renderContext.DrawingContext.DrawRegularRectangle(Brushes.Transparent, pen, RenderTransform.ToDevice(MathUtil.GetNormalizedRectangle(Point8, Point9)));
            renderContext.DrawingContext.DrawRegularRectangle(Brushes.Transparent, pen, RenderTransform.ToDevice(MathUtil.GetNormalizedRectangle(Point9, Point10)));
            renderContext.DrawingContext.DrawRegularRectangle(Brushes.Transparent, pen, RenderTransform.ToDevice(MathUtil.GetNormalizedRectangle(Point10, Point11)));
            renderContext.DrawingContext.DrawRegularRectangle(Brushes.Transparent, pen, RenderTransform.ToDevice(MathUtil.GetNormalizedRectangle(Point11, Point12)));
            renderContext.DrawingContext.DrawRegularRectangle(Brushes.Transparent, pen, RenderTransform.ToDevice(MathUtil.GetNormalizedRectangle(Point12, Point1)));


            renderContext.DrawingContext.DrawRegularRectangle(fillBrush, new Pen(brush, ROIDesc.StrokeThickness), RenderTransform.ToDevice(TopLeftRect));
            renderContext.DrawingContext.DrawRegularRectangle(fillBrush, new Pen(brush, ROIDesc.StrokeThickness), RenderTransform.ToDevice(TopRightRect));
            renderContext.DrawingContext.DrawRegularRectangle(fillBrush, new Pen(brush, ROIDesc.StrokeThickness), RenderTransform.ToDevice(RightTopRect));
            renderContext.DrawingContext.DrawRegularRectangle(fillBrush, new Pen(brush, ROIDesc.StrokeThickness), RenderTransform.ToDevice(RightBottomRect));
            renderContext.DrawingContext.DrawRegularRectangle(fillBrush, new Pen(brush, ROIDesc.StrokeThickness), RenderTransform.ToDevice(BottomLeftRect));
            renderContext.DrawingContext.DrawRegularRectangle(fillBrush, new Pen(brush, ROIDesc.StrokeThickness), RenderTransform.ToDevice(BottomRightRect));
            renderContext.DrawingContext.DrawRegularRectangle(fillBrush, new Pen(brush, ROIDesc.StrokeThickness), RenderTransform.ToDevice(LeftTopRect));
            renderContext.DrawingContext.DrawRegularRectangle(fillBrush, new Pen(brush, ROIDesc.StrokeThickness), RenderTransform.ToDevice(LeftBottomRect));

            PointLength = Point12.X - Point11.X;

            //List<(double, double)> pointList = new List<(double, double)>();
            //pointList.Add(new(Point11.X, Point11.Y));
            //pointList.Add(new(Point12.X, Point12.Y));
            //pointList.Add(new(Point1.X, Point1.Y));
            //pointList.Add(new(Point2.X, Point2.Y));
            //pointList.Add(new(Point3.X, Point3.Y));
            //pointList.Add(new(Point4.X, Point4.Y));
            //pointList.Add(new(Point5.X, Point5.Y));
            //pointList.Add(new(Point6.X, Point6.Y));
            //pointList.Add(new(Point7.X, Point7.Y));
            //pointList.Add(new(Point8.X, Point8.Y));
            //pointList.Add(new(Point9.X, Point9.Y));
            //pointList.Add(new(Point10.X, Point10.Y));
            //PointList = new List<(double, double)>();
            //PointList.AddRange(pointList);
            BindRactPlintList();
        }
        

        public void BindRactPlintList()
        {
           
            //保存子项的顺序不能错
            //先左上->上左->上右->右上->右下->下右->下左->左下
            Cross2dRectangleDesc.RactPointList = new List<(double, double)>() {
                 new (LeftTopRect.X, LeftTopRect.Y+LeftTopRect.Height/2),//左上角的矩形开始添加
                 new (LeftTopRect.X+LeftTopRect.Width, LeftTopRect.Y+LeftTopRect.Height/2),

                 new (TopLeftRect.X+TopLeftRect.Width/2, TopLeftRect.Y+TopLeftRect.Height),//上左角的矩形
                 new (TopLeftRect.X+TopLeftRect.Width/2, TopLeftRect.Y),

                 new (TopRightRect.X+TopRightRect.Width/2, TopRightRect.Y),
                 new (TopRightRect.X+TopRightRect.Width/2, TopRightRect.Y+TopRightRect.Height),//上右角的矩形

                 new (RightTopRect.X, RightTopRect.Y+RightTopRect.Height/2),
                 new (RightTopRect.X+RightTopRect.Width, RightTopRect.Y+RightTopRect.Height/2),//右上角的矩形

                 new (RightBottomRect.X+RightBottomRect.Width, RightBottomRect.Y+RightBottomRect.Height/2),//右下角的矩形
                 new (RightBottomRect.X, RightBottomRect.Y+RightBottomRect.Height/2),

                 
                 new (BottomLeftRect.X+BottomLeftRect.Width/2, BottomLeftRect.Y),
                 new (BottomLeftRect.X+BottomLeftRect.Width/2, BottomLeftRect.Y+BottomLeftRect.Height),//底右角的矩形

                 new (BottomRightRect.X+BottomRightRect.Width/2, BottomRightRect.Y+BottomRightRect.Height),
                 new (BottomRightRect.X+BottomRightRect.Width/2, BottomRightRect.Y),  //底左角的矩形

                 new (LeftBottomRect.X+LeftBottomRect.Width, LeftBottomRect.Y+LeftBottomRect.Height/2),
                 new (LeftBottomRect.X, LeftBottomRect.Y+LeftBottomRect.Height/2),//左下角的矩形
                 
            };

            //var points = new List<Point2>() { Point11, Point12, Point1, Point2, Point3, Point4, Point5, Point6, Point7, Point8, Point9, Point10 };

            //string filename = "roi.log";
            //int i = 1;
            //foreach (var point in points)
            //{
            //    File.AppendAllText(filename, $"第{i}点：{point.ToString()}\r\n");
            //    i++;
            //}
            //string filename2 = "roi2.log";
            //i = 1;
            //foreach (var p in Cross2dRectangleDesc.RactPointList)
            //{

            //    File.AppendAllText(filename2, $"第{i}点：{p.ToString()}\r\n");
            //    i++;
            //}
        }

        public double BandPercent
        {
            get => Cross2dRectangleDesc.BandPercent / 100;
            set
            {
                Cross2dRectangleDesc.BandPercent = value * 100;
            }
        }

        public Cross2dRectangleDesc Cross2dRectangleDesc => this.ROIDesc as Cross2dRectangleDesc;
        public double SmallRectPosition_Y
        {
            get 
            {
                //起始点位置
                //高度-间隔
                return (Height - IntervalLength_Y) / 2 * Cross2dRectangleDesc.SmallRectPositionPercent;

            }
        }
        public double SmallRectPosition_X
        {
            get
            {
                //起始点位置
                //高度-间隔
                return (Width - IntervalLength_X) / 2 * Cross2dRectangleDesc.SmallRectPositionPercent;

            }
        }

        public double IntervalLength_X
        {
            get => Cross2dRectangleDesc.IntervalLength_X;
            set
            {
                if (Cross2dRectangleDesc.IntervalLength_X != value)
                {
                    Cross2dRectangleDesc.IntervalLength_X = value;
                    OnPropertyChanged();
                }
            }
        }
        public double IntervalLength_Y
        {
            get => Cross2dRectangleDesc.IntervalLength_Y;
            set
            {
                if (Cross2dRectangleDesc.IntervalLength_Y != value)
                {
                    Cross2dRectangleDesc.IntervalLength_Y = value;
                    OnPropertyChanged();
                }
            }
        }
        //public double BandLength
        //{
        //    get => Cross2dRectangleDesc.BandLength;
        //    set
        //    {
        //        if (Cross2dRectangleDesc.BandLength != value)
        //        {
        //            Cross2dRectangleDesc.BandLength = value;
        //            OnPropertyChanged();
        //        }
        //    }
        //}
        public double BandLength_Y
        {
            get
            {
                //通过比例计算BandLength
                double height = Point12.Y - Point1.Y;
                return height * BandPercent;
            }
        }
        public double BandLength_X
        {
            get
            {
                //通过比例计算BandLength
                double height = Point12.X - Point11.X;
                return height * BandPercent;
            }
        }
        public double ColumnWidth
        {
            get => Cross2dRectangleDesc.ColumnWidth;
            set
            {
                if (Cross2dRectangleDesc.ColumnWidth != value)
                {
                    Cross2dRectangleDesc.ColumnWidth = value;
                    OnPropertyChanged();
                }
            }
        }
        //public List<(double, double)> PointList
        //{
        //    get => Cross2dRectangleDesc.PointList;
        //    set
        //    {
        //        if (Cross2dRectangleDesc.PointList != value)
        //        {
        //            Cross2dRectangleDesc.PointList = value;
        //            OnPropertyChanged();
        //        }
        //    }
        //}
        public double PointLength
        {
            get => Cross2dRectangleDesc.PointLength;
            set
            {
                if (Cross2dRectangleDesc.PointLength != value)
                {
                    Cross2dRectangleDesc.PointLength = value;
                    OnPropertyChanged();
                }
            }
        }
        #region Point
        public System.Windows.Point Point1
        {
            get
            {
                var bound = Rect;
                var top = bound.Top;
                var left = bound.Left + (bound.Width - IntervalLength_X) / 2;
                return new System.Windows.Point(left, top);
            }
        }

        public System.Windows.Point Point2
        {
            get
            {
                var bound = Rect;
                var top = bound.Top;
                var left = bound.Left + ((bound.Width - IntervalLength_X) / 2) + IntervalLength_X;
                return new System.Windows.Point(left, top);
            }
        }
        public System.Windows.Point Point3
        {
            get
            {
                var bound = Rect;
                var top = bound.Top + ((bound.Height - IntervalLength_Y) / 2);
                var left = bound.Left + ((bound.Width - IntervalLength_X) / 2) + IntervalLength_X;
                return new System.Windows.Point(left, top);
            }
        }
        public System.Windows.Point Point4
        {
            get
            {
                var bound = Rect;
                var top = bound.Top + ((bound.Height - IntervalLength_Y) / 2);
                var left = bound.Right;
                return new System.Windows.Point(left, top);
            }
        }
        public System.Windows.Point Point5
        {
            get
            {
                var bound = Rect;
                var top = bound.Top + ((bound.Height - IntervalLength_Y) / 2) + IntervalLength_Y;
                var left = bound.Right;
                return new System.Windows.Point(left, top);
            }
        }
        public System.Windows.Point Point6
        {
            get
            {
                var bound = Rect;
                var top = bound.Top + ((bound.Height - IntervalLength_Y) / 2) + IntervalLength_Y;
                var left = bound.Left + ((bound.Width - IntervalLength_X) / 2) + IntervalLength_X;
                return new System.Windows.Point(left, top);
            }
        }
        public System.Windows.Point Point7
        {
            get
            {
                var bound = Rect;
                var top = bound.Bottom;
                var left = bound.Left + ((bound.Width - IntervalLength_X) / 2) + IntervalLength_X;
                return new System.Windows.Point(left, top);
            }
        }
        public System.Windows.Point Point8
        {
            get
            {
                var bound = Rect;
                var top = bound.Bottom;
                var left = bound.Left + ((bound.Width - IntervalLength_X) / 2);
                return new System.Windows.Point(left, top);
            }
        }
        public System.Windows.Point Point9
        {
            get
            {
                var bound = Rect;
                var top = bound.Bottom - ((bound.Height - IntervalLength_Y) / 2);
                var left = bound.Left + ((bound.Width - IntervalLength_X) / 2);
                return new System.Windows.Point(left, top);
            }
        }
        public System.Windows.Point Point10
        {
            get
            {
                var bound = Rect;
                var top = bound.Bottom - ((bound.Height - IntervalLength_Y) / 2);
                var left = bound.Left;
                return new System.Windows.Point(left, top);
            }
        }
        public System.Windows.Point Point11
        {
            get
            {
                var bound = Rect;
                var top = bound.Top + ((bound.Height - IntervalLength_Y) / 2);
                var left = bound.Left;
                return new System.Windows.Point(left, top);
            }
        }
        public System.Windows.Point Point12
        {
            get
            {
                var bound = Rect;
                var top = bound.Top + ((bound.Height - IntervalLength_Y) / 2);
                var left = bound.Left + ((bound.Width - IntervalLength_X) / 2);
                return new System.Windows.Point(left, top);
            }
        }
        #endregion
        #region Rect
        //public double SmallRectCenter { get; set; } = 30;

        public Rect TopLeftRect
        {
            get
            {
                var x1 = Point1.X;
                var y1 = Point1.Y;
                var x2 = Point12.X;
                var y2 = Point12.Y;
                var left = x1 - (ColumnWidth / 2);
                //var top = ((y2 - y1 - BandLength) / 2) + y1;
                var top = (SmallRectPosition_Y - (BandLength_Y /2)) + y1;
                return new Rect(left, top, ColumnWidth, BandLength_Y);
            }
        }
        public Rect TopRightRect
        {
            get
            {
                var x1 = Point2.X;
                var y1 = Point2.Y;
                var x2 = Point3.X;
                var y2 = Point3.Y;
                var left = x1 - (ColumnWidth / 2);
                //var top = ((y2 - y1 - BandLength) / 2) + y1;
                var top = (SmallRectPosition_Y - (BandLength_Y / 2)) + y1;
                return new Rect(left, top, ColumnWidth, BandLength_Y);
            }
        }

        public Rect RightTopRect
        {
            get
            {
                var x1 = Point4.X;
                var y1 = Point4.Y;
                var x2 = Point3.X;
                var y2 = Point3.Y;
                //var left = (x1 - x2 - BandLength) / 2 + x2;
                var left = x1 - SmallRectPosition_X - BandLength_X / 2;
                var top = y1 - (ColumnWidth / 2);
                return new Rect(left, top, BandLength_X, ColumnWidth);
            }
        }
        public Rect RightBottomRect
        {
            get
            {
                var x1 = Point5.X;
                var y1 = Point5.Y;
                var x2 = Point6.X;
                var y2 = Point6.Y;
                //var left = (x1 - x2 - BandLength) / 2 + x2;
                var left = x1 - SmallRectPosition_X - BandLength_X / 2;
                var top = y1 - (ColumnWidth / 2);
                return new Rect(left, top, BandLength_X, ColumnWidth);
            }
        }

        public Rect BottomLeftRect
        {
            get
            {
                var x1 = Point7.X;
                var y1 = Point7.Y;
                var x2 = Point6.X;
                var y2 = Point6.Y;
                var left = x2 - (ColumnWidth / 2);
                //var top = ((y1 - y2 - BandLength) / 2) + y2;
                var top = y1 - (SmallRectPosition_Y + (BandLength_Y / 2));
                return new Rect(left, top, ColumnWidth, BandLength_Y);    
            }
        }
        public Rect BottomRightRect
        {
            get
            {
                var x1 = Point8.X;
                var y1 = Point8.Y;
                var x2 = Point9.X;
                var y2 = Point9.Y;
                var left = x2 - (ColumnWidth / 2);
                //var top = ((y1 - y2 - BandLength) / 2) + y2;
                var top = y1 - (SmallRectPosition_Y + (BandLength_Y / 2));
                return new Rect(left, top, ColumnWidth, BandLength_Y);
            }
        }
        public Rect LeftTopRect
        {
            get
            {
                var x1 = Point11.X;
                var y1 = Point11.Y;
                var x2 = Point12.X;
                var y2 = Point12.Y;
                //var left = (x2 - x1 - BandLength) / 2 + x1;

                var left = x1 + SmallRectPosition_X-BandLength_X/2;

                var top = y1 - (ColumnWidth / 2);
                
                return new Rect(left, top, BandLength_X, ColumnWidth);
            }
        }
        public Rect LeftBottomRect
        {
            get
            {
                var x1 = Point10.X;
                var y1 = Point10.Y;
                var x2 = Point9.X;
                var y2 = Point9.Y;
                //var left = (x2 - x1 - BandLength) / 2 + x1;
                var left = x1 + SmallRectPosition_X - BandLength_X / 2;
                var top = y1 - (ColumnWidth / 2);
                return new Rect(left, top, BandLength_X, ColumnWidth);
            }
        }
        #endregion
        /// <summary>Get handle point by 1-based number</summary>
        /// <param name="handleNumber"></param>
        /// <returns></returns>
        public override Point2 GetHandle(int handleNumber)
        {
            try
            {
                var index = (handleNumber - 1) / 4;
                var remainder = (handleNumber - 1) % 4 + 1;
                if (index == 0)
                {
                    return RenderTransform.ToDevice(GetRectHandle(Rect, remainder)).ToWindowPoint();
                }
                else if (index == 1 || index ==2)
                {
                    remainder = (handleNumber - 1) % 5 + 1;
                    return RenderTransform.ToDevice(GetSmallRectHandle(TopLeftRect, remainder)).ToWindowPoint();
                }
            }
            catch (Exception ex)
            {

            }

            return new Point2(0, 0);
        }
        /// <summary>
        /// 绘制小矩形的点
        /// </summary>
        /// <param name="rectangle"></param>
        /// <param name="handleNumber"></param>
        /// <param name="includeCenterPoint"></param>
        /// <returns></returns>
        internal static Core.Primitives.Point GetSmallRectHandle(Core.Primitives.Rect rectangle, int handleNumber, bool includeCenterPoint = true)
        {
            double x, y, xCenter, yCenter;
            xCenter = rectangle.X + rectangle.Width / 2;
            yCenter = rectangle.Y + rectangle.Height / 2;
            x = rectangle.X;
            y = rectangle.Y;

            if (includeCenterPoint == false)
            {
                handleNumber = (handleNumber - 1) * 2 + 1;
            }
            switch (handleNumber)
            {
                case 1:
                    x = xCenter;
                    y = rectangle.Y;
                    break;
                case 2:
                    x = rectangle.Right;
                    y = yCenter;
                    break;
                case 3:
                    x = xCenter;
                    y = rectangle.Bottom;
                    break;
                case 4:
                    x = rectangle.X;
                    y = yCenter;
                    break;
                case 5:
                    x = xCenter;
                    y = yCenter;
                    break;
                default: return new Core.Primitives.Point(0, 0);
            }
            return new Core.Primitives.Point(x, y);
        }
        /// <summary>
        /// 绘制点
        /// </summary>
        /// <param name="rectangle"></param>
        /// <param name="handleNumber"></param>
        /// <param name="includeCenterPoint"></param>
        /// <returns></returns>
        internal Core.Primitives.Point GetRectHandle(Core.Primitives.Rect rectangle, int handleNumber, bool includeCenterPoint = true)
        {
            double x, y, xCenter, yCenter;
            xCenter = rectangle.X + rectangle.Width / 2;
            yCenter = rectangle.Y + rectangle.Height / 2;
            x = rectangle.X;
            y = rectangle.Y;

            if (includeCenterPoint == false)
            {
                handleNumber = (handleNumber - 1) * 2 + 1;
            }
            switch (handleNumber)
            {
                case 1:
                    x = xCenter - IntervalLength_X / 2;
                    y = rectangle.Y;
                    break;
                case 2:
                    x = rectangle.Right;
                    y = yCenter + IntervalLength_Y / 2;
                    break;
                case 3:
                    x = xCenter + IntervalLength_X / 2;
                    y = rectangle.Bottom;
                    break;
                case 4:
                    x = rectangle.X;
                    y = yCenter - IntervalLength_Y / 2;
                    break;
            }
            return new Core.Primitives.Point(x, y);
        }


        internal static Core.Primitives.Point GetLineHandle(System.Windows.Point pointStart, System.Windows.Point pointEnd, int remainder, int index, double columnWidth, bool includeCenterPoint = true)
        {
            double colWidth = (columnWidth / 2);
            if (remainder == 1)
            {
                double x = pointStart.X + ((index == 0 || index == 3) ? -colWidth : colWidth);
                double y = pointStart.Y + ((index == 0 || index == 1) ? -colWidth : colWidth);
                return new Point(x, y);
            }
            else if (remainder == 2)
            {
                return new Point(pointStart.X, pointStart.Y);
            }
            else if (remainder == 3)
            {
                return new Point(pointEnd.X, pointEnd.Y);
            }
            else
            {
                double x = pointEnd.X + ((index == 0 || index == 1) ? colWidth : -colWidth);
                double y = pointEnd.Y + ((index == 0 || index == 3) ? -colWidth : colWidth);
                return new Point(x, y);
            }
        }
        public override int HandleCount => 9;
        protected override void MoveHandleToInternal(int handleNumber, Point2 point)
        {
            var index = (handleNumber - 1) / 4;
            var remainder = (handleNumber - 1) % 4 + 1;
            if (index == 0)
            {
                //外矩形四个点的坐标位置
                Point pointRect = new Point(0, 0);
                //头部点位
                if (remainder == 1)
                {
                    //左上角位置
                    pointRect = new Point(Rect.TopLeft.X-5, Rect.TopLeft.Y-5);// Rect.TopLeft;
                }
                else if (remainder == 2)
                {
                    //右上角位置
                    pointRect = Rect.TopRight;
                }
                else if (remainder == 3)
                {
                    //右下角位置
                    pointRect = Rect.BottomRight;
                }
                else if (remainder == 4)
                {
                    pointRect = Rect.BottomLeft;
                }
                //计算当前点位距离大矩形点位的位置
                var normalizedRect = MathUtil.GetNormalizedRectangle(point, new System.Windows.Point(pointRect.X, pointRect.Y), false);
                var right = Left + Width;
                var bottom = Top + Height;
                var rect_top = Top;
                var rect_left = Left;
                var rect_height = Height;
                var rect_width = Width;
                var _intervalLength_X = IntervalLength_X;
                var _intervalLength_Y = IntervalLength_Y;
                if (remainder == 1)
                {
                    rect_top = point.Y;
                    rect_height = bottom - rect_top;
                    //计算外框到选中点的距离
                    var pointLength = (rect_width - _intervalLength_X) / 2;
                    //外框坐标更改
                    rect_left = point.X - pointLength;
                    //修改动态间隔长度
                    _intervalLength_X = right - rect_left - pointLength * 2;
                    //更改外边框宽度
                    rect_width = right - rect_left;
                }
                else if (remainder == 2)
                {
                    //rect_left += (normalizedRect.Width/2);
                    //rect_width = rect_width - normalizedRect.Width;

                    rect_width = (right - rect_left)-normalizedRect.Width;
                    
                    //计算外框到选中点的距离
                    var pointLength = (rect_height - _intervalLength_Y) / 2;
                    rect_height = point.Y + pointLength - rect_top;
                    _intervalLength_Y = rect_height - (pointLength * 2);
                }
                else if (remainder == 3)
                {
                    rect_height = (bottom - rect_top) - normalizedRect.Height;
                    //计算外框到选中点的距离
                    var pointLength = (rect_width - _intervalLength_X) / 2;
                    //外框坐标更改
                    rect_width = point.X + pointLength - rect_left;
                    _intervalLength_X = rect_width - (pointLength * 2);
                }
                else if (remainder == 4)
                {
                    rect_left = point.X;
                    rect_width = rect_width + (normalizedRect.Width);
                    //rect_width=Left-rect_left;
                    Console.WriteLine("Left:" + Left + "--rect_left:" + rect_left);
                    //计算外框到选中点的距离
                    var pointLength = (rect_height - _intervalLength_Y) / 2;
                    //外框坐标更改
                    rect_top = point.Y - pointLength;
                    //修改动态间隔长度
                    _intervalLength_Y = bottom - rect_top - pointLength * 2;
                    //更改外边框高度
                    rect_height = bottom - rect_top;

                }

                if (_intervalLength_X<=3) _intervalLength_X = 3;
                if (_intervalLength_Y <= 3) _intervalLength_Y = 3;
                if (rect_left < 0)
                    rect_left = 0;
                if (rect_left > ImageWidth)
                    rect_left = ImageWidth;                
                if (rect_top < 0) rect_top = 0;
                if (rect_top > ImageHeight)
                    rect_top = ImageHeight;
                if ((rect_left+rect_width) > ImageWidth) 
                    rect_width = ImageWidth- rect_left;
                if ((rect_top+rect_height) > ImageHeight) rect_height = ImageHeight-rect_top;

                if (!(rect_width <= (IntervalLength_X + SmallRectPosition_X +BandLength_X) || rect_height <= (IntervalLength_Y+ SmallRectPosition_Y + BandLength_Y))) 
                {
                    Top = rect_top;
                    Left = rect_left;
                    Height = rect_height;
                    Width = rect_width;
                    IntervalLength_X = _intervalLength_X;
                    IntervalLength_Y = _intervalLength_Y;
                }

                //Console.WriteLine("rect_width:"+ rect_width+ "--Left:"+rect_left);
            }
            else if(index==1 || index==2)
            {
                remainder = (handleNumber - 1) % 5 + 1;

                if (remainder !=5) 
                {
                    double columnWidth = ColumnWidth;
                    double bandLength = BandLength_Y;

                    var normalizedRect = MathUtil.GetNormalizedRectangle(point, new System.Windows.Point(TopLeftRect.TopLeft.X, TopLeftRect.TopLeft.Y), false);
                    if (remainder == 1)
                    {
                        bandLength = bandLength + normalizedRect.Height;
                    }
                    else if (remainder == 2)
                    {
                        columnWidth = -normalizedRect.Width;
                    }
                    else if (remainder == 3)
                    {
                        bandLength = -normalizedRect.Height;
                    }
                    else
                    {
                        columnWidth = columnWidth + normalizedRect.Width;
                    }
                    if (columnWidth < IntervalLength_X + 5 && columnWidth >= 3)
                    {
                        ColumnWidth = columnWidth;
                    }
                    if (bandLength < Point12.Y - Point1.Y && bandLength >= 3)
                    {
                        BandPercent = bandLength / (Point12.Y-Point1.Y);
                        //BandLength = bandLength;
                    }
                }
                else
                {
                    //获取小矩形的中心点
                    var centerX = TopLeftRect.TopLeft.X + TopLeftRect.Width / 2;
                    var conterY = TopLeftRect.TopLeft.Y + TopLeftRect.Height / 2;
                    //计算中心点到边的差值
                    var normalizedRect = MathUtil.GetNormalizedRectangle(point, new System.Windows.Point(centerX, conterY), false);

                    //计算新的中心点到边的距离
                    var positionLength = SmallRectPosition_Y - normalizedRect.Height;
                    var length = (Height - IntervalLength_Y) / 2;
                    if (positionLength > (BandLength_Y/2) && positionLength + (BandLength_Y / 2) < length)
                    {
                        //计算外框边到小矩形的比例，然后根据比例，在计算实际差值位置
                        Cross2dRectangleDesc.SmallRectPositionPercent= positionLength / length;
                        //Console.WriteLine("height"+ positionLength);
                    }
                }
            }
        }
        public override bool GetROIRegion(double x, double y)
        {
            if (Top < y && (Top + Height) > y && Left < x && (Left + Width) > x)
            {
                return true;
            }
            return false;
        }
        public override Cursor GetCursorsType(int handleNumber)
        {
            var index = (handleNumber - 1) / 4;
            if (index == 0)
                return Cursors.SizeNWSE;//  "\" 方向
            else if (index == 1 || index == 2)
            {

                if (handleNumber == 6 || handleNumber == 8)
                    return Cursors.SizeNS;//上下方向
                else if (handleNumber == 9 || handleNumber == 7)
                    return Cursors.SizeWE;//横向
                else
                {
                    return Cursors.Hand;
                }

            }
            return base.GetCursorsType(handleNumber);
        }
    }
}
