using InteVega.CBB.Mvvm.Bases;
using Marvel.DataModels.Core;
using Marvel.DataModels.Job;
using Marvel.DataModels.Recipe;
using OVL.Algorithm.DoNet;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Ovl.UI.ViewModels.Models
{
    public class RunningJobDataAndStatistics : BindableBase
    {
        public ObservableCollection<TargetResultBase> RawData { get; set; }

        private bool _isSelectedRawData;
        public bool IsSelectedRawData
        {
            get { return _isSelectedRawData; }
            set { SetProperty(ref _isSelectedRawData, value); }
        }
        private bool _isVisibleRawData;
        public bool IsVisibleRawData
        {
            get { return _isVisibleRawData; }
            set { SetProperty(ref _isVisibleRawData, value); }
        }

        public ObservableCollection<TargetOVLResult> OVLData { get; set; }
        private bool _isSelectedOVLData;
        public bool IsSelectedOVLData
        {
            get { return _isSelectedOVLData; }
            set { SetProperty(ref _isSelectedOVLData, value); }
        }
        private bool _isVisibleOVLData;
        public bool IsVisibleOVLData
        {
            get { return _isVisibleOVLData; }
            set { SetProperty(ref _isVisibleOVLData, value); }
        }
        public ObservableCollection<TargetOverlay> TISRawData { get; set; }
        private bool _isSelectedTISRawData;
        public bool IsSelectedTISRawData
        {
            get { return _isSelectedTISRawData; }
            set { SetProperty(ref _isSelectedTISRawData, value); }
        }
        private bool _isVisibleTISRawData;
        public bool IsVisibleTISRawData
        {
            get { return _isVisibleTISRawData; }
            set { SetProperty(ref _isVisibleTISRawData, value); }
        }
        public ObservableCollection<TargetResultBase> ErrorData { get; set; }
        private bool _isSelectedErrorData;
        public bool IsSelectedErrorData
        {
            get { return _isSelectedErrorData; }
            set { SetProperty(ref _isSelectedErrorData, value); }
        }
        private bool _isVisibleErrorData;
        public bool IsVisibleErrorData
        {
            get { return _isVisibleErrorData; }
            set { SetProperty(ref _isVisibleErrorData, value); }
        }
        public ObservableCollection<LotStatisticsData> LotStatistics_Misreg { get; set; }
        private bool _isSelectedLotStatistics_Misreg;
        public bool IsSelectedLotStatistics_Misreg
        {
            get { return _isSelectedLotStatistics_Misreg; }
            set { SetProperty(ref _isSelectedLotStatistics_Misreg, value); }
        }
        private bool _isVisibleLotStatistics_Misreg;
        public bool IsVisibleLotStatistics_Misreg
        {
            get { return _isVisibleLotStatistics_Misreg; }
            set { SetProperty(ref _isVisibleLotStatistics_Misreg, value); }
        }
        public ObservableCollection<LotStatisticsData> LotStatistics_TIS { get; set; }
        private bool _isSelectedLotStatistics_TIS;
        public bool IsSelectedLotStatistics_TIS
        {
            get { return _isSelectedLotStatistics_TIS; }
            set { SetProperty(ref _isSelectedLotStatistics_TIS, value); }
        }
        private bool _isVisibleLotStatistics_TIS;
        public bool IsVisibleLotStatistics_TIS
        {
            get { return _isVisibleLotStatistics_TIS; }
            set { SetProperty(ref _isVisibleLotStatistics_TIS, value); }
        }
        public ObservableCollection<WaferStatisticsData> WaferStatistics { get; set; }
        private bool _isSelectedWaferStatistics;
        public bool IsSelectedWaferStatistics
        {
            get { return _isSelectedWaferStatistics; }
            set { SetProperty(ref _isSelectedWaferStatistics, value); }
        }
        private bool _isVisibleWaferStatistics;
        public bool IsVisibleWaferStatistics
        {
            get { return _isVisibleWaferStatistics; }
            set { SetProperty(ref _isVisibleWaferStatistics, value); }
        }
        public ObservableCollection<TargetResultBase> BFCDResultsStatistics { get; set; }
        private bool _isSelectedBFCDResultsStatistics;
        public bool IsSelectedBFCDResultsStatistics
        {
            get { return _isSelectedBFCDResultsStatistics; }
            set { SetProperty(ref _isSelectedBFCDResultsStatistics, value); }
        }
        private bool _isVisibleBFCDResultsStatistics;
        public bool IsVisibleBFCDResultsStatistics
        {
            get { return _isVisibleBFCDResultsStatistics; }
            set { SetProperty(ref _isVisibleBFCDResultsStatistics, value); }
        }

        public Dictionary<RowColumnInfo, Dictionary<WcsPoint, OvQMerit>> JobQmeritData { get; set; }
        private bool _isSelectedJobQmeritData;
        public bool IsSelectedJobQmeritData
        {
            get { return _isSelectedJobQmeritData; }
            set { SetProperty(ref _isSelectedJobQmeritData, value); }
        }
        private bool _isVisibleJobQmeritData;
        public bool IsVisibleJobQmeritData
        {
            get { return _isVisibleJobQmeritData; }
            set { SetProperty(ref _isVisibleJobQmeritData, value); }
        }
        public RunningJobDataAndStatistics()
        {
            RawData = new ObservableCollection<TargetResultBase>();
            TISRawData = new ObservableCollection<TargetOverlay>();
            ErrorData = new ObservableCollection<TargetResultBase>();
            LotStatistics_Misreg = new ObservableCollection<LotStatisticsData>();
            LotStatistics_TIS = new ObservableCollection<LotStatisticsData>();
            WaferStatistics = new ObservableCollection<WaferStatisticsData>();
            BFCDResultsStatistics = new ObservableCollection<TargetResultBase>();
            JobQmeritData = new Dictionary<RowColumnInfo, Dictionary<WcsPoint, OvQMerit>>();
            OVLData = new ObservableCollection<TargetOVLResult> { };
        }
    }
}
