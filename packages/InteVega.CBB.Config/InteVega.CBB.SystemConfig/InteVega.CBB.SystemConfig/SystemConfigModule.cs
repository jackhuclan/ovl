using IntellVega.CBB.Module;
using InteVega.CBB.SystemConfig.Interface;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InteVega.CBB.SystemConfig
{
    public class SystemConfigModule : IReleasableModule
    {
        public void Init(IServiceProvider provider)
        {
        }

        public IServiceCollection Register(IServiceCollection services)
        {
            services.AddSingleton<ISystemConfigService, SystemConfigService>();
            services.AddSingleton<ISystemConfigCollection, SystemConfigCollection>();
            services.AddSingleton<IConfigLoader, ConfigLoader>();
            return services;
        }

        public void Release(IServiceProvider provider)
        {
        }
    }
}
