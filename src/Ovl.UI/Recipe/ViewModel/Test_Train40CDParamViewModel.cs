using HPImageViewer.Core.Persistence;
using ImTools;
using InteVega.CBB.Common;
using InteVega.CBB.Common.Algorithm.Shapes;
using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.DomainService.Api.Extensions;
using InteVega.CBB.DomainService.Api.Interfaces;
using InteVega.CBB.DomainService.Api.Models;
using Marvel.DataModels.DeviceParameter;
using Marvel.DataModels.Enums;
using Marvel.DataModels.Extensions;
using Marvel.DataModels.Recipe.CD;
using Marvel.Interfaces.Application;
using Megarobo.Ovl.Service.Interface;
using Megarobo.Ovl.Service.IntfFn;
using Megarobo.Ovl.Service.IntfFn.fn;
using Megarobo.Ovl.Type;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using InteVega.CBB.MotionProof.Abstraction;
using Wafer.Algorithm.Common.DoNet;
using WinCopies.Util;

using Ovl.UI.Extensions;
namespace Ovl.UI.Recipe.ViewModel
{
    public partial class Test_Train40CDParamViewModel : Test_TrainCDParamViewModelBase
    {
        internal AxisType CurAxis => GetTestCamera() == ECamera.Meas ? _machine40Config.PatternCameraCfg.MeasCamAxis : _machine40Config.PatternCameraCfg.MeasCamExtAxis;
        public Test_Train40CDParamViewModel(IServiceProvider serviceProvider, IFocusConfigService focusMeterService) : base(serviceProvider)
        {
            _focusMeterService = focusMeterService;
            _turrenService = serviceProvider.GetRequiredService<IServiceTurrent>();
            var _machineDomainService = serviceProvider.GetService<IMachineDomainService>();
            _machine40Config = _machineDomainService.MachineDomainConfig;
            _focusService40 = serviceProvider.GetService<IFocusService_SpecCam>();
            _servicePlat = serviceProvider.GetService<IServicePlatNoProof>() as IServicePlat;
        }
        protected readonly MachineDomainConfig _machine40Config;
        protected readonly IServicePlat _servicePlat;
        protected readonly IFocusService_SpecCam _focusService40;
        protected readonly IFocusConfigService _focusMeterService;
        protected IAlignmentService40 _alignmentService40 => _alignmentService as IAlignmentService40;
        protected IServiceTurrent _turrenService;
        ///// <summary>
        ///// 加载转塔数据
        ///// </summary>
        //protected void LoadTurrent()
        //{
        //    if (_turrenService == null) return;
        //    //获取转塔列表
        //    List<TurrentMeta> turrent = _turrenService.Turrent.ToList();
        //    TurrentMetaList = new ObservableCollection<TurrentMeta>(turrent);
        //    int currentTurrent = 0;
        //    try
        //    {
        //        //查询当前相机ID
        //        currentTurrent = _turrenService.GetU();//当前这边报错
        //        //绑定当前相机
        //        TurrentMeta = TurrentMetaList.FirstOrDefault(w => w.Uid == currentTurrent);
        //    }
        //    catch (Exception ex)
        //    {
        //        TurrentMeta = TurrentMetaList.FirstOrDefault();
        //    }
        //    //如果是修改，则加载配方转塔ID
        //    if (TrainSetting_CD.TurrentX != 0)
        //    {
        //        TurrentMeta = TurrentMetaList.FirstOrDefault(f => f.Uid == TrainSetting_CD.TurrentX);
        //    }
        //}
        protected override void TrainFocusAction()
        {
            if (!CheckFocusParam()) return;
            LogHelper.LogRunMethodStart();
            _changeCameraInfoAction?.Invoke("Focusing …");
            Task.Run(async () =>
            {
                try
                {
                    var macroFocusZ = _alignmentService.MacroFocusZ;
                    if (_cameraType == CDCameraType.Align)
                    {
                        HpImageViewer.UpdateROIs();
                        _StageController.StartGrabbing();
                        var waferThickness = _alignmentService.WaferMapInfo.Thickness;
                        var focusInfo = GetTestFocusOrDefault_new(GetTestCamera());
                        //粗聚焦的开始位置为标定的聚焦位置+ 晶圆的厚度
                        var coarseFocusInfo = focusInfo.CourseFocusInfo;
                        coarseFocusInfo.StartPosition = macroFocusZ + waferThickness;

                        ShapeBase shapeBaseRoi = TrainSetting_CD.FocusRoiDesc.GetEnumerableValue(0).CreateRoi();
                        // 刷新界面的ROI颜色
                        TrainSetting_CD.FocusRoiDesc.ToList().ForEach(_ => _.Color = "#FFFF00");
                        HpImageViewer.UpdateROIs(TrainSetting_CD.FocusRoiDesc.ToArray());
                        var coarseFocus = _focusService.CoarseFocusWithImages(coarseFocusInfo, shapeBaseRoi, true);
                        FocusZHeight = coarseFocus.ZFit;
                        //将Z轴移动到粗聚焦位置
                        _alignmentService.MoveZTo(FocusZHeight, "TrainedFocusHeight", false);
                        MoveAndWait(null);
                        if (_cameraType == CDCameraType.Align)
                        {
                            //根据量测Roi，训练两点曲线
                            var trainArg = CreateTrainArg(EnumGrabMode.SingleGrab);
                            var ret = _focusService.TrainCurve(coarseFocusInfo, shapeBaseRoi, coarseFocus.PegImages, trainArg);
                            TrainSetting_CD.GlobalCurve = ret.Curve;

                            var image = _alignmentService.MoveAndGrab(null);
                            string filePath = Path.Combine(TestFilesFolder, FocusImageName + ".bmp");
                            FileHelper.DeleteFile(filePath);
                            image.SaveBmp(filePath);

                            string fileMesaurePath = Path.Combine(TestFilesFolder, MeasureImageName + ".bmp");
                            FileHelper.DeleteFile(fileMesaurePath);
                            image.SaveBmp(fileMesaurePath);
                        }
                    }
                    else
                    {
                        //自动精聚焦
                        await TrainFineFocus();
                    }
                    //TrainSetting_CD.TurrentX = TurrentMeta.Uid;

                    var zCal = _alignmentService40.GetMacroFocusZ(GetTestCamera()).z;
                    TrainSetting_CD.FocusModuleInfo.BaseFocusHeight = FocusZHeight - zCal - _alignmentService40.WaferMapInfo.Thickness;

                   TrainWLI("");
                    if (wliPztOffset != null)
                    {
                        AbsolateWliHeight = _pztservice.CombineZPzt(_servicePlat.GetCurrentZ(AxisType.Z), wliPztOffset.Value);
                    }

                    TrainSetting_CD.FocusModuleInfo.WliFocusOffset = TrainSetting_CD.GetDefaultFocusHeight() - (AbsolateWliHeight - macroFocusZ);
                    AutoFocusHeight = FocusZHeight;
                    LogHelper.LogInfo($"Focus Result:FocusFinalHeight={TrainSetting_CD.FocusModuleInfo.BaseFocusHeight:0.######}, FocusZHeight={FocusZHeight:0.######}, PztOffset={TrainSetting_CD.PztOffset:0.######}");
                    _StageController.StopGrabbing();
                    isTrainedFocus = true;
                }
                catch (Exception ex)
                {
                    LogHelper.LogError(ex);
                    string errmsg = "Focusing failed. Please modify the focusing parameters and try again.";
                    AppHelper.DispatcherInvoke(() =>
                    {
                        _messageBoxService.ErrorDialog(errmsg);
                    });
                }
                finally
                {

                    _changeCameraInfoAction?.Invoke("");
                    HpImageViewer.UpdateROIs();
                    LogHelper.LogRunMethodEnd("", nameof(TrainFocusAction));
                }
            });

        }


