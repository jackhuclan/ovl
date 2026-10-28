using System.Text.Json;
using InteVega.CBB.SystemConfig.Interface;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace InteVega.CBB.SystemConfig
{
    public class SystemConfigService : ISystemConfigService
    {
        ISystemConfigCollection _systemConfigCollection;
        private readonly ILogger<SystemConfigService> _logger;

        public SystemConfigService(ISystemConfigCollection systemConfigCollection,ILogger<SystemConfigService> logger)
        {
            _systemConfigCollection = systemConfigCollection;
            _logger = logger;
        }
        public List<ISystemConfig> GetAllConfigs()
        {
            return _systemConfigCollection.GetAllConfigs();
        }

        public T GetConfig<T>() where T : ISystemConfig
        {
            var config = _systemConfigCollection.Get<T>();
            if (config == null)
            {
                _logger.LogError("The config is empty!");
            }
            return config;
        }

        public bool SetConfig<T>(T systemConfig)
        {
            if (systemConfig == null) return false;
            string json = JsonConvert.SerializeObject(systemConfig);
            _logger.LogInformation($"Save config: {systemConfig.GetType()}, data={json}");
            return _systemConfigCollection.Set(systemConfig);
        }
        
       
    }
}
