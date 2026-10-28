using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace IntellVega.CBB.EasyConfig
{
    /// <summary>
    /// 
    /// </summary>
    public static class ConfigServiceCollectionExtensions
    {

        /// <summary>
        /// ServiceCollection扩展方法，添加配置
        /// </summary>
        /// <param name="services"></param>
        /// <param name="builderAction"></param>
        /// <returns></returns>
        public static IServiceCollection AddEasyConfig(this IServiceCollection services, Action<EasyConfigFactoryBuilder>  builderAction=null)
        {
            var builder = new EasyConfigFactoryBuilder();

            builderAction?.Invoke(builder);


            //注入工厂单例
            services.TryAddSingleton<IEasyConfigCollectionFactory>(builder.Build());
            //将默认配置集注入为单例
            services.TryAddSingleton<IEasyConfigCollection>(p=>
                p.GetRequiredService<IEasyConfigCollectionFactory>().Create());
            //其余泛型配置集注入为单例
            services.TryAddSingleton(typeof(IEasyConfigCollection<>), typeof(EasyConfigCollection<>));
            

            

            return services;
        }

    }
}