using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Marvel.DataModels.Enums;
using Marvel.DataModels.Recipe;
using Marvel.DataModels.Recipe.CD;
using Marvel.DataModels.Recipe.Overlay;
using Marvel.DataModels.Recipe40;

namespace Ovl.UI.Recipe.ViewModel;

public partial class TestManageInfo : ObservableObject
{
    [ObservableProperty]
    private string _id;
    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private EnumTargetType _innerType;
    [ObservableProperty]
    private EnumTargetType _outerType;
    [ObservableProperty]
    private Visibility _isShowButton=Visibility.Collapsed;
    [ObservableProperty]
    private bool _isEnable;
    [ObservableProperty]
    private bool _nameEditFlag=true;
    //[ObservableProperty]
    //public Visibility _isShowTargetType=Visibility.Collapsed;

    public TestInfoBase TestInfo { get; set; }
    public Action<TestManageInfo> OpenTestCallback { get; set; }
    public Action<TestInfoBase> TrainTestCallback { get; set; }
    public Action<TestManageInfo> CopyTestCallback { get; set; }
    public Action<TestManageInfo> RenameTestCallback { get; set; }
    public Action<TestManageInfo> EnableTestCallback { get; set; }
    public Action<TestManageInfo> DisableTestCallback { get; set; }
    public Action<TestManageInfo> DeleteTestCallback { get; set; }
    [RelayCommand]
    public void OpenTest()
    {
        OpenTestCallback?.Invoke(this);
    }
    [RelayCommand]
    private void TrainTest()
    {
        TrainTestCallback?.Invoke(TestInfo);
    }
    [RelayCommand]
    private void CopyTest()
    {
        CopyTestCallback?.Invoke(this);
    }
    [RelayCommand]
    private void RenameTest()
    {
        RenameTestCallback?.Invoke(this);
    }
    [RelayCommand]
    private void EnableTest()
    {
        EnableTestCallback?.Invoke(this);
    }
    [RelayCommand]
    public void DisableTest()
    {
        DisableTestCallback?.Invoke(this);
    }
    [RelayCommand]
    private void DeleteTest()
    {
        DeleteTestCallback?.Invoke(this);
    }
}
