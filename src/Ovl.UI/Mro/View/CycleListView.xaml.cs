using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Marvel.DataModels.Recipe;
using Marvel.MRO.DataModels.Models;
using Microsoft.Extensions.DependencyInjection;

using Ovl.UI.Recipe.ViewModel;

namespace Ovl.UI.Recipe.View;

/// <summary>
/// RunParametersView.xaml 的交互逻辑
/// </summary>
public partial class CycleListView : UserControl
{
    public CycleListView(IServiceProvider serviceProvider, RecipeElementsInfo _recipeElementsInfo, MroElementInfo mroElementInfo)
    {
        InitializeComponent();
        this.DataContext = new CycleListViewModel(serviceProvider,_recipeElementsInfo,mroElementInfo);
        this.Unloaded += MeasurementListCollectionView_Unloaded;
        this.Loaded += MeasurementListCollectionView_loaded;
    }

    private void MeasurementListCollectionView_loaded(object sender, RoutedEventArgs e)
    {
        var viewModel = this.DataContext as CycleListViewModel;
        if (viewModel != null)
        {
            viewModel.LoadOptionsCollectionChanged();
        }
    }
    private void MeasurementListCollectionView_Unloaded(object sender, RoutedEventArgs e)
    {
        var viewModel = this.DataContext as CycleListViewModel;
        if (viewModel != null)
        {
            viewModel.UnLoadOptionsCollectionChanged();
        }
    }
}
