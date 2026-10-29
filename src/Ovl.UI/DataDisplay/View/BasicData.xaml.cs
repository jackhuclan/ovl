using System.Windows.Controls;
using InteVega.CBB.DomainService.Api.Interfaces;

namespace Ovl.UI.DataDisplay.View;

/// <summary>
/// BasicData.xaml 的交互逻辑
/// </summary>
public partial class BasicData : UserControl
{
    private readonly IPipeCommunicatorService _pipeClient;
    public BasicData()
    {
        InitializeComponent();
        Unloaded += (s, e) => (DataContext as IDisposable)?.Dispose();
    }
}
