using System.Windows;
using System.Windows.Controls;

using Ovl.UI.Recipe.ViewModel.OMVersion;
namespace Ovl.UI.Recipe.View.OMVersion;

/// <summary>
/// AlignmentView.xaml 的交互逻辑
/// </summary>
public partial class AlignmentView_100 : UserControl
{
    ~AlignmentView_100()
    {
    }
    public AlignmentView_100()
    {

        InitializeComponent();
        SetImageViewer();

        this.Loaded += AlignmentView_Loaded;
        this.Unloaded += AlignmentView_100_Unloaded;
    }

    private void AlignmentView_Loaded(object sender, RoutedEventArgs e)
    {
        this.DataContextChanged += AlignmentView_DataContextChanged;

        AlignmentVmBase vm = this.DataContext as AlignmentVmBase;
        wmsc.Init(vm.AlignmentService, vm.RecipeElementsInfo.WaferMap);
        vm.WaferMapStageControl = wmsc;
    }

    private void AlignmentView_100_Unloaded(object sender, RoutedEventArgs e)
    {
        this.DataContextChanged -= AlignmentView_DataContextChanged;
        this.Loaded -= AlignmentView_Loaded;
        this.Unloaded -= AlignmentView_100_Unloaded;
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


    private void edgeSel_Checked(object sender, RoutedEventArgs e)
    {
        //bool isCoarse = edgeSel.IsChecked.Value;
        //if (isCoarse && this.DataContext is AlignmentVmBase vm)
        //{
        //    vm.MoveToCenter();
        //}
    }
}
