using InteVega.CBB.Common.Algorithm.Shapes;
using Marvel.DataModels.Core;
using Marvel.DataModels.Enums;
using Marvel.DataModels.Recipe;
using OVL.Algorithm.DoNet;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using InteVega.CBB.ComUI.Helpers;
using OpenTK.Mathematics;
using InteVega.CBB.CommonLib.Helpers;

namespace Ovl.UI.ViewModels.Controls
{
    /// <summary>
    /// 处理与Qmerit相关的交互逻辑
    /// </summary>
    public partial class WaferMapControl : UserControl
    {

        /// <summary>
        /// 清除所有量测结果数据
        /// </summary>
        public void ClearResults()
        {
            canvasMisreg.Children.Clear();
        }
        /// <summary>
        /// 添加一个量测结果
        /// </summary>
        /// <param name="nmPerPixel"></param>
        /// <param name="field"></param>
        /// <param name="llcR3"></param>
        /// <param name="misreg"></param>
        /// <param name="color"></param>
        public void AddOneResult(double nmPerPixel, RowColumnInfo field, WcsPoint llcR3, WcsPoint misreg, Color color)
        {
            if (!double.IsNormal(misreg.X) || !double.IsNormal(misreg.Y)) { return; }//如果数据不正常则不绘制
            if (gridInner.ActualWidth == 0 || gridInner.ActualHeight == 0) return; //如果界面还没加载成功，则不绘制
            var wcsPoint = WaferMapInfo.GetWcsPoint(field, llcR3);
            var cow = new WcsPoint(gridInner.ActualWidth / 2, gridInner.ActualHeight / 2);
            var startPoint = new WcsPoint(wcsPoint.X * WaferMapInfo.CoefficientX, wcsPoint.Y * WaferMapInfo.CoefficientY);
            var endPoint = new WcsPoint(misreg.X * WaferMapInfo.CoefficientX, misreg.Y * WaferMapInfo.CoefficientY);
            startPoint += cow;
            endPoint += startPoint;
            var pathData = CreatePath(nmPerPixel, new Point(startPoint.X, startPoint.Y), new Point(endPoint.X, endPoint.Y), color);
            canvasMisreg.Children.Add(pathData.Line);
            canvasMisreg.Children.Add(pathData.Arrow);
        }

        /// <summary>
        /// 绘制qmerit矢量箭头
        /// </summary>
        /// <param name="nmPerPixel">每个像素多少nm</param>
        /// <param name="rowIndex">field 行</param>
        /// <param name="columnIndex">field 列</param>
        /// <param name="startPoint">起始点--target坐标</param>
        /// <param name="endPoint">结束点--qmeritX,Y偏移;原始测量值为nm</param>
        /// <param name="arrowColor">箭头颜色</param>
        /// <param name="vectorMaxLength">矢量箭头的长度最大值,用于计算长度匹配对应的颜色时计算比例分母,一般可用field的对角线长</param>
        /// <param name="lengthToColor">是否使用不同颜色表示</param>
        public void DrawLineInCell(double nmPerPixel, int rowIndex, int columnIndex, Point startPoint, Point endPoint, double vectorMaxLength, Color arrowColor, bool lengthToColor = true)
        {
            startPoint = new Point(startPoint.X * WaferMapInfo.CoefficientX, startPoint.Y * WaferMapInfo.CoefficientY);
            endPoint = new Point(endPoint.X * WaferMapInfo.CoefficientX, endPoint.Y * WaferMapInfo.CoefficientY);
            //11nm|14nm;fieldWidth=20mm; 放大后不能大于一个或2个field的大小
            endPoint.X = (startPoint.X + endPoint.X);
            endPoint.Y = (startPoint.Y + endPoint.Y);
            //根据偏差大小，用不同的颜色表示
            if (lengthToColor)
            {
                // 计算线段长度
                double length = Math.Sqrt(Math.Pow(endPoint.X - startPoint.X, 2) + Math.Pow(endPoint.Y - startPoint.Y, 2));
                // 计算长度所占比例
                double ratio = length / vectorMaxLength;// (10 * 1.414);这个可以用field的对角线的长度
                // 计算颜色值,红色值随着长度增加而增加，蓝色值随着长度减小而减小
                byte blue = (byte)(255 * (1 - ratio));
                byte red = (byte)(255 * ratio);
                arrowColor = Color.FromArgb(255, red, 0, blue);
            }

            var pathData = CreatePath(nmPerPixel, startPoint, endPoint, arrowColor);

            var absField = GetAbsoluteField(new RowColumnInfo(rowIndex, columnIndex));
            int id = GetId(absField.RowIndex, absField.ColumnIndex);

            if (dicElments.TryGetValue(id, out Button btn))
            {
                Canvas container = btn.Content as Canvas ?? new Canvas();
                Canvas.SetZIndex(container, 10);
                //if (btn.Content == null) return;
                //bool fieldselectNum = int.TryParse(btn.Content.ToString(), out int num);
                //Viewbox viewbox = new Viewbox();
                //TextBlock textBlock = new TextBlock
                //{
                //    Text = fieldselectNum ? num.ToString() : "",
                //    HorizontalAlignment = HorizontalAlignment.Center,
                //    VerticalAlignment = VerticalAlignment.Center
                //};
                //viewbox.Child = textBlock;
                //Canvas container = new Canvas();
                // 将直线和箭头添加到容器中
                //container.Children.Add(viewbox);//取消显示field序号
                container.Children.Add(pathData.Line);
                container.Children.Add(pathData.Arrow);
                btn.Opacity = 0.7;
                //
                btn.Content = container;
            }
        }

