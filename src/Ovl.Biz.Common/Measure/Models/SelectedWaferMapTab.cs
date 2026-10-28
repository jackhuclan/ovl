namespace Ovl.Biz.Common.Measure.Models;

/// <summary>
/// Wafer Map 页签选择（原样移植自 Marvel.App.Shared.model.SelectedWaferMapTab）。
/// XAML 中以 x:Static 传给 SelectWaferMapCommand 的 CommandParameter。
/// </summary>
public enum SelectedWaferMapTab
{
    /// <summary>Misreg Wafer Map 页签。</summary>
    WaferMap,

    /// <summary>Qmerit Wafer Map 页签。</summary>
    QmeritWaferMap,
}
