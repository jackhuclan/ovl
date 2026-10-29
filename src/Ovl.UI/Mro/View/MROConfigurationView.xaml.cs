using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Marvel.DataModels.Job;

using Ovl.UI.Recipe.ViewModel;

namespace Ovl.UI.Recipe.View;

/// <summary>
/// MROConfigurationView.xaml 的交互逻辑
/// </summary>
public partial class MROConfigurationView : Window
{
    public MROConfigurationView(IServiceProvider serviceProvider, string RecipeId)
    {
        InitializeComponent();
        this.DataContext = new MROConfigurationViewModel(serviceProvider, RecipeId);
        // 在加载完成后居中
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        if (DataContext is MROConfigurationViewModel viewModel)
        {
            // 订阅关闭事件
            viewModel.RequestClose += (s, e) => Close();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        if (DataContext is MROConfigurationViewModel viewModel)
        {
            // 清理
            viewModel.RequestClose -= (s, e) => Close();
        }
        base.OnClosed(e);
    }
}
