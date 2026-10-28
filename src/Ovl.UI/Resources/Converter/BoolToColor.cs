using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
namespace Ovl.UI.Resources.Converter
{
    internal class BoolToColor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value is bool Boolean)
            {
                return Boolean?new SolidColorBrush(Color.FromRgb(10, 70, 113)):new SolidColorBrush(Color.FromRgb(109, 109, 109));
            }
            return new SolidColorBrush(Color.FromRgb(109, 109, 109));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
