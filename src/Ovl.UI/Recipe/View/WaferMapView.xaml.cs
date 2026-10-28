using InteVega.CBB.CommonLib.Helpers;
using Marvel.DataModels.Recipe;
using Marvel.WaferMapCommon;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

using Ovl.UI.Recipe.ViewModel;
using DataModels = Marvel.DataModels;
namespace Ovl.UI.Recipe.View
{
    /// <summary>
    /// WaferMapView.xaml 的交互逻辑
    /// </summary>
    public partial class WaferMapView : UserControl
    {
        ~WaferMapView() 
        { }
        public WaferMapView()
        {
            InitializeComponent();
            this.Loaded += WaferMapView_Loaded;
            this.Unloaded += WaferMapView_Unloaded;
           
        }
        bool _isEditing = false;
        WaferMapViewModel vm;

        private void WaferMapView_Unloaded(object sender, RoutedEventArgs e)
        {
            LogHelper.LogInfo("WaferMapView Unloaded!");
            if (vm != null)
            {
                LoadFields(vm.WaferMapInfo);
            }
        }
        bool isPropertyChanged;
        private void WaferMapView_Loaded(object sender, RoutedEventArgs e)
        {
            LogHelper.LogInfo("WaferMapView Loaded!");
           // isPropertyChanged = false;
            vm = this.DataContext as WaferMapViewModel;

            if (vm == null) return;
            vm.ActionR1Changed = () =>
            {
                //此处更新一下数据绑定，否则界面不会刷新
                this.DataContext = null;
                this.DataContext = vm;
                DrawWaferMap();
            };

            // wmc.FuncSetField0 = vm.ConfirmResetField0;
            if (vm.WaferMapInfo == null)
            {
                vm.WaferMapInfo = new DataModels.Recipe.WaferMapInfo();
            }
            if (vm != null && vm.WaferMapInfo != null)
            {
                // vm.WaferMapInfo.ActionPropertyChanged += (arg) => { isPropertyChanged = true; };
                // vm.WaferMapInfo.ActionPropertyChanged = (arg) => { isPropertyChanged = true; };
                if (vm.WaferMapInfo.FieldInfos.Count != 0)
                {
                    DrawWaferMap();
                }
                else
                {
                    wmc.RedrawByLlc(vm.WaferMapInfo);
                }
            }
        }

        private void DrawWaferMap()
        {
            wmc.Draw(vm.WaferMapInfo);
        }

        private void Button_Create_Click(object sender, RoutedEventArgs e)
        {
            var wafermap = vm.WaferMapInfo;
            LogHelper.LogInfo($"Create Wafer Map:{wafermap}");
            if (vm.ConfirmRedraw())
            {
                wmc.RedrawByLlc(wafermap);
            }
            else
            {
                wmc.Draw(wafermap);
            }
            Save();
        }


        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void LoadFields(WaferMapInfo wmi)
        {
            if (wmi == null) { return; }
            wmi.FieldInfos = wmc.GetFields();
        }


        private void Button_Edit_Click(object sender, RoutedEventArgs e)
        {
            //if (vm.WaferMapInfo.IsShiftedFields)
            //{
            //    vm.ShowShiftFieldDialog();
            //    wmc.Draw(vm.WaferMapInfo);
            //}
            //else
            {
                if (_isEditing)
                {
                    (sender as Button).Content = "Edit Wafer Map";
                    wmc.EditMode = EnumEditMode.None;
                }
                else
                {
                    (sender as Button).Content = "Complete";
                    wmc.EditMode = EnumEditMode.EditField;
                }
                _isEditing = !_isEditing;
            }
        }

        private void Button_Select_Click(object sender, RoutedEventArgs e)
        {
            wmc.EditMode = EnumEditMode.MultiSelect;
        }

        private void Button_Check_Click(object sender, RoutedEventArgs e)
        {
            wmc.EditMode = EnumEditMode.Check;
        }

        private void Button_Move_Click(object sender, RoutedEventArgs e)
        {
            wmc.EditMode = EnumEditMode.Move;

            wmc.ActionOffset = (field, offset) =>
            {
                var wcspoint = vm.WaferMapInfo.GetWcsPoint(field, offset);
                wmc.UpdateOffset(wcspoint);
            };
        }


        private void CheckBoxFieldCenter_Click(object sender, RoutedEventArgs e)
        {
            DrawWaferMap();
        }

        private void cbIsNoFieldMode_Checked(object sender, RoutedEventArgs e)
        {
            DrawWaferMap();
            wmc.DisplayFields(vm.IsCheckedNoField);
        }

        private void btnEdit_Checked(object sender, RoutedEventArgs e)
        {
            wmc.EditMode = EnumEditMode.EditField;
            wmc.ClearSelected();
        }

        private void btnShift_Checked(object sender, RoutedEventArgs e)
        {
            wmc.EditMode = EnumEditMode.BoxSelect;
        }

        private void ShiftedField_Click(object sender, RoutedEventArgs e)
        {
            if (!vm.WaferMapInfo.IsShiftedFields)
            {
                if (!vm.ConfirmRedraw(true))
                {
                    vm.WaferMapInfo.IsShiftedFields = true;
                    return;
                }
                vm.WaferMapInfo.FieldRowSpan = 1;
                vm.WaferMapInfo.FieldColumnSpan = 1;
                vm.WaferMapInfo.Field0 = RowColumnInfo.Zero;
                wmc.RedrawByLlc(vm.WaferMapInfo);
            }
        }

        private void Button_Click_LeftField(object sender, RoutedEventArgs e)
        {
            wmc.MoveLeft();
        }

        private void Button_Click_RightField(object sender, RoutedEventArgs e)
        {
            wmc.MoveRight();
        }

        private void Button_Click_UpField(object sender, RoutedEventArgs e)
        {
            wmc.MoveTop();
        }

        private void Button_Click_DownField(object sender, RoutedEventArgs e)
        {
            wmc.MoveDown();
        }

        private void btnComplete_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void Save()
        {
            btnComplete.IsChecked = true;
            if (wmc == null || vm == null) return;
            wmc.EditMode = EnumEditMode.None;
            wmc.ClearSelected();

            var wafermap = vm.WaferMapInfo;
            LogHelper.LogInfo($"Save Wafer Map:{wafermap}");
            LoadFields(wafermap);
            vm.Save();
        }
    }
}
