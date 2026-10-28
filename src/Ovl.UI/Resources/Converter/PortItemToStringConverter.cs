using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

using Ovl.UI.WaferHandler.ViewModel;
namespace Ovl.UI.Resources.Converter
{
    internal class PortItemToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SlotItem portItem)
            {
                if (int.TryParse(parameter.ToString(), out int index))
                {
                    switch (index)
                    {
                        case 0:
                            return portItem.MinIndex == 0 ? "" : portItem.MinIndex.ToString();
                        case 1:
                            return portItem.MaxIndex == 0 ? "" : portItem.MaxIndex.ToString();
                        case 2:
                            return portItem.Value?.ToString() ?? "";
                        default:
                            break;
                    }
                }
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return false;
        }
    }
}
