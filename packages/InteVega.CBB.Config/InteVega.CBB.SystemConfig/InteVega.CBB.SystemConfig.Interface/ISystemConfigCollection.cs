using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InteVega.CBB.SystemConfig.Interface
{
    public interface ISystemConfigCollection
    {
        List<ISystemConfig> GetAllConfigs();
        T Get<T>();

        /// <summary>设置,成功返回true，失败返回false。</summary>
        bool Set<T>(T config);
    }
}
