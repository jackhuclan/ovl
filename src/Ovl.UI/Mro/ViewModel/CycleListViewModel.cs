using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dm.util;
using InteVega.CBB.Prism.UI.Extensions.MessageBox;
using Marvel.DataModels.Recipe;
using Marvel.Interfaces.Application;
using Marvel.MRO.DataModels.Models;
using Microsoft.Extensions.DependencyInjection;
using OVL.Algorithm.DoNet;
using Prism.Services.Dialogs;
using WinCopies.Util;
using Bases = Ovl.UI.Bases;

using Ovl.UI.Recipe.View;

namespace Ovl.UI.Recipe.ViewModel;

partial class CycleListViewModel : Bases.ViewModelBase
{
    public IMessageBoxService _messageBoxService;
    protected IMroService? _mroService;
    public RecipeElementsInfo recipeElementsInfo;
    public MroElementInfo mroElementInfo;
    public IServiceProvider _serviceProvider;
    public CycleListViewModel(IServiceProvider serviceProvider, RecipeElementsInfo _recipeElementsInfo, MroElementInfo _mroElementInfo)
    {
        _messageBoxService = serviceProvider.GetService<IMessageBoxService>();
        _serviceProvider = serviceProvider;
        _mroService = serviceProvider.GetService<IMroService>();
        recipeElementsInfo = _recipeElementsInfo;
        mroElementInfo = _mroElementInfo;
        InitializeData();
    }


    private void InitializeData()
    {
        Cycles.Clear();
        if (mroElementInfo.CycleParameterList == null || mroElementInfo.CycleParameterList.Count == 0)
        {
            mroElementInfo.CycleParameterList = new List<CycleParameter>();
            CycleParameter cy = new CycleParameter();
            mroElementInfo.CycleParameterList.Add(cy);
            CycleParameter cy1 = new CycleParameter();
            mroElementInfo.CycleParameterList.Add(cy1);
            _mroService.SaveMroConfig(recipeElementsInfo, mroElementInfo);
        }
        int count = 1;
        foreach (var CycleParameter in mroElementInfo.CycleParameterList)
        {
            CycleParameter.CyCleIdenx = count;
            Cycles.Add(new CycleParameterShow
            {
                Name = "Cycle_" + count,
                _CycleParameter = CycleParameter,
            });
            count++;
        }
        SelectedCycle = Cycles.First();
        LoadOptionsCollectionChanged();
    }

    [ObservableProperty]
    private ObservableCollection<CycleParameterShow> _cycles = new ObservableCollection<CycleParameterShow>();

    [ObservableProperty]
    private ObservableCollection<string> _filteringMethods = new ObservableCollection<string>();

    [ObservableProperty]
    private object _aparametersView;
    partial void OnAparametersViewChanged(object oldView, object View)
    {
        if (oldView != null)
        {
            if ((oldView as ParametersKPIChooseView).DataContext is ParametersKPIChooseViewModel viewMode)
            {
                if ((viewMode.Parameters as ParametersView).DataContext is ParametersViewModel parametersView)
                {
                    viewMode.NowCycleParameter.SitesParam.Fields = new ObservableCollection<RowColumnInfo>(parametersView.WaferMapControl.GetSelectedFields());

                }
                if ((viewMode.Kpi as KPIView).DataContext is KPIViewModel model)
                {
                    viewMode.NowCycleParameter.KPIParams.Items.Clear();
                    foreach (var param in model.KpiParameters)
                    {
                        viewMode.NowCycleParameter.KPIParams.Items.add(new FilteringItem
                        {
                            Name = param.Name,
                            IsEnalbe = param.IsEnalbe,
                            Order = param.Order,
                            FilteringMethod = param.FilteringMethod,
                            Threshold = param.Threshold,
                            Percentage = param.Percentage,
                        });
                    }
                }

            }
        }

    }


    [ObservableProperty]
    private CycleParameterShow _selectedCycle;

