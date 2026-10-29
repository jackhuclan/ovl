using System.Globalization;
using System.Windows.Data;

namespace Ovl.UI.Resources.Converter;

/// <summary>
/// 整数转布尔值转换器，用于RadioButton绑定
/// </summary>
public class IntegerToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int intValue && parameter is string strParam
            && int.TryParse(strParam, out int paramValue))
        {
            return intValue == paramValue;
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue && boolValue && parameter is string strParam
            && int.TryParse(strParam, out int paramValue))
        {
            return paramValue;
        }
        return Binding.DoNothing;
    }
}
