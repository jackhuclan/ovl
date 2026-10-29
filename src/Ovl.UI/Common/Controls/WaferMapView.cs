using System.Windows;
using System.Windows.Media;

namespace Ovl.UI.Common.Controls;

/// <summary>
/// Wafer Map 占位视图（替代原版 WaferMapCommon/Halcon 显示控件）。
///
/// M3b-2 阶段无 Halcon 环境，用纯 WPF 绘制晶圆轮廓与测点阵列：
/// 外圈为晶圆边缘，内部按行列绘制测点（Pass=绿 / Fail=红 / 未测=浅灰），
/// 数据源由 <see cref="PointsSource"/> 提供（元素为 (col,row,state) 三元组）。
/// 原版图像显示控件在 Stage Control 图像区迁移（M5）时按同接口接入。
/// </summary>
public sealed class WaferMapView : FrameworkElement
{
    /// <summary>测点状态。</summary>
    public enum PointState
    {
        Unmeasured,
        Pass,
        Fail,
    }

    /// <summary>单个测点。</summary>
    public sealed record MapPoint(int Column, int Row, PointState State);

    public static readonly DependencyProperty PointsSourceProperty =
        DependencyProperty.Register(
            nameof(PointsSource),
            typeof(IEnumerable<MapPoint>),
            typeof(WaferMapView),
            new PropertyMetadata(null, OnPointsChanged));

    /// <summary>测点集合；为空时绘制空晶圆。</summary>
    public IEnumerable<MapPoint>? PointsSource
    {
        get => (IEnumerable<MapPoint>?)GetValue(PointsSourceProperty);
        set => SetValue(PointsSourceProperty, value);
    }

    private static readonly Brush WaferEdge = new SolidColorBrush(Color.FromRgb(0x9A, 0xA0, 0xA6));
    private static readonly Brush PassBrush = new SolidColorBrush(Color.FromRgb(0x34, 0xA8, 0x53));
    private static readonly Brush FailBrush = new SolidColorBrush(Color.FromRgb(0xEA, 0x43, 0x35));
    private static readonly Brush IdleBrush = new SolidColorBrush(Color.FromRgb(0xDA, 0xDC, 0xE0));

    private static void OnPointsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((WaferMapView)d).Invalidate();

    public WaferMapView()
    {
        SnapsToDevicePixels = true;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var side = Math.Min(
            double.IsInfinity(availableSize.Width) ? 300 : availableSize.Width,
            double.IsInfinity(availableSize.Height) ? 300 : availableSize.Height);
        return new Size(side, side);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        var side = Math.Min(ActualWidth, ActualHeight);
        if (side <= 0)
        {
            return;
        }

        var center = new Point(side / 2, side / 2);
        var radius = side / 2 - 4;

        // 晶圆底与边缘
        drawingContext.DrawEllipse(Brushes.White, new Pen(WaferEdge, 1.5), center, radius, radius);
        // Notch 缺口示意
        drawingContext.DrawEllipse(Brushes.White, new Pen(WaferEdge, 1), new Point(center.X, center.Y + radius), 10, 6);

        var points = PointsSource ?? Enumerable.Empty<MapPoint>();
        var dotRadius = Math.Max(2.5, radius / 24);
        foreach (var point in points)
        {
            var angle = (Math.PI * 2 * point.Column / 16) + (Math.PI * point.Row / 40);
            var ring = radius * 0.78 * (0.35 + (0.65 * point.Row / 20.0));
            var position = new Point(
                center.X + (Math.Cos(angle) * ring),
                center.Y + (Math.Sin(angle) * ring));
            if (Distance(position, center) > radius - dotRadius - 2)
            {
                continue;
            }

            var brush = point.State switch
            {
                PointState.Pass => PassBrush,
                PointState.Fail => FailBrush,
                _ => IdleBrush,
            };
            drawingContext.DrawEllipse(brush, null, position, dotRadius, dotRadius);
        }
    }

    private void Invalidate()
    {
        InvalidateVisual();
    }

    private static double Distance(Point a, Point b)
        => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
}
