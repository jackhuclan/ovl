using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPImageViewer.Core.Persistence;
using InteVega.CBB.Common;
using InteVega.CBB.Common.Algorithm.Shapes;
using InteVega.CBB.CommonLib.Helpers;
using Marvel.Application.Services;
using Marvel.DataModels.Recipe;
using Megarobo.Ovl.Service.IntfFn.fn;
using Megarobo.Ovl.Type;
using Wafer.Algorithm.Common.DoNet;

namespace Ovl.UI.Recipe.ViewModel.OMVersion;

public partial class AlignmentVmBase
{
    /// <summary>
    /// 预定义的点1，x
    /// </summary>
    [ObservableProperty]
    private double pre_Define_CircelPoint1_X;
    /// <summary>
    /// 预定义的点2，x
    /// </summary>
    [ObservableProperty]
    private double pre_Define_CircelPoint2_X;
    /// <summary>
    /// 预定义的点3，x
    /// </summary>
    [ObservableProperty]
    private double pre_Define_CircelPoint3_X;
    /// <summary>
    /// 预定义的点1，y
    /// </summary>
    [ObservableProperty]
    private double pre_Define_CircelPoint1_Y;
    /// <summary>
    /// 预定义的点2，y
    /// </summary>
    [ObservableProperty]
    private double pre_Define_CircelPoint2_Y;
    /// <summary>
    /// 预定义的点3，y
    /// </summary>
    [ObservableProperty]
    private double pre_Define_CircelPoint3_Y;

    [ObservableProperty]
    private bool ifManualCal = false;

    [ObservableProperty]
    private Visibility isUseNotchVisible = Visibility.Visible;
    partial void OnIfManualCalChanged(bool oldValue, bool newValue)
    {
        //if (newValue == false)
        //    MoveToCenter();
    }



    Dictionary<int, List<Point2d>> ManualedgePoint2Ds = new Dictionary<int, List<Point2d>>();

    int lastEdgeIndex = -1;
    public IEnumerable<TransitionType> DetectEdgeTransitionEnum { get; } = Enum.GetValues(typeof(TransitionType)).Cast<TransitionType>();
    private void GetWaferEdge(object pointIndex, double defineX, double defineY)
    {
        Task.Run(() =>
        {
            AssistantBase.ActionErrored = null;
            if (pointIndex == null) return;
            int.TryParse(pointIndex.ToString(), out int index);
            try
            {
                IsEnableClick = false;
                Point2d pre_DefineP0 = new Point2d() { X = defineX, Y = defineY };
                if (index == lastEdgeIndex)//旋转中心未标定，使用当前位置手动边缘拟合
                {
                    pre_DefineP0 = _alignmentService.GetCurrentAxisPosition();
                }
                lastEdgeIndex = index;
                var result = _alignmentService.GetEdgePoints(pre_DefineP0, index);
                List<Point2d> edgePoint2Ds0 = result.EdgePoints;
                Point2d samplePoint = result.SamplePixelPoint;
                AppHelper.DispatcherInvoke(() =>
                {
                    HpImageViewer.UpdateROIs();
                    HpImageViewer.AddROIs(new ROIDesc[] { new CrosshairsDesc() { CenterX = samplePoint.X, CenterY = samplePoint.Y, Size = 15, Angle = 0, StrokeThickness = 2, Color = "#00FF00" } });
                });
                ManualedgePoint2Ds[index] = edgePoint2Ds0;
                var no = index + 1;
#if DEBUG
                foreach (var point2 in edgePoint2Ds0)
                {
                    Console.WriteLine($"获取到第{no}组点:{point2}");
                }
#endif
                AppHelper.DispatcherInvoke(() =>
                {
                    if (edgePoint2Ds0 != null)
                    {
                        switch (lastEdgeIndex)
                        {
                            case 0:
                                this.Pre_Define_CircelPoint1_X = pre_DefineP0.X;
                                this.Pre_Define_CircelPoint1_Y = pre_DefineP0.Y;
                                break;
                            case 1:
                                this.Pre_Define_CircelPoint2_X = pre_DefineP0.X;
                                this.Pre_Define_CircelPoint2_Y = pre_DefineP0.Y;
                                break;
                            case 2:
                                this.Pre_Define_CircelPoint3_X = pre_DefineP0.X;
                                this.Pre_Define_CircelPoint3_Y = pre_DefineP0.Y;
                                break;
                            default:
                                break;
                        }
                        _messageBoxService.Success($"Succeeded in obtaining the edge point of group {no}", "RecipeMessage");
                    }
                    else
                    {
                        _messageBoxService.Error($"Failed to obtain the edge point of group {no}", "RecipeMessage");
                    }
                });
            }
            catch (Exception ex)
            {
                LogHelper.LogError(ex, nameof(GetWaferEdge));
                if (ex is OperationCanceledException)
                {
                    _messageBoxService.Error($"Get edge point thread terminated", "RecipeMessage");
                }
                else
                {
                    _messageBoxService.Error($"Failed to obtain edge points", "RecipeMessage");
                }
            }
            finally
            {
                IsEnableClick = true;
                _StageController.StartGrabbing();
            }
        });
    }

