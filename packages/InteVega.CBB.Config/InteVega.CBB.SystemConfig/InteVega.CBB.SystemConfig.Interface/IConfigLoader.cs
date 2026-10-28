using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InteVega.CBB.SystemConfig.Interface
{
    public interface IConfigLoader
    {
        TConfig LoadConfig<TConfig>(TConfig configInstance = null) where TConfig : class, ISystemConfig, new();
    }
}
