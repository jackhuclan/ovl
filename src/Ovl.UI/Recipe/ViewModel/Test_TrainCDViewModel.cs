
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HalconDotNet;
using HPImageViewer.Core;
using HPImageViewer.Core.Persistence;
using InteVega.CBB.Common.Algorithm.Shapes;
using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.CommonLib.Helpers.Reflection;
using InteVega.CBB.DomainService.Api.Interfaces;
using InteVega.CBB.Recipe.Interface;
using InteVega.CBB.TemplateMatchingModule.Views;
using Marvel.Application.Extentions;
using Marvel.DataModels.Core;
using Marvel.DataModels.Enums;
using Marvel.DataModels.Recipe.CD;
using Marvel.DataModels40.DeviceParameter;
using Marvel.Interfaces.Application;
//using Marvel.Service.Unit;
using Megarobo.Ovl.Service.IntfFn;
using Megarobo.Ovl.Service.IntfFn.fn;
using Megarobo.Ovl.Type;
using Microsoft.Extensions.DependencyInjection;
using OpenCvSharp.Detail;
using OVL.Algorithm.DoNet;
using Prism.Services.Dialogs;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Wafer.Algorithm.Common.DoNet;
using WinCopies.Util;



using Ovl.UI.Controls;
using Ovl.UI.Extensions;
using DataModels = Marvel.DataModels;
namespace Ovl.UI.Recipe.ViewModel
{
    /// <summary>
    /// CD Train 主界面 ViewModel。
    /// 负责组织两个视野的参数页、执行 Train 阶段 CD 试跑、显示量测曲线与边缘点，
    /// 并在试跑时生成和保存正式量测使用的 CD 算法参数。
    /// </summary>
    public partial class Test_TrainCDViewModel : Test_TrainViewModelBase<TestInfo_CD>
    {
        const string _messageBoxToken = "TestTrainCDMessage";
        /// <summary>
        /// 是否跨视野
        /// </summary>
        [ObservableProperty]
        private bool isAcrossTheCameraVision;
        [ObservableProperty]
        private bool isShowMeasurement = false;
        [ObservableProperty]
        private EnumVision _Vision = EnumVision.Vision1;

        [ObservableProperty]
        private Test_TrainCDParamViewModelBase _trainParam1;
        [ObservableProperty]
        private Test_TrainCDParamViewModelBase _trainParam2;
        [ObservableProperty]
        private bool _measurementROI_EdgeEnabled;

        [ObservableProperty]
        ObservableCollection<PropertyViewModel> _propertyAlgoViewModels;
        /// <summary>
        /// 是否显示巡边的控制模块
        /// </summary>
        [ObservableProperty]
        bool isShowEdgeControl = true;
        public SingleTemplateControl TemplateControl1 { get; set; }
        public SingleTemplateControl TemplateControl2 { get; set; }

        IMeasurementService _measService;
        private readonly IServiceCalData _serviceCalData;
        private readonly IVisionService _StageController;
        DimensionEdgePoint[] EdgePointsPair;
        IEnumerable<CDCurve> Curves;
        /// <summary>
        /// Physical参数
        /// </summary>
        private Physical_CD PhysicalParam => TestInfo.Physical as Physical_CD;
        ///// <summary>
        ///// 图标控件
        ///// </summary>
        private CDParam _cdParam;
 

        public Test_TrainCDViewModel(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _serviceCalData = serviceProvider.GetService<IServiceCalData>();
            _measService = serviceProvider.GetService<IMeasurementService>();
            _StageController = serviceProvider.GetService<IVisionServiceManager>().GetMainVision();
 
        }

 
        public override  string Title => "Train Test";
        public override void OpenDialogThread(IDialogParameters parameters)
        {
            base.OpenDialogThread(parameters);
            if (TestInfo != null)
            {
                if (IsEnableWLI && TestInfo.TrainSetting.FocusModuleInfo.WliFocusOffset != 0)
                {
                    AbsolateWliHeight = TestInfo.TrainSetting_CD.GetFocusHeight(EnumCDFocusOffsetType.Wli) + _alignmentService.MacroFocusZ;
                }
            }
        }

