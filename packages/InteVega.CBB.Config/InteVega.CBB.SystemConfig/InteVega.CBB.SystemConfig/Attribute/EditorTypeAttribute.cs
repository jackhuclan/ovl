using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InteVega.CBB.SystemConfig
{
    public class EditorTypeAttribute:Attribute
    {
        public EnumEditorType EnumEditorType { get; set; }
        public string Key { get; set; }
        public EditorTypeAttribute(EnumEditorType enumEditorType) 
        { 
            EnumEditorType = enumEditorType;
        }
        public EditorTypeAttribute(string key)
        {
            Key = key;
        }
    }
    public enum EnumEditorType
    { 
        SelectedProperty,
        DateTimeProperty,
        NumericProperty,
    }
}
