using HandyControl.Controls;
using InteVega.CBB.SystemConfig;
using InteVega.CBB.SystemConfig.Interface;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Sample.Module
{
    [DisplayName("测试类1")]
    public class TestModel1 : ISystemConfig
    {
        [DisplayName("名字")]
        public string Name { get; set; }
        [DisplayName("地址"), EditorType(EnumEditorType.SelectedProperty)]
        public string Path { get; set; }
        [DisplayName("时间"), EditorType(EnumEditorType.DateTimeProperty)]
        public DateTime CreateDate { get; set; }
        [Browsable(false)]
        [DisplayName("子类")]
        public ChildModel1 ChildModel1 { get; set; }
        [DisplayName("图片")]
        public ImageSource ImageBrush { get; set; }
        [DisplayName("asdf")]
        [Browsable(false)]
        public List<TestInfo> TestList { get; set; }
    }
    [DisplayName("子类")]
    public class ChildModel1 : ISystemConfig
    {
        [DisplayName("备注"), EditorType(EnumEditorType.SelectedProperty)]
        public string Remark { get; set; }
        [Browsable(false)]
        [DisplayName("子类-子类")]
        public ChildModelNew ChildModelNew { get; set; }
    }
    public class TestInfo: ISystemConfigList
    {
        [DisplayName("123")]
        public int Id { get; set; }
        [DisplayName("122223")]
        public string TestCode { get; set; }
        //[Browsable(false)]
        //public string Name { get; set; }
    }
    public class ChildModelNew : ISystemConfig
    {
        [DisplayName("级别")]
        public string Remark { get; set; }
    }
}
