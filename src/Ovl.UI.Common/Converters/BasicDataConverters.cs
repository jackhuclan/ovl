using System.Globalization;
using System.Windows.Data;

namespace Ovl.UI.Common.Converters;

/// <summary>
/// bool → 字符串（键 BoolToStringConverter）。原样移植自
/// Marvel.Main.Resources.Converter.BoolToStringConverter：ConverterParameter 以 ';' 分隔
/// 两个值，true 取第一个、false 取第二个。
/// </summary>
public sealed class BoolToStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue && parameter is string str)
        {
            var arr = str.Split(';');
            if (arr.Length > 1)
            {
                return boolValue ? arr[0] : arr[1];
            }
        }

        return string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Lot 统计数值换算（键 LotStatisticsDataUnitFactorConverter）。原样移植自
/// Marvel.Main.Resources.Converter.LotStatisticsDataUnitFactorConverter：
/// MultiBinding [数值, 单位系数]，输出 value / factor，格式 "0.0000"。
/// </summary>
public sealed class LotStatisticsDataUnitFactorConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Length == 2
            && values[0] is double value
            && TryGetFactor(values[1], out var factor)
            && factor != 0)
        {
            return (value / factor).ToString("0.0000", culture);
        }

        return string.Empty;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();

    private static bool TryGetFactor(object input, out double factor)
    {
        switch (input)
        {
            case int i:
                factor = i;
                return true;
            case double d:
                factor = d;
                return true;
            default:
                factor = 0;
                return false;
        }
    }
}
