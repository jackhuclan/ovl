using InteVega.CBB.Prism.UI.Extensions.MessageBox;
using Marvel.DataModels.Recipe;
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
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Ovl.UI.Recipe.ViewModel;
using Ovl.UI.ViewModels.Recipe.ViewModel;
namespace Ovl.UI.Recipe.View
{
    /// <summary>
    /// TestManageView.xaml 的交互逻辑
    /// </summary>
    public partial class TestManageView : UserControl
    {
        ~TestManageView() 
        {
        }
        public TestManageView()
        {
            InitializeComponent();
            this.Loaded += MainView_Loaded;
            UseTheScrollViewerScrolling(Grid_content);
        }
        public void UseTheScrollViewerScrolling(FrameworkElement fElement)
        {
            fElement.PreviewMouseWheel += (sender, e) =>
            {
                var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                eventArg.RoutedEvent = UIElement.MouseWheelEvent;
                eventArg.Source = sender;
                fElement.RaiseEvent(eventArg);
            };
        }



        private void MainView_Loaded(object sender, RoutedEventArgs e)
        {
            var vm = (this.DataContext as TestManageViewModel);

            UpdateList(vm);
            vm.ActionCollectionChanged = () =>
            {
                UpdateList(vm);

            };
        }

        private void UpdateList(TestManageViewModel vm)
        {
            Dispatcher.Invoke(() =>
            {
                var selectedTest = vm.SelectTestRowData;
                testinfos.ItemsSource = null;
                testinfos.ItemsSource = vm.TestManageList;
                if (vm.TestManageList.Contains(selectedTest))
                {
                    vm.SelectTestRowData  = selectedTest;
                }
                else if (vm.TestManageList.Count > 0)
                {
                    vm.SelectTestRowData = vm.TestManageList[0];
                }
                else
                {
                    vm.SelectTestRowData=null;
                }
            });
        }

        //private void testinfos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    // 检查是否选中了某一行
        //    if (sender is DataGrid dataGrid && dataGrid.SelectedItem != null)
        //    {
        //        // 获取选中的行的数据对象
        //        var selectedRowData = dataGrid.SelectedItem;

        //        // 调用 ViewModel 中的方法，传递选中的行的数据对象
        //        if (DataContext is TestManageViewModel viewModel)
        //        {
        //            viewModel.OpenTest();
        //        }
        //    }
        //}

        private void nextpage(object sender, RoutedEventArgs e)
        {
            if (TestTarget.IsChecked == true)
            {
                TestPysical.IsChecked = true;
            }
            else if (TestPysical.IsChecked == true)
            {
                TestDataCulling.IsChecked = true;
            }
            else if (TestDataCulling.IsChecked == true)
            {
                TestTis.IsChecked = true;
            }
            else if (TestTis.IsChecked == true)
            {
                TestTarget.IsChecked = true;
            }
        }

        private void previousepage(object sender, RoutedEventArgs e)
        {
            if (TestTarget.IsChecked == true)
            {
                TestTis.IsChecked = true;
            }
            else if (TestPysical.IsChecked == true)
            {
                TestTarget.IsChecked = true;
            }
            else if (TestDataCulling.IsChecked == true)
            {
                TestPysical.IsChecked = true;
            }
            else if (TestTis.IsChecked == true)
            {
                TestDataCulling.IsChecked = true;
            }
        }

        /// <summary>
        /// 右键点击同时选中改行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void testinfos_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            DependencyObject source = e.OriginalSource as DependencyObject;

            // 如果事件源不是 Visual 或 Visual3D，则不做处理
            if (!(source is Visual) && !(source is Visual3D))
            {
                return;
            }

            DataGridRow row = FindDataGridRow(source);
            if (row != null)
            {
                row.IsSelected = true;
                ChangeSelectedTest();
            }
        }
        private DataGridRow FindDataGridRow(DependencyObject current)
        {
            while (current != null && !(current is DataGridRow))
            {
                current = VisualTreeHelper.GetParent(current);
            }

            return current as DataGridRow;
        }

        private void testinfos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ChangeSelectedTest();
        }

        private void ChangeSelectedTest()
        {
            var vm = (this.DataContext as TestManageViewModel);
            vm.OpenTest(testinfos.SelectedItem as TestManageInfo);
        }

        private void Test_TargetView_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void Test_DataCulling_New_Loaded(object sender, RoutedEventArgs e)
        {
            var vm = (this.DataContext as TestManageViewModel);
            if (vm.TestManageList.Count > 0)
                vm.OpenTest(vm.TestManageList[0]);
        }
    }
}
