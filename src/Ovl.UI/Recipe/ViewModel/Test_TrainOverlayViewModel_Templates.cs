using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPImageViewer.Core;
using HPImageViewer.Core.Persistence;
using InteVega.CBB.Common.Algorithm;
using InteVega.CBB.Common.Algorithm.Shapes;
using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.DomainService.Api.Extensions;
using InteVega.CBB.StageControlModule.Views;
using InteVega.CBB.TemplateMatching.Core;
using InteVega.CBB.TemplateMatchingModule.Views;
using Marvel.DataModels;
using Marvel.DataModels.Enums;
using Megarobo.Ovl.Type;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Ovl.UI.Recipe.View;
using Ovl.UI.ViewModels.Extensions;
namespace Ovl.UI.Recipe.ViewModel
{
    public abstract partial class Test_TrainOverlayViewModel<T>
    {
        private SingleTemplateControl InnerTemplateControl;
        private SingleTemplateControl OuterTemplateControl;

        [ObservableProperty]
        private bool _searchROI_SetEnabled;

        
        /// <summary>
        /// 设置需要依赖的控件
        /// </summary>
        /// <param name="imageViewer">图像控件</param>
        /// <param name="templatesView">模板控件</param>
        internal void SetControl(IHPImageViewer imageViewer, Test_TrainTemplatesView templatesView)
        {
            HpImageViewer = imageViewer;
            PositioningTemplateControl = templatesView.PositioningControl.TemplateControl;
            PositioningTemplateControl.ImageViewer = imageViewer;
            InnerTemplateControl = templatesView.InnerTemplateControl;
            OuterTemplateControl = templatesView.OuterTemplateControl;
        }


        void LoadTemplates()
        {
            RoiExtendSize_Inner = TrainSetting_Overlay.RoiExtendSize_Inner;
            RoiExtendSize_Outer = TrainSetting_Overlay.RoiExtendSize_Outer;
            if (TrainSetting_Overlay.RoiExtendSize_Inner == 0 && TrainSetting_Overlay.RoiExtendSize_Outer == 0)
            {
                if (TrainSetting_Overlay.InnerType == EnumTargetType.AIM || TrainSetting_Overlay.InnerType == EnumTargetType.AIM_XYS)
                {
                    RoiExtendSize_Inner = 40;
                    RoiExtendSize_Outer = 40;
                }
                else
                {
                    RoiExtendSize_Inner = 0;
                    RoiExtendSize_Outer = 0;
                }
            }

            if (InnerTemplateControl != null)
            {
                if (!TrainSetting_Overlay.OverlayMark_InnerModels.Any())
                {
                    TrainSetting_Overlay.OverlayMark_InnerModels = new List<SingleTemplateMatchingContext>() { new() };
                }
                TrainSetting_Overlay.OverlayMark_InnerModels[0].Name = "Inner Model";

                InnerTemplateControl.TemplateContext = TrainSetting_Overlay.OverlayMark_InnerModels[0];
                InnerTemplateControl.ActionUserModified = TemplateUserModified;
                InnerTemplateControl.FuncBeforeCreateModel = InnerTemplateBeforeCreateModel;
                InnerTemplateControl.FuncBeforeCleanModel = FuncBeforeCleanModel;
                if (_focusSetting.FocusBasedOn == EnumFocusBasedOn.Inner)
                {
                    HpImageViewer.SetTemplateImage(InnerTemplateControl.TemplateContext, TestFilesFolder);
                }
            }
            if (OuterTemplateControl != null)
            {
                if (!TrainSetting_Overlay.OverlayMark_OuterModels.Any())
                {
                    TrainSetting_Overlay.OverlayMark_OuterModels = new List<SingleTemplateMatchingContext>() { new() };
                }
                TrainSetting_Overlay.OverlayMark_OuterModels[0].Name = "Outer Model";
                OuterTemplateControl.TemplateContext = TrainSetting_Overlay.OverlayMark_OuterModels[0];
                OuterTemplateControl.ActionUserModified = TemplateUserModified;
                OuterTemplateControl.FuncBeforeCreateModel = OuterTemplateBeforeCreateModel;
                OuterTemplateControl.FuncBeforeCleanModel = FuncBeforeCleanModel;
                if (_focusSetting.FocusBasedOn == EnumFocusBasedOn.Outer)
                {
                    HpImageViewer.SetTemplateImage(OuterTemplateControl.TemplateContext, TestFilesFolder);
                }
            }

      
            InitMeasurementRoiButton();
        }


