using HandyControl.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace InteVega.CBB.SystemConfig.Ui.PropertyEditor
{
    public class DateTimePropertyEditor : PropertyEditorBase
    {
        public override FrameworkElement CreateElement(PropertyItem propertyItem) => new DateTimePicker
        {
            //FontSize=20,
            //Text=DateTime.Now.ToString("yyyy-MM-dd")
        };

        public override DependencyProperty GetDependencyProperty() => DateTimePicker.SelectedDateTimeProperty;
    }
}
