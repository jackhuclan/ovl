using System.Windows.Controls;

namespace Ovl.UI.Views.DataDisplay;

/// <summary>
/// 日志视图（对应原版 Marvel.Main.DataDisplay.View.Log 的本地等价实现：
/// 原版依赖厂内 InteVega.CBB.Log.Display 包与 Prism 容器注入，本地实现
/// 直接继承 BasicDataViewModel 作为 DataContext，绑定路径保持一致）。
/// </summary>
public partial class LogView : UserControl
{
    public LogView()
    {
        InitializeComponent();
    }
}
