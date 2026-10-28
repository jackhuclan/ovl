

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.DomainLib.Services;
using InteVega.CBB.DomainService.Api.Interfaces;
using InteVega.CBB.DomainService.Api.Models;
using InteVega.CBB.Prism.UI.Extensions.Dialogs;
using InteVega.CBB.Prism.UI.Extensions.MessageBox;
using InteVega.CBB.SectionManager.Interface.Model;
using InteVega.CBB.SystemConfig.Interface;
using Marvel.DataModels.Enums;
using Marvel.DataModels.Job;
using Marvel.Interfaces.Application;
using Microsoft.Extensions.DependencyInjection;
using Prism.Services.Dialogs;
using System;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

using Ovl.UI.ViewModels.Bases;
namespace Ovl.UI.ViewModels.DataDisplay.ViewModel
{
    public partial class BasicDataViewModel : ObservableObject, IDisposable
    {
        [ObservableProperty]
        ObservableCollection<MenuNode> _menuNodeList;
        [ObservableProperty]
        ObservableCollection<JobAlarm> _jobAlarmList;
        [ObservableProperty]
        ObservableCollection<LotStatisticsData> _overlayJobHistoryData;
        [ObservableProperty]
        Visibility _historyVisible;
        [ObservableProperty]
        Visibility _logVisible;
        [ObservableProperty]
        Visibility _errorVisible;
        //[ObservableProperty]
        //Visibility _deviceVisible;

        IDialogService _dialogService;
        //IMainRunThread _mainRunThread;
        protected IMessageBoxService _messageBoxService;

        private IPipeCommunicatorService _pipeClient;
        private readonly SectionImageConfig _sectionImageConfig;
        private IMainRunThread mainRunThread;
        private IServiceProvider _serviceProvider;
        public BasicDataViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _dialogService = serviceProvider.GetService<IDialogService>();

            //! connect to the main work thread
            mainRunThread = serviceProvider.GetService<IMainRunThread>();
            mainRunThread.ActionStatisticsDataUpdated += UpdataJobHistoryData;

            _measureValueFactorConfig = new MeasureDataCustomConfig().PrepareConfig(_serviceProvider);
            MeasureValueUnitFactor = _measureValueFactorConfig.Factor;
            OverlayJobHistoryData = new ObservableCollection<LotStatisticsData>() { };

            SetCollapsed();
            LoadMenu();
            LoadJobAlarm();
            InitPipe();
            _pipeClient.StartAsync();
            _sectionImageConfig = serviceProvider.GetService<IConfigLoader>().LoadConfig<SectionImageConfig>();
            _messageBoxService = serviceProvider.GetService<IMessageBoxService>();

        }

        private void InitPipe()
        {
            if (_pipeClient == null)
            {
                _pipeClient = _serviceProvider.GetService<IPipeCommunicatorService>();

                // 订阅事件
                _pipeClient.ConnectionStatusChanged += OnConnectionStatusChanged;
                _pipeClient.MessageReceived += OnMessageReceived;
                _pipeClient.StatusUpdated += OnStatusUpdated;
            }
        }
        [RelayCommand]
        public void OpenGrid(string id)
        {
            SetCollapsed();
            for (int i = 0; i < _menuNodeList.Count; i++)
            {
                var item = _menuNodeList[i];
                item.IsSelect = item.Id == id ? true : false;
                if (item.IsSelect)
                {
                    switch (i)
                    {
                        case 0:
                            HistoryVisible = Visibility.Visible;
                            break;
                        case 1:
                            LogVisible = Visibility.Visible;
                            break;
                        case 2:
                            ErrorVisible = Visibility.Visible;
                            break;
                        //case "设备状态":
                        //    DeviceVisible = Visibility.Visible;
                        //    break;
                        default:
                            break;
                    }
                }

            }
        }
        [RelayCommand]
        public void AlarmMouseDouble(JobAlarm jobAlarm)
        {
            if (jobAlarm != null && jobAlarm.States == EnumJobAlarmStates.UnProcessed)
            {
                AlarmProcessingViewModel alarmProcessingViewModel = new AlarmProcessingViewModel();
                alarmProcessingViewModel.FormWidth = 450;
                alarmProcessingViewModel.FormHeight = 200;
                alarmProcessingViewModel.Message = jobAlarm.Message;
                alarmProcessingViewModel.AlarmTitle = "Prompt";
                _dialogService.ShowDialog("AlarmProcessing", alarmProcessingViewModel);
                //string message = "";
            }
        }
        private void OnConnectionStatusChanged(bool isConnected)
        {
        }

