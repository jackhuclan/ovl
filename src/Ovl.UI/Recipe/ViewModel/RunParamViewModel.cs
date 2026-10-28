using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.CommonLib.Helpers.Reflection;
using InteVega.CBB.Prism.UI.Extensions.MessageBox;
using InteVega.CBB.Recipe;
using InteVega.CBB.Recipe.Core;
using InteVega.CBB.Recipe.Interface;
using Marvel.Application.Extentions;
using Marvel.DataModels.Core;
using Marvel.DataModels.Enums;
using Marvel.DataModels.Job;
using Marvel.DataModels.Recipe;
using Marvel.Interfaces.Application;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

using Ovl.UI.Recipe.View;
namespace Ovl.UI.Recipe.ViewModel
{
    public partial class RunParamViewModel : RecipeElementViewModelBase
    {
        [ObservableProperty]
        RunParameter _runParameter;
      
        [ObservableProperty]
        Visibility _isShowSubmit=Visibility.Visible;
        [ObservableProperty]
        int _buttonHeight=60;

        [ObservableProperty]
        private bool _isTISOptimizeEnabled;

        [ObservableProperty]
        private bool _isTISOptimizeUseable;

   

        [ObservableProperty]
        private bool _isMroUseable;

        [ObservableProperty]
        private bool _isAutoRetrainUseable;

        [ObservableProperty]
        private List <AutoRetrainItem > _autoRetrainItems=new List<AutoRetrainItem> ();

        [ObservableProperty]
        private AutoRetrainItem _selectedAutoRetrainItem;
        /// <summary>
        /// 是否是Job界面的运行参数
        /// </summary>
        public bool IsJobRunParameter { get; set; }
        IMessageBoxService _messageBoxService;
        private readonly IRecipeManager _recipeManager;
        private readonly IServiceProvider _serviceProvider;
        public MROConfigurationView mroConfigurationView;
        /// <summary>
        /// 进入界面时的RunParameter对象
        /// </summary>
        private RunParameter _orgRunParam = new RunParameter();

        public RunParamViewModel(IServiceProvider serviceProvider, IMessageBoxService messageBoxService) : base(serviceProvider)
        {
            _messageBoxService = messageBoxService;
            _recipeManager = serviceProvider.GetService<IRecipeManager>();
            _serviceProvider = serviceProvider;
            _isTISOptimizeUseable = MarvelVersion.CurrentVersion != EnumOMVersion.OM40;
            IsAutoRetrainUseable = MarvelVersion.CurrentVersion != EnumOMVersion.OM40;
            IsMroUseable = MarvelVersion.CurrentVersion >= EnumOMVersion.OM600;
            AutoRetrainItems.Add(new AutoRetrainItem() { Value = EnumAutoRetrain.Unable, Text = "Unable" });
            AutoRetrainItems.Add(new AutoRetrainItem() { Value = EnumAutoRetrain.FirstWafer, Text = "First Wafer" });
            AutoRetrainItems.Add(new AutoRetrainItem() { Value = EnumAutoRetrain.AllWafers, Text = "All Wafers" });
        }
        public override void RequestData(IRecipe data, RecipeElementsInfo recipe, RecipeInfo recipeInfo)
        {
            base.RequestData(data, recipe, recipeInfo);
            if (IsJobRunParameter)
            {
                RunParameter = CloneHelper.ReflectionClone<RunParameter>(_recipeElementsInfo.RunParameter);
            }
            else
            {
                RunParameter = _recipeElementsInfo.RunParameter;
            }
            SelectedAutoRetrainItem = AutoRetrainItems.FirstOrDefault(x => x.Value == RunParameter.AutoRetrainMode);
            LoadPage(recipe.Id);
        }

