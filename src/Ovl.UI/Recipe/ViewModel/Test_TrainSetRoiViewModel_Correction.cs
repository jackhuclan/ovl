using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InteVega.CBB.Common.Algorithm;
using InteVega.CBB.CommonLib.Helpers;
using Marvel.DataModels.Enums;
using Microsoft.Extensions.DependencyInjection;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using Ovl.UI.Recipe.View;
namespace Ovl.UI.Recipe.ViewModel
{
    public partial class Test_TrainSetRoiViewModel
    {
        [ObservableProperty]
        Visibility _correctionVisibility;
        [ObservableProperty]
        Visibility _innerCorrectionVisibility;
        [ObservableProperty]
        Visibility _outerCorrectionVisibility;
        [ObservableProperty]
        double _innerCorrectionValue;
        [ObservableProperty]
        double _outerCorrectionValue;
        [ObservableProperty]
        double _innerCorrectionHeight;
        [ObservableProperty]
        double _outerCorrectionHeight;

        //焦面补偿控件是否可用，训练了量测roi焦面之后可用
        [ObservableProperty]
        bool _IsInnerCorrectionEnabled;
        [ObservableProperty]
        bool _IsOuterCorrectionEnabled;

        public bool IsInnerCorrectioned { get; private set; } /*=> !(DoubleHelper.EqualsExt(InnerCorrectionHeight, 0) && DoubleHelper.EqualsExt(InnerCorrectionValue, 0));*/

        public bool IsOuterCorrectioned { get; private set; } /* => !(DoubleHelper.EqualsExt(OuterCorrectionHeight, 0) && DoubleHelper.EqualsExt(OuterCorrectionValue, 0));*/

        private void InitPztOffsetCorrections()
        {
            CorrectionVisibility = _grabMode != EnumGrabMode.SingleGrab_Average && _focusSetting.IsEnableFocusCorrection ? Visibility.Visible : Visibility.Collapsed;
            InnerCorrectionVisibility = !_isSingleGrab || _isBaseOnInner ? Visibility.Visible : Visibility.Collapsed;
            OuterCorrectionVisibility = !_isSingleGrab || !_isBaseOnInner ?
                Visibility.Visible : Visibility.Collapsed;
            InnerPztOffset = TrainParam.InnerLayer.SharpnessFocusOffset;
            OuterPztOffset = TrainParam.OuterLayer.SharpnessFocusOffset;
            // 调试用
            //IsInnerCorrectionEnabled = true;
            //IsOuterCorrectionEnabled = true;
            IsInnerCorrectionEnabled = TrainParam.InnerLayer.MeasureROI != null && InnerCorrectionVisibility == Visibility.Visible;
            IsOuterCorrectionEnabled = TrainParam.OuterLayer.MeasureROI != null && OuterCorrectionVisibility == Visibility.Visible;
            InnerCorrectionValue = TrainParam.InnerLayer.FocusCorrection;
            OuterCorrectionValue = TrainParam.OuterLayer.FocusCorrection;
            InnerCorrectionHeight = InnerPztOffset + InnerCorrectionValue;
            OuterCorrectionHeight = OuterPztOffset + OuterCorrectionValue;
        }

        /// <summary>
        /// 重置bool遍历
        /// </summary>
        public void Reset()
        {
            IsUpdateMeasureRoi = false;
            IsInnerCorrectioned = false;
            IsOuterCorrectioned = false;
        }

        private async void GrabCorrectionImage(double value, bool isInner)
        {
            _pztservice?.AMov(value);

            SetCPLParam(isInner);

            bool isGrabing = _visionService.Camera.IsGrabbing;
            if (!isGrabing)
            {
                _visionService.StartGrabbing();
            }
            var imagedata = await _visionService.GrabOneImageAsync();
            HpImageViewer.Image = imagedata;
            if (!isGrabing)
            {
                _visionService.StopGrabbing();
            }
        }

