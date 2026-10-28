using CommunityToolkit.Mvvm.ComponentModel;
using Ovl.Biz.Common.Equipment;
using Ovl.UI.ViewModels.Navigation;

namespace Ovl.UI.ViewModels.Shell;

/// <summary>
/// 主窗口 Shell：左侧竖向 Sidebar + 右侧内容区 + 底部 Footer。
/// 点击 Sidebar 菜单项后经导航服务在内容区内嵌切换页面。
/// </summary>
public partial class ShellViewModel : ObservableObject
{
    private readonly INavigationService _navigation;

    public ShellViewModel(INavigationService navigation, IEquipment? equipment = null)
    {
        _navigation = navigation;
        EquipmentName = equipment?.Name ?? "-";
        _navigation.CurrentChanged += OnNavigationCurrentChanged;

        Items =
        [
            new MenuItemViewModel("Measure", "Home", "\uE80F"),
            new MenuItemViewModel("StageControl", "Stage Control", "\uE7F8"),
            new MenuItemViewModel("WaferHandler", "Wafer Handler", "\uE8AB"),
            new MenuItemViewModel("Diagnostics", "Diagnostics", "\uE9D9"),
            new MenuItemViewModel("Batch", "Batch", "\uE8FD"),
            new MenuItemViewModel("Recipe", "Recipe", "\uE8A5"),
            new MenuItemViewModel("Result", "Result", "\uE9F9"),
            new MenuItemViewModel("Setting", "Setting", "\uE713"),
            new MenuItemViewModel("Help", "Help", "\uE897"),
            new MenuItemViewModel("Motion", "Motion", "\uE768"),
            new MenuItemViewModel("Defect", "Defect", "\uE7BE"),
            new MenuItemViewModel("Alarm", "Alarm", "\uE7BA"),
        ];
        SelectedItem = Items[0];
    }

    /// <summary>Sidebar 菜单项。</summary>
    public IReadOnlyList<MenuItemViewModel> Items { get; }

    /// <summary>当前设备名（Footer 展示）。</summary>
    public string EquipmentName { get; }

    /// <summary>程序版本（Footer 展示）。</summary>
    public string Version => typeof(ShellViewModel).Assembly.GetName().Version?.ToString(3) ?? "-";

    /// <summary>当前内嵌展示的页面 ViewModel（ContentControl 绑定）。</summary>
    [ObservableProperty]
    private object? _currentPage;

    /// <summary>Sidebar 当前选中项。</summary>
    [ObservableProperty]
    private MenuItemViewModel? _selectedItem;

    partial void OnSelectedItemChanged(MenuItemViewModel? value)
    {
        if (value is not null)
        {
            _navigation.NavigateTo(value.Key);
        }
    }

    private void OnNavigationCurrentChanged(object? sender, EventArgs e)
    {
        CurrentPage = _navigation.Current;
    }
}
