using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dm;
using InteVega.CBB.ComUI.Helpers;
using InteVega.CBB.DomainService.Api.Interfaces;
using InteVega.CBB.Recipe.Interface;
using InteVega.CBB.StageControlModule.ViewModels;
using Marvel.DataModels.Recipe;
using Marvel.Interfaces.Application;
using Marvel.MRO.DataModels.Models;
using Marvel.WaferMapCommon;
using Megarobo.Ovl.Service.IntfFn.fn;
using Microsoft.Extensions.DependencyInjection;
using Org.BouncyCastle.Math.EC.Multiplier;
using OVL.Algorithm.DoNet;
using Wafer.Algorithm.Common.DoNet;
using Bases = Ovl.UI.Bases;
using static NPOI.HSSF.UserModel.HeaderFooter;

namespace Ovl.UI.Recipe.ViewModel;

partial class ParametersViewModel : Bases.ViewModelBase
{
    protected IMroService? _mroService;
    public RecipeElementsInfo recipeElementsInfo;
    public WaferMapInfo waferMapInfo = new WaferMapInfo();
    public CycleParameter _CyccleParameter { get; set; } = new CycleParameter();

    public ParametersViewModel(IServiceProvider serviceProvider, CycleParameter NowcycleParameter, RecipeElementsInfo _recipeElementsInfo,bool Enable,bool FirstCycle)
    {
        recipeElementsInfo = _recipeElementsInfo;
        _CyccleParameter = NowcycleParameter;
        FocusScanStepEnable = Enable;
        ROIEnable = !FirstCycle;
        InitializeData();
    }
    private void InitializeData()
    {
        #region Measurement
        IsEnable = _CyccleParameter.MeasurementParam.IsEnable;//
        IsTIS = _CyccleParameter.MeasurementParam.IsExcuteTis;//
        FocusScanRange = _CyccleParameter.MeasurementParam.FocusScanRange;//
        FocusScanStep = _CyccleParameter.MeasurementParam.FocusScanStep;//
        //FocusScanStep = _CyccleParameter.MeasurementParam.FocusScanStep;//
        ANRA = _CyccleParameter.MeasurementParam.AnraCount;//
        if (ROIEnable)
        {
            IsROI_OptimizeEnable = _CyccleParameter.ARORoiAdjustParam.IsEnable;
            ROIOffsetAdjustRange = _CyccleParameter.ARORoiAdjustParam.RoiOffsetAdjustRange;
            ROIOffsetAdjustStep = _CyccleParameter.ARORoiAdjustParam.RoiOffsetAdjustStep;
            ROISizeAdjustRange = _CyccleParameter.ARORoiAdjustParam.RoiSizeAdjustRange;
            ROISizeAdjustStep = _CyccleParameter.ARORoiAdjustParam.RoiSizeAdjustStep;
        }

        #endregion

        #region Sites
        waferMapInfo = recipeElementsInfo.WaferMap;
        //waferMapInfo.FieldInfos
        WaferMapControl.EditMode = EnumEditMode.MultiSelect;
        WaferMapControl.RedrawByLlc(waferMapInfo);
        if (_CyccleParameter.SitesParam.Fields.Count <= 0 && _CyccleParameter.CyCleIdenx == 1)
        {
            _CyccleParameter.SitesParam.Fields.Add(new RowColumnInfo(0, 0));
        }
        else if(_CyccleParameter.SitesParam.Fields.Count <= 0)
        {
            _CyccleParameter.SitesParam.Fields.Add(new RowColumnInfo(0, 0));
            _CyccleParameter.SitesParam.Fields.Add(new RowColumnInfo(-2, 0));
            _CyccleParameter.SitesParam.Fields.Add(new RowColumnInfo(2, 0));
            _CyccleParameter.SitesParam.Fields.Add(new RowColumnInfo(-2, 2));
            _CyccleParameter.SitesParam.Fields.Add(new RowColumnInfo(0, 2));
            _CyccleParameter.SitesParam.Fields.Add(new RowColumnInfo(-2, -2));
            _CyccleParameter.SitesParam.Fields.Add(new RowColumnInfo(0, -2));
            _CyccleParameter.SitesParam.Fields.Add(new RowColumnInfo(2, 2));
            _CyccleParameter.SitesParam.Fields.Add(new RowColumnInfo(2, -2));
        }
        WaferMapControl.Select(_CyccleParameter.SitesParam.Fields);
        WaferMapControl.IsEnableScale = false;
        TrainSiteSampling = _CyccleParameter.SitesParam.IsUseSampling;
        SamplingplanNumber = _CyccleParameter.SitesParam.SamplingCount;
        #endregion

        #region Output
        MeasRankFlagSelected = _CyccleParameter.PutParam.RankParam.MeasRankFlag;
        OptRankFlagSelected = _CyccleParameter.PutParam.RankParam.OptRankFlag;
        FocusRange = _CyccleParameter.PutParam.TrivialRemovalParam.TrivialFocusRange;
        WLRange = _CyccleParameter.PutParam.TrivialRemovalParam.TrivialWLRange;
        AcceptedNumber = _CyccleParameter.PutParam.OutNumberOfCandidates;
        #endregion

        #region Data Removal
        DataRemovalSelected = _CyccleParameter.DataRemovalParam.DataRemoval;
        RemovePercentage = _CyccleParameter.DataRemovalParam.RemovePercentage;
        Multiplier = _CyccleParameter.DataRemovalParam.SigmaMultiplier;
        #endregion

    }

    #region Measurement
    [ObservableProperty]
    private bool _isEnable = true;

