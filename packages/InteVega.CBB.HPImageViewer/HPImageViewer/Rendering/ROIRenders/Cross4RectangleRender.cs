using HPImageViewer.Core.Persistence;
using HPImageViewer.Core.Primitives;
using HPImageViewer.Utils;
using System;
using System.Windows.Input;
using System.Windows.Media;
using Point = System.Windows.Point;

namespace HPImageViewer.Rendering.ROIRenders
{

    internal class Cross4RectangleRender : BoxRender
    {
        public Cross4RectangleRender(Cross4RectangleDesc quadRectangleDesc) : base(quadRectangleDesc)
        {

        }

        protected override void OnROIRender(RenderContext renderContext)
        {

            //base.OnROIRender(renderContext);
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

            renderContext.DrawingContext.DrawRegularRectangle(fillBrush, new Pen(brush, ROIDesc.StrokeThickness), RenderTransform.ToDevice(LeftVerticalRect));
            renderContext.DrawingContext.DrawRegularRectangle(fillBrush, new Pen(brush, ROIDesc.StrokeThickness), RenderTransform.ToDevice(LeftTopRect));
            renderContext.DrawingContext.DrawRegularRectangle(fillBrush, new Pen(brush, ROIDesc.StrokeThickness), RenderTransform.ToDevice(RightVerticalRect));
            renderContext.DrawingContext.DrawRegularRectangle(fillBrush, new Pen(brush, ROIDesc.StrokeThickness), RenderTransform.ToDevice(RightTopRect));
            renderContext.DrawingContext.DrawRegularRectangle(fillBrush, new Pen(brush, ROIDesc.StrokeThickness), RenderTransform.ToDevice(LeftBottomVerticalRect));
            renderContext.DrawingContext.DrawRegularRectangle(fillBrush, new Pen(brush, ROIDesc.StrokeThickness), RenderTransform.ToDevice(LeftBottomRect));
            renderContext.DrawingContext.DrawRegularRectangle(fillBrush, new Pen(brush, ROIDesc.StrokeThickness), RenderTransform.ToDevice(RightBottomRect));
            renderContext.DrawingContext.DrawRegularRectangle(fillBrush, new Pen(brush, ROIDesc.StrokeThickness), RenderTransform.ToDevice(RightBottomVerticalRect));
        }

        public Cross4RectangleDesc towRectangleDesc => this.ROIDesc as Cross4RectangleDesc;
        public double BandPercent
        {
            get => towRectangleDesc.BandPercent / 100;
            set
            {
                towRectangleDesc.BandPercent = value * 100;
            }
        }

        /// <summary>
        /// 左上竖着的矩形
        /// </summary>
        public Rect LeftVerticalRect
        {
            get
            {
                var bound = Bound;
                var left = bound.Left;
                var length = (bound.Height - BandWidth) * BandPercent / 2;
                var top = bound.Top + BandWidth/2;
                return new Rect(left, top, BandWidth, length);
            }
        }

        /// <summary>
        /// 左上横着的矩形
        /// </summary>
        public Rect LeftTopRect
        {
            get
            {
                var bound = Bound;
                var length = (bound.Width - BandWidth) * BandPercent / 2;
                var left = bound.Left + BandWidth/2;
                var top = bound.Top;
                return new Rect(left, top, length, BandWidth);
            }
        }

        /// <summary>
        /// 右上竖着的矩形
        /// </summary>
        public Rect RightVerticalRect
        {

            get
            {
                var bound = Bound;
                var length = (bound.Height - BandWidth) * BandPercent / 2;
                var right = bound.Right - BandWidth;
                var top = bound.Top + BandWidth / 2;
                return new Rect(right, top, BandWidth, length);
            }
        }


        /// <summary>
        /// 右上横着的矩形
        /// </summary>
        public Rect RightTopRect
        {

            get
            {
                var bound = Bound;
                var length = (bound.Width - BandWidth) * BandPercent / 2;
                var right = bound.Right - length - BandWidth/2;
                var top = bound.Top;
                return new Rect(right, top, length, BandWidth);
            }
        }

        /// <summary>
        /// 左下竖着的矩形
        /// </summary>
        public Rect LeftBottomVerticalRect
        {
            get
            {
                var bound = Bound;
                var left = bound.Left;
                var length = (bound.Height - BandWidth) * BandPercent / 2;
                var bottom = bound.Bottom - BandWidth / 2 - length;
                return new Rect(left, bottom, BandWidth, length);
            }
        }

        /// <summary>
        /// 左下横着的矩形
        /// </summary>
        public Rect LeftBottomRect
        {
            get
            {
                var bound = Bound;
                var length = (bound.Width - BandWidth) * BandPercent / 2;
                var left = bound.Left + BandWidth / 2;
                var top = bound.Bottom - BandWidth;
                return new Rect(left, top, length, BandWidth);
            }
        }

