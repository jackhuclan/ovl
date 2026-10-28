namespace IntellVega.CBB.EasyConfig
{
   
    /// <summary>
    /// 配置集创建工厂
    /// </summary>
    public interface IEasyConfigCollectionFactory
    {
        /// <summary>
        /// 不指定名称，创建默认配置集合
        /// </summary>
        /// <returns></returns>
        IEasyConfigCollection Create();
        /// <summary>
        /// 使用指定配置集名称，创建创建配置集合。配置集名称是唯一的。
        /// </summary>
        /// <returns></returns>
        IEasyConfigCollection Create(string collectionName);

        /// <summary>
        /// 使用指定配置集名称，创建创建配置集合。配置集名称是唯一的。
        /// </summary>
        /// <returns></returns>
        IEasyConfigCollection Create<T>();

    }


}