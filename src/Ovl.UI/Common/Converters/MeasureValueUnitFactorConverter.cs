using System.Globalization;
using System.Windows.Data;

namespace Ovl.UI.Common.Converters
{
    public class MeasureValueUnitFactorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 && values[1] is int factor && factor != 0 && TryGetDouble(values[0], culture, out double value))
            {
                return (value / factor).ToString("0.###", culture);
            }
            return "";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException(); // 仅显示，无需反向转换
        }

        /// <summary>
        /// Ovl_X/Ovl_Y 为 object，可能是 boxed double；Misreg 为 double。统一按数值换算单位。
        /// </summary>
        private static bool TryGetDouble(object input, CultureInfo culture, out double value)
        {
            switch (input)
            {
                case double d:
                    value = d;
                    return true;
                case float f:
                    value = f;
                    return true;
                case int i:
                    value = i;
                    return true;
                default:
                    return double.TryParse(System.Convert.ToString(input, culture), NumberStyles.Any, culture, out value);
            }
        }
    }
}
