using System.Text;
using CommunityToolkit.Mvvm.Input;
using InteVega.CBB.Common;
using InteVega.CBB.Common.Algorithm.Shapes;
using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.DomainLib.Services;
using InteVega.CBB.DomainService.Api.Extensions;
using InteVega.CBB.DomainService.Api.Interfaces;
using InteVega.CBB.DomainService.Api.Models;
using InteVega.CBB.MotionProof.Abstraction;
using Marvel.Application.Exceptions;
using Marvel.Application.Services;
using Marvel.DataModels.Enums;
using Marvel.Interfaces.Application;
using Megarobo.Ovl.Service.Interface;
using Megarobo.Ovl.Service.IntfFn;
using Megarobo.Ovl.Service.IntfFn.fn;
using Megarobo.Ovl.Type;
using Microsoft.Extensions.DependencyInjection;

namespace Ovl.UI.Defect.ViewModel;

public partial class DefectDetectViewModel40 : DefectDetectViewModel
{
    public DefectDetectViewModel40(IServiceProvider serviceProvicder) : base(serviceProvicder)
    {
        Focus40Visible = System.Windows.Visibility.Visible;
        _serviceCamera = serviceProvicder.GetService<IServiceCamera>();
        _focusService40 = serviceProvicder.GetService<IFocusService_SpecCam>();
        _cfg40 = serviceProvicder.GetService<IMachineDomainService>().MachineDomainConfig;
    }
    IServiceCamera _serviceCamera;
    IFocusService_SpecCam _focusService40;
    MachineDomainConfig _cfg40;
    [RelayCommand]
    public async Task Sharpness()
    {
        if (SelectedRecipe == null)
        {
            _messageBoxService.ErrorDialog("Please select the recipe first!");
            return;
        }
        await Move(GetMacFocusPos(SelectedRecipe.WaferMap.Thickness));
    }

    [RelayCommand]
    public async Task Zero()
    {
        await Move(0);
    }

    protected override void InvokeCoarseFocus()
    {
        var curCamera = IServiceCamera.CurCamera;
        if (curCamera == ECamera.Align && _cfg40.PatternCameraCfg.AlignCamFocusable == false)
        {
            _messageBoxService.ErrorDialog("The current camera does not support coarse focusing!");
            return;
        }
        var focusRunInfo = _visionService.GetSystemFocusInfo(curCamera);
        var roi = ImageViewer.ImageViewerDesc.ROIDescs.FirstOrDefault()?.CreateRoi();
        FocusZSpec(roi, focusRunInfo, false);
    }

    protected override void InvokeFineFocus()
    {
        var roi = ImageViewer.ImageViewerDesc.ROIDescs.FirstOrDefault()?.CreateRoi();
        var focusRunInfo = _visionService.GetSystemFocusInfo(IServiceCamera.CurCamera, false);

        // 对于Meas相机使用Z轴进行精细聚焦
        FocusZSpec(roi, focusRunInfo, true);
    }



    private AxisType GetAxisType()
    {
        switch (IServiceCamera.CurCamera)
        {
            case ECamera.Meas:
                return AxisType.Z; // Z 等于 Z1
            case ECamera.MeasExt:
                return AxisType.Z_DOWN; // Z_DOWN 等于 Z2
            case ECamera.Align:
                return AxisType.Z_ALIGN; // Assuming Z_Align exists
            default:
                throw new Exception("Cannot move Z for current camera in OM40.");
        }
    }

    private double GetMacFocusPos(double waferHeight)
    {
        IServiceCalData serviceCal = _serviceProvider.GetService<IServiceCalData>();
        var curCam = IServiceCamera.CurCamera;
        var curAttr = _serviceCamera.GetCameraAttr(curCam);
        var focusZ = serviceCal.GetMacroFocusZ(curCam, curAttr);

        return focusZ.Item1 + (curCam == ECamera.MeasExt ? 0 : waferHeight);
    }

    protected async Task Move(double distance)
    {
        AxisType axisType = GetAxisType();
        try
        {
            using (_motionService.TakeUIOwnership())
            {
                //await _motionControllerService.MultiAxisMotionAsync(new AxisType[] { AxisType.X, AxisType.Y, AxisType.Z }, new double[] { -270, 1, -30 }, false);
                await _motionService.SingleAxisToPointAsync(axisType, distance, false, timeout: int.MaxValue);
            }
        }
        catch (Exception ex)
        {
            var errorMsg = $"Axis {axisType} move failed:{ex.Message}";
            LogHelper.LogError(ex);
            _messageBoxService?.ErrorDialog(errorMsg, "Axis move failed");
        }
    }

    /// <summary>
    /// 只用指定Z轴聚焦
    /// </summary>
    protected void FocusZSpec(ShapeBase roi, AutoRunInfo focusInfo, bool isFine)
    {
        var axisType = _focusService40.GetAxisByCamera(IServiceCamera.CurCamera);
        focusInfo.StartPosition = _motionService.GetAxisRtPositions(axisType)[0]; // 获取当前Z轴位置
        double targetZ = double.NaN;
        if (isFine)
        {
            var ret = _focusService40.FineFocusSingleCam(IServiceCamera.CurCamera, focusInfo, shape: roi);
            targetZ = ret.ZFit; // 计算目标Z值
        }
        else
        {
            var ret = _focusService40.CoarseFocusSingleCam(IServiceCamera.CurCamera, focusInfo, shape: roi);
            targetZ = ret.ZFit;// 计算目标Z值

        }
        _serviceProvider.GetService<IServicePlatNoProof>().MoveZ(false, axisType, targetZ);
    }



}
