using HPImageViewer.Core.Persistence;
using HPImageViewer.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using HPImageViewer.Core.Primitives;
using Point2 = System.Windows.Point;
using System.Windows.Controls;
using System.Windows.Media.Media3D;
using System.Windows.Input;

namespace HPImageViewer.Rendering.ROIRenders
{
    internal class CrossRectangleRender : BoxRender
    {
        public CrossRectangleRender(CrossRectangleDesc crossRectangleDesc) : base(crossRectangleDesc)
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

            renderContext.DrawingContext.DrawRegularRectangle(Brushes.Transparent, new Pen(FillBrush, ROIDesc.StrokeThickness)
            {
                DashStyle = new DashStyle(new double[] { 4, 4 }, 0)
            }, RenderTransform.ToDevice(Rect));

            renderContext.DrawingContext.DrawRegularRectangle(Brushes.Transparent, new Pen(brush, ROIDesc.StrokeThickness), RenderTransform.ToDevice(MathUtil.GetNormalizedRectangle(Point1, Point2)));
            renderContext.DrawingContext.DrawRegularRectangle(Brushes.Transparent, new Pen(brush, ROIDesc.StrokeThickness), RenderTransform.ToDevice(MathUtil.GetNormalizedRectangle(Point2, Point3)));
            renderContext.DrawingContext.DrawRegularRectangle(Brushes.Transparent, new Pen(brush, ROIDesc.StrokeThickness), RenderTransform.ToDevice(MathUtil.GetNormalizedRectangle(Point3, Point4)));
            renderContext.DrawingContext.DrawRegularRectangle(Brushes.Transparent, new Pen(brush, ROIDesc.StrokeThickness), RenderTransform.ToDevice(MathUtil.GetNormalizedRectangle(Point4, Point5)));
            renderContext.DrawingContext.DrawRegularRectangle(Brushes.Transparent, new Pen(brush, ROIDesc.StrokeThickness), RenderTransform.ToDevice(MathUtil.GetNormalizedRectangle(Point5, Point6)));
            renderContext.DrawingContext.DrawRegularRectangle(Brushes.Transparent, new Pen(brush, ROIDesc.StrokeThickness), RenderTransform.ToDevice(MathUtil.GetNormalizedRectangle(Point6, Point7)));
            renderContext.DrawingContext.DrawRegularRectangle(Brushes.Transparent, new Pen(brush, ROIDesc.StrokeThickness), RenderTransform.ToDevice(MathUtil.GetNormalizedRectangle(Point7, Point8)));
            renderContext.DrawingContext.DrawRegularRectangle(Brushes.Transparent, new Pen(brush, ROIDesc.StrokeThickness), RenderTransform.ToDevice(MathUtil.GetNormalizedRectangle(Point8, Point9)));
            renderContext.DrawingContext.DrawRegularRectangle(Brushes.Transparent, new Pen(brush, ROIDesc.StrokeThickness), RenderTransform.ToDevice(MathUtil.GetNormalizedRectangle(Point9, Point10)));
            renderContext.DrawingContext.DrawRegularRectangle(Brushes.Transparent, new Pen(brush, ROIDesc.StrokeThickness), RenderTransform.ToDevice(MathUtil.GetNormalizedRectangle(Point10, Point11)));
            renderContext.DrawingContext.DrawRegularRectangle(Brushes.Transparent, new Pen(brush, ROIDesc.StrokeThickness), RenderTransform.ToDevice(MathUtil.GetNormalizedRectangle(Point11, Point12)));
            renderContext.DrawingContext.DrawRegularRectangle(Brushes.Transparent, new Pen(brush, ROIDesc.StrokeThickness), RenderTransform.ToDevice(MathUtil.GetNormalizedRectangle(Point12, Point1)));
            var deviceBandWidth = ColumnWidth * renderContext.Scale;
            renderContext.DrawingContext.DrawRegularRectangle(Brushes.Transparent, new Pen(FillBrush, deviceBandWidth), RenderTransform.ToDevice(MathUtil.GetNormalizedRectangle(Point1, Point2)));
            renderContext.DrawingContext.DrawRegularRectangle(Brushes.Transparent, new Pen(FillBrush, deviceBandWidth), RenderTransform.ToDevice(MathUtil.GetNormalizedRectangle(Point2, Point3)));
            renderContext.DrawingContext.DrawRegularRectangle(Brushes.Transparent, new Pen(FillBrush, deviceBandWidth), RenderTransform.ToDevice(MathUtil.GetNormalizedRectangle(Point3, Point4)));
            renderContext.DrawingContext.DrawRegularRectangle(Brushes.Transparent, new Pen(FillBrush, deviceBandWidth), RenderTransform.ToDevice(MathUtil.GetNormalizedRectangle(Point4, Point5)));
            renderContext.DrawingContext.DrawRegularRectangle(Brushes.Transparent, new Pen(FillBrush, deviceBandWidth), RenderTransform.ToDevice(MathUtil.GetNormalizedRectangle(Point5, Point6)));
            renderContext.DrawingContext.DrawRegularRectangle(Brushes.Transparent, new Pen(FillBrush, deviceBandWidth), RenderTransform.ToDevice(MathUtil.GetNormalizedRectangle(Point6, Point7)));
            renderContext.DrawingContext.DrawRegularRectangle(Brushes.Transparent, new Pen(FillBrush, deviceBandWidth), RenderTransform.ToDevice(MathUtil.GetNormalizedRectangle(Point7, Point8)));
            renderContext.DrawingContext.DrawRegularRectangle(Brushes.Transparent, new Pen(FillBrush, deviceBandWidth), RenderTransform.ToDevice(MathUtil.GetNormalizedRectangle(Point8, Point9)));
            renderContext.DrawingContext.DrawRegularRectangle(Brushes.Transparent, new Pen(FillBrush, deviceBandWidth), RenderTransform.ToDevice(MathUtil.GetNormalizedRectangle(Point9, Point10)));
            renderContext.DrawingContext.DrawRegularRectangle(Brushes.Transparent, new Pen(FillBrush, deviceBandWidth), RenderTransform.ToDevice(MathUtil.GetNormalizedRectangle(Point10, Point11)));
            renderContext.DrawingContext.DrawRegularRectangle(Brushes.Transparent, new Pen(FillBrush, deviceBandWidth), RenderTransform.ToDevice(MathUtil.GetNormalizedRectangle(Point11, Point12)));
            renderContext.DrawingContext.DrawRegularRectangle(Brushes.Transparent, new Pen(FillBrush, deviceBandWidth), RenderTransform.ToDevice(MathUtil.GetNormalizedRectangle(Point12, Point1)));

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
        }

