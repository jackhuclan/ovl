using CommunityToolkit.Mvvm.ComponentModel;
using Ovl.UI.Navigation;

namespace Ovl.UI.Placeholder;

/// <summary>
/// 占位页 ViewModel：M2 骨架期各菜单项的临时页面，M3 起逐个替换为迁移的真实功能页。
/// </summary>
public partial class PlaceholderViewModel : ObservableObject, IPageViewModel
{
    [ObservableProperty]
    private string _title = string.Empty;
}
