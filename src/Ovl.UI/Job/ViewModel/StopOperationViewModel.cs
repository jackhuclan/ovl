using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Marvel.DataModels.Enums;
using Prism.Services.Dialogs;
using Bases = Ovl.UI.Bases;

namespace Ovl.UI.Job.ViewModel;

public partial class StopOperationViewModel : Bases.ViewModelBase
{

    public override string Title { get; set; } = "Stop Operation";
    public Action<EnumStopMode> ActionStopMode { get; set; }

    public Action ActionCancel { get; set; }

    [ObservableProperty]
    private bool _isAbortWaferVisible = true;

    [RelayCommand]
    public void AbortWafer()
    {
        Close();
        ActionStopMode?.Invoke(EnumStopMode.AbortWafer);

    }

    [RelayCommand]
    public void AbortJob()
    {
        Close();
        ActionStopMode?.Invoke(EnumStopMode.AbortJob);
    }

    [RelayCommand]
    public void AbortQueue()
    {
        Close();
        ActionStopMode?.Invoke(EnumStopMode.AbortQueue);
    }

    //[RelayCommand]
    public void Close()
    {
        RaiseRequestClose(new DialogResult());
    }
    [RelayCommand]
    public void Cancel()
    {
        ActionCancel?.Invoke();
        Close();
    }
}
