using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InteVega.CBB.Common.Algorithm.Shapes;
using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.CommonLib.Helpers.Reflection;
using InteVega.CBB.Prism.UI.Extensions.Dialogs;
using Marvel.DataModels.Recipe;
using Marvel.TargetSelectionControl.Control;
using Prism.Events;
using Prism.Services.Dialogs;
using WinCopies.Util;

using DataModels = Marvel.DataModels;

namespace Ovl.UI.Recipe.ViewModel;

public partial class Test_TargetViewModel : Test_SettingsViewModelBase
{
    [ObservableProperty]
    TargetLocation selectTarget;
    [ObservableProperty]
    TargetSettingControl targetSettingView;
    [ObservableProperty]
    bool isEnable=false;
    //Point2d selectTarget;
    partial void OnSelectTargetChanged(TargetLocation value)
    {
        TargetSettingView.MarkSelectedTargets(value);
    }
    public void SelectTargetInUi(TargetLocation value)
    {
        OnSelectTargetChanged(value);
    }
    public Action TargetsChanged;
    private void OnTargetsChanged()
    {
        Applay();
        TargetsChanged?.Invoke();
        Save();
    }
    /// <summary>
    /// 添加Target
    /// </summary>
    [RelayCommand]
    void AddTarget()
    {
        if (_testInfo == null) return;
        //TestInfo.TargetPoints.Add(
        //    new Point2d()
        //    {
        //        X = 10,
        //        Y = 10
        //    });
        var points = _testInfo.TargetSetting.TargetPoints2;
        points.Add(
         new TargetLocation()
         {
             OrgionPoint = DataModels.Enums.EnumR3OrigionPoint.LLC,
             Id = IdentityHelper.NewGuid(points, x => x.Id)
         });

        Save();
    }


    /// <summary>
    /// 添加Target数组
    /// </summary>
    [RelayCommand]
    void AddArrayTarget()
    {
        if (_testInfo == null) return;
        AddArrayTargetViewModel viewModel = new AddArrayTargetViewModel();
        viewModel.Title = "Add Array Target";
        viewModel.Test = _testInfo;
        viewModel.ArrayTarget = new TargetArrayLocation();
        viewModel.FunSave = () =>
        {
            try
            {
                if (viewModel.ArrayTarget.ReferenceTarget == null)
                {
                    _messageBox.ErrorDialog("No reference point is selected, please right-click to select reference point");// ("未选择参考点，请右键点击选择参考点");
                    return false;
                }
                if (viewModel.ArrayTarget.SelectedTargets.Count == 0)
                {
                    var res = _messageBox.AskDialog("If the target point required for measurement is not selected, the measurement task will not be performed on the target point group. Is it confirmed to submit?", "Prompt");// ("未选择需要量测的目标点，将不会对该目标点数组执行量测任务，确定提交吗？", "Prompt");
                    if (res != ButtonResult.OK)
                    {
                        return false;
                    }
                }
                _testInfo.TargetSetting.TargetPoints2.Add(viewModel.ArrayTarget);
                OnTargetsChanged();
            }
            catch (Exception ex)
            {
                _messageBox.ErrorDialog(ex.Message);
                return false;
            }

            return true;
        };
        _dialogService.ShowDialog("AddArrayTargetView", viewModel);
    }

    [RelayCommand]
    public void EditArray()
    {
        if (SelectTarget == null || !SelectTarget.IsArray) return;
        AddArrayTargetViewModel viewModel = new AddArrayTargetViewModel();
        viewModel.Title = "Edit Array Target";
        viewModel.Test = _testInfo;
        viewModel.ArrayTarget = CloneHelper.ReflectionClone(SelectTarget) as TargetArrayLocation;
        viewModel.FunSave = () =>
        {
            try
            {
                if (viewModel.ArrayTarget.ReferenceTarget == null)
                {
                    _messageBox.ErrorDialog("No reference point is selected, please right-click to select reference point");// ("未选择参考点，请右键点击选择参考点");
                    return false;
                }
                if (viewModel.ArrayTarget.SelectedTargets.Count == 0)
                {
                    var res = _messageBox.AskDialog("If the target point required for measurement is not selected, the measurement task will not be performed on the target point group. Is it confirmed to submit?", "Prompt");
                    if (res != ButtonResult.OK)
                    {
                        return false;
                    }
                }
                CloneHelper.Copy(viewModel.ArrayTarget, SelectTarget);
            }
            catch (Exception ex)
            {
                _messageBox.ErrorDialog(ex.Message);
                return false;
            }
            return true;
        };
        _dialogService.ShowDialog("AddArrayTargetView", viewModel);
    }

