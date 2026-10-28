using System.ComponentModel;

namespace InteVega.CBB.SystemConfig.Interface
{
    public interface ISystemConfig
    {
    }
    public abstract class ISystemConfigList:ISystemConfig
    {
        [Browsable(false)]
        [DisplayName("Order")]
        [Category("1.Base Setting")]
        public int Sys_OrderId { get; set; }
        [Browsable(false)]
        [DisplayName("Name")]
        [Category("1、Base Setting")]
        public string Sys_Name { get; set; }
    }
}