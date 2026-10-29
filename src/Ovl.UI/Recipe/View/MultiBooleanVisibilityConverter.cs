using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Ovl.UI.Recipe.View;

/// <summary>
/// Multi-value converter that handles combined visibility conditions
/// When IsRename is true, always returns Collapsed
/// Otherwise, returns the visibility based on the second value (IsSingleFace/IsDoubleFace)
/// </summary>
public class MultiBooleanVisibilityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        // First value is IsRename - if true, always hide
        if (values.Length > 0 && values[0] is bool isRename && isRename)
        {
            return Visibility.Collapsed;
        }

        // Second value is the condition (IsSingleFace or IsDoubleFace)
        // If true, show; otherwise, hide
        if (values.Length > 1 && values[1] is bool condition)
        {
            return condition ? Visibility.Visible : Visibility.Collapsed;
        }

        // Default to collapsed if not enough values or wrong types
        return Visibility.Collapsed;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
