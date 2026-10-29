using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InteVega.CBB.CommonLib.Classes;
using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.Prism.UI.Extensions.MessageBox;
using InteVega.CBB.TemplateMatching.Core;
using Marvel.DataModels.Recipe;
using Marvel.DataModels.Recipe.CD;
using Marvel.WaferMapCommon;
using Microsoft.Extensions.DependencyInjection;
using Prism.Events;
using Prism.Services.Dialogs;

using Ovl.UI.EventAggregators;

namespace Ovl.UI.Recipe.ViewModel;

public  partial  class Test_TargetViewModel:Test_SettingsViewModelBase
{
    public WaferMapInfo WaferMapInfo { get; private set; }
    public bool bShowAllTargetSelect = true;
    [ObservableProperty]
     Visibility _isShowTargetType = Visibility.Collapsed;
    [ObservableProperty]
    bool _isShowFieldCount = false;

    [ObservableProperty]
    bool _noShowFieldCount = true;

    partial void OnIsShowFieldCountChanged(bool value)
    {
        NoShowFieldCount = !_noShowFieldCount;
        _testInfo.TargetSetting.TargetSamefields = value;
        if (waferMapControlinstance == null || _testInfo.TargetSetting.TargetPoints2.Count == 0)
        {
            return;
        }
        if (value)
        {
            waferMapControlinstance.UnSelectAll();
            waferMapControlinstance.Select(_testInfo.TargetSetting.Fields);
        }
        else
        {
            waferMapControlinstance.UnSelectAll();
            if (_testInfo.TargetSetting.TargetPoints2.Count > 0 && (SelectIndex < 0 || SelectIndex > _testInfo.TargetSetting.TargetPoints2.Count))
            {
                waferMapControlinstance.Select(_testInfo.TargetSetting.TargetPoints2[0].Selectfields);
                return;
            }
            else
            {
                if (SelectIndex < 0)
                {
                    return;
                }
                waferMapControlinstance.Select(_testInfo.TargetSetting.TargetPoints2[SelectIndex].Selectfields);
            }

        }
    }

    [ObservableProperty]
    TestInfoBase _testInfo;

    [ObservableProperty]
    bool _isTarSetVisible;

    [ObservableProperty]
    int _selectIndex ;
    partial void OnSelectIndexChanged(int oldValue, int newValue)
    {
        if (IsShowFieldCount)//Target使用相同的feildID返回
        {
            return;
        }
        if (bShowAllTargetSelect && newValue == -1)
        {
            return;
        }
        if (oldValue >= 0 && oldValue < _testInfo.TargetSetting.TargetPoints2.Count && newValue < 0)
        {
            bShowAllTargetSelect = false;
            SelectIndex = oldValue;
            return;
        }
        if (newValue < 0)
        {
            return;
        }
        waferMapControlinstance.UnSelectAll();
        waferMapControlinstance.Select(_testInfo.TargetSetting.TargetPoints2[newValue].Selectfields);
        bShowAllTargetSelect = false;
    }

    private readonly IMessageBoxService _messageBox;
    private IDialogService _dialogService;
    private ITemplateMatchingService _templateMatchingService;
    public Test_TargetViewModel(IServiceProvider serviceProvider, IEventAggregator eventAggregator, IMessageBoxService messageBoxService, IDialogService dialogService) : base(eventAggregator)
    {
        _templateMatchingService = serviceProvider.GetService<ITemplateMatchingService>();
        _dialogService = dialogService;
        _messageBox = messageBoxService;
        _isTarSetVisible = true;
    }


