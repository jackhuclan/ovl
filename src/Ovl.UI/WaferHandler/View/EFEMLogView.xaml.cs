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

using CommunityToolkit.Mvvm.ComponentModel;
using InteVega.CBB.CommonLib.Helpers;
using Ovl.UI.WaferHandler.ViewModel;

namespace Ovl.UI.WaferHandler.View;

/// <summary>
/// EFEMLogView.xaml 的交互逻辑
/// </summary>
public partial class EFEMLogView : UserControl
{
    public EFEMLogView()
    {
        InitializeComponent();
    }
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
    }
    private void LogGridOnRequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
    {
        e.Handled = true;
    }
    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is not EFEMLogViewModel efemLogViewModel)
        {
            return;
        }

        try
        {
            efemLogViewModel.Init();
        }
        catch (Exception ex)
        {
            // 新外壳尚未初始化厂内日志库（InteVegaLogHelper）时 Init 会抛错，不能因此拖垮整页
            LogHelper.LogError(ex, "EFEMLog 初始化失败");
        }
    }
}
