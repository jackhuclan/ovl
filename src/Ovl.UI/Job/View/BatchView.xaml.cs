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
using InteVega.CBB.ComUI.Helpers;
using InteVega.CBB.Mvvm.Bases;
using Marvel.DataModels.Job;

using Ovl.UI.Job.ViewModel;

namespace Ovl.UI.Job.View;

/// <summary>
/// JobQueueView.xaml 的交互逻辑
/// </summary>
public partial class BatchView : Window
{
    public BatchView()
    {
        InitializeComponent();
        vm=this .DataContext as BatchViewModel;
        vm.View = this;
    }

    BatchViewModel vm;
    private void Grid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        //ContextMenu cm = (ContextMenu)FindResource("MenuJobs");
        //cm.DataContext = vm;
        //cm.IsOpen = true;
    }

    private void treeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        var data =(TreeNodeItemBase)e.NewValue ;
        vm.SelectItem (data);
    }



    private void MenuItem_Click_AddBatch(object sender, RoutedEventArgs e)
    {
      var data=  ControlHelper.GetSenderObjDataContext<TreeNodeItemBase >(sender);
        vm.AddBatch(data);
    }

    private void MenuItem_Click_AddFolder(object sender, RoutedEventArgs e)
    {
        var data = ControlHelper.GetSenderObjDataContext<TreeNodeItemBase>(sender);
        vm.AddBatchFolder(data);
    }

    private void MenuItem_Click_Rename(object sender, RoutedEventArgs e)
    {
        var data = ControlHelper.GetSenderObjDataContext<TreeNodeItemBase>(sender);
        vm.RenameBatch(data);
    }

    private void MenuItem_Click_Delete(object sender, RoutedEventArgs e)
    {
        var data = ControlHelper.GetSenderObjDataContext<TreeNodeItemBase>(sender);
        vm.DeleteBatch(data);
    }

    private void MenuItem_Click_EditJob(object sender, RoutedEventArgs e)
    {
        var data = ControlHelper.GetSenderObjDataContext<JobItem >(sender);
        vm.SelectedJob = data;
        vm.EditJob();
    }

    private void MenuItem_Click_InsertJob(object sender, RoutedEventArgs e)
    {
        var data = ControlHelper.GetSenderObjDataContext<JobItem>(sender);
        vm.SelectedJob = data;
        vm.InsertJob();
    }

    private void MenuItem_Click_MoveDown(object sender, RoutedEventArgs e)
    {
        var data = ControlHelper.GetSenderObjDataContext<JobItem>(sender);
        vm.SelectedJob = data;
        vm.MoveDownJob();
    }

    private void MenuItem_Click_MoveUp(object sender, RoutedEventArgs e)
    {
        var data = ControlHelper.GetSenderObjDataContext<JobItem>(sender);
        vm.SelectedJob = data;
        vm.MoveUpJob ();
    }

    private void MenuItem_Click_DeleteJob(object sender, RoutedEventArgs e)
    {
        var data = ControlHelper.GetSenderObjDataContext<JobItem>(sender);
        vm.SelectedJob = data;
        vm.DeleteJob();
    }

    private void MenuItem_Click_JoinJob(object sender, RoutedEventArgs e)
    {
        var data = ControlHelper.GetSenderObjDataContext<JobItem>(sender);
        vm.SelectedJob = data;
        vm.Join ();
    }
}
