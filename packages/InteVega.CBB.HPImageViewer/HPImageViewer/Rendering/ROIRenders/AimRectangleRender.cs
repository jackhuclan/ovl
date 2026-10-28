using HPImageViewer.Core.Persistence;
using HPImageViewer.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using HPImageViewer.Core.Primitives;
using Point = System.Windows.Point;
using System.Windows.Documents;
using System.Windows.Input;

namespace HPImageViewer.Rendering.ROIRenders
{
    internal class AimRectangleRender : BoxRender
    {
        public AimRectangleRender(AimRectangleDesc aimRectangleDesc) : base(aimRectangleDesc)
        {

        }

        protected override void OnROIRender(RenderContext renderContext)
        {
            //  base.OnROIRender(renderContext);

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

            renderContext.DrawingContext.DrawRegularRectangle(fillBrush, new Pen(brush, ROIDesc.StrokeThickness), RenderTransform.ToDevice(LeftRect));
            renderContext.DrawingContext.DrawRegularRectangle(fillBrush, new Pen(brush, ROIDesc.StrokeThickness), RenderTransform.ToDevice(TopRect));
            renderContext.DrawingContext.DrawRegularRectangle(fillBrush, new Pen(brush, ROIDesc.StrokeThickness), RenderTransform.ToDevice(RightRect));
            renderContext.DrawingContext.DrawRegularRectangle(fillBrush, new Pen(brush, ROIDesc.StrokeThickness), RenderTransform.ToDevice(BottomRect));
        }

        public AimRectangleDesc AimRectangleDesc => this.ROIDesc as AimRectangleDesc;
        //public double BandLength
        //{
        //    get => AimRectangleDesc.BandLength;
        //    set
        //    {
        //        if (AimRectangleDesc.BandLength != value)
        //        {
        //            AimRectangleDesc.BandLength = value;
        //            OnPropertyChanged();
        //        }

        //    }
        //}
        public double OffsetX
        {
            get => AimRectangleDesc.OffsetX;
            set
            {
                if (AimRectangleDesc.OffsetX != value)
                {
                    AimRectangleDesc.OffsetX = value;
                    OnPropertyChanged();
                }
            }
        }
        public double OffsetY
        {
            get => AimRectangleDesc.OffsetY;
            set
            {
                if (AimRectangleDesc.OffsetY != value)
                {
                    AimRectangleDesc.OffsetY = value;
                    OnPropertyChanged();
                }
            }
        }
        public double RegionWidth
        {
            get => AimRectangleDesc.RegionWidth;
            set
            {
                if (AimRectangleDesc.RegionWidth != value)
                {
                    AimRectangleDesc.RegionWidth = value;
                    OnPropertyChanged();
                }
            }
        }
        public double RegionHeight
        {
            get => AimRectangleDesc.RegionHeight;
            set
            {
                if (AimRectangleDesc.RegionHeight != value)
                {
                    AimRectangleDesc.RegionHeight = value;
                    OnPropertyChanged();
                }
            }
        }

        public Rect LeftRect
        {
            get
            {
                var bound = Bound;
                var cententX = bound.Left + bound.Width / 2;
                var cententY = bound.Top + bound.Height / 2;
                var x = cententX - OffsetX;
                var y = cententY - OffsetY;
                var left = x - RegionWidth;
                var top = y-RegionHeight;
                return new Rect(left, top, RegionWidth, RegionHeight);
            }
        }
        public Rect TopRect
        {
            get
            {
                var bound = Bound;
                var cententX = bound.Left + bound.Width / 2;
                var cententY = bound.Top + bound.Height / 2;
                var x = cententX + OffsetY;
                var y = cententY - OffsetX;
                var left = x;
                var top = y - RegionWidth;
                return new Rect(left, top, RegionHeight , RegionWidth);

            }
        }
        public Rect RightRect
        {
            get
            {
                var bound = Bound;
                var cententX = bound.Left + bound.Width / 2;
                var cententY = bound.Top + bound.Height / 2;
                var x = cententX + OffsetX;
                var y = cententY + OffsetY;
                var left = x;
                var top = y;
                return new Rect(left, top, RegionWidth, RegionHeight);
            }
        }
        public Rect BottomRect
        {
            get
            {
                var bound = Bound;
                var cententX = bound.Left + bound.Width / 2;
                var cententY = bound.Top + bound.Height / 2;
                var x = cententX - OffsetY;
                var y = cententY + OffsetX;
                var left = x-RegionHeight;
                var top = y;
                return new Rect(left, top, RegionHeight, RegionWidth);
            }
        }
        public Core.Primitives.Point LeftPoint
        {
            get { 
                return new Core.Primitives.Point(LeftRect.X + (RegionWidth / 2), LeftRect.Y + (RegionHeight / 2)); 
            }
        }


