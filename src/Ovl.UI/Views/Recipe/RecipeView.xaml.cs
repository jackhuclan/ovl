using System.Windows.Controls;

namespace Ovl.UI.Views.Recipe;

/// <summary>
/// Recipe 页面（Sidebar 右侧内嵌展示，替代原版 RecipeWindow 弹窗；
/// 布局与页签样式沿用原版，配方树为本地等价实现）。
/// </summary>
public partial class RecipeView : UserControl
{
    public RecipeView()
    {
        InitializeComponent();
    }
}
