using System.Collections.Generic;

namespace IntellVega.CBB.EasyConfig
{
    /// <summary>
    /// 增加一个泛型实现，目的是方便直接从IOC容器中获取配置集，简化调用方式
    /// </summary>
    /// <typeparam name="TCategory">类型，作为配置集的名称</typeparam>
    public interface IEasyConfigCollection<out TCategory> : IEasyConfigCollection
    {

    }


    /// <summary>
    /// 对配置集的Category做了一个泛型的封装，方便注入使用。
    /// </summary>
    /// <typeparam name="TCategory"></typeparam>
    public class EasyConfigCollection<TCategory> : IEasyConfigCollection<TCategory> 
    {
        private readonly IEasyConfigCollection _easyConfigCollection;

        /// <inheritdoc />
        public string Category => _easyConfigCollection?.Category;
        /// <inheritdoc />
        public EasyConfigCollection(IEasyConfigCollectionFactory collectionFactory)
        {
            _easyConfigCollection = collectionFactory.Create<TCategory>();
        }


        /// <inheritdoc />
        public void ReLoad()
        {
            _easyConfigCollection.ReLoad();
        }


        /// <inheritdoc />
        public void Commit()
        {
            _easyConfigCollection.Commit();
        }

        /// <inheritdoc />
        public TConfig Get<TConfig>() 
        {
            return _easyConfigCollection.Get<TConfig>();
        }

        /// <inheritdoc />
        public TConfig Get<TConfig>(string key)
        {
            return _easyConfigCollection.Get<TConfig>(key);
        }


        /// <inheritdoc />
        public bool Set<TConfig>(TConfig config)
        {
            return _easyConfigCollection.Set(config);
        }

        /// <inheritdoc />
        public bool Set<TConfig>(string key, TConfig config)
        {
            return _easyConfigCollection.Set(key,config);
        }

        public IEnumerable<object> GetAll()
        {
            return _easyConfigCollection.GetAll();
        }
    }
}