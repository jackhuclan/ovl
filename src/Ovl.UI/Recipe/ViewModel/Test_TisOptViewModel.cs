using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.Prism.UI.Extensions.MessageBox;
using Marvel.DataModels.Core;
using Marvel.DataModels.Enums;
using Marvel.DataModels.Recipe;
using Marvel.DataModels.Recipe.AIM;
using Marvel.DataModels.Recipe.Tis;
using Microsoft.Extensions.DependencyInjection;
using Prism.Events;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

using Ovl.UI.Recipe.View;
using Ovl.UI.ViewModels.EventAggregators;
using DataModels = Marvel.DataModels;
namespace Ovl.UI.Recipe.ViewModel
{
    public partial class Test_TisOptViewModel : Test_SettingsViewModelBase
    {
        #region Binding property
        [ObservableProperty]
        private ObservableCollection<PropertyViewModel> _propertyViewModels;
        [ObservableProperty]
        private TestInfo_Overlay  _testInfo;

        [ObservableProperty]
        private TisInfo_TOF _TOF;

        [ObservableProperty]
        private TisInfo_TON _TON;

        [ObservableProperty]
        private TisInfo_TOV _TOV;

        [ObservableProperty]
        Visibility _TONYVisibility;

        [ObservableProperty]
        bool _isOptimizeByEnable = true;
        private IServiceProvider _prod;
        private readonly IDialogService _dialogService;
        private readonly IMessageBoxService _messageBoxService;

        public WaferMapInfo WaferMapInfo { get; private set; }
        #endregion

        public Test_TisOptViewModel(IEventAggregator eventAggregator, IServiceProvider serviceProvider) : base(eventAggregator)
        {
            _prod = serviceProvider;
            _dialogService = _prod.GetService<IDialogService>();
            _messageBoxService = _prod.GetService<IMessageBoxService>();
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
        }

        public override void TestInfoRecive(TestInfoAndWaferMap testInfoAndWaferMap)
        {
            if (testInfoAndWaferMap.TestInfo .TestInfo is TestInfo_Overlay to)
            {
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(System.Windows.Application.Current.Dispatcher));
                SynchronizationContext.Current.Post(pl =>
                {
                    TestInfo = to ;
                    WaferMapInfo = testInfoAndWaferMap.WaferMapInfo;
                    // TODO TOF,TON,TOV
                    TOF = TestInfo.TisGroup.TOF;
                    TON = TestInfo.TisGroup.TON;
                    TOV = TestInfo.TisGroup.TOV;
                    if (MarvelVersion.CurrentVersion == DataModels.Enums.EnumOMVersion.OM100)
                    {
                        // OM100不支持Y方向的NA 
                        TONYVisibility = Visibility.Collapsed;
                        TON.Config.OptimizeType = DataModels.Enums.EnumOptimizeType.X;
                    }
                    else
                    {
                        TONYVisibility = Visibility.Visible;
                    }
                  
                    SetXYSOptimizeBy();
                    PropertyViewModels = new ObservableCollection<PropertyViewModel>(PropertyViewModelFactory.CreatePropertyModels(TestInfo.TisGroup, new Action(() =>
                    {
                        //PropertyViewModelManager.RegisterNotifyPara("LayoutSetup", EnumOptimizeType.Both, "OptimizeType");
                        //PropertyViewModelManager.RegisterNotifyPara("GrabMode", EnumGrabMode.SingleGrab, "FocusBasedOn");
                        //PropertyViewModelManager.RegisterNotifyPara("IsEnableMatchFormula", true, "A0");
                        //PropertyViewModelManager.RegisterNotifyPara("IsEnableMatchFormula", true, "A1");
                    })));

                }, null);
            }
        }

        public void SetXYSOptimizeBy()
        {
            if (TestInfo.Physical is PhysicalXYS_AIM xysPhysical)
            {
                var layoutSetup = xysPhysical.LayoutSetup;
                if (layoutSetup != EnumOptimizeType.Both)
                {
                    IsOptimizeByEnable = false;
                    TOF.Config.OptimizeType = layoutSetup;
                    TOV.Config.OptimizeType = layoutSetup;
                    TON.Config.OptimizeType = layoutSetup;
                }
                else
                {
                    IsOptimizeByEnable = true;
                }
                Save();
            }
        }

        [RelayCommand]
        void SelectField(string tistype)
        {
            var parameters = new DialogParameters()
                {
                    { "TestInfo", TestInfo },
                    { "WaferMapInfo", WaferMapInfo }
                };


            if (tistype.ToLower() == "tof")
            {
                parameters.Add("TisInfo", TestInfo.TisGroup.TOF);
            }
            else if (tistype.ToLower() == "ton")
            {
                parameters.Add("TisInfo", TestInfo.TisGroup.TON);

            }
            else if(tistype.ToLower() == "tov")
            {
                parameters.Add("TisInfo", TestInfo.TisGroup.TOV);

            }

            _dialogService.ShowDialog(nameof(TisOptFieldSelectDialog), parameters, _ => { });
        }

        /// <summary>
        /// 检查所有参数是否在合理范围内
        /// </summary>
        /// <returns></returns>
        private bool IsParamInRanges()
        {
            var errors = CheckParam_TOF();

            // 如果有错误信息，弹出错误对话框
            if (errors.Any())
            {
                var combinedErrorMessage = string.Join(Environment.NewLine, errors);
                _messageBoxService.ErrorDialog(combinedErrorMessage, "Error");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 检查TOF参数
        /// </summary>
        /// <returns></returns>
        private List<string> CheckParam_TOF()
        {
            var cfg = TestInfo.TisGroup.TOF.Config;
            var locations = TestInfo.TisGroup.TOF.Locations;

            var errorMessages = new List<string>();
            if (System.Math.Abs(cfg.Slices * cfg.DistanceSlices) > 2)
            {
                errorMessages.Add("TOF - 'Number of Slices' * 'Distance Between Slices(um):' Must be less than 4μm!");
            }

            if (cfg.Slices % 2 != 1)
            {
                errorMessages.Add("TOF - 'Number Of Slices' must be ODD (Ex: 3,5,7...)!"); // ODD 表示奇数
            }

            if (cfg.Slices < 1)
            {
                errorMessages.Add("TOF - 'Number Of Slices' must be greater than 1!");
            }

            if (cfg.IsSiteBySite && !(locations.TisTargets?.Any(x=>x.IsEnable) ?? false))
            {
                errorMessages.Add("TOF - Please check the target!");
            }

            if (cfg.IsSiteBySite && !(locations.TisFields?.Any() ?? false))
            {
                errorMessages.Add("TOF - Please select the fields!");
            }
            return errorMessages;
        }

        /// <summary>
        /// 检查TOV参数
        /// </summary>
        /// <returns></returns>
        private List<string> CheckParam_TOV()
        {
            // TODO 待添加TOV防呆
            return default;
        }

        /// <summary>
        /// 检查TON参数
        /// </summary>
        /// <returns></returns>
        private List<string> CheckParam_TON()
        {
            // TODO 待添加TON防呆
            return default;
        }
    }
}
