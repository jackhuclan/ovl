using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Marvel.DataModels.Recipe;
using Marvel.Interfaces.Application;
using Microsoft.Extensions.DependencyInjection;
using Prism.Services.Dialogs;
using Wafer.Algorithm.Common.DoNet;

using Ovl.UI.Controls;

namespace Ovl.UI.Recipe.ViewModel;

public partial class CorrectionValueSettingDialogViewModel : ObservableObject, IDialogAware
{
    private IServiceProvider _sp;

    /// <summary>
    /// FocusPosition
    /// </summary>
    [ObservableProperty]
    private double _focusPosition;

    [ObservableProperty]
    private double _correctionValue;

    [ObservableProperty]
    private string _unit;

    [ObservableProperty]
    private double selectedXPosition;

    [ObservableProperty]
    private double selectedYPosition;

    [ObservableProperty]
    private double _finalFousHeight;

    [ObservableProperty]
    private double _hightLightThreshHold;

    public CorrectionValueParam CorrectionValueParam { get; set; }

    [ObservableProperty]
    private ChartCurveControl _chart;

    public CorrectionValueSettingDialogViewModel(IServiceProvider sp)
    {
        _sp = sp;
    }

    [RelayCommand]
    private void PointSelected(object parameter)
    {
        var param = parameter as PointSelectedEventArgs;
        var zHeight = param.XPosition;
        CorrectionValue = zHeight - FocusPosition;
        CorrectionValueParam.CorrectionPointSelectedAction?.Invoke(CorrectionValueParam.IsInner, zHeight);
    }

    public string Title => "Correction Setting";

    public event Action<IDialogResult> RequestClose;

    public bool CanCloseDialog()
    {
        return true;
    }

    public void OnDialogClosed()
    {
        CloseView();
    }

    public void OnDialogOpened(IDialogParameters parameters)
    {
        CorrectionValueParam = parameters.GetValue<CorrectionValueParam>(nameof(CorrectionValueParam));
        FocusPosition = CorrectionValueParam.FocusPosition;
        Unit = CorrectionValueParam.Unit;
        CorrectionValue = CorrectionValueParam.CorrectionValue;
        HightLightThreshHold = CorrectionValueParam.HightLightThreshHold;

        GetFinalHeight();
    }

    [RelayCommand]
    void CloseView()
    {
        RequestClose?.Invoke(new DialogResult(ButtonResult.None));
    }

    partial void OnCorrectionValueChanged(double value)
    {
        CorrectionValueParam.CorrectionPointSelectedAction?.Invoke(CorrectionValueParam.IsInner, GetFinalHeight());
    }

    [RelayCommand]
    void Save()
    {
        CorrectionValueParam.SetFinalHeightAction?.Invoke(CorrectionValueParam.IsInner, GetFinalHeight(), CorrectionValue);
    }

    private double GetFinalHeight()
    {
        FinalFousHeight = FocusPosition + CorrectionValue;
        return FinalFousHeight;
    }

    [RelayCommand]
    void Reset()
    {
        ReturnToFocusPosition();
    }

    [RelayCommand]
    void Cancel()
    {
        CloseView();
    }

    void ReturnToFocusPosition()
    {
        // 设置Chart
        CorrectionValue = 0;
        CorrectionValueParam.CorrectionPointSelectedAction?.Invoke(CorrectionValueParam.IsInner, FocusPosition);
    }
}

public class CorrectionValueParam
{
    public CorrectionValueParam()
    {

    }

    public double FocusPosition { get; set; }

    public string Unit { get; set; }

    public Curve Curve { get; set; }

    public Action<bool, double> CorrectionPointSelectedAction;

    public Action<bool, double, double> SetFinalHeightAction;
    public bool IsInner { get; set; }
    public double CorrectionValue { get; set; }
    public double HightLightThreshHold { get; set; }

}
