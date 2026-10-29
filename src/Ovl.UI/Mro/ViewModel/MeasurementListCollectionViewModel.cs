using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InteVega.CBB.Prism.UI.Extensions.MessageBox;
using Marvel.DataModels.Enums;
using Marvel.DataModels.Recipe;
using Marvel.Interfaces.Application;
using Marvel.MRO.DataModels.Models;
using Megarobo.Ovl.Service.IntfFn.fn;
using Microsoft.Extensions.DependencyInjection;
using WinCopies.Collections;
using Bases = Ovl.UI.Bases;

namespace Ovl.UI.Recipe.ViewModel;

partial class MeasurementListCollectionViewModel : Bases.ViewModelBase
{
    public IMessageBoxService _messageBoxService;
    IServiceFilter _serviceFilter;
    IServiceWavTune _serviceWavTune;
    IServicePolar _servicePolar;
    //IServiceNA _serviceNa;
    protected IMroService? _mroService;
    public RecipeElementsInfo recipeElementsInfo;
    public MroElementInfo mroElementInfo;
    public IServiceProvider _serviceProvider;


    DataCollectionParameter DataCollectionParameter = new DataCollectionParameter();

    public MeasurementListCollectionViewModel(IServiceProvider serviceProvider, RecipeElementsInfo _recipeElementsInfo, MroElementInfo _mroElementInfo)
    {
        _messageBoxService = serviceProvider.GetService<IMessageBoxService>();
        _servicePolar = serviceProvider.GetService<IServicePolar>();
        _serviceFilter = serviceProvider.GetService<IServiceFilter>();
        _serviceWavTune = serviceProvider.GetService<IServiceWavTune>();
        _mroService = serviceProvider.GetService<IMroService>();
        //_serviceNa = serviceProvider.GetService<IServiceNA>();
        mroElementInfo = _mroElementInfo;
        recipeElementsInfo = _recipeElementsInfo;
        DataCollectionParameter = mroElementInfo.DataCollectionParameter;
        InitializeData();

    }

    private void InitializeData()
    {
        if (DataCollectionParameter.NACollection.IsUseRecipe)
        {
            NaUseRecipe = WavelengthMode.UseRecipe;
        }
        else
        {
            NaUseRecipe = WavelengthMode.Reset;
        }

        if (DataCollectionParameter.PolarizerCollection.IsUseRecipe)
        {
            PolarizerUseRecipe = WavelengthMode.UseRecipe;
        }
        else
        {
            PolarizerUseRecipe = WavelengthMode.Reset;
        }

        if (DataCollectionParameter.FilterCollection.IsUseRecipe)
        {
            WavelengthUseRecipe = WavelengthMode.UseRecipe;
        }
        else
        {
            WavelengthUseRecipe = WavelengthMode.Reset;
        }

        if (DataCollectionParameter.BandWidthCollection.IsUseRecipe)
        {
            BandwidthUseRecipe = WavelengthMode.UseRecipe;
        }
        else
        {
            BandwidthUseRecipe = WavelengthMode.Reset;
        }
        WaveTunerMin = DataCollectionParameter.WaveTunerSettings.MinWL;
        WaveTunerMax = DataCollectionParameter.WaveTunerSettings.MaxWL;
        WaveTunerStep = DataCollectionParameter.WaveTunerSettings.WLStep;
        Focus3sigmaRange = DataCollectionParameter.HLKpiSettings.Focus3SigmaRange;
        Wavelength3sigmaRange = DataCollectionParameter.HLKpiSettings.WL3SigmaRange;

        int count = 0;
        if (DataCollectionParameter.NACollection.Items == null)
        {
            // NA Options
            NaOptions = new ObservableCollection<Option>
            {
            new Option { Name = "Low NA",IsSelected = true,Id = 0},
            new Option { Name = "Middle NA",IsSelected = true,Id = 1 },
            new Option { Name = "High NA",IsSelected = true ,Id = 2 }
            };
        }
        else
        {
            // NA Options
            NaOptions = new ObservableCollection<Option>
            {
            new Option { Name = "Low NA",IsSelected = DataCollectionParameter.NACollection.Items.Contains(0),Id = 0},
            new Option { Name = "Middle NA",IsSelected = DataCollectionParameter.NACollection.Items.Contains(1),Id = 1 },
            new Option { Name = "High NA",IsSelected = DataCollectionParameter.NACollection.Items.Contains(2) ,Id = 2 }
            };
        }

        if (DataCollectionParameter.PolarizerCollection.Items == null)
        {
            foreach (var item in _servicePolar.Polars.ToList())
            {
                PolarizerOptions.Add(new Option { Name = item, IsSelected = true, Id = count });
                count++;
            }
        }
        else
        {
            //Polarizer Options
            foreach (var item in _servicePolar.Polars.ToList())
            {
                PolarizerOptions.Add(new Option { Name = item, IsSelected = DataCollectionParameter.PolarizerCollection.Items.Contains(count), Id = count });
                count++;
            }
        }

        count = 0;
        if (DataCollectionParameter.FilterCollection.Items == null)
        {
            foreach (var item in _serviceFilter.Filters.ToList())
            {
                if (item != "Wave Tuner")
                {
                    ColorOptions.Add(new Option { Name = item, IsSelected = true, Id = count });
                }
                else
                {
                    ColorOptions.Add(new Option { Name = "Wave Tuner", IsSelected = true, Id = count });
                    WaveTunerEnable = true;
                }
                count++;
            }
        }
        else
        {
            foreach (var item in _serviceFilter.Filters.ToList())
            {
                if (item != "Wave Tuner")
                {
                    ColorOptions.Add(new Option { Name = item, IsSelected = DataCollectionParameter.FilterCollection.Items.Contains(count), Id = count });
                }
                else
                {
                    ColorOptions.Add(new Option { Name = "Wave Tuner", IsSelected = DataCollectionParameter.FilterCollection.Items.Contains(count), Id = count });
                    WaveTunerEnable = DataCollectionParameter.FilterCollection.Items.Contains(count);
                }
                count++;
            }
        }

        count = 0;
        if (DataCollectionParameter.BandWidthCollection.Items == null)
        {
            foreach (var item in _serviceWavTune.Bws.ToList())
            {
                if (item != "Full")
                {

                    Bandwidth.Add(new Option { Name = item, IsSelected = true, Id = count });
                }
                count++;
            }
        }
        else
        {
            foreach (var item in _serviceWavTune.Bws.ToList())
            {
                if (item != "Full")
                {
                    Bandwidth.Add(new Option { Name = item, IsSelected = DataCollectionParameter.BandWidthCollection.Items.Contains(count), Id = count });
                }

                count++;
            }
        }
    }

