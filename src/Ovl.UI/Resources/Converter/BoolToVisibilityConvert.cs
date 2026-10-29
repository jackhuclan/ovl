using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace Ovl.UI.Resources.Converter;

internal class BoolToVisibilityConvert : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if(value is bool BoolValue)
        {
            return BoolValue ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if(value is Visibility VisibleValue)
        {
            return VisibleValue = Visibility.Visible;
        }
        return false;
    }
}
