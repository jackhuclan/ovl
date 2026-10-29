using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Marvel.DataModels.Job;

namespace Ovl.UI.Job.View;

/// <summary>
/// AddBatch.xaml 的交互逻辑
/// </summary>
public partial class AddBatchView : Window
{
    public AddBatchView(bool isFolder, string parentName, string oldName = "")
    {
        InitializeComponent();
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        txtParent.Text = parentName;
        txtOldName.Text = oldName;
        if (string.IsNullOrWhiteSpace(oldName))
        {
            if (isFolder)
            {
                Title = "Add folder";
            }
            else
            {
                Title = "Add batch";
            }
            txtOldName.Visibility = Visibility.Collapsed;
            txtOldNameTitle.Visibility = Visibility.Collapsed;
            txtNameTitle.Text = "Name:";
        }
        else
        {
            if (isFolder)
            {
                Title = "Rename folder";
            }
            else
            {
                Title = "Rename batch";
            }

        }
    }

    /// <summary>
    /// Save 按钮事件
    /// </summary>
   public  Func<string, string> funcSave;


    private void btnClose_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private void btnSave_Click(object sender, RoutedEventArgs e)
    {
        string name = txtName.Text;
        if (string.IsNullOrWhiteSpace(name))
        {
            txtError.Text = "Please enter the name!";
            return;
        }
        if (name.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0)
        {
            txtError.Text = "name are not allowed to have special characters!";
            return;
        }
        var err = funcSave(name);
        if (!string.IsNullOrWhiteSpace(err))
        {
            txtError.Text = err;
            return;
        }
        Close();
    }
}
