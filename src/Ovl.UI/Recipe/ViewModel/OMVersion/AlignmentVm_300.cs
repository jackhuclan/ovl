using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.Prism.UI.Extensions.MessageBox;
using InteVega.CBB.Recipe.Core;
using InteVega.CBB.Recipe.Interface;
using InteVega.CBB.TemplateMatching.Core;
using InteVega.CBB.TemplateMatchingModule.ViewModels;
using Marvel.DataModels.DeviceParameter;
using Marvel.DataModels.Recipe;
using Megarobo.Ovl.Type;
using Prism.Services.Dialogs;
using System;
using System.Threading.Tasks;

namespace Ovl.UI.Recipe.ViewModel.OMVersion
{
    public partial class AlignmentVm_300 : AlignmentVmBase
    {
        public AlignmentVm_300(IDialogService dialogService, IServiceProvider sp, IMessageBoxService messageBoxService) : base(dialogService, sp, messageBoxService)
        {
            _coarseAlignTemplateContext ??= new MultiTemplateMatchingContext() { IsForAlignment = true };
            _coarseAlignCamera = ECamera.AlignExt;
        }

        /// <summary>
        /// 精对准模板匹配信息
        /// </summary>
        MultiTemplateMatchingContext _coarseAlignTemplateContext;

        /// <summary>
        /// 精对准模板匹配ViewModel
        /// </summary>
        [ObservableProperty]
        MultiTemplateMatchingViewModel _coarseAlignTemplateViewModel;
        public override void RequestData(IRecipe data, RecipeElementsInfo recipe, RecipeInfo recipeInfo)
        {
            _isFineAlignment = false;
            base.RequestData(data, recipe, recipeInfo);
            ChangeCamera(_coarseAlignCamera);
        }

        protected override void Loaded()
        {
            base.Loaded();
            if (AlignmentInfo.CoarseAlignModels != null)
            {
                _coarseAlignTemplateContext.TemplateContexts = AlignmentInfo.CoarseAlignModels;
            }
            else
            {
                AlignmentInfo.CoarseAlignModels = _coarseAlignTemplateContext.TemplateContexts;
                AlignmentInfo.CoarseAlignModels[0].Name = "Coarse Align Model";
            }

            CoarseAlignTemplateViewModel = _serviceProvider.CreateTemplateViewModel(_coarseAlignTemplateContext, HpImageViewer) as MultiTemplateMatchingViewModel;
            CoarseAlignTemplateViewModel.UserModified += TemplateViewModel_UserModified;
            initDisplayAlignInfo();
        }

        protected override void UpdataStageCameraParamAndWriteToCamera(ECamera camera)
        {
            if (camera == ECamera.Align)
            {
                _cameraLightParamService.SetCamearaParam(camera, AlignmentInfo.AlignCameraParms);
                _cameraLightParamService.SetLightSourceParam(camera, AlignmentInfo.AlignPointLightParms);

            }
            else if (camera == ECamera.AlignExt)
            {
                _cameraLightParamService.SetCamearaParam(camera, AlignmentInfo.CoarseAlignCameraParms);
                _cameraLightParamService.SetLightSourceParam(camera, AlignmentInfo.CoarseAlignPointLightParms);

            }
            // var param = _cameraLightParamService.GetCamearaParam(camera);
        }

        protected override void OnTableSelectedChanged(int value)
        {
            if (value == -1) return;
            if (value == 2)
            {
                _isFineAlignment = true;
                ChangeCamera(ECamera.Align, MoveToTemplateMark);
            }
            else
            {
                _isFineAlignment = false;
                Action ActMoveToMark = value == 1 ? MoveToTemplateMark : null;
                if (_StageController.CurrentStageCameraConfig.CameraCoordId != ECamera.AlignExt.ToString())
                {
                    ChangeCamera(ECamera.AlignExt, ActMoveToMark);
                }
                else
                {
                    ActMoveToMark?.Invoke();
                }
            }
            DisplayAlignmentMarkPos();
        }

        /// <summary>
        /// 下一步，进入精对齐
        /// </summary>
        [RelayCommand]
        protected virtual void ToNextStep()
        {
            if (CoarseOrFineAlignmentSel < 2)
                CoarseOrFineAlignmentSel++;
        }

        /// <summary>
        /// 上一步，返回到粗对齐
        /// </summary>
        [RelayCommand]
        protected virtual void ToLastStep()
        {
            if (CoarseOrFineAlignmentSel > 0)
                CoarseOrFineAlignmentSel--;
        }


        /// <summary>
        /// Align && TrainField
        /// </summary>
        [RelayCommand]
        public async Task CoarseAlignTrain()
        {
            await ExecuteAlignment(false);
        }


        protected override void DisplayAlignmentMarkPos()
        {
            var vm = FineAlignTemplateViewModel;
            if (!_isFineAlignment)
            {
                vm = CoarseAlignTemplateViewModel;
            }
            AlignmentMarkPos = GetModelPosition(vm);
        }

        protected override void SaveModelInifos()
        {
            base.SaveModelInifos();
            AlignmentInfo.CoarseAlignModels = _coarseAlignTemplateContext.TemplateContexts;
        }

        protected override void SaveCameraParams()
        {
            if (_isFineAlignment)
            {
                SaveFineAlignCamaraParams();
            }
            else
            {
                SaveCoarseAlignCamaraParams();
            }
        }

        private void SaveCoarseAlignCamaraParams()
        {
            try
            {
                if (!_isFineAlignment)
                {
                    AlignmentInfo.CoarseAlignCameraParms = _cameraLightParamService.GetCamearaParam(ECamera.AlignExt) as AlignmentCameraParam;
                    AlignmentInfo.CoarseAlignPointLightParms = _cameraLightParamService.GetLightSourceParam(ECamera.AlignExt) as AlignmentPointLightParam;
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError(ex.Message);
            }
        }
    }
}
