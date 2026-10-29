using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using InteVega.CBB.TemplateMatchingModule.Views;

namespace Ovl.UI.Controls;

/// <summary>
/// PositioningModelControl.xaml 的交互逻辑
/// </summary>
public partial class PositioningModelControl : UserControl
{

    /// <summary>
    /// 定义IsPositioningEnabled依赖属性
    /// </summary>
    public static readonly DependencyProperty IsPositioningEnabledProperty =
        DependencyProperty.Register(
            "IsPositioningEnabled",
            typeof(bool),
            typeof(PositioningModelControl),
            new PropertyMetadata(false));

    /// <summary>
    /// 定义IsForEachTarget依赖属性
    /// </summary>
    public static readonly DependencyProperty IsForEachTargetProperty =
        DependencyProperty.Register(
            "IsForEachTarget",
            typeof(bool),
            typeof(PositioningModelControl),
            new PropertyMetadata(false));

    /// <summary>
    /// 可选：覆盖 DataContext 上的 TestPositionCommand（如 CrossFov Inner/Outer 分命令）。
    /// </summary>
    public static readonly DependencyProperty TestPositionCommandProperty =
        DependencyProperty.Register(
            nameof(TestPositionCommand),
            typeof(ICommand),
            typeof(PositioningModelControl),
            new PropertyMetadata(null));

    /// <summary>
    /// 可选：覆盖 DataContext 上的 FindTarget0Command（如 CrossFov Inner/Outer 分命令）。
    /// </summary>
    public static readonly DependencyProperty FindTarget0CommandProperty =
        DependencyProperty.Register(
            nameof(FindTarget0Command),
            typeof(ICommand),
            typeof(PositioningModelControl),
            new PropertyMetadata(null));

    /// <summary>
    /// IsPositioningEnabled属性包装器
    /// </summary>
    public bool IsPositioningEnabled
    {
        get { return (bool)GetValue(IsPositioningEnabledProperty); }
        set { SetValue(IsPositioningEnabledProperty, value); }
    }

    /// <summary>
    /// IsForEachTarget属性包装器
    /// </summary>
    public bool IsForEachTarget
    {
        get { return (bool)GetValue(IsForEachTargetProperty); }
        set { SetValue(IsForEachTargetProperty, value); }
    }

    public ICommand TestPositionCommand
    {
        get { return (ICommand)GetValue(TestPositionCommandProperty); }
        set { SetValue(TestPositionCommandProperty, value); }
    }

    public ICommand FindTarget0Command
    {
        get { return (ICommand)GetValue(FindTarget0CommandProperty); }
        set { SetValue(FindTarget0CommandProperty, value); }
    }

    public PositioningModelControl()
    {
        InitializeComponent();
    }

    public SingleTemplateControl TemplateControl { get { return positionTemplateControl; } }
}
