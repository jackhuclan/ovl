using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HandyControl.Controls;
using InteVega.CBB.Prism.UI.Extensions.MessageBox;
using InteVega.CBB.SystemConfig.Interface;
using InteVega.CBB.SystemConfig.Ui.PropertyEditor;
using Prism.Services.Dialogs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows.Markup;


namespace InteVega.CBB.SystemConfig.Ui.ViewModel
{
    public partial class PropertyControlViewModel: ObservableObject//BindableBase //  
    {

        [ObservableProperty]
        public MenuTreeModel _treeViewModel;
        [ObservableProperty]
        public ISystemConfig _SelectPropertyConfig;

        ISystemConfigService _systemConfigService;
        IMessageBoxService _MessageBoxService;
        public Action<bool> CloseAction;
        public PropertyControlViewModel(ISystemConfigService systemConfigService,IMessageBoxService messageBoxService)
        {
            _systemConfigService = systemConfigService;
            _MessageBoxService = messageBoxService;
            LoadMenuTree();
        }
        /// <summary>
        /// 加载树
        /// </summary>
        private void LoadMenuTree()
        {
            TreeViewModel = new MenuTreeModel();
            List<ISystemConfig> dataList = _systemConfigService.GetAllConfigs();

            //! sort it to get a stable order
            dataList.Sort( (f1,f2)=>f1.GetType().Name.CompareTo(f2.GetType().Name) ) ;

            if (dataList != null)
            {
                foreach (var item in dataList)
                {
                    Type type = item.GetType();
                    MenuTreeModel treeMenu = new MenuTreeModel();
                    treeMenu.Name = type.Name;
                    treeMenu.DisplayName = ((DisplayNameAttribute)type.GetCustomAttribute(typeof(DisplayNameAttribute)))?.DisplayName ?? type.Name;
                    treeMenu.SystemConfig = item;
                    TreeViewModel.Children.Add(treeMenu);
                    BindChildTreeMenu(treeMenu, item);
                }
            }
        }
        private void BindChildTreeMenu(MenuTreeModel treeMode, ISystemConfig systemConfig)
        {
            if (systemConfig == null)
                return;
            foreach (PropertyInfo childItem in systemConfig.GetType().GetProperties())
            {
                if (childItem.PropertyType.Namespace == systemConfig.GetType().Namespace)
                {
                    MenuTreeModel childMenu = new MenuTreeModel();
                    childMenu.Name = childItem.Name;
                    childMenu.DisplayName = ((DisplayNameAttribute)childItem.GetCustomAttribute(typeof(DisplayNameAttribute)))?.DisplayName ?? childItem.Name;
                    Type type = systemConfig.GetType();
                    PropertyInfo[] properties = type.GetProperties();
                    foreach (PropertyInfo property in properties)
                    {
                        Console.WriteLine("Name: " + property.Name + ", Value: " + property.GetValue(systemConfig, null));
                        if (property.Name == childItem.Name)
                        {
                            childMenu.SystemConfig = property.GetValue(systemConfig) as ISystemConfig;
                        }
                    }
                    //Activator.CreateInstance(childItem.PropertyType) as BaseSystemConfig;
                    treeMode.Children.Add(childMenu);
                    BindChildTreeMenu(childMenu, childMenu.SystemConfig);
                }
                else if (childItem.PropertyType.Name.Contains("List"))
                {
                    var dataList = childItem.GetValue(systemConfig, null) as IEnumerable;
                    List<ISystemConfigList> list = new List<ISystemConfigList>();
                    foreach (var item in dataList)
                    {
                        var data= item as ISystemConfigList;
                        list.Add(data);
                    }
                    foreach (var item in list.OrderBy(o=>o.Sys_OrderId))
                    {
                        MenuTreeModel childMenu = new MenuTreeModel();
                        childMenu.Name = item.Sys_Name;
                        childMenu.DisplayName = item.Sys_Name;
                        childMenu.SystemConfig = item;
                        treeMode.Children.Add(childMenu);
                        BindChildTreeMenu(childMenu, childMenu.SystemConfig);
                    }
                }
            }
        }        
        [RelayCommand]
        public void SaveData()
        {
            //_systemConfigService.SetConfig(new TestModel1());
            
            Save(TreeViewModel,true);
            _MessageBoxService.SuccessDialog("Successfully saved!");
            //bool result = true;// _systemConfigService.SetConfig(baseSystemConfigList);
            //if (result)
            //{
            //    _MessageBoxService.SuccessDialog("已保存配置！");
            //}

            CloseAction?.Invoke(false);
        }
        [RelayCommand]
        public void SubmitData() 
        {
            Save(TreeViewModel, true);
            CloseAction?.Invoke(true);
        }