    [RelayCommand]
    private void ResetEdgePoints()
    {
        _alignmentService.SaveCowOffset(_alignmentService.RotateCenter, true);
        GenWaferEdgeThreePointRandom();
        //DebuggerService.DebugCow(_serviceProvider.GetService<IWaferCenteringService>(), AlignmentInfo.EdgeParams);
    }

    /// <summary>
    /// 手动获取圆上3点
    /// </summary>
    /// <param name="pointIndex">获取哪个点的坐标</param>
    [RelayCommand(IncludeCancelCommand = true)]
    private async Task GetWaferEdgePoint(object pointIndex, CancellationToken token)
    {
        GenWaferEdgeThreePointRandom();
        HpImageViewer.UpdateROIs();
        switch (pointIndex)
        {
            case string index when index == "0":
                GetWaferEdge(pointIndex, Pre_Define_CircelPoint1_X, Pre_Define_CircelPoint1_Y);
                break;
            case string index when index == "1":
                GetWaferEdge(pointIndex, Pre_Define_CircelPoint2_X, Pre_Define_CircelPoint2_Y);
                break;
            case string index when index == "2":
                GetWaferEdge(pointIndex, Pre_Define_CircelPoint3_X, Pre_Define_CircelPoint3_Y);
                break;
            default:
                break;
        }
    }
    /// <summary>
    /// 根据标定的alignment对齐相机中心或当前recipe中保存的拟合的圆心计算预设3点，默认为前一种
    /// </summary>
    /// <param name="ifuseCurRecipeExitWaferCenter"></param>
    private void GenWaferEdgeThreePointRandom()
    {

        if (_alignmentService.RotateCenter == null || _recipeElementsInfo.WaferMap.Diameter == 0)
        {
            Console.WriteLine("alignment中心还没标定,无法自动计算边缘预定义拍照点位");
            LogHelper.LogInfo("alignment中心还没标定,无法自动计算边缘预定义拍照点位");
            return;
        }
        var p1 = _alignmentService.GetEdgeTeachingPoint(0);
        var p2 = _alignmentService.GetEdgeTeachingPoint(1);
        var p3 = _alignmentService.GetEdgeTeachingPoint(2);
        this.Pre_Define_CircelPoint1_X = p1.X;
        this.Pre_Define_CircelPoint1_Y = p1.Y;
        this.Pre_Define_CircelPoint2_X = p2.X;
        this.Pre_Define_CircelPoint2_Y = p2.Y;
        this.Pre_Define_CircelPoint3_X = p3.X;
        this.Pre_Define_CircelPoint3_Y = p3.Y;
    }

    /// <summary>
    /// 3点找圆异常报警处理后继续执行 Continue
    /// </summary>
    [RelayCommand]
    private void ContinueExecute3PointWaferCenterThread()
    {
        ContinueExecute();
    }


    /// <summary>
    /// 3点找圆异常报警处理，退出线程 Stop
    /// </summary>
    [RelayCommand]
    private void ExitExecute3PointWaferCenterThread()
    {
        _alignmentService.Stop();
        AssistantBase.Skip();
    }