        /// <summary>Get handle point by 1-based number</summary>
        /// <param name="handleNumber"></param>
        /// <returns></returns>
        public override Point GetHandle(int handleNumber)
        {
            var index = (handleNumber - 1) / 9;
            var remainder = (handleNumber - 1) % 9 + 1;
            if (index == 0)
            {
                if (remainder == 9)
                    return RenderTransform.ToDevice(new Core.Primitives.Point(Rect.X + (Rect.Width / 2), Rect.Y + (Rect.Height / 2))).ToWindowPoint();
                return RenderTransform.ToDevice(GetRectHandle(Rect, remainder)).ToWindowPoint();
            }
            else if (index == 1)
            {
                if (remainder == 9)
                    return RenderTransform.ToDevice(new Core.Primitives.Point(LeftRect.X + (RegionWidth / 2), LeftRect.Y + (RegionHeight / 2))).ToWindowPoint();

                return RenderTransform.ToDevice(GetRectHandle(LeftRect, remainder)).ToWindowPoint();
            }
            else if (index == 2)
            {
                if (remainder == 9)
                    return RenderTransform.ToDevice(new Core.Primitives.Point(TopRect.X + (RegionHeight / 2), TopRect.Y + (RegionWidth / 2))).ToWindowPoint();
                return RenderTransform.ToDevice(GetRectHandle(TopRect, remainder)).ToWindowPoint();
            }
            else if (index == 3)
            {
                if (remainder == 9)
                    return RenderTransform.ToDevice(new Core.Primitives.Point(RightRect.X + (RegionWidth / 2), RightRect.Y + (RegionHeight / 2))).ToWindowPoint();
                return RenderTransform.ToDevice(GetRectHandle(RightRect, remainder)).ToWindowPoint();
            }
            else if (index == 4)
            {
                if (remainder == 9)
                    return RenderTransform.ToDevice(new Core.Primitives.Point(BottomRect.X + (RegionHeight / 2), BottomRect.Y + (RegionWidth / 2))).ToWindowPoint();
                return RenderTransform.ToDevice(GetRectHandle(BottomRect, remainder)).ToWindowPoint();
            }



            throw new ArgumentOutOfRangeException($"无效的{nameof(handleNumber)}");
        }

        public override int HandleCount => 45;