        [RelayCommand]
        async void Loaded()
        {
            // OM40 使用带转塔设置的参数页，其它机型沿用通用 CD 参数页。
            if (MarvelVersion.CurrentVersion == DataModels.Enums.EnumOMVersion.OM40)
            {
                TrainParam1 = _serviceProvider.GetService<Test_Train40CDParamViewModel>();
                TrainParam2 = _serviceProvider.GetService<Test_Train40CDParamViewModel>();
                TrainParam1.IsShowTurrent = Visibility.Visible;
            }
            else
            {
                TrainParam1 = _serviceProvider.GetService<Test_TrainCDParamViewModel>();
                TrainParam2 = _serviceProvider.GetService<Test_TrainCDParamViewModel>();
            }

            // 两个视野共用主界面的图像显示控件，但各自维护独立的训练参数。
            TrainParam1.TemplateControl = TemplateControl1;
            TrainParam2.TemplateControl = TemplateControl2;
            TrainParam1.LoadData(HpImageViewer, _StageController, TestInfo, EnumVision.Vision1, TestFilesFolder, c => ChangeCameraInfo = c);
            TrainParam2.LoadData(HpImageViewer, _StageController, TestInfo, EnumVision.Vision2, TestFilesFolder, c => ChangeCameraInfo = c);

            // 不同机型的物理配置类不同，跨视野标志需要按实际 TestSettings 类型读取。
            if (((Physical_CD)TestInfo.Physical).TestSettings is TestPhysical_LargeCD testSetting)
            {
                IsAcrossTheCameraVision = testSetting.IsAcrossTheCameraVision;
            }
            if (((Physical_CD)TestInfo.Physical).TestSettings is TestPhysical_LargeCD100 testSetting100)
            {
                IsAcrossTheCameraVision = testSetting100.IsAcrossTheCameraVision;
            }
            if (((Physical_CD)TestInfo.Physical).TestSettings is TestPhysical_LargeCD40 testSetting40)
            {
                IsAcrossTheCameraVision = testSetting40.IsAcrossTheCameraVision;
            }
            if (TestInfo.TrainSetting_CD.Type == EnumTargetType.DS)
            {
                IsShowEdgeControl = false;
            }
            IsShowMeasurement = !IsAcrossTheCameraVision;

            // 先构造两路 TrainSetting 对应的算法参数，再加载已有 hdict 覆盖 UI 中的算法参数显示。
            BuildParam(TestInfo.TrainSettingSecond_CD);
            BuildParam(TestInfo.TrainSetting_CD);
            await StartGrabbing();
            string cdParamPath = Path.Combine(TestFilesFolder, "omParamTest.hdict");
            if (File.Exists(cdParamPath))
            {
                HOperatorSet.ReadDict(cdParamPath, new HTuple(), new HTuple(), out HTuple CDParamDict);
                CDParam param = (CDParam)new CDParam().ConvertFromDict(CDParamDict);
                _cdParam = param;
            }

            BuildAlgoParams(TestInfo.TrainSetting_CD);
            LoadCDMultiRoi();
        }

        protected override void SetVisionParams()
        {
            var cameraLightParamService = _serviceProvider.GetRequiredService<ICameraLightParamService>();
            var curcam = IServiceCamera.CurCamera;
            if (TrainSetting.CameraParams == null)
            {
                TrainSetting.CameraParams = cameraLightParamService.GetCamearaParam(curcam);
            }
            base.SetVisionParams();
            //训练界面不做曝光补偿 ，之前训练时保存的曝光值是多少，就显示多少
            //if (cameraLightParamService is ICameraLightParamService40 cameraLightParamService40)
            //{
            //    var exposureByPM = cameraLightParamService40.CalculateExposureByPM(IServiceCamera.CurCamera, TrainSetting.CameraParams as MeasureCameraParam40);
            //    var runtimeLightParam = CloneHelper.ReflectionClone<MeasureLightSourceParam40>(TrainSetting.LightSourceParam);
            //    cameraLightParamService.SetCamearaParam(curcam, TrainSetting.CameraParams, exposureByPM);
            //    cameraLightParamService40.SetLightSourceParamMeasureSingleFace(runtimeLightParam);
            //}
          
        }

        [RelayCommand]
         void OpenVision(object vision)
        {
            IsShowMeasurement = false;

            _StageController.StartGrabbing();
            var trainParam = vision.ToString() == "0" ? TrainParam1 : TrainParam2;
            var absolutePoint = trainParam.TrainSetting_CD.StageAbsolutePoint;
            if (absolutePoint != null && !absolutePoint.Equals(new Point2d(0, 0)))
                _alignmentService.MoveTo(absolutePoint, "CD_Train_" + trainParam.Vision);
            trainParam.InitMeasurementRoiButton();
            trainParam.SetButtonEnabled(true, false, false);
        }
        [RelayCommand]
         void OpenMeasurement() 
        {
            IsShowMeasurement = true;
        }
         

