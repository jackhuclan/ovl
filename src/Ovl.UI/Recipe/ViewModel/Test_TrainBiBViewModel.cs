using System.ComponentModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HalconDotNet;
using HPImageViewer.Core;
using HPImageViewer.Core.Persistence;
using InteVega.CBB.Common.Algorithm;
using InteVega.CBB.Common.Algorithm.Shapes;
using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.CommonLib.Helpers.Reflection;
using InteVega.CBB.DomainService.Api.Extensions;
using InteVega.CBB.DomainService.Api.Interfaces;
using InteVega.CBB.Recipe.Interface;
using InteVega.CBB.TemplateMatching.Core;
using Marvel.DataModels;
using Marvel.DataModels.Enums;
using Marvel.DataModels.Recipe;
using Marvel.DataModels.Recipe.Overlay;
using Marvel.Interfaces.Application;
using Megarobo.Ovl.Type;
using Microsoft.Extensions.DependencyInjection;
using OVL.Algorithm.DoNet;
using Prism.Services.Dialogs;
using Wafer.Algorithm.Common.DoNet;
using Point = HPImageViewer.Core.Primitives.Point;

using Ovl.UI.Recipe.View;

namespace Ovl.UI.Recipe.ViewModel;

public partial class Test_TrainBiBViewModel : Test_TrainOverlayViewModel<TestInfo_BiB>
{
    public Test_TrainBiBViewModel(IServiceProvider serviceProvider) : base(serviceProvider)
    {

    }

    [ObservableProperty]
    Test_TrainSetRoiViewModel _setRoiVM;

    [ObservableProperty]
    Test_TrainFocusPredictionViewModel _focusPrediction;
    [ObservableProperty]
    bool _isFocusPredictionTabEnabled;

    [ObservableProperty]
    bool _isDamageSettingsVisible;

    [ObservableProperty]
    TestOverlayTrainParam _trainParam;

    protected TrainSettings_BiB BibTrainSetting;
    public override void OpenDialogThread(IDialogParameters parameters)
    {
        base.OpenDialogThread(parameters);
        BibTrainSetting = TrainSetting_Overlay as TrainSettings_BiB;
        UnsubscribePolarityChanged();
        TrainParam = BibTrainSetting.TrainParam;
        IsDamageSettingsVisible = (TestInfo.DataCulling as DataCulling_Overlay)?.DamageCulling != null;
        SetRoiVM = new Test_TrainSetRoiViewModel(this, _serviceProvider, TestInfo, TrainParam, true);
        SetRoiVM.Checked();
        SetRoiVM.ActionSetFocusRoi = SetVisionParamsEnable;
        SetRoiVM.ActionConfirmFocusRoi = SetVisionParamsDisable;
        TrainSettingVisible = System.Windows.Visibility.Visible;
        // 初始化聚焦预测 ViewModel
        FocusPrediction = new Test_TrainFocusPredictionViewModel(this, _serviceProvider,
            TestInfo, TrainParam, () => HpImageViewer);
        SubscribePolarityChanged();


    }

    //??? Inner 、Outer 相关的参数都改成了使用TrainParam.InnerLayer 和 TrainParam.OuterLayer来获取，包括FocusRoi\ FocusKernel\ MeasureRoi\RoiCurve 等

    protected override ROIDesc GetMeasureRoiDesc(bool isInner)
    {
        return SetRoiVM.TrainParam.GetRoiDesc(isInner, true);
    }

    protected override ImageData LoadTemplateModeImage(bool isInner)
    {
        try
        {
            return SetRoiVM.LoadFocusImageData(isInner);
        }
        catch
        { return null; }
    }

    private ImageData LoadLayerImage(bool isInner)
    {
        return SetRoiVM.LoadFocusImageData(isInner);
    }



    /// <summary>用户切换到「Focus Prediction」Tab 时，初始化预测子 ViewModel 的数据与订阅。</summary>
    [RelayCommand]
    private void FocusPredictionChecked()
    {
        FocusPrediction?.Initialize(TrainSetting_Overlay,TrainParam );
    }

