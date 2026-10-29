using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Marvel.DataModels.Job;
using Prism.Services.Dialogs;

namespace Ovl.UI.DataDisplay.ViewModel;

public partial class AlarmProcessingViewModel : ObservableObject
{
    [ObservableProperty]
    string _alarmTitle;
    [ObservableProperty]
    string _message;
    [ObservableProperty]
    int _formWidth;
    [ObservableProperty]
    int _formHeight;
    [ObservableProperty]
    int _leftLocation;
    [ObservableProperty]
    int _topLocation;
    [ObservableProperty]
    JobAlarm _jobAlarm;

    public AlarmProcessingViewModel()
    {
    }
    [RelayCommand]
    public void Confirm()
    {
        RaiseRequestClose(new DialogResult());
    }
    [RelayCommand]
    public void Stop()
    {

    }
    [RelayCommand]
    public void Initialize()
    {

    }
}
public partial class AlarmProcessingViewModel : IDialogAware
{
    public virtual void RaiseRequestClose(IDialogResult dialogResult)
    {
        RequestClose?.Invoke(dialogResult);
    }

    /// <summary>Determines if the dialog can be closed.</summary>
    /// <returns>If <c>true</c> the dialog can be closed. If <c>false</c> the dialog will not close.</returns>
    public bool CanCloseDialog()
    {
        return true;
    }

    /// <summary>Called when the dialog is closed.</summary>
    public void OnDialogClosed()
    {
    }

    /// <summary>Called when the dialog is opened.</summary>
    /// <param name="parameters">The parameters passed to the dialog.</param>
    public void OnDialogOpened(IDialogParameters parameters)
    {

    }

    /// <summary>
    /// The title of the dialog that will show in the window title bar.
    /// </summary>
    public string Title { get; } = "Alarm View";


    /// <summary>
    /// Instructs the <see cref="T:Prism.Services.Dialogs.IDialogWindow" /> to close the dialog.
    /// </summary>
    public event Action<IDialogResult>? RequestClose;
}
