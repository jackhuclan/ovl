using CommunityToolkit.Mvvm.ComponentModel;

namespace Ovl.UI.Measure;

/// <summary>
/// 主量测页面 MRO（离线光谱/相位/滤波等算法自检）进度区状态。
/// 属性名与原版 mainviewmodel.cs 中 MRO 面板绑定路径一致，
/// 界面控件 MRONewProgressBoard / MroAutoTrainBoard 原样迁入后可直接绑定。
/// </summary>
public sealed partial class MainViewModel
{
    [ObservableProperty]
    private string _mroType = string.Empty;

    [ObservableProperty]
    private string _mroCycleProgress = "-";

    [ObservableProperty]
    private string _mroTestProgress = "-";

    [ObservableProperty]
    private string _mroRemainTime = "-";

    [ObservableProperty]
    private string _mroConfigProgress = "0/0";

    [ObservableProperty]
    private double _mroConfigProgressValue;

    [ObservableProperty]
    private string _mroTargetProgress = "0/0";

    [ObservableProperty]
    private double _mroTargetProgressValue;

    [ObservableProperty]
    private string _mroNAProgress = "-";

    [ObservableProperty]
    private string _mroPolarProgress = "-";

    [ObservableProperty]
    private string _mroFilterProgress = "-";

    [ObservableProperty]
    private string _mroBandProgress = "-";

    [ObservableProperty]
    private string _mroWaveProgress = "-";
}
