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

using Ovl.UI.Recipe.ViewModel;

namespace Ovl.UI.Recipe.View;

/// <summary>
/// Test_TrainFocusPredictionView.xaml 的交互逻辑
/// </summary>
public partial class Test_TrainFocusPredictionView : UserControl
{
    public Test_TrainFocusPredictionView()
    {
        InitializeComponent();
        this.IsVisibleChanged += Test_TrainFocusPredictionViewModel_IsVisibleChanged;
    }

    private void Test_TrainFocusPredictionViewModel_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        var vm = this.DataContext as Test_TrainFocusPredictionViewModel;
        if (vm != null)
        {
            //vm.InitFocusRoiButton();
            //vm.InitMeasurementRoiButton();
        }
    }
}
