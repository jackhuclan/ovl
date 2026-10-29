using System.Globalization;
using System.Windows.Data;
using Marvel.DataModels.Recipe40;

namespace Ovl.UI.Resources.Converter;

public class ItemTypeValueConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
            return string.Empty;

        // If the parameter is specified, check if the item is of that type
        string targetTypeName = parameter as string;
        if (!string.IsNullOrEmpty(targetTypeName))
        {
            if (!value.GetType().FullName.EndsWith(targetTypeName))
                return string.Empty;
        }

        // For TestInfo40, we want to show the FaceMode
        if (value is TestInfo40 testInfo40)
        {
            return testInfo40.FaceMode;
        }

        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