    partial void OnIsEnableChanged(bool value)
    {
        if (_CyccleParameter.CyCleIdenx == 1 && !value)//第一轮不支持关闭
        {
            IsEnable = true;
            return;
        }
        _CyccleParameter.MeasurementParam.IsEnable = value;
    }
    [ObservableProperty]
    private bool _isTIS = true;
    partial void OnIsTISChanged(bool value)
    {
        _CyccleParameter.MeasurementParam.IsExcuteTis = value;
    }
    [ObservableProperty]
    private double _focusScanRange = 4.5;
    partial void OnFocusScanRangeChanged(double value)
    {
        _CyccleParameter.MeasurementParam.FocusScanRange = value;
    }
    [ObservableProperty]
    private double _focusScanStep = 4.6;
    partial void OnFocusScanStepChanged(double value)
    {
        _CyccleParameter.MeasurementParam.FocusScanStep = value;
    }

    [ObservableProperty]
    private bool _focusScanStepEnable = true;

    [ObservableProperty]
    private int _aNRA = 4;
    partial void OnANRAChanged(int value)
    {
        _CyccleParameter.MeasurementParam.AnraCount = value;
    }

    [ObservableProperty]
    private bool _ROIEnable = false;

    [ObservableProperty]
    private bool _isROI_OptimizeEnable = false;

    partial void OnIsROI_OptimizeEnableChanged(bool value)
    {
        _CyccleParameter.ARORoiAdjustParam.IsEnable = value;
    }

    [ObservableProperty]
    private double _rOIOffsetAdjustRange = 0;
    partial void OnROIOffsetAdjustRangeChanged(double value)
    {
        _CyccleParameter.ARORoiAdjustParam.RoiOffsetAdjustRange = value;
    }
    [ObservableProperty]
    private double _rOIOffsetAdjustStep = 0;
    partial void OnROIOffsetAdjustStepChanged(double value)
    {
        _CyccleParameter.ARORoiAdjustParam.RoiOffsetAdjustStep = value;
    }

    [ObservableProperty]
    private double _rOISizeAdjustRange = 0;
    partial void OnROISizeAdjustRangeChanged(double value)
    {
        _CyccleParameter.ARORoiAdjustParam.RoiSizeAdjustRange = value;
    }

    [ObservableProperty]
    private double _rOISizeAdjustStep = 0;
    partial void OnROISizeAdjustStepChanged(double value)
    {
        _CyccleParameter.ARORoiAdjustParam.RoiSizeAdjustStep = value;
    }
    #endregion

    #region Sites

    [ObservableProperty]
    private WaferMapControl _waferMapControl = new WaferMapControl() { EditMode = EnumEditMode.Check };

    [ObservableProperty]
    private bool _trainSiteSampling  = true;

    partial void OnTrainSiteSamplingChanged(bool value)
    {
        _CyccleParameter.SitesParam.IsUseSampling = value;
    }
    [ObservableProperty]
    private int _samplingplanNumber= 5;
    partial void OnSamplingplanNumberChanged(int value)
    {
        _CyccleParameter.SitesParam.SamplingCount = value;
    }
    [RelayCommand]
    private void Selectall()
    {
        WaferMapControl.SelectAll();
    }

    [RelayCommand]
    private void ClearAll()
    {
        WaferMapControl.UnSelectAll();
    }
    #endregion

    #region Output
    [ObservableProperty]
    private Array _measRankFlagList = Enum.GetValues(typeof(RankType));

    [ObservableProperty]
    private RankType _measRankFlagSelected;
    partial void OnMeasRankFlagSelectedChanged(RankType value)
    {
        _CyccleParameter.PutParam.RankParam.MeasRankFlag = value;
    }
    [ObservableProperty]
    private Array _optRankFlagList = Enum.GetValues(typeof(RankType));

    [ObservableProperty]
    private RankType _optRankFlagSelected;
    partial void OnOptRankFlagSelectedChanged(RankType value)
    {
        _CyccleParameter.PutParam.RankParam.OptRankFlag = value;
    }
    [ObservableProperty]
    private double _focusRange = 5.0;
    partial void OnFocusRangeChanged(double value)
    {
        _CyccleParameter.PutParam.TrivialRemovalParam.TrivialFocusRange = value;
    }
    [ObservableProperty]
    private double _wLRange = 5.1;
    partial void OnWLRangeChanged(double value)
    {
        _CyccleParameter.PutParam.TrivialRemovalParam.TrivialWLRange = value;
    }
    [ObservableProperty]
    private int _acceptedNumber = 5;
    partial void OnAcceptedNumberChanged(int value)
    {
        _CyccleParameter.PutParam.OutNumberOfCandidates = value;
    }

    #endregion

    #region Data Removal

    [ObservableProperty]
    private Array _dataRemovalList = Enum.GetValues(typeof(DataRemoval));

    [ObservableProperty]
    private DataRemoval _dataRemovalSelected;
    partial void OnDataRemovalSelectedChanged(DataRemoval value)
    {
        _CyccleParameter.DataRemovalParam.DataRemoval = value;
    }
    [ObservableProperty]
    private int _removePercentage = 5;
    partial void OnRemovePercentageChanged(int value)
    {
        _CyccleParameter.DataRemovalParam.RemovePercentage = value;
    }
    [ObservableProperty]
    private int _multiplier = 5;
    partial void OnMultiplierChanged(int value)
    {
        _CyccleParameter.DataRemovalParam.SigmaMultiplier = value;
    }
    #endregion
}
