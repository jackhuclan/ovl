using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Ovl.UI.Controls
{
    public partial class ChartResultViewModel_Ovl : ObservableObject, IDialogAware
    {
        [ObservableProperty]
        private ObservableCollection<KeyValuePair<string, ChartResultDetail>> _datas
            = new ObservableCollection<KeyValuePair<string, ChartResultDetail>>();

        [ObservableProperty]
        private KeyValuePair<string, ChartResultDetail> _selectedData
            = new KeyValuePair<string, ChartResultDetail>();

        public ChartResultViewModel_Ovl()
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

            SetViewModelDatas(tmpDatas);
        }

        public void SetViewModelDatas(ObservableCollection<KeyValuePair<string, List<CDChartCurve>>> tmpDatas)
        {
            Datas = new ObservableCollection<KeyValuePair<string, ChartResultDetail>>(
    tmpDatas.Select(_ => new KeyValuePair<string, ChartResultDetail>(
        _.Key,
        new ChartResultDetail
        {
            Charts = new ObservableCollection<CDChartCurve>(_.Value)
        })));

            foreach (var item in Datas)
            {
                foreach (var chart in item.Value.Charts)
                {
                    ScottPlot.Color color = chart.Name == "Inner" ? ScottPlot.Colors.Green : ScottPlot.Colors.RoyalBlue;
                    item.Value.CreatePlot(chart.Xs, chart.Ys, color);
                    chart.Color = color.ToHex();
                }
            }
        }
    }
}
