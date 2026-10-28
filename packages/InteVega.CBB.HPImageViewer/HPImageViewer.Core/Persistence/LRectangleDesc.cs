using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text;

namespace HPImageViewer.Core.Persistence
{
    /// <summary>
    /// L 型 ROI 方向
    /// </summary>
    public enum LShapeDirection
    {
        TopLeft,      // 左上角 L（└）
        TopRight,     // 右上角 L（┘）
        BottomLeft,   // 左下角 L（┌）
        BottomRight   // 右下角 L（┐）
    }

    /// <summary>
    /// L 型 ROI 描述类（支持4方向）
    /// </summary>
    public class LRectangleDesc : BoxDesc
    {
        /// <summary>
        /// L 型方向
        /// </summary>
        public LShapeDirection Direction { get; set; } = LShapeDirection.TopLeft;

        /// <summary>
        /// 横向矩阵的宽
        /// </summary>
        public double HorizontalRectWidth { get; set; }

        /// <summary>
        /// 横向矩阵的高
        /// </summary>
        public double HorizontalRectHeight { get; set; }

        /// <summary>
        /// 纵向矩阵的宽
        /// </summary>
        public double VerticalRectWidth { get; set; }

        /// <summary>
        /// 纵向矩阵的高
        /// </summary>
        public double VerticalRectHeight { get; set; }

        /// <summary>
        /// 是否旋转
        /// </summary>
        public bool IsSpin = false;


        /// <summary>
        /// 获取矩形边线中点
        /// </summary>
        /// <param name="index"></param>
        /// <returns>0：左边，1：上边，2：右边，3：下边</returns>
        public List<Primitives.Point> GetInnerRectStartEnd()
        {
            var rect = GetRectangle(0);//横向
            double xCenter = rect.l + rect.w / 2;
            double yCenter = rect.t + rect.h / 2;
            double widthHalf = rect.w / 2;
            double heightHalf = rect.h / 2;

            var rect1 = GetRectangle(1);//纵向
            double xCenter1 = rect1.l + rect1.w / 2;
            double yCenter1 = rect1.t + rect1.h / 2;
            double widthHalf1 = rect1.w / 2;
            double heightHalf1 = rect1.h / 2;

            if (!IsSpin)//从上到下 左到右
            {
                // 计算旋转后的四个中线点的坐标
                var rotatedPoints = new List<Core.Primitives.Point>
                {
                    new Core.Primitives.Point(
                        xCenter,
                        yCenter - heightHalf
                    ),
                     new Core.Primitives.Point(
                        xCenter,
                        yCenter + heightHalf
                    ),
                     new Primitives.Point(
                         rect.w,rect.h
                         ),
                     new Core.Primitives.Point(
                        xCenter1 - widthHalf1 ,
                        yCenter1
                    ),
                    new Core.Primitives.Point(
                        xCenter1 + widthHalf1 ,
                        yCenter1
                    ),
                     new Primitives.Point(
                         rect1.w,rect1.h
                         ),
                };
                return rotatedPoints;
            }
            else //下到上 右到左
            {
                // 计算旋转后的四个中线点的坐标
                var rotatedPoints = new List<Core.Primitives.Point>
                {
                    new Core.Primitives.Point(
                        xCenter,
                        yCenter + heightHalf
                    ),
                     new Core.Primitives.Point(
                        xCenter,
                        yCenter - heightHalf
                    ),
                     new Primitives.Point(
                         rect.w,rect.h
                         ),
                     new Core.Primitives.Point(
                        xCenter1 + widthHalf1 ,
                        yCenter1
                    ),
                    new Core.Primitives.Point(
                        xCenter1 - widthHalf1 ,
                        yCenter1
                    ),
                    new Primitives.Point(
                         rect1.w,rect1.h
                         ),
                };
                return rotatedPoints;
            }
        }

        /// <summary>
        /// index = 0 横向 index = 1 纵向
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public (double l, double t, double w, double h) GetRectangle(int index)
        {
            //var angle = 0;
            switch (Direction)
            {
                case LShapeDirection.TopLeft:
                    if (index == 0)//横向
                    {
                        return (Left + (Width - BandLengthRatio) / 2, Top - HorizontalRectHeight / 2,BandLengthRatio, HorizontalRectHeight);
                    }
                    else//纵向
                    {
                        return (Left - VerticalRectWidth / 2, Top + (Height - BandWidthRatio)/ 2, VerticalRectWidth, BandWidthRatio);
                    }
                case LShapeDirection.BottomLeft:
                    if (index == 0)
                    {
                        return (Left + (Width  - BandLengthRatio) / 2, Top + Height - HorizontalRectHeight / 2, BandLengthRatio, HorizontalRectHeight);
                    }
                    else
                    {
                        return (Left - VerticalRectWidth / 2, Top + (Height- BandWidthRatio) / 2, VerticalRectWidth, BandWidthRatio);
                    }
                case LShapeDirection.TopRight:
                    if (index == 0)//横向
                    {
                        return (Left + (Width - BandLengthRatio) /2, Top - HorizontalRectHeight / 2, BandLengthRatio, HorizontalRectHeight);
                    }
                    else//纵向
                    {
                        return (Left + Width - VerticalRectWidth / 2, Top + (Height - BandWidthRatio) / 2, VerticalRectWidth,BandWidthRatio);
                    }
                case LShapeDirection.BottomRight:
                if (index == 0)//横向
                    {
                        return (Left + (Width - BandLengthRatio) / 2, Top + Height - HorizontalRectHeight / 2, BandLengthRatio, HorizontalRectHeight);
                    }
                    else//纵向
                    {
                        return (Left + Width  - VerticalRectWidth / 2 , Top + (Height - BandWidthRatio) / 2, VerticalRectWidth, BandWidthRatio);
                    }
                default:
                    throw new ArgumentOutOfRangeException($"无效的{nameof(Direction)}");
            }
        }

        /// <summary>
        /// 臂宽
        /// </summary>
        public double BandWidthRatio { get; set; } = 60;

        /// <summary>
        /// 臂长
        /// </summary>
        public double BandLengthRatio { get; set; } = 60;
    }
}
