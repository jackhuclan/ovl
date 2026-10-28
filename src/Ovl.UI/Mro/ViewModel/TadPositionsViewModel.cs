using Bases = Ovl.UI.ViewModels.Bases;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InteVega.CBB.Common.Algorithm.Shapes;
using NPOI.SS.Formula.Functions;
using OVL.Algorithm.DoNet;
using Superpower.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ovl.UI.Mro.ViewModel
{
    partial class TadPositionsViewModel : Bases.ViewModelBase
    {
        
        public TadPositionsViewModel(ObservableCollection<Point2d> pTadPositionsData)
        {
            PTadPositionsData = pTadPositionsData;
            InitializeData();
        }

        private void InitializeData()
        {
            TadPositionsDatas.Clear();
            int count = 1;
            foreach (var Positions in PTadPositionsData)
            {
                var tadPositionsShow = new TadPositionsShow
                {
                    Index = count,
                    X = Positions.X,
                    Y = Positions.Y
                };
                tadPositionsShow.OnResidualModelChangeder += PointChanged;
                TadPositionsDatas.Add(tadPositionsShow);
                count++;
            }
            SelectedPosition = TadPositionsDatas?.FirstOrDefault();
        }

        public void UnloadnResidualModelChangede()
        {
            foreach (var item in TadPositionsDatas)
            {
                item.OnResidualModelChangeder -= PointChanged;
            }
        }

        [ObservableProperty]
        public ObservableCollection<TadPositionsShow> _tadPositionsDatas = new ObservableCollection<TadPositionsShow>();

        [ObservableProperty]
        public TadPositionsShow _selectedPosition;

        public void PointChanged(double X,double Y,int Index)
        {
            PTadPositionsData.Clear();
            foreach (var item in TadPositionsDatas)
            {
                if (Index == item.Index)
                {
                    item.X = X;
                    item.Y = Y;
                }
                PTadPositionsData.Add(new Point2d(item.X, item.Y));
            }
            OnPTadPositionsDataChanged(PTadPositionsData);
        }
        [ObservableProperty]
        public ObservableCollection<Point2d> _pTadPositionsData;
        partial void OnPTadPositionsDataChanged(ObservableCollection<Point2d> value)
        {
            OnTadPositionsDataChangeder?.Invoke(value);
        }

        public event Action<ObservableCollection<Point2d>> OnTadPositionsDataChangeder;

        [RelayCommand]
        private void AddPosition()
        {
            var tadPositionsShow = new TadPositionsShow
            {
                Index = TadPositionsDatas.Count + 1,
                X = 0,
                Y = 0
            };
            TadPositionsDatas.Add(tadPositionsShow);
            PTadPositionsData.Clear();
            foreach (var item in TadPositionsDatas)
            {
                PTadPositionsData.Add(new Point2d(item.X, item.Y));
            }
            OnPTadPositionsDataChanged(PTadPositionsData);
            InitializeData();
        }

        [RelayCommand]
        private void DeletePosition()
        {
           
            TadPositionsDatas.Remove(_selectedPosition);
            PTadPositionsData.Clear();
            foreach (var item in TadPositionsDatas)
            {
                PTadPositionsData.Add(new Point2d(item.X, item.Y));
            }
            OnPTadPositionsDataChanged(PTadPositionsData);
            InitializeData();
        }

    }

    partial class TadPositionsShow : ObservableObject
    {
        [ObservableProperty]
        public int _index;


        [ObservableProperty]
        public double _x;
        partial void OnXChanged(double value)
        {
            OnResidualModelChangeder?.Invoke(value,Y ,_index);
        }

        [ObservableProperty]
        public double _y;

        partial void OnYChanged(double value)
        {
            OnResidualModelChangeder?.Invoke(X, value, _index);
        }


        // 属性变更事件
        public event Action<double,double,int> OnResidualModelChangeder;
    }
}