        private (Line Line,Path Arrow) CreatePath(double nmPerPixel, Point startPoint, Point endPoint, Color arrowColor)
        {
            // ************************计算当前线段的长度
            double currentLength = CalculateDistance(startPoint, endPoint);
            // 设置要变为的倍数
            double n = nmPerPixel;
            // 计算新的线段长度
            double newLength = n * currentLength;
            // 计算新的终点坐标
            endPoint = CalculateNewEndPoint(startPoint, endPoint, newLength);
            //箭头长度
            double arrowLength = 2;
            // 角度
            double angle = Math.Atan2(endPoint.Y - startPoint.Y, endPoint.X - startPoint.X);
            //箭头终点1
            Point arrowPoint1 = new Point(
                endPoint.X - arrowLength * Math.Cos(angle + Math.PI / 8),
                endPoint.Y - arrowLength * Math.Sin(angle + Math.PI / 8)
            );
            //箭头终点2
            Point arrowPoint2 = new Point(
                endPoint.X - arrowLength * Math.Cos(angle - Math.PI / 8),
                endPoint.Y - arrowLength * Math.Sin(angle - Math.PI / 8)
            );
            Line line=new Line() { X1=startPoint.X , Y1 = startPoint.Y ,X2 =endPoint.X, Y2=endPoint.Y ,Stroke= new SolidColorBrush(arrowColor)};
            //创建Path对象表示直线
            Path linePath = new Path();
            linePath.Stroke = new SolidColorBrush(arrowColor);
            linePath.StrokeThickness = 1;


            //创建 PathGeometry，用于定义直线
            PathGeometry lineGeometry = new PathGeometry();
            PathFigure lineFigure = new PathFigure();
            lineFigure.StartPoint = startPoint;
            LineSegment lineSegment = new LineSegment();
            lineSegment.Point = endPoint;
            lineFigure.Segments.Add(lineSegment);
            lineGeometry.Figures.Add(lineFigure);
            linePath.Data = lineGeometry;

            //箭头颜色、宽度等属性
            Path arrowPath = new Path();
            arrowPath.Stroke = new SolidColorBrush(arrowColor);
            arrowPath.StrokeThickness = 1;

            // 创建箭头 PathGeometry
            PathGeometry arrowGeometry = new PathGeometry();
            PathFigure arrowFigure = new PathFigure();
            //箭头起始位置从外侧一个点起始
            arrowFigure.StartPoint = arrowPoint1;
            //组成箭头的直线1，终点使用线段的终点
            LineSegment arrowSegment1 = new LineSegment();
            arrowSegment1.Point = endPoint;
            arrowFigure.Segments.Add(arrowSegment1);
            //组成箭头的直线2，起点为线段终点，终点为箭头末端
            LineSegment arrowSegment2 = new LineSegment();
            arrowSegment2.Point = arrowPoint2;
            arrowFigure.Segments.Add(arrowSegment2);

            arrowGeometry.Figures.Add(arrowFigure);
            arrowPath.Data = arrowGeometry;
            return (line ,arrowPath);
        }

        // 根据给定的起点、终点和新的长度计算新的终点坐标
        static Point CalculateNewEndPoint(Point startPoint, Point endPoint, double newLength)
        {
            double currentLength = CalculateDistance(startPoint, endPoint);

            double ratio = newLength / currentLength;

            double newX = startPoint.X + (endPoint.X - startPoint.X) * ratio;
            double newY = startPoint.Y + (endPoint.Y - startPoint.Y) * ratio;

            return new Point(newX, newY);
        }
        // 计算两点之间的距离
        static double CalculateDistance(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
        }
        public void DrawCross(int rowIndex, int columnIndex)
        {
            int id = GetId(rowIndex, columnIndex);
            if (dicElments.TryGetValue(id, out var btn))
            {
                Canvas container = btn.Content as Canvas ?? new Canvas();
                MarkPointCrossWafer(container, 0, 0,new SolidColorBrush(Color.FromRgb(255,0,0)),20,1);
                btn.Content = container;
            }
        }
        public void MarkPointCrossWafer(Canvas canvas, double pointX, double PointY, SolidColorBrush colorMark, int marklength, double strokethickness = 1)
        {
            Line crossLine1 = new Line();
            crossLine1.X1 = pointX - marklength;
            crossLine1.Y1 = PointY - marklength;
            crossLine1.X2 = pointX + marklength;
            crossLine1.Y2 = PointY + marklength;
            crossLine1.Stroke = colorMark;
            crossLine1.StrokeThickness = strokethickness;
            //
            Line crossLine2 = new Line();
            crossLine2.X1 = pointX - marklength;
            crossLine2.Y1 = PointY + marklength;
            crossLine2.X2 = pointX + marklength;
            crossLine2.Y2 = PointY - marklength;
            crossLine2.Stroke = colorMark;
            crossLine2.StrokeThickness = strokethickness;
            //
            canvas.Children.Add(crossLine1);
            canvas.Children.Add(crossLine2);
        }
    }
}
