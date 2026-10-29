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
using Marvel.Interfaces.Application;
using Microsoft.Extensions.DependencyInjection;
using Prism.Ioc;

using Ovl.UI.Recipe.ViewModel;

namespace Ovl.UI.Recipe.View;

/// <summary>
/// Test_Train_CD_View.xaml 的交互逻辑
/// </summary>
public partial class Test_Train_CD_View : UserControl
{
    public Test_Train_CD_View()
    {
        InitializeComponent();
        SetImageViewer();
    }


    private void SetImageViewer()
    {

        if (this.DataContext is Test_TrainCDViewModel vm)
        {
            vm.HpImageViewer = StageControlImageView?.ImageViewer;
            vm.TemplateControl1  = paramView1.modelTemplateControl;
            vm.TemplateControl2 = paramView2.modelTemplateControl;


        }
        var _serviceProvider = ContainerLocator.Current.Resolve<IServiceProvider>();
        var alignmentService = _serviceProvider.GetService<IAlignmentService>();
        wmsc.Init(alignmentService, alignmentService.WaferMapInfo);
    }
}
