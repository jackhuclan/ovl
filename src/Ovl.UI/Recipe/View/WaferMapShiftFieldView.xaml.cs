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
/// WaferMapShiftFieldView.xaml 的交互逻辑
/// </summary>
public partial class WaferMapShiftFieldView : UserControl
{
    public WaferMapShiftFieldView()
    {
        InitializeComponent();
        this.Loaded += WaferMapShiftFieldView_Loaded;
    }
    WaferMapShiftFieldViewModel vm;
    private void WaferMapShiftFieldView_Loaded(object sender, RoutedEventArgs e)
    {
       vm=this .DataContext as WaferMapShiftFieldViewModel;
        wmc.Draw(vm.WaferMapInfo);
    }

    private void CheckBoxFieldCenter_Click(object sender, RoutedEventArgs e)
    {
        wmc.Draw(vm.WaferMapInfo);
    }
}
