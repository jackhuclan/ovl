using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPImageViewer.Core;
using InteVega.CBB.Common.Algorithm.Shapes;
using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.Recipe;
using InteVega.CBB.Recipe.Interface;
using InteVega.CBB.TemplateMatching.Core;
using InteVega.CBB.TemplateMatchingModule.ViewModels;
using Marvel.Application.Extentions;
using Marvel.Application.Services;
using Marvel.DataModels.Core;
using Marvel.DataModels.DeviceParameter;
using Marvel.DataModels.Recipe;
using Marvel.WaferMapCommon;
using Megarobo.Ovl.Type;
using Microsoft.Extensions.DependencyInjection;
using Ovl.UI.Controls;
using Prism.Services.Dialogs;

using DataModels = Marvel.DataModels;

namespace Ovl.UI.Recipe.ViewModel.OMVersion;

public partial class AlignmentVmBase
{
    /// <summary>
    /// 获取或设置晶圆图导航控件
    /// </summary>
    public WaferMapStageControl WaferMapStageControl { get; set; }

    /// <summary>
    /// 精对准模板匹配信息
    /// </summary>
    MultiTemplateMatchingContext _fineAlignTemplateContext;

    /// <summary>
    /// 精对准模板匹配ViewModel
    /// </summary>
    [ObservableProperty]
    MultiTemplateMatchingViewModel _FineAlignTemplateViewModel;

    protected ITemplateMatchingService _templateMatchingService;

    public IHPImageViewer HpImageViewer { get; set; }

    /// <summary>
    /// 是否是精对准
    /// </summary>
    protected bool _isFineAlignment = true;
    [RelayCommand]
    protected virtual void Loaded()
    {
        try
        {
            StageExpanderState = true;
            LogHelper.LogInfo("Alignment View Loaded, 加载模板及参数");
            string templatesFolder = Path.Combine(_serviceProvider.GetService<IRecipeManager>().GetRecipePath(_recipeElementsInfo.Id), RecipeElementsInfo.ALIGN);
            _templateMatchingService.TemplatesFolder = templatesFolder;
            if (AlignmentInfo.LLCModels != null)
            {
                _fineAlignTemplateContext.TemplateContexts = AlignmentInfo.LLCModels;
            }
            else
            {
                AlignmentInfo.LLCModels = _fineAlignTemplateContext.TemplateContexts;
                AlignmentInfo.LLCModels[0].Name = "Align_Model";
            }

            FineAlignTemplateViewModel = _serviceProvider.CreateTemplateViewModel(_fineAlignTemplateContext, HpImageViewer) as MultiTemplateMatchingViewModel;
            FineAlignTemplateViewModel.UserModified += TemplateViewModel_UserModified;
            initDisplayAlignInfo();
            GenWaferEdgeThreePointRandom();
            //MoveToCenter();
        }
        catch (Exception ex)
        {
            LogHelper.LogError(ex, "AlignmentViewModel Loaded");
        }
    }

    [RelayCommand]
    void Unloaded()
    {
        AssistantBase.ActionErrored = null;
        _alignmentService.Stop();
        LogHelper.LogInfo("Alignment View unloaded!");
    }

    int curModelSelectTab;
    [RelayCommand]
    void GoModelToAcsPos()
    {
        DisplayAlignmentMarkPos();
        Point2d targetpos = AlignmentMarkPos;
        ButtonResult buttonResult = _messageBoxService.AskDialog($"Whether to go to the machine position corresponding to the current template X:{targetpos.X:F4}Y:{targetpos.Y:F4}?");//是否走当前模板对应的机台位置
        if (buttonResult == ButtonResult.OK)
        {
            _alignmentService.MoveTo(targetpos);
        }
        return;
    }


