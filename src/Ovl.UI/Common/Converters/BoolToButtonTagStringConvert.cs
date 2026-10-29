using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Ovl.UI.Common.Converters
{
    public class BoolToButtonTagStringConvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return "";
            return (bool)value ? "Selected" :"";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