    /// <summary>
    /// 清除所有target
    /// </summary>
    [RelayCommand]
    void RemoveTarget()
    {
        ButtonResult buttonResult = _messageBox.YesNoCancelDialog($"Are you sure you want to clear all targets?", "Clear prompt");
        if (buttonResult == ButtonResult.Yes)
        {
            _testInfo.TargetSetting.TargetPoints2.Clear();
            TargetsPostionCanvas.Children.Clear();
        }
    }

    [RelayCommand]
    void TargetSetting()
    {
        OpenTargetSettingWindow();
    }

    [RelayCommand]
    void CopyTarget()
    {
        if (SelectTarget == null) return;
        var copyTarget = (TargetLocation)CloneHelper.ReflectionClone(SelectTarget);
        _testInfo.TargetSetting.TargetPoints2.Add(copyTarget);
        OnTargetsChanged();
    }
    [RelayCommand]
    void DeleteTraget()
    {
        if (SelectTarget == null) return;
        if (SelectTarget == TestInfo.TargetSetting.EnabledTargetPoints.FirstOrDefault() && TestInfo.TrainSetting.PositioningTemplateInfo.IsAvailable && !TestInfo.TrainSetting.PositioningTemplateInfo.IsForEachTarget)
        {
            var res = _messageBox.AskDialog("Deleting the first available target point requires re-creating the positioning template. Are you sure you want to delete it?");
            if (res != ButtonResult.OK)
            {
                return;
            }
            else
            {
                _templateMatchingService.ClearAllModelsAsync(TestInfo.TrainSetting.PositioningTemplateInfo.Template);
            }
        }
        TestInfo.TargetSetting.TargetPoints2.Remove(SelectTarget);
        OnTargetsChanged();
    }

    [RelayCommand]
    void EnableTraget()
    {
        if (SelectTarget == null) return;
        var target = TestInfo.TargetSetting.TargetPoints2.FirstOrDefault(t => t.Id.Equals(SelectTarget.Id));
        bool isNeedReAdd = false;
        if (TestInfo.TrainSetting.PositioningTemplateInfo.IsAvailable && !TestInfo.TrainSetting.PositioningTemplateInfo.IsForEachTarget)
        {
            int index = TestInfo.TargetSetting.TargetPoints2.IndexOf(target);
            var firstEnableTarget = TestInfo.TargetSetting.EnabledTargetPoints.FirstOrDefault();
            if (firstEnableTarget != null)
            {
                int index0 = TestInfo.TargetSetting.TargetPoints2.IndexOf(firstEnableTarget);
                if (index < index0)
                {
                    isNeedReAdd = true;
                }
            }
        }
        target.IsEnable = true;
        if (isNeedReAdd)
        {
            TestInfo.TargetSetting.TargetPoints2.Remove(target);
            TestInfo.TargetSetting.TargetPoints2.Add(target);
        }
        OnTargetsChanged();
    }
    [RelayCommand]
    void DisableTraget()
    {
        if (SelectTarget == null) return;
        if (SelectTarget == TestInfo.TargetSetting.EnabledTargetPoints.FirstOrDefault() && TestInfo.TrainSetting.PositioningTemplateInfo.IsAvailable && !TestInfo.TrainSetting.PositioningTemplateInfo.IsForEachTarget)
        {
            var res = _messageBox.AskDialog("Disabling the first available target point requires re-creating the positioning template. Are you sure you want to delete it?");
            if (res != ButtonResult.OK)
            {
                return;
            }
            else
            {
                _templateMatchingService.ClearAllModelsAsync(TestInfo.TrainSetting.PositioningTemplateInfo.Template);
            }
        }
        _testInfo.TargetSetting.TargetPoints2.FirstOrDefault(t => t.Id.Equals(SelectTarget.Id)).IsEnable = false;
        OnTargetsChanged();
    }

    [RelayCommand]
    public void Applay()
    {
        TargetsPostionCanvas = new Canvas();
        List<TargetLocation> points = new List<TargetLocation>();
        if (_testInfo == null || _testInfo.TargetSetting.TargetPoints2 == null)
            return;
        LogHelper.LogInfo($"Draw test targets:TestName={_testInfo.Name}");
        foreach (var target in TestInfo.TargetSetting.EnabledTargetPoints)
        {
            points.Add(target);
        }
        //此处需要考虑坐标方向
        TargetSettingView?.Refresh(points, WaferMapInfo.CoordFieldWidth, WaferMapInfo.CoordFieldHeight);
    }
}