    protected void TemplateViewModel_UserModified(object sender, ITemplateMatchingView.UserModifyArgs e)
    {

        if (e.UserModifyType == ITemplateMatchingView.UserModifyType.ModelCreated || e.UserModifyType == ITemplateMatchingView.UserModifyType.ModelTested)
        {
            var templateModels = _isFineAlignment ? AlignmentInfo.LLCModels : AlignmentInfo.CoarseAlignModels;

            string err = string.Empty;
            if (e.IsSuccess)
            {
                var model = e.Context;
                var imageData = HpImageViewer.Image as ImageData;
                if (imageData == null)
                    return;
                var point = _alignmentService.ConvertImageToAxis(_alignmentService.GetCurrentAxisPosition(), model.MatchingItems[0], imageData.Width, imageData.Height);
                model.MarkPosition = point - _alignmentService.GetAbsoluteCow();
                //如果不是主模板，需要计算其与主模板直接的偏移量
                if (model != templateModels[0])
                {
                    var mainModelR2 = _isFineAlignment ? AlignmentInfo.R2 : AlignmentInfo.CoarseR2;
                    //将WCS坐标转换为Field坐标
                    mainModelR2 = _alignmentService.GetRelativeR2(mainModelR2);
                    //此处为左下角模板，获取到的相对于Field的位置就是相对于主模板的位置

                    if (!_alignmentService.IsFinishAlignment || mainModelR2 == null)
                    {
                        err = "An alignment operation must be performed before training alternate templates.Otherwise, the results of the calculation may not be accurate.";
                    }
                    else
                    {
                        var wcsPoint = _alignmentService.GetFieldR3(point)?.R3;
                        if (wcsPoint != null)
                            model.R4 = (wcsPoint - mainModelR2).ToPoint2d();
                    }
                }
                DisplayAlignmentMarkPos();
                SaveResult(true, false,false );
            }
            bool isSuccess = e.IsSuccess && string.IsNullOrWhiteSpace(err);
            if (e.UserModifyType == ITemplateMatchingView.UserModifyType.ModelCreated)
            {
                if (isSuccess)
                {
                    _messageBoxService.Success("The template is created successfully.", "RecipeMessage");
                }
                else
                {
                    _messageBoxService.Error($"Failed to create a template.{err}", "RecipeMessage");
                }
            }
            else if (e.UserModifyType == ITemplateMatchingView.UserModifyType.ModelTested)
            {
                if (isSuccess)
                {
                    _messageBoxService.Success("The template test pass.", "RecipeMessage");
                }
                else
                {
                    _messageBoxService.Error($"Failed to test the template.{err}", "RecipeMessage");
                }
            }
        }
        else if (e.UserModifyType == ITemplateMatchingView.UserModifyType.Removed)
        {

        }
    }

    private void RedrawWaferMap(WcsPoint llcR1)
    {
        var oldR1 = WaferInfo.R1;
        if (WaferInfo.IsFieldCenter)
        {
            WaferInfo.R1 = WaferInfo.ConvertToLlcR3(llcR1, DataModels.Enums.EnumR3OrigionPoint.COF);
        }
        else
        {
            WaferInfo.R1 = llcR1;
        }
        var r1Offset = WaferInfo.R1 - oldR1;
        //var oldR2 = AlignmentInfo.R2;
        //if (oldR2 != null)
        //{
        //    AlignmentInfo.R2 = oldR2 - r1Offset;
        //}
        //if (AlignmentInfo.CoarseR2 != null)
        //{
        //    AlignmentInfo.CoarseR2 = AlignmentInfo.CoarseR2 - r1Offset;
        //}
        LogHelper.LogInfo($"LLC Updated , renew the r3 of tests,R1Offset={r1Offset},oldR1={oldR1},newR1={WaferInfo.R1},newR2={AlignmentInfo.R2},newCoarseR2={AlignmentInfo.CoarseR2}");

        AppHelper.DispatcherInvoke(() =>
        {
            if (!WaferInfo.IsShiftedFields)
            {
                var wmc = new WaferMapControl();
                wmc.RedrawByLlc(WaferInfo);
                WaferInfo.FieldInfos=wmc.GetFields();
            }
            else
            {

            }
            WaferMapStageControl?.Init(_alignmentService, WaferInfo);
            SetWaferMapField(_recipeElementsInfo);

        });

        //取消更新R3值
        ////没有可用的目标点时，则不提示该对话框
        //if (RecipeElementsInfo.GetTestPointCount() == 0)
        //    return;
        //var res = _messageBoxService.AskDialog("The LLC has been modified, Do you want to modify the R3 value of the target points synchronously?");
        //if (res != ButtonResult.OK)
        //{
        //    return;
        //}
        ////此处不需要判断IsEnable属性，不管是可用还是不可用的，当LLC发生改变时，所有的点都需要做平移
        ////否则，原来不可用的test改成可用之后，会找不到目标点
        //foreach (var test in _recipeElementsInfo.Tests)
        //{
        //    if (test.TargetSetting.TargetPoints2.Count == 0)
        //        continue;
        //    var target0 = test.TargetSetting.TargetPoints2.FirstOrDefault();
        //    var oldR3_Target0 = target0.R3Vector;
        //    var newR3_Target0 = oldR3_Target0 - r1Offset;

        //    //修改第一个点的XY坐标值，保证第一个点在Field的内部
        //    newR3_Target0 = WaferInfo.ConvertToLlcR3(newR3_Target0, target0.OrgionPoint);
        //    var x = newR3_Target0.X;
        //    if (Math.Abs(x) > WaferInfo.FieldWidth)
        //    {
        //        x %= WaferInfo.FieldWidth;
        //    }
        //    if (x < 0) x += WaferInfo.FieldWidth;
        //    var y = newR3_Target0.Y;
        //    if (Math.Abs(y) > WaferInfo.FieldHeight)
        //    {
        //        y %= WaferInfo.FieldHeight;
        //    }
        //    if (y < 0) y += WaferInfo.FieldHeight;
        //    newR3_Target0 = WaferInfo.ConvertToOrigionR3(target0.OrgionPoint, new WcsPoint(x, y), DataModels.Enums.EnumR3OrigionPoint.LLC);
        //    foreach (var target in test.TargetSetting.TargetPoints2)
        //    {
        //        var oldR3 = target.R3Vector;
        //        var targetOffset = target.R3Vector - oldR3_Target0;
        //        target.R3Vector = newR3_Target0 + targetOffset;
        //        LogHelper.LogInfo($"Update r3, Test={test.Name},OldR3={oldR3},NewR3={target.R3Vector}");
        //    }
        //}
    }




