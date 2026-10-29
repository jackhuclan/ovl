using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dm.util;
using HPImageViewer.Core.Persistence;
using InteVega.CBB.Common.Algorithm.Shapes;
using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.CommonLib.Helpers.Reflection;
using InteVega.CBB.DomainService.Api.Extensions;
using Marvel.DataModels.Enums;
using Marvel.DataModels.Extensions;
using Marvel.DataModels.Recipe.CD;
using Megarobo.Ovl.Type;
using NetTaste;
using OVL.Algorithm.DoNet;
using Wafer.Algorithm.Common.DoNet;

namespace Ovl.UI.Recipe.ViewModel;

/// <summary>
/// CD Train 主界面
/// </summary>
public partial class Test_TrainCDViewModel
{
    /// <summary>
    /// 多维ROI列表
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<MultiRoiInfo> _multiRoiList;
    /// <summary>
    /// 是否设置边缘ROI
    /// </summary>
    [ObservableProperty]
    private bool _isSetEdgeROI;
    /// <summary>
    /// 加载多维量测列表
    /// </summary>
    void LoadCDMultiRoi()
    {
        MultiRoiList = GetCDParamViewModel().TrainSetting_CD.MultiRoiList;
        SetEnableEditEdge(null, false);
    }
    /// <summary>
    ///  设置是否可以编辑边缘
    /// </summary>
    /// <param name="multiRoi"></param>
    /// <param name="isSetEdgeROI"></param>
    private void SetEnableEditEdge(object multiRoi, bool isSetEdgeROI)
    {
        var dataList = GetCDParamViewModel().TrainSetting_CD.MultiRoiList;// MultiRoiList.ToList();
        foreach (var item in dataList)
        {
            item.IsSetEdgeROI = false;
        }
        if (multiRoi != null)
        {
            var id = ((MultiRoiInfo)multiRoi).Id;
            dataList.FirstOrDefault(f => f.Id == id).IsSetEdgeROI = isSetEdgeROI;
        }
        MultiRoiList = dataList;
        MeasurementROI_EdgeEnabled = isSetEdgeROI;
    }
    [RelayCommand]
    private void SetEdgeROIMultiRoi(object obj)
    {
        bool issuccess = DrawMeasureROI(true, (MultiRoiInfo)obj, true);
        if (!issuccess) return;
        SetEnableEditEdge(obj, true);

    }
    [RelayCommand]
    private void ConfirmMultiEdgeRoi(object obj)
    {
        var roiList = GetCDParamViewModel().HpImageViewer.ImageViewerDesc.ROIDescs;
        List<Point2d> edgeRoiList = new List<Point2d>();
        foreach (var item in roiList)
        {
            if (item is CrosshairsDesc crosshairItem)
            {
                crosshairItem.IsMove = false;
                edgeRoiList.Add(new Point2d(crosshairItem.CenterX, crosshairItem.CenterY));
            }
        }
        ((MultiRoiInfo)obj).ROIEdgeList = edgeRoiList;
        LogHelper.LogInfo("Update Roi Edge:");
        foreach (var item in edgeRoiList)
        {
            LogHelper.LogInfo($"New edge point:{item}");
        }
        GetCDParamViewModel().HpImageViewer.UpdateROIs(roiList.ToArray());
        Run();
        SetEnableEditEdge(obj, false);
    }
    [RelayCommand]
    private void CancelMultiEdgeRoi(object obj)
    {
        HpImageViewer.UpdateROIs();
        SetEnableEditEdge(obj, false);
    }

    [RelayCommand]
    private void ResetMultiEdgeRoi(object obj)
    {
        ((MultiRoiInfo)obj).ROIEdgeList = null;

        SetEnableEditEdge(obj, true);
    }

    private CrosshairsDesc NewCrosshairsDesc(Point2d point ,bool isMove)
    {
        string color = "Lime";
        return new CrosshairsDesc(point.X, point.Y) { Color = color, Size = 10, IsMove = isMove };
    }

    /// <summary>
    /// 量测后生成量测ROI
    /// </summary>
    /// <param name="test_TrainCDParamViewModel"></param>
    /// <param name="enumVision"></param>
    /// <param name="isEdge"></param>
    /// <param name="isMoveEdge"></param>
    /// <returns></returns>
    private bool DrawMeasureROI(bool isEdge, MultiRoiInfo multiRoiInfo = null, bool isMoveEdge = false)
    {

        if (Curves == null || !(Curves?.Any() ?? false))
        {
            _messageBoxService.ErrorDialog("Please run the measurement first!");
            return false;
        }
        var rOIDescs = new List<ROIDesc>();
        int num = 1;
        foreach (var curve in Curves)
        {
            var pointCurve = new List<HPImageViewer.Core.Primitives.Point>();
            if (curve.Xs.Length > 0)
            {
                for (int i = 0; i < curve.Xs.Length; i++)
                {
                    pointCurve.Add(new HPImageViewer.Core.Primitives.Point(curve.Xs[i], curve.Ys[i]));
                }
                if (multiRoiInfo == null)
                {
                    rOIDescs.Add(new PolygonDesc() { Vertices = pointCurve, IsClosed = false, IsMove = false });
                }
                else
                {
                    if (num >= (multiRoiInfo.Id * 2 - 1) && num <= (multiRoiInfo.Id * 2))
                    {
                        rOIDescs.Add(new PolygonDesc() { Vertices = pointCurve, IsClosed = false, IsMove = false });
                    }
                }
                num++;
            }
        }
        if (isEdge)
        {

            for (int i = 0; i < EdgePointsPair.Length; i++)
            {
                var point1 = EdgePointsPair[i].Point1;
                var point2 = EdgePointsPair[i].Point2;
                if (multiRoiInfo == null)
                {
                    List<Point2d> edgeRoiList = new List<Point2d>();
                    if (!IsAcrossTheCameraVision || Vision == EnumVision.Vision1)
                    {
                        rOIDescs.Add(NewCrosshairsDesc(point1, isMoveEdge));
                        edgeRoiList.Add(point1);

                    }
                    if (!IsAcrossTheCameraVision || Vision == EnumVision.Vision2)
                    {
                        rOIDescs.Add(NewCrosshairsDesc(point2,isMoveEdge ));
                        edgeRoiList.Add(point2);
                    }
                    var measureRoi = GetCDParamViewModel().MultiRoiList[i];
                    //此处只有当边缘点为null时才使用算法生成的边缘点设置roi
                    //如果不为null的时候，说明不是第一次执行Run 方法，或者是用户手动修改了边缘点，此时不能再用算法生成的边缘点更新，
                    //否则会使边缘点距离越来越大，或者越来越小
                    if (measureRoi.ROIEdgeList == null)
                    {
                        measureRoi.ROIEdgeList = edgeRoiList;
                    }
                }
                else
                {
                    if (i + 1 == multiRoiInfo.Id)
                    {
                        if (!IsAcrossTheCameraVision || Vision == EnumVision.Vision1)
                        {
                            rOIDescs.Add(NewCrosshairsDesc(point1, isMoveEdge ));
                        }
                        if (!IsAcrossTheCameraVision || Vision == EnumVision.Vision2)
                        {
                            rOIDescs.Add(NewCrosshairsDesc(point2, isMoveEdge ));
                        }
                    }
                }
            }
        }
        HpImageViewer.UpdateROIs(rOIDescs.ToArray());
        return true;
    }
}
