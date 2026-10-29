using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPImageViewer.Core.Persistence;
using InteVega.CBB.Common.Algorithm.Shapes;
using InteVega.CBB.CommonLib.Helpers;
using Marvel.DataModels.Enums;
using Megarobo.Ovl.Type;
using OVL.Algorithm.DoNet;
using WinCopies.Collections.Generic;

namespace Ovl.UI.Recipe.ViewModel;

internal abstract partial class Test_Train40ViewModelBase
{
    /// <summary>
    /// CD OVL 边极性枚举的全部取值，避免每次 Loaded 时重复反射创建集合。
    /// </summary>
    private static readonly List<EdgePolarity> AllEdgePolarityDirections = Enum.GetValues(typeof(EdgePolarity))
                                                                           .Cast<EdgePolarity>()
                                                                           .ToList();

    /// <summary>
    /// CD OVL 边选择枚举的全部取值，避免每次 Loaded 时重复反射创建集合。
    /// </summary>
    private static readonly List<EdgeSelect> AllEdgeSelectDirections = Enum.GetValues(typeof(EdgeSelect))
                                                                       .Cast<EdgeSelect>()
                                                                       .ToList();

    /// <summary>内层 CD OVL 补偿十字的显示颜色。</summary>
    protected const string InnerCompCrossColor = "#FFFF00";

    /// <summary>外层 CD OVL 补偿十字的显示颜色。</summary>
    protected const string OuterCompCrossColor = "#00FFFF";

    /// <summary>标准 CD OVL 与 CrossFov（OverFov）均支持 EdgeSelect 边点补偿。</summary>
    protected virtual bool SupportsCdOvlEdgeCompensation() =>
        TestInfo?.OMParam?.AlgorithmParam is OVL.Algorithm.DoNet.CDOvlAlgorithmParam or OVL.Algorithm.DoNet.CDOvlOfOverFovAlgorithmParam;

    /// <summary>
    /// CD OVL 内层 L 型量测 ROI 两条边上的参考边点（像素，共 2 点） 配置后的
    /// <c>null</c> 或不足 2 点表示由算法自动找边；重画量测 ROI 后应置 <c>null</c>。
    /// </summary>
    public IEnumerable<Point2d> InneredgeConfirmRois { get; set; }

    /// <summary>
    /// CD OVL 外层 L 型量测 ROI 两条边上的参考边点（像素，共 2 点  配置后的
    /// </summary>
    public IEnumerable<Point2d> OuteredgeConfirmRois { get; set; }

    /// <summary>
    /// CD OVL 内层 L 型量测 ROI 两条边上的参考边点（像素，共 2 点）显示在画面上的位置，供用户微调用。
    /// <c>null</c> 或不足 2 点表示由算法自动找边；重画量测 ROI 后应置 <c>null</c>。
    /// </summary>
    public IEnumerable<Point2d> InneredgeShowRois { get; set; }

    /// <summary>
    /// CD OVL 外层 L 型量测 ROI 两条边上的参考边点（像素，共 2 点）显示在画面上的位置，供用户微调用。
    /// </summary>
    public IEnumerable<Point2d> OuteredgeShowRois { get; set; }

    /// <summary>
    /// 进入仅十字编辑态前保存当前 ROI，便于 Cancel/Confirm 恢复。
    /// DoubleFace 会在派生类里改成分别保存 inner/outer compare viewer 的快照。
    /// </summary>
    protected List<ROIDesc> CdOvlRoiSnapshotBeforeEdgeOnlyMode { get; set; }

    /// <summary>
    /// 是否处于 CD OVL 十字补偿编辑态：为 true 时十字可拖动；Confirm/Reset 后置 false。
    /// Run 完成后会自动置为 true，便于用户直接继续微调。
    /// </summary>
    [ObservableProperty]
    private bool _isCdOvlCrossEditMode = false;

    /// <summary>
    /// 控制界面上 EdgeSelect 按钮与 Confirm/Cancel/Clear 按钮组的切换。
    /// true 表示显示入口按钮，false 表示进入编辑态显示操作按钮组。
    /// </summary>
    [ObservableProperty]
    private bool _edgeSelect = true;

    [ObservableProperty]
    private ObservableCollection<EdgePolarity> _enumCDOVLPolarity;

