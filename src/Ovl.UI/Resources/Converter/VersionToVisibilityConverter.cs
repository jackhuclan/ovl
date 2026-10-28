using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Marvel.DataModels.Core;
using Marvel.DataModels.Enums;

namespace Ovl.UI.Resources.Converter
{
    /// <summary>
    /// Provides visibility based on whether the application's current version is OM40.
    /// Static property is used for direct access in XAML for elements that don't support
    /// standard binding (like DataGridTemplateColumn).
    /// </summary>
    public class VersionToVisibilityConverter
    {
        /// <summary>
        /// Static property that returns Visibility.Visible if the current version is OM40,
        /// otherwise returns Visibility.Collapsed
        /// </summary>
        public static Visibility OM40Visibility
        {
            get
            {
                return MarvelVersion.CurrentVersion == EnumOMVersion.OM40 
                    ? Visibility.Visible 
                    : Visibility.Collapsed;
            }
        }
    }
}