        #region 巡边ROI 操作
        ///// <summary>
        ///// 巡边ROI-Set
        ///// </summary>
        //[RelayCommand]
        //void MeasureRoiEdge_Set()
        //{
        //    //查找图片
        //    ImageData imageData = GetCDParamViewModel().LoadFocusImageData(null);
        //    HpImageViewer.Image = imageData;
        //    if (HpImageViewer.Image == null)
        //    {
        //        _messageBoxService.InfoDialog("The current image data is invalid！");
        //        return;
        //    }
        //    if (DrawROI(GetCDParamViewModel(), Vision, true))
        //    {
        //        MeasurementROI_EdgeEnabled = true;
        //    }
        //}
        //[RelayCommand]
        //void MeasureRoi_Edge_Confirm()
        //{
        //    MeasurementROI_EdgeEnabled = false;
        //    var roiList = GetCDParamViewModel().HpImageViewer.ImageViewerDesc.ROIDescs;
        //    List<Point2d> edgeRoiList = new List<Point2d>();
        //    foreach (var item in roiList)
        //    {
        //        if (item is CrosshairsDesc crosshairItem)
        //        {
        //            crosshairItem.IsMove = false;
        //            edgeRoiList.Add(new Point2d(crosshairItem.CenterX, crosshairItem.CenterY));
        //        }
        //    }
        //    GetCDParamViewModel().TrainSetting_CD.ROIEdgeList = edgeRoiList;
        //    GetCDParamViewModel().HpImageViewer.UpdateROIs(roiList.ToArray());
        //    SaveVisionParams();

        //}

        //[RelayCommand]
        //void MeasureRoi_Edge_ReSet()
        //{
        //    GetCDParamViewModel().TrainSetting_CD.ROIEdgeList = null;
        //    MeasureRoi_Edge_Cancel();
        //}


        [RelayCommand]
        void MeasureRoi_Edge_Cancel()
        {
            MeasurementROI_EdgeEnabled = false;
            HpImageViewer.UpdateROIs();
        }
      
        public Test_TrainCDParamViewModelBase GetCDParamViewModel()
        {
            return (Vision == EnumVision.Vision1 ? TrainParam1 : TrainParam2);
        }
        #endregion 

        /// <summary>
        /// 切换量测相机，开始采流
        /// </summary>
        /// <returns></returns>
        private async Task StartGrabbing()
        {
            await ChangeMeasureCamera(PhysicalParam.TestSettings.GetECamera());
            if (HpImageViewer.Image == null)
            {
                _StageController.StartGrabbing();
            }
        }

        [RelayCommand]
        void CloseView()
        {
            var result = new DialogResult(ButtonResult.OK, new DialogParameters() { { "modifiedTestInfo", TestInfo }, { "isClose", true } });
            RaiseRequestClose(result);
            NotifySaveAction = null;
        }
        [RelayCommand]
        void Submit()
        {
            NotifySaveAction?.Invoke(TestInfo, _messageBoxToken);
            var result = new DialogResult(ButtonResult.OK, new DialogParameters() { { "modifiedTestInfo", TestInfo } });
            RaiseRequestClose(result);
            NotifySaveAction = null;
        }
        [RelayCommand]
        void Save()
        {
            if (!AlgorithmParamVerify()) return;
            NotifySaveAction?.Invoke(TestInfo, _messageBoxToken);
        }

        bool AlgorithmParamVerify()
        {
            if (_cdParam.AlgorithmParam != null && _cdParam.AlgorithmParam is LineAlgorithmParam cdAlgorithmParam)
            {
                if (cdAlgorithmParam.EdgeFilterLevel > 3 || cdAlgorithmParam.EdgeFilterLevel < 0)
                {
                    _messageBoxService.ErrorDialog("Edge Filter Level(0-3)！");
                    return false;
                }
                else if (cdAlgorithmParam.MinEdgeContrast > 255 || cdAlgorithmParam.MinEdgeContrast < 0)
                {
                    _messageBoxService.ErrorDialog("Min Edge Contrast(0-255)！");
                    return false;
                }
            }
            return true;
        }
        public ObservableCollection<KeyValuePair<string, List<CDChartCurve>>> Datas = default;

