using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.Prism.UI.Extensions.MessageBox;
using Marvel.Application.Services;
using Marvel.DataModels.Core;
using Marvel.DataModels.DeviceParameter;
using Marvel.DataModels.Recipe;
using InteVega.CBB.DomainService.Api.Interfaces;

using Marvel.Interfaces.Application;
using Megarobo.Ovl.Type;
using Microsoft.Extensions.DependencyInjection;
using Prism.Services.Dialogs;
using System;
using System.Threading.Tasks;
using InteVega.CBB.DomainService.Api.Models;
using HPImageViewer.Core;
using Megarobo.Ovl.Service.IntfFn.fn;
using System.Linq;

using Ovl.UI.Controls;
using DataModels = Marvel.DataModels;
namespace Ovl.UI.Recipe.ViewModel
{
    internal partial class Test_SetR3ViewModel : Test_TrainViewModelBase<TestInfoBase>, IDialogAware
    {
        public Test_SetR3ViewModel(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _messagBox = serviceProvider.GetService<IMessageBoxService>();

            Title = "Target Setting";
        }

        internal virtual void Loaded()
        {

        }

        IMessageBoxService _messagBox;

        [ObservableProperty]
        TargetLocation selectTarget;

        public WaferMapStageControl StageControl { get; set; }

        /// <summary>
        /// 打开Dialog时的方法
        /// </summary>
        /// <param name="parameters">参数合集</param>
        public override void OpenDialogThread(IDialogParameters parameters)
        {
            base.OpenDialogThread(parameters);
            _visionService.OpticalConfigChanging += _visionService_OpticalConfigChanging;
            _visionService.OpticalConfigChanged += _visionService_OpticalConfigChanged;
            _visionService.ApplyConfig(IServiceCamera.CurCamera.ToString());
            OnCameraChanged(_visionService.CurrentStageCameraConfig.CameraDeviceId);
            _visionService.StartGrabbing();
            AppHelper.DispatcherInvoke(() =>
            {
                StageControl.Init(_alignmentService, _alignmentService.WaferMapInfo);
                //UpdataStageCameraParam();
            });
            ChangeCameraInfo = "";
        }

        private void _visionService_OpticalConfigChanged(object sender, StageCameraConfig e)
        {
            ChangeCameraInfo = "";
            OnCameraChanged(e.CameraCoordId);
        }

        private void OnCameraChanged(string coordId)
        {
            if (coordId == ECamera.Meas.ToString())
            {
                //此处需要先注销事件订阅，等下面参数设置完成后再重新订阅，否则会造成死循环
                _visionService.OpticalConfigChanged -= _visionService_OpticalConfigChanged;
                //SetVisionParams();
                _visionService.OpticalConfigChanged += _visionService_OpticalConfigChanged;
            }
        }


        private void _visionService_OpticalConfigChanging(object sender, StageCameraConfig e)
        {
            ChangeCameraInfo = "Camera switching …";
        }

        /// <summary>
        /// 打开Dialog时的方法
        /// </summary>
        /// <param name="parameters">参数合集</param>
        public override void OnDialogClosed()
        {
            //NotifySaveAction?.Invoke(TestInfo);
            base.OnDialogClosed();
            _visionService.OpticalConfigChanging -= _visionService_OpticalConfigChanging;
            _visionService.OpticalConfigChanged -= _visionService_OpticalConfigChanged;

        }

        [RelayCommand]
        public async void Goto()
        {
            if (SelectTarget == null)
                return;
            await Task.Run(() =>
            {
                var point = _alignmentService.GetAxisPoint(TestInfo.TargetSetting.FieldForR3, SelectTarget, true);
                _alignmentService.MoveTo(point, $"Target:{SelectTarget},Field={TestInfo.TargetSetting.FieldForR3}");
            });
        }

        [RelayCommand]
        public async void GetR3()
        {
            if (SelectTarget == null)
                return;

            if (!_alignmentService.IsFinishAlignment)
            {
                var res = _messagBox.WarningAskDialog("An alignment operation must be performed before obtaining the target point.Otherwise, the results of the calculation may not be accurate.\r\nAre you sure it's aligned?");
                if (res != ButtonResult.OK)
                    return;
            }

            await Task.Run(() =>
            {
                //获取field0左下角对应的机台坐标
                var axisPoint = _alignmentService.GetCurrentAxisPosition();
                var fieldR3 = _alignmentService.GetFieldR3(axisPoint);
                var orgionPointR3 = _alignmentService.WaferMapInfo.ConvertToOrigionR3(SelectTarget.OrgionPoint, fieldR3.R3, DataModels.Enums.EnumR3OrigionPoint.LLC);
                if (!SelectTarget.R3Vector.Equals(new WcsPoint()))
                {
                    orgionPointR3 = GetNearestR3(orgionPointR3, SelectTarget.R3Vector);
                }
                var field = fieldR3.Field;
                var wcsPoint = _alignmentService.WaferMapInfo.GetWcsPoint(field, fieldR3.R3);
                var r = GeometryHelper.MeasureRadius(wcsPoint.X, wcsPoint.Y) + GeometryHelper.MeasureRadius(_alignmentService.WaferMapInfo.FieldWidth, _alignmentService.WaferMapInfo.FieldHeight);
                bool isEdgeField = r > _alignmentService.WaferMapInfo.Diameter / 2;
                // SelectTarget.R2 = r3 + _alignmentService.WaferMapInfo.LlcR1;
                LogHelper.LogInfo($"Getted Target:{SelectTarget},AxisPoint={axisPoint},Field={field},OrgionPoint={SelectTarget.OrgionPoint},R3={orgionPointR3}, isEdgeField={isEdgeField}");
                //此处判断当前是否在晶圆边沿位置，如果在边缘位置添加弹窗提示
                if (isEdgeField)
                {
                    var mesRes = _messageBoxService.WarningAskDialog("The machine is located at the edge of the wafer. Are you sure you need to update R3?");
                    if (mesRes != ButtonResult.OK)
                    {
                        LogHelper.LogInfo("Cancel set field and r3");
                        return;
                    }
                }
                else
                {
                    var btnResult = _messagBox.AskDialog("Are you sure to update the R3?");
                    if (btnResult != ButtonResult.OK)
                        return;
                }

                TestInfo.TargetSetting.FieldForR3 = field;
                SelectTarget.R3Vector = orgionPointR3;


            });
        }

        private WcsPoint GetNearestR3(WcsPoint r3, WcsPoint r3Vector)
        {
            double x = GetNearestValue(r3.X, r3Vector.X, _alignmentService.WaferMapInfo.FieldWidth);
            double y = GetNearestValue(r3.Y, r3Vector.Y, _alignmentService.WaferMapInfo.FieldHeight);
            return new WcsPoint(x, y);
        }

        private double GetNearestValue(double x1, double x2, double fieldWidth)
        {
            var index = (int)((x2 - x1) / fieldWidth);
            double newX1 = x1;
            double diff = fieldWidth /2;
            for (int i = index - 1; i <= index + 1; i++)
            {
                var x1_i = x1 + i * fieldWidth;
                if (Math.Abs(x1_i - x2) <= diff)
                {
                    diff = Math.Abs(x1_i - x2);
                    newX1 = x1_i;
                }
            }
            return newX1;
        }
    }
}
