using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Text;

namespace HPImageViewer.Core.Persistence
{
    public class RotatedPickersDesc: ROIDesc

    {
        /// <summary>
        /// 圆的中心X
        /// </summary>
        public double CenterX { get; set; } = 200d;

        /// <summary>
        /// 圆的中心Y
        /// </summary>
        public double CenterY { get; set; } = 200d;

        /// <summary>
        /// 大圆半径宽
        /// </summary>
        public double RadiusW { get; set; } = 100d;

        /// <summary>
        /// 大圆半径宽
        /// </summary>
        public double RadiusH { get; set; } = 100d;

        /// <summary>
        /// 每个矩阵的宽
        /// </summary>
        public double RectWidth { get; set; }
        /// <summary>
        /// 旋转长度
        /// </summary>
        public double SpinLength { get; set; }

        /// <summary>
        /// 每个矩阵的高
        /// </summary>
        public double RectHeight { get; set; }

        /// <summary>
        /// 拖拽矩形框的宽
        /// </summary>
        public double BoundRectWidth => RadiusW * 2;

        /// <summary>
        /// 拖拽矩形框的长
        /// </summary>
        public double BoundRectHeight => RadiusH * 2;

        /// <summary>
        /// 拖拽矩形框的X
        /// </summary>
        public double BoundRect_X => CenterX - RadiusW;

        /// <summary>
        /// 拖拽矩形框的Y
        /// </summary>
        public double BoundRect_Y => CenterY - RadiusH;

        /// <summary>
        /// 起始角度
        /// </summary>
        public double StartAngle { get; set; }

        /// <summary>
        /// 角度步长
        /// </summary>
        public double StepAngle => 360 / PickerCount;

        /// <summary>
        /// PickerCount
        /// </summary>
        public int PickerCount { get; set; }

        /// <summary>
        /// 是否显示圆
        /// </summary>
        public bool IsShowCircle = false;

        /// <summary>
        /// 是否倒转箭头
        /// </summary>
        public bool IsReverseArrow = false;
        /// <summary>
        /// 是否旋转
        /// </summary>
        public bool IsSpin = false;
        /// <summary>
        /// 是否启用巡边ROI
        /// </summary>
        public bool IsEdge = false;

        /// <summary>
        /// 用于边缘位置计算
        /// 从箭头方向开始累加
        /// </summary>
        public List<Primitives.Point> Points { get; set; }
        /// <summary>
        /// 边缘坐标位置
        /// </summary>
        public List<Primitives.Point> EdgePoints { get; set; }

        /// <summary>
        /// 获取RectangleCenters
        /// </summary>
        //public List<(double, double, double, bool)> GetRectagnelCenters()
        //{
        //    List<(double, double, double, bool)> centers = new List<(double, double, double, bool)>();
        //    double radius = this.Radius;
        //    double centerX = CenterX;
        //    double centerY = CenterY;

        //    // 转换角度为弧度
        //    for (double angle = StartAngle; angle < StartAngle + 360; angle += StepAngle)
        //    {
        //        double x = centerX + radius * Math.Cos(angle * Math.PI / 180);
        //        double y = centerY - radius * Math.Sin(angle * Math.PI / 180);
        //        centers.Add((x, y, angle, IsReverseArrow));
        //    }
        //    return centers;
        //}

        public List<(double, double, double, bool)> GetEllipseCenters()
        {
            if (IsSpin)
            {
                return GetEllipseCentersSpin();
            }

            List<(double, double, double, bool)> centers = new List<(double, double, double, bool)>();
            double centerX = CenterX;
            double centerY = CenterY;
            double semiMajorAxis = RadiusW;
            double semiMinorAxis = RadiusH;

            // 转换角度为弧度
            for (double angle = StartAngle; angle < StartAngle + 360; angle += StepAngle)
            {
                double radian = angle * Math.PI / 180;

                double x = centerX + semiMajorAxis * Math.Cos(radian);
                double y = centerY - semiMinorAxis * Math.Sin(radian);

                centers.Add((x, y, angle, IsReverseArrow));
            }
            return centers;
        }
        /// <summary>
        /// 旋转目前只支持PickerCount=2
        /// </summary>
        /// <returns></returns>
        public List<(double, double, double, bool)> GetEllipseCentersSpin() 
        {
            List<(double, double, double, bool)> centers = new List<(double, double, double, bool)>();
            double centerX = CenterX;
            double centerY = CenterY;
            double semiMajorAxis = RadiusW;
            double semiMinorAxis = RadiusH;

            // 转换角度为弧度
            for (double angle = StartAngle; angle < StartAngle + 360; angle += StepAngle)
            {
                double radian = angle * Math.PI / 180;
                double x = centerX + semiMajorAxis * Math.Cos(radian);
                double y = centerY + semiMajorAxis * Math.Sin(radian);
                centers.Add((x, y, angle, IsReverseArrow));
            }
            return centers;
        }
        //public (double l, double t, double w, double h, double angle) GetRectangle(int index)
        //{
        //    var angle = StartAngle + StepAngle * index;
        //    double centerX = CenterX + Radius * Math.Cos(angle * Math.PI / 180);
        //    double centerY = CenterY - Radius * Math.Sin(angle * Math.PI / 180);

