
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;

namespace Ovl.UI.Recipe.View;

/// <summary>
/// RecipeWindow.xaml 的交互逻辑
/// </summary>
public partial class RecipeWindow : Window
{
    public RecipeWindow()
    {
        InitializeComponent();
        //WaferMapInfo wmi = new WaferMapInfo() { FieldWidth = 23.31, FieldHeight = 29.97 };
        ////            LLC 0.002847    1.036919
        ////LRC 0.000916    1.922377
        ////ULC 0.003332 - 0.553398
        ////URC 0.003336 - 1.43726
        ////COF 1.66587 1.742554
        //var r1 = wmi.ConvertToOrigionR3(EnumR3OrigionPoint.COF, new WcsPoint(0.002847, 1.036919), EnumR3OrigionPoint.LLC);
        //var r2 = wmi.ConvertToOrigionR3(EnumR3OrigionPoint.COF, new WcsPoint(0.000916, 1.922377), EnumR3OrigionPoint.LRC);
        //var r3 = wmi.ConvertToOrigionR3(EnumR3OrigionPoint.COF, new WcsPoint(0.003332, -0.553398), EnumR3OrigionPoint.ULC);
        //var r4 = wmi.ConvertToOrigionR3(EnumR3OrigionPoint.COF, new WcsPoint(0.003336, -1.43726), EnumR3OrigionPoint.URC);
    }

    /// <summary>
    /// 窗口关闭事件处理
    /// </summary>
    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        (DataContext as IDisposable)?.Dispose();
    }

    private void EscCommand(object sender, ExecutedRoutedEventArgs arg)
    {

        Close();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // vendor 适配：Marvel.Agent 悬浮球（FloatingBallWindow）已按需求移除
    }

    private void Window_Closed(object sender, EventArgs e)
    {

    }
}