        #region Run 
        [RelayCommand]
        void Run()
        {
            LogHelper.LogRunMethodStart();
            // 量测模板变化后必须重新建模，否则当前图像和训练模板的仿射关系不可靠。
            if (TrainParam1.IsNeedCreateModel)
            {
                _messageBoxService.ErrorDialog("Please Create CD_Mark_Model!", "Error");
                return;
            }
            _StageController.StopGrabbing();
            HpImageViewer.FitImageToArea();
            var trainParam = GetCDParamViewModel();
            HpImageViewer.Image = trainParam.LoadFocusImageData(true);
            if (HpImageViewer.Image == null)
                return;
            HpImageViewer.UpdateROIs();

            try
            {
                // Train Run 使用保存的量测焦面图，跨视野时需要同时拿到两个视野的量测图。
                ImageData imageMeasure1 = TrainParam1.LoadFocusImageData(true);
                bool isTest = false;
                if (isTest)
                {
                    imageMeasure1 = HpImageViewer.Image as ImageData;
                }
                if (imageMeasure1 == null)
                {
                    return;
                }
                ImageData imageMeasure2 = IsAcrossTheCameraVision ? TrainParam2.LoadFocusImageData(true) : null;
                if (TrainParam1.IsAcrossTheCameraVision && imageMeasure2 == null)
                {
                    return;
                }
             
                var testCamera = GetTestCamera();
                var curCamera = IServiceCamera.CurCamera;
                if(testCamera!=curCamera )
                {
                    _messageBoxService.ErrorDialog($"The current camera:{curCamera} is not the test camera:{testCamera}!", "Error");
                    return;

                }
                var pixels = _serviceCalData.GetUmPixel(GetTestCamera(), _serviceProvider);
                // 每次 Run 前刷新像素尺寸，避免标定值变化后沿用旧 hdict 中的比例。
                _cdParam.PixelSizeX = pixels.Item1;
                _cdParam.PixelSizeY = pixels.Item2;
                var imagedata_Vision = Vision == EnumVision.Vision1 ? imageMeasure1 : imageMeasure2;
                CDResultBase ret = null;
                if (TestInfo.TrainSetting_CD.Type == EnumTargetType.DS)
                {
                    // DSCD 量测使用聚焦图和 DotSpaceCDParam，普通 CD 使用量测图和 MultiDimensionROI。
                    ImageData imageFocus1 = TrainParam1.LoadFocusImageData(false);
                    ImageData imageFocus2 = IsAcrossTheCameraVision ? TrainParam2.LoadFocusImageData(false) : null;
                    ret = DSCDRun(imageFocus1, imageFocus2);
                    if (ret == null)
                        return;
                    if (Vision == EnumVision.Vision1)
                    {
                        List<CDCurve> cDCurves = new List<CDCurve>();
                        cDCurves.AddRange(_measService.GetCDProfileForImageView(imageFocus1, TestInfo.TrainSetting_CD.DotSpaceCDParam.CDRoi1));
                        cDCurves.AddRange(_measService.GetCDProfileForImageView(imageFocus2, TestInfo.TrainSetting_CD.DotSpaceCDParam.CDRoi2));
                        Curves = cDCurves;
                    }
                    else
                        Curves = _measService.GetCDProfileForImageView(imageFocus2, TestInfo.TrainSettingSecond_CD.DotSpaceCDParam.CDRoi1);

                }
                else
                {
                    ret = CDRun(imageMeasure1, imageMeasure2);
                    if (ret == null)
                        return;

                    Curves = GetCDParamViewModel().GetMultiRoiProfileCurves(imagedata_Vision);
                }
                var values = ret.CDValue;
                EdgePointsPair = ret.EdgePoints;

                // 先清掉旧显示，再根据本次量测结果重画曲线和边缘点；ROIEdgeList 只在为空时被自动初始化。
                MeasureRoi_Edge_Cancel();
                //处理显示
                //if (!DrawROI(TrainParam1, Vision, true)) return;
                if (!DrawMeasureROI(true)) return;


                var roidesc = GetCDParamViewModel().TrainSetting_CD.MultiROIToDimension;
                var curveProfiles = _measService.GetCDProfileForProfile(imagedata_Vision, roidesc).ToList();

                // 曲线窗口需要同时显示 Profile 和 Derivative，便于人工判断边缘定位是否合理。
                Datas = new ObservableCollection<KeyValuePair<string, List<CDChartCurve>>>(
                    curveProfiles.Select(item => new KeyValuePair<string, List<CDChartCurve>>(
                        SetCurveName(item.name),
                        new List<CDChartCurve>
                        {
                        new CDChartCurve
                        {
                            Name = "Profile",
                            Xs = item.profile.Xs,
                            Ys = item.profile.Ys
                        },
                        new CDChartCurve
                        {
                            Name = "Derivative",
                            Xs = item.derivative.Xs,
                            Ys = item.derivative.Ys
                        }
                        })));

                int num = 0;
                foreach (var item in MultiRoiList)
                {
                    item.Value = values[num];
                    num++;
                }
                if (IsAcrossTheCameraVision)
                {
                    // 跨视野只有一组 CD 值，需要同步回另一视野的 MultiRoiList，保证两个参数页显示一致。
                    var otherTrainParm = trainParam == TrainParam1 ? TrainParam2 : TrainParam1;
                    num = 0;
                    foreach (var item in otherTrainParm.TrainSetting_CD.MultiRoiList)
                    {
                        item.Value = values[num];
                        num++;
                    }
                }
                TestInfo.TrainSetting_CD.MeasuredValue = values;
                TestInfo.TrainSetting_CD.IsTrained = true;
                LogHelper.LogInfo($"Train test cd success, testName={TestInfo?.Name} ");
            }
            catch (Exception ex)
            {
                LogHelper.LogError(ex);
                _messageBoxService.Error(ex.Message, _messageBoxToken);
            }

            LogHelper.LogRunMethodEnd();
        }
        public string SetCurveName(string key) 
        {
            if (int.TryParse(key,out int value))
            {
                key= (value + 1).ToString();
            }
            return key;
        }

