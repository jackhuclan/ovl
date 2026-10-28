using System.Windows.Controls;

namespace Ovl.UI.Views.DataDisplay;

/// <summary>
/// BasicData 面板（History / Log / Alarm 页签，原样迁移自
/// Marvel.Main.DataDisplay.View.BasicData，样式零改动；
/// 原版经 Prism ContainerProvider 注入 VM，本地由 DataContext 继承提供）。
/// </summary>
public partial class BasicData : UserControl
{
    public BasicData()
    {
        InitializeComponent();
    }
}
