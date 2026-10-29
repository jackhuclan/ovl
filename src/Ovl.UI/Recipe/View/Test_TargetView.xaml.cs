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
using InteVega.CBB.ComUI.Extensions;
using Marvel.DataModels.Recipe;
using Microsoft.Extensions.DependencyModel;

using Ovl.UI.Recipe.ViewModel;
using DataModels = Marvel.DataModels;

namespace Ovl.UI.Recipe.View;

/// <summary>
/// Test_TargetView.xaml 的交互逻辑
/// </summary>
public partial class Test_TargetView : UserControl
{
    public Test_TargetView()
    {
        InitializeComponent();
        this.Loaded += Test_TargetView_Loaded;
        this.Unloaded += Test_TargetView_UnLoaded;
    }

    Test_TargetViewModel vm;
    private void Test_TargetView_Loaded(object sender, RoutedEventArgs e)
    {

        vm = (Test_TargetViewModel)DataContext;
        if (vm != null)
        {
            vm.Subscribe();
            vm.TargetSettingView = targetControl;
            vm.TargetsChanged = TargetChanged;
        }
        targetControl.ActionClickTarget = (target) =>
        {
            if (vm != null)
                vm.SelectTarget = target;
        };
    }

    private void Test_TargetView_UnLoaded(object sender, RoutedEventArgs e)
    {
        vm?.UnSubscribe();
    }
    private void TargetChanged()
    {
        try
        {
            targetinfos.ItemsSource = null;
            targetinfos1.ItemsSource = null;
            targetinfos.ItemsSource = vm.TestInfo.TargetSetting.TargetPoints2;
            targetinfos1.ItemsSource = vm.TestInfo.TargetSetting.TargetPoints2;
        }
        catch
        {

        }
    }



    private void targetinfos_LoadingRow(object sender, DataGridRowEventArgs e)
    {
        e.Row.Header = e.Row.GetIndex() + 1;
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {

    }

    private void Button_Click_1(object sender, RoutedEventArgs e)
    {
        vm.EditArray();
    }

    private TargetLocation GetTarget(object sender)
    {
        var fe = sender as FrameworkElement;
        var target = fe.DataContext as TargetLocation;
        if (target == null)
        {
            var dr = InteVega.CBB.ComUI.Helpers.VisualLogicTreeHelper.FindVisualParent<DataGridRow>(fe);
            if (dr != null)
                target = dr.DataContext as TargetLocation;
        }
        return target;
    }

    private void targetinfos_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        vm.SelectTarget = GetTarget (e.OriginalSource );
        vm.IsEnable = vm.SelectTarget != null && vm.SelectTarget.IsEnable;
        vm.SelectTargetInUi(vm.SelectTarget);

    }
    private void DataGrid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        vm.SelectTarget = GetTarget(e.OriginalSource);
        vm.IsEnable = vm.SelectTarget != null && vm.SelectTarget.IsEnable;
    }

    private void MoveTarget(object sender, int columnOffset, int rowOffset)
    {
        var target = (sender as FrameworkElement).DataContext as TargetLocation;
        if (target == null) return;
        var x = target.R3Vector.X + vm.WaferMapInfo.FieldWidth * columnOffset;
        var y = target.R3Vector.Y + vm.WaferMapInfo.FieldHeight * rowOffset;
        target.R3Vector = new DataModels.Core.WcsPoint(x, y);
        vm.Applay();
    }

    private void Button_Click_LeftField(object sender, RoutedEventArgs e)
    {
        MoveTarget(sender, -1, 0);
    }



    private void Button_Click_RightField(object sender, RoutedEventArgs e)
    {
        MoveTarget(sender, 1, 0);
    }

    private void Button_Click_DownField(object sender, RoutedEventArgs e)
    {

        MoveTarget(sender, 0, -1);
    }
    private void Button_Click_UpField(object sender, RoutedEventArgs e)
    {
        MoveTarget(sender, 0, 1);
    }


}
