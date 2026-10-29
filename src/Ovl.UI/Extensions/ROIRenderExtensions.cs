using HPImageViewer.Core;
using HPImageViewer.Core.Persistence;
using InteVega.CBB.Common.Algorithm.Shapes;
using Marvel.DataModels.Extensions;
using Marvel.DataModels.Recipe.CD;
using OVL.Algorithm.DoNet;
using WinCopies.Linq;

namespace Ovl.UI.Extensions;

public static class ROIRenderExtensions
{
    public static void LockAll(this IHPImageViewer viewer)
    {
        viewer.ROIRenders.ToList().ForEach(item => item.IsLocked = true);
    }

    /// <summary>
    /// 获取集合指定行数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="values"></param>
    /// <param name="vision">从0开始</param>
    /// <returns></returns>
    public static T GetEnumerableValue<T>(this IEnumerable<T> obj, int vision)
    {
        if (obj == null)
        {
            return default(T);
        }
        if (obj.Count() <= vision)
        {
            return default(T);
        }
        return obj.ElementAt(vision);
    }

    /// <summary>
    /// 正常CD转换成CDROIBase
    /// </summary>
    /// <param name="pickerRoi">前端roi类型</param>
    /// <returns></returns>
    /// <exception cref="Exception">如果超过小于两个pickercount就报异常</exception>
    public static CDROIBase CreateDimensionROI(CD_Orientention ori, IEnumerable<RotatedPickersDesc> pickerRois,IEnumerable<Point2d> edgeRois=null)
    {
        //var pickers = new List<LinePicker2d>();
        //var linePicker = new LinePicker2d();
        if (!(pickerRois?.Any()?? false)) throw new Exception("Picker Roi Count is unkown counting! \n None Picker Count!");
        switch (ori)
        {
            case CD_Orientention.Vertical:
            case CD_Orientention.Horizontal:
            case CD_Orientention.User_Defined:
                if (pickerRois.Count() > 1) throw new Exception("Picker Roi Count is unkown counting! \n Should be count 1!");

                return new DimensionROI()
                {
                    PointRoi1 = ConvertToAlgoRoi(pickerRois.FirstOrDefault())[0],
                    PointRoi2 = ConvertToAlgoRoi(pickerRois.FirstOrDefault())[1],
                    SetEdgePoint1 = (edgeRois != null && edgeRois.Count() == 2) ? edgeRois.FirstOrDefault() : null,
                    SetEdgePoint2 = (edgeRois != null && edgeRois.Count() == 2) ? edgeRois.Skip(1).FirstOrDefault() : null,
                };
            //case CD_Orientention.Both:
            //    if (pickerRois.Count() != 2) throw new Exception("Picker Roi Count is unkown counting! \n Should be 1 Veri Roi and 1 Hori Roi!");
            //    var horidesc = pickerRois.Where(_ => Convert.ToInt32(_.StartAngle) == 0).FirstOrDefault();
            //    var horiRoi = new DimensionROI()
            //    {
            //        PointRoi1 = ConvertToAlgoRoi(horidesc)[0],
            //        PointRoi2 = ConvertToAlgoRoi(horidesc)[1]
            //    };
            //    var veridesc = pickerRois.Where(_ => Convert.ToInt32(_.StartAngle) == 90).FirstOrDefault();
            //    var veriRoi = new DimensionROI()
            //    {
            //        PointRoi1 = ConvertToAlgoRoi(veridesc)[0],
            //        PointRoi2 = ConvertToAlgoRoi(veridesc)[1]
            //    };
            //    return new RectangularDimensionROI()
            //    {
            //        HorRoi = horiRoi,
            //        VerRoi = veriRoi,
            //    };
            case CD_Orientention.Circle:
                if (pickerRois.Count() != 1) throw new Exception("Picker Roi Count is unkown counting! \n Should be 1 Veri Roi and 1 Hori Roi!");
                var dimensionRois = new List<DimensionROI>();
                var pickerRoi = pickerRois.FirstOrDefault();
                var pointRois = ConvertToAlgoRoi(pickerRoi);

                for (int i = 0; i < pointRois.Count / 2; i++)
                {
                    dimensionRois.Add(new DimensionROI()
                    {
                        PointRoi1 = pointRois[i],
                        PointRoi2 = pointRois[pointRois.Count / 2 + i],
                        SetEdgePoint1 = edgeRois.GetEnumerableValue(i * 2),
                        SetEdgePoint2 = edgeRois.GetEnumerableValue((i * 2) + 1),
                    }) ;
                }
                return new CircularDimensionROI()
                {
                    Rois = dimensionRois
                };
            default:
                throw new Exception("Picker Roi Count is unkown counting! \n None Type!");
        }
    }
    /// <summary>
    /// 跨视野CD转换成CDROIBase
    /// </summary>
    /// <param name="ori"></param>
    /// <param name="pickerRois"></param>
    /// <param name="edgeRois"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static CDROIBase CreateDimensionROIOverFov(CD_Orientention ori, IEnumerable<RotatedPickersDesc> pickerRois, IEnumerable<Point2d> edgeRois = null)
    {
        if (!(pickerRois?.Any() ?? false)) throw new Exception("Picker Roi Count is unkown counting! \n None Picker Count!");
        switch (ori)
        {
            case CD_Orientention.Vertical:
            case CD_Orientention.Horizontal:
                if (pickerRois.Count() > 1) throw new Exception("Picker Roi Count is unkown counting! \n Should be count 1!");
                Point2d point2D = (edgeRois != null && edgeRois.Count() >0) ? edgeRois.FirstOrDefault() : null;
                if (point2D!=null && point2D.Equals(new Point2d(0,0)))
                    point2D = null;
                RotatedPickersDesc rotatedPickersDesc = pickerRois.FirstOrDefault();
                rotatedPickersDesc.EdgePoints = new List<HPImageViewer.Core.Primitives.Point>();
                if (point2D!=null)
                {
                    rotatedPickersDesc.EdgePoints.Add(new HPImageViewer.Core.Primitives.Point(point2D.X, point2D.Y));
                }
                return new DimensionROI()
                {
                    PointRoi1 = ConvertToAlgoRoiOverFov(rotatedPickersDesc),
                    PointRoi2 = ConvertToAlgoRoiOverFov(rotatedPickersDesc),
                    SetEdgePoint1 = point2D,
                    SetEdgePoint2 = point2D,
                };
            case CD_Orientention.Circle:
                if (pickerRois.Count() != 1) throw new Exception("Picker Roi Count is unkown counting! \n Should be 1 Veri Roi and 1 Hori Roi!");
                var dimensionRois = new List<DimensionROI>();
                var pickerRoi = pickerRois.FirstOrDefault();
                var pointRois = ConvertToAlgoRoi(pickerRoi);

                for (int i = 0; i < pointRois.Count / 2; i++)
                {
                    dimensionRois.Add(new DimensionROI()
                    {
                        PointRoi1 = pointRois[i],
                        PointRoi2 = pointRois[pointRois.Count / 2 + i]
                    });
                }
                return new CircularDimensionROI()
                {
                    Rois = dimensionRois
                };
            default:
                throw new Exception("Picker Roi Count is unkown counting! \n None Type!");
        }
    }
    /// <summary>
    /// DSCD转换成CDROIBase
    /// （包含跨视野）
    /// </summary>
    /// <param name="pickerRois"></param>
    /// <returns></returns>
    public static List<CDROIBase> CreateDsDimensionROI(IEnumerable<RotatedPickersDesc> pickerRois)
    {
        List<CDROIBase> cDROIBases = new List<CDROIBase>();

        foreach (var item in pickerRois)
        {
            //需要判断是圆还是方框
            if (item.IsShowCircle)
            {
                CircularDimensionROI circularDimensionROI = new CircularDimensionROI();
                circularDimensionROI.Rois = new List<DimensionROI>();
                var pointRois = ConvertToAlgoRoi(item);
                for (int i = 0; i < pointRois.Count / 2; i++)
                {
                    circularDimensionROI.Rois.Add(new DimensionROI
                    {
                        PointRoi1 = ConvertToAlgoRoi(item)[i],
                        PointRoi2 = ConvertToAlgoRoi(item)[pointRois.Count / 2 + i],
                        SetEdgePoint1 = null,
                        SetEdgePoint2 = null
                    });
                }
                cDROIBases.Add(circularDimensionROI);
            }
            else
            {
                RectangularDimensionROI rectangularDimensionROI = new RectangularDimensionROI();
                var horiRoi = new DimensionROI()
                {
                    PointRoi1 = ConvertToAlgoRoi(item)[0],
                    PointRoi2 = ConvertToAlgoRoi(item)[2]
                };
                var veriRoi = new DimensionROI()
                {
                    PointRoi1 = ConvertToAlgoRoi(item)[1],
                    PointRoi2 = ConvertToAlgoRoi(item)[3]
                };
                rectangularDimensionROI.HorRoi = horiRoi;
                rectangularDimensionROI.VerRoi = veriRoi;
                cDROIBases.Add(rectangularDimensionROI);
            }
        }
        return cDROIBases;
    }

