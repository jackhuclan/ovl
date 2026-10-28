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
using System.Windows.Automation.Provider;

namespace HPImageViewer.Rendering.ROIRenders
{
    internal class AimXYSRender : BoxRender
    {
        public AimXYSRender(AimXYSDesc aimRectangleDesc) : base(aimRectangleDesc)
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

            renderContext.DrawingContext.DrawRegularRectangle(fillBrush, new Pen(brush, ROIDesc.StrokeThickness), RenderTransform.ToDevice(LeftTopRect));
            renderContext.DrawingContext.DrawRegularRectangle(fillBrush, new Pen(brush, ROIDesc.StrokeThickness), RenderTransform.ToDevice(RightBottomRect));
        }

        public AimXYSDesc AimRectangleDesc => this.ROIDesc as AimXYSDesc;
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

        public Rect LeftTopRect
        {
            get
            {
                var bound = Bound;
                var cententX = bound.Left + bound.Width / 2;
                var cententY = bound.Top + bound.Height / 2;
                var x = cententX - OffsetX;
                var y = cententY - OffsetY;
                var left = x - RegionWidth;
                var top = y - RegionHeight;
                return new Rect(left, top, RegionWidth, RegionHeight,false);
            }
        }
        public Rect RightBottomRect
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
                return new Rect(left, top, RegionWidth, RegionHeight,false);
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
                    return RenderTransform.ToDevice(new Core.Primitives.Point(LeftTopRect.X + (RegionWidth / 2), LeftTopRect.Y + (RegionHeight / 2))).ToWindowPoint();

                return RenderTransform.ToDevice(GetRectHandle(LeftTopRect, remainder)).ToWindowPoint();
            }
            else if (index == 2)
            {
                if (remainder == 9)
                    return RenderTransform.ToDevice(new Core.Primitives.Point(RightBottomRect.X + (RegionWidth / 2), RightBottomRect.Y + (RegionHeight / 2))).ToWindowPoint();
                return RenderTransform.ToDevice(GetRectHandle(RightBottomRect, remainder)).ToWindowPoint();
            }
            throw new ArgumentOutOfRangeException($"无效的{nameof(handleNumber)}");
        }

        public override int HandleCount => 27;


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
                    rect = LeftTopRect;
                }
                else if (index == 2)
                {
                    rect = RightBottomRect;
                }
                else
                {
                    throw new ArgumentOutOfRangeException($"无效的{nameof(index)}");
                }
                if (remainder != 9)
                {
                    var (l, t, w, h) = MoveRectangle(remainder, point, rect.Left, rect.Top, rect.Width, rect.Height);
                    var normalizedRect = MathUtil.GetNormalizedRectangle(l, t, l + w, t + h, false);
                    Point centerPoint = new Point(Rect.X + (Rect.Width / 2), Rect.Y + (Rect.Height / 2));
                    var normalizedRectOffset = MathUtil.GetNormalizedRectangle(point, centerPoint, false);
                    if (index == 1 || index == 2)
                    {
                        if (index == 1 && remainder == 5)
                        {
                            OffsetX = normalizedRectOffset.Width;
                            OffsetY = normalizedRectOffset.Height;
                            RegionWidth = normalizedRect.Width;
                            RegionHeight = normalizedRect.Height;
                        }
                        else if (index == 1 && remainder == 3)
                        {
                            OffsetX = normalizedRectOffset.Width;
                            RegionWidth = normalizedRect.Width;
                            RegionHeight = normalizedRect.Height;
                        }
                        else if (index == 1 && remainder == 4)
                        {
                            OffsetX = normalizedRectOffset.Width;
                            RegionWidth = normalizedRect.Width;
                        }
                        else if (index == 1 && remainder == 6)
                        {
                            OffsetY = normalizedRectOffset.Height;
                            RegionHeight = normalizedRect.Height;
                        }
                        else if (index == 1 && remainder == 7)
                        {
                            OffsetY = normalizedRectOffset.Height;
                            RegionWidth = normalizedRect.Width;
                            RegionHeight = normalizedRect.Height;
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
                            OffsetX = normalizedRectOffset.Height;
                            OffsetY = normalizedRectOffset.Width;
                            RegionWidth = normalizedRect.Height;
                            RegionHeight = normalizedRect.Width;
                        }
                        else if (index == 2 && remainder == 8)
                        {
                            OffsetY = normalizedRectOffset.Width;
                            RegionHeight = normalizedRect.Width;
                        }
                        else if (index == 2 && remainder == 1)
                        {
                            OffsetY = normalizedRectOffset.Width;
                            RegionWidth = normalizedRect.Height;
                            RegionHeight = normalizedRect.Width;
                        }
                        else if (index == 2 && remainder == 5)
                        {
                            OffsetX = normalizedRectOffset.Height;
                            RegionWidth = normalizedRect.Height;
                            RegionHeight = normalizedRect.Width;
                        }
                        else if (index == 2 && remainder == 6)
                        {
                            OffsetX = normalizedRectOffset.Height;
                            RegionWidth = normalizedRect.Height;
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
                    var normalizedRect = MathUtil.GetNormalizedRectangle(point, new Point(Rect.X + (Rect.Width / 2), Rect.Y + (Rect.Height / 2)), false);

                    if (index == 1)
                    {
                        OffsetX = normalizedRect.Width - (RegionWidth / 2);
                        OffsetY = normalizedRect.Height - (RegionHeight / 2);
                    }
                    else if (index == 2)
                    {
                        OffsetX = normalizedRect.Width - (RegionWidth / 2);
                        OffsetY = normalizedRect.Height - (RegionHeight / 2);
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
                else if (handleNumber==4 || handleNumber==8)
                {
                    return Cursors.SizeWE;//横向
                }
            }
            return base.GetCursorsType(handleNumber);
        }
    }
}
