using Bases = Ovl.UI.ViewModels.Bases;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InteVega.CBB.Prism.UI.Extensions.MessageBox;
using InteVega.CBB.Recipe;
using InteVega.CBB.Recipe.Interface;
using Marvel.DataModels.Recipe;
using Marvel.Interfaces.Application;
using Marvel.MRO.DataModels.Models;
using Microsoft.Extensions.DependencyInjection;
using OVL.Algorithm.DoNet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using static Quartz.Logging.OperationName;

namespace Ovl.UI.Recipe.ViewModel
{
    partial class ComprehensiveAbnormalFactorsViewModel : Bases.ViewModelBase
    {
        protected IMroService? _mroService;
        public RecipeElementsInfo recipeElementsInfo; 
        public MroElementInfo mroElementInfo;
        public IMessageBoxService _messageBoxService;
        [ObservableProperty]
        public ObservableCollection<CafParameter> _cafParameters = new ObservableCollection<CafParameter>();

        public ComprehensiveAbnormalFactorsViewModel(IServiceProvider serviceProvider, RecipeElementsInfo _recipeElementsInfo,MroElementInfo _mroElementInfo)
        {
            _messageBoxService = serviceProvider.GetService<IMessageBoxService>();
            _mroService = serviceProvider.GetService<IMroService>();
            mroElementInfo = _mroElementInfo;
            recipeElementsInfo = _recipeElementsInfo;
            InitializeData();
        }

        private void InitializeData()
        {
            int count = 1;
            foreach (var parameter in mroElementInfo.CAFParameter.MoabParameter.Items) 
            {
                CafParameters.Add(new CafParameter { Id = count, Name = parameter.Name, IsEnabled = parameter.IsEnable, Mean = parameter.Mean, Alpha = parameter.Alpha });
                count++;
            }
        }

        [RelayCommand]
        public void Save()
        {
            // 实现保存逻辑
            mroElementInfo.CAFParameter.MoabParameter.Items.Clear();
            foreach (var parameter in CafParameters)
            {
                mroElementInfo.CAFParameter.MoabParameter.Items.Add(new MOABItem { Name = parameter.Name, IsEnable = parameter.IsEnabled, Mean = parameter.Mean, Alpha = parameter.Alpha });
            }
            _mroService.SaveMroConfig(recipeElementsInfo, mroElementInfo);
            _messageBoxService.Success("CAFParameter Data saved successfully!!!", "MROConfiguration");
        }

        [RelayCommand]
        private void SelectAll()
        {

        }

        [RelayCommand]
        private void EnableAll()
        {

        }

        [RelayCommand]
        private void DisableAll()
        {
            foreach (var param in CafParameters)
            {
                //param.IsEnabled = false;
            }
        }
    }

    partial class CafParameter : ObservableObject
    {
        [ObservableProperty]
        private int _id;
        [ObservableProperty]
        private ItempName _name;
        [ObservableProperty]
        private bool _isEnabled;
        [ObservableProperty]
        private double _mean;
        [ObservableProperty]
        private double _alpha;

        [RelayCommand]
        private void MeanUp()
        {
            Mean += 0.1;
        }

        [RelayCommand]
        private void MeanDown()
        {
            Mean -= 0.1;
        }

        [RelayCommand]
        private void AlphaUp()
        {
            Alpha += 0.1;
        }

        [RelayCommand]
        private void AlphaDown()
        {
            Alpha -= 0.1;
        }
    }
}