    /// <summary>
    /// lRectangleDesc转换成DimensionROI
    /// </summary>
    /// <param name="pickerRoi">前端roi类型</param>
    /// <returns></returns>
    /// <exception cref="Exception">如果超过小于两个pickercount就报异常</exception>
    public static DimensionROI CreateDimensionROI(LRectangleDesc lRectangleDesc, IEnumerable<Point2d> edgeRois = null)
    {
        return new DimensionROI()
        {
            PointRoi1 = ConvertToAlgoRoi(lRectangleDesc,0),
            PointRoi2 = ConvertToAlgoRoi(lRectangleDesc,1),
            SetEdgePoint1 = (edgeRois != null && edgeRois.Count() == 2) ? edgeRois.FirstOrDefault() : null,
            SetEdgePoint2 = (edgeRois != null && edgeRois.Count() == 2) ? edgeRois.Skip(1).FirstOrDefault() : null,
        };
    }

    public static ShapeBase CreateDiemension2d(CD_Orientention roi, IEnumerable<RotatedPickersDesc> pickerRois)
    {
        return CreateDimensionROI(roi, pickerRois).ToDimension2d();
    }


    /// <summary>
    /// 创建多维算法层量测roi
    /// </summary>
    /// <param name="mutliRois">界面层roi 信息列表</param>
    /// <param name="isAcrossTheCameraVision">是否跨视野</param>
    /// <returns></returns>
    public static CDROIBase CreateMultiDimensionROI(this IEnumerable<MultiRoiInfo> mutliRois, bool isAcrossTheCameraVision)
    {
        MultiDimensionROI cdRoi = new MultiDimensionROI();
        cdRoi.Rois = new List<DimensionROI>();

        foreach (var item in mutliRois)
        {
            if (item.MeasureROI != null)
            {
                List<RotatedPickersDesc> rotatedPickersDescs = new List<RotatedPickersDesc>() { (RotatedPickersDesc)item.MeasureROI };
                if (isAcrossTheCameraVision)
                {
                    cdRoi.Rois.Add((DimensionROI)CreateDimensionROIOverFov(item.CDOrientention, rotatedPickersDescs, item.ROIEdgeList));
                }
                else
                {
                    cdRoi.Rois.Add((DimensionROI)CreateDimensionROI(item.CDOrientention, rotatedPickersDescs, item.ROIEdgeList));
                }
            }
        }
        return cdRoi;
    }
    public static IEnumerable<ROIDesc> CreateDescROI(IEnumerable<RoiDefaultParam_CD> defaults,
                                              double centerX,
                                              double centerY,
                                              double radiusW,
                                              double radiusH)
    {
        var rois = new List<ROIDesc>();
        switch (defaults?.Count())
        {
            case 1:
                var para = defaults.FirstOrDefault();
                rois.Add(new RotatedPickersDesc()
                {
                    CenterX = centerX,
                    CenterY = centerY,
                    RadiusW = radiusW,
                    RadiusH = radiusH,
                    RectWidth = para.RectWidth,
                    RectHeight = para.RectHeight,
                    StartAngle = para.StartAngle,
                    PickerCount = para.PickerCount,
                    IsShowCircle = para.IsShowCircle,
                    SpinLength=50,
                    IsSpin = para.IsShowSpin
                }) ;
                break;
            case 2:
                foreach (var para1 in defaults)
                {
                    rois.Add(new RotatedPickersDesc()
                    {
                        CenterX = centerX,
                        CenterY = centerY,
                        RadiusW = radiusW,
                        RadiusH = radiusH,
                        RectWidth = para1.RectWidth,
                        RectHeight = para1.RectHeight,
                        StartAngle = para1.StartAngle,
                        PickerCount = para1.PickerCount,
                        IsShowCircle = para1.IsShowCircle,
                        IsSpin=para1.IsShowSpin
                    });
                }
                break;
            default:
                throw new ArgumentException("param error", nameof(defaults));
        }
        return rois;
    }

