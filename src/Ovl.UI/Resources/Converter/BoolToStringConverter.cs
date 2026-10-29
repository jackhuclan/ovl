using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace Ovl.UI.Resources.Converter;

public class BoolToStringConverter: IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            if (parameter is string str)
            {
                var arr = str.Split(';');
                if (arr.Length > 1)
                {
                    return boolValue ? arr[0] : arr[1];
                }
                return "";
            }
            return "";
        }
        return "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
