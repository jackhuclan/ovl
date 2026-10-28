using Bases = Ovl.UI.ViewModels.Bases;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InteVega.CBB.Prism.UI.Extensions.MessageBox;
using InteVega.CBB.Recipe.Interface;
using Marvel.DataModels.Recipe;
using Marvel.Interfaces.Application;
using Marvel.MRO.DataModels.Models;
using Marvel.WaferMapCommon;
using Microsoft.Extensions.DependencyInjection;
using OVL.Algorithm.DoNet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Ovl.UI.Recipe.View;
namespace Ovl.UI.Recipe.ViewModel
{
    partial class ParametersKPIChooseViewModel : Bases.ViewModelBase
    {
        protected IMroService? _mroService;
        public RecipeElementsInfo recipeElementsInfo;
        public IServiceProvider _serviceProvider;
        public IMessageBoxService _messageBoxService;
        public CycleParameter NowCycleParameter = new CycleParameter();

        public ParametersKPIChooseViewModel(IServiceProvider serviceProvider, CycleParameter CycleParameter, RecipeElementsInfo _recipeElementsInfo,bool Enable = true, bool FirstCycle = false)
        {
            recipeElementsInfo = _recipeElementsInfo;
            _mroService = serviceProvider.GetService<IMroService>();
            _messageBoxService = serviceProvider.GetService<IMessageBoxService>();
            NowCycleParameter = CycleParameter;

            InitializeData(serviceProvider,NowCycleParameter, _recipeElementsInfo, Enable, FirstCycle);
        }
        private void InitializeData(IServiceProvider serviceProvider,CycleParameter CycleParameter, RecipeElementsInfo _recipeElementsInfo, bool Enable = true, bool FirstCycle = false)
        {
            Parameters = new ParametersView(serviceProvider, CycleParameter, _recipeElementsInfo ,Enable, FirstCycle);
            Kpi = new KPIView(CycleParameter);
            SelectedTab = 0;
        }
        [ObservableProperty]
        private int _selectedTab;

        partial void OnSelectedTabChanged(int value)
        {

            //Save();
        }

        [ObservableProperty]
        public ParametersView _parameters;

        [ObservableProperty]
        public KPIView _kpi;

        [ObservableProperty]
        public bool _parameterVisable = false;

        [ObservableProperty]
        public bool _kpiVisable = true;
        [RelayCommand]
        public void ShowParameter()
        {
            ParameterVisable = false;
            KpiVisable = true;
            Save();
        }

        [RelayCommand]
        public void ShowKpi()
        {
            ParameterVisable = true;
            KpiVisable = false;
            Save();
        }

        [RelayCommand]
        public void Save()
        {
            NowCycleParameter = (Parameters.DataContext as ParametersViewModel)._CyccleParameter;
            if ((Kpi.DataContext as KPIViewModel).KpiParameters.Count > 0)
            {
                NowCycleParameter.KPIParams.Items.Clear();
                foreach (var param in (Kpi.DataContext as KPIViewModel).KpiParameters)
                {
                    NowCycleParameter.KPIParams.Items.Add(new FilteringItem
                    {
                        Name = param.Name,
                        IsEnalbe = param.IsEnalbe,
                        Order = param.Order,
                        FilteringMethod = param.FilteringMethod,
                        Threshold = param.Threshold,
                        Percentage = param.Percentage,
                    });
                    NowCycleParameter.PutParam.ResidualModel = (Kpi.DataContext as KPIViewModel)._cycleParameter.PutParam.ResidualModel;
                }
            }
            NowCycleParameter.SitesParam.Fields = new ObservableCollection<RowColumnInfo>((Parameters.DataContext as ParametersViewModel).WaferMapControl.GetSelectedFields());
           OnCycleParameterChangeder?.Invoke(NowCycleParameter);
        }

        public bool JudgementTheFieldIDCount()
        {
            if ((Parameters.DataContext as ParametersViewModel).WaferMapControl.GetSelectedFields().Count > 0)
            {
                return true;
            }
            else
            {
                _messageBoxService.WarningDialog($"In Cycle{(Parameters.DataContext as ParametersViewModel)._CyccleParameter.CyCleIdenx},Please select at least one FieldID in the WaferMap.");
                return false;
            }
        }

        public event Action<CycleParameter> OnCycleParameterChangeder;
    }
}