    bool isFirstAuto = true;
    /// <summary>
    /// 计算wafer中心 Caculate
    /// </summary>
    [RelayCommand]
    private async Task CaculateWaferCenter(CancellationToken token)
    {
        HpImageViewer.UpdateROIs();
        IsEnableClick = false;
        _alignmentService.Reset();
        //
        //
        await Task.Run((Func<Task>)(async () =>
        {
            try
            {

                if (ifManualCal)
                {
                    //手动过程计算
                    List<Point2d> combinedList = ManualedgePoint2Ds.Values.SelectMany(list => list).ToList();
                    Point2d waferCenter = _alignmentService.GetWaferCenter(combinedList);
                    _alignmentService.NotchAlign(false, true);
                    AbsoluteCow = waferCenter;
                    _alignmentService.MoveTo(AbsoluteCow);
                    AppHelper.DispatcherInvoke(() =>
                    {
                        SaveResult();
                    });
                }
                else
                {

                    AssistantBase.ActionErrored = AlignmentHandleError;
                    _alignmentService.Init(RecipeElementsInfo);
                    //自动过程计算
                    //执行一次自动聚焦
                    ChangeCameraInfo = "Focusing ...";
                    //// 移动到聚焦位置
                    //if (AlignmentInfo.FocusPosition != null)
                    //{
                    //    _alignmentService.MoveTo(_alignmentService.GetCoarseFocusPosition(), "Last focus position");
                    //}
                    MoveToCenter();


                    ChangeCameraInfo = "Auto getting center of wafer ...";
                    //判断是否已停止
                    if (_alignmentService.IsCancelled)
                        return;
                    _ = AutoGetWaferCenter();
                }
                GenWaferEdgeThreePointRandom();
            }
            catch (Exception ex)
            {
                AppHelper.DispatcherInvoke(() =>
                {
                    _messageBoxService.ErrorDialog($"Failed to Finding a circle with 3 points:{ex.Message}", "Find circle center");
                });
                LogHelper.LogError("执行3点找圆报错" + ex);
            }
            finally
            {
                ChangeCameraInfo = "";
                IsEnableClick = true;
                IsErrored = false;
                AssistantBase.ActionErrored = null;
                _StageController.StartGrabbing();
            }
        }));
    }

    /// <summary>
    /// Go to Focus Pos
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    public void ToFocusPos()
    {
        MoveToCenter();
        // _alignmentService.MoveTo(_alignmentService.GetCoarseFocusPosition(), "Coarse Focus Pos XY");
    }

    /// <summary>
    /// UpdateFocusPos
    /// </summary>
    [RelayCommand]
    public void UpdateFocusPos()
    {
        var res = _messageBoxService.AskDialog("Confirm updating Focus Pos ?");
        if (res != Prism.Services.Dialogs.ButtonResult.OK)
            return;
        try
        {
            _alignmentService.SetCoarseFocusPosition();
        }
        catch (Exception ex)
        {
            _messageBoxService.Error(ex.Message);
        }
    }

    /// <summary>
    /// UpdateFocusPos
    /// </summary>
    [RelayCommand]
    public async void TrainNotch()
    {
        HpImageViewer.UpdateROIs();
        await Task.Run(() =>
          {
              bool isSuccess = _alignmentService.NotchAlign(true, false);
              if (!isSuccess)
              {   //Notch拉平失败，请确保晶圆是有notch口，并且notch的方向设置是正确的
                  _messageBoxService.ErrorDialog("Failed to notch align. Please mark sure the notch is exist and the notch direction is correct");
              }

          });
    }

    private AutoRunInfo GetFocusInfo()
    {
        var coarseFocus = _StageController.GetSystemFocusInfo(IServiceCamera.CurCamera);
        coarseFocus.StartPosition = _alignmentService.GetCurrentZAlign();
        return coarseFocus;
    }

    public virtual void MoveToCenter()
    {
        if (AlignmentInfo == null)
            return;
        double z = AlignmentInfo.FocusHeight;
        if (z == 0)
            z = WaferInfo.Thickness;
        z += AlignmentService.MacroFocusZ;
        _alignmentService.MoveTo(_alignmentService.GetCoarseFocusPosition(), "Last focus position");
        _alignmentService.MoveZSpecTo(AxisType.Z, z, false);
    }

    /// <summary>
    /// 自动寻找圆心（AI 工具 calculate_wafer_center 的执行体）。
    /// 返回真实的执行结果，供 AI 工具作为事实来源；成功后会移动载台到圆心位置。
    /// </summary>
    private Task<string> AutoGetWaferCenter()
    {
        var isSuccessFull = _alignmentService.AutoGetWaferCenter((p) =>
         {
             AppHelper.DispatcherInvoke(() =>
             {
                 HpImageViewer.UpdateROIs();
                 HpImageViewer.AddROIs(new ROIDesc[] { new CrosshairsDesc() { CenterX = p.X, CenterY = p.Y, Size = 15, Angle = 0, StrokeThickness = 2, Color = "#00FF00" } });
             });
         }, true);

        var found = isSuccessFull && _alignmentService.Cow != null;
        AppHelper.DispatcherInvoke(() =>
        {
            HpImageViewer.UpdateROIs();
            if (!found)
                return;
            Point2d waferCenter = _alignmentService.Cow;
            _alignmentService.SaveCowOffset(waferCenter, true);
            AbsoluteCow = waferCenter;
            _alignmentService.MoveTo(AbsoluteCow);
            SaveResult();
        });

        return Task.FromResult(found
            ? "成功找到圆心，并已把载台移动到圆心位置"
            : "自动寻找圆心没有成功，请人工确认晶圆是否完整出现在相机视野中，必要时调整后再试");
    }
}
