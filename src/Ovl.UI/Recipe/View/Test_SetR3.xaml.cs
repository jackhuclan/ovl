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
using Marvel.DataModels.Recipe;

using Ovl.UI.Recipe.ViewModel;

namespace Ovl.UI.Recipe.View;

/// <summary>
/// Test_SetR3.xaml 的交互逻辑
/// </summary>
public partial class Test_SetR3 : UserControl
{
    public Test_SetR3()
    {
        InitializeComponent();
        //SetImageViewer();
        this.DataContextChanged += Test_TrainView_DataContextChanged;
    }

    private void Test_TrainView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        SetImageViewer();
    }

    private void SetImageViewer()
    {
        vm = DataContext as Test_SetR3ViewModel;
        vm.StageControl = wmsc;

        if (vm != null)
        {
            vm.HpImageViewer = StageControlImageView?.ImageViewer;
            vm.Loaded();
        }
    }

    Test_SetR3ViewModel vm;

    private void targetinfos_LoadingRow(object sender, DataGridRowEventArgs e)
    {
        e.Row.Header = e.Row.GetIndex() + 1;
    }
    private void targetinfos_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        vm.SelectTarget = (e.OriginalSource as FrameworkElement).DataContext as TargetLocation;
        //var dataGrid = sender as DataGrid;
        //if (dataGrid != null && e.OriginalSource is DependencyObject depObj)
        //{
        //    // 通过可视树查找被点击的单元格
        //    var row = depObj.FindVisualParent<DataGridRow>();
        //    if (row != null)
        //    {
        //        // 处理单击单元格的逻辑
        //        // 例如更新 SelectTarget 属性
        //        ((Test_TargetViewModel)this.DataContext).SelectTarget = row.Item as TargetLocation;
        //        // 执行其他操作
        //    }
        //}
    }

    private void btnGoto_Click(object sender, RoutedEventArgs e)
    {
        vm?.Goto();
    }

    private void btnGetR3_Click(object sender, RoutedEventArgs e)
    {
        vm?.GetR3();
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {

    }
}
