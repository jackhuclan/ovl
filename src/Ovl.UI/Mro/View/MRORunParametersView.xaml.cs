using Marvel.DataModels.Recipe;
using Marvel.MRO.DataModels.Models;
using Megarobo.Ovl.NumericWheel;
using System;
using System.Collections.Generic;
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
    public partial class MRORunParametersView : UserControl
    {
        public MRORunParametersView(IServiceProvider serviceProvider, RecipeElementsInfo recipeElementsInfo, MroElementInfo mroElementInfo)
        {
            InitializeComponent();
            this.DataContext = new MRORunParametersViewModel(serviceProvider, recipeElementsInfo, mroElementInfo);
        }

        private void NumericUpDown_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            NumericWheel.MouseWheel(sender, e);
        }
    }
}