        //    return (centerX - RectWidth / 2, centerY - RectHeight / 2, RectWidth, RectHeight, angle);
        //}

        public (double l, double t, double w, double h, double angle) GetRectangle(int index)
        {
            if (IsSpin)
            {
                return GetRectangleSpin(index);
            }
            var angle = StartAngle + StepAngle * index;
            double radian = angle * Math.PI / 180;
            double semiMajorAxis = RadiusW;
            double semiMinorAxis = RadiusH;

            double centerX = CenterX + semiMajorAxis * Math.Cos(radian);
            double centerY = CenterY - semiMinorAxis * Math.Sin(radian);

            return (centerX - RectWidth / 2, centerY - RectHeight / 2, RectWidth, RectHeight, angle);
        }
        public (double l, double t, double w, double h, double angle) GetRectangleSpin(int index)
        {
            var angle = StartAngle + StepAngle * index;
            double radian = angle * Math.PI / 180;
            double semiMajorAxis = RadiusW;
            double semiMinorAxis = RadiusH;

            double centerX = CenterX + semiMajorAxis * Math.Cos(radian);
            double centerY = CenterY + semiMajorAxis * Math.Sin(radian);

            return (centerX - RectWidth / 2, centerY - RectHeight / 2, RectWidth, RectHeight, angle);
        }
        /// <summary>
        /// 获取矩形边线中点
        /// </summary>
        /// <param name="index"></param>
        /// <returns>0：左边，1：上边，2：右边，3：下边</returns>
        public List<Primitives.Point> GetInnerRectStartEnd(int index)
        {
            var rect = GetRectangle(index);
            double xCenter = rect.l + rect.w / 2;
            double yCenter = rect.t + rect.h / 2;
            double widthHalf = rect.w / 2;
            double heightHalf = rect.h / 2;

            // 计算旋转后的角度
            double angleRadians = (IsSpin? rect.angle: -rect.angle) * Math.PI / 180;
            double cosAngle = Math.Cos(angleRadians);
            double sinAngle = Math.Sin(angleRadians);

            // 计算旋转后的四个中线点的坐标
            var rotatedPoints = new List<Core.Primitives.Point>
            {
                new Core.Primitives.Point(
                    xCenter + (rect.l - xCenter) * cosAngle - (rect.t + heightHalf - yCenter) * sinAngle,
                    yCenter + (rect.l - xCenter) * sinAngle + (rect.t + heightHalf - yCenter) * cosAngle
                ),
                new Core.Primitives.Point(
                    xCenter + (rect.l + widthHalf - xCenter) * cosAngle - (rect.t - yCenter) * sinAngle,
                    yCenter + (rect.l + widthHalf - xCenter) * sinAngle + (rect.t - yCenter) * cosAngle
                ),
                new Core.Primitives.Point(
                    xCenter + (rect.l + rect.w - xCenter) * cosAngle - (rect.t + heightHalf - yCenter) * sinAngle,
                    yCenter + (rect.l + rect.w - xCenter) * sinAngle + (rect.t + heightHalf - yCenter) * cosAngle
                ),
                new Core.Primitives.Point(
                    xCenter + (rect.l + widthHalf - xCenter) * cosAngle - (rect.t + rect.h - yCenter) * sinAngle,
                    yCenter + (rect.l + widthHalf - xCenter) * sinAngle + (rect.t + rect.h - yCenter) * cosAngle
                )
            };

            return rotatedPoints;
        }
        //public (double l, double t, double r, double b) GetRotatedStartCenterXY(int index)
        //{
        //    var angle = StartAngle + StepAngle * index;
        //    double centerX = CenterX + Radius * Math.Cos(angle * Math.PI / 180);
        //    double centerY = CenterY - Radius * Math.Sin(angle * Math.PI / 180);

        //    double deltaX1 = right - centerX;
        //    double deltaY1 = centerY - bottom;
        //    //double distance1 = Math.Sqrt(deltaX1 * deltaX1 + deltaY1 * deltaY1);

        //    return (rotatedl, rotatedt, rotatedr, rotatedb);
        //}
    }
}