    [ObservableProperty]
    private WaferMapControl waferMapControlinstance;
    [ObservableProperty]
    private Canvas targetsPostionCanvas;
    public override void TestInfoRecive(TestInfoAndWaferMap testInfoAndWaferMap)
    {
        TestInfo = testInfoAndWaferMap.TestInfo.TestInfo;
        //Save();//保存旧Test数据
        IsShowTargetType = TestInfo is TestInfo_AIM || TestInfo is TestInfo_AIM_XYS ? Visibility.Visible : Visibility.Collapsed;

        LogHelper.LogInfo($"Set test target:TestName={TestInfo.Name}");
        WaferMapInfo = testInfoAndWaferMap.WaferMapInfo;
        IsShowFieldCount = _testInfo.TargetSetting.TargetSamefields;
        WaferMapControlinstance = new WaferMapControl() { IsEnableScale = false };
        WaferMapControlinstance.ClickSelectAction = SelectFieldSave;
        WaferMapControlinstance.Draw(testInfoAndWaferMap.WaferMapInfo);
        if (IsShowFieldCount)
        {
            waferMapControlinstance.Select(_testInfo.TargetSetting.Fields);
        }
        else
        {
            if (_testInfo.TargetSetting.TargetPoints2.Count > 0)
            {
                waferMapControlinstance.Select(_testInfo.TargetSetting.TargetPoints2[0].Selectfields);
            }
        }
        SetVisibleByTestType();
        Applay();
        TargetsChanged?.Invoke();
    }
    private void SetVisibleByTestType()
    {
        if(TestInfo is TestInfo_CD)
        {
            // CD量测时Target Centering选项不显示
            IsTarSetVisible = false;
        }
        else
        {
            IsTarSetVisible = true;
        }
    }
    [RelayCommand]
    void CheckALL()
    {
        //TestInfo.Fields?.Clear();
        //for (int i = 0; i < WaferMapInfo.TisFields.Count; i++)
        //{
        //    TestInfo.Fields.Add(new RowColumnInfo() { RowIndex = WaferMapInfo.TisFields[i].RowIndex, ColumnIndex = WaferMapInfo.TisFields[i].ColumnIndex });
        //}
        waferMapControlinstance.SelectAll();
        SelectFieldSave();
    }
    [RelayCommand]
    void UnCheckAll()
    {
        waferMapControlinstance.UnSelectAll();
        SelectFieldSave();
        //WaferMapControlinstance.Draw(WaferMapInfo);
        //TestInfo.Fields?.Clear();
        //waferMapControlinstance.Select(TestInfo.Fields);
    }

    public override void Save()
    {
        //if (WaferMapInfo != null && _testInfo!=null )
        //{
        //    if (WaferMapControlinstance != null)
        //    {
        //        List<RowColumnInfo> selectedRowColumnInfos = WaferMapControlinstance.GetSelectedFields();
        //        _testInfo.TargetSetting.Fields.Clear();
        //        for (int i = 0; i < selectedRowColumnInfos.Count; i++)
        //        {
        //            _testInfo.TargetSetting.Fields.Add(new RowColumnInfo() { RowIndex = selectedRowColumnInfos[i].RowIndex, ColumnIndex = selectedRowColumnInfos[i].ColumnIndex });
        //        }

        //    }
        //}


    }
    //
    //打开target设置页面
    [RelayCommand]
    private void OpenTargetSettingWindow()
    {
        var parameters = new DialogParameters
        {
            { nameof (TestInfoBase), _testInfo}

        };
        _dialogService.ShowDialog("Test_SetR3", parameters, result =>
        {
            Applay();
            Save();
        });
    }

    [RelayCommand]
    private void ShowAllTargetSelect()
    {
        bShowAllTargetSelect = true;
        SelectIndex = -1;
        waferMapControlinstance.UnSelectAll();
        waferMapControlinstance.Select(_testInfo.TargetSetting.GetSelectFields());
    }

    public void SelectFieldSave()
    {
        if (_testInfo.TargetSetting.TargetSamefields)
        {
            if (WaferMapInfo != null && _testInfo != null)
            {
                if (WaferMapControlinstance != null)
                {
                    List<RowColumnInfo> selectedRowColumnInfos = WaferMapControlinstance.GetSelectedFields();
                    _testInfo.TargetSetting.Fields.Clear();
                    for (int i = 0; i < selectedRowColumnInfos.Count; i++)
                    {
                        _testInfo.TargetSetting.Fields.Add(new RowColumnInfo() { RowIndex = selectedRowColumnInfos[i].RowIndex, ColumnIndex = selectedRowColumnInfos[i].ColumnIndex });
                    }

                }
            }
        }
        else
        {
            if (WaferMapInfo != null && _testInfo != null)
            {
                if (WaferMapControlinstance != null)
                {
                    List<RowColumnInfo> selectedRowColumnInfos = WaferMapControlinstance.GetSelectedFields();
                    if (SelectIndex < 0 || bShowAllTargetSelect)
                    {
                        return;
                    }
                    _testInfo.TargetSetting.TargetPoints2[SelectIndex].Selectfields.Clear();
                    for (int i = 0; i < selectedRowColumnInfos.Count; i++)
                    {
                        _testInfo.TargetSetting.TargetPoints2[SelectIndex].Selectfields.Add(new RowColumnInfo() { RowIndex = selectedRowColumnInfos[i].RowIndex, ColumnIndex = selectedRowColumnInfos[i].ColumnIndex });
                    }

                }
            }
        }
        OnTargetsChanged();
    }
}
