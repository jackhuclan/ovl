using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Ovl.UI.Recipe.View;
using Ovl.UI.Recipe.ViewModel;
namespace Ovl.UI
{
    public class MainModelPrism : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            var regionManager = containerProvider.Resolve<IRegionManager>();
            //regionManager.RegisterViewWithRegion("ContentRegionTestList", typeof(TestManageView));
            //regionManager.RegisterViewWithRegion("ContentRegionAllTestPage", typeof(Test_TargetView));
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<TestManageView, TestManageViewModel>();

            //containerRegistry.RegisterForNavigation<Test_TargetView,Test_TargetViewModel>();
        }
    }
}
