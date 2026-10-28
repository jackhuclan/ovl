using HPImageViewer.Core.Persistence;
using HPImageViewer.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace HPImageViewer.Rendering.ROIRenders
{
    internal class CrosshairsRender : ROIRender
    {
        public CrosshairsRender(CrosshairsDesc roiDesc) : base(roiDesc)
        {
        }
        private CrosshairsDesc CrosshairsDesc
        {
            get => (CrosshairsDesc)ROIDesc;
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
            get => CrosshairsDesc.CenterX;
            set
            {
                if (CrosshairsDesc.CenterX != value)
                {
                    CrosshairsDesc.CenterX = value;
                    OnPropertyChanged();
                }

            }
        }

        public double CenterY
        {
            get => CrosshairsDesc.CenterY;
            set
            {
                if (CrosshairsDesc.CenterY != value)
                {
                    CrosshairsDesc.CenterY = value;
                    OnPropertyChanged();
                }

            }
        }
        public double ActualX 
        {
            get => CrosshairsDesc.ActualX;
            set
            {
                if (CrosshairsDesc.ActualX != value)
                {
                    CrosshairsDesc.ActualX = value;
                    OnPropertyChanged();
                }

            }
        }
                public double ActualY
        {
            get => CrosshairsDesc.ActualY;
            set
            {
                if (CrosshairsDesc.ActualY != value)
                {
                    CrosshairsDesc.ActualY = value;
                    OnPropertyChanged();
                }

            }
        }
        public Core.Primitives.Point TransformedCenter { get; set; }
        public double Size
        {
            get => CrosshairsDesc.Size;
            set
            {
                if (CrosshairsDesc.Size != value)
                {
                    CrosshairsDesc.Size = value;
                    OnPropertyChanged();
                }

            }
        }
        public double Angle
        {
            get => CrosshairsDesc.Angle;
            set
            {
                if (CrosshairsDesc.Angle != value)
                {
                    CrosshairsDesc.Angle = value;
                    OnPropertyChanged();
                }

            }
        }
        public override int HandleCount =>1;

        public override int HitTest(Point point)
        {
            if (CrosshairsDesc.IsMove)
            {
                Rect rectConter = new Rect(TransformedCenter.X - Size, TransformedCenter.Y - Size, Size * 2, Size * 2);
                //Console.WriteLine($"X:{CenterX}Y:{CenterY}，鼠标轴：X{point.X},y{point.Y}");
                //Console.WriteLine("结果："+ rectConter.Contains(point));
                if (rectConter.Contains(point))
                    return 0;

                return -1;
            }
            else
            {
                return -1;
                throw new NotImplementedException();
            }

        }
        protected override void MoveToInternal(double wx, double wy)
        {
            CenterX += wx;
            if (CenterX - Size < 0)
                CenterX = Size;
            if (CenterX + Size > ImageWidth)
                CenterX = ImageWidth - Size;
            CenterY += wy;
            if (CenterY - Size < 0)
                CenterY = Size;
            if (CenterY + Size > ImageHeight)
                CenterY = ImageHeight - Size;

            //Console.WriteLine($"X:{CenterX}Y:{CenterY}");
        }
        protected override void OnROIRender(RenderContext renderContext)
        {
            if (Brush.IsFrozen == false)
            {
                Brush.Freeze();
            }
            var fillBrush = IsSelected ? FillBrush : null;
            if (fillBrush?.IsFrozen == false)
            {
                fillBrush.Freeze();
            }


            var center = new Point(CenterX, CenterY);
            var transformedCenter = RenderTransform.ToDevice(center.ToPoint());
            TransformedCenter = transformedCenter;
            ActualX=transformedCenter.X;
            ActualY=transformedCenter.Y;
            double rotationRadians = Angle * Math.PI / 180.0;
            double cosTheta = Math.Cos(rotationRadians);
            double sinTheta = Math.Sin(rotationRadians);
            Core.Primitives.Point startPoint1 = new Core.Primitives.Point(
                (int)(transformedCenter.X - Size * sinTheta),
                (int)(transformedCenter.Y - Size * cosTheta)
            );
            Core.Primitives.Point endPoint1 = new Core.Primitives.Point(
                (int)(transformedCenter.X + Size * sinTheta),
                (int)(transformedCenter.Y + Size * cosTheta)
            );
            Core.Primitives.Point startPoint2 = new Core.Primitives.Point(
                (int)(transformedCenter.X + Size * cosTheta),
                (int)(transformedCenter.Y - Size * sinTheta)
            );
            Core.Primitives.Point endPoint2 = new Core.Primitives.Point(
                (int)(transformedCenter.X - Size * cosTheta),
                (int)(transformedCenter.Y + Size * sinTheta)
            );

            renderContext.DrawingContext.DrawLine(new Pen(Brush, this.ROIDesc.StrokeThickness), startPoint1, endPoint1);
            renderContext.DrawingContext.DrawLine(new Pen(Brush, this.ROIDesc.StrokeThickness), startPoint2, endPoint2);

            //Console.WriteLine($"--X:{transformedCenter.X}Y:{transformedCenter.Y}");
            //Console.WriteLine($"X:{CenterX}Y:{CenterY}");
        }
    }
}
