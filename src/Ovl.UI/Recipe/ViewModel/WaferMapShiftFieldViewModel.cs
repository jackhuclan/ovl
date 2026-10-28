using Bases = Ovl.UI.ViewModels.Bases;
using CommunityToolkit.Mvvm.ComponentModel;
using InteVega.CBB.Prism.UI.Extensions.MessageBox;
using Marvel.DataModels.Recipe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ovl.UI.Recipe.ViewModel
{
    public partial class WaferMapShiftFieldViewModel : Bases.ViewModelBase
    {

        public WaferMapShiftFieldViewModel(WaferMapInfo waferMapInfo, IMessageBoxService messageBoxService)
        {
            WaferMapInfo = waferMapInfo;
            _messageBoxService = messageBoxService;
        }

        [ObservableProperty]
        WaferMapInfo _waferMapInfo;

        IMessageBoxService _messageBoxService;
    }
}
