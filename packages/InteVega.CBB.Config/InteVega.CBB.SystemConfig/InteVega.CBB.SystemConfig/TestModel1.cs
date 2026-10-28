using InteVega.CBB.SystemConfig.Interface;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InteVega.CBB.SystemConfig
{
    [DisplayName("测试类1")]
    public class TestModel1: ISystemConfig
    {
        [DisplayName("名字")]
        // type.AssemblyQualifiedName
        public string Name { get; set; }
        [DisplayName("地址"), Editor(UserControlConfig.SelectFolderPropertyEditor, UserControlConfig.PropertyEditorBase)]
        public string Path { get; set; }
        [DisplayName("时间"), Editor(UserControlConfig.DateTimePropertyEditor,UserControlConfig.PropertyEditorBase)]
        public DateTime CreateDate { get; set; }
        [Browsable(false)]
        [DisplayName("子类")]
        public ChildModel1 ChildModel1 { get; set; }
        //public void d() 
        //{
        //    string d= typeof(TestModel1).AssemblyQualifiedName;
        //}
    }
    [DisplayName("子类")]
    public class ChildModel1 : ISystemConfig
    {
        [DisplayName("备注")]
        public string Remark { get; set; }
        [Browsable(false)]
        [DisplayName("子类-子类")]
        public ChildModelNew ChildModelNew { get; set; }
    }
    public class ChildModelNew : ISystemConfig
    {
        [DisplayName("级别")]
        public string Remark { get; set; }
    }
}
