using InteVega.CBB.Mvvm.Bases;
using Marvel.DataModels.Recipe;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;


using Ovl.UI.Recipe.ViewModel;
namespace Ovl.UI.Recipe.View
{
    /// <summary>
    /// AddArrayTargetView.xaml 的交互逻辑
    /// </summary>
    public partial class AddArrayTargetView : UserControl
    {
        public AddArrayTargetView()
        {
            InitializeComponent();
            this.Loaded += AddArrayTargetView_Loaded;
        }

        AddArrayTargetViewModel vm;
        private void AddArrayTargetView_Loaded(object sender, RoutedEventArgs e)
        {
            vm = DataContext as AddArrayTargetViewModel;
            cta.Draw(vm.Test, vm.ArrayTarget);
            vm.ArrayTarget.ActionPropertyChanged = Redraw;
            
        }

        private void Redraw(PropertyChangeArg arg)
        {
            if (arg == null || arg.PropertyName == nameof(TargetArrayLocation.RowCount) || arg.PropertyName == nameof(TargetArrayLocation.ColumnCount))
                cta.Draw(vm.Test, vm.ArrayTarget);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            vm.ArrayTarget.SelectedTargets.Clear();
            vm.ArrayTarget.SelectedTargets.AddRange(cta.GetSelectedTargets());
        }

        private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            cta .SelectAll();
        }

        private void BtnClearAll_Click(object sender, RoutedEventArgs e)
        {
            cta.UnSelectAll();
        }

        private void NumericUpDown_ValueChanged(object sender, HandyControl.Data.FunctionEventArgs<double> e)
        {
            if(!IsLoaded ) return;
            Redraw(null);
        }
    }
}
