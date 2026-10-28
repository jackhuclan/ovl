using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.Prism.UI.Extensions.MessageBox;
using InteVega.CBB.Recipe.Core;
using InteVega.CBB.Recipe.Interface;
using Marvel.DataModels.Enums;
using Marvel.DataModels.Recipe;
using Prism.Services.Dialogs;
using System;
using System.Collections.ObjectModel;

namespace Ovl.UI.ViewModels.Recipe.ViewModel
{
    internal partial class WaferMapViewModel : RecipeElementViewModelBase
    {
        ~WaferMapViewModel()
        {

        }
        public Action ActionR1Changed { get; set; }
        [ObservableProperty]
        WaferMapInfo _waferMapInfo;
        /// <summary>
        /// 晶圆形状
        /// </summary> 
        [ObservableProperty]
        ObservableCollection<EnumWrapeShape> _shapeDataList;
        /// <summary>
        /// 晶圆缺口类型
        /// </summary>
        [ObservableProperty]
        ObservableCollection<EnumNotchType> _notchTypeDataList;
        /// <summary>
        /// 晶圆缺口朝向
        /// </summary>
        [ObservableProperty]
        ObservableCollection<EnumNotchDirection> _notchDirectionDataList;

        private bool _isCheckedNoField;
        public bool IsCheckedNoField
        {
            get { return _isCheckedNoField; }
            set
            {
                SetProperty(ref _isCheckedNoField, value);
            }
        }

        [ObservableProperty]
        ObservableCollection<EnumWaferSizeType> _waferSizeTypeList;

        IMessageBoxService _messageBoxService;
        public WaferMapViewModel(IServiceProvider serviceProvider,
            IMessageBoxService messageBoxService) : base(serviceProvider)
        {
            _messageBoxService = messageBoxService;
            BindListData();


        }

        private void UpdateWaferMapInfo(string paramName, string paramValue)
        {
            if (paramName.Contains("Inch", StringComparison.OrdinalIgnoreCase)
                && Enum.TryParse(paramValue, out EnumWaferSizeType size))
                WaferMapInfo.WaferSizeType = size;

            if (paramName.Contains("Notch type", StringComparison.OrdinalIgnoreCase)
                && Enum.TryParse(paramValue, out EnumNotchType notchType))
                WaferMapInfo.NotchType = notchType;

            if (paramName.Contains("Direction", StringComparison.OrdinalIgnoreCase)
                && Enum.TryParse(paramValue, out EnumNotchDirection notchDirection))
                WaferMapInfo.NotchDirection = notchDirection;

            if (paramName.Contains("Thickness", StringComparison.OrdinalIgnoreCase)
                && double.TryParse(paramValue, out double thickness))
                WaferMapInfo.Thickness = thickness;

            if (paramName.Contains("Exclusion", StringComparison.OrdinalIgnoreCase)
                && double.TryParse(paramValue, out double exclusion))
                WaferMapInfo.EdgeExclusion = exclusion;

            if (paramName.Contains("ClipToBounds", StringComparison.OrdinalIgnoreCase)
                && bool.TryParse(paramValue, out bool ClipToBounds))
                WaferMapInfo.ClipToBounds = ClipToBounds;

            if (paramName.Contains("Specific", StringComparison.OrdinalIgnoreCase)
                && bool.TryParse(paramValue, out bool Specific))
                WaferMapInfo.IsSpecificNotch = Specific;

            if (paramName.Contains("Angle", StringComparison.OrdinalIgnoreCase)
                && double.TryParse(paramValue, out double SpecificNotch))
                WaferMapInfo.SpecificNotch = SpecificNotch;

            if (paramName.Contains("Width", StringComparison.OrdinalIgnoreCase)
                && double.TryParse(paramValue, out double FieldWidth))
                WaferMapInfo.FieldWidth = FieldWidth;

            if (paramName.Contains("Height", StringComparison.OrdinalIgnoreCase)
                && double.TryParse(paramValue, out double FieldHeight))
                WaferMapInfo.FieldHeight = FieldHeight;

            if (paramName.Contains("Manual", StringComparison.OrdinalIgnoreCase)
                && bool.TryParse(paramValue, out bool Manual))
                WaferMapInfo.IsManual = Manual;

            if (paramName.Contains("Center of field", StringComparison.OrdinalIgnoreCase)
                && bool.TryParse(paramValue, out bool IsFieldCenter))
                WaferMapInfo.IsFieldCenter = IsFieldCenter;

            if (paramName.Contains("X(mm)", StringComparison.OrdinalIgnoreCase)
                && double.TryParse(paramValue, out double x))
                WaferMapInfo.R1.X = x;

            if (paramName.Contains("Y(mm)", StringComparison.OrdinalIgnoreCase)
                && double.TryParse(paramValue, out double y))
                WaferMapInfo.R1.Y = y;
        }

