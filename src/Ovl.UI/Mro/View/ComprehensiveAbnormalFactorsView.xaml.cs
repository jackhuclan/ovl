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
public partial class ComprehensiveAbnormalFactorsView : UserControl
{
    public ComprehensiveAbnormalFactorsView(IServiceProvider serviceProvider, RecipeElementsInfo recipeElementsInfo, MroElementInfo mroElementInfo)
    {
        InitializeComponent();
        this.DataContext = new ComprehensiveAbnormalFactorsViewModel(serviceProvider, recipeElementsInfo, mroElementInfo);
    }

}
