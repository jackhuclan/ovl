using CommunityToolkit.Mvvm.ComponentModel;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Ovl.UI.ViewModels.EventAggregators;
namespace Ovl.UI.Recipe.ViewModel
{
    public class Test_SettingsViewModelBase : ObservableObject
    {
        readonly IEventAggregator _eventAggregator;
        public Test_SettingsViewModelBase(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
        }

        public void Subscribe()
        {
            UnSubscribe();
            _eventAggregator.GetEvent<TestInfoAndWaferMapEventAggregators>().Subscribe(TestInfoRecive);
            _eventAggregator.GetEvent<SaveEventAggregator>().Subscribe(Save);
        }

        public void UnSubscribe()
        {
            _eventAggregator.GetEvent<TestInfoAndWaferMapEventAggregators>().Unsubscribe(TestInfoRecive);
            _eventAggregator.GetEvent<SaveEventAggregator>().Unsubscribe (Save);
        }

        public virtual void Save()
        {
             
        }

        public virtual void TestInfoRecive(TestInfoAndWaferMap map)
        {
            
        }
    }
}