        [RelayCommand]
        public void InitData()
        {
            //_systemConfigService.SetConfig(new TestModel1
            //{
            //    Name = "abc",
            //    ChildModel1 = new ChildModel1
            //    {
            //        Remark = "cas",
            //        ChildModelNew = new ChildModelNew
            //        {
            //            Remark = "asdfd"
            //        }
            //    }
            //});
            //_systemConfigService.SetConfig(new TestModel2 { AxisX = 23 });
            //_systemConfigService.SetConfig(new TestModel3 { Type = "adf3" });
        }
        [RelayCommand]
        public void Close() 
        {
            CloseAction?.Invoke(true);

            
        }
        public void Save(MenuTreeModel Model,bool isAdd) 
        {
           
            foreach (var item in Model.Children)
            {
                if (isAdd)
                    _systemConfigService.SetConfig(item.SystemConfig);
                if (item.Children != null && item.Children.Count > 0)
                {
                    Save(item,false);
                }
            }

        }
        [RelayCommand]
        public void MouseLeftButtonUp(object value)
        {
            EditObjectAttribute(value);
            SelectPropertyConfig = value as ISystemConfig;
        }
        /// <summary>
        /// 修改特性值
        /// </summary>
        /// <param name="value"></param>
        public void EditObjectAttribute(object value)
        {
            if (value == null) return;
            PropertyDescriptorCollection appSetingAttributes = TypeDescriptor.GetProperties(value);
            #region 查询和编辑特性
            //Type displayType = typeof(EditorAttribute);
            //查询特性里的参数列表
            //var fieldInfos = (FieldInfo[])((TypeInfo)displayType).DeclaredFields;
            //获取特性内部值
            //FieldInfo editorTypeId = displayType.GetField("_typeId", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.CreateInstance);
            //FieldInfo editorBase = displayType.GetField("<EditorBaseTypeName>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.CreateInstance);
            //FieldInfo editorType = displayType.GetField("<EditorTypeName>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.CreateInstance);
            //改特性值
            //editorBase.SetValue(appSetingAttributes["Path"].Attributes[displayType], UserControlConfig.PropertyEditorBase);
            //editorType.SetValue(appSetingAttributes["Path"].Attributes[displayType], UserControlConfig.SelectFolderPropertyEditor);
            #endregion

            foreach (var attr in value.GetType().GetProperties())
            {
                Attribute attribute = attr.GetCustomAttribute(typeof(EditorTypeAttribute));
                if (attribute != null)
                {
                    Type memberDescriptorType = typeof(MemberDescriptor);
                    Attribute[] attributes = new Attribute[1];
                    switch (((EditorTypeAttribute)attribute).EnumEditorType)
                    {
                        case EnumEditorType.SelectedProperty:
                            attributes[0] = new EditorAttribute(typeof(SelectFolderPropertyEditor).AssemblyQualifiedName, typeof(PropertyEditorBase).AssemblyQualifiedName);
                            break;
                        case EnumEditorType.DateTimeProperty:
                            attributes[0] = new EditorAttribute(typeof(PropertyEditor.DateTimePropertyEditor).AssemblyQualifiedName, typeof(PropertyEditorBase).AssemblyQualifiedName);
                            break;
                        case EnumEditorType.NumericProperty:
                            attributes[0] = new EditorAttribute(typeof(NumericPropertyEditor).AssemblyQualifiedName, typeof(PropertyEditorBase).AssemblyQualifiedName);
                            break;
                        default:
                            attributes[0] = new EditorAttribute(typeof(SelectFolderPropertyEditor).AssemblyQualifiedName, typeof(PropertyEditorBase).AssemblyQualifiedName);
                            break;
                    }
                    FieldInfo fieldInfoList = memberDescriptorType.GetField("_originalAttributes", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.CreateInstance);
                    fieldInfoList.SetValue(appSetingAttributes[attr.Name], attributes);
                }
            }
        }
    }

    public class MenuTreeModel
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public ISystemConfig SystemConfig { get; set; }
        public ObservableCollection<MenuTreeModel> Children { get; set; } = new ObservableCollection<MenuTreeModel>();

    }
}
