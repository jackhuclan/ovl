using Bases = Ovl.UI.ViewModels.Bases;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dm;
using InteVega.CBB.Prism.UI.Extensions.MessageBox;
using InteVega.CBB.Recipe;
using InteVega.CBB.Recipe.Interface;
using Marvel.DataModels.Job;
using Marvel.DataModels.Recipe;
using Marvel.Interfaces.Application;
using Marvel.MRO.DataModels.Models;
using Marvel.WaferMapCommon;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

using Ovl.UI.Recipe.View;
namespace Ovl.UI.Recipe.ViewModel
{
    partial class MROConfigurationViewModel : Bases.ViewModelBase
    {
        public event EventHandler RequestClose;
        IRecipeManager _RecipeManager;
        protected IMroService? _mroService;
        public RecipeElementsInfo recipeElementsInfo;
        public MroElementInfo mroElementInfo;
        public IServiceProvider _serviceProvider;
        public IMessageBoxService _messageBoxService;

        public MeasurementListCollectionView _measurementListCollectionView;
        public MRORunParametersView _mroRunParametersView;
        public ComprehensiveAbnormalFactorsView _comprehensiveAbnormalFactorsView;
        public CycleListView _cycleListView;
        public ObservableCollection<NavigationItem> NavigationItems { get; set; }

        public MROConfigurationViewModel(IServiceProvider serviceProvider, string RecipeId)
        {
            _serviceProvider = serviceProvider;
            _messageBoxService = serviceProvider.GetService<IMessageBoxService>();
            _RecipeManager = serviceProvider.GetService<IRecipeManager>();
            _mroService = serviceProvider.GetService<IMroService>();
            var recipe = _RecipeManager.GetCurrentRecipe(RecipeId);
            recipeElementsInfo = recipe.Get<RecipeElementsInfo>();
            try
            {
                mroElementInfo = _mroService.OpenMroConfig(recipeElementsInfo);
            }
            catch (Exception)
            {
            }
            if (mroElementInfo == null)
            {
                mroElementInfo = new MroElementInfo();
            }
            _mroService = serviceProvider.GetService<IMroService>();
            InitializeData();

        }
        
        private void InitializeData()
        {
            // Navigation Items
            NavigationItems = new ObservableCollection<NavigationItem>
            {
            new NavigationItem { Name = "Run Parameters", IsSelected = true },
            new NavigationItem { Name = "Data Collection" },
            new NavigationItem { Name = "CAF" },
            new NavigationItem { Name = "Cycle List" }
            };
            SelectedNavigationItem = NavigationItems.First();
            _measurementListCollectionView = new MeasurementListCollectionView(_serviceProvider, recipeElementsInfo, mroElementInfo);
            _mroRunParametersView = new MRORunParametersView(_serviceProvider, recipeElementsInfo, mroElementInfo);
            _comprehensiveAbnormalFactorsView = new ComprehensiveAbnormalFactorsView(_serviceProvider, recipeElementsInfo, mroElementInfo);
            _cycleListView = new CycleListView(_serviceProvider, recipeElementsInfo, mroElementInfo);
            CurrentView = _mroRunParametersView;

        }
        [ObservableProperty]
        private object _currentView;


        [ObservableProperty]
        private NavigationItem _selectedNavigationItem;

        partial void OnSelectedNavigationItemChanged(NavigationItem Value)
        {
            switch (Value.Name)
            {
                case "Run Parameters":
                    CurrentView = _mroRunParametersView;//OK
                    break;
                case "Data Collection":
                    CurrentView = _measurementListCollectionView;
                    break;
                case "CAF":
                    CurrentView = _comprehensiveAbnormalFactorsView;
                    break;
                case "Cycle List":
                    CurrentView = _cycleListView;
                    break;
                default:
                    break;
            }
        }

        [RelayCommand]
        private void Save()
        {
            //_mroService.SaveMroConfig(recipeElementsInfo, mroElementInfo);
            
            if (_measurementListCollectionView.DataContext is MeasurementListCollectionViewModel MeasurementViewModel)
            {
                if (!MeasurementViewModel.bIsPossibleToSave())
                {   
                    return;
                }
                MeasurementViewModel.Save(); //保存measurementList
            }
            if (_mroRunParametersView.DataContext is MRORunParametersViewModel MRORunParametersModel)
            {
                if (MRORunParametersModel.Check() == false)
                {
                    return;
                }
                MRORunParametersModel.Save();//保存RunParameters
            }
            if (_comprehensiveAbnormalFactorsView.DataContext is ComprehensiveAbnormalFactorsViewModel ComprehensiveAbnormalFactorsModel)
            {
                ComprehensiveAbnormalFactorsModel.Save();//保存RunParameters
            }
            if (_cycleListView?.DataContext is CycleListViewModel cycleViewModel)
            {
                if (cycleViewModel.AparametersView is ParametersKPIChooseView parametersKPIChooseView)
                {
                    if (parametersKPIChooseView?.DataContext is ParametersKPIChooseViewModel parametersKPIChooseViewModel)
                    {
                        if (!parametersKPIChooseViewModel.JudgementTheFieldIDCount())
                        {
                            return;
                        }
                        parametersKPIChooseViewModel.Save();
                    }
                }
            }
            try
            {
                _mroService.CheckMroConfig(recipeElementsInfo, mroElementInfo);
            }
            catch (Exception ex)
            {
                _messageBoxService.Warning(ex.Message , "MROConfiguration");
                return  ;
            }
            // 请求关闭窗口
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void Close()
        {
            // 请求关闭窗口
            RequestClose?.Invoke(this, EventArgs.Empty);
        }
    }

    partial class NavigationItem : ObservableObject
    {
        [ObservableProperty]
        private string _name;
        [ObservableProperty]
        private bool _isSelected;
    }
}
