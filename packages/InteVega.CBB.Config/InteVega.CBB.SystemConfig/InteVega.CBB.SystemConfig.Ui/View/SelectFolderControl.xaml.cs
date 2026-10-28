using InteVega.CBB.Prism.UI.Extensions.MessageBox;
using Microsoft.Win32;
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
using static System.Net.Mime.MediaTypeNames;

namespace InteVega.CBB.SystemConfig.Ui.View
{
    /// <summary>
    /// SelectFolderControl.xaml 的交互逻辑
    /// </summary>
    public partial class SelectFolderControl : UserControl
    {
        public string Path { get; set; }
        public Action<string> SelectAction = null;

        public SelectFolderControl()
        {
            InitializeComponent();
        }

        private void btnSelect_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string filename = folderBrowserDialog.SelectedPath;

                Path = filename;
                txtPath.Text = filename;
                SelectAction?.Invoke(Path);
            }
        }
        public void SetValue(string path) 
        {
            Path = path;
            txtPath.Text = path;
        }
    }
}
