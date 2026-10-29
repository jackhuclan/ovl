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
using Marvel.DataModels.Enums;

using Ovl.UI.Recipe.ViewModel;

namespace Ovl.UI.Recipe.View;

/// <summary>
/// RunParamView.xaml 的交互逻辑
/// </summary>
public partial class RunParamView : UserControl
{
    ~RunParamView()
    {
    }
    public RunParamView()
    {
        InitializeComponent();
        if (!IsAddJob)
            Margin = new Thickness(20);
        cbRunOptimizeMode.Items.Add("Minimum Load");
        cbRunOptimizeMode.Items.Add("Shortest Path");
    }

    public bool IsAddJob
    {
        get;set;
    }

    public class OptimizeModeInfo
    {
        public OptimizeModeInfo(string desc, EnumRunOptimizeMode model)
        {

        }
    }

    private void RadioButton_Checked(object sender, RoutedEventArgs e)
    {
        if (DataContext is RunParamViewModel vm)
        {
            vm.ApplyTisModeUiConstraints();
        }
    }
}
