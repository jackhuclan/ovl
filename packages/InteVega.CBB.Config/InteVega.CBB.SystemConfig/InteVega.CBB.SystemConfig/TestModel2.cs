using InteVega.CBB.SystemConfig.Interface;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InteVega.CBB.SystemConfig
{
    [DisplayName("测试类2")]
    public class TestModel2:ISystemConfig
    {
        [DisplayName("AxisX")]
        public int AxisX { get; set; }
        [DisplayName("AxisY")]
        public int AxisY { get; set; }
        [DisplayName("AxisZ")]
        public int AxisZ { get; set; }
    }
}
