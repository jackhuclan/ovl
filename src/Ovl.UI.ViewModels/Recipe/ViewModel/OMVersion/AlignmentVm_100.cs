using CommunityToolkit.Mvvm.Input;
using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.Prism.UI.Extensions.MessageBox;
using InteVega.CBB.Recipe.Core;
using InteVega.CBB.Recipe.Interface;
using Marvel.DataModels.Recipe;
using Megarobo.Ovl.Type;
using Prism.Services.Dialogs;
using System;

namespace Ovl.UI.ViewModels.Recipe.ViewModel.OMVersion;

public partial class AlignmentVm_100 : AlignmentVmBase
{
    ~AlignmentVm_100()
    {
    }
    public AlignmentVm_100(IDialogService dialogService,
        IServiceProvider sp,
        IMessageBoxService messageBoxService)
        : base(dialogService, sp, messageBoxService)
    {
    }

    public override void RequestData(IRecipe data, RecipeElementsInfo recipe, RecipeInfo recipeInfo)
    {
        base.RequestData(data, recipe, recipeInfo);
        ChangeCamera(_coarseAlignCamera);
    }

    protected override void UpdataStageCameraParamAndWriteToCamera(ECamera camera)
    {
        LogHelper.LogInfo("open algnment view, loading alignment camera params...");

        if (camera == ECamera.Align)
        {
            _cameraLightParamService.SetCamearaParam(camera, AlignmentInfo.AlignCameraParms);
            _cameraLightParamService.SetLightSourceParam(camera, AlignmentInfo.AlignPointLightParms);
        }
    }

    /// <summary>
    /// 下一步，进入精对齐
    /// </summary>
    [RelayCommand]
    protected virtual void ToNextStep()
    {
        CoarseOrFineAlignmentSel = 1;
        MoveToTemplateMark();
    }

    /// <summary>
    /// 上一步，返回到粗对齐
    /// </summary>
    [RelayCommand]
    protected virtual void ToLastStep()
    {
        CoarseOrFineAlignmentSel = 0;
    }

    protected override void SaveCameraParams()
    {
        SaveFineAlignCamaraParams();
    }
}