    partial void OnSelectedCycleChanged(CycleParameterShow oldValue, CycleParameterShow value)
    {
        if (value != null)
        {
            if (value.Name != "Cycle_1" && value.Name != "Cycle_2")
            {
                AparametersView = new ParametersKPIChooseView(_serviceProvider, value._CycleParameter, recipeElementsInfo, false);
            }
            else
            {
                if (value.Name == "Cycle_1")
                {
                    AparametersView = new ParametersKPIChooseView(_serviceProvider, value._CycleParameter, recipeElementsInfo,true,true);
                }
                else
                {
                    AparametersView = new ParametersKPIChooseView(_serviceProvider, value._CycleParameter, recipeElementsInfo);
                }
            }

            if ((AparametersView as ParametersKPIChooseView).DataContext is ParametersKPIChooseViewModel viewMode)
            {
                viewMode.OnCycleParameterChangeder -= ParametersKPIChanger;
                viewMode.OnCycleParameterChangeder += ParametersKPIChanger;
            }
            else
            {
                // 如果 DataContext 为 null，创建并设置
                viewMode = new ParametersKPIChooseViewModel(_serviceProvider, value._CycleParameter, recipeElementsInfo);
                (AparametersView as ParametersKPIChooseView).DataContext = viewMode;
                viewMode.OnCycleParameterChangeder -= ParametersKPIChanger;
                viewMode.OnCycleParameterChangeder += ParametersKPIChanger;
            }
        }
    }

    [RelayCommand]
    public void SaveData()
    {
        mroElementInfo.CycleParameterList.Clear();
        foreach (var item in Cycles)
        {
            mroElementInfo.CycleParameterList.Add(item._CycleParameter);
        }
        _mroService.SaveMroConfig(recipeElementsInfo, mroElementInfo);

        _messageBoxService.Success("CycleParameterList Data saved successfully!!!", "MROConfiguration");
    }

    [RelayCommand]
    private void AddParameter()
    {
        // 添加新参数逻辑
    }

    [RelayCommand]
    private void AddCycle()
    {

        var newCycle = new CycleParameter();
        newCycle.MeasurementParam.FocusScanStep = 0;
        mroElementInfo.CycleParameterList.Add(newCycle);
        //newCycle.OnPropertyValueChanged += CloseCycle;
        var newCycleShoW = new CycleParameterShow
        {
            Name = "Cycle_" + newCycle.CyCleIdenx,
            _CycleParameter = newCycle,
        };
        newCycleShoW.OnPropertyValueChanged += CloseCycle;
        Cycles.Add(newCycleShoW);
        _mroService.SaveMroConfig(recipeElementsInfo, mroElementInfo);
        InitializeData();
    }

    public void ParametersKPIChanger(CycleParameter NowCycleParameter)
    {
        foreach (var parameter in Cycles)
        {
            if (parameter.Name == _selectedCycle.Name)
            {
                parameter._CycleParameter = NowCycleParameter;
            }
        }
        SaveData();
    }


    /// <summary>
    /// 关闭标签页命令
    /// </summary>
    private void CloseCycle(string cycle)
    {
        if (cycle == "")
        {
            return;
        }
        if (cycle == "Cycle_1" || cycle == "Cycle_2")
        {
            _messageBoxService.WarningDialog($"{cycle} is not allowed to turn off");
            return;
        }

        var result = _messageBoxService.AskDialog($"Are you sure close {cycle}?");

        if (result == ButtonResult.OK)
        {
            var itemToRemove = Cycles.FirstOrDefault(p => p.Name == cycle);
            itemToRemove.OnPropertyValueChanged -= CloseCycle;
            mroElementInfo.CycleParameterList.Remove(itemToRemove._CycleParameter);
            Cycles.Remove(itemToRemove);
            _mroService.SaveMroConfig(recipeElementsInfo, mroElementInfo);
            InitializeData();
        }

    }

    public void LoadOptionsCollectionChanged()
    {
        foreach (var item in Cycles)
        {
            item.OnPropertyValueChanged -= CloseCycle;
            item.OnPropertyValueChanged += CloseCycle;
        }
    }

    public void UnLoadOptionsCollectionChanged()
    {
        foreach (var item in Cycles)
        {
            item.OnPropertyValueChanged -= CloseCycle;
        }
        if ((AparametersView as ParametersKPIChooseView).DataContext is ParametersKPIChooseViewModel viewMode)
        {
            viewMode.OnCycleParameterChangeder -= ParametersKPIChanger;
        }
    }
}

partial class CycleParameterShow: ObservableObject
{
    [ObservableProperty]
    private string _name;
    public CycleParameter _CycleParameter { get; set; } = new CycleParameter();

    [RelayCommand]
    private void CloseCycle()
    {
        OnPropertyValueChanged?.Invoke(Name);
    }
    // 自定义属性变更事件
    public event Action<string> OnPropertyValueChanged;
}
