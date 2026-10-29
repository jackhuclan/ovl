using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Marvel.DataModels.Recipe;
using Prism.Services.Dialogs;
using Bases = Ovl.UI.Bases;

namespace Ovl.UI.Recipe.ViewModel;

internal partial class AddArrayTargetViewModel : Bases.ViewModelBase
{
    [ObservableProperty]
    TestInfoBase _test;

    [ObservableProperty]
    TargetArrayLocation _arrayTarget;

    /// <summary>
    /// 检查名称是否正常
    /// </summary>
    public Func<bool> FunSave;
    [RelayCommand]
    public void SaveData()
    {
        var res = FunSave?.Invoke();
        if (false == res)
        {
            return;
        }
        RaiseRequestClose(new DialogResult());
    }
    [RelayCommand]
    public void Close()
    {
        RaiseRequestClose(new DialogResult());
    }
}
