using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Ovl.UI.Views.Measure.Controls;

/// <summary>
/// MRO 进度面板（原样迁移自 Marvel.App.Shared.view.MRONewProgressBoard）。
/// 交互逻辑：鼠标进入详情区或弹出区时打开 Popup，两者都离开后延迟关闭，避免移动过程中闪烁。
/// </summary>
public partial class MRONewProgressBoard : UserControl
{
    public MRONewProgressBoard()
    {
        InitializeComponent();
    }

    private void Detail_MouseEnter(object sender, MouseEventArgs e)
    {
        pop.IsOpen = true;
    }

    private void Detail_MouseLeave(object sender, MouseEventArgs e)
    {
        TryClosePopup();
    }

    private void PopContent_MouseEnter(object sender, MouseEventArgs e)
    {
        pop.IsOpen = true;
    }

    private void PopContent_MouseLeave(object sender, MouseEventArgs e)
    {
        TryClosePopup();
    }

    /// <summary>鼠标离开 Detail 与 Popup 区域后关闭，避免移向 Popup 时闪烁关闭。</summary>
    private void TryClosePopup()
    {
        Dispatcher.BeginInvoke(() =>
        {
            if (!tbDetail.IsMouseOver && !popContent.IsMouseOver)
            {
                pop.IsOpen = false;
            }
        }, DispatcherPriority.Input);
    }
}
