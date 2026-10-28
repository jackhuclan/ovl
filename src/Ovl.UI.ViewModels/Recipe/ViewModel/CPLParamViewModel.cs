using CommunityToolkit.Mvvm.ComponentModel;
using InteVega.CBB.DomainService.Api.Interfaces;
using InteVega.CBB.DomainService.Api.Models;
using InteVega.CBB.Prism.UI.Extensions.MessageBox;
using Marvel.DataModels.Enums;
using Marvel.DataModels.Recipe.Overlay;
using Marvel.Interfaces.Application;
using Megarobo.Ovl.Service.IntfFn.fn;
using Megarobo.Ovl.Type;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using Wafer.Algorithm.Common.DoNet;

namespace Ovl.UI.ViewModels.Recipe.ViewModel
{
    public  partial class CPLParamViewModel : ObservableObject
    {
        protected static bool _exec_filter = false;
        static bool _exec_Bandwidth = false;
        protected static object _exec_lock = new object();
        protected bool _isFirstGet = true;
        public bool _FilterIsMove = false;
        protected ILightServiceForMeasure _lightServiceForMeasure;
        ILightServiceForMeasure600 _lightServiceForMeasure600;
        protected ILogger _logger;
        protected IMessageBoxService _messageBoxService;
        protected ICameraLightParamService _cameraLightParamService;
        protected IVisionService _visionService;
        public CPLParamViewModel(IServiceProvider prod, bool isEnableCPL,CPLParam param)
        {
            _isEnableCPL = isEnableCPL;
            _messageBoxService = prod.GetRequiredService<IMessageBoxService>();
            _logger = prod.GetService<ILogger<CPLParamViewModel>>();
            _lightServiceForMeasure600 = prod.GetRequiredService<ILightServiceForMeasure>() as ILightServiceForMeasure600;
            _cameraLightParamService = prod.GetService<ICameraLightParamService>();
            _visionService =prod.GetService<IVisionServiceManager>().GetMainVision();
            if (isEnableCPL)
            {
                Filters = _lightServiceForMeasure600.GetFilterNames();
                IsFilterEnable = Filters.Count > 1;
                Bandwidths = _lightServiceForMeasure600.GetBandwidthNames();
                WaveSteps = new List<int>() { 1, 5, 10, 20, 30 };
                SelectedStep = ILightServiceForMeasure600.WaveStep;
                if (param != null)
                {
                    ApplyParam(param, applyHardware: false);
                }
                _lightServiceForMeasure = prod.GetRequiredService<ILightServiceForMeasure>();
            }
        }

        [ObservableProperty]
        bool _isEnableCPL;

        [ObservableProperty]
        List<string> _filters = new List<string>();
        [ObservableProperty]
        bool _isFilterEnable;
        [ObservableProperty]
        int _curFilter;


        [ObservableProperty]
        List<string> _bandwidths = new List<string>();

        [ObservableProperty]
        int _curBandwidthIndex = 1;


        [ObservableProperty]
        double _curWave;
        [ObservableProperty]
        double _minWave = 410;
        [ObservableProperty]
        double _maxWave = 820;
        [ObservableProperty]
        List<int> _waveSteps;
        [ObservableProperty]
        int _selectedStep;
        [ObservableProperty]
        bool _isEnableWave;

        CPLParam _currentParam = null;

        /// <summary>
        /// 切换 Inner/Outer CPL 参数：下发硬件并同步 BW/Wave 显示。
        /// </summary>
        public void SetCPLParam(CPLParam param)
        {
            if (_currentParam == param) return;
            _currentParam = param;
            if (!IsEnableCPL || param == null) return;

            _cameraLightParamService?.SetCPLExposureTimeParams(IServiceCamera.CurCamera, param);
            ApplyParam(param, applyHardware: false);
            _visionService?.NotifyParamsChanged();
        }

        /// <summary>
        /// 从 CPLParam 同步 UI（Filter / BW / Wave / IsEnableWave），可选是否重复下发硬件。
        /// </summary>
        private void ApplyParam(CPLParam param, bool applyHardware)
        {
            if (!IsEnableCPL || param == null) return;

            if (applyHardware)
            {
                _cameraLightParamService?.SetCPLExposureTimeParams(IServiceCamera.CurCamera, param);
            }

            _isFirstGet = true;
            CurFilter = param.Filter;
            if (IsWaveTunerFilter(CurFilter))
            {
                CurBandwidthIndex = Math.Max(0, param.BandwidthIndex - 1);
                CurWave = param.Wave;
                IsEnableWave = true;
            }
            else
            {
                IsEnableWave = false;
            }
            _isFirstGet = false;
        }

