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
using OVL.Algorithm.DoNet;

using Ovl.UI.Recipe.ViewModel;

namespace Ovl.UI.Recipe.View;

/// <summary>
/// RunParametersView.xaml 的交互逻辑
/// </summary>
public partial class ParametersKPIChooseView : UserControl
{
    public ParametersKPIChooseView(IServiceProvider serviceProvider, CycleParameter NowCycleParameter, RecipeElementsInfo _recipeElementsInfo, bool Enable = true,bool FirstCycle = false)
    {
        InitializeComponent();
        this.DataContext = new ParametersKPIChooseViewModel(serviceProvider, NowCycleParameter, _recipeElementsInfo, Enable, FirstCycle);
    }
}
