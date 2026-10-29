using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using InteVega.CBB.Prism.UI.Extensions.MessageBox;
using Marvel.DataModels.Recipe;
using Bases = Ovl.UI.Bases;

namespace Ovl.UI.Recipe.ViewModel;

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
