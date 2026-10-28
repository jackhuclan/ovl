using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.DomainService.Api.Interfaces;
using Marvel.DataModels.Enums;
using Marvel.DataModels.Recipe40.TestInfo;
using Megarobo.Ovl.Type;
using Microsoft.Extensions.DependencyInjection;
using Prism.Services.Dialogs;
using System;
using System.Windows;

using Ovl.UI.Recipe.View;
namespace Ovl.UI.Recipe.ViewModel
{
    internal abstract partial class Test_Train40ViewModelBase
    {
        [ObservableProperty]
        Visibility _correctionVisibility;
        [ObservableProperty]
        Visibility _innerCorrectionVisibility;
        [ObservableProperty]
        Visibility _outerCorrectionVisibility;
        [ObservableProperty]
        double _innerCorrectionHeight;
        [ObservableProperty]
        double _outerCorrectionHeight;
        [ObservableProperty]
        double _innerCorrectionValue;
        [ObservableProperty]
        double _outerCorrectionValue;
        //焦面补偿控件是否可用，训练了量测roi焦面之后可用
        [ObservableProperty]
        bool _isInnerCorrectionEnabled = false;
        [ObservableProperty]
        bool _isOuterCorrectionEnabled = false;

        public bool IsInnerCorrectioned { get; private set; }

        public bool IsOuterCorrectioned { get; private set; }
        protected abstract void InitCorrections();

        /// <summary>
        /// 重置bool遍历
        /// </summary>
        public void Reset()
        {
            IsUpdateMeasureRoi = false;
            IsInnerCorrectioned = false;
            IsOuterCorrectioned = false;
        }

        protected abstract AxisType GetZAxis(bool isInner);

        protected abstract IVisionService GetVisionService(bool isInner);

        protected abstract ECamera GetECamera(bool isInner);
        protected bool SaveCorrectHeight(bool isInner)
        {
            if (isInner && !IsInnerCorrectionEnabled) return false;
            if (!isInner && !IsOuterCorrectionEnabled) return false;
            //没有聚焦过，则不采集焦面补偿图片，因为此时的焦面补偿图片的位置不一定准确
            //另外只有第一个方向的补偿焦面需要存储补偿后焦面的图片，第二个方向（Y方向）不需要


            //如果开启了手动焦面补偿，则采集补偿后的焦面作为量测原图，并以此图来创建量测的定位模板
            if (_focusSetting.IsEnableFocusCorrection)
            {
                var correctionVisibility = isInner ? InnerCorrectionVisibility : OuterCorrectionVisibility;
                if (correctionVisibility != Visibility.Visible)
                    return false;
                //如果补偿焦面未发生变化，则不需要重新采图
                if (isInner && !IsInnerCorrectioned)
                {
                    return false;
                }
                else if (!isInner && !IsOuterCorrectioned)
                {
                    return false;
                }
                var layerParam = isInner ? TrainParam.InnerLayer : TrainParam.OuterLayer;
                var imgName = isInner ? CONST_Inner : CONST_Outer;
                var correctionHeight = isInner ? InnerCorrectionHeight : OuterCorrectionHeight;
                var focusHeight = isInner ? InnerFocusHeight : OuterFocusHeight;

                //_servPlat.MoveZ(false, axisType, correctionHeight);
                var correctionValue = correctionHeight - focusHeight;
                if (isInner) InnerCorrectionValue = correctionValue;
                else OuterCorrectionValue = correctionValue;
                layerParam.FocusCorrection = correctionHeight - focusHeight;
                LogHelper.LogInfo($"Move to layer correction focus:isInner={isInner},FocusHeight={focusHeight},FocusCorrection={correctionHeight}");
                //此处的Roi 需要置为空，不能使用补偿后的焦面获取FocusKernel
                //visionService.StartGrabbing();
                //await SaveFocusImage_ByGrab(imgName, visionService, isInner, null, isMeasure: true);
                return true;
            }
            return false;
        }

        #region Correction设置界面相关

        [RelayCommand]
        private void ShowCorrectionValueSettingView(object isInner)
        {
            var inner = (bool)isInner;
            _visionService.StartGrabbing();
            var dialogService = _serviceProvider.GetService<IDialogService>();

            var parameters = new CorrectionValueParam()
            {
                IsInner = inner,
                FocusPosition = inner ? InnerFocusHeight : OuterFocusHeight,
                CorrectionValue = inner ? InnerCorrectionValue : OuterCorrectionValue,
                Unit = "mm",
                Curve = inner ? TrainParam.InnerLayer.RoiFocusCurve : TrainParam.OuterLayer.RoiFocusCurve,
                CorrectionPointSelectedAction = Vm_CorrectionPointSelected,
                SetFinalHeightAction = GrabCorrectionImageActionMethod,
                HightLightThreshHold = CreateFullTrainInfo(GetECamera(inner)).SmallStep / 2
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
            dialogService.ShowDialog("CorrectionValueSettingDialogView", dialogParameters, _ =>
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
            SaveFocusImage_ByCorrectionMove(isInner);
        }

        protected abstract void SaveFocusImage_ByCorrectionMove(bool isInner);

        private void Vm_CorrectionPointSelected(bool isInner, double focusPos)
        {
            _servPlat.MoveZ(GetZAxis(isInner), focusPos);
        }

        /// <summary>
        /// 是否设置过补偿焦面图片
        /// </summary>
        /// <param name="isInner"></param>
        /// <returns></returns>
        protected abstract bool IsCorrectionGrabbed(bool isInner);
        //{
        //    if (_focusSetting.IsEnableFocusCorrection)
        //    {
        //        if (TestInfo.IsDoubleFace)
        //        {
        //            return isInner ? IsInnerCorrectioned : IsOuterCorrectioned;
        //        }
        //        else
        //        {
        //            if(TestInfo.)
        //            return _focusBasedOn == EnumFocusBasedOn.Inner ? IsInnerCorrectioned : IsOuterCorrectioned;

        //        }
        //    }
        //    return false;
        //}

        #endregion


        /// <summary>
        /// 更新预匹配模板的最小匹配分数，因为界面上没有提供修改预匹配模板的最小匹配分数的地方，所以此处根据FocusBaseOn自动更新最小匹配分数
        /// </summary>
        protected void UpdateTemplateModelScore()
        {
            if (TestTrainSetting.PreviewTemplate == null) return;
            var measureTemplate = _focusSetting.FocusBasedOn == EnumFocusBasedOn.Inner ? TestTrainSetting.OverlayMark_InnerModels[0] : TestTrainSetting.OverlayMark_OuterModels[0];
            Action<string> logAct = (err) => { LogHelper.LogInfo(err); };
            TestTrainSetting.PreviewTemplate.UpdateMinScoreAndSearchRect(measureTemplate, logAct);
            if (TestTrainSetting is TrainSettings40_DoubleFace td)
            {
                td.PreviewTemplateOuter.UpdateMinScoreAndSearchRect(td.OverlayMark_OuterModels[0], logAct);
                td.PreviewTemplateInner.UpdateMinScoreAndSearchRect(td.OverlayMark_InnerModels[0], logAct);
            }

        }
    }
}

