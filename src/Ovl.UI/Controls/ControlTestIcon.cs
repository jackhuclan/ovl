using Marvel.DataModels.Enums;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using InteVega.CBB.ComUI.Helpers;

namespace Ovl.UI.Controls
{
    public class ControlTestIcon : UserControl
    {
    }


    /// <summary>
    /// Path的画刷转换器
    /// </summary>
    public class PathBrushConverter : IValueConverter
    {
        /// <summary>
        /// 是否取反
        /// </summary>
        public bool IsInverse { get; set; }

        /// <summary>
        /// 是否是外圈
        /// </summary>
        public bool IsOutter { get; set; }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is EnumTargetType type && parameter != null)
            {
                string color = parameter.ToString();
                var brush = ConvertHelper.BrushFromString(color);
                bool isVisible = CheckVisible(type);
                if (IsInverse)
                {
                    isVisible = !isVisible;
                }
                return isVisible ? brush : null;
            }
            else
            {
                return null;
            }
        }

        private bool CheckVisible(EnumTargetType type)
        {
            switch (type)
            {
                case EnumTargetType.Bar:
                case EnumTargetType.Frame:
                case EnumTargetType.LAngle:
                    return false;
                case EnumTargetType.Circle:
                    return !IsOutter;
                default:
                    return true;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class MarginConveter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is EnumTargetType type)
            {
                switch (type)
                {
                    case EnumTargetType.Cross:
                    case EnumTargetType.AIM:
                    case EnumTargetType.AIM_XYS:
                    case EnumTargetType.LS:
                    case EnumTargetType.LAngle:
                        return new Thickness();
                }

            }
            return new Thickness(10);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
