using InteVega.CBB.StageControlModule.EventAggregators;
using Marvel.Interfaces.Application;
using Microsoft.Extensions.DependencyInjection;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ovl.UI.ViewModels.Recipe.ViewModel
{
    internal class Test_SetR3ViewModel40 : Test_SetR3ViewModel
    {
        private readonly IEventAggregator _eventAggregator;

        public Test_SetR3ViewModel40(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _eventAggregator = serviceProvider.GetService<IEventAggregator>();
        }

        internal override void Loaded()
        {
            base.Loaded();
            SetRecipeWaferHeightToFocusControl();
        }

        private void SetRecipeWaferHeightToFocusControl()
        {
            var alignmentService40 = _serviceProvider.GetRequiredService<IAlignmentService>() as IAlignmentService40;
            _eventAggregator?.GetEvent<SetSpecWaferHeightEvent>().Publish(alignmentService40.WaferMapInfo.Thickness);
        }
    }
}
