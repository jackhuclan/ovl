using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.Mvvm.Bases;
using InteVega.CBB.Prism.UI.Extensions.MessageBox;
using InteVega.CBB.SystemConfig.Interface;
using Marvel.DataModels.Config;
using Marvel.DataModels.Core;
using Marvel.DataModels.Enums;
using Marvel.DataModels.Recipe;
using Marvel.DataModels.Recipe.AIM;
using Marvel.DataModels.Recipe.CD;
using Marvel.DataModels.Recipe.Overlay;
using Microsoft.Extensions.DependencyInjection;
using Prism.Events;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Linq;
using Prism.Services.Dialogs;

using Ovl.UI.ViewModels.EventAggregators;
namespace Ovl.UI.ViewModels.Recipe.ViewModel
{
    public partial class Test_PhysicalNewViewModel : Test_SettingsViewModelBase
    {
        /// <summary>
        /// 判断指定的参数是否有发生改变，如果发生改变，则提示用户重新训练Test
        /// </summary>
        bool _isNeedTrain;
        IMessageBoxService _messageBoxService;
        IServiceProvider _serviceProvider;
        [ObservableProperty]
        private TestInfoBase _testInfo;
        [ObservableProperty]
        private ObservableCollection<MultiRoiInfo> _multiRoiList;
        [ObservableProperty]
        private int _multiRoiWidth;
        [ObservableProperty]
        private ObservableCollection<PropertyViewModel> _propertyViewModels;


        public Test_PhysicalNewViewModel(IEventAggregator eventAggregator, IServiceProvider serviceProvider) : base(eventAggregator)
        {
            _messageBoxService = serviceProvider.GetService<IMessageBoxService>();
            _serviceProvider = serviceProvider;
        }


        private void PhysicalPropertyChanged(PropertyChangeArg arg)
        {
            string propertyName = arg.PropertyName;
            switch (propertyName)
            {
                case nameof(TestPhysical.GrabMode):
                case nameof(IFocusPhysical.FocusMode):
                case nameof(FocusPhysical.FocusBasedOn):
                case nameof(TestPhysical_LargeCD.CameraType):
                case nameof(TestPhysical_LargeCD.IsAcrossTheCameraVision):
                    {
                        NeedReTrain(propertyName);
                        break;
                    }
                case nameof(IFocusPhysical.IsUseWLI):
                case nameof(IFocusPhysical.IsEnableFocusCorrection):
               
                    {
                        var isOpen = (bool)arg.NewValue;
                        if (isOpen)
                        {
                            NeedReTrain(propertyName);
                        }
                        break;
                    }
                case nameof(TestPhysicalXYS.LayoutSetup):
                    if (TestInfo is TestInfo_AIM_XYS testXYS)
                    {
                        var oldValue = (EnumOptimizeType)arg.OldValue;
                        var layoutSetup = testXYS.PhysicalXYS.LayoutSetup;
                        if (TestInfo.Physical.TisCorrectionSettings is TisCorrectionPhysical_Overlay tcpo && layoutSetup != EnumOptimizeType.Both)
                        {
                            tcpo.OptimizeType = layoutSetup;
                        }
                        if (oldValue != EnumOptimizeType.Both)
                        {
                            NeedReTrain(propertyName);
                        }
                    }
                    break;
            }

        }

        private void NeedReTrain(string propertyName)
        {
            _isNeedTrain = true;
            TestInfo.TrainSetting.IsTrained = false;
            LogHelper.LogInfo($"Physical param {propertyName} changed,need retrain the test {TestInfo.Name}");
        }

       