        #region
        private async Task TrainFineFocus()
        {
            // 全视野、pzt全行程、小步进精聚焦训练
            HpImageViewer.UpdateROIs();
            await Task.Run(() => fine_Res = TrainGlobal());
            FocusZHeight = TrainRoiFocus(fine_Res, false);

            //await FocusInnerOuterSingleGrab(false, true);
        }
        /// <summary>
        /// 全视野聚焦，并训练聚焦曲线
        /// </summary>
        /// <returns></returns>
        public override FocusPegResult TrainGlobal()
        {
            var allRangeFocusInfo = CreateFullTrainInfo(GetTestCamera());
            // 调用示例
            return ExecuteGlobalFineFocus(allRangeFocusInfo);
        }
        /// <summary>
        /// 创建聚焦训练行程和步进
        /// 行程用全行程设置为1，步进用0.1
        /// </summary>
        /// <returns></returns>
        protected AutoRunInfo CreateFullTrainInfo(ECamera cam)
        {
            // 40改为使用全行程，因为使用各自层面的行程会导致有些层面行程不够导致训练曲线太窄
            //var iCfg = _serviceProvider.GetService<ISystemConfigService>();
            //var lCfg = iCfg.GetConfig<FocusCfg>();
            var measCfg = _focusMeterService.GetCurrentFocusMeter();// lCfg.MeasZMeter; //cam == ECamera.Meas ? lCfg.MeasZMeter : lCfg.MeasZExtMeter;
            int unit = 1000;
            // 此处大行程步进必须大于小形成步进，否则执行精聚焦参数校验时会报错
            return new AutoRunInfo() { Range = measCfg.AllRange / unit, Step = measCfg.AllRangeStep * 2 / unit, SmallRange = measCfg.AllRange / unit, SmallStep = measCfg.AllRangeStep / unit };
        }