        private bool SaveFinalCorrectionValue(bool isInner)
        {
            // if (!_isFirstDirection) return false;
            if (isInner && !IsInnerCorrectionEnabled) return false;
            if (!isInner && !IsOuterCorrectionEnabled) return false;

            //没有聚焦过，则不用计算最终CorrectionValue
            //另外只有第一个方向的补偿焦面需要计算最终CorrectionValue，第二个方向（Y方向）不需要

            //如果开启了手动焦面补偿，则以选择补偿焦面高度和最新量测roi聚焦高度计算最终CorrectionValue
            if (_focusSetting.IsEnableFocusCorrection)
            {
                var correctionVisibility = isInner ? InnerCorrectionVisibility : OuterCorrectionVisibility;
                if (correctionVisibility != Visibility.Visible)
                    return false;
                // 如果补偿焦面未发生变化，则不需要重新计算
                // 只需要关心是否设置了补偿焦面高度，不用关心是否动了量测roi，补偿焦面是一个绝对高度，就算量测roi改变了补偿焦面也不会改变
                if (isInner && !IsInnerCorrectioned)
                {
                    return false;
                }
                else if (!isInner && !IsOuterCorrectioned)
                {
                    return false;
                }
                SetCPLParam(isInner);
                var layerParam = isInner ? TrainParam.InnerLayer : TrainParam.OuterLayer;
                //var imgName = isInner ? CONST_Inner : CONST_Outer;
                //var correctionValue = isInner ? InnerCorrectionValue : OuterCorrectionValue;
                var correctionFinalHeight = isInner ? InnerCorrectionHeight : OuterCorrectionHeight;
                double pztOffset = isInner ? InnerPztOffset : OuterPztOffset;
                var correctionValue = correctionFinalHeight - pztOffset;
                if (isInner) InnerCorrectionValue = correctionValue;
                else OuterCorrectionValue = correctionValue;
                LogHelper.LogInfo($"Move to layer correction focus:isInner={isInner},pztOffset={pztOffset},FocusCorrection={correctionValue}");
                layerParam.FocusCorrection = correctionValue;
                // 不用重新采图，补偿焦面的图像已经在CorrectionValueSettingDialog里面设置过
                //MoveAndWait(null, pztOffset + correctionValue);
                //_visionService.StartGrabbing();
                //SaveFocusImage(imgName, null);
                return true;
            }
            return false;
        }

        #region Correction设置界面相关

        [RelayCommand]
        private void ShowCorrectionValueSettingView(object isInner)
        {
            _visionService.StartGrabbing();

            var inner = (bool)isInner;
            var dialogService = _serviceProvider.GetService<IDialogService>();

            var parameters = new CorrectionValueParam()
            {
                IsInner = inner,
                FocusPosition = inner ? InnerPztOffset : OuterPztOffset,
                CorrectionValue = inner ? InnerCorrectionValue : OuterCorrectionValue,
                Unit = "μm",
                Curve = inner ? TrainParam.InnerLayer.RoiFocusCurve : TrainParam.OuterLayer.RoiFocusCurve,
                CorrectionPointSelectedAction = Vm_CorrectionPointSelected,
                SetFinalHeightAction = GrabCorrectionImageActionMethod,
                HightLightThreshHold = _testTrainService.CreateFullRangeFocusInfo().Step / 2
            };
            var dialogParameters = new DialogParameters
            {
                { nameof(CorrectionValueParam), parameters }
            };

            bool isGrabing = _visionService.Camera.IsGrabbing;
            if (!isGrabing)
            {
                _visionService.StartGrabbing();
            }
            dialogService.ShowDialog(nameof(CorrectionValueSettingDialogView), dialogParameters, _ =>
            {
                parameters.CorrectionPointSelectedAction -= Vm_CorrectionPointSelected;
            });
            // 打开窗体前是停止采图的状态就停止采图
            if (!isGrabing)
            {
                _visionService.StopGrabbing();
            }
        }

        private void GrabCorrectionImageActionMethod(bool isInner, double finalHeight, double correctionValue)
        {
            // 记录最终在Correction聚焦偏移界面设置的聚焦高度
            if (isInner)
            {
                InnerCorrectionHeight = finalHeight;
                InnerCorrectionValue = correctionValue;// 此InnerCorrectionValue会在run时重新设置
                IsInnerCorrectioned = true;
            }
            else
            {
                OuterCorrectionHeight = finalHeight;
                OuterCorrectionValue = correctionValue;// 此OuterCorrectionValue会在run时重新设置
                IsOuterCorrectioned = true;
            }

            _visionService.StartGrabbing();

            _test_TrainViewModelBase.ChangeCameraInfo = "Correcting Focus ...";
            Task.Run(() =>
            {
                try
                {
                    if (_isCPLMode)
                        SetCPLParam(isInner);
                    if (_isSingleGrab)
                    {
                        //此处的Roi 需要置为空，不能使用补偿后的焦面获取FocusKernel
                        _setRoiService.SaveLayerImage(true, null);
                        _setRoiService.SaveLayerImage(false, null);
                    }
                    else
                    {
                        //var imgName = isInner ? CONST_Inner : CONST_Outer;
                        //此处的Roi 需要置为空，不能使用补偿后的焦面获取FocusKernel
                        _setRoiService.SaveLayerImage(isInner, null);
                    }
                }
                catch (Exception ex)
                {
                    _messageBoxService?.ErrorDialog(ex.Message);
                }
                finally
                {
                    _test_TrainViewModelBase.ChangeCameraInfo = "";
                }
            });


        }


        private void Vm_CorrectionPointSelected(bool isInner, double focusPos)
        {
            _pztservice?.AMov(focusPos);
        }
        #endregion
    }
}
