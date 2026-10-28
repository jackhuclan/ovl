using Bases = Ovl.UI.ViewModels.Bases;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InteVega.CBB.Common.Algorithm.Shapes;
using InteVega.CBB.DomainService.Api.Interfaces;
using InteVega.CBB.MotionController.Entities.Io;
using InteVega.CBB.StageControlModule.ViewModels;
using Marvel.DataModels.Recipe;
using Marvel.Interfaces.Application;
using Marvel.MRO.DataModels.Models;
using NPOI.SS.Formula.Functions;
using OVL.Algorithm.DoNet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

using Ovl.UI.Mro.ViewModel;
using Ovl.UI.Recipe.View;
namespace Ovl.UI.Recipe.ViewModel
{
    partial class KPIViewModel : Bases.ViewModelBase
    {
        [ObservableProperty]
        public List<FilteringItemShow> _kpiParameters = new List<FilteringItemShow>();

        public TadPositionsView tadPositionsView ;
        public CycleParameter _cycleParameter = new CycleParameter();
        //[ObservableProperty]
        //public Array _arrFilteringMethods = Enum.GetValues(typeof(FilteringMethod));

        public KPIViewModel(CycleParameter KpiParameters)
        {
            _cycleParameter = KpiParameters;
            InitializeData();
        }

        private void InitializeData()
        {
            KpiParameters.Clear();
            // 初始化KPI参数数据
            foreach (var param in _cycleParameter.KPIParams.Items) 
            {
                var filteringItemShow = new FilteringItemShow
                {
                    Name = param.Name,
                    IsEnalbe = param.IsEnalbe,
                    Order = param.Order,
                    FilteringMethod = param.FilteringMethod,
                    Threshold = param.Threshold,
                    Percentage = param.Percentage,
                    TADPositions = param.Name == ItempName.TisVsTADPosition3Sigma ? false : true,
                    W1F1 = param.Name == ItempName.Residuals ? false : true,
                    ResidualModel = _cycleParameter.PutParam.ResidualModel
                };
                //filteringItemShow.OnResidualModelChangeder += ResidualModelChanged;
                //filteringItemShow.Click += TadPoisitionsClick;
                KpiParameters.Add(filteringItemShow);
            }
            tadPositionsView = new TadPositionsView(_cycleParameter.TadPositionList);
            // 检查 DataContext 或 Content
        }

        public void LoadOptionsCollectionChanged()
        {
            foreach (var item in KpiParameters)
            {
                item.OnResidualModelChangeder -= ResidualModelChanged;
                item.Click -= TadPoisitionsClick;
                item.OnResidualModelChangeder += ResidualModelChanged;
                item.Click += TadPoisitionsClick;
            }
        }

        public void UnLoadOptionsCollectionChanged()
        {
            foreach (var item in KpiParameters)
            {
                item.OnResidualModelChangeder -= ResidualModelChanged;
                item.Click -= TadPoisitionsClick;
            }
        }

        private void ResidualModelChanged(ResidualModel value)
        {
            _cycleParameter.PutParam.ResidualModel = value;
        }

        private void TadPoisitionsClick()
        {
            tadPositionsView = new TadPositionsView(_cycleParameter.TadPositionList);
            if (tadPositionsView.DataContext is TadPositionsViewModel viewModel)
            {
                viewModel.OnTadPositionsDataChangeder += TadPoisitionsChangerSave;
            }
            else
            {
                // 如果 DataContext 为 null，创建并设置
                viewModel = new TadPositionsViewModel(_cycleParameter.TadPositionList);
                tadPositionsView.DataContext = viewModel;
                viewModel.OnTadPositionsDataChangeder += TadPoisitionsChangerSave;
            }
            tadPositionsView.ShowDialog();
            viewModel.OnTadPositionsDataChangeder -= TadPoisitionsChangerSave;
        }

        public void TadPoisitionsChangerSave(ObservableCollection<Point2d> TadPoisitions)
        {
            _cycleParameter.TadPositionList = TadPoisitions;
        }
    }

    partial class FilteringItemShow: ObservableObject
    {
        [ObservableProperty]
        public ItempName _name;

        [ObservableProperty]
        public bool _isEnalbe;

        [ObservableProperty]
        public int _order;

        [ObservableProperty]
        public FilteringMethod _filteringMethod;

        [ObservableProperty]
        public double _threshold;

        [ObservableProperty]
        public int _percentage;

        partial void OnPercentageChanged(int value)
        {
            if (value < 0)
            {
                Percentage = 0;
            }
            else if(value > 100)
            {
                Percentage = 100;
            }
        }

        [ObservableProperty]
        public bool _tADPositions;


        [ObservableProperty]
        public bool _w1F1;

        [ObservableProperty]
        public ResidualModel _residualModel;

        partial void OnResidualModelChanged(ResidualModel value)
        {
            OnResidualModelChangeder?.Invoke(value);
        }

        [RelayCommand]
        private void ThresholdUp()
        {
            Threshold += 1;
        }
        [RelayCommand]
        private void ThresholdDown()
        {
            Threshold -= 1;
        }

        [RelayCommand]
        private void PercentageUp()
        {
            Percentage += 1;
        }
        [RelayCommand]
        private void PercentageDown()
        {
            Percentage -= 1;
        }
        [RelayCommand]
        private void ButtonClick()
        {
            Click?.Invoke();
        }

        // 属性变更事件
        public event Action<ResidualModel> OnResidualModelChangeder;

        // 按钮点击事件
        public event Action Click;
    }

}
