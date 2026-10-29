using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using InteVega.CBB.Common.Algorithm.Shapes;
using Marvel.MRO.DataModels.Models;
using OVL.Algorithm.DoNet;

using Ovl.UI.Mro.ViewModel;
using Ovl.UI.Recipe.ViewModel;

namespace Ovl.UI.Recipe.View;

/// <summary>
/// KPIView.xaml 的交互逻辑
/// </summary>
partial class TadPositionsView : Window
{
    public TadPositionsView(ObservableCollection<Point2d> TadPositionsData)
    {
        InitializeComponent();
        this.DataContext = new TadPositionsViewModel(TadPositionsData);
        // 在加载完成后居中
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        this.Unloaded += TadPositionsView_Unloaded;
    }

    private void TadPositionsView_Unloaded(object sender, RoutedEventArgs e)
    {
        var viewModel = this.DataContext as TadPositionsViewModel;
        if (viewModel != null)
        {
            viewModel.UnloadnResidualModelChangede();
        }
    }
}