    public void LoadOptionsCollectionChanged()
    {
        foreach (var item in ColorOptions)
        {
            item.OnPropertyValueChanged -= OnColorOptionsCollectionChanged;
            item.OnPropertyValueChanged += OnColorOptionsCollectionChanged;
        }
    }
    public void UnLoadOptionsCollectionChanged()
    {
        foreach (var item in ColorOptions)
        {
            item.OnPropertyValueChanged -= OnColorOptionsCollectionChanged;
        }
    }

    /// <summary>
    /// 监听集合项属性变更
    /// </summary>
    private void OnColorOptionsCollectionChanged(string propertyName, object newValue)
    {
        WaveTunerEnable = !(bool)newValue;
    }


    #region NA Options
    [ObservableProperty]
    private ObservableCollection<Option> _naOptions = new ObservableCollection<Option>();

    [ObservableProperty]
    private ObservableCollection<Option> _polarizerOptions = new ObservableCollection<Option>();

    [ObservableProperty]
    private WavelengthMode _naUseRecipe;

    partial void OnNaUseRecipeChanged(WavelengthMode value)
    {
        if (value == 0)
        {
            DataCollectionParameter.NACollection.IsUseRecipe = true;
        }
        else
        {
            DataCollectionParameter.NACollection.IsUseRecipe = false;
        }
    }

    [ObservableProperty]
    private bool _naAll = false;
    partial void OnNaAllChanged(bool value)
    {
        foreach (var item in _naOptions)
        {
            item.IsSelected = value;
        }
    }

    [ObservableProperty]
    private WavelengthMode _polarizerUseRecipe;

    partial void OnPolarizerUseRecipeChanged(WavelengthMode value)
    {
        if (value == 0)
        {
            DataCollectionParameter.PolarizerCollection.IsUseRecipe = true;
        }
        else
        {
            DataCollectionParameter.PolarizerCollection.IsUseRecipe = false;
        }
    }

    [ObservableProperty]
    private bool _polarizerAll = false;

    partial void OnPolarizerAllChanged(bool value)
    {
        foreach (var item in PolarizerOptions)
        {
            item.IsSelected = value;
        }
    }
    #endregion

    #region Wavelength Options
    [ObservableProperty]
    private ObservableCollection<Option> _colorOptions = new ObservableCollection<Option>();

    [ObservableProperty]
    private WavelengthMode _wavelengthUseRecipe;

    partial void OnWavelengthUseRecipeChanged(WavelengthMode value)
    {
        if (value == 0)
        {
            DataCollectionParameter.FilterCollection.IsUseRecipe = true;
        }
        else
        {
            DataCollectionParameter.FilterCollection.IsUseRecipe = false;
        }
    }

    [ObservableProperty]
    private bool _wavelengthAll = false;
    partial void OnWavelengthAllChanged(bool value)
    {
        foreach (var item in ColorOptions)
        {
            item.IsSelected = value;
        }
    }
    #endregion

    #region Bandwidth Options
    [ObservableProperty]
    private ObservableCollection<Option> _bandwidth = new ObservableCollection<Option>();

    [ObservableProperty]
    private WavelengthMode _bandwidthUseRecipe;