        /// <summary>
        /// 执行全视野精细聚焦
        /// </summary>
        /// <returns>聚焦结果</returns>
        private FocusPegResult ExecuteGlobalFineFocus(AutoRunInfo allRangeFocusInfo)
        {
            allRangeFocusInfo.StartPosition = _servicePlat.GetCurrentZ(CurAxis);
            var res = _focusService40.FineFocusSingleCamTrain(GetTestCamera(), allRangeFocusInfo, null);
            MoveAndWait(res.ZFit);
            return res;
        }
        #endregion


        private void MoveAndWait(double? focusZHeight)
        {
            _servicePlat.MoveZ(false, CurAxis, focusZHeight.Value);
            //Task.Delay(2000).Wait();
        }

        protected override void MoveAndWait(double? focusZHeight, double? pztHeight)
        {
            MoveAndWait(pztHeight);
        }

        protected override double GetRoiFocusPosition()
        {
            return FocusZHeight;
        }

        protected override void SetRoiFocusPostion(double roiFocusOffset)
        {
            FocusZHeight = roiFocusOffset;
        }
        protected override void Vm_CorrectionPointSelected(bool isInner, double focusPos)
        {
            _servicePlat.MoveZ(_machine40Config.PatternCameraCfg.MeasCamAxis, focusPos);
        }
        protected override double TrainRoiFocus(FocusPegResult focusPegResult, bool isMeasureRoi)
        {
            IEnumerable<ROIDesc> roiDescList = null;
            ShapeBase shapeBase = null;
            if (isMeasureRoi)
            {
                roiDescList = MultiRoiList.Select(s => s.MeasureROI);
                shapeBase = MultiRoiList.CreateMultiDimensionROI(IsAcrossTheCameraVision).ToDimension2d();
            }
            else
            {
                roiDescList = TrainSetting_CD.FocusRoiDesc;
                shapeBase = roiDescList.FirstOrDefault().CreateRoi();
            }

            HpImageViewer.UpdateROIs(roiDescList.ToArray());
            var coarseFocusInfo = GetTestFocusOrDefault_new(IServiceCamera.CurCamera).CourseFocusInfo;
            var focusImages = focusPegResult.PegImages;
            var trainArg = CreateTrainArg(EnumGrabMode.SingleGrab);
            //使用全视野的聚焦序列图进行内、外层聚焦，并且获取聚焦曲线
            var ret = _focusService.TrainCurve(coarseFocusInfo, shapeBase, focusImages, trainArg);
            TrainSetting_CD.RoiCurve = ret.Curve;
            var pztOffset = ret.Curve.RefMaximumZ.First();

            MoveAndWait(pztOffset);
            SaveFocusImage(FocusImageName, shapeBase, isMeasureRoi);
            //创建量测模板用图
            SaveFocusImage(MeasureImageName, null);

            //此处不需要考虑有没有焦面偏移，只有在焦面偏移值发生变更的时候
            //if (!(TrainSetting_CD.FocusCorrection != 0 && CorrectionVisibility == Visibility.Visible && isMeasureRoi))
            //{
            //    MoveAndWait(pztOffset);
            //    SaveFocusImage(FocusImageName, shapeBase, isMeasureRoi);
            //}
            //else
            //{
            //    MoveAndWait(pztOffset + TrainSetting_CD.FocusCorrection);
            //}


            return pztOffset;
        }

        protected void SaveAttenuationCoef(ECamera camera, CameraParamBase cameraParam)
        {
            var camAttr = _serviceProvider.GetService<IServiceCamera>().GetCameraAttr(camera);
            var age = _serviceProvider.GetService<IServiceCalData>().GetLightAge(camera, camAttr);
            if (cameraParam is MeasureCameraParam measureCameraParam)
            {
                TestInfo.IsLightPmExecuted = true;
                measureCameraParam.AttenuationCoef = age;
                LogHelper.LogDebug($"Set Age:{age}, Camera: {camera}");
            }
        }

        protected override void SaveVisionParams()
        {
            try
            {
                var curCamera = IServiceCamera.CurCamera;
                TrainSetting.CameraParams = _cameraLightParamService.GetCamearaParam(curCamera);
                TrainSetting.LightSourceParam = _cameraLightParamService.GetLightSourceParam(curCamera);
                SaveLightAgeInitialInfo(curCamera, TrainSetting.CameraParams, TrainSetting.LightSourceParam);
                SaveAttenuationCoef(curCamera,TrainSetting.CameraParams);
            }
            catch (Exception e)
            {
                LogHelper.LogError(e);
            }
        }
    }
}