    [ObservableProperty]
    private EdgePolarity _selectEdgePolarity;

    [ObservableProperty]
    private ObservableCollection<EdgeSelect> _enumCDOVLEdgeSelect;

    [ObservableProperty]
    private EdgeSelect _selectEdgeSelect;

    [ObservableProperty]
    private int _edgeFilterLevel;

    [ObservableProperty]
    private int _minEdgeContrast;

    /// <summary>
    /// 仅当存在至少 2 个像素点时返回前两点；否则返回 <c>null</c>，
    /// 与 <see cref="ROIRenderExtensions.CreateDimensionROI"/> 的「null 或恰好 2 点」约定一致。
    /// </summary>
    protected static IEnumerable<Point2d> NormalizeCdOvlEdgeEnumerableForKernel(IEnumerable<Point2d> edges)
    {
        if (edges == null)
        {
            return null;
        }

        IList<Point2d> list = edges as IList<Point2d> ?? edges.ToList();
        if (list.Count < 2)
        {
            return null;
        }

        return new List<Point2d> { list[0], list[1] };
    }

    /// <summary>是否已经拿到两条有效边点，供界面显示和防呆校验共用。</summary>
    protected static bool TryGetCdOvlTwoEdgePoints(IEnumerable<Point2d> edges, out Point2d p0, out Point2d p1)
    {
        p0 = default;
        p1 = default;
        if (edges == null)
        {
            return false;
        }

        IList<Point2d> list = edges as IList<Point2d> ?? edges.ToList();
        if (list.Count < 2)
        {
            return false;
        }

        p0 = list[0];
        p1 = list[1];
        return true;
    }

    /// <summary>
    /// CD OVL 当前界面是否需要显示内层补偿十字。
    /// 单面沿用原有逻辑；双抓时默认只显示当前 <see cref="FocusBasedOn"/> 对应层。
    /// </summary>
    protected virtual bool RequiresInnerCdOvlEdgeForDisplay()
    {
        return _grabMode != EnumGrabMode.DoubleGrab || FocusBasedOn == EnumFocusBasedOn.Inner;
    }

    /// <summary>
    /// CD OVL 当前界面是否需要显示外层补偿十字。
    /// 单面沿用原有逻辑；双抓时默认只显示当前 <see cref="FocusBasedOn"/> 对应层。
    /// </summary>
    protected virtual bool RequiresOuterCdOvlEdgeForDisplay()
    {
        return _grabMode != EnumGrabMode.DoubleGrab || FocusBasedOn == EnumFocusBasedOn.Outer;
    }

    /// <summary>
    /// EdgePolarity 改变后同步回算法参数，并重建量测 Kernel。
    /// </summary>
    partial void OnSelectEdgePolarityChanged(EdgePolarity value)
    {
        if ((CDOvlAlgorithmParam == null || CDOvlAlgorithmParam.EdgePolarity == value )&&(CDOvlOfOverFovAlgorithmParam == null || CDOvlOfOverFovAlgorithmParam.EdgePolarity == value))
        {
            return;
        }
        if (CDOvlAlgorithmParam != null)
        {
            CDOvlAlgorithmParam.EdgePolarity = value;
            QueueCreateMeasureKernelAsync();
        }
        else if(CDOvlOfOverFovAlgorithmParam != null)
        {
            CDOvlOfOverFovAlgorithmParam.EdgePolarity = value;
            QueueCreateMeasureKernelAsync();
        }
    }

    /// <summary>
    /// EdgeSelect 模式改变后同步回算法参数，并重建量测 Kernel。
    /// </summary>
    partial void OnSelectEdgeSelectChanged(EdgeSelect value)
    {
        if ((CDOvlAlgorithmParam == null || CDOvlAlgorithmParam.EdgeSelect == value) && (CDOvlOfOverFovAlgorithmParam == null || CDOvlOfOverFovAlgorithmParam.EdgeSelect == value))
        {
            return;
        }
        if (CDOvlAlgorithmParam != null)
        {
            CDOvlAlgorithmParam.EdgeSelect = value;
            QueueCreateMeasureKernelAsync();
        }
        else if (CDOvlOfOverFovAlgorithmParam != null)
        {
            CDOvlOfOverFovAlgorithmParam.EdgeSelect = value;
            QueueCreateMeasureKernelAsync();
        }

    }

