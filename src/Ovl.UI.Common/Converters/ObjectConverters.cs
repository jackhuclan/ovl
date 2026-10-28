using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Ovl.UI.Common.Converters;

/// <summary>
/// 对象 → Visibility（键 OtvCVT）。
/// 本地等价实现，替代厂内 InteVega.CBB.ComUI/Themes/Converters.xaml 中的同名转换器：
/// null 或空字符串 → Collapsed，其余 → Visible。
/// </summary>
public sealed class ObjectToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            null => Visibility.Collapsed,
            string s when string.IsNullOrEmpty(s) => Visibility.Collapsed,
            _ => Visibility.Visible,
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// bool → Visibility（键 Boolean2VisibilityConverter，与厂内/控件库同名键等价）：
/// true → Visible，false/null/非 bool → Collapsed。
/// </summary>
public sealed class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// 对象 → bool 取反（键 OtbReCVT）。
/// 本地等价实现：bool 取反；null / 非 bool 视为 true（无信息时保持可用），保持与厂内控件行为一致。
/// </summary>
public sealed class ObjectToBooleanReverseConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool b ? !b : true;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
