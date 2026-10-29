using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using InteVega.CBB.Common;
using InteVega.CBB.Common.Algorithm.Shapes;
using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.DomainService.Api.Extensions;
using InteVega.CBB.DomainService.Api.Interfaces;
using Marvel.Application.Services;
using Marvel.DataModels.Enums;
using Marvel.DataModels.Recipe;
using Marvel.DataModels.Recipe.Overlay;
using MathNet.Numerics;
using Megarobo.Ovl.Service.IntfFn.fn;

namespace Ovl.UI.Recipe.ViewModel;

public partial class Test_TrainViewModel
{
    bool _isCPLMode;
    [ObservableProperty]
    CPLParamViewModel _cplParamViewModel_Inner;

    [ObservableProperty]
    CPLParamViewModel _cplParamViewModel_Outer;

    FocusPegResult _focusRes_Inner;
    FocusPegResult _focusRes_Outer;
    double? _wliFocus_Inner;
    double ? _wliFocus_Outer;
    private void InitCPLParamViewModel()
    {
        _isCPLMode = Physical.TestSettings.GrabMode == EnumGrabMode.CPL;
        CplParamViewModel_Inner = new CPLParamViewModel(_serviceProvider, _isCPLMode, TestTrainSetting.TrainParam.InnerLayer.CplParam);
        CplParamViewModel_Outer = new CPLParamViewModel(_serviceProvider, _isCPLMode, TestTrainSetting.TrainParam.OuterLayer.CplParam);
    }
    private void SetCPLParam(bool isInner)
    {
        if (!_isCPLMode) { return; }
        CPLParamViewModel cplvm = isInner ? CplParamViewModel_Inner : CplParamViewModel_Outer;
        CPLParam param = isInner ? TestTrainSetting.TrainParam.InnerLayer.CplParam : TestTrainSetting.TrainParam.OuterLayer.CplParam;
        cplvm?.SetCPLParam(param);
    }

    private void ConfirmCPLParam(bool isInner)
    {
        if (!_isCPLMode) { return; }
        CPLParamViewModel cplvm = isInner ? CplParamViewModel_Inner : CplParamViewModel_Outer;
        var param = cplvm.GetCPLParam();
        if (isInner)
        {
            TestTrainSetting.TrainParam.InnerLayer.CplParam = param;

        }
        else
        {
            TestTrainSetting.TrainParam.OuterLayer.CplParam = param;
        }
        cplvm.SetCPLParam(null);
    }

    private void TrainFineFocus_CPL()
    {
        HpImageViewer.UpdateROIs(TestTrainSetting.TrainParam.InnerLayer.FocusROI);
        CplParamViewModel_Inner.SetCPLParam(TestTrainSetting.TrainParam.InnerLayer.CplParam);
        _focusRes_Inner = TrainGlobal();
        _wliFocus_Inner = TrainWLI_CPL(true);
        InnerPztOffset = TrainRoiFocus(_focusRes_Inner, true, false);


        HpImageViewer.UpdateROIs(TestTrainSetting.TrainParam.OuterLayer.FocusROI);
        CplParamViewModel_Outer.SetCPLParam(TestTrainSetting.TrainParam.OuterLayer.CplParam);
        _focusRes_Outer = TrainGlobal();
        _wliFocus_Outer = TrainWLI_CPL(true);
        OuterPztOffset = TrainRoiFocus(_focusRes_Outer, true, false);

        SetFocusOffset_CPL(false);
    }




    /// <summary>
    /// 训练白光干涉聚焦高度
    /// </summary>
    protected double? TrainWLI_CPL(bool isInner)
    {
        if (IsEnableWLI)
        {
            AutoRunInfo interfFocusInfo = _wliService.GetSystemInterfMeter();
            string wliFile = Application.Services.Extensions.GetWLI_CPLFile(TestFilesFolder, isInner);
            var wliPztOffset = _wliService.TrainWliFocus(interfFocusInfo, TestInfo.Physical.FocusSettings.FocusMode, wliFile);
            return wliPztOffset;
        }
        return null;
    }

