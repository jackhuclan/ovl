namespace Ovl.UI.ViewModels.Shell;

/// <summary>
/// Sidebar 中的一个菜单项。
/// </summary>
public sealed class MenuItemViewModel(string key, string title, string glyph)
{
    /// <summary>导航键（与 NavigationService 注册键一致）。</summary>
    public string Key { get; } = key;

    /// <summary>显示标题。</summary>
    public string Title { get; } = title;

    /// <summary>图标字符（Segoe MDL2 Assets 字形）。</summary>
    public string Glyph { get; } = glyph;
}