        public override void TestInfoRecive(TestInfoAndWaferMap testInfoAndWaferMap)
        {
            if (testInfoAndWaferMap.TestInfo != null)
            {
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(System.Windows.Application.Current.Dispatcher));
                SynchronizationContext.Current.Post(pl =>
                {
                    TestInfo = testInfoAndWaferMap.TestInfo.TestInfo;

                    var workFlowConfig = _serviceProvider.GetRequiredService<IConfigLoader>().LoadConfig<WorkFlowConfig>();
                    if (TestInfo.Physical is Physical ovlPhysicalForOffset)
                    {
                        ovlPhysicalForOffset.TestSettings.IsShowOvlOffset = workFlowConfig.IsShowOvlOffset;
                    }

                    // TestInfo = TestInfoFactory.Create
                    PropertyViewModels = new ObservableCollection<PropertyViewModel>(PropertyViewModelFactory.CreatePropertyModels(TestInfo.Physical, new Action(() =>
                    {
                        PropertyViewModelManager.RegisterNotifyPara("CameraType", CDCameraType.Measure, "LightSourceParam", EnumPropertyAbility.Enable);
                        PropertyViewModelManager.RegisterNotifyPara(new List<(string, object)>
                                                                    {
                                                                        ("GrabMode", EnumGrabMode.CPL),
                                                                        ("GrabMode", EnumGrabMode.DoubleGrab)
                                                                    },
                                                                    LogicOperator.Or,
                                                                    "AnraCycle",
                                                                    EnumPropertyAbility.Browsable
                                                                    );

                        PropertyViewModelManager.RegisterNotifyPara("LayoutSetup", EnumOptimizeType.Both, "OptimizeType", EnumPropertyAbility.Enable);
                        PropertyViewModelManager.RegisterNotifyPara("IsUseWLI", true, "IsCalWliOffset", EnumPropertyAbility.Enable);
                        PropertyViewModelManager.RegisterNotifyPara("IsCalWliOffset", true, "CalWliIterations", EnumPropertyAbility.Enable);
                        PropertyViewModelManager.RegisterNotifyPara("IsEnableMatchFormula", true, "A0", EnumPropertyAbility.Enable);
                        PropertyViewModelManager.RegisterNotifyPara("IsEnableMatchFormula", true, "A1", EnumPropertyAbility.Enable);

                        PropertyViewModelManager.RegisterNotifyPara("IsTempWliHide", false, "IsUseWLI", EnumPropertyAbility.Browsable);
                        PropertyViewModelManager.RegisterNotifyPara("IsTempWliHide", false, "IsCalWliOffset", EnumPropertyAbility.Browsable);
                        PropertyViewModelManager.RegisterNotifyPara("IsTempWliHide", false, "CalWliIterations", EnumPropertyAbility.Browsable);
                        PropertyViewModelManager.RegisterNotifyPara("IsTempWliHide", false, "IsVerifyMeasurementFocus", EnumPropertyAbility.Browsable);

                        PropertyViewModelManager.RegisterNotifyPara("IsShowOvlOffset", true, "OvlOffsetX", EnumPropertyAbility.Browsable);
                        PropertyViewModelManager.RegisterNotifyPara("IsShowOvlOffset", true, "OvlOffsetY", EnumPropertyAbility.Browsable);

                        PropertyViewModelManager.RegisterNotifyPara("GrabMode", EnumGrabMode.DoubleGrab, "DoubleGrabMode", EnumPropertyAbility.Browsable);
                        PropertyViewModelManager.RegisterNotifyPara("GrabMode", EnumGrabMode.DoubleGrab, "IsDoubleFocus", EnumPropertyAbility.Browsable);


                    })));

                    (TestInfo.Physical.FocusSettings as BindableBase).ActionPropertyChanged = PhysicalPropertyChanged;
                    if (TestInfo.Physical is Physical ovlPhysical)
                    {
                        ovlPhysical.TestSettings.ActionPropertyChanged = PhysicalPropertyChanged;
                        ovlPhysical.AnraSettings.ActionPropertyChanged = PhysicalPropertyChanged;
                    }
                    else if (TestInfo.Physical is Physical_CD cdPhysical)
                    {
                        cdPhysical.TestSettings.ActionPropertyChanged = PhysicalPropertyChanged;
                    }
                    LoadCDMultiRoiList();
                }, null);
            }
        }
        public override void Save()
        {
            if (TestInfo != null && _isNeedTrain && !TestInfo.TrainSetting.IsTrained)
            {
                string info = "Please retrain the Test due to measuring focus changes.";
                LogHelper.LogInfo("info");
                _messageBoxService.ErrorDialog(info);
                _isNeedTrain = false;
            }
        }
        public void LoadCDMultiRoiList()
        {
            if (TestInfo is TestInfo_CD testCD)
            {
                MultiRoiList = testCD.TrainSetting_CD.MultiRoiList;
                MultiRoiWidth = 470;
                if (MultiRoiList == null || MultiRoiList.Count == 0)
                {
                    AddCDMultiRoiInternal();
                }
            }
            else
            {
                MultiRoiWidth = 0;
            }
        }
        public void IsCanAcross(bool value)
        {
            if (((Physical_CD)TestInfo.Physical).TestSettings is TestPhysical_LargeCD100 testSetting100)
            {
                testSetting100.IsCanAcross = value;

            }
            if (((Physical_CD)TestInfo.Physical).TestSettings is TestPhysical_LargeCD40 testSetting40)
            {
                testSetting40.IsCanAcross = value;
            }
            if (((Physical_CD)TestInfo.Physical).TestSettings is TestPhysical_LargeCD testSetting)
            {
                testSetting.IsCanAcross = value;
            }
        }
        [RelayCommand]
        public void SaveCDMultiRoi()
        {
            if (TestInfo is TestInfo_CD testCD)
            {
                bool IsAcrossTheCameraVision = false;
                if (((Physical_CD)testCD.Physical).TestSettings is TestPhysical_LargeCD testSetting)
                {
                    IsAcrossTheCameraVision = testSetting.IsAcrossTheCameraVision;
                }

                if (MultiRoiList.Any(m => string.IsNullOrWhiteSpace(m.Name)))
                {
                    _messageBoxService.ErrorDialog("CD Name cannot be empty!");
                    return;
                }
                else if (MultiRoiList.GroupBy(g => g.Name).Where(w => w.Count() > 1).Any())
                {
                    _messageBoxService.ErrorDialog("Data with the same name exists!");
                    return;
                }
                else if (MultiRoiList.Count > 1 && IsAcrossTheCameraVision)
                {
                    _messageBoxService.ErrorDialog("Cannot create multiple CDs across different perspectives!");
                    return;
                }

                testCD.TrainSetting_CD.MultiRoiList = MultiRoiList;
                _messageBoxService.Success("Data saved successfully！", "RecipeMessage");
                IsCanAcross(MultiRoiList.Count < 2);
            }

        }