        /// <summary>
        /// 加载创建模板的原图
        /// </summary>
        /// <param name="isInner">是否是内层</param>
        /// <returns></returns>
        protected abstract ImageData LoadTemplateModeImage(bool isInner);




        /// <summary>
        /// 创建量测的KernelSize和定位模板
        /// </summary>
        /// <param name="isInner"></param>
        /// <param name="isShowTestResult">是否显示模板匹配结果</param>
        public async Task CreateMeasureKernelSizeAndTemplate(bool isInner, bool isShowTestResult)
        {
            try
            {
                await AutoCreateModel(isInner, isShowTestResult);
                AutoCreateKernelModel(isInner);
            }
            catch (Exception ex)
            {
                LogHelper.LogError(ex);
                _messageBoxService.ErrorDialog("Failed to generate measurement template data！");
            }
        }

        protected abstract void AutoCreateKernelModel(bool isInner);

        private bool BeforeCreateLayerModel(bool isInner)
        {
            var imageData = LoadTemplateModeImage(isInner);
            HpImageViewer.Image = imageData;
            if (HpImageViewer.Image == null)
            {
                _messageBoxService.InfoDialog("The current image data is invalid！");
                return false;
            }

            //点击Confirm按钮时，重绘量测Roi，避免被其它roi影响
            var roiDesc = GetMeasureRoiDesc(isInner);// isInner ? TestTrainSetting.InnerRectangle : TestTrainSetting.OuterRectangle;
            if (roiDesc == null)
            {
                _messageBoxService.InfoDialog("Please draw the corresponding ROI！");
                return false;
            }
            if (TargetPoint == null)
            {
                _messageBoxService.InfoDialog("Please redraw the measure ROI！");
                return false;
            }
            HpImageViewer.UpdateROIs(roiDesc);

            _visionService.StopGrabbing();
            SetRoiExtendSize(isInner);
            return true;
        }

        internal bool InnerTemplateBeforeCreateModel()
        {
            return BeforeCreateLayerModel(true);
        }
        internal void TemplateUserModified(SingleTemplateControl control, ITemplateMatchingView.UserModifyType type, SingleTemplateMatchingContext context, bool isSuccess)
        {
            if (!isSuccess) return;
            bool isInner = control == InnerTemplateControl;
            TemplateUserModified(isInner, type, context);
        }

        private void TemplateUserModified(bool isInner, ITemplateMatchingView.UserModifyType type, SingleTemplateMatchingContext context)
        {
            
            if (type == ITemplateMatchingView.UserModifyType.ModelCreated)
            {
                if (isInner)
                {
                    TrainSetting_Overlay.InnerTrainMatchItem = context.MatchingItems[0];
                }
                else
                {
                    TrainSetting_Overlay.OuterTrainMatchItem = context.MatchingItems[0];
                }
            }
            if (type == ITemplateMatchingView.UserModifyType.ModelCleanUp)
            {
                HpImageViewer.Image = LoadTemplateModeImage(isInner);
            }

        }

        internal bool OuterTemplateBeforeCreateModel()
        {
            return BeforeCreateLayerModel(false);

        }



        /// <summary>
        /// 自动创建定位模板
        /// </summary>
        /// <param name="isInner"></param>
        protected async Task AutoCreateModel(bool isInner, bool isShowTestResult)
        {
            //if (!BeforeCreateLayerModel(isInner))
            //    return;
            var modelControl = isInner ? InnerTemplateControl : OuterTemplateControl;
            //如果启用了定位模板，但是未设置搜索区域的话，则自动添加搜索区域
            if (TrainSetting_Overlay.PositioningTemplateInfo.IsAvailable)
            {
                var shapeBase = GetMeasureRoiDesc(false).CreateTemplateRoi();
                var roiRect = CreateRectByShapeBase(shapeBase);
                var searchRect = modelControl.TemplateContext.SelectedMatchModelGroup.MatchModel.SearchingRect;
                if (searchRect == null || searchRect.Width < roiRect.Width || searchRect.Height < roiRect.Height)
                {
                    modelControl.TemplateContext?.UpdateSearchArea(roiRect);
                }
            }
            LogHelper.LogDebug($"AutoCreateModel isInner = {isInner},searchRect = {modelControl.TemplateContext.SelectedMatchModelGroup.MatchModel.SearchingRect}, searchRect180 = {modelControl.TemplateContext.SelectedMatchModelGroup.MatchModel180.SearchingRect}");

            await modelControl.CreateModelAsync(isShowTestResult);
            //var templateVM = isInner ? TemplateViewModel_Inner : TemplateViewModel_Outter;
            //if (!templateVM.IsShowCreateModel)
            //{
            //    await templateVM.TrainModel(isTest);
            //}
        }