    private void SetFocusOffset_CPL(bool isMeasure)
    {

        var midZHeight = (InnerFocusHeight + OuterFocusHeight) / 2;
        var macroFocusZ = _alignmentService.MacroFocusZ;
        var midPztHeight = (InnerPztOffset + OuterPztOffset) / 2;
        SetAutoFocusHeight(midZHeight, midPztHeight);

        MoveAndWait(null, midPztHeight);
        SaveFocusImage(CONST_Average,null);
        //点击量测按钮时，才保存最终的数据
        if (isMeasure)
        {
            var image = base._alignmentService.MoveAndGrab(null);
            //此处需要保存全图的清晰度
            var paramFocus = GetFineParamFocus(IServiceCamera.CurCamera);
            var sharpness = _focusService.GetImageClarity(image, paramFocus: paramFocus);
            TestInfo.TrainSetting.Sharpness = sharpness;

            TestTrainSetting.TrainParam.InnerLayer.SharpnessFocusOffset = InnerPztOffset - midPztHeight;
            TestTrainSetting.TrainParam.OuterLayer.SharpnessFocusOffset = OuterPztOffset - midPztHeight;
            var firstGlobalFocus = _focusSetting.FocusBasedOn == EnumFocusBasedOn.Inner ? _focusRes_Inner.ZFit : _focusRes_Outer.ZFit;
            TestTrainSetting.GlobalFocusOffset = firstGlobalFocus - midPztHeight; //保存第一层的全视野聚焦高度偏移量
            //TestTrainSetting.GlobalFocusOffset_CPLInner = _focusRes_Inner.ZFit - midPztHeight;
            //TestTrainSetting.GlobalFocusOffset_CPLOuter = _focusRes_Outer.ZFit - midPztHeight;
            if (_wliFocus_Inner != null)
            {
                // TestTrainSetting.WliFocusOffset_CPLInner = _wliFocus_Inner.Value - midPztHeight;
            }
            if (_wliFocus_Outer != null)
            {
                // TestTrainSetting.WliFocusOffset_CPLOuter = _wliFocus_Outer.Value - midPztHeight;
            }
            TestTrainSetting.BaseFocusHeight = CombineZPzt(midZHeight, midPztHeight) - macroFocusZ;

            LogHelper.LogInfo($"Focus Result:InnerFocusHeight={InnerFocusHeight:0.######},OuterFocusHeight={OuterFocusHeight:0.######},InnerPztOffset={InnerPztOffset:0.######},OuterPztOffset={OuterPztOffset:0.######},GlobalFocusOffset={TestTrainSetting.GlobalFocusOffset:0.######},WliFocusOffset_CPLInner= ");
        }
        _visionService.StopGrabbing();
    }


    private async Task FocusByMeasureRoi_CPL()
    {
        if (_focusRes_Inner == null || _focusRes_Outer == null) return;
        LogHelper.LogRunMethodStart();
        //使用量测Roi以及聚焦序列图更新聚焦位置及聚焦图片
        //因为此处需要采图，需要提前设置相应的光源参数
        CplParamViewModel_Inner.SetCPLParam(TestTrainSetting.TrainParam.InnerLayer.CplParam);
        InnerPztOffset = TrainRoiFocus(_focusRes_Inner, true, false);
        CplParamViewModel_Outer.SetCPLParam(TestTrainSetting.TrainParam.OuterLayer.CplParam);
        OuterPztOffset = TrainRoiFocus(_focusRes_Outer, true, false);

        SetFocusOffset_CPL(true);

        //自动创建定位模板和 kernel Model
        await CreateMeasureKernelSizeAndTemplate(true, false);
        await CreateMeasureKernelSizeAndTemplate(false, false);
        _focusRes_Inner = null;
        _focusRes_Outer = null;
        LogHelper.LogRunMethodEnd();
    }

    protected override void GetParams(TestInfoBase @base, string messageToken)
    {
        base.GetParams(@base, messageToken);
        InitCPLParamViewModel();
        if (!_isCPLMode) return;
        var lightParam = TestInfo.TrainSetting.LightSourceParam as MeasureLightSourceParam;
        if (lightParam != null)
        {
            //CPL模式下，将第一层的CPL参数设置为默认的光源参数，在量测时设置第一个量测点的光源参数时会用到
            var baseOn = _focusSetting.FocusBasedOn;
            var cplParam = baseOn == EnumFocusBasedOn.Inner ? TestTrainSetting.TrainParam.InnerLayer.CplParam : TestTrainSetting.TrainParam.OuterLayer.CplParam;
            lightParam.Filter = cplParam.Filter;
            lightParam.Wave = cplParam.Wave;
            lightParam.BandwidthIndex = cplParam.BandwidthIndex;
        }
    }

}