        [RelayCommand]
        public void AddCDMultiRoi()
        {
            _isNeedTrain = true;
            AddCDMultiRoiInternal();
        }
        public void AddCDMultiRoiInternal()
        { 
            bool IsAcrossTheCameraVision = false;
            var TestSettings = ((Physical_CD)TestInfo.Physical).TestSettings;
            if (TestSettings is TestPhysical_LargeCD100 testSetting && testSetting != null)
            {
                IsAcrossTheCameraVision = testSetting.IsAcrossTheCameraVision;
            }
            else if (TestSettings is TestPhysical_LargeCD testSettingCD && testSettingCD != null)
            {
                IsAcrossTheCameraVision = testSettingCD.IsAcrossTheCameraVision;
            }
            else if (TestSettings is TestPhysical_LargeCD40 testSettingCD40 && testSettingCD40 != null)
            {
                IsAcrossTheCameraVision = testSettingCD40.IsAcrossTheCameraVision;
            }
            if (MultiRoiList != null && MultiRoiList.Count >= 1 && IsAcrossTheCameraVision)
            {
                _messageBoxService.ErrorDialog("Cannot create multiple CDs across different perspectives!");
                return;
            }

            if (MultiRoiList == null)
            {
                MultiRoiList = new ObservableCollection<MultiRoiInfo>();
            }
            MultiRoiInfo multiRoiInfo = new MultiRoiInfo();
            multiRoiInfo.Id = MultiRoiList.Count + 1;
            multiRoiInfo.Name = "CD-" + multiRoiInfo.Id;
            multiRoiInfo.NominalDimenslons = 0;
            multiRoiInfo.Angle = 90;
            MultiRoiList.Add(multiRoiInfo);
            //MultiRoiOrder();

            if (TestInfo is TestInfo_CD testCD)
            {
                testCD.TrainSetting_CD.MultiRoiList = MultiRoiList;
            }
            IsCanAcross(MultiRoiList.Count < 2);
        }
        [RelayCommand]
        public void DeleteMultiRoi(object dataItem)
        {
            if (MultiRoiList == null) { return; }
            int id = ((MultiRoiInfo)dataItem).Id;
            MultiRoiList.Remove(MultiRoiList[id - 1]);
            MultiRoiReOrder();
            if (TestInfo is TestInfo_CD testCD)
            {
                testCD.TrainSetting_CD.MultiRoiList = MultiRoiList;
            }
            IsCanAcross(MultiRoiList.Count < 2);
            _isNeedTrain = true;
        }
        /// <summary>
        /// 对Roi的重新进行编号
        /// </summary>
        private void MultiRoiReOrder()
        {
            int num = 1;
            var list = MultiRoiList.ToList();
            foreach (var item in MultiRoiList.OrderBy(o => o.Id))
            {
                item.Id = num;
                num++;
            }

        }
    }
}
