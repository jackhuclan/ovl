using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using InteVega.CBB.StageControlModule.Views;
using InteVega.CBB.StageControlModule.Views.CameraView;
using Marvel.Interfaces.Application;
using Microsoft.Extensions.DependencyInjection;
using Prism.Ioc;
using Prism.Regions;

using Ovl.UI.Recipe.ViewModel;

namespace Ovl.UI.Recipe.View;

/// <summary>
/// Test_Train40View.xaml 的交互逻辑
/// </summary>
[ContentProperty("Content")]
public partial class Test_Train40View : UserControl
{
    private readonly IRegionManager _regionManager;
    private readonly IServiceProvider _serviceProvider;

    public Test_Train40View(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
        //Loaded += Test_TrainDoubleFaceView_Loaded;
    }

    private void Test_TrainDoubleFaceView_Loaded(object sender, RoutedEventArgs e)
    {
    }

    private void CheckBox_Pattern_Tab_Clicked(object sender, RoutedEventArgs e)
    {
    }
}
