using CommunityToolkit.Mvvm.ComponentModel;
using Marvel.DataModels.Enums;
using Marvel.DataModels.Recipe;
using Prism.Events;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows.Threading;

using Ovl.UI.ViewModels.EventAggregators;
namespace Ovl.UI.Recipe.ViewModel
{
    public partial class Test_DataCullingViewModel : Test_SettingsViewModelBase
    {
        [ObservableProperty]
        private TestInfoBase _testInfo;



        public Test_DataCullingViewModel(IEventAggregator eventAggregator) : base(eventAggregator)
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
