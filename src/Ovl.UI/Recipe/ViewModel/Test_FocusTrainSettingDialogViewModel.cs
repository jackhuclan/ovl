using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InteVega.CBB.Common;
using InteVega.CBB.CommonLib.Helpers.Reflection;
using InteVega.CBB.DomainService.Api.Interfaces;
using InteVega.CBB.Prism.UI.Extensions.MessageBox;
using Marvel.Application.Extentions;
using Marvel.DataModels.Core;
using Marvel.DataModels.Recipe;
using Marvel.Interfaces.Application;
using Megarobo.Ovl.Service.IntfFn.fn;
using Prism.Services.Dialogs;

namespace Ovl.UI.Recipe.ViewModel;

public partial class Test_FocusTrainSettingDialogViewModel : ObservableObject, IDialogAware
{
    public Test_FocusTrainSettingDialogViewModel(IMessageBoxService messageBoxService, ITestService  testService)
    {
        _messageBoxService = messageBoxService;
        _testService = testService;
    }
    [ObservableProperty]
    private ObservableCollection<PropertyViewModel> _propertyViewModels;
    /// <summary>
    /// 是否使用系统默认的聚焦参数
    /// </summary>
    [ObservableProperty]
    bool _isUseSystemDefault;
    private Action<FocusInfo> _saveAction;
    private FocusInfo _testFocusInfo;
    protected FocusInfo _clonedFocusInfo;
    IMessageBoxService _messageBoxService;
    ITestService _testService;
    public string Title => "Focus Setting";

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
        _testFocusInfo = parameters.GetValue<FocusInfo>("FocusInfo");
        _saveAction = parameters.GetValue<Action<FocusInfo>>("SaveAction");
        if (_testFocusInfo == null)
        {
            _testFocusInfo = _testService.GetSystemFocusInfo(IServiceCamera.CurCamera);
            IsUseSystemDefault = true;
        }
        CreatePropertyViewModels(_testFocusInfo);
    }

    protected virtual void CreatePropertyViewModels(FocusInfo focusInfo)
    {
        _clonedFocusInfo = CloneHelper.ReflectionClone(focusInfo ) as FocusInfo;
        PropertyViewModels = new ObservableCollection<PropertyViewModel>(PropertyViewModelFactory.CreatePropertyModels(_clonedFocusInfo));
    }

    [RelayCommand]
    void ResetDefault()
    {
        var focusInfo = IsUseSystemDefault ? _testService.GetSystemFocusInfo(IServiceCamera.CurCamera) : _testFocusInfo;
        CreatePropertyViewModels(focusInfo);
    }

    [RelayCommand]
    void CloseView()
    {
        //DialogResult result = default;
        //if(isSubmit)
        //{
        //    result = new DialogResult(ButtonResult.OK, new DialogParameters() { { "FocusInfo", _clonedFocusInfo } });
        //}
        //else
        //{
        //    result = new DialogResult(ButtonResult.Cancel, new DialogParameters() { { "FocusInfo", _focusInfo } });
        //}
        RequestClose?.Invoke(new DialogResult(ButtonResult.None));
    }
    private bool isSubmit = false;
    [RelayCommand]
    void Submit()
    {
        isSubmit = true;
        bool isValid = CheckFocusData(_clonedFocusInfo.CourseFocusInfo, "coarse");
        if (!isValid)
        {
            return;
        }
        isValid = CheckFocusData(_clonedFocusInfo.FineFocusInfo, "fine");
        if (!isValid)
        {
            return;
        }
        if (IsUseSystemDefault)
            _clonedFocusInfo = null;
        _saveAction?.Invoke(_clonedFocusInfo);
        _messageBoxService.Success("Data saved successfully！", "TestTrainMessage");
        _messageBoxService.Success("Data saved successfully！", "TestTrainCDMessage");
        RequestClose?.Invoke(new DialogResult(ButtonResult.OK));
        // Focus Info参数会即时刷新到属性里面，不需要额外再设置
    }

    private bool CheckFocusData(AutoRunInfo focusInfo, string type)
    {
        string error = focusInfo.CheckFocusData(type);
        if (!string.IsNullOrWhiteSpace(error))
        {
            _messageBoxService.ErrorDialog(error);
            return false;
        }
        return true;
    }
}