        private void OnMessageReceived(string message)
        {
        }

        private void OnStatusUpdated(string status)
        {
        }
        [RelayCommand]
        public void LotStatisticsDataMouseDoubleAsync(LotStatisticsData lotStatisticsData)
        {
            if (lotStatisticsData != null)
            {
                string message = JsonHelper.SerializerJson(lotStatisticsData);

                // 2. Base64编码（将JSON转为纯字符，避免特殊字符被处理）
                byte[] bytes = Encoding.UTF8.GetBytes(message);
                LogHelper.LogInfo($"Current click jobid json: {message}");
                message = Convert.ToBase64String(bytes);
                string appFolder = _sectionImageConfig.AnalyzerAppFolder;
                string appName = _sectionImageConfig.AnalyzerAppName;
                if (CheckAppIsOpen(appFolder, appName))
                {

                    if (!string.IsNullOrEmpty(message))
                    {
                        if (_pipeClient != null && _pipeClient.IsConnected)
                        {
                            bool success = false;
                            Task tskSendMessage = Task.Run(async () =>
                            {
                                success = await _pipeClient.SendMessageAsync(message);
                                if (success)
                                {
                                    LogHelper.LogInfo($"Current click jobid json encode: {message}");
                                }
                            });

                            Task.WaitAll(tskSendMessage);
                        }
                        else
                        {
                            LogHelper.LogInfo($" Start AndSendMessage: {message}");
                            InitPipe();
                            Task tskStartAndSendMessage = Task.Run(async () =>
                            {
                                await _pipeClient.StartAsync();
                                LogHelper.LogInfo($" Start connect");
                                LogHelper.LogInfo($" end connect");
                                LogHelper.LogInfo($"connect is:{_pipeClient.IsConnected}");
                                while (!_pipeClient.IsConnected)
                                {

                                }
                                if (_pipeClient.IsConnected)
                                {
                                    LogHelper.LogInfo($"Connect ok start send message: {message}");
                                    await _pipeClient.SendMessageAsync(message);
                                    LogHelper.LogInfo($"Connect ok send message end");
                                }
                            }); tskStartAndSendMessage.WaitAsync(TimeSpan.FromMilliseconds(60000));
                        }

                    }
                }
                else
                {
                    try
                    {
                        LogHelper.LogInfo($"LaunchApp");
                        ExternalAppLauncherService.LaunchFromFolder(appFolder, appName, message);
                        Thread.Sleep(2000);
                        Task task1 = Task.Run(async () => { _pipeClient.StartAsync(); });
                        Task.WaitAll(task1);

                    }
                    catch (Exception ex)
                    {

                        LogHelper.LogError($"LaunchApp: {ex.Message}");
                        _messageBoxService?.Error("LaunchApp failed!");


                    }
                }
            }
        }
        private Mutex singleInstanceMutex;
        // 标记当前线程是否持有互斥体所有权（避免重复释放或错误释放）
        private bool isMutexOwned;

        // 初始化互斥体（示例）
        public bool InitializeMutex(string mutexName)
        {
            // 尝试创建或打开互斥体，并获取所有权
            bool createdNew;
            singleInstanceMutex = new Mutex(true, mutexName, out createdNew);
            isMutexOwned = createdNew; // 只有新创建的互斥体才会被当前线程持有
            return createdNew;
        }

