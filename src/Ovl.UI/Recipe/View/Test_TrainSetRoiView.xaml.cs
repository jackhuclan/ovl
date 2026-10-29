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
/// Test_TrainSetRoiView.xaml 的交互逻辑
/// </summary>
public partial class Test_TrainSetRoiView : UserControl
{
    public Test_TrainSetRoiView()
    {
        InitializeComponent();
        this.IsVisibleChanged += Test_TrainSetRoiView_IsVisibleChanged;
    }

    private void Test_TrainSetRoiView_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        var vm = this.DataContext as Test_TrainSetRoiViewModel;
        if (vm != null)
        {
            vm.InitFocusRoiButton();
            vm.InitMeasurementRoiButton();
        }
    }
}
