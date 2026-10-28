namespace Ovl.Biz.Common.Measure.Models;

/// <summary>
/// Lot 级统计数据行。字段与原版 Marvel.DataModels.Job.LotStatisticsData 对齐；
/// TIS* 字段对应原版绑定中的 TISMean / TISMin / TISMax / TIS3Sigma / TISMeanPlus3Sigma。
/// </summary>
public class LotStatisticsRow
{
    /// <summary>数据类型（OVL / CD / TIS 等）。</summary>
    public string DataType { get; set; } = "OVL";

    public string LotID { get; set; } = string.Empty;

    public string RecipeName { get; set; } = string.Empty;

    public double XMean { get; set; }
    public double YMean { get; set; }
    public double X3Sigma { get; set; }
    public double Y3Sigma { get; set; }
    public double XMeanPlusX3Sigma { get; set; }
    public double YMeanPlusY3Sigma { get; set; }
    public double XMin { get; set; }
    public double YMin { get; set; }
    public double XMax { get; set; }
    public double YMax { get; set; }
    public double XMedian { get; set; }
    public double YMedian { get; set; }

    /// <summary>统计生成时间（历史记录列表展示用）。</summary>
    public DateTime Time { get; set; } = DateTime.Now;

    public Point2D TISMean { get; set; } = new();
    public Point2D TISMin { get; set; } = new();
    public Point2D TISMax { get; set; } = new();
    public Point2D TIS3Sigma { get; set; } = new();
    public Point2D TISMeanPlusX3Sigma { get; set; } = new();
    public Point2D TISMeanPlusY3Sigma { get; set; } = new();
}

/// <summary>Wafer 级统计数据行（在 Lot 统计基础上加 SLot#）。</summary>
public sealed class WaferStatisticsRow : LotStatisticsRow
{
    public int WaferSlotNo { get; set; }
}