        private void SaveOMParam(CDParamBase param)
        {
            var paramPath = Path.Combine(TestFilesFolder, "omParamTest.hdict");
            LogHelper.LogInfo($"SaveOMParam,File={paramPath}");
            param.SerializeToDictFile(paramPath);
        }

        CDResultBase CDRun(ImageData imageMeasure1, ImageData imageMeasure2)
        {
            HpImageViewer.LockAll();


            CDResultBase ret = null;
            if (IsAcrossTheCameraVision)
            {
                // 跨视野量测必须同时具备第二视野 ROI，否则无法建立 CDRoi2 和对应模型。
                if (TestInfo.TrainSettingSecond_CD.ROIDescs == null)
                {
                    _messageBoxService.ErrorDialog("Measure roi for vision2 is not created!");
                    return null;
                }

                // 算法跨视野接口使用 um 级 Stage 坐标描述两个 FOV 的相对位置。
                OverFovInfo overFovInfo = new OverFovInfo();
                overFovInfo.StagePosFov1 = (TestInfo.TrainSetting_CD.StageAbsolutePoint ?? new Point2d(0, 0)) * 1000;//.StagePosFovPoint;
                overFovInfo.StagePosFov2 = ((TestInfo.TrainSettingSecond_CD.StageAbsolutePoint ?? TestInfo.TrainSetting_CD.StageAbsolutePoint) ?? new Point2d(0, 0)) * 1000;//   

                CDModelPar ModelPar = new CDModelPar();
                ModelPar.DimensionType = CDParamFactory.GetAlgoDimensionType(TrainParam1.MultiRoiList.FirstOrDefault()?.CDOrientention ?? PhysicalParam.CDSettings.Orientention);
                ModelPar.AlgorithmParam = _cdParam.AlgorithmParam;



                // 将两路 MultiRoiInfo 转为算法 MultiDimensionROI；这里会把 ROIEdgeList 写入 SetEdgePoint。
                CreateMultiDimensionROI(TestInfo.TrainSetting_CD);
                CreateMultiDimensionROI(TestInfo.TrainSettingSecond_CD);
                OverFovCDParam overFovCDParam = new OverFovCDParam();
                overFovCDParam.CDRoi1 = TestInfo.TrainSetting_CD.MultiROIToDimension;//.ROIToDimension;
                overFovCDParam.CDRoi2 = TestInfo.TrainSettingSecond_CD.MultiROIToDimension;//.ROIToDimension;

                // Train 阶段创建的 ModelInfo 和 ROI 会一起保存到 hdict，供正式量测服务复用。
                overFovCDParam.ModelInfo1 = _measService.CreateMeasureModelInfo(imageMeasure1, overFovCDParam.CDRoi1, ModelPar);//
                overFovCDParam.ModelInfo2 = _measService.CreateMeasureModelInfo(imageMeasure2, overFovCDParam.CDRoi2, ModelPar);//
                overFovCDParam.PixelSizeX = _cdParam.PixelSizeX;
                overFovCDParam.PixelSizeY = _cdParam.PixelSizeY;
                overFovCDParam.DimensionType = CDParamFactory.GetAlgoDimensionType(TrainParam1.MultiRoiList.FirstOrDefault()?.CDOrientention ?? PhysicalParam.CDSettings.Orientention);
                overFovCDParam.AlgorithmParam = _cdParam.AlgorithmParam;
                overFovCDParam.StageXIsSameDirectionWithImage = true;
                overFovCDParam.StageYIsSameDirectionWithImage = false;
                overFovCDParam.ConvertToDict(out var cdDict);

                SaveOMParam(overFovCDParam);
                
                var homMat2D_1 = GethomMat2D(imageMeasure1, TrainParam1);
                var homMat2D_2 = GethomMat2D(imageMeasure2, TrainParam2);
                // 试跑时直接调用算法，验证刚保存的跨视野参数是否可用。
                using var hImage1 = imageMeasure1.ToHObject();
                using var hImage2 = imageMeasure2.ToHObject();
                ret = _measService.MeasureCDOverFov(hImage1, hImage2, cdDict, homMat2D_1, homMat2D_2, overFovInfo);
            }
            else
            {
                // 单视野先创建并保存 MultiDimension 模型，再用同一份参数执行试跑。
                if (!CreateMultiModel(TrainParam1.TrainSetting_CD, imageMeasure1)) return null;
                ret = MultiCDRun(imageMeasure1);
                //if (!CreateModel(imageMeasure1)) return null;
                //_cdParam.ConvertToDict(out var cdDict);
                //ret = _measService.MeasureCD(imageMeasure1, cdDict, homMat2D_1);
            }
            oldParam = JsonHelper.Serialize(_cdParam);
            return ret;
        }
        string oldParam = string.Empty;
      
