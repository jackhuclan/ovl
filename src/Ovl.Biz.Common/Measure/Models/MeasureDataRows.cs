namespace Ovl.Biz.Common.Measure.Models;

/// <summary>二维点（单位由 MeasureValueUnitFactor 决定）。</summary>
public sealed class Point2D
{
    public double X { get; set; }
    public double Y { get; set; }
}

/// <summary>场/列信息，FieldCVT 转换为 "列,行" 显示。</summary>
public sealed class RowColumnInfo
{
    public int ColumnIndex { get; set; }
    public int RowIndex { get; set; }

    public override string ToString() => $"{ColumnIndex},{RowIndex}";
}

/// <summary>相关系数集合（四边）。</summary>
public sealed class CorrScoreSet
{
    public double[] LeftCorr { get; set; } = [];
    public double[] RightCorr { get; set; } = [];
    public double[] TopCorr { get; set; } = [];
    public double[] BottomCorr { get; set; } = [];
}

/// <summary>QMerit 量测值。</summary>
public sealed class QMeritData
{
    public double X { get; set; }
    public double Y { get; set; }
    public double InnerX { get; set; }
    public double InnerY { get; set; }
    public double OuterX { get; set; }
    public double OuterY { get; set; }
}

/// <summary>Overlay 量测数据（内/外层中心、Misreg、相关系数、QMerit）。</summary>
public sealed class OverlayData
{
    public Point2D CenterInner { get; set; } = new();
    public Point2D CenterOuter { get; set; } = new();
    public CorrScoreSet CorrScoreInner { get; set; } = new();
    public CorrScoreSet CorrScoreOuter { get; set; } = new();
    public double MisregX { get; set; }
    public double MisregY { get; set; }
    public QMeritData QMerit { get; set; } = new();
}

/// <summary>
/// 单测点结果行基类。对应原版 Marvel.DataModels.Core.TargetResultBase 在界面绑定中
/// 实际用到的字段子集，列名与绑定路径与原版一致。
/// </summary>
public class TargetResultRow
{
    /// <summary>SLot#。</summary>
    public int WaferSlotNo { get; set; }

    /// <summary>Iteration（晶圆迭代）。</summary>
    public int WaferIterations { get; set; }

    /// <summary>量测状态（Pass/Fail/Error/Empty 等）。</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>晶圆朝向（0/90/180/270 或 Notch 方向描述）。</summary>
    public string Orientation { get; set; } = string.Empty;

    /// <summary>测试项名称。</summary>
    public string TestName { get; set; } = string.Empty;

    /// <summary>测试项类型。</summary>
    public string TestType { get; set; } = string.Empty;

    /// <summary>场/列信息。</summary>
    public RowColumnInfo Field { get; set; } = new();

    /// <summary>Cell 编号。</summary>
    public int Cell { get; set; }

    /// <summary>TargetID。</summary>
    public int Index { get; set; }

    /// <summary>靶点坐标 R3。</summary>
    public Point2D R3 { get; set; } = new();

    /// <summary>静态迭代次数。</summary>
    public int Iterations { get; set; }

    /// <summary>Overlay 数据。</summary>
    public OverlayData Overlay { get; set; } = new();

    /// <summary>CD 量测值（字符串，原版即字符串）。</summary>
    public string CdResultStr { get; set; } = string.Empty;
}

/// <summary>Raw Data 行。</summary>
public sealed class RawDataRow : TargetResultRow;

/// <summary>OVL 数据行（含 Ovl_X / Ovl_Y 列）。</summary>
public sealed class OvlDataRow : TargetResultRow
{
    public double Ovl_X => Overlay.MisregX;
    public double Ovl_Y => Overlay.MisregY;
}

/// <summary>TIS 原始数据行（含 Tis_X / Tis_Y、剔除原因）。</summary>
public sealed class TisRawDataRow : TargetResultRow
{
    public double Tis_X => Overlay.MisregX;
    public double Tis_Y => Overlay.MisregY;
    public string CulledReason { get; set; } = string.Empty;
}

/// <summary>异常数据行（含失败原因）。</summary>
public sealed class ErrorDataRow : TargetResultRow
{
    public string FailReason { get; set; } = string.Empty;
}

/// <summary>BFCD 结果行（含 CD 值）。</summary>
public sealed class BfcdResultRow : TargetResultRow
{
    /// <summary>CD 迭代次数。</summary>
    public int Iteration { get; set; }
}
