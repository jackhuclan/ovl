using LiveCharts.Wpf.Charts.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Ovl.UI.Recipe.ViewModel;
namespace Ovl.UI.Recipe.View
{
    /// <summary>
    /// CorrectionValueSettingDialogView.xaml 的交互逻辑
    /// </summary>
    public partial class CorrectionValueSettingDialogView : UserControl
    {
        public CorrectionValueSettingDialogView()
        {
            InitializeComponent();
            
        }

        private void CorrectionChart_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var viewModel = (CorrectionValueSettingDialogViewModel)DataContext;
            if(viewModel.CorrectionValueParam.Curve == null)
            {
                MessageBox.Show("Curve is Empty, please train measure roi train completely!","Error", MessageBoxButton.OK);
                return;
            }
            CorrectionChart.CreatePlot(viewModel.CorrectionValueParam.Curve.RefCurvePos.ToArray(), viewModel.CorrectionValueParam.Curve.RefCurveValue.ToArray());
            viewModel.Chart = CorrectionChart;

            StageControlImageView.SetProductMode(false);
        }
    }
}