        protected override void MoveHandleToInternal(int handleNumber, Point point)
        {
            //base.MoveHandleToInternal(handleNumber, point);
            var index = (handleNumber - 1) / 9;
            var remainder = (handleNumber - 1) % 9 + 1;
            if (index == 0)
            {
                base.MoveHandleToInternal(handleNumber, point);
            }
            else
            {
                Rect rect;
                if (index == 1)
                {
                    rect = LeftRect;
                }
                else if (index == 2)
                {
                    rect = TopRect;
                }
                else if (index == 3)
                {
                    rect = RightRect;
                }
                else if (index == 4)
                {
                    rect = BottomRect;
                }
                else
                {
                    throw new ArgumentOutOfRangeException($"无效的{nameof(index)}");
                }
                if (remainder != 9)
                {
                    var (l, t, w, h) = MoveRectangle(remainder, point, rect.Left, rect.Top, rect.Width, rect.Height);
                    var normalizedRect = MathUtil.GetNormalizedRectangle(l, t, l + w, t + h);
                    Point centerPoint = new Point(Rect.X + (Rect.Width / 2), Rect.Y + (Rect.Height / 2));
                     var normalizedRectOffset = MathUtil.GetNormalizedRectangle(point, centerPoint);
                    if (index == 1 || index == 3)
                    {
                        if (index == 1 && remainder == 5)
                        {
                            if ((centerPoint.X - point.X) > 0 && (centerPoint.Y - point.Y) > 0)
                            {
                                OffsetX = normalizedRectOffset.Width;
                                OffsetY = normalizedRectOffset.Height;
                                RegionWidth = normalizedRect.Width;
                                RegionHeight = normalizedRect.Height;
                            }
                        }
                        else if (index == 1 && remainder == 3)
                        {
                            if ((centerPoint.X - point.X) > 0 && (centerPoint.X - point.X) > 0)
                            {
                                OffsetX = normalizedRectOffset.Width;
                                RegionWidth = normalizedRect.Width;
                                RegionHeight = normalizedRect.Height;
                            }
                        }
                        else if (index == 1 && remainder == 4)
                        {
                            if ((centerPoint.X - point.X) > 0 && (centerPoint.X - point.X) > 0)
                            {
                                OffsetX = normalizedRectOffset.Width;
                                RegionWidth = normalizedRect.Width;
                            }
                        }
                        else if (index == 1 && remainder == 6)
                        {
                            if ((centerPoint.X - point.X) > 0 && (centerPoint.Y - point.Y) > 0)
                            {
                                OffsetY = normalizedRectOffset.Height;
                                RegionHeight = normalizedRect.Height;
                            }
                        }
                        else if (index == 1 && remainder == 7)
                        {
                            if ((centerPoint.X - point.X) > 0 && (centerPoint.Y - point.Y) > 0)
                            {
                                OffsetY = normalizedRectOffset.Height;
                                RegionWidth = normalizedRect.Width;
                                RegionHeight = normalizedRect.Height;
                            }
                        }
                        else if (index == 3 && remainder == 1)
                        {
                            if (-(centerPoint.X - point.X) > 0 && -(centerPoint.Y - point.Y) > 0)
                            {
                                OffsetX = normalizedRectOffset.Width;
                                OffsetY = normalizedRectOffset.Height;
                                RegionWidth = normalizedRect.Width;
                                RegionHeight = normalizedRect.Height;

                            }
                        }
                        else if (index == 3 && remainder == 2)
                        {
                            if (-(centerPoint.X - point.X) > 0 && -(centerPoint.Y - point.Y) > 0)
                            {
                                OffsetY = normalizedRectOffset.Height;
                                RegionHeight = normalizedRect.Height;
                            }
                        }
                        else if (index == 3 && remainder == 3)
                        {
                            if (-(centerPoint.X - point.X) > 0 && -(centerPoint.Y - point.Y) > 0)
                            {
                                OffsetY = normalizedRectOffset.Height;
                                RegionWidth = normalizedRect.Width;
                                RegionHeight = normalizedRect.Height;
                            }
                        }
                        else if (index == 3 && remainder == 8)
                        {
                            if (-(centerPoint.X - point.X) > 0 && -(centerPoint.Y - point.Y) > 0)
                            {
                                OffsetX = normalizedRectOffset.Width;
                                RegionWidth = normalizedRect.Width;
                            }
                        }

                        else
                        {
                            RegionWidth = normalizedRect.Width;
                            RegionHeight = normalizedRect.Height;
                        }
                    }
                    else
                    {
                        if (index == 2 && remainder == 7)
                        {
                            if (-(centerPoint.X - point.X) > 0 && (centerPoint.Y - point.Y) > 0)
                            {
                                OffsetX = normalizedRectOffset.Height;
                                OffsetY = normalizedRectOffset.Width;
                                RegionWidth = normalizedRect.Height;
                                RegionHeight = normalizedRect.Width;
                            }
                        }
                        else if (index == 2 && remainder == 8)
                        {
                            if (-(centerPoint.X - point.X) > 0 && (centerPoint.Y - point.Y) > 0)
                            {
                                OffsetY = normalizedRectOffset.Width;
                                RegionHeight = normalizedRect.Width;
                            }
                        }
                        else if (index == 2 && remainder == 1)
                        {
                            if (-(centerPoint.X - point.X) > 0 && (centerPoint.Y - point.Y) > 0)
                            {
                                OffsetY = normalizedRectOffset.Width;
                                RegionWidth = normalizedRect.Height;
                                RegionHeight = normalizedRect.Width;
                            }
                        }
                        else if (index == 2 && remainder == 5)
                        {
                            if (-(centerPoint.X - point.X) > 0 && (centerPoint.Y - point.Y) > 0)
                            {
                                OffsetX = normalizedRectOffset.Height;
                                RegionWidth = normalizedRect.Height;
                                RegionHeight = normalizedRect.Width;
                            }
                        }
                        else if (index == 2 && remainder == 6)
                        {
                            if (-(centerPoint.X - point.X) > 0 && (centerPoint.Y - point.Y) > 0)
                            {
                                OffsetX = normalizedRectOffset.Height;
                                RegionWidth = normalizedRect.Height;
                            }
                        }

                        else if (index == 4 && remainder == 3)
                        {
                            if ((centerPoint.X - point.X) > 0 && -(centerPoint.Y - point.Y) > 0)
                            {
                                OffsetX = normalizedRectOffset.Height;
                                OffsetY = normalizedRectOffset.Width;
                                RegionWidth = normalizedRect.Height;
                                RegionHeight = normalizedRect.Width;
                            }
                        }
                        else if (index == 4 && remainder == 4)
                        {
                            if ((centerPoint.X - point.X) > 0 && -(centerPoint.Y - point.Y) > 0)
                            {
                                OffsetY = normalizedRectOffset.Width;
                                RegionHeight = normalizedRect.Width;
                            }
                        }
                        else if (index == 4 && remainder == 5)
                        {
                            if ((centerPoint.X - point.X) > 0 && -(centerPoint.Y - point.Y) > 0)
                            {
                                OffsetY = normalizedRectOffset.Width;
                                RegionWidth = normalizedRect.Height;
                                RegionHeight = normalizedRect.Width;
                            }
                        }
                        else if (index == 4 && remainder == 1)
                        {
                            if ((centerPoint.X - point.X) > 0 && -(centerPoint.Y - point.Y) > 0)
                            {
                                OffsetX = normalizedRectOffset.Height;
                                RegionWidth = normalizedRect.Height;
                                RegionHeight = normalizedRect.Width;
                            }
                        }
                        else if (index == 4 && remainder == 2)
                        {
                            if ((centerPoint.X - point.X) > 0 && -(centerPoint.Y - point.Y) > 0)
                            {
                                OffsetX = normalizedRectOffset.Height;
                                RegionWidth = normalizedRect.Height;
                            }
                        }

                        else
                        {
                            RegionWidth = normalizedRect.Height;
                            RegionHeight = normalizedRect.Width;
                        }

                    }
                }
                else
                {
                    var normalizedRect = MathUtil.GetNormalizedRectangle(point, new Point(Rect.X + (Rect.Width / 2), Rect.Y + (Rect.Height / 2)));

                    if (index == 1|| index ==3)
                    {
                        OffsetX = normalizedRect.Width - (RegionWidth / 2);
                        OffsetY = normalizedRect.Height - (RegionHeight / 2);
                    }
                    else if (index==2|| index==4) 
                    {
                        OffsetX = normalizedRect.Height - (RegionWidth / 2);
                        OffsetY = normalizedRect.Width - (RegionHeight / 2);
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
                handleNumber = handleNumber % 9;
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
            return base.GetCursorsType(handleNumber);
        }
    }
}
