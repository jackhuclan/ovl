using InteVega.CBB.SystemConfig.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace InteVega.CBB.SystemConfig
{
    public class ConfigLoader:IConfigLoader 
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ConfigLoader> _logger;

        public ConfigLoader(IServiceProvider serviceProvider,ILogger<ConfigLoader> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public TConfig LoadConfig<TConfig>(TConfig configInstance = null) where TConfig : class, ISystemConfig, new()
        {
            var lCfg = configInstance ?? GetConfig<TConfig>();
            if (lCfg == null)
            {
                _logger.LogDebug($"config is null, {typeof(TConfig)}");
                var configService = _serviceProvider.GetService<ISystemConfigService>();
                configService.SetConfig(new TConfig());
            }

            return lCfg;
        }

        private TConfig GetConfig<TConfig>() where TConfig : class, ISystemConfig
        {
            return _serviceProvider.GetService<ISystemConfigService>().GetConfig<TConfig>();
        }
    }

}