        public CrossRectangleDesc CrossRectangleDesc => this.ROIDesc as CrossRectangleDesc;
        public double BandLength
        {
            get => CrossRectangleDesc.BandLength;
            set
            {
                if (CrossRectangleDesc.BandLength != value)
                {
                    CrossRectangleDesc.BandLength = value;
                    OnPropertyChanged();
                }
            }
        }
        public double ColumnWidth
        {
            get => CrossRectangleDesc.ColumnWidth;
            set
            {
                if (CrossRectangleDesc.ColumnWidth != value)
                {
                    CrossRectangleDesc.ColumnWidth = value;
                    OnPropertyChanged();
                }
            }
        }
        //public List<(double,double)> PointList
        //{
        //    get => CrossRectangleDesc.PointList;
        //    set
        //    {
        //        if (CrossRectangleDesc.PointList != value)
        //        {
        //            CrossRectangleDesc.PointList = value;
        //            OnPropertyChanged();
        //        }
        //    }
        //}
        #region Point
        public System.Windows.Point Point1
        {
            get
            {
                var bound = Bound;
                var top = bound.Top;
                var left = bound.Left + (bound.Width - BandLength) / 2;
                return new System.Windows.Point(left, top);
            }
        }

        public System.Windows.Point Point2
        {
            get
            {
                var bound = Bound;
                var top = bound.Top;
                var left = bound.Left + ((bound.Width - BandLength) / 2) + BandLength;
                return new System.Windows.Point(left, top);
            }
        }
        public System.Windows.Point Point3
        {
            get
            {
                var bound = Bound;
                var top = bound.Top + ((bound.Height - BandLength) / 2);
                var left = bound.Left + ((bound.Width - BandLength) / 2) + BandLength;
                return new System.Windows.Point(left, top);
            }
        }
        public System.Windows.Point Point4
        {
            get
            {
                var bound = Bound;
                var top = bound.Top + ((bound.Height - BandLength) / 2);
                var left = bound.Right;
                return new System.Windows.Point(left, top);
            }
        }
        public System.Windows.Point Point5
        {
            get
            {
                var bound = Bound;
                var top = bound.Top + ((bound.Height - BandLength) / 2) + BandLength;
                var left = bound.Right;
                return new System.Windows.Point(left, top);
            }
        }
        public System.Windows.Point Point6
        {
            get
            {
                var bound = Bound;
                var top = bound.Top + ((bound.Height - BandLength) / 2) + BandLength;
                var left = bound.Left + ((bound.Width - BandLength) / 2) + BandLength;
                return new System.Windows.Point(left, top);
            }
        }
        public System.Windows.Point Point7
        {
            get
            {
                var bound = Bound;
                var top = bound.Bottom;
                var left = bound.Left + ((bound.Width - BandLength) / 2) + BandLength;
                return new System.Windows.Point(left, top);
            }
        }
        public System.Windows.Point Point8
        {
            get
            {
                var bound = Bound;
                var top = bound.Bottom;
                var left = bound.Left + ((bound.Width - BandLength) / 2);
                return new System.Windows.Point(left, top);
            }
        }
        public System.Windows.Point Point9
        {
            get
            {
                var bound = Bound;
                var top = bound.Bottom - ((bound.Height - BandLength) / 2);
                var left = bound.Left + ((bound.Width - BandLength) / 2);
                return new System.Windows.Point(left, top);
            }
        }
        public System.Windows.Point Point10
        {
            get
            {
                var bound = Bound;
                var top = bound.Bottom - ((bound.Height - BandLength) / 2);
                var left = bound.Left;
                return new System.Windows.Point(left, top);
            }
        }
        public System.Windows.Point Point11
        {
            get
            {
                var bound = Bound;
                var top = bound.Top + ((bound.Height - BandLength) / 2);
                var left = bound.Left;
                return new System.Windows.Point(left, top);
            }
        }
        public System.Windows.Point Point12
        {
            get
            {
                var bound = Bound;
                var top = bound.Top + ((bound.Height - BandLength) / 2);
                var left = bound.Left + ((bound.Width - BandLength) / 2);
                return new System.Windows.Point(left, top);
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
                var index = (handleNumber - 1) / 8;
                var remainder = (handleNumber - 1) % 8 + 1;
                if (index == 0)
                {
                    return RenderTransform.ToDevice(GetRectHandle(Rect, remainder)).ToWindowPoint();
                }
                else
                {
                    index = (handleNumber - 9) / 4;
                    remainder = (handleNumber - 9) % 4 + 1;
                    if (index == 0)
                    {
                        return RenderTransform.ToDevice(GetLineHandle(Point1, Point2, remainder, index, ColumnWidth)).ToWindowPoint();
                    }
                    else if (index == 1)
                    {
                        return RenderTransform.ToDevice(GetLineHandle(Point4, Point5, remainder, index, ColumnWidth)).ToWindowPoint();
                    }
                    else if (index == 2)
                    {
                        return RenderTransform.ToDevice(GetLineHandle(Point7, Point8, remainder, index, ColumnWidth)).ToWindowPoint();
                    }
                    else if (index == 3)
                    {
                        return RenderTransform.ToDevice(GetLineHandle(Point10, Point11, remainder, index, ColumnWidth)).ToWindowPoint();
                    }
                }
            }
            catch (Exception ex)
            {

            }
            //throw new ArgumentOutOfRangeException($"无效的{nameof(handleNumber)}");

            return new Point2(0, 0);
        }
        internal static Core.Primitives.Point GetRectHandle(Core.Primitives.Rect rectangle, int handleNumber, bool includeCenterPoint = true)
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
                    x = rectangle.X;
                    y = rectangle.Y;
                    break;
                case 2:
                    x = xCenter;
                    y = rectangle.Y;
                    break;
                case 3:
                    x = rectangle.Right;
                    y = rectangle.Y;
                    break;
                case 4:
                    x = rectangle.Right;
                    y = yCenter;
                    break;
                case 5:
                    x = rectangle.Right;
                    y = rectangle.Bottom;
                    break;
                case 6:
                    x = xCenter;
                    y = rectangle.Bottom;
                    break;
                case 7:
                    x = rectangle.X;
                    y = rectangle.Bottom;
                    break;
                case 8:
                    x = rectangle.X;
                    y = yCenter;
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
        public override int HandleCount => 24;
        protected override void MoveHandleToInternal(int handleNumber, Point2 point)
        {
            var index = (handleNumber - 1) / 8;
            var remainder = (handleNumber - 1) % 8 + 1;
            if (index == 0)
            {
                base.MoveHandleToInternal(handleNumber, point);
            }
            else
            {
                index = (handleNumber - 9) / 4;
                remainder = (handleNumber - 9) % 4 + 1;

                System.Windows.Point pointDetail = new System.Windows.Point(0, 0);
                if (index == 0)
                {
                    pointDetail = GetPoint(Point1, Point2, remainder);
                }
                else if (index == 1)
                {
                    pointDetail = GetPoint(Point4, Point5, remainder);
                }
                else if (index == 2)
                {
                    pointDetail = GetPoint(Point7, Point8, remainder);
                }
                else if (index == 3)
                {
                    pointDetail = GetPoint(Point10, Point11, remainder);
                }

                var normalizedRect = MathUtil.GetNormalizedRectangle(pointDetail, new System.Windows.Point(point.X, point.Y));


                if (remainder == 1 || remainder == 4)
                {
                    if (index == 1 || index == 3)
                    {
                        if (normalizedRect.Height * 2<BandLength)
                        {
                            ColumnWidth = normalizedRect.Height * 2;
                        }                        
                    }
                    else
                    {
                        if (normalizedRect.Width * 2 < BandLength)
                        {
                            ColumnWidth = normalizedRect.Width * 2;
                        }
                    }
                }
                else
                {
                    if (index == 1 || index == 3)
                    {
                        if (ColumnWidth< normalizedRect.Height)
                        {
                            BandLength = normalizedRect.Height;
                        }
                    }
                    else
                    {
                        if (ColumnWidth < normalizedRect.Width)
                        {
                            BandLength = normalizedRect.Width;
                        } 
                    }
                }
            }
        }
        public  double CheckWidth(double width,double bandLength) 
        {
            if (width * 2 < bandLength + 1) 
            {
                return width;
            }
            throw new ArgumentOutOfRangeException($"无效的宽度");
        }
        protected static System.Windows.Point GetPoint(System.Windows.Point pointStart, System.Windows.Point pointEnd, int remainder) 
        {
            //1找2
            if (remainder == 1 || remainder==3)
            {
                return pointStart;
            }
            //2找3
            else if (remainder == 2 || remainder==4)
            {
                return pointEnd;
            }
            throw new ArgumentOutOfRangeException($"无效的点位");
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
            var index = (handleNumber - 1) / 8;
            if (index == 0)
            {
                if (handleNumber == 1 || handleNumber == 5)
                {
                    return Cursors.SizeNWSE;//  "\" 方向
                }
                else if (handleNumber == 2 || handleNumber == 6)
                {
                    return Cursors.SizeNS;//上下方向
                }
                else if (handleNumber == 3 || handleNumber == 7)
                {
                    return Cursors.SizeNESW;//  "/" 方向
                }
                else if (handleNumber == 4 || handleNumber == 8)
                {
                    return Cursors.SizeWE;//横向
                }
            }
            else
            {
                var remainder4 = handleNumber % 4;
                var remainder8 = handleNumber % 8;
                if (remainder8 >= 1 && remainder8 <= 4) 
                {
                    if (remainder4 == 1)
                    {
                        return Cursors.SizeNWSE;//  "\" 方向
                    }
                    else if (remainder4 == 0)
                    {
                        return Cursors.SizeNESW;//  "/" 方向
                    }
                    else if (remainder4 == 2)
                    {
                        return Cursors.SizeWE;//横向
                    }
                    else if (remainder4 == 3)
                    {
                        return Cursors.SizeWE;//横向
                    }
                }
                else
                {
                    if (remainder4 == 1)
                    {
                        return Cursors.SizeNESW;//  "/" 方向                    
                    }
                    else if (remainder4 == 0)
                    {
                        return Cursors.SizeNWSE;//  "\" 方向
                    }
                    else if (remainder4 == 2)
                    {
                        return Cursors.SizeNS;//上下方向
                    }
                    else if (remainder4 == 3)
                    {
                        return Cursors.SizeNS;//上下方向
                    }
                }
            }
            return base.GetCursorsType(handleNumber);
        }
    }
}
