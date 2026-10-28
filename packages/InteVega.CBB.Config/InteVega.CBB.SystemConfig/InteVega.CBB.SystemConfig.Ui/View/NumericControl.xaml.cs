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

namespace InteVega.CBB.SystemConfig.Ui.View
{
    /// <summary>
    /// NumericControl.xaml 的交互逻辑
    /// </summary>
    public partial class NumericControl : UserControl
    {
        public double Value { get; set; }
        public double Minimum { get; set; }
        public double Maximum { get; set; }
        public int DecimalPlaces { get; set; }
        public Action<double> CallbackValue = null;
        public NumericControl()
        {
            InitializeComponent();
        }
        public void InitControl() 
        {
            numUpDown.Minimum = Minimum;
            numUpDown.Maximum = Maximum;
            numUpDown.Value = Value;
            numUpDown.DecimalPlaces= DecimalPlaces;
            
        }

        private void numUpDown_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            CallbackValue?.Invoke(numUpDown.Value);
        }
    }
}
