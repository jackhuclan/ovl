using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InteVega.CBB.Common.Algorithm.Shapes;
using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.DomainService.Api.Interfaces;
using InteVega.CBB.SystemConfig.Interface;
using Marvel.Application.Services;
using Marvel.DataModels.Config;
using Marvel.DataModels.Core;
using Marvel.DataModels.Enums;
using Marvel.DataModels.Recipe;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Ovl.UI.Recipe.ViewModel.OMVersion
{
    public partial class AlignmentVmBase
    {
        

        [ObservableProperty]
        Visibility stopFineAlignVis = Visibility.Collapsed;
        [ObservableProperty]
        bool runFineAlignmentBtnEnable = true;

        private void EnableDisableBtn(bool finealignment)
        {
            RunFineAlignmentBtnEnable = finealignment;
        }

        /// <summary>
        /// 拉平异常处理后继续执行
        /// </summary>
        [RelayCommand]
        private void ContinueAlign()
        {
            ContinueExecute();
        }
        [ObservableProperty]
        public double angle_FineAlign;

        [ObservableProperty]
        public double angle_CoarseAlign;

        [ObservableProperty]
        private bool _isShowKeyPoint;

        [ObservableProperty]
        private bool _isShowAlignMarkPoint;

        private double _angleThreshold_CoarseAlign = 0.01;

        /// <summary>
        /// 粗对准和拉平角度阈值，供界面显示
        /// </summary>
        public double AngleThreshold_CoarseAlign
        {
            get { return _angleThreshold_CoarseAlign; }
            set 
            { 
                SetProperty(ref _angleThreshold_CoarseAlign, value);
                if (AlignmentInfo.CoarseAngleThreshold != value)
                    AlignmentInfo.CoarseAngleThreshold = value;
            }
        }

        /// <summary>
        /// 执行拉平（AI 工具 run_coarse_align / run_fine_align 的执行体）。
        /// 返回真实的执行结果（成功 / 取消 / 失败原因），供 AI 作为事实来源，避免固定返回成功。
        /// </summary>
        protected async Task<string> ExecuteAlignment(bool isFine)
        {
            IsEnableClick = false;
            _alignmentService.Reset();
            try
            {
                AssistantBase.ActionErrored = AlignmentHandleError;
                LogHelper.LogInfo($"Start Execute Alignment");
                EnableDisableBtn(false);
                HpImageViewer.UpdateROIs(null);
                await Task.Run(() =>
                {
                   //Thread.Sleep(10000);
                    _alignmentService.TrainAlignment(isFine);
                    SaveResult(true);
                });
                if (_alignmentService.IsCancelled)
                    return "拉平执行被取消";
                IsShowAlignMarkPoint = false;
                IsShowKeyPoint = false;
                Angle_FineAlign = AlignmentInfo.FineAngle;
                Angle_CoarseAlign = AlignmentInfo.CoarseAngle;
                //拉平完成后，更新晶圆图
                //new WaferMapControl().Redraw(WaferInfo);
                return isFine ? "成功执行精细拉平" : "成功执行粗定位";
            }
            catch (Exception ex)
            {
                if (ex is ThreadInterruptedException || _alignmentService.IsCancelled)
                {
                    _messageBoxService.ErrorDialog("The thread executing the leveling operation was interrupted!", "Error");
                    LogHelper.LogError($" 执行拉平线程被中断");
                    return "拉平执行被中断";
                }
                else
                {
                    _messageBoxService.ErrorDialog(ex.Message, "Error");
                    return $"执行拉平失败：{ex.Message}";
                }
            }
            finally
            {
                ResetStatus();
            }
        }

        private void ResetStatus()
        {
            ResetFineAlignEnableState();
            IsEnableClick = true;
            IsErrored = false;
            AssistantBase.ActionErrored = null;
            _StageController.StartGrabbing();
        }

        [RelayCommand]
        private void ExecuteAlignmentScanRangeCancel()
        {
            _alignmentService.Stop();
            EnableDisableBtn(false);
            StopFineAlignVis = Visibility.Visible;
        }

        /// <summary>
        /// 重置精对齐按钮状态
        /// </summary>
        private void ResetFineAlignEnableState(bool enableCancel = false)
        {
            StopFineAlignVis = Visibility.Collapsed;
            EnableDisableBtn(true);
        }


        /// <summary>
        /// Align && TrainField（AI 工具 run_fine_align 的执行体）。
        /// 返回真实的执行结果，field 未勾选、被取消、失败时如实返回，供 AI 作为事实来源。
        /// </summary>
        [RelayCommand]
        public async Task<string> AlignAndTrain()
        {
            _alignmentService.Reset();
            var fields = WaferMapControlinstance?.GetSelectedFields();
            if (fields != null && fields.Count >= 2 && fields.Count <= 4)
            {
                IsEnableClick = false;
                EnableDisableBtn(false);
                AssistantBase.ActionErrored = AlignmentHandleError;
                await Task.Run(() =>
                {
                    var coarseFocus = GetFocusInfo();
                    _alignmentService.CoarseFocus(coarseFocus, false);
                    var workFlowConfig = _serviceProvider.GetService<IConfigLoader>()?.LoadConfig<WorkFlowConfig>();
                    if (workFlowConfig?.IsRefreshWaferThicknessAfterAlignAndTrain == true)
                        WaferInfo.Thickness = _alignmentService.GetCurrentZAlign() - _alignmentService.MacroFocusZ;
                });
                HpImageViewer.UpdateROIs();
                _alignmentService.RecipeElementsInfo.Alignment.CustomizeFieldList = fields;
                if (_alignmentService.IsCancelled)
                {
                    ResetStatus();
                    return "精细拉平被取消";
                }
                return await ExecuteAlignment(true);
            }
            else
            {
                _messageBoxService.ErrorDialog("Please select 2-4 fields", "RecipeMessage");
                return "没有在 WaferMap 上勾选 2~4 个 field，请先在 WaferMap 界面勾选 2~4 个 field 后再执行精细拉平";
            }

        }

        /// <summary>
        /// Align && TrainField
        /// </summary>
        [RelayCommand]
        public async Task UpdateLLC()
        {
            var res = _messageBoxService.AskDialog("Updating LLC will redraw the wafermap, are you sure you need to update it?");
            if (res != Prism.Services.Dialogs.ButtonResult.OK)
                return;
            bool isFinishAlign = _alignmentService.IsFinishAlignment;
            LogHelper.LogInfo($"Start updateLLC, IsFinishAlignment={isFinishAlign}");
            if (!isFinishAlign)
            {
                res = _messageBoxService.WarningAskDialog("An alignment operation must be performed before LLC can be updated. Otherwise, the results of the calculation may not be accurate.\r\nAre you sure it's aligned?");
                if (res != Prism.Services.Dialogs.ButtonResult.OK)
                {
                    return;
                }
            }
            try
            {
                var axisPoint = _alignmentService.GetCurrentAxisPosition();
                // var llcR1 = _alignmentService.CalculateCowOffset(axisPoint);
                var llcR1 = _alignmentService.GetWcsPoint(axisPoint);//计算LLC的实际WCS坐标，不限制XY的取值范围
                RedrawWaferMap(llcR1);
                ToLLC();
            }
            catch (Exception ex)
            {
                LogHelper.LogError(ex);
                _messageBoxService.Error(ex.Message);
            }
        }

        [RelayCommand]
        public void ToLLC()
        {
            TargetLocation targetLLC = new TargetLocation() { OrgionPoint = WaferInfo.IsFieldCenter ? EnumR3OrigionPoint.COF : EnumR3OrigionPoint.LLC };
            if (WaferInfo.IsFieldCenter)
            {
                targetLLC.R3Vector = WaferInfo.ConvertToOrigionR3(EnumR3OrigionPoint.COF, new WcsPoint(), EnumR3OrigionPoint.LLC);
            }
            var axisPoint = _alignmentService.GetAxisPoint(RowColumnInfo.Zero, targetLLC, true);
            _alignmentService.MoveTo(axisPoint, "Field0 LLC");
        }

        #region 定位调试代码

        /// <summary>
        /// 走到晶圆中心位置
        /// </summary>
        [RelayCommand]
        public void GotoCow()
        {
            HpImageViewer.UpdateROIs();
            _alignmentService.MoveTo(_alignmentService.Cow, "Center of wafer");
        }

        /// <summary>
        /// 走到Field0位置
        /// </summary>
        [RelayCommand]
        public void GotoField0()
        {
            HpImageViewer.UpdateROIs();
            Task.Run(() =>
            {
                var point = _alignmentService.GetAxisPoint(RowColumnInfo.Zero, new WcsPoint(), true);
                _alignmentService.MoveTo(point, "Field0 LLC");
            });

        }
        #endregion 
    }
}
