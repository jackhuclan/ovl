using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InteVega.CBB.Prism.UI.Extensions.MessageBox;
using Marvel.Application.Services.TisOptimize;
using Marvel.DataModels.Recipe;
using Marvel.TargetSelectionControl.Control;
using Marvel.WaferMapCommon;
using Microsoft.Extensions.DependencyInjection;
using Prism.Services.Dialogs;

namespace Ovl.UI.Recipe.ViewModel;

public partial class TisOptFieldSelectViewModel : ObservableObject, IDialogAware
{
    private readonly IServiceProvider _prod;
    #region field

    IMessageBoxService _messageBoxService;
    #endregion

    #region binding property
    [ObservableProperty]
    private WaferMapControl _waferMapControl;
    [ObservableProperty]
    private TargetSettingControl _targetSettingControl;
    public WaferMapInfo WaferMapInfo { get; set; }
    public TestInfoBase TestInfo { get; set; }
    public TisInfoBase TisUtility { get; set; }
    #endregion

    private void BuildWaferMap()
    {
        WaferMapControl = new WaferMapControl
        {
            EditMode = EnumEditMode.Check
        };
        WaferMapControl.Draw(WaferMapInfo);
        WaferMapControl.Select(TestInfo.TargetSetting.GetSelectFields());
        WaferMapControl.Check(TisUtility.Locations.TisFields);
    }

    private void BuildTarget()
    {
        this.TargetSettingControl = new TargetSettingControl()
        {
            IsCheckable = true
        };

        TargetSettingControl.Refresh(TestInfo.TargetSetting.EnabledTargetPoints.ToList(),
                                     WaferMapInfo.CoordFieldWidth,
                                     WaferMapInfo.CoordFieldHeight);

        TargetSettingControl.Check(TisUtility.Locations.TisTargets?.Where(x => x.IsEnable).ToList());
    }

    public TisOptFieldSelectViewModel(IServiceProvider serviceProvider, IMessageBoxService messageBoxService)
    {
        _prod = serviceProvider.GetService<IServiceProvider>();
        _messageBoxService = serviceProvider.GetService<IMessageBoxService>();
    }

    public string Title => "Select";

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
        BuildData(parameters);
        BuildWaferMap();
        BuildTarget();
    }
    public void BuildData(IDialogParameters parameters)
    {
        TestInfo = parameters.GetValue<TestInfoBase>("TestInfo");
        TisUtility = parameters.GetValue<TisInfoBase>("TisInfo");
        WaferMapInfo = parameters.GetValue<WaferMapInfo>("WaferMapInfo");
    }
    [RelayCommand]
    void CloseView()
    {
        RequestClose?.Invoke(new DialogResult(ButtonResult.None));
    }
    [RelayCommand]
    void Submit()
    {
        var tisOptService = _prod.GetService<TisOptThresholdService>();
        TisUtility.Locations.TisFields = WaferMapControl.GetCheckedFields();
        TisUtility.Locations.TisTargets = TargetSettingControl.GetCheckedFields();
        var validateInfo = tisOptService.IsParamInRange(TisUtility);
        if (!validateInfo.isInRange)
        {
            _messageBoxService.ErrorDialog(validateInfo.msg);
            return;
        }
        RequestClose?.Invoke(new DialogResult(ButtonResult.OK));
    }
}
