using Marvel.DataModels.Recipe;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Ovl.UI.Recipe.ViewModel;
namespace Ovl.UI.EventAggregators
{
    public class TestInfoAndWaferMap
    {
        public TestManageInfo TestInfo { get; set; }
        public WaferMapInfo WaferMapInfo { get; set; }
    }
    public partial class SaveEventAggregator : PubSubEvent{}
    /// <summary>
    /// 当前选中Test
    /// </summary>
    public class TestInfoEventAggregators : PubSubEvent<TestManageInfo>
    {
    }    
    /// <summary>
    /// 当前选中Test+WaferMap
    /// </summary>
    public class TestInfoAndWaferMapEventAggregators : PubSubEvent<TestInfoAndWaferMap>
    {
    }
}
