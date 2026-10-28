
namespace InteVega.CBB.SystemConfig.Interface
{

    public interface ISystemConfigService
    {
        List<ISystemConfig> GetAllConfigs();
        /// <summary>
        /// 每种系统配置只有一个实例
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T GetConfig<T>() where T : ISystemConfig;

        bool SetConfig<T>(T config);


    }

}