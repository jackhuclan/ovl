using HandyControl.Controls;
using InteVega.CBB.SystemConfig.Ui.View;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace InteVega.CBB.SystemConfig.Ui.PropertyEditor
{
    public class NumericPropertyEditor : PropertyEditorBase
    {
        public override FrameworkElement CreateElement(PropertyItem propertyItem)
        {
            NumericControl numericControl = new NumericControl();
           
            Type menuInfo = propertyItem.Value.GetType();
            foreach (var item in menuInfo.GetProperties())
            {
                if (item.Name.Equals(propertyItem.PropertyName))
                {
                    var numberic = item.GetCustomAttribute<NumbericAttribute>();

                    numericControl.Minimum = numberic.Minimum;
                    numericControl.Maximum = numberic.Maximum;
                    numericControl.DecimalPlaces=numberic.DecimalPlaces;
                    numericControl.InitControl();
                }
            }
            numericControl.CallbackValue = val => {
                foreach (var item in menuInfo.GetProperties())
                {
                    if (item.Name.Equals(propertyItem.PropertyName))
                    {
                        if (numericControl.DecimalPlaces == 0)
                        {
                            item.SetValue(propertyItem.Value, (int)val);
                        }
                        else 
                        {
                            item.SetValue(propertyItem.Value, val);
                        }

                    }
                }
            };
            return numericControl;
        }

        public override DependencyProperty GetDependencyProperty() => System.Windows.Controls.TextBox.TextProperty;
    }
}