    /// <summary>
    /// EdgeFilterLevel 限制在算法可接受范围内，并在更新后重建量测 Kernel。
    /// </summary>
    partial void OnEdgeFilterLevelChanged(int value)
    {
        if ((CDOvlAlgorithmParam == null || CDOvlAlgorithmParam.EdgeFilterLevel == value) && (CDOvlOfOverFovAlgorithmParam == null || CDOvlOfOverFovAlgorithmParam.EdgeFilterLevel == value))
        {
            return;
        }

        if (value < 0 || value > 3)
        {
            EdgeFilterLevel = 0;
            return;
        }
        if (CDOvlAlgorithmParam != null)
        {
            CDOvlAlgorithmParam.EdgeFilterLevel = value;
            QueueCreateMeasureKernelAsync();
        }
        else if (CDOvlOfOverFovAlgorithmParam != null)
        {
            CDOvlOfOverFovAlgorithmParam.EdgeFilterLevel = value;
            QueueCreateMeasureKernelAsync();
        }

    }

    /// <summary>
    /// MinEdgeContrast 限制在算法可接受范围内，并在更新后重建量测 Kernel。
    /// </summary>
    partial void OnMinEdgeContrastChanged(int value)
    {
        if ((CDOvlAlgorithmParam == null || CDOvlAlgorithmParam.MinEdgeContrast == value) && (CDOvlOfOverFovAlgorithmParam == null || CDOvlOfOverFovAlgorithmParam.MinEdgeContrast == value))
        {
            return;
        }

        if (value < 0 || value > 255)
        {
            MinEdgeContrast = 0;
            return;
        }
        if (CDOvlAlgorithmParam != null)
        {
            CDOvlAlgorithmParam.MinEdgeContrast = value;
            QueueCreateMeasureKernelAsync();
        }
        else if (CDOvlOfOverFovAlgorithmParam != null)
        {
            CDOvlOfOverFovAlgorithmParam.MinEdgeContrast = value;
            QueueCreateMeasureKernelAsync();
        }

    }

    /// <summary>
    /// 清空进入 EdgeSelect 编辑态前保存的 ROI 快照。
    /// DoubleFace 会覆写为分别清空 inner/outer 两份快照。
    /// </summary>
    protected virtual void ClearCdOvlRoiSnapshot()
    {
        CdOvlRoiSnapshotBeforeEdgeOnlyMode = null;
    }

    /// <summary>
    /// 进入仅十字编辑态前保存当前 ROI，供 Cancel/Confirm 恢复。
    /// 单面场景直接保存主 viewer 当前 ROI。
    /// </summary>
    protected virtual void CaptureCdOvlRoiSnapshotForRestore()
    {
        CdOvlRoiSnapshotBeforeEdgeOnlyMode = HpImageViewer?.ImageViewerDesc?.ROIDescs?.ToList() ?? new List<ROIDesc>();
    }

    /// <summary>
    /// 尝试恢复进入 EdgeSelect 编辑态前的 ROI 快照。
    /// 返回 true 表示已恢复快照，false 表示当前没有可恢复内容。
    /// </summary>
    protected virtual bool TryRestoreCdOvlSnapshotBeforeEdgeOnlyMode()
    {
        if (CdOvlRoiSnapshotBeforeEdgeOnlyMode == null || CdOvlRoiSnapshotBeforeEdgeOnlyMode.Count == 0)
        {
            return false;
        }

        HpImageViewer.UpdateROIs(CdOvlRoiSnapshotBeforeEdgeOnlyMode.ToArray());
        ClearCdOvlRoiSnapshot();
        return true;
    }

    /// <summary>
    /// 恢复完整 Overlay，并按当前边点重新叠加补偿十字。
    /// 单面直接恢复主 viewer；DoubleFace 会覆写到 inner/outer compare viewer。
    /// </summary>
    protected virtual void RestoreCdOvlFullOverlayAndCrosses(bool crossesMovable)
    {
        if (!SupportsCdOvlEdgeCompensation())
        {
            return;
        }

        if (CdOvlRoiSnapshotBeforeEdgeOnlyMode == null || CdOvlRoiSnapshotBeforeEdgeOnlyMode.Count == 0)
        {
            RefreshCdOvlCrosshair(crossesMovable);
            return;
        }

        var rois = CdOvlRoiSnapshotBeforeEdgeOnlyMode.Where(r => r is not CrosshairsDesc).ToList();
        AppendCdOvlCrosshair(rois, crossesMovable);
        HpImageViewer.UpdateROIs(rois.ToArray());
        ClearCdOvlRoiSnapshot();
    }

