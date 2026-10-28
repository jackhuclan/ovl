using InteVega.CBB.SystemConfig.Interface;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InteVega.CBB.SystemConfig
{
    [DisplayName("测试类3")]
    public class TestModel3: ISystemConfig
    {
        [DisplayName("分类")]
        public string Type { get; set; }
    }

}