        //void CheckTemplateData(List<SingleTemplateMatchingContext> templates, string key)
        //{
        //    foreach (SingleTemplateMatchingContext template in templates)
        //    {
        //        CheckTemplateData(template, key);
        //    }
        //}

        void CheckTemplateData(SingleTemplateMatchingContext template, string key)
        {
            _templateMatchingService.ReadTemplatesModelInfo(template, TestFilesFolder);
            if (template.SelectedMatchModelGroup == null || template.SelectedMatchModelGroup.MatchModel.ModelInfo == null)
            {
                _messageBoxService.Error($"The {key} template is empty!", _messageBoxToken);
            }
        }

        void UpdateTestInfoData(bool isCheckData)
        {
            if (isCheckData)
            {
                UpdateTemplateModelScore();
                CheckTemplateData(InnerTemplateControl.TemplateContext, CONST_Inner);
                CheckTemplateData(OuterTemplateControl.TemplateContext, CONST_Outer);
            }

        }
        protected void SetRoiExtendSize(bool isInner)
        {
            var template = isInner ? InnerTemplateControl.TemplateContext : OuterTemplateControl.TemplateContext;
            if (template.SelectedMatchModelGroup != null)
            {
                template.SelectedMatchModelGroup.RoiExtendSize = isInner ? RoiExtendSize_Inner : RoiExtendSize_Outer;
            }
        }

        #region  搜索区域


        public void InitMeasurementRoiButton(bool isClearRoi = true)
        {
            SearchROI_SetEnabled = true;
            if (isClearRoi)
                HpImageViewer.UpdateROIs();
        }

        /// <summary>
        ///设置或显示SearchRoi
        /// </summary>
        [RelayCommand]
        void SearchRoi_Set()
        {
            var searchRoi = GetSearchingRect();
            if (searchRoi == null && HpImageViewer.Image is ImageData img)
            {
                double size = Math.Min(img.Width, img.Height) / 2;
                double left = img.Width / 2 - size / 2;
                double top = img.Height / 2 - size / 2;
                searchRoi = new Rect2d(left, top, size, size);
            }
            HpImageViewer.UpdateROIs(searchRoi?.CreateRoiDesc());
            SearchROI_SetEnabled = false;

        }

        /// <summary>
        /// 创建SearchRoi
        /// </summary>
        [RelayCommand]
        void SearchRoi_Confirm()
        {
            var rOIDescs = HpImageViewer.ImageViewerDesc.ROIDescs.FirstOrDefault();
            if (rOIDescs == null || rOIDescs is not RectangleDesc desc)
            {
                _messageBoxService.InfoDialog("Please draw the Rect ROI！");
                return;
            }
            SetSearchingRect(desc.CreateRoi() as Rect2d);
            InitMeasurementRoiButton();
        }

        private SingleTemplateMatchingContext GetTemplate(bool isInner)
        {
            return isInner ? TrainSetting_Overlay.OverlayMark_InnerModels[0] : TrainSetting_Overlay.OverlayMark_OuterModels[0];
        }
        private void SetSearchingRect(Rect2d rect)
        {
            GetTemplate(true).UpdateSearchArea(rect);
            GetTemplate(false).UpdateSearchArea(rect);
        }

        private Rect2d GetSearchingRect()
        {
            return GetTemplate(true).SelectedMatchModelGroup.MatchModel.SearchingRect;
        }


        /// <summary>
        /// 清除SearchRoi
        /// </summary>
        [RelayCommand]
        void SearchRoi_Clear()
        {
            SetSearchingRect(null);
            HpImageViewer.UpdateROIs();
            InitMeasurementRoiButton();
        }

        /// <summary>
        /// 取消设置SearchRoi
        /// </summary>
        [RelayCommand]
        void SearchRoi_Cancel()
        {
            InitMeasurementRoiButton();
        }

        #endregion

    }
}
