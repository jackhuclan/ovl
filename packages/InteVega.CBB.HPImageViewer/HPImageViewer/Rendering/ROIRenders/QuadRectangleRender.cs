using HPImageViewer.Core.Persistence;
using HPImageViewer.Core.Primitives;
using HPImageViewer.Utils;
using System;
using System.Windows.Input;
using System.Windows.Media;
using Point = System.Windows.Point;

namespace HPImageViewer.Rendering.ROIRenders
{

    internal class QuadRectangleRender : BoxRender
    {
        public QuadRectangleRender(QuadRectangleDesc quadRectangleDesc) : base(quadRectangleDesc)
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

        public QuadRectangleDesc QuadRectangleDesc => this.ROIDesc as QuadRectangleDesc;
        public double BandPercent
        {
            get => QuadRectangleDesc.BandPercent / 100;
            set
            {
                QuadRectangleDesc.BandPercent = value * 100;
            }
        }
        public Rect LeftRect
        {
            get
            {
                var bound = Bound;
                var left = bound.Left;
                //var length = bound.Height* BandPercent;
                var length = (bound.Height - BandWidth) * BandPercent;
                var top = bound.Top + bound.Height / 2 - length / 2;
                return new Rect(left, top, BandWidth, length);
            }
        }
        public Rect TopRect
        {
            get
            {
                var bound = Bound;
                //var length = bound.Width * BandPercent;
                var length = (bound.Width - BandWidth) * BandPercent;
                var left = bound.Left + bound.Width / 2 - length / 2;
                var top = bound.Top;
                return new Rect(left, top, length, BandWidth);
            }
        }
        public Rect RightRect
        {

            get
            {
                var leftTopRect = LeftRect;
                var left = leftTopRect.Left + Rect.Width;
                var top = leftTopRect.Top;
                return new Rect(left, top, leftTopRect.Width, leftTopRect.Height);

            }
        }
        public Rect BottomRect
        {

            get
            {
                var topRect = TopRect;
                var left = topRect.Left;
                var top = Bound.Top + Rect.Height;
                return new Rect(left, top, topRect.Width, topRect.Height);

            }
        }


        /// <summary>Get handle point by 1-based number</summary>
        /// <param name="handleNumber"></param>
        /// <returns></returns>
        public override Point GetHandle(int handleNumber)
        {
            var index = (handleNumber - 1) / 8;
            var remainder = (handleNumber - 1) % 8 + 1;
            if (index == 0)
            {
                return RenderTransform.ToDevice(GetRectHandle(Rect, remainder)).ToWindowPoint();
            }
            else if (index == 1)
            {
                return RenderTransform.ToDevice(GetRectHandle(LeftRect, remainder)).ToWindowPoint();
            }
            else if (index == 2)
            {
                return RenderTransform.ToDevice(GetRectHandle(TopRect, remainder)).ToWindowPoint();
            }
            else if (index == 3)
            {
                return RenderTransform.ToDevice(GetRectHandle(RightRect, remainder)).ToWindowPoint();
            }
            else if (index == 4)
            {
                return RenderTransform.ToDevice(GetRectHandle(BottomRect, remainder)).ToWindowPoint();
            }

            throw new ArgumentOutOfRangeException($"无效的{nameof(handleNumber)}");
        }

        public override int HandleCount => 40;

        protected override void MoveHandleToInternal(int handleNumber, Point point)
        {
            //base.MoveHandleToInternal(handleNumber, point);
            var index = (handleNumber - 1) / 8;
            var remainder = (handleNumber - 1) % 8 + 1;
            if (index == 0)
            {
                base.MoveHandleToInternal(handleNumber, point);
                //Bound.Top
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

                var (l, t, w, h) = MoveRectangle(remainder, point, rect.Left, rect.Top, rect.Width, rect.Height);
                var normalizedRect = MathUtil.GetNormalizedRectangle(l, t, l + w, t + h);

                if (index == 1 || index == 3)
                {
                    BandWidth = normalizedRect.Width;
                    BandPercent = normalizedRect.Height / (Bound.Height - BandWidth);
                    //var Percent = normalizedRect.Height / (Bound.Height- BandWidth);
                    //Console.WriteLine($"Percent:{BandPercent}----,height:{normalizedRect.Height},总高度：{Bound.Height},----{Height}");
                }
                else
                {
                    BandWidth = normalizedRect.Height;
                    BandPercent = normalizedRect.Width / (Bound.Width - BandWidth);
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
            handleNumber = handleNumber % 8;
            handleNumber = handleNumber == 0 ? 8 : handleNumber;
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
            return base.GetCursorsType(handleNumber);
        }

    }


}
