using System.Windows;
using System.Windows.Controls;
using Ovl.UI.Recipe.ViewModel.OMVersion;

namespace Ovl.UI.Recipe.View.OMVersion;

/// <summary>
/// AlignmentView.xaml 的交互逻辑
/// </summary>
public partial class AlignmentView_300 : UserControl
{
    public AlignmentView_300()
    {

        InitializeComponent();
        SetImageViewer();
        this.DataContextChanged += AlignmentView_DataContextChanged;
        this.Loaded += AlignmentView_Loaded;
    }

    private void SetImageViewer()
    {
        if (this.DataContext is AlignmentVmBase vm)
        {
            vm.HpImageViewer = StageControlImageView?.ImageViewer;
        }
    }
    private void AlignmentView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        SetImageViewer();
    }

    private void AlignmentView_Loaded(object sender, RoutedEventArgs e)
    {
        AlignmentVmBase vm = this.DataContext as AlignmentVmBase;
        wmsc.Init(vm.AlignmentService, vm.RecipeElementsInfo.WaferMap);
        vm.WaferMapStageControl = wmsc;
    }






    private void edgeSel_Checked(object sender, RoutedEventArgs e)
    {
        //bool isCoarse = edgeSel.IsChecked.Value;
        //if (isCoarse && this.DataContext is AlignmentVmBase vm)
        //{
        //    vm.MoveToCenter();
        //}
    }
}