    public static IEnumerable<ROIDesc> UpdateDescROI(IEnumerable<RotatedPickersDesc> rois,  bool isReverse)
    {
        foreach (var roi in rois)
        {
            roi.IsReverseArrow = isReverse;
        }
        return rois;
    }

    private static LinePicker2d ConvertToAlgoRoi(LRectangleDesc pickerRoi,int Index)
    {
        if (Index == 0)
        {
            var lines = pickerRoi.GetInnerRectStartEnd();
            var linePicker = new LinePicker2d();
            linePicker.Axis = new Line2d()
            {
                Start = new Point2d(lines[0].X, lines[0].Y),
                End = new Point2d(lines[1].X, lines[1].Y)
            };
            linePicker.Width = lines[2].X;
            return linePicker;
        }
        else
        {
            var lines = pickerRoi.GetInnerRectStartEnd();
            var linePicker = new LinePicker2d();
            linePicker.Axis = new Line2d()
            {
                Start = new Point2d(lines[3].X, lines[3].Y),
                End = new Point2d(lines[4].X, lines[4].Y)
            };
            linePicker.Width = lines[5].Y;
            return linePicker;
        }

    }

    private static List<LinePicker2d> ConvertToAlgoRoi(RotatedPickersDesc pickerRoi)
    {
        var pickers = new List<LinePicker2d>();
        if (pickerRoi.PickerCount >= 2)
        {
            for (int i = 0; i < pickerRoi.PickerCount; i++)
            {
                var lines = pickerRoi.GetInnerRectStartEnd(i);
                var linePicker = new LinePicker2d();
                linePicker.Axis = new Line2d()
                {
                    Start = new Point2d(lines[0].X, lines[0].Y), //new Point2d((rect.l + rect.r) / 2 , rect.t),
                    End = new Point2d(lines[2].X, lines[2].Y)//new Point2d((rect.l + rect.r) / 2 , rect.b),
                };
                var deltaX = lines[1].X - lines[3].X;
                var deltaY = lines[1].Y - lines[3].Y;
                var distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
                linePicker.Width = distance;
                pickers.Add(linePicker);
            }
        }
        else
        {
            throw new Exception("Picker Roi Count is unkown counting!");
        }
        return pickers;
    }

    private static LinePicker2d ConvertToAlgoRoiOverFov(RotatedPickersDesc pickerRoi)
    {
        var lines = pickerRoi.GetInnerRectStartEnd(0);
        var linePicker = new LinePicker2d();
        linePicker.Axis = new Line2d()
        {
            Start = new Point2d(lines[0].X, lines[0].Y), //new Point2d((rect.l + rect.r) / 2 , rect.t),
            End = new Point2d(lines[2].X, lines[2].Y)//new Point2d((rect.l + rect.r) / 2 , rect.b),
        };
        var deltaX = lines[1].X - lines[3].X;
        var deltaY = lines[1].Y - lines[3].Y;
        var distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        linePicker.Width = distance;
        return linePicker;
    }

}
