using IntellVega.CBB.Module;
using InteVega.CBB.SystemConfig.Interface;
using InteVega.CBB.SystemConfig.Ui.View;
using InteVega.CBB.SystemConfig.Ui.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InteVega.CBB.SystemConfig.Ui
{
    public static class SystemConfigContainerModule
    {
        public static void AddSystemConfigModule(this IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterServices(d => {  });
           
            containerRegistry.RegisterDialog<SystemConfigWin, SystemConfigWinViewModel>();
            //containerRegistry.RegisterSingleton<IDialogSystemWindow, DialogSystemWindow>();
        }
        //[Obsolete]
        //public static IServiceCollection AddInteVegaSystemConfigDisplayModule(this IServiceCollection services) 
        //{
        //    services.AddSingleton<ISystemConfigService, SystemConfigService>();
        //    services.AddTransient<PropertyControlViewModel>();
        //    return services;
        //}
    }
    [Depend(typeof(SystemConfigModule))]
    public class SystemConfigUIModule : IReleasableModule
    {
     
        public void Init(IServiceProvider provider)
        {
        }

        public IServiceCollection Register(IServiceCollection services)
        {
            services.AddSingleton<IDialogSystemWindow, DialogSystemWindow>();
            services.AddTransient<PropertyControlViewModel>();
            return services;
        }

        public void Release(IServiceProvider provider)
        {
        }
    }
}