        public CPLParam GetCPLParam()
        {
            if (!IsEnableCPL) return null;
            int NowCurBandwidthIndex = _lightServiceForMeasure600.GetBandwidthIndex();
            return new CPLParam() { Intensity = _lightServiceForMeasure.GetAttenIndex(), ExposureTime = (double)_visionService.Camera.GetExploreTime<float>(), Filter = CurFilter, BandwidthIndex = NowCurBandwidthIndex, Wave = CurWave };
        }

        private void GetBandWidthWave()
        {
            CurBandwidthIndex = _lightServiceForMeasure600.GetBandwidthIndex() - 1;
            GetWave();
        }

        private void GetWave()
        {
            var rang = _lightServiceForMeasure600.GetWaveRange();
            MaxWave = rang.Item2;
            MinWave = rang.Item1;
            CurWave = _lightServiceForMeasure600.GetWave();
        }

        partial void OnSelectedStepChanged(int value)
        {
            ILightServiceForMeasure600.WaveStep = value;
        }



        protected void ApplyFilter(int value)
        {
            InteVega.CBB.ComUI.Helpers.ProgressDialogHelper.ShowDialog("ApplyFilter please later...", () =>
            {
                ExecServiceAction(ref _exec_filter,
                                                    () =>
                                                    {
                                                        try
                                                        {
                                                            _FilterIsMove = true;
                                                            IsFilterEnable = false;
                                                            _lightServiceForMeasure600.SetFilter(value);
                                                            IsFilterEnable = true;
                                                            _FilterIsMove = false;
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                            IsFilterEnable = true;
                                                            _FilterIsMove = false;
                                                         
                                                        }


                                                    },
                                                    () => { _exec_filter = false; });
                return true;
            });

           
        }


        partial void OnCurFilterChanged(int oldValue, int newValue)
        {
            InteVega.CBB.ComUI.Helpers.ProgressDialogHelper.ShowDialog("CurFilter is Moving please later...", () =>
            {
                if (oldValue != newValue)
                {
                    ApplyFilter(newValue);
                }
                if (IsWaveTunerFilter(newValue))
                {
                    IsEnableWave = true;
                    GetBandWidthWave();
                }
                else
                {
                    IsEnableWave = false;
                }
                return true;
            });
        }


        partial void OnCurWaveChanged(double oldValue, double newValue)
        {
            if (oldValue != newValue)
            {
                ApplyWave(newValue);
            }
        }



        partial void OnCurBandwidthIndexChanged(int oldValue, int newValue)
        {
            int now = _lightServiceForMeasure600.GetBandwidthIndex();
            if (oldValue != newValue)
            {
                newValue += 1;
                ApplyBandWidth(newValue);
             
            }
        }

        private void ApplyBandWidth(int newValue)
        {
            InteVega.CBB.ComUI.Helpers.ProgressDialogHelper.ShowDialog("ApplyBandWidth please later...", () =>
            {

                ExecServiceAction(ref _exec_Bandwidth,
                                () => _lightServiceForMeasure600.SetBandwidthIndex(newValue),
                                () => _exec_Bandwidth = false);
                GetWave();
                return true;
            });
        }



        private void ApplyWave(double wave)
        {

            InteVega.CBB.ComUI.Helpers.ProgressDialogHelper.ShowDialog("ApplyWave please later...", () =>
            {
                ExecServiceAction(ref _exec_Bandwidth,
                               () =>
                               {
                                   _lightServiceForMeasure600.SetWave(wave);
                               },
                               () => { _exec_Bandwidth = false; });
                return true;
            });
        }


        protected void ExecServiceAction(ref bool bInExec, Action act, Action? postAct = null,
                                [CallerMemberName] string memberName = "",
                                [CallerLineNumber] int sourceLineNumber = 0)
        {
            if (_isFirstGet) return;
            //! check exec now
            lock (_exec_lock)
            {
                if (bInExec)
                {
                    //var fName = System.IO.Path.GetFileName(sourceFilePath);
                    _logger?.LogWarning($"action in exec now {memberName},{sourceLineNumber}");
                    return;
                }
                else
                {
                    try
                    {
                        bInExec = true;

                        act.Invoke();
                    }
                    finally
                    {
                        bInExec = false;
                        postAct?.Invoke();
                    }

                }
            }

        }

        static bool IsWaveTunerFilter(int filterIndex, IReadOnlyList<string> filters)
        {
            if (filters == null || filterIndex < 0 || filterIndex >= filters.Count)
            {
                return false;
            }

            var name = filters[filterIndex];
            if (name.Contains("Wave Tuner", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Tuner", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return filterIndex == filters.Count - 1
                && (name.Contains("Wave", StringComparison.OrdinalIgnoreCase)
                    || name.Equals("Full", StringComparison.OrdinalIgnoreCase));
        }

        bool IsWaveTunerFilter(int filterIndex) => IsWaveTunerFilter(filterIndex, Filters);

    }
}
