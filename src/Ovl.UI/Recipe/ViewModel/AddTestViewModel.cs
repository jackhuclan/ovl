using System.Text;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InteVega.CBB.Prism.UI.Extensions.MessageBox;
using Marvel.DataModels.Job;
using Marvel.DataModels.Recipe;
using Prism.Services.Dialogs;
using Bases = Ovl.UI.Bases;

using DataModels = Marvel.DataModels;

namespace Ovl.UI.Recipe.ViewModel;

public partial class AddTestViewModel : Bases.ViewModelBase
{
    public AddTestViewModel(string defaultName)
    {
        Test = new TestInfo_BiB( DataModels.Enums.EnumTargetType.Bar) { Name = defaultName };
    }

    [ObservableProperty]
    TestInfoBase _test;

    [ObservableProperty]
    bool _isRename;

    /// <summary>
    /// 检查名称是否正常
    /// </summary>
    public Func<bool> FunCheckName;
    [RelayCommand]
    public void SaveData()
    {
        var res = FunCheckName?.Invoke();
        if (false == res)
        {
            return;
        }
        RaiseRequestClose(new DialogResult());
    }
    [RelayCommand]
    public void Close()
    {
        Test = null;
        RaiseRequestClose(new DialogResult());
    }
}
