using InteVega.CBB.SystemConfig.Interface;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using InteVega.CBB.SystemConfig;

namespace Sample.Module
{
    [DisplayName("测试类2")]
    public class TestModel2:ISystemConfig
    {
        [DisplayName("AxisX"), EditorType(EnumEditorType.NumericProperty), NumbericAttribute(5,8,3)]
        public double AxisX { get; set; }
        [Category("测试"),DisplayName("AxisY"), EditorType(EnumEditorType.NumericProperty), Numberic(1.1, 5.5, 1)]
        public double AxisY { get; set; }
        [DisplayName("AxisZ"), EditorType(EnumEditorType.NumericProperty), Numberic(1, 5, 0)]
        public int AxisZ { get; set; }
    }
}
