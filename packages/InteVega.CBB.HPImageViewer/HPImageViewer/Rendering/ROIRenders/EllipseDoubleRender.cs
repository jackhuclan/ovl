using HPImageViewer.Core.Persistence;
using HPImageViewer.Utils;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;

namespace HPImageViewer.Rendering.ROIRenders
{
    public class EllipseDoubleRender : ROIRender
    {
        public EllipseDoubleRender() : this(new EllipseDoubleDesc())
        {

        }


        public EllipseDoubleRender(EllipseDoubleDesc ellipseDesc) : base(ellipseDesc)
        {
        }
        SolidColorBrush _fillBrush;
        protected SolidColorBrush FillBrush
        {
            get
            {
                if (_fillBrush == null)
                {
                    _fillBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                    _fillBrush.Opacity = 0.0;
                }
                return _fillBrush;
            }
        }
        SolidColorBrush _fillBrushOther;
        protected SolidColorBrush FillBrushOther
        {
            get
            {
                if (_fillBrushOther == null)
                {
                    _fillBrushOther = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color));
                    _fillBrushOther.Opacity = 0.2;
                }
                return _fillBrushOther;
            }
        }
        private EllipseDoubleDesc EllipseDesc
        {
            get => (EllipseDoubleDesc)ROIDesc;
            set
            {
                if (ROIDesc != value)
                {
                    ROIDesc = value;
                    OnPropertyChanged();
                }

            }
        }

        public double CenterX
        {
            get => EllipseDesc.CenterX;
            set
            {
                if (EllipseDesc.CenterX != value)
                {
                    EllipseDesc.CenterX = value;
                    OnPropertyChanged();
                }

            }
        }

        public double CenterY
        {
            get => EllipseDesc.CenterY;
            set
            {
                if (EllipseDesc.CenterY != value)
                {
                    EllipseDesc.CenterY = value;
                    OnPropertyChanged();
                }

            }
        }

        public double R
        {
            get => EllipseDesc.R;
            set
            {
                if (EllipseDesc.R != value)
                {
                    EllipseDesc.R = value;
                    OnPropertyChanged();
                }

            }
        }
        public double BandLength
        {
            get => EllipseDesc.BandLength;
            set
            {
                if (EllipseDesc.BandLength != value)
                {
                    EllipseDesc.BandLength = value;
                    OnPropertyChanged();
                }
            }
        }

        public HPImageViewer.Core.Primitives.Rect Rectangle => new HPImageViewer.Core.Primitives.Rect(CenterX - Math.Abs(R + BandLength), CenterY - Math.Abs(R + BandLength), 2 * Math.Abs(R + BandLength), 2 * Math.Abs(R + BandLength));
        private HPImageViewer.Core.Primitives.Rect DeviceRectangle => RenderTransform.ToDevice(Rectangle);
        protected bool PointInObject(Point devicePoint)
        {
            return DeviceRectangle.Contains(devicePoint.ToPoint());
        }
        RenderContext _renderContext;
        protected override void OnROIRender(RenderContext renderContext)
        {
            var brush = Brush;
            brush.Freeze();
            var fillBrush = IsSelected ? FillBrush : null;
            if (fillBrush?.IsFrozen == false)
            {
                fillBrush.Freeze();
            }
            var center = new Point(CenterX, CenterY);
            var transformedCenter = RenderTransform.ToDevice(center.ToPoint());
            //renderContext.DrawingContext.DrawEllipse(fillBrush, new Pen(Brush, this.ROIDesc.StrokeThickness), transformedCenter, R * renderContext.Scale, R * renderContext.Scale);
            _renderContext = renderContext;
            renderContext.DrawingContext.DrawEllipse(fillBrush, new Pen(Brush, this.ROIDesc.StrokeThickness), transformedCenter, (R  -(BandLength / 2)) * renderContext.Scale, (R - (BandLength / 2)) * renderContext.Scale );
            renderContext.DrawingContext.DrawEllipse(fillBrush, new Pen(Brush, this.ROIDesc.StrokeThickness), transformedCenter, (R + (BandLength / 2)) * renderContext.Scale, (R + (BandLength / 2)) * renderContext.Scale);
            var deviceBandWidth = BandLength * renderContext.Scale;
            renderContext.DrawingContext.DrawEllipse(Brushes.Transparent, new Pen(FillBrushOther, deviceBandWidth), transformedCenter, R * renderContext.Scale, R * renderContext.Scale);
        }
        public override int HitTest(Point point)
        {
            var band = BandLength < 0 ? -BandLength : BandLength;
            if (!MathUtil.IsPointInCircle(RenderTransform.ToDomain(point.ToPoint()), new Core.Primitives.Point(CenterX, CenterY), R+ band))
            {
                return -1;
            }
            if (MathUtil.IsPointInCircle(RenderTransform.ToDomain(point.ToPoint()), new Core.Primitives.Point(CenterX, CenterY), R - (band / 2) - (ROIDesc.StrokeThickness / 2)))
            {
                return 0;
            }

            if (IsSelected)
            {
                for (int i = 1; i <= HandleCount; i++)
                {
                    if (GetHandlePoint(i).Contains(point))
                        return i;
                }
                return 0;
            }
            return -1;
        }

        /// <summary>
        /// Get handle point by 1-based number
        /// </summary>
        /// <param name="handleNumber"></param>
        /// <returns></returns>
        public override Point GetHandle(int handleNumber)
        {
            var index = (handleNumber - 1) / 4;
            var remainder = (handleNumber - 1) % 4 + 1;

            var rectangle = Rectangle;
            double x, y, xCenter, yCenter;
            xCenter = rectangle.X + rectangle.Width / 2;
            yCenter = rectangle.Y + rectangle.Height / 2;
            x = rectangle.X;
            y = rectangle.Y;
            //var band = BandLength > 0 ? BandLength : -BandLength;
            if (index==0)
            {
                DrawPoint(remainder, rectangle, ref x, ref y, xCenter, yCenter, (BandLength / 2));
            }
            else if (index==1) 
            {
                DrawPoint(remainder, rectangle, ref x, ref y, xCenter, yCenter, BandLength);
            }
            else if (index==2)
            {
                DrawPoint(remainder, rectangle, ref x, ref y, xCenter, yCenter, BandLength + (BandLength / 2));               
            }           
            return RenderTransform.ToDevice(new Point(x, y).ToPoint()).ToWindowPoint();
        }

        private static void DrawPoint(int remainder, Core.Primitives.Rect rectangle, ref double x, ref double y, double xCenter, double yCenter,double bandLength)
        {
            switch (remainder)
            {
                case 1:
                    x = xCenter;
                    y = rectangle.Y+ bandLength;
                    break;
                case 2:
                    x = rectangle.Right- bandLength;
                    y = yCenter;
                    break;
                case 3:
                    x = xCenter;
                    y = rectangle.Bottom- bandLength;
                    break;
                case 4:
                    x = rectangle.X+ bandLength;
                    y = yCenter;
                    break;
            }
        }

        public override int HandleCount => 12;

        protected override void MoveToInternal(double wx, double wy)
        {
            CenterX += wx;
            if (CenterX - R < 0)
                CenterX = R;
            if (CenterX + R > ImageWidth)
                CenterX = ImageWidth - R;
            CenterY += wy;
            if (CenterY - R < 0)
                CenterY = R;
            if (CenterY + R > ImageHeight)
                CenterY = ImageHeight - R;
        }

        protected override bool NeedRender(RenderContext renderContext)
        {
            return true;// RectangleRender.NeedRender(new Rect(0, 0, renderContext.RenderSize.Width, renderContext.RenderSize.Height), new Rect(DeviceRectangle.X, DeviceRectangle.Y, DeviceRectangle.Width, DeviceRectangle.Height), EllipseDesc);
        }
        protected override void MoveHandleToInternal(int handleNumber, Point point)
        {
            var index = (handleNumber - 1) / 4;
            var remainder = (handleNumber - 1) % 4 + 1;
            var x = point.X;
            var y = point.Y;
            if (x < 0) x = 0;
            if (x > ImageWidth) x = ImageWidth;
            if (y < 0) y = 0;
            if (y > ImageHeight) y = ImageHeight;
            var centerVector = new Vector(CenterX, CenterY);
            var currentVector = new Vector(x, y);
            var band = ((centerVector - currentVector).Length - R) * 2;
            band = band > 0 ? band : -band;
            if (band / 2 > R || (R + band / 2) <= R)
            {
                return;
            }

            if (index == 0 || index == 2)
            {
                BandLength = band;
            }
            else if (index == 1)
            {
                currentVector = new Vector(x, y);
                R = (centerVector - currentVector).Length;
            }
        }

        internal override bool IntersectsWith(Rect rect)
        {
            return MathUtil.CircleIntersectsRectangle(CenterX, CenterY, R + ROIDesc.StrokeThickness / 2, rect);
        }
        public override bool GetROIRegion(double x, double y)
        {
            Point point=new Point(x, y);
            var band = BandLength < 0 ? -BandLength : BandLength;
            if (MathUtil.IsPointInCircle(RenderTransform.ToDomain(point.ToPoint()), new Core.Primitives.Point(CenterX, CenterY), R))
            {
                return true;
            }
            return false;
        }
        public override Cursor GetCursorsType(int handleNumber)
        {

            var index = handleNumber % 4;
            if (index == 1 || index==3)
            {
                return Cursors.SizeNS;//竖
            }
            else
            {
                return Cursors.SizeWE;//横向
            }
        }
    }
}
