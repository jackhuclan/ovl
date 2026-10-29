using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Ovl.UI.Common.Controls;

using Ovl.UI.DataDisplay.ViewModel;
using Ovl.UI.Measure;
namespace Ovl.UI.Views.Measure;

/// <summary>
/// 主量测页（原样迁移自 Marvel.App.Shared.view.mainview，样式零改动）。
/// 代码后置仅承担三件原版由 Prism 容器/框架完成的事：
/// 1. 把 Wafer Map 占位控件实例注入 VM（原版 VM 直接持有控件实例）；
/// 2. 为测点占位视图准备演示数据；
/// 3. 给 BasicData 面板注入独立 DataContext（原版经 Prism ContainerProvider 解析）。
/// </summary>
public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Unloaded += (_, _) => (DataContext as IDisposable)?.Dispose();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is not MainViewModel viewModel)
        {
            return;
        }

        if (viewModel.MisregWaferMapControl is null)
        {
            viewModel.MisregWaferMapControl = CreateWaferMap();
        }

        if (viewModel.QmeritWaferMapControl is null)
        {
            viewModel.QmeritWaferMapControl = CreateWaferMap();
        }

        if (BasicDataPanel.DataContext is null && AppHost.Services is { } services)
        {
            BasicDataPanel.DataContext = services.GetRequiredService<BasicDataViewModel>();
        }
    }

    private static WaferMapView CreateWaferMap()
    {
        var map = new WaferMapView();
        var points = new List<WaferMapView.MapPoint>();
        var random = new Random(20261028);
        for (var column = 0; column < 16; column++)
        {
            for (var row = 0; row < 20; row++)
            {
                var state = random.Next(10) switch
                {
                    < 7 => WaferMapView.PointState.Pass,
                    < 9 => WaferMapView.PointState.Unmeasured,
                    _ => WaferMapView.PointState.Fail,
                };
                points.Add(new WaferMapView.MapPoint(column, row, state));
            }
        }

        map.PointsSource = points;
        return map;
    }
}
