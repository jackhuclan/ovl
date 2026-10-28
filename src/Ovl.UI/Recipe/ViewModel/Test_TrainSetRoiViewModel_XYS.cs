using CommunityToolkit.Mvvm.ComponentModel;
using InteVega.CBB.ComUI.Helpers;
using InteVega.CBB.DomainService.Api.Interfaces;
using Marvel.DataModels.Recipe;
using Marvel.DataModels.Recipe.AIM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Ovl.UI.Recipe.ViewModel
{
    public partial class Test_TrainSetRoiViewModel
    {
        /// <summary>
        /// X方向光源参数
        /// </summary>
        protected MeasureLightSourceParam _lightsourceParam_X;
        protected VisionParams _visionParams_Y;

        public bool _FirstInRoiView = true;

        /// <summary>
        /// 是否显示与X方法使用同样的光源参数
        /// </summary>
        [ObservableProperty]
        Visibility _sameParamsVisible = Visibility.Collapsed;

        [ObservableProperty]
        private bool _isSameNA;

        partial void OnIsSameNAChanged(bool value)
        {
            if (_FirstInRoiView) 
            {
                return;
            }
            _lightServiceForMeasure.SetNAEnable(!value);
            if (value && _lightsourceParam_X != null)
            {
                ProgressDialogHelper.ShowDialog("Setting NA ...", () =>
                {
                    _lightServiceForMeasure.SetNA((int)_lightsourceParam_X.Na);
                    return true;
                });
                _visionService.NotifyParamsChanged();
            }
        }

        [ObservableProperty]
        private bool _isSameFilter;

        partial void OnIsSameFilterChanged(bool value)
        {
            if (_FirstInRoiView)
            {
                return;
            }
            _lightServiceForMeasure.SetFilterEnable(!value);

            if (_isCPLMode && value && TestInfo is TestInfo_AIM_XYS)
            {
                TrainSettings_XYS trainSetting_XYS = TestTrainSetting as TrainSettings_XYS;
                trainSetting_XYS.TrainParamY.InnerLayer.CplParam = trainSetting_XYS.TrainParamX.InnerLayer.CplParam;
                trainSetting_XYS.TrainParamY.OuterLayer.CplParam = trainSetting_XYS.TrainParamX.OuterLayer.CplParam;
            }

            //if (value && _lightsourceParam_X != null)
            //{
            //    ProgressDialogHelper.ShowDialog("Setting Color ...", () =>
            //    {
            //        _cameraLightParamService.SetColorParams(_lightsourceParam_X.Filter, _lightsourceParam_X.BandwidthIndex, _lightsourceParam_X.Wave).Wait();
            //        return true;
            //    });
            //    _visionService.NotifyParamsChanged();
            //}
        }

        [ObservableProperty]
        private bool _isSamePolarizer;

        partial void OnIsSamePolarizerChanged(bool value)
        {
            if (_FirstInRoiView)
            {
                return;
            }
            if (_lightServiceForMeasure is ILightServiceForMeasure600 lightServiceForMeasure600)
            {
                lightServiceForMeasure600.SetPolarizerEnable(!value);
                if (value && _lightsourceParam_X != null)
                {
                    ProgressDialogHelper.ShowDialog("Setting Color ...", () =>
                    {
                        lightServiceForMeasure600.SetPolarizer(_lightsourceParam_X.PoliarizerIndex);
                        return true;
                    });
                    _visionService.NotifyParamsChanged();
                }
            }
        }


        private void InitXYSVisionParams()
        {
            if (!_isFirstDirection && TestInfo is TestInfo_AIM_XYS)
            {
                TrainSettings_XYS trainSetting_XYS = TestTrainSetting as TrainSettings_XYS;
                _lightsourceParam_X = trainSetting_XYS.LightSourceParam as MeasureLightSourceParam;
                _visionParams_Y = trainSetting_XYS.VisionParams_Y;
                SameParamsVisible = Visibility.Visible;
                IsSameNA = trainSetting_XYS.IsSameNA;
                IsSameFilter = trainSetting_XYS.IsSameFilter;
                IsSamePolarizer = trainSetting_XYS.IsSamePolarizer;
                _FirstInRoiView = false;
            }
            else
            {
                SameParamsVisible = Visibility.Collapsed;
            }
        }

        private void SaveXYSVisionParams()
        {
            if (!_isFirstDirection && TestInfo is TestInfo_AIM_XYS)
            {
                TrainSettings_XYS trainSetting_XYS = TestTrainSetting as TrainSettings_XYS;
                trainSetting_XYS.IsSameNA = IsSameNA;
                trainSetting_XYS.IsSameFilter = IsSameFilter;
                trainSetting_XYS.IsSamePolarizer = IsSamePolarizer;
            }
        }
    }
}
