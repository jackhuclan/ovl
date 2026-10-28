using CommunityToolkit.Mvvm.ComponentModel;
using InteVega.CBB.Common;
using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.DomainService.Api.Interfaces;
using Marvel.DataModels.Enums;
using Marvel.DataModels.Recipe;
using Marvel.DataModels.Recipe.Overlay;
using Megarobo.Ovl.Service.IntfFn.fn;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WinCopies;

namespace Ovl.UI.ViewModels.Recipe.ViewModel
{
    public partial class Test_TrainSetRoiViewModel
    {
        bool _isCPLMode;
        //[ObservableProperty]
        //CPLParamViewModel _cplParamViewModel_Inner;

        //[ObservableProperty]
        //CPLParamViewModel _cplParamViewModel_Outer;

        FocusPegResult _focusRes_Inner;
        FocusPegResult _focusRes_Outer;
        double? _wliFocus_Inner;
        double? _wliFocus_Outer;

        private void SetCPLParam(bool isInner)
        {
            if (!_isCPLMode) { return; }

            CPLParam param = isInner ? TrainParam.InnerLayer.CplParam : TrainParam.OuterLayer.CplParam;
            if (TestTrainSetting.LightSourceParam is MeasureLightSourceParam measureLightSourceParam)
            {
                try
                {
                    _lightServiceForMeasure.SetNA((int)measureLightSourceParam.Na);
                }
                catch(Exception ex)
                {
                    LogHelper.LogError (ex);
                    _messageBoxService.Error (ex.Message);
                }
                if(_lightServiceForMeasure is ILightServiceForMeasure600  lightServiceForMeasure600)
                {
                    lightServiceForMeasure600.SetPolarizer(measureLightSourceParam.PoliarizerIndex);
                }
            }
           // _cameraLightParamService.SetLightSourceParam(IServiceCamera.CurCamera, TestInfo.TrainSetting.LightSourceParam);
            _cameraLightParamService.SetCPLExposureTimeParams(IServiceCamera.CurCamera, param);
            _visionService.NotifyParamsChanged();
        }

        private void SetCPLVisionState(bool isInner)
        {
            if (!_isCPLMode) { return; }
            if (!isInner)
            {
                //Thread.Sleep(1000);
                _lightServiceForMeasure.SetNAEnable(false);
                if (_lightServiceForMeasure is ILightServiceForMeasure600 lightServiceForMeasure600)
                {
                    lightServiceForMeasure600.SetPolarizerEnable(false);
                }
            }
        }

        private void ReSetCPLVisionState()
        {
            if (!_isCPLMode) { return; }
            _lightServiceForMeasure.SetNAEnable(true && !IsSameNA);
            if (_lightServiceForMeasure is ILightServiceForMeasure600 lightServiceForMeasure600)
            {
                lightServiceForMeasure600.SetPolarizerEnable(true && !IsSamePolarizer);
            }
        }

        private void ConfirmCPLParam(bool isInner)
        {
            if (!_isCPLMode) { return; }
            TestTrainSetting.IsTrained = false;
            LogHelper.LogInfo($"Reset IsTrained,testName={_testTrainService.TestInfo?.Name}");
            var param = _cameraLightParamService.GetCPLParam();
            if (isInner)
            {
                TrainParam.InnerLayer.CplParam = param;
            }
            else
            {
                TrainParam.OuterLayer.CplParam = param;
            }
            // cplvm.SetCPLParam(null);
        }


        private void TrainTempRoiFineFocus_CPL()
        {
            HpImageViewer.UpdateROIs(TrainParam.InnerLayer.FocusROI);
            SetCPLParam(true);

            _focusRes_Inner = _setRoiService.TrainGlobal(_isBaseOnInner);
            _wliFocus_Inner = _testTrainService.TrainWLI_CPL(true,TrainParam.DirectionFileExt);
            InnerPztOffset = TrainRoiFocus(_focusRes_Inner, true, false);


            HpImageViewer.UpdateROIs(TrainParam.OuterLayer.FocusROI);
            SetCPLParam(false);

            _focusRes_Outer = _setRoiService.TrainGlobal(!_isBaseOnInner);
            _wliFocus_Outer = _testTrainService.TrainWLI_CPL(false, TrainParam.DirectionFileExt);
            OuterPztOffset = TrainRoiFocus(_focusRes_Outer, false, false);

            SaveAverageImage();
        }




        ///// <summary>
        ///// 训练白光干涉聚焦高度
        ///// </summary>
        //protected double? TrainWLI_CPL(bool isInner)
        //{
        //    if (IsEnableWLI)
        //    {
        //        AutoRunInfo interfFocusInfo = _wliService.GetSystemInterfMeter();
        //        string wliFile = Marvel.Application.Extentions.Extensions.GetWLI_CPLFile(_testFilesFolder, isInner, TrainParam.DirectionFileExt);
        //        var wliPztOffset = _wliService.TrainWliFocus(interfFocusInfo, TestInfo.Physical.FocusSettings.FocusMode, wliFile);
        //        return wliPztOffset;
        //    }
        //    return null;
        //}

        //private void SetFocusOffset_CPL(bool isMeasure)
        //{

        //}



        /// <summary>
        /// 保存CPL模式下的焦面偏移
        /// </summary>
        /// <returns></returns>
        private bool SaveFocusOffsets_CPL()
        {
            if (_focusRes_Inner == null || _focusRes_Outer == null) return false;
            if (!IsUpdateMeasureRoi) return false;
            LogHelper.LogRunMethodStart();

            ////使用量测Roi以及聚焦序列图更新聚焦位置及聚焦图片
            ////因为此处需要采图，需要提前设置相应的光源参数
            //SetCPLParam(true);
            //_visionService.StartGrabbing();//启动采集
            //InnerPztOffset = TrainRoiFocus(_focusRes_Inner, true, true);
            //SetCPLParam(false);
            //OuterPztOffset = TrainRoiFocus(_focusRes_Outer, false, true);

            SaveAverageImage();
            //点击量测按钮时，才保存最终的数据

            //if (wliPztOffset != null)
            //{
            //    TestTrainSetting.FocusModuleInfo.WliFocusOffset = wliPztOffset.Value;
            //}
            //var zFit = GetFocusResult().ZFit;
            //TestTrainSetting.FocusModuleInfo.GlobalFocusOffset = zFit;
            //TrainParam.InnerLayer.SharpnessFocusOffset = InnerPztOffset;
            //TrainParam.OuterLayer.SharpnessFocusOffset = OuterPztOffset;

            SaveFocusModuleInfo();

            if (_wliFocus_Inner != null)
            {
                TrainParam.InnerLayer.WliFocusOffset = _wliFocus_Inner.Value;
            }
            if (_wliFocus_Outer != null)
            {
                TrainParam.OuterLayer.WliFocusOffset = _wliFocus_Outer.Value;
            }
            HpImageViewer.UpdateROIs();

            LogHelper.LogInfo($"Focus Result:InnerFocusHeight={InnerFocusHeight:0.######},OuterFocusHeight={OuterFocusHeight:0.######},InnerPztOffset={InnerPztOffset:0.######},OuterPztOffset={OuterPztOffset:0.######},GlobalFocusOffset={TestTrainSetting.FocusModuleInfo.GlobalFocusOffset:0.######},InnerLayer Param={TrainParam.InnerLayer},OuterLayer Param={TrainParam.OuterLayer},AutoFocusPostion={AutoFocusHeight:0.######}");
            //_focusRes_Inner = null;
            //_focusRes_Outer = null;
            LogHelper.LogRunMethodEnd();
            return true;
        }
    }
}
