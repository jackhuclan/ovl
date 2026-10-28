using HPImageViewer.Core.Persistence;
using HPImageViewer.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using Rect = HPImageViewer.Core.Primitives.Rect;
namespace HPImageViewer.Rendering.ROIRenders
{
    public class RotatedPickersRender
        : ROIRender
    {
        public Rect BoundRect => MathUtil.GetNormalizedRectangle(RotatedPickersDesc.BoundRect_X,
                                                                 RotatedPickersDesc.BoundRect_Y,
                                                                 RotatedPickersDesc.BoundRect_X + RotatedPickersDesc.BoundRectWidth,
                                                                 RotatedPickersDesc.BoundRect_Y + RotatedPickersDesc.BoundRectHeight);

        public RotatedPickersDesc RotatedPickersDesc => ROIDesc as RotatedPickersDesc;

        public RotatedPickersRender(ROIDesc roiDesc) : base(roiDesc)
        {
            if (RotatedPickersDesc.Points == null)
            {
                RotatedPickersDesc.Points = new List<Core.Primitives.Point>();
                for (int i = 0; i < RotatedPickersDesc.PickerCount; i++)
                {
                    RotatedPickersDesc.Points.Add(new Core.Primitives.Point(20, 20));
                }
            }
            if (RotatedPickersDesc.EdgePoints == null)
            {
                RotatedPickersDesc.EdgePoints = new List<Core.Primitives.Point>();
                for (int i = 0; i < RotatedPickersDesc.PickerCount; i++)
                {
                    RotatedPickersDesc.EdgePoints.Add(new Core.Primitives.Point(0, 0));
                }
            }
        }
        public Rect RectSpin { get; set; } = new Rect();
        /// <summary>
        /// 单块区域点位个数
        /// </summary>
        public int RegionPointCount  => RotatedPickersDesc.IsEdge ? 5 : 4;
        //需要修改
        public override int HandleCount => RotatedPickersDesc.PickerCount * RegionPointCount + RegionPointCount; 

        protected override void OnROIRender(RenderContext renderContext)
        {
            var brush = Brush;
            brush.Freeze();
            var fillBrush = IsSelected ? FillBrush : null;
            if (fillBrush?.IsFrozen == false)
            {
                fillBrush.Freeze();
            }
            if (RotatedPickersDesc.IsSpin) 
            {
                DrawSpin(renderContext);
            }
            var centers = RotatedPickersDesc.GetEllipseCenters();
            int index = 0;
            foreach (var center in centers)
            {
                DrawBoundRect(renderContext);
                var centerPoint = new Point(center.Item1, center.Item2);
                var transformedCenterPoint = RenderTransform.ToDevice(centerPoint.ToPoint());
                var hV = new Vector(RotatedPickersDesc.RectWidth, 0);
                var vV = new Vector(0, RotatedPickersDesc.RectHeight);
                var matrix = Matrix.Identity;
                matrix.RotateAt(RotatedPickersDesc.IsSpin ? center.Item3 : -center.Item3, centerPoint.X, centerPoint.Y);
                var t = new RenderTransform(matrix);
                var transformedWidth = RenderTransform.ToDevice(t.ToDevice(hV)).Length;
                var transformedHeight = RenderTransform.ToDevice(t.ToDevice(vV)).Length;
                var pen = new Pen(brush, RotatedPickersDesc.StrokeThickness);
                pen.Freeze();
                var transformRect = new Core.Primitives.Rect(transformedCenterPoint.X - transformedWidth / 2, transformedCenterPoint.Y - transformedHeight / 2, transformedWidth, transformedHeight);
                renderContext.DrawingContext.DrawRectangle(fillBrush, pen, transformRect, RotatedPickersDesc.IsSpin ? center.Item3 : -center.Item3);

                DrawArrows(renderContext,
                    RotatedPickersDesc.IsSpin ? center.Item3 : -center.Item3,
                    transformRect,
                    pen,
                    center.Item4, index);
                //启用巡边ROI
                if (RotatedPickersDesc.IsEdge)
                {
                    var rectCross = RotatedPickersDesc.GetRectangle(index);
                    DrawCross(renderContext, pen, new Core.Primitives.Rect(rectCross.l, rectCross.t, rectCross.w, rectCross.h), RotatedPickersDesc.IsSpin ? center.Item3 : -center.Item3, index);
                }

                index++;
                if (RotatedPickersDesc.IsShowCircle)
                {
                    DrawCircle(renderContext);
                }
            }
        }
        /// <summary>
        /// 绘制圆
        /// </summary>
        /// <param name="renderContext"></param>
        private void DrawCircle(RenderContext renderContext)
        {
            Pen pen1 = new Pen(Brushes.Brown, 1);
            pen1.DashStyle = new DashStyle(new double[] { 5, 5 }, 0); // 设置虚线样式
            var radiusW = RenderTransform.ToDevice(new Vector(RotatedPickersDesc.RadiusW, 0)).Length;
            var radiusH = RenderTransform.ToDevice(new Vector(RotatedPickersDesc.RadiusH, 0)).Length;

            var centerCircle = new Core.Primitives.Point(RotatedPickersDesc.CenterX, RotatedPickersDesc.CenterY);
            var centerCircleTransformed = RenderTransform.ToDevice(centerCircle);
            if (RotatedPickersDesc.IsSpin)
            {
                renderContext.DrawingContext.DrawEllipse(null, pen1, centerCircleTransformed, radiusW, radiusH, RotatedPickersDesc.StartAngle);
            }
            else
            {
                renderContext.DrawingContext.DrawEllipse(null, pen1, centerCircleTransformed, radiusW, radiusH);
            }
            
        }
        /// <summary>
        /// 绘制方框
        /// </summary>
        /// <param name="renderContext"></param>
        private void DrawBoundRect(RenderContext renderContext)
        {
            if (RotatedPickersDesc.IsSpin)
            {
                renderContext.DrawingContext.DrawRectangle(Brushes.Transparent, new Pen(FillBrush, ROIDesc.StrokeThickness)
                {
                    DashStyle = new DashStyle(new double[] { 4, 4 }, 0)
                }, RenderTransform.ToDevice(BoundRect), RotatedPickersDesc.StartAngle);
            }
            else
            {
                renderContext.DrawingContext.DrawRegularRectangle(Brushes.Transparent, new Pen(FillBrush, ROIDesc.StrokeThickness)
                {
                    DashStyle = new DashStyle(new double[] { 4, 4 }, 0)
                }, RenderTransform.ToDevice(BoundRect));
            }
        }
        /// <summary>
        /// 绘制箭头
        /// </summary>
        /// <param name="renderContext"></param>
        /// <param name="angle"></param>
        /// <param name="rect"></param>
        /// <param name="pen"></param>
        /// <param name="isReverse"></param>
        private void DrawArrows(RenderContext renderContext, double angle, Core.Primitives.Rect rect, Pen pen, bool isReverse, int index)
        {
            var arrowPoints = new List<Core.Primitives.Point>
                {
                    new Core.Primitives.Point(rect.Left, (rect.Top + rect.Bottom) / 2),
                    new Core.Primitives.Point(rect.Right, (rect.Top + rect.Bottom) / 2),
                    new Core.Primitives.Point(rect.Right - 5, (rect.Top + rect.Bottom) / 2 - 5),
                    new Core.Primitives.Point(rect.Right - 5, (rect.Top + rect.Bottom) / 2 + 5),
                };

            var matrix = Matrix.Identity;
            if (isReverse)
            {
                matrix.RotateAt(angle + 180, (rect.Left + rect.Right) / 2, (rect.Top + rect.Bottom) / 2);
            }
            else
            {
                matrix.RotateAt(angle, (rect.Left + rect.Right) / 2, (rect.Top + rect.Bottom) / 2);
            }
            var t = new RenderTransform(matrix);
            arrowPoints = arrowPoints.Select(s => t.ToDevice(s)).ToList();

            renderContext.DrawingContext.DrawLine(pen, arrowPoints[0], arrowPoints[1]);
            renderContext.DrawingContext.DrawLine(pen, arrowPoints[1], arrowPoints[2]);
            renderContext.DrawingContext.DrawLine(pen, arrowPoints[2], arrowPoints[3]);
            renderContext.DrawingContext.DrawLine(pen, arrowPoints[3], arrowPoints[1]);

        }
        /// <summary>
        /// 绘制十字
        /// </summary>
        /// <param name="renderContext"></param>
        /// <param name="pen"></param>
        /// <param name="rectangle"></param>
        /// <param name="angle"></param>
        /// <param name="index"></param>
        private void DrawCross(RenderContext renderContext, Pen pen, Rect rectangle, double angle, int index)
        {
            double x, y, xCenter, yCenter;
            xCenter = rectangle.X + rectangle.Width / 2;
            yCenter = rectangle.Y + rectangle.Height / 2;

            var matrix = Matrix.Identity;
            matrix.RotateAt(RotatedPickersDesc.IsSpin ? -angle : angle, xCenter, yCenter);
            var t1 = new RenderTransform(matrix);
            x = RotatedPickersDesc.Points[index].X;
            y = RotatedPickersDesc.Points[index].Y;
            var rectPoint = t1.ToDevice(new Core.Primitives.Point(rectangle.Left + x, rectangle.Top + y));
            Point point = RenderTransform.ToDevice(rectPoint).ToWindowPoint();
            var p1 = new Core.Primitives.Point(point.X, point.Y - 12);
            var p2 = new Core.Primitives.Point(point.X, point.Y + 12);
            renderContext.DrawingContext.DrawLine(pen, p1, p2);
            var p3 = new Core.Primitives.Point(point.X - 12, point.Y);
            var p4 = new Core.Primitives.Point(point.X + 12, point.Y);
            renderContext.DrawingContext.DrawLine(pen, p3, p4);
            //保存坐标数据
            RotatedPickersDesc.EdgePoints[index] = new Core.Primitives.Point(point.X, point.Y);
        }

        /// <summary>
        /// 绘制旋转
        /// </summary>
        /// <param name="renderContext"></param>
        private void DrawSpin(RenderContext renderContext)
        {
            ImageBrush imageBrush = new ImageBrush();
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri("pack://application:,,,/HPImageViewer;Component/resources/Spin.png", UriKind.Absolute);
            bitmap.EndInit();
            imageBrush.ImageSource = bitmap;
            double lengthZ = ((RotatedPickersDesc.RadiusW) * (RotatedPickersDesc.RadiusW))*2;
            double z = Math.Sqrt(lengthZ);
            var (spinX, spinY) = GetPointByRadiusAndAngle(RotatedPickersDesc.CenterX, RotatedPickersDesc.CenterY, z + RotatedPickersDesc.SpinLength, RotatedPickersDesc.StartAngle);
            RectSpin.Width = 30;
            RectSpin.Height = 30;
            RectSpin.X = spinX - 15;           
            RectSpin.Y = spinY;

            renderContext.DrawingContext.DrawImage(imageBrush.ImageSource, RenderTransform.ToDevice(RectSpin));
        }
        double _angle = 0;
        public static (double x, double y) GetPointByRadiusAndAngle(double centerX, double centerY, double radius, double angle)
        {
            // 角度转弧度
            double radians = angle * (Math.PI / 180);
            // 计算坐标
            double x = centerX + radius * Math.Cos(radians);
            double y = centerY + radius * Math.Sin(radians);
            return (x, y);
        }

        public override int HitTest(Point point)
        {
            if (IsSelected)
            {
                for (int i = 1; i <= HandleCount; i++)
                {
                    if (GetHandlePoint(i).Contains(point))
                        return i;
                }
            }
            if (PointInSpin(point))
            {
                return 99999;
            }
            if (PointInObject(point))
                return 0;

            return -1;
        }
        private bool PointInSpin(Point devicePoint)
        {
            return RenderTransform.ToDevice(RectSpin).Contains(devicePoint.ToPoint());
        }
        private bool PointInObject(Point devicePoint)
        {
            return RenderTransform.ToDevice(BoundRect).Contains(devicePoint.ToPoint());
        }
        //int num = 0;
        public override Point GetHandle(int handleNumber)
        {
            var index = (handleNumber - 1) / RegionPointCount;
            var remainder = (handleNumber - 1) % RegionPointCount + 1;
            if (index == 0)
            {
                //var rectangle= RenderTransform.ToDevice(BoundRect);
                if (RotatedPickersDesc.IsSpin)
                {
                    var rect = new RectangleGeometry() { Rect = new System.Windows.Rect(BoundRect.X, BoundRect.Y, BoundRect.Width, BoundRect.Height), Transform = new RotateTransform(RotatedPickersDesc.StartAngle, BoundRect.X + BoundRect.Width / 2, BoundRect.Y + BoundRect.Height / 2) };
                    return RenderTransform.ToDevice(GetRectHandle(BoundRect, rect.Transform, remainder)).ToWindowPoint();
                }
                else
                {
                    return RenderTransform.ToDevice(GetRectHandle(BoundRect, remainder)).ToWindowPoint();
                }
            }
            else if (index == 1)
            {
                var rect = RotatedPickersDesc.GetRectangle(0);
                if (remainder<5)
                {                   
                    return RenderTransform.ToDevice(GetInnerRectHandle(new Core.Primitives.Rect(rect.l,
                                                                                                rect.t,
                                                                                                rect.w,
                                                                                                rect.h),
                                                                       rect.angle,
                                                                       remainder))
                                           .ToWindowPoint();
                }
                //else
                //{
                //    return RenderTransform.ToDevice(GetEdgeRectHandle(new Core.Primitives.Rect(rect.l,
                //                                                                                    rect.t,
                //                                                                                    rect.w,
                //                                                                                    rect.h),
                //                                                           rect.angle,
                //                                                           index-1))
                //                               .ToWindowPoint();
                //}
            }

            if(index!=0)
            {
                if (remainder > 4)
                {

                    var rect = RotatedPickersDesc.GetRectangle(index - 1);
                    return RenderTransform.ToDevice(GetEdgeRectHandle(new Core.Primitives.Rect(rect.l,
                                                                                                    rect.t,
                                                                                                    rect.w,
                                                                                                    rect.h),
                                                                           rect.angle,
                                                                           index - 1))
                                               .ToWindowPoint();
                }
            }
            //获取位置信息
            //if (handleNumber>= HandleCount - RotatedPickersDesc.PickerCount)
            //{
            //    RotatedPickersDesc.Points = new List<System.Drawing.Point>();

            //    return RenderTransform.ToDevice(new Core.Primitives.Point(100, 100)).ToWindowPoint();
            //}

            return default;
        }

        private Core.Primitives.Point GetRectHandle(Core.Primitives.Rect rectangle, Transform transform, int handleNumber, bool includeCenterPoint = true)
        {
            double x, y, xCenter, yCenter;
            xCenter = rectangle.X + rectangle.Width / 2;
            yCenter = rectangle.Y + rectangle.Height / 2;
            x = transform.Transform(new Point(rectangle.TopLeft.X, rectangle.TopLeft.Y)).X;
            y = transform.Transform(new Point(rectangle.TopLeft.X, rectangle.TopLeft.Y)).Y;

            if (includeCenterPoint == false)
            {
                handleNumber = (handleNumber - 1) * 2 + 1;
            }

            switch (handleNumber)
            {
                case 1:
                    x = transform.Transform(new Point(rectangle.TopLeft.X, rectangle.TopLeft.Y)).X;
                    y = transform.Transform(new Point(rectangle.TopLeft.X, rectangle.TopLeft.Y)).Y;
                    break;
                case 2:
                    x = transform.Transform(new Point(rectangle.TopRight.X, rectangle.TopRight.Y)).X;
                    y = transform.Transform(new Point(rectangle.TopRight.X, rectangle.TopRight.Y)).Y;
                    break;
                case 3:
                    x = transform.Transform(new Point(rectangle.BottomRight.X, rectangle.BottomRight.Y)).X;
                    y = transform.Transform(new Point(rectangle.BottomRight.X, rectangle.BottomRight.Y)).Y;
                    break;
                case 4:
                    x = transform.Transform(new Point(rectangle.BottomLeft.X, rectangle.BottomLeft.Y)).X;
                    y = transform.Transform(new Point(rectangle.BottomLeft.X, rectangle.BottomLeft.Y)).Y;
                    break;
            }
            
            return new Core.Primitives.Point(x, y);
        }

        private Core.Primitives.Point GetRectHandle(Core.Primitives.Rect rectangle, int handleNumber, bool includeCenterPoint = true)
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
                    x = rectangle.Right;
                    y = rectangle.Y;
                    break;
                case 3:
                    x = rectangle.Right;
                    y = rectangle.Bottom;
                    break;
                case 4:
                    x = rectangle.X;
                    y = rectangle.Bottom;
                    break;
            }
            return new Core.Primitives.Point(x, y);
        }

        private Core.Primitives.Point GetInnerRectHandle(Rect rectangle, double angle, int handleNumber)
        {
            double x, y, xCenter, yCenter;
            xCenter = rectangle.X + rectangle.Width / 2;
            yCenter = rectangle.Y + rectangle.Height / 2;
            _angle = angle;
            var matrix = Matrix.Identity;
            matrix.RotateAt(RotatedPickersDesc.IsSpin ? angle : -angle, xCenter, yCenter);
            var t1 = new RenderTransform(matrix);

            var rectPoints = new List<Core.Primitives.Point>
                {
                    new Core.Primitives.Point(rectangle.Left, rectangle.Top + rectangle.Height / 2),
                    new Core.Primitives.Point(rectangle.Left + rectangle.Width / 2, rectangle.Top),
                    new Core.Primitives.Point(rectangle.Right, rectangle.Top + rectangle.Height / 2),
                    new Core.Primitives.Point(rectangle.Left + rectangle.Width / 2, rectangle.Bottom),
                };
            rectPoints = rectPoints.Select(s => t1.ToDevice(s)).ToList();
            return rectPoints[handleNumber - 1];
        }
        private Core.Primitives.Point GetEdgeRectHandle(Rect rectangle, double angle,int index)
        {
            double x, y, xCenter, yCenter;
            xCenter = rectangle.X + rectangle.Width / 2;
            yCenter = rectangle.Y + rectangle.Height / 2;

            var matrix = Matrix.Identity;
            matrix.RotateAt(RotatedPickersDesc.IsSpin ? angle : -angle, xCenter, yCenter);
            var t1 = new RenderTransform(matrix);
            x = RotatedPickersDesc.Points[index].X;
            y = RotatedPickersDesc.Points[index].Y;
            var rectPoint = t1.ToDevice( new Core.Primitives.Point(rectangle.Left+x, rectangle.Top+y));
            return rectPoint;
        }

        protected override void MoveToInternal(double wx, double wy)
        {
            RotatedPickersDesc.CenterX += wx;
            RotatedPickersDesc.CenterY += wy;
        }

        protected override void MoveHandleToInternal(int handleNumber, Point point)
        {
            if (handleNumber==99999)
            {
                // 两个点的坐标
                double x1 = RotatedPickersDesc.CenterX, y1 = RotatedPickersDesc.CenterY; // 点 P1 的坐标
                double x2 = point.X, y2 = point.Y; // 点 P2 的坐标
                // 计算两个点之间的差值
                double dx = x2 - x1;
                double dy = y2 - y1;
                // 使用 Atan2 计算夹角（弧度）
                double angleRadians = Math.Atan2(dy, dx);
                // 将弧度转换为角度
                double angleDegrees = angleRadians * (180.0 / Math.PI);
                RotatedPickersDesc.StartAngle = angleDegrees;//旋转只给出角度
                return;
            }
            base.MoveHandleToInternal(handleNumber, point);
            int index = (handleNumber - 1) / RegionPointCount;
            int remainder = (handleNumber - 1) % RegionPointCount + 1;

            if (index == 0)
            {
                var rect = CalcMovedRectangle(remainder,
                              point,
                              BoundRect.Left,
                              BoundRect.Top,
                              BoundRect.Width,
                              BoundRect.Height);
                var wOffset = (rect.w / 2) - RotatedPickersDesc.RadiusW;
                var hOffset = (rect.h / 2) - RotatedPickersDesc.RadiusH;
                RotatedPickersDesc.RadiusW = rect.w / 2;
                RotatedPickersDesc.RadiusH = rect.h / 2;
                if (!RotatedPickersDesc.IsSpin)
                {
                    RotatedPickersDesc.CenterX = (rect.l + (rect.l + rect.w)) / 2;
                    RotatedPickersDesc.CenterY = (rect.t + (rect.t + rect.h)) / 2;

                }
                else
                {
                    RotatedPickersDesc.CenterX = rect.l;
                    RotatedPickersDesc.CenterY = rect.t;
                }
            }
            else
            {
                var rectangle = RotatedPickersDesc.GetRectangle(index-1);

                //这边需要调整
                //当出现点位的时候需要单点执行
                var rect1 = CalcMovedInnerRectangle(remainder,
                                                    point,
                                                    rectangle.l,
                                                    rectangle.t,
                                                    rectangle.w,
                                                    rectangle.h,
                                                    rectangle.angle,index-1);
                RotatedPickersDesc.RectWidth = rect1.w;
                RotatedPickersDesc.RectHeight = rect1.h;
            }
        }
        private (double l, double t, double w, double h) CalcMovedRectangle(int handleNumber, Point point, double left, double top, double width, double height, bool includeCenterPoint = true)
        {
            if (RotatedPickersDesc.IsSpin)
            {
                return CalcMovedRectangle(handleNumber,point,includeCenterPoint);
            }

            var x = point.X;
            var y = point.Y;

            //var left = rect.Left;
            //var top = rect.Top;
            var right = left + width;
            var bottom = top + height;

            if (includeCenterPoint == false)
            {
                handleNumber = (handleNumber - 1) * 2 + 1;
            }
            switch (handleNumber)
            {
                case 1:
                    left = x;
                    top = y;
                    break;
                case 2:
                    right = x;
                    top = y;
                    break;
                case 3:
                    right = x;
                    bottom = y;
                    break;
                case 4:
                    left = x;
                    bottom = y;
                    break;
            }

            width = right - left;
            height = bottom - top;
            return (left, top, width, height);
        }
        private (double x,double y, double w, double h) CalcMovedRectangle(int handleNumber, Point point, bool includeCenterPoint = true)
        {
            double width = 0, height = 0;
            double centerX, centerY=0;
            var x = point.X;
            var y = point.Y;
            //var rectangle = RenderTransform.ToDevice(BoundRect);
            Transform transform = new RotateTransform( RotatedPickersDesc.StartAngle, BoundRect.X + BoundRect.Width / 2, BoundRect.Y + BoundRect.Height / 2);

            //获取转换后的坐标点位
            Point topLeft = transform.Transform(new Point(BoundRect.TopLeft.X, BoundRect.TopLeft.Y));
            Point topRight = transform.Transform(new Point(BoundRect.TopRight.X, BoundRect.TopRight.Y));
            Point bottomRight = transform.Transform(new Point(BoundRect.BottomRight.X, BoundRect.BottomRight.Y));
            Point bottomLeft = transform.Transform(new Point(BoundRect.BottomLeft.X, BoundRect.BottomLeft.Y));

            if (includeCenterPoint == false)
            {
                handleNumber = (handleNumber - 1) * 2 + 1;
            }
            switch (handleNumber)
            {
                case 1:
                    width = Math.Sqrt(Math.Pow(topRight.X - x, 2) + Math.Pow(topRight.Y - y, 2));
                    height = Math.Sqrt(Math.Pow(bottomLeft.X - x, 2) + Math.Pow(bottomLeft.Y - y, 2));
                    break;
                case 2:
                    width = Math.Sqrt(Math.Pow(x - topLeft.X, 2) + Math.Pow(y - topLeft.Y, 2));
                    height = Math.Sqrt(Math.Pow(bottomRight.X - x, 2) + Math.Pow(bottomRight.Y - y, 2));
                    break;
                case 3:
                    width = Math.Sqrt(Math.Pow(x - bottomLeft.X, 2) + Math.Pow(y - bottomLeft.Y, 2));
                    height = Math.Sqrt(Math.Pow(x - topRight.X, 2) + Math.Pow(y - topRight.Y, 2));
                    break;
                case 4:
                    width = Math.Sqrt(Math.Pow(bottomRight.X - x, 2) + Math.Pow(bottomRight.Y - y, 2));
                    height = Math.Sqrt(Math.Pow(x - topLeft.X, 2) + Math.Pow(y - topLeft.Y, 2));
                    break;
            }
            double hypotenuse = Math.Sqrt(Math.Pow(width / 2, 2) + Math.Pow(height / 2, 2));

            centerX = (topRight.X + bottomLeft.X) / 2;
            centerY = (topRight.Y + bottomLeft.Y) / 2;
            return (centerX,centerY,width, height);
        }


        private (double l, double t, double w, double h) CalcMovedInnerRectangle(int handleNumber, Point point, double left, double top, double width, double height, double angle, int index)
        {
            var x = point.X;
            var y = point.Y;

            var right = left + width;
            var bottom = top + height;
            var centerX = (left + right) / 2;
            var centerY = (top + bottom) / 2;

            Matrix matrix = Matrix.Identity;
            matrix.RotateAt(-angle, centerX, centerY);
            var t1 = new RenderTransform(matrix);
            var rotatedPoint = t1.ToDevice(new Core.Primitives.Point(x, y));
            var diagonal = MathUtil.GetDistance(rotatedPoint.X, rotatedPoint.Y, centerX, centerY);

            double x1 = centerX + diagonal * Math.Cos((180) * Math.PI / 180);
            double x2 = centerX + diagonal * Math.Cos(Math.PI / 180);

            double y1 = centerY - diagonal * Math.Sin((90) * Math.PI / 180);
            double y2 = centerY - diagonal * Math.Sin((270) * Math.PI / 180);

            switch (handleNumber)
            {
                case 1:
                    left = x1;
                    //Console.WriteLine($"x:{x},x1:{x1}");

                    break;
                case 2:
                    top = y1;
                    //Console.WriteLine($"y:{y},y1:{y1}");
                    break;
                case 3:
                    right = x2;
                    //Console.WriteLine($"x:{x},x2:{x2}");
                    break;
                case 4:
                    bottom = y2;
                    //Console.WriteLine($"y:{y},y2:{y2}");
                    break;
                case 5:
                    bool isInRange = CheckPointRange(point, left, top, width, height, angle);
                    if (!isInRange) break;

                    //移动点位
                    Matrix matrix1 = Matrix.Identity;
                    matrix1.RotateAt(angle, (left + right) / 2, (top + bottom) / 2);
                    var t = new RenderTransform(matrix1);
                    var points = t.ToDevice(new Core.Primitives.Point(point.X, point.Y));
                    RotatedPickersDesc.Points[index].X = points.X - left;
                    RotatedPickersDesc.Points[index].Y = points.Y - top;

                    break;
            }

            width = right - left;
            height = bottom - top;
            return (left, top, width, height);
        }
        /// <summary>
        /// 判断点位是否在范围内
        /// </summary>
        /// <param name="point"></param>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="angle"></param>
        /// <returns></returns>
        static bool CheckPointRange(Point point,double left, double top, double width, double height,double angle) 
        {
            var right = left + width;
            var bottom = top + height;
            var arrowPoints = new List<Core.Primitives.Point>
                    {
                        new Core.Primitives.Point(left, top),
                        new Core.Primitives.Point(right, top),
                        new Core.Primitives.Point(right, bottom),
                        new Core.Primitives.Point(left, bottom),
                    };
            var matrixNew = Matrix.Identity;
            matrixNew.RotateAt(-angle, (left + right) / 2, (top + bottom) / 2);
            var t2 = new RenderTransform(matrixNew);
            arrowPoints = arrowPoints.Select(s => t2.ToDevice(s)).ToList();
            List<Point> pointList = new List<Point>();
            foreach (var item in arrowPoints)
            {
                pointList.Add(new Point(item.X, item.Y));
            }
            return IsPointInQuadrilateral(point, pointList.ToArray());
        }

        /// <summary>
        /// 判断点是否在四边形内部（射线法）
        /// </summary>
        /// <param name="p"></param>
        /// <param name="quad"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        static bool IsPointInQuadrilateral(Point p, Point[] quad)
        {
            if (quad.Length != 4)
                throw new ArgumentException("四边形必须有四个点");
            int intersections = 0;
            int count = quad.Length;
            for (int i = 0; i < count; i++)
            {
                Point p1 = quad[i];
                Point p2 = quad[(i + 1) % count]; // 形成封闭四边形
                // 判断射线是否与边相交
                if ((p.Y > Math.Min(p1.Y, p2.Y)) && (p.Y <= Math.Max(p1.Y, p2.Y)) &&
                    (p.X <= Math.Max(p1.X, p2.X)))
                {
                    double xIntersection;
                    if (p1.Y != p2.Y)
                    {
                        xIntersection = (p.Y - p1.Y) * (p2.X - p1.X) / (p2.Y - p1.Y) + p1.X;
                    }
                    else
                    {
                        xIntersection = p1.X; // 水平边
                    }
                    if (p.X < xIntersection)
                    {
                        intersections++;
                    }
                }
            }
            return (intersections % 2 == 1); // 奇数交点 => 在内部
        }

        public override bool GetROIRegion(double x, double y)
        {
            if (BoundRect.Top < y && (BoundRect.Top + BoundRect.Height) > y && BoundRect.Left < x && (BoundRect.Left + BoundRect.Width) > x)
            {
                return true;
            }
            return false;
        }
        public override Cursor GetCursorsType(int handleNumber)
        {
            if (handleNumber == 1 || handleNumber == 3)
            {
                
                return Cursors.SizeNWSE;//  "\" 方向
            }
            else if (handleNumber == 2 || handleNumber == 4)
            {
                return Cursors.SizeNESW;//  "/" 方向
            }
            else if (handleNumber == 6 || handleNumber == 8)
            {
                if ((_angle >= 90 && _angle<180) || (_angle>=270 && _angle<360) )
                {
                    return Cursors.SizeWE;
                }
                return Cursors.SizeNS;//上下方向
            }
            else if (handleNumber == 7 || handleNumber == 5)
            {
                if ((_angle >= 90 && _angle < 180) || (_angle >= 270 && _angle < 360))
                {
                    return Cursors.SizeNS;
                }
                return Cursors.SizeWE;//横向
            }
            return base.GetCursorsType(handleNumber);
        }
    }
}