        CDResultBase MultiCDRun(ImageData imageMeasure1)
        {
            //CDResultBase ret = new CDResultBase();
            // 根据训练模板位置和当前模板匹配位置生成仿射矩阵，算法用它把 Train ROI 映射到当前图。
            var homMat2D_1 = GethomMat2D(imageMeasure1, TrainParam1);
      
            _cdParam.ConvertToDict(out var cdDict);
            CDResultBase ret = _measService.MeasureCD(imageMeasure1, cdDict, homMat2D_1);
            return ret;
        }



        CDResultBase DSCDRun(ImageData image1, ImageData image2)
        {
            // DSCD 使用 DotSpaceCDParam，需要先从前端 ROI 创建两组 CDROI 和 DS 专用模型。
            if (!CreateDSModel(TestInfo.TrainSetting_CD, TrainParam1)) return null;
            var homMat2D_1 = GethomMat2D(image1, TrainParam1);
            CDResultBase ret = null;
            if (IsAcrossTheCameraVision)
            {
                // 跨视野 DSCD 把两个视野各自的 CDRoi/ModelInfo 包装成 OverFovCDParam。
                if (!CreateDSModel(TestInfo.TrainSettingSecond_CD, TrainParam2)) return null;
                var homMat2D_2 = GethomMat2D(image2, TrainParam2);
                OverFovInfo overFovInfo = new OverFovInfo();
                overFovInfo.StagePosFov1 = TestInfo.TrainSetting_CD.StageAbsolutePoint;
                overFovInfo.StagePosFov2 = TestInfo.TrainSetting_CD.StageAbsolutePoint;

                OverFovCDParam overFovCDParam = new OverFovCDParam();
                overFovCDParam.CDRoi1 = TestInfo.TrainSetting_CD.DotSpaceCDParam.CDRoi1;
                overFovCDParam.CDRoi2 = TestInfo.TrainSettingSecond_CD.DotSpaceCDParam.CDRoi1;
                overFovCDParam.ModelInfo1 = TestInfo.TrainSetting_CD.DotSpaceCDParam.ModelInfo1;
                overFovCDParam.ModelInfo2 = TestInfo.TrainSettingSecond_CD.DotSpaceCDParam.ModelInfo1;
                overFovCDParam.PixelSizeX = _cdParam.PixelSizeX;
                overFovCDParam.PixelSizeY = _cdParam.PixelSizeY;
                overFovCDParam.StageXIsSameDirectionWithImage = true;
                overFovCDParam.StageYIsSameDirectionWithImage = false;
                overFovCDParam.DimensionType = CDParamFactory.GetAlgoDimensionType(PhysicalParam.CDSettings.Orientention);
                overFovCDParam.AlgorithmParam = TestInfo.TrainSetting_CD.DotSpaceCDParam.AlgorithmParam;
                overFovCDParam.ConvertToDict(out var cdDict);
                SaveOMParam(overFovCDParam);
                using var hImage1 = image1.ToHObject();
                using var hImage2 = image2.ToHObject();
                ret = _measService.MeasureCDOverFov(hImage1, hImage2, cdDict, homMat2D_1, homMat2D_2, overFovInfo);
            }
            else
            {
                ret = _measService.MeasureDSCD(image1, TestInfo.TrainSetting_CD.DotSpaceCDParam, homMat2D_1);
            }
            return ret;
        }
        #endregion