        /// <summary>
        /// 
        /// </summary>
        private void BindListData()
        {
            ShapeDataList = new ObservableCollection<EnumWrapeShape>();
            foreach (EnumWrapeShape value in Enum.GetValues<EnumWrapeShape>())
            {
                ShapeDataList.Add(value);
            }
            NotchTypeDataList = new ObservableCollection<EnumNotchType>();
            foreach (EnumNotchType value in Enum.GetValues<EnumNotchType>())
            {
                NotchTypeDataList.Add(value);
            }
            NotchDirectionDataList = new ObservableCollection<EnumNotchDirection>();
            foreach (EnumNotchDirection value in Enum.GetValues<EnumNotchDirection>())
            {
                NotchDirectionDataList.Add(value);
            }
            WaferSizeTypeList = new ObservableCollection<EnumWaferSizeType>();
            foreach (EnumWaferSizeType value in Enum.GetValues(typeof(EnumWaferSizeType)))
            {
                WaferSizeTypeList.Add(value);
            }
        }
        public override void RequestData(IRecipe data, RecipeElementsInfo recipe, RecipeInfo recipeInfo)
        {
            base.RequestData(data, recipe, recipeInfo);
            WaferMapInfo = _recipeElementsInfo.WaferMap;
            if (WaferMapInfo == null)
            {
                _messageBoxService.ErrorDialog("Failed to load data！");
            }
        }

        //public void ShowShiftFieldDialog()
        //{
        //    var _dialogService = _serviceProvider.GetService<IDialogService>();
        //    var viewmodel = new WaferMapShiftFieldViewModel(WaferMapInfo, _messageBoxService);
        //    _dialogService.ShowDialog("WaferMapShiftFieldView", viewmodel);

        //}

        [RelayCommand]
        public void DecreaseFieldX()
        {
            WaferMapInfo.R1.X -= WaferMapInfo.FieldWidth;
            ActionR1Changed?.Invoke();
        }

        [RelayCommand]
        public void IncreaseFieldX()
        {
            WaferMapInfo.R1.X += WaferMapInfo.FieldWidth;
            ActionR1Changed?.Invoke();
        }

        [RelayCommand]
        public void DecreaseFieldY()
        {
            WaferMapInfo.R1.Y -= WaferMapInfo.FieldWidth;
            ActionR1Changed?.Invoke();
        }

        [RelayCommand]
        public void IncreaseFieldY()
        {
            WaferMapInfo.R1.Y += WaferMapInfo.FieldWidth;
            ActionR1Changed?.Invoke();
        }

        [RelayCommand]
        public void Save()
        {
            try
            {
                // RecipeElementsInfo recipeElementsInfo = new RecipeElementsInfo();
                //_recipeElementsInfo.WaferMap = WaferMapInfo;
                _recipeElementsInfo.WaferMap.ModuleState = EnumModuleState.SetUp;
                SaveRecipeFile();
                _messageBoxService.SuccessDialog("Data saved successfully！", "");
            }
            catch (Exception ex)
            {
                LogHelper.LogError(ex, "Failed to save data！");
                _messageBoxService.ErrorDialog("Failed to save data！");
            }
        }

        internal bool ConfirmRedraw(bool isAlways = false)
        {
            if (WaferMapInfo.IsShiftedFields || isAlways)
            {
                if (_messageBoxService.AskDialog("This operation will redraw the wafermap. After redrawing, you will need to re-edit the shifted fields. Are you sure you want to do this?") != ButtonResult.OK)
                { return false; }
            }
            return true;
        }

        internal bool ConfirmResetField0()
        {
            if (WaferMapInfo.IsShiftedFields)
            {
                if (_messageBoxService.AskDialog("Changing Field0 requires re-editing Shifted Fields. Are you sure you want to change it?") != ButtonResult.OK)
                { return false; }
            }
            return true;
        }
    }
}
