using ImTools;
using Marvel.DataModels.Core;
using Marvel.DataModels.Recipe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Ovl.UI.Controls
{
    public class DrawTarget
    {
        
        public static void DrawTargetPanel(Canvas canvas,double width,double height )
        {
            canvas.Children.Clear();

            // Base Recg
            Rectangle rectangleBase = new Rectangle()
            {
                Width = width,
                Height = height,
                Stroke = Brushes.DarkBlue,
                StrokeThickness = 1,// 0.5 / scale.Width * 5,
                Fill = new SolidColorBrush(Color.FromArgb(200, 227, 234, 245))
            };
            Canvas.SetTop(rectangleBase, 0);
            Canvas.SetLeft(rectangleBase, 0);
            canvas.Children.Add(rectangleBase);
            // Apply LayoutTransform to the Canvas
        }
        public static IEnumerable<Shape> DrawTargets(List<TargetLocation> targetPoints,
                                       Canvas canvas,
                                       double targetRectSize,
                                       double coordFieldWith,
                                       double coordFieldHeight
                                       )
        {
            double left = 0;
            double top = 0;
            double fieldWidth = Math.Abs(coordFieldWith);
            double fieldHeight = Math.Abs(coordFieldHeight);
            double right = fieldWidth;
            double bottom = fieldHeight;
            IEnumerable<Shape> shapes = new List<Shape>();
            for (int i = 0; i < targetPoints.Count; i++)
            {
                var target = targetPoints[i];
                // target RECG
                Shape rectangle1target = new Rectangle()
                {
                    Width = targetRectSize,
                    Height = targetRectSize,
                    Stroke = Brushes.Red,
                    StrokeThickness = 0.5,
                    Fill = new SolidColorBrush(Color.FromArgb(100, 240, 124, 10))
                };

                if (target.IsArray)
                {
                    rectangle1target = new Path()
                    {
                        Width = targetRectSize,
                        Height = targetRectSize,
                        Stroke = Brushes.Red,
                        StrokeThickness = 0.5,
                        Fill = new SolidColorBrush(Color.FromArgb(100, 240, 124, 10)),
                        //Data = new PathGeometry() { Figures = pfc }
                    };
                    double w = rectangle1target.Width;
                    double h = rectangle1target.Height;

                    PathFigure figure = new PathFigure();
                    figure.StartPoint = new Point();
                    figure.Segments.Add(new LineSegment() { Point = new Point(w, 0) });
                    figure.Segments.Add(new LineSegment() { Point = new Point(w, h) });
                    figure.Segments.Add(new LineSegment() { Point = new Point(0, h) });
                    figure.IsClosed = true;
                    PathFigureCollection pfc = new PathFigureCollection
                    {
                        figure
                    };
                    figure = new PathFigure();
                    figure.StartPoint = new Point(0, h / 2);
                    figure.Segments.Add(new LineSegment() { Point = new Point(w, h / 2) });
                    pfc.Add(figure);
                    figure = new PathFigure();
                    figure.StartPoint = new Point(w / 2, 0);
                    figure.Segments.Add(new LineSegment() { Point = new(w / 2, h) });
                    pfc.Add(figure);

                    (rectangle1target as Path).Data = new PathGeometry { Figures = pfc };

                }

                var orginPoint = new WcsPoint(0, fieldHeight);
                orginPoint = orginPoint.ToLlcPoint(target.OrgionPoint, fieldWidth, fieldHeight);
                //此处需要考虑坐标方向
                var coordX = coordFieldWith > 0 ? 1 : -1;
                var coordY = coordFieldHeight > 0 ? 1 : -1;
                var r3 = new WcsPoint(target.R3Vector.X * coordX, target.R3Vector.Y * coordY);
                var targetPoint = orginPoint + r3;
                // line
                Line line = new Line()
                {
                    X1 = orginPoint.X,
                    Y1 = orginPoint.Y,
                    X2 = targetPoint.X,
                    Y2 = targetPoint.Y,
                    Stroke = Brushes.Black,
                    StrokeThickness = 0.5,
                    Fill = new SolidColorBrush(Color.FromArgb(150, 0, 0, 0))
                };

                Canvas.SetTop(line, 0);
                Canvas.SetLeft(line, 0);
                canvas.Children.Add(line);

                double targetLeft = targetPoint.X - rectangle1target.Width / 2;
                double targetTop = targetPoint.Y - rectangle1target.Height / 2;
                double targetRight = targetPoint.X + rectangle1target.Width / 2;
                double targetBottom = targetPoint.Y + rectangle1target.Height / 2;
                if (targetLeft < left) { left = targetLeft; }
                if (targetTop < top) { top = targetTop; }
                if (targetRight > right) { right = targetRight; }
                if (targetBottom > bottom) { bottom = targetBottom; }
                // targetrecg
                Canvas.SetTop(rectangle1target, targetTop);
                Canvas.SetLeft(rectangle1target, targetLeft);
                canvas.Children.Add(rectangle1target);

                shapes = shapes.Append(rectangle1target);
            }
            double width = right - left;
            double height = bottom - top;
            if (left < 0 || top < 0)
            {
                var marginLeft = left < 0 ? -left + 5 : 5;
                var marginTop = top < 0 ? -top + 5 : 5;
                canvas.Margin = new Thickness(marginLeft, marginTop, 5, 5);
            }
            canvas.Width = width;
            canvas.Height = height;
            return shapes;
        }

        

        public static bool IsInRect(Point p, double left, double top, double width,double height, Canvas canvas)
        {

            return p.X >= left && p.X <= left + width
                && p.Y >= top && p.Y <= top + height;
        }

    }
}
