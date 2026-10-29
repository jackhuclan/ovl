using System.Globalization;
using System.Windows.Data;

namespace Ovl.UI.Resources.Converter;

public class LotStatisticsDataUnitFactorConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length == 2 && values[0] is double value && values[1] is int factor && factor != 0)
        {
            return (value / factor).ToString("0.0000", culture);
        }
        return "";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException(); // 仅显示，无需反向转换
    }
}
