using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Ovl.Biz.Common.Measure.Models;

namespace Ovl.UI.Common.Converters;

/// <summary>
/// Field 列转换（键 FieldCVT）：RowColumnInfo → "列,行"。
/// 与原版 Marvel.ComModule.FieldToStringConvert 行为一致。
/// </summary>
public sealed class FieldToStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is RowColumnInfo info ? $"{info.ColumnIndex},{info.RowIndex}" : string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Port 列转换（键 portCVT）：端口索引（0 基）→ 显示号（1 基）。
/// </summary>
public sealed class PortIndexConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is int index ? (index + 1).ToString(culture) : string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => int.TryParse(value?.ToString(), NumberStyles.Integer, culture, out var display) ? display - 1 : 0;
}

/// <summary>
/// 对象 → 可见性取反（键 OtvReCVT）。
/// 传入 ConverterParameter 时：值等于参数（字符串比较）→ Collapsed，否则 Visible（用于"运行中隐藏 Run 按钮"）；
/// 未传参数时：null / false / Collapsed / Hidden → Visible，其余 → Collapsed（用于"Run 隐藏则显示 Pause"）。
/// </summary>
public sealed class ObjectToVisibilityReverseConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (parameter is not null)
        {
            return string.Equals(value?.ToString(), parameter.ToString(), StringComparison.OrdinalIgnoreCase)
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        var hidden = value switch
        {
            null => true,
            bool b => !b,
            Visibility v => v != Visibility.Visible,
            _ => false,
        };

        return hidden ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