    /// <summary>
    /// 清空边点后重建当前 viewer 的量测 ROI 显示，不再叠加补偿十字。
    /// </summary>
    protected virtual void RebuildCdOvlViewerMeasureRoisOnly()
    {
        var list = new List<ROIDesc>();
        if (TrainParam.InnerLayer.MeasureROI != null)
        {
            list.Add(TrainParam.InnerLayer.MeasureROI);
        }

        if (TrainParam.OuterLayer.MeasureROI != null)
        {
            list.Add(TrainParam.OuterLayer.MeasureROI);
        }

        HpImageViewer.UpdateROIs(list.ToArray());
    }

    /// <summary>
    /// 从本次 Run 的测量结果中提取内外层边点，作为后续 EdgeSelect 补偿的初始可拖十字位置。
    /// </summary>
    protected void CaptureRawCdOvlResult(OvResultBase result)
    {
        if (!SupportsCdOvlEdgeCompensation()
            || result?.CenterInner == null || result.CenterOuter == null
            || result.EdgePointsInner?.MatchPoint == null || result.EdgePointsInner.MatchPoint.Count < 2
            || result.EdgePointsOuter?.MatchPoint == null || result.EdgePointsOuter.MatchPoint.Count < 2)
        {
            InneredgeConfirmRois = null;
            OuteredgeConfirmRois = null;
            return;
        }

        InneredgeShowRois = new List<Point2d> { result.EdgePointsInner.MatchPoint[0], result.EdgePointsInner.MatchPoint[1] };
        OuteredgeShowRois = new List<Point2d> { result.EdgePointsOuter.MatchPoint[0], result.EdgePointsOuter.MatchPoint[1] };
        IsCdOvlCrossEditMode = true;
        EdgeSelect = true;
    }

    /// <summary>
    /// 仅向 ROI 列表追加内层补偿十字。
    /// </summary>
    protected void AppendInnerCdOvlCrosshair(List<ROIDesc> rois, bool isMovable)
    {
        IEnumerable<Point2d> InneredgeRois = null;
        if (InneredgeConfirmRois == null )
        {
            InneredgeRois = InneredgeShowRois;
        }
        else
        {
            InneredgeRois = InneredgeConfirmRois;
        }
        if (TryGetCdOvlTwoEdgePoints(InneredgeRois, out var ix0, out var ix1))
        {
            rois.Add(new CrosshairsDesc(ix0.X, ix0.Y)
            {
                Angle = 0,
                Color = InnerCompCrossColor,
                Size = 10,
                IsMove = isMovable
            });
            rois.Add(new CrosshairsDesc(ix1.X, ix1.Y)
            {
                Angle = 180,
                Color = InnerCompCrossColor,
                Size = 10,
                IsMove = isMovable
            });
        }
    }

    /// <summary>
    /// 仅向 ROI 列表追加外层补偿十字。
    /// </summary>
    protected void AppendOuterCdOvlCrosshair(List<ROIDesc> rois, bool isMovable)
    {

        IEnumerable<Point2d> OuteredgeRois = null;
        if (OuteredgeConfirmRois == null)
        {
            OuteredgeRois = OuteredgeShowRois;
        }
        else
        {
            OuteredgeRois = OuteredgeConfirmRois;
        }
        if (TryGetCdOvlTwoEdgePoints(OuteredgeRois, out var ox0, out var ox1))
        {
            rois.Add(new CrosshairsDesc(ox0.X, ox0.Y)
            {
                Angle = 0,
                Color = OuterCompCrossColor,
                Size = 10,
                IsMove = isMovable
            });
            rois.Add(new CrosshairsDesc(ox1.X, ox1.Y)
            {
                Angle = 180,
                Color = OuterCompCrossColor,
                Size = 10,
                IsMove = isMovable
            });
        }
    }

