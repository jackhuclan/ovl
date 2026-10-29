using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ScottPlot;
using ScottPlot.Plottables;
using ScottPlot.Control;
using SkiaSharp.Internals;
using System.Reflection.Emit;
using ScottPlot.Rendering.RenderActions;

namespace Ovl.UI.Controls
{
    /// <summary>
    /// ChartCurveControl.xaml 的交互逻辑
    /// </summary>
    public partial class ChartCurveControl : UserControl
    {
        public event EventHandler<PointSelectedEventArgs> HighLightPointSelected;

        #region Dependency Properties

        public static readonly DependencyProperty XAxisLabelProperty = DependencyProperty.Register(
            nameof(XAxisLabel), typeof(string), typeof(ChartCurveControl),
            new PropertyMetadata("X Axis", OnAxisLabelChanged));

        public static readonly DependencyProperty YAxisLabelProperty = DependencyProperty.Register(
            nameof(YAxisLabel), typeof(string), typeof(ChartCurveControl),
            new PropertyMetadata("Y Axis", OnAxisLabelChanged));

        public static readonly DependencyProperty FixedVerticalLinePositionProperty = DependencyProperty.Register(
            nameof(FixedVerticalLinePosition), typeof(double), typeof(ChartCurveControl),
            new PropertyMetadata(0.0, OnFixedVerticalLinePositionChanged));

        public static readonly DependencyProperty FixedMarkerAxisXPositionProperty = DependencyProperty.Register(
            nameof(FixedMarkerAxisXPosition), typeof(double), typeof(ChartCurveControl),
            new PropertyMetadata(0.0, FixedMarkerAxisXPositionChanged)); 

        public static readonly DependencyProperty PointHighlightThresholdProperty = DependencyProperty.Register(
            nameof(PointHighlightThreshold), typeof(double), typeof(ChartCurveControl),
            new PropertyMetadata(0.04, PointHighlightThresholdChanged));

        // Highlight color is now fixed internally

        public string XAxisLabel
        {
            get { return (string)GetValue(XAxisLabelProperty); }
            set { SetValue(XAxisLabelProperty, value); }
        }

        public string YAxisLabel
        {
            get { return (string)GetValue(YAxisLabelProperty); }
            set { SetValue(YAxisLabelProperty, value); }
        }

        public double FixedVerticalLinePosition
        {
            get { return (double)GetValue(FixedVerticalLinePositionProperty); }
            set { SetValue(FixedVerticalLinePositionProperty, value); }
        }

        public double FixedMarkerAxisXPosition
        {
            get { return (double)GetValue(FixedMarkerAxisXPositionProperty); }
            set { SetValue(FixedMarkerAxisXPositionProperty, value); }
        }
        public double PointHighlightThreshold
        {
            get { return (double)GetValue(PointHighlightThresholdProperty); }
            set { SetValue(PointHighlightThresholdProperty, value); }
        }
        
        // Fixed highlight color - no longer externally settable
        private readonly System.Drawing.Color _highlightColor = System.Drawing.Color.Red;

