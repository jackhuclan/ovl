using CommunityToolkit.Mvvm.ComponentModel;
using Marvel.DataModels.Enums;
using Marvel.DataModels.Recipe;
using Marvel.Interfaces.Application;
using Microsoft.Extensions.DependencyInjection;
using Prism.Events;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows.Threading;

using Ovl.UI.ViewModels.EventAggregators;
namespace Ovl.UI.ViewModels.Recipe.ViewModel
{
    public partial class Test_PhysicalViewModel:Test_SettingsViewModelBase 
    {
        [ObservableProperty]
        private TestInfoBase _testInfo;
        [ObservableProperty]
        ObservableCollection<EnumNAMode> _enumNAModeList;
        [ObservableProperty]
        ObservableCollection<EnumFilterMode> _enumFilterModeList;
        [ObservableProperty]
        ObservableCollection<EnumOptimizeType> _enumOptimizeTypeList;
        [ObservableProperty]
        ObservableCollection<EnumCalculateType> _enumCalculateTypeList;

        IAlignmentService _alignmentService;
        
        public Test_PhysicalViewModel(IEventAggregator eventAggregator,IServiceProvider serviceProvider):base (eventAggregator)
        {
            _alignmentService = serviceProvider.GetRequiredService<IAlignmentService>();
            LoadData();
        }
        void LoadData()
        {
            EnumCalculateTypeList = new ObservableCollection<EnumCalculateType>(EnumCalculateTypeHelper.GetDisplayOrder());
            EnumOptimizeTypeList = new ObservableCollection<EnumOptimizeType>();
            foreach (EnumOptimizeType value in Enum.GetValues<EnumOptimizeType>())
            {
                EnumOptimizeTypeList.Add(value);
            }
            EnumNAModeList = new ObservableCollection<EnumNAMode>();
            foreach (EnumNAMode value in Enum.GetValues<EnumNAMode>())
            {
                EnumNAModeList.Add(value);
            }
            EnumFilterModeList = new ObservableCollection<EnumFilterMode>();
            foreach (EnumFilterMode value in Enum.GetValues<EnumFilterMode>())
            {
                EnumFilterModeList.Add(value);
            }
             
        }
        public override   void TestInfoRecive(TestInfoAndWaferMap testInfoAndWaferMap)
        {
            if (testInfoAndWaferMap.TestInfo!=null)
            {
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(System.Windows.Application.Current.Dispatcher));
                SynchronizationContext.Current.Post(pl =>
                {
                    TestInfo = testInfoAndWaferMap.TestInfo.TestInfo;
                }, null);
            }
        }
        public override  void Save()
        {
            if (TestInfo != null) 
            {
            }
        }
    }
}