        string _recipeId = string.Empty;
        public void LoadPage(string recipeID) 
        {
            _recipeId = recipeID;
            _orgRunParam = CloneHelper.ReflectionClone<RunParameter>(RunParameter);
            if (RunParameter != null && RunParameter.WaferSelectInfo == null)
            {
                RunParameter.WaferSelectInfo = new WaferSelectInfo();
            }
            if (RunParameter.WaferSelectInfo.SelectedSlots.Count <= 0)
            {
                AppHelper.DispatcherInvoke(() =>
                {
                    for (int i = 1; i < 26; i++)
                    {
                        RunParameter.WaferSelectInfo.SelectedSlots.Add(new SlotData
                        {
                            SlotNum = i,
                            IsSelected = false
                        });
                    }
                });
            }
            SelectedAutoRetrainItem = AutoRetrainItems.FirstOrDefault(x => x.Value == RunParameter.AutoRetrainMode)
                ?? AutoRetrainItems.First();
            ApplyTisModeUiConstraints();
            SelectHanding();
        }

        /// <summary>
        /// 根据 TisMode 更新 Tis Optimize / Tis Precalbration 控件可用性；NoWafer 时重置预校准为 Unable。
        /// </summary>
        public void ApplyTisModeUiConstraints()
        {
            IsTISOptimizeEnabled = RunParameter.TisMode != EnumTisMode.NoWafer;
            if (!IsTISOptimizeEnabled && RunParameter.TisPrecalbration != TisDataUseMode.Unable)
            {
                RunParameter.TisPrecalbration = TisDataUseMode.Unable;
            }
        }
        [RelayCommand]
        public void SelectSlot(SlotData slotData)
        {
            if (slotData != null)
            {
                slotData.IsSelected = !slotData.IsSelected;
            }
        }
        [RelayCommand]
        public void CheckAll()
        {
            foreach (var item in RunParameter.WaferSelectInfo.SelectedSlots)
            {
                item.IsSelected = true;
            }
        }
        [RelayCommand]
        public void ClearAll()
        {
            foreach (var item in RunParameter.WaferSelectInfo.SelectedSlots)
            {
                item.IsSelected = false;
            }
        }
        [RelayCommand]
        public void SelectHanding()
        {
            //IsSequential = SelectMode == EnumWaferSelectMode.Sequential ? true : false;
            //IsUserSelection = SelectMode == EnumWaferSelectMode.Sequential ? false : true;
        }