        /// <summary>
        /// 创建多维量测模板
        /// </summary>
        /// <param name="trainSettingCD"></param>
        /// <param name="imageData"></param>
        /// <returns></returns>
        bool CreateMultiModel(TrainSettings_CD trainSettingCD, ImageData imageData)
        {
            //多维ROI中存在空ROI则返回报错
            if (trainSettingCD.MultiRoiList.And(a => a.MeasureROI == null))
            {
                _messageBoxService.ErrorDialog("Measure Roi Not Create!");
                return false;
            }
            string newParam = JsonHelper.Serialize(_cdParam);
            //算法参数发生变化时，重置边缘点
            if (newParam != oldParam)
            {
                foreach (var roi in trainSettingCD.MultiRoiList)
                {
                    roi.ROIEdgeList = null;
                }
            }
            //量测ROI，使用 MultiDimensionROI 类型ROI
            _cdParam.CDRoi = CreateMultiDimensionROI(trainSettingCD);
            //量测尺寸类型，使用 MultiDimension 类型
            _cdParam.DimensionType = CDDimensionType.MultiDimension;
            //赋值生成的量测模板信息
            CDModelPar ModelPar = new CDModelPar();
            ModelPar.DimensionType = CDDimensionType.MultiDimension;
            ModelPar.AlgorithmParam = _cdParam.AlgorithmParam;
            _cdParam.ModelInfo = _measService.CreateMeasureModelInfo(imageData, _cdParam.CDRoi, ModelPar);
            // 保存后的 omParamTest.hdict 是正式量测服务的输入来源。
            SaveOMParam(_cdParam);
            return true;
        }
        private CDROIBase CreateMultiDimensionROI(TrainSettings_CD trainSettingCD)
        {
            // 扩展方法会把每个 MultiRoiInfo.MeasureROI 和 ROIEdgeList 转为算法层 DimensionROI。
            var cdRoi= trainSettingCD.MultiRoiList?.CreateMultiDimensionROI(IsAcrossTheCameraVision);
            trainSettingCD.MultiROIToDimension= cdRoi;
            return cdRoi;
        }
        /// <summary>
        /// 创建DSCD模板
        /// </summary>
        /// <param name="trainSettings_CD"></param>
        /// <param name="TestTrainCDParamViewModel"></param>
        /// <param name="enumVision"></param>
        /// <returns></returns>
        bool CreateDSModel(TrainSettings_CD trainSettings_CD, Test_TrainCDParamViewModelBase TestTrainCDParamViewModel)
        {
            if (trainSettings_CD.ROIDescs == null)
            {
                _messageBoxService.ErrorDialog("Measure Roi Not Create!");
                return false;
            }
            List<CDROIBase> cDROIBaseList = ROIRenderExtensions.CreateDsDimensionROI(trainSettings_CD.ROIDescs.Cast<RotatedPickersDesc>());
            if (cDROIBaseList != null && cDROIBaseList.Count > 0)
            {
                trainSettings_CD.DotSpaceCDParam.CDRoi1 = cDROIBaseList.GetEnumerableValue(0);
                if (IsAcrossTheCameraVision)
                    trainSettings_CD.DotSpaceCDParam.CDRoi2 = cDROIBaseList.GetEnumerableValue(0);
                else
                    trainSettings_CD.DotSpaceCDParam.CDRoi2 = cDROIBaseList.GetEnumerableValue(1);
            }
            CDModelPar ModelPar = new CDModelPar();
            ModelPar.DimensionType = CDParamFactory.GetAlgoDimensionType(PhysicalParam.CDSettings.Orientention);
            ModelPar.AlgorithmParam = trainSettings_CD.DotSpaceCDParam.AlgorithmParam;
            var image = TestTrainCDParamViewModel.HpImageViewer.Image as ImageData;
            //根据图片,roi,ModelPar 生成Model
            var (modelInfo1, modelInfo2) = _measService.CreateDSMeasureModelInfo(image, trainSettings_CD.DotSpaceCDParam.CDRoi1, trainSettings_CD.DotSpaceCDParam.CDRoi2, ModelPar);

            trainSettings_CD.DotSpaceCDParam.ModelInfo1 = modelInfo1;
            trainSettings_CD.DotSpaceCDParam.ModelInfo2 = modelInfo2;

            TestTrainCDParamViewModel.HpImageViewer.LockAll();

            SaveOMParam(trainSettings_CD.DotSpaceCDParam);
            return true;
        }
        HTuple GethomMat2D(ImageData image, Test_TrainCDParamViewModelBase test_TrainCDParamViewModel)
        {

            var template0 = test_TrainCDParamViewModel.TemplateControl.TemplateContext;
            // 模板模型文件可能未加载到内存，匹配前先从模板目录补齐 ModelInfo。
            _templateMatchingService.ReadTemplatesModelInfo(template0, _templateMatchingService.TemplatesFolder);
            var matchmodel = template0.SelectedMatchModelGroup.MatchModel;
            var matchItems = _templateMatchingService.FindTemplate(image, matchmodel.SearchingRect, matchmodel.ModelInfo, matchmodel.ParamMatch)?.MatchItems;

            if (!(matchItems?.Any() ?? false))
            {
                _messageBoxService.ErrorDialog("Not Find Template Reuslt!");
                return null;
            }
            var oldMatchItem = test_TrainCDParamViewModel.TrainSetting_CD.TrainMatchItem;
            var newMathItem = matchItems[0];
            LogHelper.LogInfo($"oldMatchItem={oldMatchItem},newMathItem={newMathItem}");
            // 用训练时模板位置和当前模板位置计算刚性变换，后续算法据此变换 ROI。
            HOperatorSet.VectorAngleToRigid(oldMatchItem.Row,
                                oldMatchItem.Column,
                                new HTuple(oldMatchItem.Angle).TupleRad(),
                                newMathItem.Row,
                                newMathItem.Column,
                                new HTuple(newMathItem.Angle).TupleRad(),
                                out var homMat2D);
            return homMat2D;
        }
        
