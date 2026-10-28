using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Prism.DryIoc;
using Prism.Ioc;
using DryIoc;
using Microsoft.Extensions.DependencyInjection;
using IntellVega.CBB.Module;
using InteVega.CBB.SystemConfig.Ui;
using IntellVega.CBB.EasyConfig;
using IntellVega.CBB.EasyConfig.Json;
using InteVega.CBB.Prism.UI.Extensions;

namespace Sample
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication
    {
        //protected override Window CreateShell()
        //{
        //    return Container.Resolve<MainWindow>();
        //}
        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterServices(RegisterTypesToServiceCollection);
            containerRegistry.AddSystemConfigModule();
            containerRegistry.AddPrismUIExtensionModule();
            containerRegistry.RegisterForNavigation<MainWindow, MainWindowViewModel>();
        }
        private static void RegisterTypesToServiceCollection(IServiceCollection serviceCollection)
        {
            serviceCollection.AddModuleManager(m => m.AddModule<SystemConfigUIModule>());
            serviceCollection.AddEasyConfig(b => b.UseJson(op => op.SetAutoCommit(true)));
        }
        //protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        //{
        //    var moduleAType = typeof(TestModule);
        //    moduleCatalog.AddModule(new ModuleInfo()
        //    {
        //        ModuleName = moduleAType.Name,
        //        ModuleType = moduleAType.AssemblyQualifiedName,
        //        InitializationMode = InitializationMode.WhenAvailable
        //    });
        //}
    }
}
