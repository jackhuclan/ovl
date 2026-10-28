using IntellVega.CBB.EasyConfig;
using InteVega.CBB.SystemConfig.Interface;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;


namespace InteVega.CBB.SystemConfig
{
    /// <summary>
    /// 系统配置接口的实现
    /// </summary>
    public class SystemConfigCollection : ISystemConfigCollection
    {
        string id = "SystemConfig";
        /// <summary>
        /// 实际上使用easy config实现，但是对内屏蔽easy config
        /// </summary>
        private readonly IEasyConfigCollection<List<ISystemConfig>> _configCollection;

        public SystemConfigCollection(IServiceProvider provider)
        {
           
            _configCollection = provider.GetService<IEasyConfigCollection<List<ISystemConfig>>>();
        }

        public List<ISystemConfig> GetAllConfigs()
        {
            IEnumerable<object> dataList = _configCollection.GetAll();
            List<ISystemConfig> list = new List<ISystemConfig>();
            if (dataList != null)
            {
                foreach (var item in dataList.Where(d => d.GetType().GetInterface(typeof(ISystemConfig).Name)!=null))
                {
                    list.Add(item as ISystemConfig);
                }
            }
            return list;
        }

        public T Get<T>()
        {
            return _configCollection.Get<T>();
        }
        public bool Set<T>(T config)
        {
            //_configCollection.Set(id, new TestModel1
            //{
            //    Name = "abc",
            //    ChildModel1 = new ChildModel1
            //    {
            //        Remark = "cas",
            //        ChildModelNew = new ChildModelNew
            //        {
            //            Remark = "asdfd"
            //        }
            //    }
            //});
            //_configCollection.Set(id, new TestModel2 { AxisX = 23 });
            //_configCollection.Set(id, new TestModel3 { Type = "adf3" });
           
            return _configCollection.Set(config);
        }
    }
}