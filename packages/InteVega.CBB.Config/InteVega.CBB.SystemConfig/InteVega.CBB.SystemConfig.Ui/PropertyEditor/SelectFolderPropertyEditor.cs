using HandyControl.Controls;
using InteVega.CBB.SystemConfig.Ui.View;
using System;
using System.Windows;
using System.Windows.Controls;

namespace InteVega.CBB.SystemConfig.Ui.PropertyEditor
{
    public class SelectFolderPropertyEditor : PropertyEditorBase
    {
        //public override FrameworkElement CreateElement(PropertyItem propertyItem) => new SelectFolderControl
        //{
        //    SelectAction = (val) =>
        //    {
        //        object d = typeof(SelectFolderPropertyEditor);
        //        Type menuInfo = propertyItem.Value.GetType();
        //        foreach (var item in menuInfo.GetProperties())
        //        {
        //            if (item.Name.Equals(propertyItem.PropertyName))
        //            {
        //                item.SetValue(propertyItem.Value, val);
        //            }
        //        }
        //    }
        //};
        public override FrameworkElement CreateElement(PropertyItem propertyItem)
        {
            SelectFolderControl selectFolderControl=  new SelectFolderControl
            {
                SelectAction = (val) =>
                {
                    Type menuInfo = propertyItem.Value.GetType();
                    foreach (var item in menuInfo.GetProperties())
                    {
                        if (item.Name.Equals(propertyItem.PropertyName))
                        {
                            item.SetValue(propertyItem.Value, val);
                        }
                    }
                }
            };
            string value = "";
            Type menuInfo = propertyItem.Value.GetType();
            foreach (var item in menuInfo.GetProperties())
            {
                if (item.Name.Equals(propertyItem.PropertyName))
                {
                    value = item.GetValue(propertyItem.Value).ToString();
                }
            }
            selectFolderControl.SetValue(value);
            return selectFolderControl;
        }
        public override DependencyProperty GetDependencyProperty() => System.Windows.Controls.TextBox.TextProperty;
    }

}
