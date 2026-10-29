using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace Ovl.UI.Resources.Converter;

internal class ModelBoolToStringConvert : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if(value is bool Boolean)
        {
            return Boolean ? "created" : "not create";
        }
        return "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