        [RelayCommand]
        private void EditMroConfig()
        {

            var recipe = _recipeManager.GetCurrentRecipe(_recipeId);
            var recipeElementsInfo = recipe.Get<RecipeElementsInfo>();
            if (recipeElementsInfo == null)
            {
                _messageBoxService.WarningDialog("RecipeElementsInfo is NULL，Not allowed Open MROConfigurationView ");
                return;
            }
            mroConfigurationView = new MROConfigurationView(_serviceProvider, _recipeId );
            mroConfigurationView.ShowDialog();
        }
        [RelayCommand]
        public void Save()
        {
            try
            {
                if (_recipeElementsInfo != null)
                {
                    if (!CheckTisOptimizeModeEnable()) return;
                    CheckSequences();
                    RunParameter.AutoRetrainMode = SelectedAutoRetrainItem.Value;
                    _recipeElementsInfo.RunParameter = RunParameter;
                    _recipeElementsInfo.RunParameter.ModuleState = EnumModuleState.SetUp;
                    SaveRecipeFile();
                    _messageBoxService.Success("Data saved successfully！", "RecipeMessage");
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError(ex, "Failed to save data！");
                _messageBoxService.ErrorDialog("Failed to save data！");
            }
        }

        /// <summary>
        /// 检查UserSelection模式下slots空选
        /// </summary>
        /// <returns>true:无异常</returns>
        public bool CheckSequences()
        {
           if( RunParameter.WaferSelectInfo.SelectMode== EnumWaferSelectMode.UserSelection
                && !RunParameter.IsManual)
            {
                if(!RunParameter.WaferSelectInfo.SelectedSlots.ToList().Exists(_ => _.IsSelected))
                {
                    _messageBoxService.ErrorDialog("Please select the slots in User Selection Mode");
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 检查上料模式是否变更
        /// </summary>
        /// <returns></returns>
        public bool CheckModeChanged()
        {
            return RunParameter.IsManual != _orgRunParam.IsManual;
        }
        public bool CheckTisOptimizeModeEnable()
        {
            List<string> tisOptModes = new List<string>();
            if (RunParameter.TisMode == EnumTisMode.NoWafer)
            {
                string tisOptName = RunParameter.TisUse_TOF != TisDataUseMode.Unable ? "TOF" :
        RunParameter.TisUse_TON != TisDataUseMode.Unable ? "TON" :
        RunParameter.TisUse_TOV != TisDataUseMode.Unable ? "TOV" : string.Empty;
                if(RunParameter.TisUse_TOF != TisDataUseMode.Unable)
                {
                    tisOptModes.Add("TOF");
                }
                if (RunParameter.TisUse_TON != TisDataUseMode.Unable)
                {
                    tisOptModes.Add("TON");
                }
                if (RunParameter.TisUse_TOV != TisDataUseMode.Unable)
                {
                    tisOptModes.Add("TOV");
                }
                
                // 把tisOptModes里面的字符串以逗号链接成一个字符串变量
                string tisOptModesStr = string.Join(",", tisOptModes);
                if (!string.IsNullOrEmpty(tisOptModesStr))
                {
                    _messageBoxService.WarningDialog($"{tisOptModesStr} Enabled, Only base on 'First Wafer' or 'All Wafer (mean)' supports the enabling of TisOptimization.");
                    return false;
                }

                if (RunParameter.TisPrecalbration != TisDataUseMode.Unable)
                {
                    _messageBoxService.WarningDialog("Tis Precalbration is only available when base on 'First Wafer' or 'All Wafer (mean)'.");
                    return false;
                }
            }


            return true;
        }


        public bool CheckTisOptimizeFoolProof(string recipeId)
        {
            RecipeElementsInfo recipeElementsInfo = _recipeElementsInfo == null ?
                _recipeManager.GetCurrentRecipe(recipeId).Get<RecipeElementsInfo>() : _recipeElementsInfo;

            var result = recipeElementsInfo.CheckTisParams(prod: _serviceProvider, runParam: _runParameter);
            if (!result.ret)
            {
                _messageBoxService.ErrorDialog(result.msgs);
                return false;
            }

            return true;
        }

        public bool CheckMroRankTisValidation(string recipeId)
        {
            if (!IsMroUseable || !RunParameter.IsEnableMro)
            {
                return true;
            }

            var mroService = _serviceProvider.GetService<IMroService>();
            if (mroService == null)
            {
                return true;
            }

            try
            {
                var recipeElementsInfo = _recipeElementsInfo ?? _recipeManager.GetCurrentRecipe(recipeId)?.Get<RecipeElementsInfo>();
                if (recipeElementsInfo == null)
                {
                    _messageBoxService.WarningDialog("RecipeElementsInfo is null, cannot validate MRO configuration.");
                    return false;
                }

                var mroConfig = mroService.OpenMroConfig(recipeElementsInfo);
                if (mroConfig == null)
                {
                    _messageBoxService.WarningDialog("MROConfig.json does not exist.");
                    return false;
                }

                mroService.ValidateRankTisRunParameter(mroConfig, RunParameter);
                return true;
            }
            catch (Exception ex)
            {
                _messageBoxService.WarningDialog(ex.Message, "MRO Configuration");
                return false;
            }
        }

        [RelayCommand]
        public void GetCamera()
        {

        }

        public class AutoRetrainItem
        {
            public EnumAutoRetrain Value { get; set; }

            public string  Text { get; set; }
        }

    }

    public class SlotDataConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not SlotData slotData) return null;
            string info = $"Slot:{slotData.SlotNum} \nWaferID:{slotData.WaferID} \nChuckID:{slotData.ChuckID}";

            return info;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
