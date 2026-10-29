using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Marvel.Interfaces.Business;
using Microsoft.Extensions.DependencyInjection;

using Ovl.UI.Execute.View;

namespace Ovl.UI.Execute;

/// <summary>
/// AssitantDialogWindow.xaml 的交互逻辑
/// </summary>
public partial class AssitantDialogWindow : Window
{
    public AssitantDialogWindow()
    {
        InitializeComponent();
    }
    Action CloseBuzzer;
    public bool? ShowDialog(object content, string message,string tip)
    {
        if (content is DebugView debugview)
        {
            CloseBuzzer=debugview.CloseBuzzer;
        }
        cp.Content = content;
        txtMessage.Text = message;
        txtTip .Text = tip;
        return this.ShowDialog();

    }


    private void Button_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        this.Close();
    }

    private void Button_Click_1(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        this.Close();
    }

    private void Button_Click_2(object sender, RoutedEventArgs e)
    {
        CloseBuzzer?.Invoke();
    }
}
