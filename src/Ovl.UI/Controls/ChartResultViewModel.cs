using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InteVega.CBB.Mvvm.Bases;
using Prism.Services.Dialogs;
using ScottPlot;
using ScottPlot.Plottables;
using ScottPlot.WPF;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Ovl.UI.Controls
{
    public partial class ChartResultViewModel : ObservableObject, IDialogAware
    {
        [ObservableProperty]
        private ObservableCollection<KeyValuePair<string, ChartResultDetail>> _datas 
            = new ObservableCollection<KeyValuePair<string, ChartResultDetail>>();

        [ObservableProperty]
        private KeyValuePair<string, ChartResultDetail> _selectedData
            = new KeyValuePair<string, ChartResultDetail>();

        public ChartResultViewModel()
        {

        }

        public string Title => "Correlation";

        public event Action<IDialogResult> RequestClose;

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            var tmpDatas = parameters.GetValue<ObservableCollection<KeyValuePair<string, List<CDChartCurve>>>>(nameof(Datas));

            Datas = new ObservableCollection<KeyValuePair<string, ChartResultDetail>>(
                tmpDatas.Select(_ => new KeyValuePair<string, ChartResultDetail>(
                    _.Key,
                    new ChartResultDetail
                    {
                        Charts = new ObservableCollection<CDChartCurve>(_.Value)
                    })));
            
            foreach (var item in Datas)
            {
               
                Datas.FirstOrDefault().Value.Charts.FirstOrDefault().IsSelected = true;
                var chart = item.Value.Charts.FirstOrDefault();
                item.Value.CreatePlot(chart.Xs, chart.Ys);
            }
        }

        [RelayCommand]
        private void RaidoChecked(RoutedEventArgs e)
        {
            RadioButton radioButton = e.Source as RadioButton;
            if (radioButton != null)
            {
                string selectedContent = radioButton.Content.ToString();
                var newValue = SelectedData.Value.Charts.FirstOrDefault(_ => _.Name == selectedContent);
                SelectedData.Value.UpdatePlot(newValue.Xs, newValue.Ys);
            }
        }
    }

    public class CDChartCurve : BindableBase
    {
        public CDChartCurve()
        {
        }

        /// <summary>
        /// 名称：Profile,Detertive
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// X轴数据
        /// </summary>
        public double[] Xs { get; set; }

        /// <summary>
        /// Y轴数据
        /// </summary>
        public double[] Ys { get; set; }

        /// <summary>
        /// 是否选中
        /// </summary>
        public bool IsSelected { get; set; }

        /// <summary>
        /// 最大值
        /// </summary>
        public double Maximum
        {
            get
            {
                if ((Ys?.Any() ?? false))
                {
                    return Ys.Max();
                }
                return 0.00d;
            }
        }

        public string Color { get; set; }
    }

    public class ChartResultDetail : BindableBase
    {
        private Crosshair ch;

        private ObservableCollection<CDChartCurve> _charts;
        public ObservableCollection<CDChartCurve> Charts
        {
            get { return _charts; }
            set { SetProperty(ref _charts, value); }
        }

        public List<string> RaidoSelectors = default;
        public ChartResultDetail()
        {
            Charts = new ObservableCollection<CDChartCurve>();
            PlotControl = new WpfPlot();
        }

        private WpfPlot _wpfPlot;
        public WpfPlot PlotControl
        {
            get { return _wpfPlot; }
            set { SetProperty(ref _wpfPlot, value); }
        }

        public void UpdatePlot(double[] xs, double[] ys)
        {
            PlotControl.Plot.Clear();
            PlotControl.Plot.Add.Scatter(xs, ys);
            
            ch = PlotControl.Plot.Add.Crosshair(0, 0);
            ch.TextColor = Colors.White;
            ch.TextBackgroundColor = ch.HorizontalLine.Color;

            Pixel mousePixel = new(0, 0);
           
            ch.Position = PlotControl.Plot.GetCoordinates(mousePixel);
            PlotControl.Refresh();
        }

        public void CreatePlot(double[] xs, double[] ys, Color? color = null)
        {
            PlotControl.Plot.Add.Scatter(xs, ys, color);
            ch = PlotControl.Plot.Add.Crosshair(0, 0);
            ch.TextColor = Colors.White;
            ch.TextBackgroundColor = ch.HorizontalLine.Color;
            PlotControl.Refresh();
            PlotControl.MouseMove += PlotControl_MouseMove;
        }

        private AxisLine? PlottableBeingDragged = null;
        private void PlotControl_MouseUp(object sender, MouseButtonEventArgs e)
        {
            
        }

        private void PlotControl_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void PlotControl_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var pos = e.GetPosition(PlotControl);
            Pixel mousePixel = new(pos.X, pos.Y);
            Coordinates mouseCoordinates = PlotControl.Plot.GetCoordinates(mousePixel);
            ch.Position = mouseCoordinates;
            ch.VerticalLine.Text = $"{mouseCoordinates.X:N3}";
            ch.HorizontalLine.Text = $"{mouseCoordinates.Y:N3}";
            PlotControl.Refresh();
        }

        private AxisLine? GetLineUnderMouse(float x, float y)
        {
            CoordinateRect rect = PlotControl.Plot.GetCoordinateRect(x, y, radius: 10);

            foreach (AxisLine axLine in PlotControl.Plot.GetPlottables<AxisLine>().Reverse())
            {
                if (axLine.IsUnderMouse(rect))
                    return axLine;
            }

            return null;
        }
    }
}
