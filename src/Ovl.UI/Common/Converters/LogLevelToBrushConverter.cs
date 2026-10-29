using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Ovl.UI.Common.Converters;

/// <summary>
/// 日志等级 → 颜色（键 LogLevelConvert）。本地等价实现，替代厂内
/// InteVega.CBB.Log.Display 资源字典中的同名转换器：information 绿、
/// warning 橙、error 红、fatal 深红、debug 灰。
/// </summary>
public sealed class LogLevelToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value?.ToString()?.ToLowerInvariant() switch
        {
            "information" => new SolidColorBrush(Color.FromRgb(0x34, 0xA8, 0x53)),
            "warning" => new SolidColorBrush(Color.FromRgb(0xF9, 0xAB, 0x00)),
            "error" => new SolidColorBrush(Color.FromRgb(0xEA, 0x43, 0x35)),
            "fatal" => new SolidColorBrush(Color.FromRgb(0xB3, 0x14, 0x00)),
            "debug" => new SolidColorBrush(Color.FromRgb(0x9A, 0xA0, 0xA6)),
            _ => Brushes.Gray,
        };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