    partial void OnBandwidthUseRecipeChanged(WavelengthMode value)
    {
        if (value == 0)
        {
            DataCollectionParameter.BandWidthCollection.IsUseRecipe = true;
        }
        else
        {
            DataCollectionParameter.BandWidthCollection.IsUseRecipe = false;
        }
    }

    [ObservableProperty]
    private bool _bandwidthAll = false;
    partial void OnBandwidthAllChanged(bool value)
    {
        foreach (var item in Bandwidth)
        {
            item.IsSelected = value;
        }
    }
    #endregion
    #region Wave Tuner Settings
    [ObservableProperty]
    private bool _waveTunerEnable = false;

    [ObservableProperty]
    private double _waveTunerMin = 3;

    partial void OnWaveTunerMinChanged(double value)
    {
        DataCollectionParameter.WaveTunerSettings.MinWL = value;
    }

    [ObservableProperty]
    private double _waveTunerMax = 4;

    partial void OnWaveTunerMaxChanged(double value)
    {
        DataCollectionParameter.WaveTunerSettings.MaxWL = value;
    }

    [ObservableProperty]
    private double _waveTunerStep = 1;
    partial void OnWaveTunerStepChanged(double value)
    {
        DataCollectionParameter.WaveTunerSettings.WLStep = value;
    }
    #endregion

    #region High-level KPl Settings
    [ObservableProperty]
    private double _focus3sigmaRange = 3;
    partial void OnFocus3sigmaRangeChanged(double value)
    {
        DataCollectionParameter.HLKpiSettings.Focus3SigmaRange = value;
    }

    [ObservableProperty]
    private double _wavelength3sigmaRange = 4;
    partial void OnWavelength3sigmaRangeChanged(double value)
    {
        DataCollectionParameter.HLKpiSettings.WL3SigmaRange = value;
    }
    #endregion

    [RelayCommand]
    public void Save()
    {
        if (!bIsPossibleToSave())
        {
            return;
        }
        DataCollectionParameter.NACollection.Items.Clear();
        foreach (var item in NaOptions.Where(s => s.IsSelected == true).ToList())
        {
            DataCollectionParameter.NACollection.Items.Add(item.Id);
        }

        DataCollectionParameter.PolarizerCollection.Items.Clear();
        foreach (var item in PolarizerOptions.Where(s => s.IsSelected == true).ToList())
        {
            DataCollectionParameter.PolarizerCollection.Items.Add(item.Id);
        }

        DataCollectionParameter.BandWidthCollection.Items.Clear();
        foreach (var item in Bandwidth.Where(s => s.IsSelected == true).ToList())
        {
            DataCollectionParameter.BandWidthCollection.Items.Add(item.Id);
        }

        DataCollectionParameter.FilterCollection.Items.Clear();
        foreach (var item in ColorOptions.Where(s => s.IsSelected == true).ToList())
        {
            DataCollectionParameter.FilterCollection.Items.Add(item.Id);
        }

        mroElementInfo.DataCollectionParameter = DataCollectionParameter;
        _mroService.SaveMroConfig(recipeElementsInfo, mroElementInfo);
        _messageBoxService.Success("DataCollectionParameter Data saved successfully!!!", "MROConfiguration");
    }

    [RelayCommand]
    private void Add()
    {

    }

    /// <summary>
    /// 判定是否能进行报错操作
    /// </summary>
    public bool bIsPossibleToSave()
    {

        if (!DataCollectionParameter.NACollection.IsUseRecipe && NaOptions.Where(s => s.IsSelected == true).ToList().Count == 0)
        {
            _messageBoxService.Warning("At least one NA option should be selected when not using recipe setting.", "MROConfiguration");
            return false;
        }
        if (!DataCollectionParameter.PolarizerCollection.IsUseRecipe && PolarizerOptions.Where(s => s.IsSelected == true).ToList().Count == 0)
        {
            _messageBoxService.Warning("At least one Polarizer option should be selected when not using recipe setting.", "MROConfiguration");
            return false;
        }
        if (!DataCollectionParameter.FilterCollection.IsUseRecipe && ColorOptions.Where(s => s.IsSelected == true).ToList().Count == 0)
        {
            _messageBoxService.Warning("At least one Wavelength option should be selected when not using recipe setting.", "MROConfiguration");
            return false;
        }
        if (ColorOptions.Where(s => s.IsSelected == true).Any(s => s.Id == 12) &&
            !DataCollectionParameter.BandWidthCollection.IsUseRecipe && Bandwidth.Where(s => s.IsSelected == true).ToList().Count == 0)
        {
            _messageBoxService.Warning("At least one Bandwidth option should be selected when Wave Tuner is selected and not using recipe setting.", "MROConfiguration");
            return false;
        }
        return true;
    }
}
partial class Option : ObservableObject
{
    // 自定义属性变更事件
    public event Action<string, object> OnPropertyValueChanged;

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private int _id;

    partial void OnIsSelectedChanged(bool value)
    {
        if (Id == 12)
        {
            OnPropertyValueChanged?.Invoke(nameof(Name), !value);
        }
    }

}
