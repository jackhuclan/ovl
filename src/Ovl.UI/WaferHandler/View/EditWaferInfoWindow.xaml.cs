using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using Ovl.UI.WaferHandler.ViewModel;

namespace Ovl.UI.WaferHandler.View;

/// <summary>
/// EditWaferInfoWindow.xaml 的交互逻辑
/// </summary>
public partial class EditWaferInfoWindow : Window
{
    public EditWaferInfoWindow()
    {
        InitializeComponent();
    }
    public EditWaferInfoWindow(EditWaferInfoViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseAction = result => DialogResult = result;
    }
}