    /// <summary>
    /// 按当前显示规则向 ROI 列表追加需要展示的补偿十字。
    /// 单面可能只追加当前层；DoubleFace 可覆写显示策略，让两层同时显示。
    /// </summary>
    protected void AppendCdOvlCrosshair(List<ROIDesc> rois, bool isMovable)
    {
        if (!SupportsCdOvlEdgeCompensation())
        {
            return;
        }
        IEnumerable<Point2d> InneredgeRois = null;
        IEnumerable<Point2d> OuteredgeRois = null;
        if (InneredgeConfirmRois == null || OuteredgeConfirmRois == null)
        {
            InneredgeRois = InneredgeShowRois;
            OuteredgeRois = OuteredgeShowRois;
        }
        else
        {
            InneredgeRois = InneredgeConfirmRois;
            OuteredgeRois = OuteredgeConfirmRois;
        }
        if (RequiresInnerCdOvlEdgeForDisplay()
            && TryGetCdOvlTwoEdgePoints(InneredgeRois, out _, out _))
        {
            AppendInnerCdOvlCrosshair(rois, isMovable);
        }

        if (RequiresOuterCdOvlEdgeForDisplay()
            && TryGetCdOvlTwoEdgePoints(OuteredgeRois, out _, out _))
        {
            AppendOuterCdOvlCrosshair(rois, isMovable);
        }
    }

    /// <summary>
    /// 将当前 viewer 切到“仅十字”编辑态，只显示补偿十字，不显示其余 ROI。
    /// </summary>
    protected virtual void RefreshCdOvlCrosshair(bool isMovable)
    {
        if (!SupportsCdOvlEdgeCompensation())
        {
            return;
        }

        var crossesOnly = new List<ROIDesc>();
        AppendCdOvlCrosshair(crossesOnly, isMovable);
        HpImageViewer.UpdateROIs(crossesOnly.ToArray());
    }

    /// <summary>
    /// 从当前 viewer 读取内层补偿十字的位置，生成要写回算法的两个边点。
    /// </summary>
    protected virtual List<Point2d> GetConfirmedInnerCdOvlCompensationPoints()
    {
        if (!RequiresInnerCdOvlEdgeForDisplay())
        {
            return null;
        }

        var crosshairs = HpImageViewer?.ImageViewerDesc?.ROIDescs?.OfType<CrosshairsDesc>().ToList();
        if (crosshairs == null || crosshairs.Count == 0)
        {
            _messageBoxService.InfoDialog("Please set compensation crosshair first.");
            return null;
        }

        var i0 = crosshairs.FirstOrDefault(c => c.Color == InnerCompCrossColor && Math.Abs(c.Angle) < 0.01);
        var i1 = crosshairs.FirstOrDefault(c => c.Color == InnerCompCrossColor && Math.Abs(c.Angle - 180) < 0.01);
        if (i0 == null || i1 == null)
        {
            _messageBoxService.InfoDialog("Inner layer: both edge crosshairs (horizontal and vertical leg) are required.");
            return null;
        }

        return new List<Point2d>
        {
            new Point2d(i0.CenterX, i0.CenterY),
            new Point2d(i1.CenterX, i1.CenterY),
        };
    }

    /// <summary>
    /// 从当前 viewer 读取外层补偿十字的位置，生成要写回算法的两个边点。
    /// </summary>
    protected virtual List<Point2d> GetConfirmedOuterCdOvlCompensationPoints()
    {
        if (!RequiresOuterCdOvlEdgeForDisplay())
        {
            return null;
        }

        var crosshairs = HpImageViewer?.ImageViewerDesc?.ROIDescs?.OfType<CrosshairsDesc>().ToList();
        if (crosshairs == null || crosshairs.Count == 0)
        {
            _messageBoxService.InfoDialog("Please set compensation crosshair first.");
            return null;
        }

        var o0 = crosshairs.FirstOrDefault(c => c.Color == OuterCompCrossColor && Math.Abs(c.Angle) < 0.01);
        var o1 = crosshairs.FirstOrDefault(c => c.Color == OuterCompCrossColor && Math.Abs(c.Angle - 180) < 0.01);
        if (o0 == null || o1 == null)
        {
            _messageBoxService.InfoDialog("Outer layer: both edge crosshairs (horizontal and vertical leg) are required.");
            return null;
        }

        return new List<Point2d>
        {
            new Point2d(o0.CenterX, o0.CenterY),
            new Point2d(o1.CenterX, o1.CenterY),
        };
    }