        // 安全释放互斥体
        private void ReleaseMutex()
        {
            if (singleInstanceMutex == null)
                return;

            try
            {
                // 仅当当前线程持有所有权时才释放
                if (isMutexOwned)
                {
                    singleInstanceMutex.ReleaseMutex();
                    isMutexOwned = false; // 释放后标记为未持有
                }
            }
            catch (ApplicationException ex)
            {
                // 捕获"未持有锁却释放"的异常
                Console.WriteLine($"释放互斥体失败：{ex.Message}");
            }
            finally
            {
                // 确保关闭互斥体（即使释放失败也关闭）
                singleInstanceMutex.Close();
                singleInstanceMutex = null;
            }
        }
        private bool CheckAppIsOpen(string appFolder, string appName)
        {
            lock (this)
            {
                try
                {
                    //string appPath = Path.Combine(appFolder, appName);
                    //bool isAnalyzerRunning = FolderAndProcessHelper.IsApplicationRunning(
                    //    appPath);
                    //LogHelper.LogInfo($"is running: {isAnalyzerRunning}");
                    //return isAnalyzerRunning;

                    const string marvelNutexName = "FD34E25A-1621-48BD-B8F1-BCCAE5307118"; //应用程序的名称或其他唯一标识符作为互斥体的名称
                    return !InitializeMutex(marvelNutexName);
                }
                catch (Exception ex)
                {
                    LogHelper.LogInfo($"opreation failed: {ex.Message}");
                    return false;
                }
                finally
                {
                    ReleaseMutex();
                }
            }
        }
        private async void Reconnect()
        {
            if (_pipeClient.IsConnected)
            {
                Task task = Task.Run(() =>
                {
                    _pipeClient.Stop();
                    _pipeClient.DisconnectAsync();
                });
                Task.WaitAll(task);
            }
            await _pipeClient.StartAsync();
        }
        public void LoadJobAlarm()
        {
            JobAlarmList = new ObservableCollection<JobAlarm>();
            //JobAlarmList.Add(new JobAlarm() {
            //    AlarmTime = DateTime.Now,
            //    Message = "System failure",// "系统出现故障",
            //    RestoreTime = DateTime.Now,
            //    States= EnumJobAlarmStates.Processed,
            //    Index = 0,
            //});
            //JobAlarmList.Add(new JobAlarm()
            //{
            //    AlarmTime = DateTime.Now,
            //    Message = "System failure",
            //    RestoreTime = DateTime.Now,
            //    States = EnumJobAlarmStates.UnProcessed,
            //    Index = 1,
            //});
        }
        public void LoadMenu()
        {
            MenuNodeList = new ObservableCollection<MenuNode>();
            MenuNodeList.Add(NewNode("History", true));
            MenuNodeList.Add(NewNode("Logs"));
            //MenuNodeList.Add(NewNode("Alarm"));
            //MenuNodeList.Add(NewNode("设备状态"));
            HistoryVisible = Visibility.Visible;
        }
        private MenuNode NewNode(string name, bool isSelect = false)
        {
            return new MenuNode
            {
                Id = Guid.NewGuid().ToString(),
                Name = name,
                Path = "",
                Visible = isSelect ? Visibility.Visible : Visibility.Collapsed,
                IsSelect = isSelect
            };
        }
        public void SetCollapsed()
        {
            HistoryVisible = Visibility.Collapsed;
            LogVisible = Visibility.Collapsed;
            ErrorVisible = Visibility.Collapsed;
            //DeviceVisible = Visibility.Collapsed;
        }

        int jobhistoryShowRowCount = 100;
        private MeasureDataCustomConfig _measureValueFactorConfig;

        public int MeasureValueUnitFactor { get; }

        private void UpdataJobHistoryData(JobItem jobItem, RealTimeStatisticsResult statisticsData)
        {
            //只有在job量测完成时才更新历史数据
            if (statisticsData == null || statisticsData.LotStatisticsData == null || statisticsData.IsJobCompleted == false) return;
            AppHelper.DispatcherInvoke(() =>
            {
                statisticsData.LotStatisticsData.JobID = jobItem.JobRunId;
                // statisticsData.LotStatisticsData.RecipeRunId = jobResult.RecipeRunId;
                OverlayJobHistoryData.Insert(0, statisticsData.LotStatisticsData);
                if (OverlayJobHistoryData.Count > jobhistoryShowRowCount)
                {
                    OverlayJobHistoryData.RemoveAt(jobhistoryShowRowCount);
                }
            });
        }

        public void Dispose()
        {
            if (mainRunThread != null)
                mainRunThread.ActionStatisticsDataUpdated -= UpdataJobHistoryData;
            if (_pipeClient != null)
            {
                _pipeClient.ConnectionStatusChanged -= OnConnectionStatusChanged;
                _pipeClient.MessageReceived -= OnMessageReceived;
                _pipeClient.StatusUpdated -= OnStatusUpdated;
                _pipeClient.Stop();
            }
        }
    }
}