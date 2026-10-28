using System.Windows;
using System.Windows.Controls;

namespace Ovl.UI.Common.Controls.Icon;

/// <summary>
/// 图标控件：按 <see cref="Kind"/> 从资源字典中查找同名 ControlTemplate 渲染矢量图标。
/// 移植自厂内 InteVega.CBB.Prism.UI.Extensions.Icon.CustomIcon（源码在 marvel 仓库，只读参考），
/// 行为与 Kind 取值保持一致，仅命名空间改为本项目，以保证原版 XAML 中的图标显示不变。
/// </summary>
public class CustomIcon : Control
{
    static CustomIcon()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(CustomIcon), new FrameworkPropertyMetadata(typeof(CustomIcon)));
    }

    public static readonly DependencyProperty KindProperty = DependencyProperty.Register(
        nameof(Kind),
        typeof(MegaIconKind),
        typeof(CustomIcon),
        new FrameworkPropertyMetadata(
            MegaIconKind.None,
            FrameworkPropertyMetadataOptions.AffectsArrange,
            new PropertyChangedCallback(OnKindPropertyChanged)));

    public MegaIconKind Kind
    {
        get => (MegaIconKind)GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    private static void OnKindPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not CustomIcon control)
        {
            return;
        }

        try
        {
            control.Template = control.TryFindResource(control.Kind.ToString()) as ControlTemplate;
        }
        catch
        {
            // 与原始实现一致：找不到模板时忽略
        }
    }
}

/// <summary>图标种类（与厂内 MegaIconKind 完全一致，勿改名以免模板键失效）。</summary>
public enum MegaIconKind
{
    None,
    Pencil,
    PauseCircle,
    WindowMin,
    WindowMax,
    WindowRestore,
    WindowClose,
    Mega,
    Selected,
    UnSelected,
    WaferHandler,
    Recipe,
    Setting,
    Batch,
    StageControl,
    DailyPM,
    Result,
    Help,
    User,
    Alarm,
    RecipeAdd,
    RecipeFoledAdd,
    RecipeCopy,
    RecipePaste,
    RecipeUpdate,
    RecipeDelete,
    RecipeOpen,
    RecipeFoled,
    RecipeFile,
    ButtonRunIcon,
    ButtonSuspendIcon,
    ButtonStopIcon,
    ButtonSuddenStopIcon,
    ButtonAddIcon,
    ButtonClearErrorIcon,
    ButtonInitIcon,
    ButtonZeroingIcon,
    ButtonEmergencyStopIcon,
    ButtonMinRunIcon,
    ButtonMinStopIcon,
    MoveUpIcon,
    MoveDownIcon,
    RefreshIcon,
    ButtonEditIcon,
    Sub,
    Camera,
    LiveCamera,
    SaveImage,
    Folder,
    AddROI,
    Pointer,
    PanHand,
    FullScreen,
    RefreshScreen,
}
