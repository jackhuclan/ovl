using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace Ovl.UI.Resources.Converter;

internal class EnumToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value?.ToString().Equals(parameter?.ToString());
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value.Equals(true) ? parameter : Binding.DoNothing;
    }
}
