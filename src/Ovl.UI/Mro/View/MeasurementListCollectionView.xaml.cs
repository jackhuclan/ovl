using Marvel.DataModels.Recipe;
using Marvel.MRO.DataModels.Models;
using Megarobo.Ovl.NumericWheel;
using System;
using System.Collections.Generic;
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
    /// RunParametersView.xaml 的交互逻辑
    /// </summary>
    public partial class MeasurementListCollectionView : UserControl
    {
        public MeasurementListCollectionView(IServiceProvider serviceProvider, RecipeElementsInfo recipeElementsInfo,MroElementInfo mroElementInfo)
        {
            InitializeComponent();
            this.DataContext = new MeasurementListCollectionViewModel(serviceProvider, recipeElementsInfo, mroElementInfo);
            this.Loaded += MeasurementListCollectionView_Loaded;
            this.Unloaded += MeasurementListCollectionView_Unloaded;
        }

        private void MeasurementListCollectionView_Loaded(object sender, RoutedEventArgs e)
        {
            var viewModel = this.DataContext as MeasurementListCollectionViewModel;
            if (viewModel != null)
            {
                viewModel.LoadOptionsCollectionChanged();
            }
        }

        private void MeasurementListCollectionView_Unloaded(object sender, RoutedEventArgs e)
        {
            var viewModel = this.DataContext as MeasurementListCollectionViewModel;
            if (viewModel != null)
            {
                viewModel.UnLoadOptionsCollectionChanged();
            }
        }

        private void NumericUpDown_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            NumericWheel.MouseWheel(sender, e);
        }
    }
}