    protected override void AutoCreateKernelModel(bool isInner)
    {
        switch (TrainSetting_Overlay.InnerType)
        {
            case EnumTargetType.Bar:
            case EnumTargetType.Box:
            case EnumTargetType.Frame:
            case EnumTargetType.Circle:
                GenKernelRoiOfBIB(isInner);
                break;
            case EnumTargetType.Cross:
                GenKernelRoiOfCross(isInner);
                break;
            case EnumTargetType.AIM:
            case EnumTargetType.AIM_XYS:

                GenKernelRoiOfAIM(isInner);
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// 创建bib外框 kernel Size调整框
    /// </summary>
    private void GenKernelRoiOfBIB(bool isInner)
    {
        CreateKernelModel(isInner, (rectRoi) =>
        {
            return GenKernelRoiOfBIB(rectRoi, out _);
        });

    }
    /// <summary>
    ///  创建Cross外框 kernel Size调整框
    /// </summary>
    private void GenKernelRoiOfCross(bool isInner)
    {
        CreateKernelModel(isInner, (roi) =>
        {
            return GenKernelRoiOfCross(roi, out _);
        });

    }
    /// <summary>
    ///  创建AIM外框 kernel Size调整框
    /// </summary>
    private void GenKernelRoiOfAIM(bool isInner)
    {


        CreateKernelModel(isInner, (roi) =>
        {
            var result = GenKernelRoiOfAIM(roi, out _);
            return result;
        });

    }



    private ExResult CreateKernelModel(bool isInner, Func<RectangleDesc, OMROIBase> funcGenKernelRoi)
    {
        var rectRoi = GetMeasureRoiDesc(isInner) as RectangleDesc;// isInner ? TestInfo.TrainSetting_BiB.InnerRectangle : TestInfo.TrainSetting_BiB.OuterRectangle;
        OMROIBase oMROIBase = funcGenKernelRoi(rectRoi);
        var imageData = HpImageViewer.Image as ImageData;
        using var imageInner = imageData.ToHObject();
        var targetType = isInner ? TrainSetting_Overlay.InnerType : TrainSetting_Overlay.OuterType;
        OMTargetType oMTargetType = ConvetTargetType(targetType);
        var result = OMUtil.GenKernelModel(imageInner, oMROIBase, oMTargetType, out OMKernelModel oMKernelModel);
        SaveKernelModel(isInner, oMROIBase, oMKernelModel);
        SaveGoldenModel(isInner, imageData, oMROIBase, oMTargetType, TrainParam);
        return result;
    }



    private BIBROI GenKernelRoiOfBIB(RectangleDesc roi, out ExResult exResult)
    {
        var bib2d = roi.CreateRoi() as Bib2d;
        exResult = OMUtil.GenKernelRoiOfBIB(bib2d, out BIBROI kernelRoi);
        return kernelRoi;
    }



    private CrossROI GenKernelRoiOfCross(RectangleDesc roi, out ExResult exResult)
    {
        var rectRoi = roi as Cross2dRectangleDesc;
        Cross2d cross2D = new Cross2d();
        cross2D.Width = rectRoi.ColumnWidth;
        cross2D.Rect = new Rect2d();
        cross2D.Rect.X = roi.Left;
        cross2D.Rect.Y = roi.Top;
        cross2D.Rect.Width = roi.Width;
        cross2D.Rect.Height = roi.Height;
        foreach (var item in rectRoi.RactPointList)
        {
            cross2D.RectAxis.Axis.Add(new Point2d(item.Item1, item.Item2));
        }
        var length = Convert.ToInt32(rectRoi.BandLength);//与算法确认过，直接取整即可
        exResult = OMUtil.GenKernelRoiOfCross(cross2D, length, out CrossROI kernelRoi);
        return kernelRoi;
    }


    private AIMROI GenKernelRoiOfAIM(RectangleDesc roi, out ExResult exResult)
    {
        var rectRoi = roi as AimRectangleDesc;
        AimOrientation aimOrientation = TestInfo.TargetSetting.OrientationType == EnumOrientationType.Clockwise ? AimOrientation.ClockWise : AimOrientation.CounterClockWise;
        var trainRoi = rectRoi.CreateRoi() as Aim2d;
        exResult = OMUtil.GenKernelRoiOfAIM(trainRoi, aimOrientation, out AIMROI kernelRoi);
        return kernelRoi;
    }

    [RelayCommand]
    async void Run()
    {
        try
        {
            LogHelper.LogRunMethodStart();
            ChangeCameraInfo = "Running...";
            //此处只是用来调试时验证数据使用的，需要加断点动态修改isTest的值
            bool isTest = true;//此处为了便于在调试中拖动代码
            isTest = false;
            _visionService.StartGrabbing();//启动采集
            await WaitLoadTemplates();
            if (isTest)
            {
                var imagedata = HpImageViewer.Image as ImageData;

                var mr = _alignmentService.FindTemplateForRun(imagedata, TrainSetting_Overlay.PreviewTemplate, true);
                // SetRoiVM.TestSharpness();
                TestMeasurement();
                return;
            }
            //如果点击Run 时，进行过聚焦操作，则将机台移动到聚焦所在的位置
            if (FocusPosition != null)
            {
                _alignmentService.MoveTo(FocusPosition);
                await Task.Run(async () =>
                {
                    var isFoucs = await SetRoiVM.SaveMeasureFocus();
                    //if (isFoucs)
                    //{
                    //    var roiCenter = TrainParam.InnerLayer.MeasureROI.CreateRoi().GetCenter();
                    //    var imagedata = SetRoiVM.LoadFocusImageData(true);
                    //    //创建量测模板后，记录模板中心所在的机台坐标，当定位模板与量测模板不一致时，用该值来计算量测模板与定位模板的偏移量
                    //    TargetPoint = _alignmentService.ConvertImageToAxis(_alignmentService.GetCurrentAxisPosition(), new MatchingItem() { Column = roiCenter.X, Row = roiCenter.Y }, imagedata.Width, imagedata.Height);
                    //}

                });

                if (SetRoiVM.IsUpdateMeasureRoi || SetRoiVM.IsInnerCorrectioned)
                {
                    if (!InnerTemplateBeforeCreateModel())
                        return;
                    //自动创建定位模板和 kernel Model
                    await CreateMeasureKernelSizeAndTemplate(true, false);
                }
                if (SetRoiVM.IsUpdateMeasureRoi || SetRoiVM.IsOuterCorrectioned)
                {
                    if (!OuterTemplateBeforeCreateModel())
                        return;
                    await CreateMeasureKernelSizeAndTemplate(false, false);
                }
                if (SetRoiVM.IsUpdateMeasureRoi)
                {
                    await AutoCorrectR3();
                }
                SetRoiVM.Reset();
                SaveOMParamDict();
                GetTrainSettings().IsTrained = true;
                LogHelper.LogInfo($"Train test bib success, testName={TestInfo?.Name} ");
            }
            _visionService.StopGrabbing();
            using var imgObjectInner = LoadLayerImage(true)?.ToHObject();
            if (imgObjectInner == null) return;
            using var imgObjectOuterOwned = !_isSingleGrab ? LoadLayerImage(false)?.ToHObject() : null;
            var imgObjectOuter = imgObjectOuterOwned ?? imgObjectInner;
            if (imgObjectOuter == null) return;

            if (imgObjectInner == null || imgObjectOuter == null) return;
            if (_omParam.KernelModelInner == null || _omParam.KernelModelOuter == null || BibTrainSetting?.TrainParam?.InnerLayer?.MeasureROI == null || BibTrainSetting?.TrainParam?.OuterLayer?.MeasureROI == null)
            {
                _messageBoxService.ErrorDialog("Please set measurement roi first!");// ("Physical 数据不完整！");
                return;
            }

            //清空Roi
            HpImageViewer.UpdateROIs();
            // 加延时是因为UpdateRois里面会使用异步刷新控件时很大几率会把上一轮图拿过来，最后一步ShowResult方法的HpImageViewer赋值的图片会被UpdateRois的图片覆盖
            await Task.Delay(100);

            var _measService = _serviceProvider.GetService<IMeasurementService>();


            var x = TrainParam.InnerLayer.MeasureROI.CenterX;
            var y = TrainParam.InnerLayer.MeasureROI.CenterY;
            var transMat = _measService.GetTransMat(y, x, new MatchingItem() { Column = x, Row = y }, 0);

            var (offsetX, offsetY) = Physical?.GetOvlOffset() ?? (0, 0);
            var ovResult = _measService.MeasureOverlay(imgObjectInner, imgObjectOuter, _omParam, transMat, transMat, offsetX, offsetY);
            if (ovResult.MisregX == 0 && ovResult.MisregY == 0)
            {
                //量测结果异常，请重新聚焦、画量测roi再重试
                throw new Exception("The measurement result is abnormal. Please refocus, draw a measurement ROI, and try again!");
            }
            ShowResult(ovResult, imgObjectInner, imgObjectOuter);
            Submit(true);
        }
        catch (Exception ex)
        {
            LogHelper.LogError(ex);
            _messageBoxService.ErrorDialog(ex.Message);
        }
        finally
        {
            //run状态不更新状态
            //Submit(false);
            LogHelper.LogRunMethodEnd();
            ChangeCameraInfo = "";
        }

    }



    private void TestMeasurement()
    {
        var _measService = _serviceProvider.GetService<IMeasurementService>();
        OvResultBase testovResult;
        if (!_isSingleGrab)
        {
            string innerImage = @"E:\工作\开发\调试数据\OM300\OM300-1\图片\结果异常\Inner.jpg";
            string outerImage = @"E:\工作\开发\调试数据\OM300\OM300-1\图片\结果异常\Outer.jpg";
            using var imgObjectInner = ImageData.ReadImage(innerImage).ToHObject();
            using var imgObjectOuter = ImageData.ReadImage(outerImage).ToHObject();
            testovResult = TestOvl(_measService, imgObjectInner, imgObjectOuter);
        }
        else
        {
            using var imgObjectInner = (HpImageViewer.Image as ImageData).ToHObject();
            testovResult = TestOvl(_measService, imgObjectInner, imgObjectInner);
        }
        LoadOvResult(testovResult);
        return;
    }


    private OvResultBase TestOvl(IMeasurementService _measService, HObject _imgInner, HObject _imgOuter)
    {
        bool isReverse = false;
        double angle = isReverse ? 180 : 0;
        var innerModel = BibTrainSetting.OverlayMark_InnerModels[0];
        var outerModel = BibTrainSetting.OverlayMark_OuterModels[0];
        var innerRefMatchItem = BibTrainSetting.InnerTrainMatchItem.ToPoint();
        var innerCenterX = innerRefMatchItem.X;
        var innerCenterY = innerRefMatchItem.Y;
        var outerRefMatchItem = BibTrainSetting.OuterTrainMatchItem .ToPoint();
        var outerCenterX = outerRefMatchItem.X;
        var outerCenterY = outerRefMatchItem.Y;
        var matchItemInner = _alignmentService.FindTemplateForRun(_imgInner.ToImageData(), innerModel, _alignmentService.IsReversed)?.BestMatchItem;
        var matchItemOuter = _alignmentService.FindTemplateForRun(_imgOuter.ToImageData(), outerModel, _alignmentService.IsReversed)?.BestMatchItem;
        var transMatInner = _measService.GetTransMat(innerCenterY, innerCenterX, matchItemInner, angle);
        var transMatOuter = _measService.GetTransMat(outerCenterY, outerCenterX, matchItemOuter, angle);
        var (ovlOffsetX, ovlOffsetY) = Physical?.GetOvlOffset() ?? (0, 0);
        var ovl = _measService.MeasureOverlay(_imgInner, _imgOuter, _omParam, transMatInner, transMatOuter, ovlOffsetX, ovlOffsetY);
        List<ROIDesc> rOIDescs = new List<ROIDesc>();
        var innerDesc = innerModel.ROI.CreateRect();
        innerDesc.X = matchItemInner.Column - innerDesc.Width / 2;
        innerDesc.Y = matchItemInner.Row - innerDesc.Height / 2;
        rOIDescs.Add(innerDesc.CreateRoiDesc());
        var outerDesc = outerModel.ROI.CreateRect();
        outerDesc.X = matchItemOuter.Column - outerDesc.Width / 2;
        outerDesc.Y = matchItemOuter.Row - outerDesc.Height / 2;
        rOIDescs.Add(outerDesc.CreateRoiDesc());
        HpImageViewer.UpdateROIs(rOIDescs.ToArray());
        return ovl;
    }

    protected void ShowResult(OvResultBase ovResult, HObject imgObjectInner, HObject imgObjectOuter)
    {

        _omParam.ConvertToDict(out HTuple hTuple);
        OvResultBase centeringOVL = ovResult;
        //底层算法是以外层为中心进行量测的，如果界面上是以内层为中心，此时的ovl值需要取反
        if (TestInfo.TargetSetting.TargetCentering == EnumCalOverlayInOrOutEdge.Inner)
        {
            centeringOVL = (OvResultBase)CloneHelper.ReflectionClone(ovResult);
        }
        CorrelationCurvesList = GetCorrelationCurve(imgObjectInner, imgObjectOuter, hTuple);
        List<ROIDesc> rOIDescs = new List<ROIDesc>();
        if (_isSingleGrab || SelectedLayer == EnumFocusBasedOn.Inner)
        {
            rOIDescs.AddRange(GetProfileForImageView(imgObjectInner, _omParam.OmROIInner, _omParam.TargetType));
            rOIDescs.Add(TrainParam.InnerLayer.MeasureROI);
        }
        if (_isSingleGrab || SelectedLayer == EnumFocusBasedOn.Outer)
        {
            rOIDescs.AddRange(GetProfileForImageView(imgObjectOuter, _omParam.OmROIOuter, _omParam.TargetType));
            rOIDescs.Add(TrainParam.OuterLayer.MeasureROI);
        }

        HpImageViewer.FitImageToArea();
        if (_isSingleGrab)
        {
            var img = imgObjectInner.ToImageData();
            HpImageViewer.Image = img;//inner 和outer相同
            img.SaveBmp("D:\\test.bmp");
        }
        else
        {
            if (SelectedLayer == EnumFocusBasedOn.Inner)
                HpImageViewer.Image = imgObjectInner.ToImageData();
            else
                HpImageViewer.Image = imgObjectOuter.ToImageData();

        }



        // TestInfo.OvResultBase = centeringOVL;
        //TrainSetting.IsTrained = true;
        // = true;
        IsFocusPredictionTabEnabled = true;
        LoadOvResult(centeringOVL);
        HpImageViewer.UpdateROIs(rOIDescs.ToArray());
        ShowCurveControl();
    }



    protected List<ROIDesc> GetProfileForImageView(HObject imageObject, OMROIBase omROI, OMTargetType oMTargetType)
    {
        List<ROIDesc> rOIDescs = new List<ROIDesc>();
        OMUtil.GetProfileForImageView(imageObject, omROI, oMTargetType, out List<ProjectionProfile> profileList);
        foreach (var item in profileList)
        {
            List<Point> pointListInner = new List<Point>();
            for (int i = 0; i < item.Xs.Count(); i++)
            {
                pointListInner.Add(new Point(item.Xs[i], item.Ys[i]));
            }
            rOIDescs.Add(new PolygonDesc() { Vertices = pointListInner, IsClosed = false });
        }
        return rOIDescs;
    }
    protected List<CorrelationCurve> GetCorrelationCurve(HObject imageObjectInner, HObject imageObjectOuter, HTuple hTuple)
    {
        OMUtil.GetCorrelationCurve(imageObjectInner, imageObjectOuter, hTuple, out List<CorrelationCurve> curves);
        return curves;
    }

    protected override void SaveOMParamDict()
    {
        RebuildGoldenModel(true);
        RebuildGoldenModel(false);
        base.SaveOMParamDict();
        SaveInspectParamDict();
    }

    private void SubscribePolarityChanged()
    {
        if (TrainParam?.InnerLayer != null)
            TrainParam.InnerLayer.PropertyChanged += InnerLayer_PropertyChanged;
        if (TrainParam?.OuterLayer != null)
            TrainParam.OuterLayer.PropertyChanged += OuterLayer_PropertyChanged;
    }

    private void UnsubscribePolarityChanged()
    {
        if (TrainParam?.InnerLayer != null)
            TrainParam.InnerLayer.PropertyChanged -= InnerLayer_PropertyChanged;
        if (TrainParam?.OuterLayer != null)
            TrainParam.OuterLayer.PropertyChanged -= OuterLayer_PropertyChanged;
    }

    private void InnerLayer_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TestTrainLayerParam.PolarityType))
        {
            RebuildGoldenModel(true);
            SaveInspectParamDict();
        }
    }

    private void OuterLayer_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TestTrainLayerParam.PolarityType))
        {
            RebuildGoldenModel(false);
            SaveInspectParamDict();
        }
    }

    private void RebuildGoldenModel(bool isInner)
    {
        try
        {
            if (_omParam == null || TrainParam == null || TargetPoint ==null )
                return;
            var oMROIBase = isInner ? _omParam.OmROIInner : _omParam.OmROIOuter;
            if (oMROIBase == null)
                return;
            var imageData = SetRoiVM?.LoadFocusImageData(isInner);
            if (imageData == null)
                return;
            var targetType = isInner ? TrainSetting_Overlay.InnerType : TrainSetting_Overlay.OuterType;
            SaveGoldenModel(isInner, imageData, oMROIBase, ConvetTargetType(targetType), TrainParam);
        }
        catch (Exception ex)
        {
            LogHelper.LogError(ex);
        }
    }

    private void SaveInspectParamDict()
    {
        try
        {
            var damageCulling = TestInfo?.OverlayDataCulling?.DamageCulling;
            if (damageCulling == null) return;
            if (TrainParam?.InnerLayer?.GoldenModel == null || TrainParam?.OuterLayer?.GoldenModel == null)
                return;
            var inspectParam = new InspectParam()
            {
                TargetType = _omParam.TargetType,
                OmROIInner = _omParam.OmROIInner,
                OmROIOuter = _omParam.OmROIOuter,
                AlgorithmParam = damageCulling.ToInspectAlgorithmParam(),
                InspectModelInner = TrainParam.InnerLayer.GoldenModel,
                InspectModelOuter = TrainParam.OuterLayer.GoldenModel
            };
            inspectParam.SerializeToDictFile(Path.Combine(TestFilesFolder, "InspectParam.hdict"));
        }
        catch (Exception ex)
        {
            LogHelper.LogError(ex);
        }
    }

}
