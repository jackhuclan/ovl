using CommunityToolkit.Mvvm.ComponentModel;
using Marvel.DataModels.Core;
using Marvel.DataModels.Recipe;
using Prism.Events;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows.Threading;

using Ovl.UI.EventAggregators;
namespace Ovl.UI.Recipe.ViewModel
{
    public partial class Test_DataCullingNewViewModel : Test_SettingsViewModelBase
    {
        [ObservableProperty]
        private TestInfoBase _testInfo;

        [ObservableProperty]
        private ObservableCollection<PropertyViewModel> _propertyViewModels;


        public Test_DataCullingNewViewModel(IEventAggregator eventAggregator) : base(eventAggregator)
        {


        }

        public override void TestInfoRecive(TestInfoAndWaferMap testInfoAndWaferMap)
        {
            if (testInfoAndWaferMap.TestInfo != null)
            {
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(System.Windows.Application.Current.Dispatcher));
                SynchronizationContext.Current.Post(pl =>
                {
                    TestInfo = testInfoAndWaferMap.TestInfo.TestInfo;
                    PropertyViewModels = new ObservableCollection<PropertyViewModel>(PropertyViewModelFactory.CreatePropertyModels(TestInfo.DataCulling, new Action(() =>
                    {
                        PropertyViewModelManager.RegisterNotifyPara("IsEnableTisCulling", true, "XTisCulling", EnumPropertyAbility.Enable);
                        PropertyViewModelManager.RegisterNotifyPara("IsEnableTisCulling", true, "YTisCulling", EnumPropertyAbility.Enable);
                        PropertyViewModelManager.RegisterNotifyPara("IsEnableQMeritCulling", true, "QMeritCullingThreshold", EnumPropertyAbility.Enable);
                        PropertyViewModelManager.RegisterNotifyPara("IsOpenMeasurementCulling", true, "InnerBox", EnumPropertyAbility.Enable);
                        PropertyViewModelManager.RegisterNotifyPara("IsOpenMeasurementCulling", true, "OuterBox", EnumPropertyAbility.Enable);

                        PropertyViewModelManager.RegisterNotifyPara("MeasurementCullingOn", true, "TargetSymmetry", EnumPropertyAbility.Enable);
                        PropertyViewModelManager.RegisterNotifyPara("MeasurementCullingOn", true, "AllowableCDChanges", EnumPropertyAbility.Enable);
                        PropertyViewModelManager.RegisterNotifyPara("MeasurementCullingOn", true, "MinimalContrast", EnumPropertyAbility.Enable);

                        //PropertyViewModelManager.RegisterNotifyPara("IsEnableWaferPassCulling", true, "IsEnableWaferOnlyMisregResPassCulling", EnumPropertyAbility.Enable);
                        //PropertyViewModelManager.RegisterNotifyPara("IsEnableWaferPassCulling", true, "IsEnableWaferOnlyOvlResPassCulling", EnumPropertyAbility.Enable);

                        PropertyViewModelManager.RegisterNotifyPara("IsEnableWaferOnlyMisregResPassCulling", false, "IsEnableWaferOnlyOvlResPassCulling", EnumPropertyAbility.Enable);

                        PropertyViewModelManager.RegisterNotifyPara("IsEnableWaferOnlyOvlResPassCulling", false, "IsEnableWaferOnlyMisregResPassCulling", EnumPropertyAbility.Enable);

                        PropertyViewModelManager.RegisterNotifyPara("IsEnableWaferOnlyMisregResPassCulling", true, "MisregXErrorUpperLimit", EnumPropertyAbility.Enable);
                        PropertyViewModelManager.RegisterNotifyPara("IsEnableWaferOnlyMisregResPassCulling", true, "MisregYErrorUpperLimit", EnumPropertyAbility.Enable);
                        PropertyViewModelManager.RegisterNotifyPara("IsEnableWaferOnlyMisregResPassCulling", true, "MisregPassCriteria", EnumPropertyAbility.Enable);


                        PropertyViewModelManager.RegisterNotifyPara("IsEnableWaferOnlyOvlResPassCulling", true, "OverlayXErrorUpperLimit", EnumPropertyAbility.Enable);
                        PropertyViewModelManager.RegisterNotifyPara("IsEnableWaferOnlyOvlResPassCulling", true, "OverlayYErrorUpperLimit", EnumPropertyAbility.Enable);
                        PropertyViewModelManager.RegisterNotifyPara("IsEnableWaferOnlyOvlResPassCulling", true, "OverlayPassCriteria", EnumPropertyAbility.Enable);

                        PropertyViewModelManager.RegisterNotifyPara("IsEnableDamageCulling", true, "AlgorithmType", EnumPropertyAbility.Enable);
                        PropertyViewModelManager.RegisterNotifyPara("IsEnableDamageCulling", true, "DamageRatioThresh", EnumPropertyAbility.Enable);
                        PropertyViewModelManager.RegisterNotifyPara("IsEnableDamageCulling", true, "DirtyRatioThresh", EnumPropertyAbility.Enable);
                        PropertyViewModelManager.RegisterNotifyPara("IsEnableDamageCulling", true, "GrayThresh", EnumPropertyAbility.Enable);
                    })));

                }, null);
            }
        }
        public override void Save()
        {
            if (TestInfo != null)
            {
            }
        }
    }
}