        [RelayCommand]
        void Correlation()
        {
            if (Datas == null)
            {
                _messageBoxService.ErrorDialog("Not run!");
                return;
            }

            var dialogParameters = new DialogParameters
            {
                { "Datas", Datas},
            };
            _dialogService.ShowDialog(nameof(ChartResult), dialogParameters, _ => { });
        }
        #region private method
        private ECamera GetTestCamera()
        {
            return PhysicalParam.TestSettings.GetECamera();
        }
        private void BuildParam(TrainSettings_CD trainSettings_CD)
        {
            if (TestInfo.Physical is Physical_CD cdPhysical)
            {
                var eCam = GetTestCamera();
                var (px, py) = _serviceCalData.GetUmPixel(eCam, _serviceProvider);
                //DSCD使用DotSpaceCDParam
                if (trainSettings_CD.Type == EnumTargetType.DS)
                {
                    if (trainSettings_CD.DotSpaceCDParam?.AlgorithmParam == null)
                    {
                        trainSettings_CD.DotSpaceCDParam = new DotSpaceCDParam()
                        {
                            PixelSizeX = px,
                            PixelSizeY = py,
                            AlgorithmParam = new DotSpaceAlgorithmParam(),
                            DimensionType = CDDimensionType.DotSpace
                        };
                    }
                }
                else
                {
                    if (_cdParam?.AlgorithmParam == null)
                    {
                        _cdParam = CDParamFactory.Build(px, py,
                                                         trainSettings_CD.Type,
                                                         cdPhysical.CDSettings.Orientention);
                    }
                    else
                    {
                        _cdParam.DimensionType = CDParamFactory.GetAlgoDimensionType(cdPhysical.CDSettings.Orientention);
                    }
                }
            }
        }
        /// <summary>
        /// 算法参数显示
        /// </summary>
        private void BuildAlgoParams(TrainSettings_CD trainSettings_CD)
        {
            if (trainSettings_CD.Type == EnumTargetType.DS)
            {
                PropertyAlgoViewModels = new ObservableCollection<PropertyViewModel>(
    PropertyViewModelFactory.CreatePropertyModels(TestInfo.TrainSetting_CD.DotSpaceCDParam.AlgorithmParam));
            }
            else
            {
                PropertyAlgoViewModels = new ObservableCollection<PropertyViewModel>(
    PropertyViewModelFactory.CreatePropertyModels(_cdParam.AlgorithmParam));
            }
        }
        #endregion
    }
}