        private static void OnAxisLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ChartCurveControl control)
            {
                control.UpdateAxisLabels();
            }
        }

        private static void OnFixedVerticalLinePositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ChartCurveControl control)
            {
                control.UpdateFixedVerticalLine();
            }
        }

        private static void FixedMarkerAxisXPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ChartCurveControl control)
            {
                control.HighLightNearbyMarker();
            }
        }

        private static void PointHighlightThresholdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
        }

        #endregion

        #region Private Fields

        private Scatter _scatterPlot;
        private AxisLine _fixedVerticalLine;
        private AxisLine _mouseVerticalLine;
        private List<int> _highlightedPoints = new List<int>();
        private double[] _xData;
        private double[] _yData;
        private int _highlightedPointSize = 10;
        private int _normalPointSize = 5;
        // Store original data and highlighted points separately
        private List<(double x, double y)> _originalPoints = new List<(double x, double y)>();
        private List<(double x, double y)> _highlightedPointCoords = new List<(double x, double y)>();
        private Scatter _highlightPlot;
        // For fixed marker when double-clicking
        private Scatter _fixedMarker;
        private bool _hasFixedMarker = false;
        private double _fixedMarkerX;
        private double _fixedMarkerY;

        #endregion

        public ChartCurveControl()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            InitializePlot();
            // Bind mouse events
            PlotControl.MouseMove += PlotControl_MouseMove;
            PlotControl.MouseLeave += PlotControl_MouseLeave;
            PlotControl.MouseDoubleClick += PlotControl_MouseDoubleClick;
        }

        private void InitializePlot()
        {
            // Basic plot setup
            PlotControl.Plot.Title("");
            HideRenderBenchmark();
            // Default axis labels
            UpdateAxisLabels();
            CreateFixedVerticalLine();
            CreateMouseVerticalLine();
            PlotControl.Refresh();
        }

        private void HideRenderBenchmark()
        {
            PlotControl.UserInputProcessor.RemoveAll<ScottPlot.Interactivity.UserActionResponses.DoubleClickBenchmark>();
        }

        private void CreateMouseVerticalLine()
        {
            // 创建鼠标跟随的垂直线（初始不可见）
            _mouseVerticalLine = PlotControl.Plot.Add.VerticalLine(0);
            _mouseVerticalLine.LineWidth = 2;
            _mouseVerticalLine.LinePattern = LinePattern.Dashed;
            _mouseVerticalLine.IsVisible = false;
            _mouseVerticalLine.Color = ScottPlot.Colors.Red;
        }

        private void CreateFixedVerticalLine()
        {
            _fixedVerticalLine = PlotControl.Plot.Add.VerticalLine(FixedVerticalLinePosition);
            _fixedVerticalLine.Color = ScottPlot.Colors.Green;
            _fixedVerticalLine.LineWidth = 2;
        }

        private void UpdateAxisLabels()
        {
            PlotControl.Plot.XLabel(XAxisLabel);
            PlotControl.Plot.YLabel(YAxisLabel);
            PlotControl.Refresh();
        }

        private void UpdateFixedVerticalLine()
        {
            if (_fixedVerticalLine == null)
            {
                CreateFixedVerticalLine();
            }
            _fixedVerticalLine.Position = FixedVerticalLinePosition;
            PlotControl.Refresh();
        }

        private void PlotControl_MouseMove(object sender, MouseEventArgs e)
        {
            var pos = e.GetPosition(PlotControl);
            double pixelX = pos.X;
            double pixelY = pos.Y;

            // Convert mouse pixel position to plot coordinates
            var mouseCoordinates = PlotControl.Plot.GetCoordinates((float)pixelX, (float)pixelY);
            double mouseX = mouseCoordinates.X; // This is the X coordinate in plot/data space

            // Update mouse vertical line
            _mouseVerticalLine.Position = mouseX;
            _mouseVerticalLine.IsVisible = true;
            // Reset previously highlighted points
            ResetHighlightedPoints();

            // Highlight points near mouse X position if data is available
            if (_xData != null && _yData != null && _scatterPlot != null)
            {
                HighlightNearbyPoints(mouseX);
            }

            PlotControl.Refresh();
        }

        private void PlotControl_MouseLeave(object sender, MouseEventArgs e)
        {
            // Hide mouse vertical line when mouse leaves the control
            _mouseVerticalLine.IsVisible = false;

            // Reset highlighted points
            ResetHighlightedPoints();

            PlotControl.Refresh();
        }

        private void ResetHighlightedPoints()
        {
            if (_highlightPlot != null)
            {
                // Remove the highlight plot and clear the list
                PlotControl.Plot.Remove(_highlightPlot);
                _highlightedPointCoords.Clear();
                _highlightedPoints.Clear();
            }
        }

        private void ThresholdHighlightedPoints(double mouseX)
        {
            if (_xData == null) return;
            for (int i = 0; i < _xData.Length; i++)
            {
                if (Math.Abs(_xData[i] - mouseX) <= PointHighlightThreshold)
                {
                    // Add to highlighted points list
                    _highlightedPoints.Add(i);
                    _highlightedPointCoords.Add((_xData[i], _yData[i]));
                }
            }
        }

        private void HighlightNearbyPoints(double mouseX)
        {
            // Find points within threshold distance
            _highlightedPointCoords.Clear();

            ThresholdHighlightedPoints(mouseX);

            // If we have points to highlight, create a new scatter plot for them
            if (_highlightedPointCoords.Count > 0)
            {
                // Extract x and y arrays from the coordinates
                double[] highlightX = _highlightedPointCoords.Select(p => p.x).ToArray();
                double[] highlightY = _highlightedPointCoords.Select(p => p.y).ToArray();

                // Use the fixed highlight color
                // Create a new scatter plot just for the highlighted points
                _highlightPlot = PlotControl.Plot.Add.Scatter(highlightX, highlightY);
                _highlightPlot.Color = ScottPlot.Colors.Red;
                _highlightPlot.MarkerSize = _highlightedPointSize;
                _highlightPlot.MarkerShape = MarkerShape.FilledCircle;

                // Add annotation for the highlighted point
                AddHighlightAnnotation(mouseX);
            }
        }

        private void AddHighlightAnnotation(double mouseX)
        {
            if (_highlightedPointCoords.Count == 0)
                return;

            // Find the closest point to the mouse position
            var closestPoint = _highlightedPointCoords
                .OrderBy(p => Math.Abs(p.x - mouseX))
                .First();

            _mouseVerticalLine.Text = $"{closestPoint.x:F3}";
        }

        public void UpdatePlot(double[] xs, double[] ys, ScottPlot.Color? color = null)
        {
            // Store data for highlighting
            _xData = xs;
            _yData = ys;

            // Store original points for later use
            _originalPoints.Clear();
            for (int i = 0; i < xs.Length; i++)
            {
                _originalPoints.Add((xs[i], ys[i]));
            }

            // Clear previous plots but maintain lines
            PlotControl.Plot.Remove(_scatterPlot);

            // Also remove the highlight plot if it exists
            if (_highlightPlot != null)
            {
                PlotControl.Plot.Remove(_highlightPlot);
                _highlightPlot = null;
            }

            // Set default color if not specified
            ScottPlot.Color plotColor = color ?? ScottPlot.Colors.Blue;

            // Create scatter plot with normal styling
            _scatterPlot = PlotControl.Plot.Add.Scatter(xs, ys);
            _scatterPlot.Color = plotColor;
            _scatterPlot.MarkerSize = _normalPointSize;
            _scatterPlot.MarkerShape = MarkerShape.FilledCircle;
            PlotControl.Plot.Axes.SetLimits(xs.Min(), xs.Max(), ys.Min(), ys.Max());

            FillY(xs, ys);

            // Ensure fixed vertical line is visible
            UpdateFixedVerticalLine();

            // Refresh the plot
            PlotControl.Refresh();
        }

        private void SetLegend()
        {
            PlotControl.Plot.Legend.IsVisible = true;
            LegendItem item1 = new()
            {
                LineColor = _fixedVerticalLine.Color,
                MarkerFillColor = _fixedVerticalLine.Color,
                MarkerLineColor = _fixedVerticalLine.Color,
                LineWidth = _fixedVerticalLine.LineWidth,
                LabelText = "AutoFocus Position"
            };
            PlotControl.Plot.ShowLegend(new List<LegendItem>() { item1 });
        }

        /// <summary>
        ///  填充Y轴的区域
        /// </summary>
        /// <param name="xs"></param>
        /// <param name="ys"></param>
        private void FillY(double[] xs, double[] ys)
        {
            if (!xs.Any()) return;
            var ys_max = ys.Max();
            var ys_min = ys.Min();

            // 创建xs个数的为0的y轴填充颜色数组
            double[] ys1 = new double[xs.Length];
            double[] ys2 = new double[xs.Length];

            for (int i = 0; i < ys.Length; i++)
            {
                ys1[i] = ys_min;
                ys2[i] = ys[i];
            }

            var fill = PlotControl.Plot.Add.FillY(xs, ys1, ys2);
            var fillStyle = fill.FillStyle;
            fillStyle.Color = ScottPlot.Colors.Blue.WithAlpha(.2); // Set fill color with transparency
            fillStyle.HatchColor = ScottPlot.Colors.Black; // Set hatch color

        }

        public void CreatePlot(double[] xs, double[] ys, System.Windows.Media.Color? color = null)
        {
            // Clear everything
            PlotControl.Plot.Clear();

            // Reinitialize components
            InitializePlot();

            // Update with data
            // Convert WPF color to ScottPlot color if provided
            ScottPlot.Color? scottPlotColor = null;
            if (color.HasValue)
            {
                scottPlotColor = new ScottPlot.Color(color.Value.R, color.Value.G, color.Value.B, color.Value.A);
            }

            UpdatePlot(xs, ys, scottPlotColor);

            SetLegend();

            // Clear any fixed marker
            RemoveFixedMarker();
        }

        /// <summary>
        /// Set the threshold for highlighting points near the mouse cursor
        /// </summary>
        /// <param name="threshold">Distance threshold in X-axis units</param>
        public void SetHighlightThreshold(double threshold)
        {
            PointHighlightThreshold = threshold;
        }

        /// <summary>
        /// Handle double-click event to place fixed marker on highlighted points
        /// </summary>
        private void PlotControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Only proceed if there are highlighted points
            if (_highlightedPointCoords.Count == 0)
                return;

            // Get the mouse position
            var pos = e.GetPosition(PlotControl);
            var mouseCoordinates = PlotControl.Plot.GetCoordinates((float)pos.X, (float)pos.Y);
            double mouseX = mouseCoordinates.X;

            // 高亮十字Marker
            var highLightMarker = HighLightNearbyMarker(mouseX);

            // Raise the PointSelected event with the X position
            HighLightPointSelected?.Invoke(this, new PointSelectedEventArgs(highLightMarker.x, highLightMarker.y));
        }

        private void HighLightNearbyMarker()
        {
            ThresholdHighlightedPoints(FixedMarkerAxisXPosition);
            HighLightNearbyMarker(FixedMarkerAxisXPosition);
        }

        private (double x, double y) HighLightNearbyMarker(double mouseX)
        {
            // Find the closest highlighted point to the mouse position
            var closestPoint = _highlightedPointCoords
                .OrderBy(p => Math.Abs(p.x - mouseX))
                .FirstOrDefault();

            // Place a fixed marker at this point
            PlaceFixedMarker(closestPoint.x, closestPoint.y);

            return closestPoint;
        }

        /// <summary>
        /// Place a fixed red cross marker at the specified coordinates
        /// </summary>
        private void PlaceFixedMarker(double x, double y)
        {
            // Remove any existing fixed marker
            RemoveFixedMarker();

            // Store the marker coordinates
            _fixedMarkerX = x;
            _fixedMarkerY = y;
            _hasFixedMarker = true;

            // Create a new scatter plot with a single point for the marker
            double[] xs = new double[] { x };
            double[] ys = new double[] { y };

            _fixedMarker = PlotControl.Plot.Add.Scatter(xs, ys);
            _fixedMarker.Color = ScottPlot.Colors.Red;
            _fixedMarker.MarkerSize = 20;
            _fixedMarker.MarkerShape = MarkerShape.Cross;
            _fixedMarker.LineWidth = 10;

            PlotControl.Refresh();
        }

        /// <summary>
        /// Remove the fixed marker if it exists
        /// </summary>
        private void RemoveFixedMarker()
        {
            if (_hasFixedMarker && _fixedMarker != null)
            {
                PlotControl.Plot.Remove(_fixedMarker);
                _fixedMarker = null;
                _hasFixedMarker = false;
                PlotControl.Refresh();
            }
        }
    }

    public class PointSelectedEventArgs : EventArgs
    {
        public double XPosition { get; }
        public double YPosition { get; }

        public PointSelectedEventArgs(double xPosition, double yPosition)
        {
            XPosition = xPosition;
            YPosition = yPosition;
        }
    }
}