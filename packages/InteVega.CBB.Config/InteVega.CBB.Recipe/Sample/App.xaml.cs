using Prism.DryIoc;
using Prism.Ioc;
using System.Windows;
using DryIoc;
using Microsoft.Extensions.DependencyInjection;
using IntellVega.CBB.Module;
using IntellVega.CBB.EasyConfig;
using IntellVega.CBB.EasyConfig.Json;
using InteVega.CBB.Prism.UI.Extensions;
using System.Windows;
using InteVega.CBB.Recipe.Ui;
using InteVega.CBB.Recipe.Model;

namespace Sample
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication
    {
        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterServices(RegisterTypesToServiceCollection);
            containerRegistry.AddRecipeModule();
           
            containerRegistry.AddPrismUIExtensionModule();
            containerRegistry.RegisterForNavigation<MainWindow, MainWindowViewModel>();
        }
        private static void RegisterTypesToServiceCollection(IServiceCollection serviceCollection)
        {
            serviceCollection.AddModuleManager(m => m.AddModule<ReccipeUIModule>());
            serviceCollection.AddEasyConfig(b => b.UseJson(op => op.SetAutoCommit(true)));
           // serviceCollection.AddInteVegaDbContextDisplayModule();
        }
    }
}