    /// <summary>
    /// 进入 CD OVL EdgeSelect 编辑态：
    /// 先校验内外层边点已存在，再保存快照并只显示可拖动十字。
    /// </summary>
    [RelayCommand]
    private void SetCdOvlCompensation()
    {
        if (!SupportsCdOvlEdgeCompensation())
        {
            return;
        }
        IEnumerable<Point2d> InneredgeRois = null;
        IEnumerable<Point2d> OuteredgeRois = null;
        if (InneredgeConfirmRois == null || OuteredgeConfirmRois == null)
        {
            InneredgeRois = InneredgeShowRois;
            OuteredgeRois = OuteredgeShowRois;
        }
        else
        {
            InneredgeRois = InneredgeConfirmRois;
            OuteredgeRois = OuteredgeConfirmRois;
        }
        if (RequiresInnerCdOvlEdgeForDisplay() && !TryGetCdOvlTwoEdgePoints(InneredgeRois, out _, out _))
        {
            _messageBoxService.InfoDialog("Inner edge points are not available. Please run measurement first (inner layer must be included).");
            return;
        }

        if (RequiresOuterCdOvlEdgeForDisplay() && !TryGetCdOvlTwoEdgePoints(OuteredgeRois, out _, out _))
        {
            _messageBoxService.InfoDialog("Outer edge points are not available. Please run measurement first (outer layer must be included).");
            return;
        }

        CaptureCdOvlRoiSnapshotForRestore();
        IsCdOvlCrossEditMode = true;
        RefreshCdOvlCrosshair(true);
        EdgeSelect = false;
    }

    /// <summary>
    /// 从当前十字位置读回补偿边点，重建量测 Kernel，并恢复完整 Overlay 显示。
    /// </summary>
    [RelayCommand]
    private async Task ConfirmCdOvlCompensation()
    {
        if (!SupportsCdOvlEdgeCompensation())
        {
            return;
        }

        try
        {
            List<Point2d> innerPoints = GetConfirmedInnerCdOvlCompensationPoints();
            if (RequiresInnerCdOvlEdgeForDisplay() && innerPoints == null)
            {
                return;
            }

            List<Point2d> outerPoints = GetConfirmedOuterCdOvlCompensationPoints();
            if (RequiresOuterCdOvlEdgeForDisplay() && outerPoints == null)
            {
                return;
            }

            if (innerPoints != null)
            {
                InneredgeConfirmRois = innerPoints;
            }

            if (outerPoints != null)
            {
                OuteredgeConfirmRois = outerPoints;
            }

            IsCdOvlCrossEditMode = false;
            EdgeSelect = true;
            await QueueCreateMeasureKernelAsync();
            RestoreCdOvlFullOverlayAndCrosses(false);
        }
        catch (Exception ex)
        {
            LogHelper.LogError(ex);
            _messageBoxService.ErrorDialog("Failed to apply CD OVL edge compensation: " + ex.Message);
        }
    }

    /// <summary>
    /// 放弃本次 EdgeSelect 编辑，恢复进入编辑态前的 ROI 显示。
    /// </summary>
    [RelayCommand]
    private void CancelCdOvlCompensation()
    {
        TryRestoreCdOvlSnapshotBeforeEdgeOnlyMode();

        IsCdOvlCrossEditMode = false;
        EdgeSelect = true;
    }

    /// <summary>
    /// 清空当前记录的 CD OVL 边点，并恢复为仅显示量测 ROI 的状态。
    /// </summary>
    [RelayCommand]
    private async Task ResetCdOvlCompensation()
    {
        if (!SupportsCdOvlEdgeCompensation())
        {
            return;
        }

        IsCdOvlCrossEditMode = false;
        ClearCdOvlRoiSnapshot();
        InneredgeConfirmRois = null;
        OuteredgeConfirmRois = null;
        await QueueCreateMeasureKernelAsync();
        RebuildCdOvlViewerMeasureRoisOnly();
        EdgeSelect = true;
    }
}