        /// <summary>
        /// 右下竖着的矩形
        /// </summary>
        public Rect RightBottomVerticalRect
        {

            get
            {
                var bound = Bound;
                var length = (bound.Height - BandWidth) * BandPercent / 2;
                var right = bound.Right - BandWidth;
                var top = bound.Bottom - BandWidth / 2- length;
                return new Rect(right, top, BandWidth, length);
            }
        }


        /// <summary>
        /// 右上横着的矩形
        /// </summary>
        public Rect RightBottomRect
        {

            get
            {
                var bound = Bound;
                var length = (bound.Width - BandWidth) * BandPercent / 2;
                var right = bound.Right - length - BandWidth / 2;
                var top = bound.Bottom - BandWidth;
                return new Rect(right, top, length, BandWidth);
            }
        }


        public Rect BottomRect
        {

            get
            {
                var topRect = RightTopRect;
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
                return RenderTransform.ToDevice(GetRectHandle(LeftVerticalRect, remainder)).ToWindowPoint();
            }
            else if (index == 2)
            {
                return RenderTransform.ToDevice(GetRectHandle(LeftTopRect, remainder)).ToWindowPoint();
            }
            else if (index == 3)
            {
                return RenderTransform.ToDevice(GetRectHandle(RightVerticalRect, remainder)).ToWindowPoint();
            }
            else if (index == 4)
            {
                return RenderTransform.ToDevice(GetRectHandle(RightTopRect, remainder)).ToWindowPoint();
            }
            else if (index == 5)
            {
                return RenderTransform.ToDevice(GetRectHandle(LeftBottomVerticalRect, remainder)).ToWindowPoint();
            }
            else if (index == 6)
            {
                return RenderTransform.ToDevice(GetRectHandle(LeftBottomRect, remainder)).ToWindowPoint();
            }
            else if (index == 7)
            {
                return RenderTransform.ToDevice(GetRectHandle(RightBottomVerticalRect, remainder)).ToWindowPoint();
            }
            else if (index == 8)
            {
                return RenderTransform.ToDevice(GetRectHandle(RightBottomRect, remainder)).ToWindowPoint();
            }
            throw new ArgumentOutOfRangeException($"无效的{nameof(handleNumber)}");
        }

        public override int HandleCount => 72;

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
                    rect = LeftVerticalRect;
                }
                else if (index == 2)
                {
                    rect = LeftTopRect;
                }
                else if (index == 3)
                {
                    rect = RightVerticalRect;
                }
                else if (index == 4)
                {
                    rect = RightTopRect;
                }
                else if(index == 5)
                {
                    rect = LeftBottomVerticalRect;
                }
                else if (index == 6)
                {
                    rect = LeftBottomRect;
                }
                else if (index == 7)
                {
                    rect = RightBottomVerticalRect;
                }
                else if (index == 8)
                {
                    rect = RightBottomRect;
                }
                else
                {
                    throw new ArgumentOutOfRangeException($"无效的{nameof(index)}");
                }

                var (l, t, w, h) = MoveRectangle(remainder, point, rect.Left, rect.Top, rect.Width, rect.Height);
                var normalizedRect = MathUtil.GetNormalizedRectangle(l, t, l + w, t + h);

                if (index == 1 || index == 3 || index == 5 || index == 7)
                {

                    //if (normalizedRect.Width * 2 > BandWidth)
                    //{
                    //    normalizedRect.Width = BandWidth / 2;
                    //}
                    BandWidth = normalizedRect.Width;
                    BandPercent = normalizedRect.Height * 2 / (Bound.Height - BandWidth);
                    if (BandPercent < 0.1)
                    {
                        BandPercent = 0.1;
                    }
                    if (BandPercent > 1)
                    {
                        BandPercent = 1;
                    }
                    //var Percent = normalizedRect.Height / (Bound.Height- BandWidth);
                    //Console.WriteLine($"Percent:{BandPercent}----,height:{normalizedRect.Height},总高度：{Bound.Height},----{Height}");
                }
                else
                {
                    //if (normalizedRect.Height * 2 > BandWidth)
                    //{
                    //    normalizedRect.Height = BandWidth / 2;
                    //}
                    BandWidth = normalizedRect.Height;
                    BandPercent = normalizedRect.Width * 2 / (Bound.Width - BandWidth);
                    if (BandPercent <0.1 )
                    {
                        BandPercent = 0.1;
                    }
                    if (BandPercent > 1)
                    {
                        BandPercent = 1;
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