    protected virtual void SaveModelInifos()
    {
        AlignmentInfo.LLCModels = _fineAlignTemplateContext.TemplateContexts;
    }

    protected void SaveFineAlignCamaraParams()
    {
        try
        {
            if (_isFineAlignment)
            {
                AlignmentInfo.AlignCameraParms = _cameraLightParamService.GetCamearaParam(ECamera.Align) as AlignmentCameraParam;
                AlignmentInfo.AlignPointLightParms = _cameraLightParamService.GetLightSourceParam(ECamera.Align) as AlignmentPointLightParam;
            }
        }
        catch (Exception ex)
        {
            LogHelper.LogError(ex.Message);
        }
    }

    /// <summary>
    /// 保存当前使用的相机参数到配方文件中
    /// </summary>
    protected virtual void SaveCameraParams()
    {

    }


    /// <summary>
    /// 退出
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        // modelMatchingWindow.Close();
    }

    [ObservableProperty]
    Point2d _AlignmentMarkPos;
    protected virtual void DisplayAlignmentMarkPos()
    {
        AlignmentMarkPos = GetModelPosition(FineAlignTemplateViewModel);
    }

    /// <summary>
    /// 获取模板的机台坐标
    /// </summary>
    /// <param name="templateViewModel"></param>
    /// <returns></returns>
    protected Point2d GetModelPosition(MultiTemplateMatchingViewModel templateViewModel)
    {
        // 代表没有获取圆心
        if (!IsFindEdge()) return Point2d.Zero;

        // 代表没有生成模板控件
        if (!IsCreateMarkControl(templateViewModel)) return Point2d.Zero;

        // 代表有模板控件但是没创建模板
        if (!IsCreateMarkModel(templateViewModel)) return Point2d.Zero;

        // 代表创建模板且获取圆心
        Point2d targetpos = templateViewModel.SelectedSingleTemplateMatchingViewModel.Context.MarkPosition + _alignmentService.GetAbsoluteCow();
        return targetpos;
    }

    /// <summary>
    /// 代表没有获取圆心
    /// </summary>
    /// <returns></returns>
    private bool IsFindEdge() => !AbsoluteCow?.Equals(Point2d.Zero) ?? false;

    /// <summary>
    /// 是否创建模板控件
    /// </summary>
    /// <param name="templateViewModel"></param>
    /// <returns></returns>
    private bool IsCreateMarkControl(MultiTemplateMatchingViewModel templateViewModel) => templateViewModel?.SelectedSingleTemplateMatchingViewModel?.Context != null;

    /// <summary>
    /// 是否创建mark模板
    /// </summary>
    /// <param name="templateViewModel"></param>
    /// <param name="zero"></param>
    /// <returns></returns>
    private bool IsCreateMarkModel(MultiTemplateMatchingViewModel templateViewModel) => !templateViewModel.SelectedSingleTemplateMatchingViewModel.Context.MarkPosition.Equals(Point2d.Zero);

}
