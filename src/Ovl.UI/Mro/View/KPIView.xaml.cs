using Marvel.MRO.DataModels.Models;
using OVL.Algorithm.DoNet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Ovl.UI.Recipe.ViewModel;
namespace Ovl.UI.Recipe.View
{
    /// <summary>
    /// KPIView.xaml 的交互逻辑
    /// </summary>
    partial class KPIView : UserControl
    {
        public KPIView(CycleParameter KpiParameters)
        {
            InitializeComponent();
            this.DataContext = new KPIViewModel(KpiParameters);
            this.Unloaded += MeasurementListCollectionView_Unloaded;
            this.Loaded += MeasurementListCollectionView_loaded;
        }

        private void MeasurementListCollectionView_loaded(object sender, RoutedEventArgs e)
        {
            var viewModel = this.DataContext as KPIViewModel;
            if (viewModel != null)
            {
                viewModel.LoadOptionsCollectionChanged();
            }
        }

        private void MeasurementListCollectionView_Unloaded(object sender, RoutedEventArgs e)
        {
            var viewModel = this.DataContext as KPIViewModel;
            if (viewModel != null)
            {
                viewModel.UnLoadOptionsCollectionChanged();
            }
        }
    }

    // StringToVisibilityConverter
    partial class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.IsNullOrEmpty(value?.ToString()) ? Visibility.Collapsed : Visibility.Visible;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }


    // 布尔到Collapsed转换器
    public class BoolToCollapsedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool boolValue && boolValue)
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
