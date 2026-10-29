using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace Ovl.UI.Resources.Converter;

internal class BoolToXmalWidthConvert : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if(value is  bool BoolValue)
        {
            return BoolValue ? 0 : GridLength.Auto;
        }
        return GridLength.Auto;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
