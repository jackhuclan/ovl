using System.Windows;
using System.Windows.Controls;

namespace Ovl.UI.Controls
{
    /// <summary>
    /// ChartResult.xaml 的交互逻辑
    /// </summary>
    public partial class ChartResult : UserControl
    {

        public ChartResult()
        {
            InitializeComponent();
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            //RadioButton radioButton = sender as RadioButton;
            //if (radioButton != null)
            //{
            //    string selectedContent = radioButton.Content.ToString();
            //    SelectedData.Value[selectedContent].PlotControl.Refresh();

            //    var newValue = SelectedData.Value[selectedContent];
            //    SelectedData.Value[selectedContent].UpdatePlot(newValue.Xs, newValue.Ys);
            //}
        }
    }


}
