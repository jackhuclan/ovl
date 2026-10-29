using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.CommunicationApi.Exceptions;
using InteVega.CBB.ComUI.Helpers;
using InteVega.CBB.DomainService.Api.Interfaces;
using InteVega.CBB.DomainService.Api.Models;
using InteVega.CBB.EfemApi;
using InteVega.CBB.EfemApi.AlignParam;
using InteVega.CBB.EfemApi.Positions;
using InteVega.CBB.EfemApi.Status;
using InteVega.CBB.Hal;
using InteVega.CBB.MachineSystem.Interface;
using Marvel.Application.Services;
using Marvel.DataModels.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ovl.UI.WaferHandler.View;
using Prism.Services.Dialogs;

namespace Ovl.UI.WaferHandler.ViewModel;

public partial class WaferHandlerViewModel : WaferHandlerViewModelBase
{
    public virtual string JitImageSource =>
      "pack://application:,,,/Ovl.UI;Component/Resources/Images/img_jit@2x.png";
    [ObservableProperty]
    protected ObservableCollection<SlotSource> _slotItemList;
    [ObservableProperty]
    protected ObservableCollection<SlotSource> _slotItemList2;
    [ObservableProperty]
    protected SlotItem _alignerPortItem;
    [ObservableProperty]
    protected SlotItem _stagePortItem;
    [ObservableProperty]
    protected SlotItem _robotPortItem1;
    [ObservableProperty]
    protected SlotItem _robotPortItem2;

    [ObservableProperty]
    protected string _currentWaferSize1;
    [ObservableProperty]
    protected string _currentWaferSize2;
    protected WaferSize _waferSize1;
    protected WaferSize _waferSize2;

    [ObservableProperty]
    protected bool _portItem1IsEnabled;
    [ObservableProperty]
    protected bool _portItem2IsEnabled;
    [ObservableProperty]
    protected bool _buttonIsEnable;
    [ObservableProperty]
    protected double _waferOrientaion;
    protected readonly IEfemStorageManager _efemStorageManager;

    public bool CanClose = true;
    [ObservableProperty]
    protected bool port1Enb;
    [ObservableProperty]
    protected bool port2Enb;

    protected IEfemService _efemService;//目前采用IEfem 和IEfemService同时存在的方式，后续可以优化为只保留IEfemService
    protected readonly IVacuumService _vacuumService;
    protected readonly ILogger<WaferHandlerViewModel>? Logger;
    protected readonly ITransferService _transferService;
    protected WaferSize BigWaferSize = WaferSize.INCH_12;
    protected List<WaferPostionInfo> _waferPostionList;
    public WaferHandlerViewModel(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        WaferOrientaion = 0;// 默认180
        _efemStorageManager = serviceProvider.GetService<IEfemStorageManager>();
        WaferOrientaion = _efemStorageManager.ReadCacheDirection();
        _waferPostionList = _efemStorageManager.WaferPostionInfos;
        _transferService = serviceProvider.GetService<ITransferService>();
        Logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger<WaferHandlerViewModel>();
        IDeviceContext<IEfem> deviceContextEfem = serviceProvider.GetService<IDeviceContext<IEfem>>();

        _efemService = serviceProvider.GetService<IEfemService>();
        _vacuumService = serviceProvider.GetService<IVacuumService>();

        Load();

        if (_vacuumService != null && _vacuumService is VacuumService100)
        {
            BigWaferSize = WaferSize.INCH_8;
        }
    }

    protected virtual void Load()
    {

        AlignerPortItem = new SlotItem() { Value = "", Status = EnumSlotStatus.Empty };
        StagePortItem = new SlotItem() { Value = "", Status = EnumSlotStatus.Empty };
        RobotPortItem1 = new SlotItem() { Value = "", Status = EnumSlotStatus.Empty };
        RobotPortItem2 = new SlotItem() { Value = "", Status = EnumSlotStatus.Empty };
    }

    protected bool IsLoadPort(EnumSlotSource source)
    {
        return source == EnumSlotSource.Port1 || source == EnumSlotSource.Port2 || source == EnumSlotSource.Port3;
    }
    protected virtual WaferSize GetWaferSize(EnumSlotSource source)
    {
        switch (source)
        {
            case EnumSlotSource.Port1:
                return _waferSize1;
            case EnumSlotSource.Port2:
                return _waferSize2;
            case EnumSlotSource.Aligner:
                return GetWaferSizeByLoaction(EnumSlotSource.Aligner);
            case EnumSlotSource.Robot1:
                return GetWaferSizeByLoaction(EnumSlotSource.Robot1);
            case EnumSlotSource.Robot2:
                return GetWaferSizeByLoaction(EnumSlotSource.Robot2);
            default:
                return _waferManager.CurWaferSize;
        }
    }
    protected WaferSize GetLPWaferSize(SlotItem selectSlotItem)
    {
        return GetWaferSize(selectSlotItem.PortNo);
    }

    public void ViewLoaded()
    {
        LogHelper.LogInfo("WaferHandler View Load");
        ProgressDialogHelper.ShowDialog("Loading...", () =>
        {
            _ = WorkOnThread(() =>
            {
                try
                {
                    RefreshEfemStation().Wait();
                }
                catch (Exception ex)
                {
                    InteVega.CBB.CommonLib.Helpers.LogHelper.LogDebug(ex.ToString());
                }
                return string.Empty;
            }, "Load completed！"); //加载完成
            return true;
        });


    }

    /// <summary>
    /// Efem的所有工站刷新包括Stage料台
    /// </summary>
    public async Task RefreshEfemStation()
    {
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(System.Windows.Application.Current.Dispatcher));

        BindSoltData();
        var isExist = LoadStageState();
        RefreshArmAligner();
        if (isExist)
        {
            await JudgeWaferLoadeByEFEMAsync();
        }
    }

    /// <summary>
    /// 刷新手臂和Aligner
    /// </summary>
    protected void RefreshArmAligner()
    {
        if (_efemService != null)
        {
            //_efem.InitAll();
            //var sysState = _efem.GetSystemSigStatus();
            //bool result = WorkOnThread(() => { _efem.Connect(); return true; }, "");
            //if (result)
            //{
            LogHelper.LogRunMethodStart();
            LoadWaferSize();
            TrackState state = WorkOnThread(() => _efemService.GetTrackState(), "");
            if (state != null)
            {
                AlignerPortItem = LoadOtherPostion("Aligner", EnumSlotSource.Aligner, () => { return state.Aligner == EnumTrackState.WithWafer; });
                RobotPortItem1 = LoadOtherPostion("Robot1", EnumSlotSource.Robot1, () => { return state.UpArm == EnumTrackState.WithWafer; });
                RobotPortItem2 = LoadOtherPostion("Robot2", EnumSlotSource.Robot2, () => { return state.DownArm == EnumTrackState.WithWafer; });
            }
            LogHelper.LogRunMethodEnd($"UpArm={state?.UpArm},DownArm={state?.DownArm},Aligner={state?.Aligner}");
        }
    }

    /// <summary>
    /// 刷新料盒工站
    /// </summary>
    protected virtual void BindSoltData()
    {
        SlotItemList = new ObservableCollection<SlotSource>();
        SlotItemList2 = new ObservableCollection<SlotSource>();
        SetSlotData(EnumSlotSource.Port1,
            _efemStorageManager.SlotsCache.ContainsKey(1) ?
            _efemStorageManager.SlotsCache[1] : new List<EnumSlotState>(),
            slotItem => SlotItemList.Add(new SlotSource { SlotItem = slotItem }));
        SetSlotData(EnumSlotSource.Port2,
            _efemStorageManager.SlotsCache.ContainsKey(2) ?
            _efemStorageManager.SlotsCache[2] : new List<EnumSlotState>(),
            slotItem => SlotItemList2.Add(new SlotSource { SlotItem = slotItem }));
    }

    /// <summary>
    /// 刷新料盒工站方法
    /// </summary>
    /// <param name="portNum">Port口</param>
    /// <param name="sources">缓存中获取Wafer列表</param>
    /// <param name="slotItemAction">刷新每个料站方法</param>
    protected void SetSlotData(EnumSlotSource portNum, List<EnumSlotState> sources, Action<SlotItem> slotItemAction)
    {
        int count = 25;
        for (int i = count; i >= 1; i--)
        {
            EnumSlotState status = sources?.Count != 0 ? sources[i - 1] : EnumSlotState.NoWafer;
            EnumSlotStatus enumSlotStatus = GetShowEnumSlotStatus(status);
            //if (i==2)
            //{
            //    enumSlotStatus = EnumSlotStatus.Normal;
            //}
            SlotItem slotItem = new SlotItem
            {
                Id = Guid.NewGuid().ToString(),
                MinIndex = i,
                MaxIndex = i,
                Value = $"{portNum}-WS000{i}",
                SourcePortNo = portNum,
                PortNo = portNum,
                Status = enumSlotStatus,
                CallbackDrag = (t, d, target) => DragPortItem(t, d, target)
            };

            AppHelper.DispatcherInvoke(() =>
            {
                slotItemAction?.Invoke(slotItem);
            });
        }
    }


    protected virtual void SetWaferSize(int port, WaferSize warfersize)
    {
        if (port == 1)
        {
            CurrentWaferSize1 = warfersize.ToString();
            _waferSize1 = warfersize;
        }
        else if (port == 2)
        {
            CurrentWaferSize2 = warfersize.ToString();
            _waferSize2 = warfersize;
        }

        else
        {
            throw new Exception("The port num is not supported");
        }
    }
    protected void GetSlotDataStatus(EnumSlotSource portNum, ObservableCollection<SlotSource> slotItemAction, Action<bool> portIsEnabled)
    {
        try
        {
            LogHelper.LogRunMethodStart($"portNum={portNum}");
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(System.Windows.Application.Current.Dispatcher));


            _ = WorkOnThread(() =>
              {
                  var portIndex = (int)portNum;
                  var wafersize = _efemService.GetLoadPortSize((int)portNum);
                  SetWaferSize(portIndex, wafersize);
                  LockPort((int)portNum);
                  _efemService.OpenAndMap((int)portNum);
                  return true;

              });


            var (slotStateList, loadPortStatus) = WorkOnThread(() =>
            {
                LoadPortStatus loadPortStatus1 = _efemService.GetPortSigStatus((int)portNum);
                //    Port1Enb = false;
                //else
                //    Port1Enb = true;

                return (_efemService.Mapdt((int)portNum), loadPortStatus1);
            });

            if (slotStateList == null) return;
            AppHelper.DispatcherInvoke(() =>
            {
                BindSoltData();
                portIsEnabled?.Invoke(slotStateList.Any(a => GetShowEnumSlotStatus(a) == EnumSlotStatus.Normal) ? true : false);
                if (portNum == EnumSlotSource.Port1)
                {
                    if (loadPortStatus == null || !loadPortStatus.IsCarrierPresent || !loadPortStatus.IsCarrierInProperPlacement)
                        Port1Enb = false;
                    else
                        Port1Enb = true;
                }
                else
                {
                    if (loadPortStatus == null || !loadPortStatus.IsCarrierPresent || !loadPortStatus.IsCarrierInProperPlacement)
                        Port2Enb = false;
                    else
                        Port2Enb = true;
                }
            });
        }
        catch (Exception ex)
        {
            //给用户看的界面不需要显示调用堆栈
            _messageBoxService.Error(ex.ToString(), "WaferHandleMessage");
            Logger?.LogError(ex, $"Load Fail");
        }
        finally
        {
            LogHelper.LogRunMethodEnd();
        }
    }


    /// <summary>
    /// 锁loadport
    /// </summary>
    /// <param name="portNo">loadPort序号</param>
    public void LockPort(int portNo)
    {
        try
        {
            _efemService.LockPort(portNo);
        }
        catch (Exception ex)
        {
            LogHelper.LogError($"Lock port {portNo} Error(Not Support)! , Exception:{ex}");
        }
    }

    protected void LoadWaferSize(int portNum, Action<bool> SetPortEnb)
    {
        LogHelper.LogRunMethodStart($"portNum={portNum}");
        try
        {
            WaferSize waferSize = WorkOnThread(() => { return _efemService.GetLoadPortSize(portNum); }, "", false);
            SetWaferSize(portNum, waferSize);
            bool portEnb = false;
            LoadPortStatus loadPortStatus = WorkOnThread(() => { return _efemService.GetPortSigStatus(portNum); }, "", false);
            if (loadPortStatus == null || !loadPortStatus.IsCarrierPresent || !loadPortStatus.IsCarrierInProperPlacement)
                portEnb = false;
            else
                portEnb = true;
            SetPortEnb?.Invoke(portEnb);
            LogHelper.LogRunMethodEnd($"portNum={portNum},waferSize={waferSize}, PortEnb={portEnb}");
        }
        catch (Exception ex)
        {
            LogHelper.LogError(ex);
            _messageBoxService.Error(ex.Message);
        }

    }

    public virtual void LoadWaferSize()
    {
        LoadWaferSize(1, (b) => { Port1Enb = b; });
        LoadWaferSize(2, (b) => { Port2Enb = b; });
    }


    public virtual EnumSlotStatus GetShowEnumSlotStatus(EnumSlotState enumSlotState)
    {
        EnumSlotStatus enumSlotStatus = EnumSlotStatus.Empty;
        switch (enumSlotState)
        {
            case EnumSlotState.NoWafer:
                break;
            case EnumSlotState.Normal:
                enumSlotStatus = EnumSlotStatus.Normal;
                break;
            case EnumSlotState.AbNormal:
                enumSlotStatus = EnumSlotStatus.Tilted;
                break;
            case EnumSlotState.CrossSlotted:
                enumSlotStatus = EnumSlotStatus.Tilted;
                break;
            case EnumSlotState.MultipleWafers:
                enumSlotStatus = EnumSlotStatus.Overlap;
                break;
            case EnumSlotState.Undefined:
                break;
            case EnumSlotState.TooThick:
                enumSlotStatus = EnumSlotStatus.Tilted;
                break;
            case EnumSlotState.TooThin://果纳协议里面定义这个是叠片
                enumSlotStatus = EnumSlotStatus.Overlap;
                break;
            case EnumSlotState.Bow:
                enumSlotStatus = EnumSlotStatus.Tilted;
                break;
            default:
                break;
        }
        return enumSlotStatus;
    }

    /// <summary>
    /// 处理EFEM事件
    /// </summary>
    /// <typeparam name="T">动作返还结果</typeparam>
    /// <param name="func">Efem实际方法</param>
    /// <param name="successMsg">成功时显示信息</param>
    /// <returns></returns>
    protected T WorkOnThread<T>(Func<T> func, string successMsg = "", bool showErrorDialog = true)
    {
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(System.Windows.Application.Current.Dispatcher));
        try
        {
            CanClose = false;
            SynchronizationContext.Current.Post(pl =>
            {
                ButtonIsEnable = false;
            }, null);
            T result = func();
            SynchronizationContext.Current.Post(pl =>
            {
                ButtonIsEnable = true;
            }, null);
            if (!string.IsNullOrWhiteSpace(successMsg))
            {
                SynchronizationContext.Current.Post(pl =>
                {
                    _messageBoxService.Success(successMsg, "WaferHandleMessage");
                }, null);
            }
            return result;
        }
        catch (EfemHardwareException efemHardwareException)
        {
            string errDescription = string.Empty;
            SynchronizationContext.Current.Post(pl =>
            {
                errDescription = EfemHardwareException.GetErrorDescription(efemHardwareException.EfemError);
                Logger?.LogError($"Operation failed!\r\nError code: {efemHardwareException.EfemError},\r\nerror message: {efemHardwareException.Message}\r\nErrDescription:{errDescription}");
            }, null);
            if (showErrorDialog)
            {
                _messageBoxService.ErrorDialog($"Operation failed!\r\nError code: {efemHardwareException.EfemError},\r\nerror message: {efemHardwareException.Message}\r\nErrDescription: {errDescription}", "Operation failed!");
            }
        }
        catch (EfemValidationException validationException)
        {
            SynchronizationContext.Current.Post(pl =>
            {
                Logger?.LogWarning(validationException, $"EFEM validation failed: {validationException.Message}");
            }, null);

            if (showErrorDialog)
            {
                _messageBoxService.ErrorDialog(validationException.Message, "Operation validation failed");
            }
        }
        catch (Exception exception)
        {
            SynchronizationContext.Current.Post(pl =>
            {
                Logger?.LogError($"Abnormal operation! Exception message: {exception.Message}");
            }, null);
            _messageBoxService.ErrorDialog($"Abnormal operation! Exception message: {exception.Message}", "Operation exception！");
        }
        finally
        {
            CanClose = true;
            SynchronizationContext.Current.Post(pl =>
            {
                ButtonIsEnable = true;
            }, null);
        }
        return (T)default;
    }


    public virtual bool LoadStageState()
    {
        try
        {
            //_vacuumService.VacuumOnSmallInchWafer();
            bool result = _vacuumService.CheckVacuum();
            if (result)
            {
                StagePortItem = new SlotItem
                {
                    MinIndex = 1,
                    MaxIndex = 1,
                    Value = GetWaferPostionName(EnumSlotSource.Stage, "Stage"),
                    Status = EnumSlotStatus.Normal,
                    PortNo = EnumSlotSource.Stage,
                    SourcePortNo = EnumSlotSource.Stage,
                    CallbackDrag = (t, d, target) => DragPortItem(t, d, target)
                };
            }
            else
            {
                Logger?.LogError("Vacuum fail!");
                StagePortItem = new SlotItem() { Value = "", Status = EnumSlotStatus.Empty };
                _efemStorageManager.RemoveWaferPostionByPosition((int)EnumSlotSource.Stage);
            }
            return result;
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, $"Load Stage Fail");
            //给用户看的界面不需要显示调用堆栈
            _messageBoxService.Error(ex.ToString(), "WaferHandleMessage");
            return false;
        }
    }
    /// <summary>
    /// Aligner，Robot1，Robot2 状态加载
    /// </summary>
    /// <param name="showName"></param>
    /// <param name="enumSlotSource"></param>
    /// <param name="judge"></param>
    /// <returns></returns>
    private SlotItem LoadOtherPostion(string showName, EnumSlotSource enumSlotSource, Func<bool> judge)
    {
        var success = judge();

        SlotItem slotItem = new SlotItem() { Value = "", Status = EnumSlotStatus.Empty };
        if (success)
        {//判断有片显示，并且可以拖动
            slotItem = new SlotItem
            {
                MinIndex = 1,
                MaxIndex = 1,
                Value = GetWaferPostionName(enumSlotSource, showName),
                SourcePortNo = enumSlotSource,
                PortNo = enumSlotSource,
                Status = EnumSlotStatus.Normal,
                CallbackDrag = (t, d, target) => DragPortItem(t, d, target)
            };
        }
        else
        {//没有晶圆或者状态异常都显示空，并且不可以拖动

            DeleteWaferPostion(enumSlotSource);
        }
        return slotItem;
    }
    /// <summary>
    /// 初始化
    /// </summary>
    [RelayCommand]
    public void Init()
    {
        Task.Run(() =>
        {
            ProgressDialogHelper.ShowDialog("Device is initializing...", () =>
            {
                _ = WorkOnThread(() =>
                {
                    try
                    {
                        LogHelper.LogRunMethodStart("", "InitEfem");
                        _efemService.ClearError();
                        _efemService.InitAll();
                        _efemService.OrginSHAll();
                        RefreshEfemStation().Wait();
                    }
                    catch (Exception ex)
                    {
                        InteVega.CBB.CommonLib.Helpers.LogHelper.LogDebug(ex.ToString());
                    }
                    finally
                    {
                        LogHelper.LogRunMethodEnd("", "InitEfem");
                    }
                    return string.Empty;
                }, "Init completed！"); //初始化完成
                return true;
            });
        });
    }

    /// <summary>
    /// 清错
    /// </summary>
    [RelayCommand]
    public void ClearError()
    {

        Task.Run(() =>
        {
            LogHelper.LogRunMethodStart("", "ClearError");
            ProgressDialogHelper.ShowDialog("Reset/Clear Error ...", () =>
            {
                _ = WorkOnThread(() =>
                {
                    _efemService.ClearError();
                    //控制EFEM的四色灯
                    //_efem.SigOutTower(EnumTowerSignal.RED, EnumSignalStatus.ON);
                    RefreshEfemStation().Wait();
                    return string.Empty;
                }, "Reset/Clear Error completed！"); //初始化完成
                return true;
            });
            LogHelper.LogRunMethodEnd("", "ClearError");
        });
    }

    /// <summary>
    /// 急停
    /// </summary>
    [RelayCommand]
    public void EmergencyStop()
    {
        var res = _messageBoxService.AskDialog("Are you sure to emergency stop?", "Emergency stop");
        if (res != ButtonResult.OK) return;
        Task.Run(() =>
        {
            ProgressDialogHelper.ShowDialog("Emergency Stop...", () =>
            {
                _ = WorkOnThread(() =>
                {

                    try
                    {
                        _efemService.EMS();
                    }
                    catch (Exception)
                    {
                        Logger?.LogDebug($"Start efem EMS.... ");
                    }



                    return string.Empty;
                }, "Emergency Stop completed！"); //初始化完成
                return true;
            });
        });
    }
    /// <summary>
    /// 设置移动速度
    /// </summary>
    [RelayCommand]
    public void SetMovementSpeed()
    {

    }
    /// <summary>
    /// 设置Wafer规格
    /// </summary>
    [RelayCommand]
    public void SetWaferData()
    {
        _efemStorageManager.SaveCacheDirection(WaferOrientaion);
        return;//只设置到界面里，实际Align动作时才会使用到Efem里面
        //_ = Task.Run(() =>
        //{
        //    var param = new AlignAngleParam(WaferOrientaion);
        //    _ = WorkOnThread(() => { _efem.Align(1, param); return string.Empty; }, "Wafer size and angle settings completed！");//Wafer尺寸和角度设置完成
        //});
    }
    /// <summary>
    /// 更新port1晶圆状态
    /// </summary>
    [RelayCommand]
    public void Map1()
    {
        //ProgressDialogHelper.ShowToastDialog("ceshi", () => {
        //    Thread.Sleep(3000);
        //    return true;
        //});
        Task.Run(() =>
        {
            ProgressDialogHelper.ShowDialog("Map1 Loading...", () =>
            {
                Logger?.LogDebug($"Map1 Loading...");
                GetSlotDataStatus(EnumSlotSource.Port1, SlotItemList, isEnable => PortItem1IsEnabled = isEnable);
                //UpdateWaferPostion(true, EnumSlotSource.Port1, null, false);
                ClearLPWaferPostion(EnumSlotSource.Port1);
                return true;
            });
        });

    }
    /// <summary>
    /// 更新port2晶圆状态
    /// </summary>
    [RelayCommand]
    public void Map2()
    {
        Task.Run(() =>
        {
            ProgressDialogHelper.ShowDialog("Map2 Loading...", () =>
            {
                Logger?.LogDebug($"Map2 Loading...");
                GetSlotDataStatus(EnumSlotSource.Port2, SlotItemList2, isEnable => PortItem2IsEnabled = isEnable);
                // UpdateWaferPostion(true, EnumSlotSource.Port2, null, false);
                ClearLPWaferPostion(EnumSlotSource.Port2);
                return true;
            });
        });
    }
    /// <summary>
    /// 加载
    ///■ lock指：loadport门锁住
    ///■ dock指：将料盒向前推（8-12寸料盒会用到）
    ///■ open指：料盒门开启
    ///6，8寸没有lock，dock动作
    /// </summary>
    [RelayCommand]
    public void Load1()
    {
        Task.Run(() =>
        {
            ProgressDialogHelper.ShowDialog("Load1 Loading...", () =>
            {
                Logger?.LogDebug($"Load1 Loading...");
                GetSlotDataStatus(EnumSlotSource.Port1, SlotItemList, isEnable => PortItem1IsEnabled = isEnable);
                return true;
            });
        });


    }

    /// <summary>
    /// 加载
    ///■ lock指：loadport门锁住
    ///■ dock指：将料盒向前推（8-12寸料盒会用到）
    ///■ open指：料盒门开启
    /// </summary>
    [RelayCommand]
    public void Load2()
    {
        Task.Run(() =>
        {
            ProgressDialogHelper.ShowDialog("Load2 Loading...", () =>
            {
                Logger?.LogDebug($"Load2 Loading...");
                GetSlotDataStatus(EnumSlotSource.Port2, SlotItemList2, isEnable => PortItem2IsEnabled = isEnable);
                return true;
            });
        });


    }
    /// <summary>
    ///■ lock指：loadport门解锁
    ///■ dock指：料盒收回
    ///■ close指：料盒门关闭
    /// </summary>
    [RelayCommand]
    public void Unload1()
    {
        Task.Run(() =>
        {
            ProgressDialogHelper.ShowDialog("Unloading...", () =>
            {
                _ = WorkOnThread(() =>
                {
                    LogHelper.LogRunMethodStart("EnumSlotSource.Port1", "Unload1");
                    _efemService.Close((int)EnumSlotSource.Port1);
                    //此处需要优化
                    _efemService.UnlockPort((int)EnumSlotSource.Port1);
                    //if (_waferSize1 == BigWaferSize || _efem.Identity.Name.ToUpper() == "REJE") //锐洁的设备需要unlock
                    //{  //昀光现场设备
                    //    _efem.UnLock((int)EnumSlotSource.Port1);
                    //}
                    LogHelper.LogRunMethodEnd("EnumSlotSource.Port1", "Unload1");
                    //_efem.Dock((int)EnumSlotSource.Port1);
                    //_efem.UnLock((int)EnumSlotSource.Port1);
                    //_efem.OpenAndMap((int)EnumSlotSource.Port1);
                    return string.Empty;
                }, "Unload completed！"); //初始化完成
                return true;
            });
        });
    }
    /// <summary>
    ///■ lock指：loadport门解锁
    ///■ dock指：料盒收回
    ///■ close指：料盒门关闭
    /// </summary>
    [RelayCommand]
    public void Unload2()
    {
        Task.Run(() =>
        {
            ProgressDialogHelper.ShowDialog("Unloading...", () =>
            {
                _ = WorkOnThread(() =>
                {
                    LogHelper.LogRunMethodStart("EnumSlotSource.Port2", "Unload2");
                    _efemService.Close((int)EnumSlotSource.Port2);

                    _efemService.UnlockPort((int)EnumSlotSource.Port2);
                    //if (_waferSize2 == BigWaferSize || _efem.Identity.Name.ToUpper() == "REJE") //锐洁的设备需要unlock
                    //{
                    //    //  //昀光现场设备
                    //    _efem.UnLock((int)EnumSlotSource.Port2);
                    //}
                    LogHelper.LogRunMethodEnd("EnumSlotSource.Port2", "Unload2");
                    //_efem.Dock((int)EnumSlotSource.Port2);
                    //_efem.UnLock((int)EnumSlotSource.Port2);
                    //_efem.OpenAndMap((int)EnumSlotSource.Port2);
                    return string.Empty;
                }, "Unload completed！"); //初始化完成
                return true;
            });
        });


    }

    protected void ProcessSlotItem(SlotItem selectSlotItem, string type, SlotItem targetSlotItem)
    {
        var targetSlotItem1 = targetSlotItem;
        targetSlotItem = ResolveReturnTargetSlotItem(selectSlotItem, type, targetSlotItem1);
        LogHelper.LogRunMethodStart($"selectSlotItem={selectSlotItem?.Value},type={type},targetSlotItem={targetSlotItem?.Value}");
        // 1) 从LP取片前检查：来源是LP
        if (selectSlotItem != null && IsLoadPort(selectSlotItem.PortNo))
        {
            AssertLoadPortWafersNormal(selectSlotItem.PortNo);
        }

        // 2) 放回LP前检查：目标是LP
        if (targetSlotItem != null && IsLoadPort(targetSlotItem.PortNo))
        {
            AssertLoadPortWafersNormal(targetSlotItem.PortNo);
        }

        AssertTargetSlotEmptyBeforeMove(type, targetSlotItem);

        bool result = false;
        ComponentNOInfoBase slotSource = SetComponentInfoBase(selectSlotItem.PortNo, selectSlotItem.MinIndex);
        EnumArm arm = DetermineArm(selectSlotItem.PortNo);

        if (selectSlotItem.Status != EnumSlotStatus.Normal)
        {
            _messageBoxService.Error($"Wafer Status is {selectSlotItem.Status}, please check wafer status!");
            return;
        }

        var handlers = new Dictionary<string, Func<bool>>
        {
            { "StagePanel", () => HandleStagePanel(selectSlotItem, slotSource, arm) },
            { "AlignerPanel", () => HandleAlignerPanel(selectSlotItem, slotSource, arm) },
            { "RobotPanel", () => HandleRobotPanel(selectSlotItem, slotSource, EnumArm.ARM1) },
            { "RobotPanel2", () => HandleRobotPanel(selectSlotItem, slotSource, EnumArm.ARM2) },
        };

        if (handlers.ContainsKey(type))
        {
            result = handlers[type].Invoke();
        }
        else
        {
            result = HandleDefaultCase(selectSlotItem, targetSlotItem);
        }

        if (result)
        {
            EnumSlotSource enumSlotSource = EnumSlotSource.Stage;
            switch (type)
            {
                case "StagePanel":
                    enumSlotSource = EnumSlotSource.Stage;
                    break;
                case "AlignerPanel":
                    enumSlotSource = EnumSlotSource.Aligner;
                    break;
                case "RobotPanel":
                    enumSlotSource = EnumSlotSource.Robot1;
                    break;
                case "RobotPanel2":
                    enumSlotSource = EnumSlotSource.Robot2;
                    break;
                default:
                    break;
            }
            //WaferSize waferSize = GetLPWaferSize(selectSlotItem);
            //WaferPostionInfo waferPostionInfo = new WaferPostionInfo
            //{
            //    WaferName = selectSlotItem.Value,
            //    PositionNum = (int)enumSlotSource,
            //    SlotNum = (int)selectSlotItem.MinIndex,
            //    PortNum = (int)selectSlotItem.PortNo,
            //    WaferSize = waferSize

            //};
            //if (!string.IsNullOrWhiteSpace(type))
            //{
            //    //先删除原先的位置信息
            //    if (!IsLoadPort(selectSlotItem.PortNo))
            //    {
            //        // UpdateWaferPostion(false, selectSlotItem.PortNo, null, true);
            //        MoveWaferPostion(selectSlotItem.PortNo, enumSlotSource);
            //    }
            //    else
            //    {
            //        AddWaferPostion(waferPostionInfo);
            //    }
            //    //保存新位置信信息
            //    //UpdateWaferPostion(false, enumSlotSource, waferPostionInfo);
            //}
            //else
            //{//放入LP 资料信息转移
            //    DeleteWaferPostion(selectSlotItem.PortNo);
            //}//资料信息转移已经放到EFEM事件里了，所以不需要再区分放回LP还是其他工位了，直接删除原有位置信息就行了

            selectSlotItem.Value = "";
            selectSlotItem.Status = EnumSlotStatus.Empty;
        }
        else
        {
            RefreshEfemStation().Wait();
        }
        LogHelper.LogRunMethodEnd();
    }

    protected virtual void AssertTargetSlotEmptyBeforeMove(string type, SlotItem targetSlotItem)
    {
        var target = GetTargetSlotItem(type, targetSlotItem);
        if (target == null)
        {
            return;
        }

        if (IsSlotItemOccupied(target))
        {
            throw new EfemValidationException(
                $"Target position has wafer. Target={FormatSlotItem(target)}, Status={target.Status}, Wafer={target.Value}.");
        }

        if (_efemStorageManager == null)
        {
            return;
        }

        if (IsLoadPort(target.PortNo))
        {
            if (_efemStorageManager.HasWaferAtLoadPortSlot((int)target.PortNo, target.MinIndex))
            {
                throw new EfemValidationException(
                    $"Target load port slot has wafer. Target={FormatSlotItem(target)}.");
            }

            return;
        }

        var waferPostionInfo = _efemStorageManager.GetWaferPostionByPosition((int)target.PortNo);
        if (waferPostionInfo != null)
        {
            throw new EfemValidationException(
                $"Target position has wafer. Target={target.PortNo}, Wafer={waferPostionInfo.WaferName}.");
        }
    }

    private SlotItem GetTargetSlotItem(string type, SlotItem targetSlotItem)
    {
        return type switch
        {
            "StagePanel" => StagePortItem,
            "AlignerPanel" => AlignerPortItem,
            "RobotPanel" => RobotPortItem1,
            "RobotPanel2" => RobotPortItem2,
            _ => targetSlotItem
        };
    }

    private static bool IsSlotItemOccupied(SlotItem slotItem)
    {
        return slotItem.Status != EnumSlotStatus.Empty;
    }

    private static string FormatSlotItem(SlotItem slotItem)
    {
        return $"{slotItem.PortNo}-{slotItem.MinIndex}";
    }

    protected void RemoveStorage(ComponentNOInfoBase slotSource)
    {
        if (slotSource is LoadPortInfo loadPortInfo)
        {
            _efemStorageManager.RemoveSlotInfoMethod(loadPortInfo);
        }
    }
    //protected void RemoveStorage(ComponentNOInfoBase slotSource)
    //{
    //    if (slotSource is LoadPortInfo loadPortInfo)
    //    {
    //        _efemStorageManager.RemoveSlotInfoMethod(loadPortInfo);
    //    }
    //}

    //protected void UpdateStorage(ComponentNOInfoBase slotSource)
    //{
    //    if (slotSource is LoadPortInfo loadPortInfo)
    //    {
    //        _efemStorageManager.UpdateSlotInfoMethod(loadPortInfo);
    //    }
    //}

    protected void UpdateStagePresence(bool isWaferLoadCompleted, WaferSize? waferSize, Action action)
    {
        _waferManager.UpdateMoveEnable(isWaferLoadCompleted, waferSize, action);
    }

    /// <summary>
    /// 从其他工站把Wafer放到Stage料台
    /// </summary>
    /// <param name="selectSlotItem">拿料位的Block块前端对象</param>
    /// <param name="slotSource">拿料位Efem传递对象</param>
    /// <param name="arm">手臂</param>
    /// <returns>true为放料成功</returns>
    private bool HandleStagePanel(SlotItem selectSlotItem, ComponentNOInfoBase slotSource, EnumArm arm)
    {
        var slotTarget = SetComponentInfoBase(EnumSlotSource.Stage);
        bool result = WorkOnThread(() =>
        {
            _transferService.GotoPrepareLocation();
            WaferSize waferSize = GetLPWaferSize(selectSlotItem);//获取选择选择的wafersize
            if (slotSource == null)
            {//当slotSource为null时，说明不知道wafer来自arm工位

                EnumSlotSource post = EnumSlotSource.Robot1;
                if (arm == EnumArm.ARM2)
                {
                    post = EnumSlotSource.Robot2;
                }
                WaferPostionInfo waferPostionInfo = _efemStorageManager.GetWaferPostionByPosition((int)post);//?.FirstOrDefault(f => f.PositionNum == (int)selectSlotItem.PortNo) ?? null;
                if (waferPostionInfo != null)
                {
                    slotSource = new LoadPortInfo(waferPostionInfo.PortNum, waferPostionInfo.SlotNum);
                }

            }
            if (slotSource == null)
            {
                //当不知道 wafer 来自哪个port时，无法进行上料
                throw new Exception("The current wafer size is missing, and it cannot be loaded onto the stage!");
            }
            if (slotSource == null)
            {
                WaferPostionInfo waferPostionInfo = _waferPostionList?.FirstOrDefault(f => f.PositionNum == (int)selectSlotItem.PortNo);
                if (waferPostionInfo != null)
                    slotSource = new LoadPortInfo(waferPostionInfo.PortNum, waferPostionInfo.SlotNum);
            }
            UpdateStagePresence(true, waferSize, () =>
            {
                ProcessToStageMethod(slotSource, arm, selectSlotItem.PortNo);
            });

            if (!_vacuumService.IsVacuumOn(waferSize)) throw new Exception("None Vacuum On");
            return true;
        }, "Operation completed！");
        if (result) StagePortItem = SlotItemClone(selectSlotItem, EnumSlotSource.Stage);
        return result;
    }

    /// <summary>
    /// 执行从机台下料前的操作 检查晶圆尺寸，并确认关闭真空
    /// </summary>
    /// <param name="selectSlotItem"></param>
    protected virtual void BeforeUnloadStageWafer(SlotItem selectSlotItem, int portNO = 1)
    {
        //var waferSize = _waferManager.BigChuck.ToWaferSize();
        //if (selectSlotItem != null)
        //{
        //    waferSize = GetWaferSize(EnumSlotSource.Stage);
        //}
        var waferSize = GetWaferSize(EnumSlotSource.Stage);
        _waferManager.AssertWaferSize(waferSize);
        //关闭内外真空
        _vacuumService.VacuumOffWithCheck(waferSize);
    }

    protected virtual void LoadWaferToStage(EnumArm arm, EnumSlotSource portNo = EnumSlotSource.None)
    {
        _efemService.Unload(new LoadLockInfo(1), arm);
    }

    /// <summary>
    /// 从其他工站把Wafer放到Aligner料台
    /// </summary>
    /// <param name="selectSlotItem">拿料位的Block块前端对象</param>
    /// <param name="slotSource">拿料位Efem传递对象</param>
    /// <param name="arm">手臂</param>
    /// <returns>true为放料成功</returns>
    protected virtual bool HandleAlignerPanel(SlotItem selectSlotItem, ComponentNOInfoBase slotSource, EnumArm arm)
    {
        var slotAlignerTarget = SetComponentInfoBase(EnumSlotSource.Aligner);
        bool result = WorkOnThread(() =>
        {
            if (selectSlotItem.PortNo == EnumSlotSource.Robot1 || selectSlotItem.PortNo == EnumSlotSource.Robot2)
            {
                _efemService.Unload(slotAlignerTarget, arm);
                var aligner = new AlignerInfo(1);
                _efemService.Align(aligner.NO, new AlignAngleParam(WaferOrientaion));
            }
            else if (selectSlotItem.PortNo == EnumSlotSource.Stage)
            {
                _transferService.GotoPrepareLocation();
                UpdateStagePresence(false, null, () =>
                {
                    BeforeUnloadStageWafer(null, slotSource.NO);
                    _efemService.SetLoadLockSize();
                    _efemService.Load(slotSource, arm);
                    _efemService.Unload(slotAlignerTarget, arm);
                    //_efemService.Trans(slotSource, arm, arm, slotAlignerTarget);
                    var aligner = new AlignerInfo(1);
                    _efemService.Align(aligner.NO, new AlignAngleParam(WaferOrientaion));
                    //判断Stage是否有晶圆，如果存在则开启真空
                    _vacuumService.CheckVacuum();
                });
            }
            else if (IsLoadPort(selectSlotItem.PortNo))
            {
                var aligner = new AlignerInfo(1);
                var slotAlignerTarget = SetComponentInfoBase(EnumSlotSource.Aligner);
                _efemService.Trans(slotSource, arm, arm, slotAlignerTarget);
                RemoveStorage(slotSource);
                _efemService.Align(aligner.NO, new AlignAngleParam(WaferOrientaion));
            }
            return true;
        }, "Operation completed！");
        if (result) AlignerPortItem = SlotItemClone(selectSlotItem, EnumSlotSource.Aligner);
        return result;
    }


    /// <summary>
    /// 从其他工站把Wafer放到手臂1
    /// </summary>
    /// <param name="selectSlotItem">拿料位的Block块前端对象</param>
    /// <param name="slotSource">拿料位Efem传递对象</param>
    /// <param name="arm">手臂1</param>
    /// <returns>true为放料成功</returns>
    protected virtual bool HandleRobotPanel(SlotItem selectSlotItem, ComponentNOInfoBase slotSource, EnumArm arm)
    {
        // LogHelper.LogInfo($"HandleRobotPanel,selectSlotItem={selectSlotItem.PortNo},slotSource={slotSource.NO},arm={arm}");
        CheckArmUser(arm);
        bool result = WorkOnThread(() =>
        {
            UpdateStagePresence(false, null, () =>
            {
                if (selectSlotItem.PortNo == EnumSlotSource.Stage)
                {
                    _transferService.GotoPrepareLocation();
                    BeforeUnloadStageWafer(null, slotSource.NO);
                    selectSlotItem.Status = EnumSlotStatus.Normal;
                    _efemService.SetLoadLockSize();
                }
                WaferSize waferSize = WaferSize.None;
                if (IsLoadPort(selectSlotItem.PortNo))
                {
                    waferSize = GetLPWaferSize(selectSlotItem);
                }
                _efemService.Load(slotSource, arm);
                RemoveStorage(slotSource);
            });
            return true;
        }, "Operation completed！");
        if (result)
        {
            if (arm == EnumArm.ARM1) RobotPortItem1 = SlotItemClone(selectSlotItem, EnumSlotSource.Robot1);
            else RobotPortItem2 = SlotItemClone(selectSlotItem, EnumSlotSource.Robot2);
        }
        return result;
    }

    /// <summary>
    /// 拖拽到Port
    /// </summary>
    /// <param name="selectSlotItem">拿料位的Block块前端对象</param>
    /// <param name="targetSlotItem">放料位的Block块前端对象，Port1或Port2</param>
    /// <returns></returns>
    protected virtual bool HandleDefaultCase(SlotItem selectSlotItem, SlotItem targetSlotItem)
    {
        bool result = PortTransHandler(SlotItemList, selectSlotItem, targetSlotItem);
        result = result ? result : PortTransHandler(SlotItemList2, selectSlotItem, targetSlotItem);
        if (result)
        {
            selectSlotItem.Value = "";
            selectSlotItem.Status = EnumSlotStatus.Empty;
        }
        return result;
    }

    /// <summary>
    /// 放料到Stage,Efem指令方法
    /// </summary>
    /// <param name="slotSource">拿料位Efem传递对象</param>
    /// <param name="arm">手臂</param>
    /// <param name="portNo">拿料位Efem传递对象的port口属性</param>
    protected virtual void ProcessToStageMethod(ComponentNOInfoBase slotSource, EnumArm arm, EnumSlotSource portNo)
    {
        if (IsLoadPort(portNo))
        {
            var aligner = new AlignerInfo(1);
            var slotAlignerTarget = SetComponentInfoBase(EnumSlotSource.Aligner);
            _transferService.ActivateVacuumByLP(slotSource.NO);
            _efemService.Trans(slotSource, arm, arm, slotAlignerTarget);
            RemoveStorage(slotSource);
            _efemService.Align(aligner.NO, new AlignAngleParam(WaferOrientaion));
            _efemService.Load(aligner, arm);
            LoadWaferToStage(arm, ToEnumSlotSource(slotSource));
            //_transferService.VerifyVacuumStatusOn(slotSource.NO);
        }
        else if (portNo == EnumSlotSource.Robot1 || portNo == EnumSlotSource.Robot2)
        {
            var aligner = new AlignerInfo(1);
            var slotAlignerTarget = SetComponentInfoBase(EnumSlotSource.Aligner);
            //根据 pa 上 获取wafer size 大小，并吸附真空
            _transferService.ActivateVacuumByArm((int)arm);
            _efemService.Unload(slotAlignerTarget, arm);
            _efemService.Align(aligner.NO, new AlignAngleParam(WaferOrientaion));
            _efemService.Load(aligner, arm);
            LoadWaferToStage(arm, ToEnumSlotSource(slotSource));
        }
        else if (portNo == EnumSlotSource.Aligner)
        {
            var aligner = new AlignerInfo(1);
            _efemStorageManager.GetWaferPostionByPosition((int)EnumSlotSource.Aligner);
            _transferService.ActivateVacuumByPA();
            _efemService.Load(aligner, arm);
            LoadWaferToStage(arm, ToEnumSlotSource(slotSource));
        }
        if (!_vacuumService.CheckVacuum())
        {
            throw new Exception("None Vacuum On");
        }
    }

    protected EnumSlotSource ToEnumSlotSource(ComponentNOInfoBase slot)
    {
        switch (slot.NO)
        {
            case 1: return EnumSlotSource.Port1;
            case 2: return EnumSlotSource.Port2;
            case 3: return EnumSlotSource.Port3;
            default:
                throw new Exception($"{slot.NO} is not suport change to EnumSlotSource Port");
        }
    }

    public virtual void DragPortItem(string type, SlotItem selectSlotItem, SlotItem targetSlotItem)
    {

        _ = Task.Run(() =>
        {
            ProgressDialogHelper.ShowDialog("Executing operation...", () =>
            {
                try
                {
                    ProcessSlotItem(selectSlotItem, type, targetSlotItem);
                }
                catch (EfemHardwareException efemexception)
                {
                    string errDescription = EfemHardwareException.GetErrorDescription(efemexception.EfemError);
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        _messageBoxService.ErrorDialog($"Operation failed!\r\nError code: {efemexception.EfemError},\r\nerror message: {efemexception.Message}\r\nErrDescription: {errDescription}", "Operation failed!");
                    });
                }
                catch (EfemValidationException exemfemValidationException)
                {

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        _messageBoxService.ErrorDialog(exemfemValidationException.Message, "", "Efem validation failed");
                    });
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        _messageBoxService.ErrorDialog(ex.Message, "", ex.StackTrace);
                    });
                }
                finally
                {
                    //判断Stage是否有晶圆，如果存在则开启真空
                    _vacuumService.CheckVacuum();
                }
                return true;
            });
        });
    }

    protected virtual EnumArm DetermineArm(EnumSlotSource portNo = EnumSlotSource.None)
    {

        if (_efemService.GetArmUser() != 3)
        {
            return _efemService.GetArmUser() == 1 ? EnumArm.ARM1 : EnumArm.ARM2;
        }

        EnumArm arm = default;
        var state = _efemService.GetTrackState();

        switch (portNo)
        {
            case EnumSlotSource.Port1:
            case EnumSlotSource.Port2:
            case EnumSlotSource.Port3:
            case EnumSlotSource.Robot1:
                arm = EnumArm.ARM1;
                break;


            case EnumSlotSource.Robot2:
                arm = EnumArm.ARM2;
                break;
            case EnumSlotSource.None:
            default:
                if (state.UpArm == EnumTrackState.NoWafer)
                {
                    arm = EnumArm.ARM1;
                }
                else if (state.DownArm == EnumTrackState.NoWafer)
                {
                    arm = EnumArm.ARM2;
                }
                else
                {
                    //throw new Exception("Not Exist free arm!");
                }
                break;
        }

        CheckArmUser(arm);
        return arm;
    }
    /// <summary>
    /// 晶圆到料盒
    /// </summary>
    /// <param name="slotItems"></param>
    /// <param name="selectSlotItem"></param>
    /// <param name="targetSlotItem"></param>
    /// <returns></returns>
    public virtual bool PortTransHandler(ObservableCollection<SlotSource> slotItems, SlotItem selectSlotItem, SlotItem targetSlotItem)
    {
        LogHelper.LogRunMethodStart($"selectSlotItem={selectSlotItem?.Value},targetSlotItem={targetSlotItem?.Value},slotItems.Count={slotItems?.Count}");
        bool result = false;
        SlotItem slotItem = slotItems.FirstOrDefault(f => f.SlotItem.Id == targetSlotItem.Id)?.SlotItem;
        if (slotItem != null)
        {
            //result = true;
            ComponentNOInfoBase slotSource = SetComponentInfoBase(selectSlotItem.PortNo, selectSlotItem.MinIndex);
            ComponentNOInfoBase slotTarget = SetComponentInfoBase(slotItem.PortNo, slotItem.MinIndex);


            if (selectSlotItem.PortNo == EnumSlotSource.Robot1 || selectSlotItem.PortNo == EnumSlotSource.Robot2)
            {
                EnumArm arm = selectSlotItem.PortNo == EnumSlotSource.Robot1 ? EnumArm.ARM1 : EnumArm.ARM2;
                CheckArmUser(arm);
                result = WorkOnThread(() =>
               {
                   var lpwaferSize = GetLPWaferSize(targetSlotItem);
                   var armwafersize = GetWaferSizeByLoaction(selectSlotItem.PortNo);
                   if (lpwaferSize != armwafersize)
                   {
                       throw new Exception($"WaferSize mismatch: cannot return wafer from {selectSlotItem.PortNo} to {targetSlotItem.PortNo}. " +
                                            $"LP WaferSize={lpwaferSize}, Arm WaferSize={armwafersize}");
                   }
                   _efemService.Unload(slotTarget, arm);//从arm放到lp
                                                        //UpdateStorage(slotTarget);
                   return true;
               },
                   "Operation completed！");
            }

            else if (selectSlotItem.PortNo == EnumSlotSource.Stage)
            {
                result = WorkOnThread(() =>
                {
                    UpdateStagePresence(false, null, () =>
                    {
                        _transferService.GotoPrepareLocation();
                        BeforeUnloadStageWafer(targetSlotItem, slotTarget.NO);
                        var arm = DetermineArm(targetSlotItem.PortNo);
                        _efemService.SetLoadLockSize();
                        _efemService.Load(slotSource, arm);
                        _efemService.Unload(slotTarget, arm);
                        //UpdateStorage(slotTarget);
                        //判断Stage是否有晶圆，如果存在则开启真空
                        _vacuumService.CheckVacuum();
                    });
                    return true;
                });
            }
            else
            {
                result = WorkOnThread(() =>
                {
                    var selectwaferSize = GetLPWaferSize(selectSlotItem);
                    var targetwaferSize = GetLPWaferSize(targetSlotItem);
                    if (selectwaferSize != targetwaferSize)
                    {
                        throw new Exception($"WaferSize mismatch: cannot return wafer from {selectSlotItem.PortNo} WaferSize={selectwaferSize} to " +
                                            $"LP {targetSlotItem.PortNo}  WaferSize={targetwaferSize}");
                    }
                    var arm = DetermineArm();
                    _efemService.Trans(slotSource, arm, arm, slotTarget);
                    //UpdateStorage(slotTarget);
                    RemoveStorage(slotSource);
                    return true;
                }, "Operation completed！");
            }


            if (result)
            {
                UpdateWaferPostion(false, selectSlotItem.PortNo, null, true);
                foreach (var item in slotItems)
                {
                    if (item.SlotItem.Id == targetSlotItem.Id)
                    {
                        item.SlotItem = new SlotItem
                        {
                            Id = item.SlotItem.Id,
                            Value = selectSlotItem.Value,
                            Status = selectSlotItem.Status,
                            SourcePortNo = selectSlotItem.SourcePortNo,
                            PortNo = slotItem.PortNo,
                            MaxIndex = item.SlotItem.MaxIndex,
                            MinIndex = item.SlotItem.MinIndex,
                            CallbackDrag = (t, d, target) => DragPortItem(t, d, target)
                        };
                    }
                }
            }
        }
        LogHelper.LogRunMethodEnd();
        return result;
    }
    public SlotItem SlotItemClone(SlotItem slotItem, EnumSlotSource enumSlotSource)
    {
        return new SlotItem
        {
            MinIndex = 1,
            MaxIndex = 1,
            Value = slotItem.Value,
            Status = slotItem.Status,
            SourcePortNo = slotItem.SourcePortNo,
            PortNo = enumSlotSource,
            CallbackDrag = (t, d, target) => DragPortItem(t, d, target)
        };
    }
    public ComponentNOInfoBase SetComponentInfoBase(EnumSlotSource slotSource, int slotNum = 0)
    {
        switch (slotSource)
        {
            case EnumSlotSource.Port1:
            case EnumSlotSource.Port2:
            case EnumSlotSource.Port3:
                return new LoadPortInfo((int)slotSource, slotNum);
            case EnumSlotSource.Aligner:
                return new AlignerInfo(1);
            case EnumSlotSource.Stage:
                return new LoadLockInfo(1);
            case EnumSlotSource.Robot1:
                return null;
            case EnumSlotSource.Robot2:
                return null;
            default:
                return null;
        }
    }
    protected void CheckArmUser(EnumArm arm)
    {
        var ok = _efemService.CheckArmUser(arm);
        if (!ok)
        {
            // _cfg.UserArm: 1=ARM1, 2=ARM2, 3=Auto（EfemService 内部配置）
            throw new InvalidOperationException($"Current configuration does not allow using {arm}.");
        }
    }




    /// <summary>
    /// 拖拽到stage，alinger时需要新增
    /// mapping 的时候需要清理
    /// </summary>
    /// <param name="isMapping">是否mapping</param>
    /// <param name="enumSlotSource">处理数据类型</param>
    /// <param name="isClear">清理数据</param>
    public void UpdateWaferPostion(bool isMapping, EnumSlotSource enumSlotSource, WaferPostionInfo waferPostionInfo = null, bool isClear = false)
    {
        List<WaferPostionInfo> waferPostionInfos = _efemStorageManager.WaferPostionInfos;
        if (isMapping)
        {
            waferPostionInfos = waferPostionInfos.Where(w => !w.WaferName.StartsWith(enumSlotSource.ToString())).ToList();
        }
        else if (isClear)
        {
            waferPostionInfos = waferPostionInfos.Where(w => w.PositionNum != (int)enumSlotSource).ToList();
        }
        else
        {
            waferPostionInfos = waferPostionInfos?.Where(w => w.PositionNum != (int)enumSlotSource).ToList() ?? new List<WaferPostionInfo>();
            if (waferPostionInfo != null)
            {
                waferPostionInfos.Add(waferPostionInfo);
            }
        }
        //_efemStorageManager.SaveWaferPosition(waferPostionInfos);
        _waferPostionList = waferPostionInfos;

    }
    /// <summary>
    /// 清理LP 全部的map数据
    /// </summary>
    /// <param name="enumSlotSource"></param>
    public void ClearLPWaferPostion(EnumSlotSource enumSlotSource)
    {
        if (!IsLoadPort(enumSlotSource))
            throw new ArgumentOutOfRangeException(nameof(enumSlotSource), enumSlotSource, "ClearLPWaferPostion only supports: Port1/Port2/Port3.");

        if (_efemStorageManager == null)
            throw new InvalidOperationException("IEfemStorageManager is not available.");

        //此处不需要实现，map的时候清除LP里面的数据，不在WaferPostionInfo信息里。
        //List<WaferPostionInfo> waferPostionInfos = _efemStorageManager.WaferPostionInfos;
        //waferPostionInfos = waferPostionInfos.Where(w => !w.WaferName.StartsWith(enumSlotSource.ToString())).ToList();
        //_efemStorageManager.SaveWaferPosition(waferPostionInfos);
        //_waferPostionList = waferPostionInfos;

    }
    public void DeleteWaferPostion(EnumSlotSource enumSlotSource)
    {

        if (_efemStorageManager == null)
            throw new InvalidOperationException("IEfemStorageManager is not available.");
        _efemStorageManager.RemoveWaferPostionByPosition((int)enumSlotSource);

    }
    /// <summary>
    /// 获取存储中的数据
    /// </summary>
    /// <param name="defaultName"></param>
    /// <returns></returns>
    public string GetWaferPostionName(EnumSlotSource enumSlotSource, string defaultName)
    {
        //_waferPostionList = _efemStorageManager.ReadWaferPostionList() ?? new List<WaferPostionInfo>();
        //WaferPostionInfo waferPostion = _waferPostionList?.Where(w => w.PositionNum == (int)enumSlotSource)?.FirstOrDefault() ?? null;
        //return waferPostion?.WaferName ?? defaultName;
        var waferPostion = _efemStorageManager.GetWaferPostionByPosition((int)enumSlotSource);

        if (waferPostion == null)
        {
            _efemStorageManager.AddWaferPostion(new WaferPostionInfo
            {
                WaferSize = WaferSize.None,
                PortNum = 0,
                SlotNum = 0,
                PositionNum = (int)enumSlotSource,
                WaferName = defaultName
            });
        }
        return waferPostion?.WaferName ?? defaultName;

    }
    /// <summary>
    ///
    /// </summary>
    /// <param name="enumSlotSource"></param>
    /// <returns></returns>
    public WaferSize GetWaferSizeByLoaction(EnumSlotSource enumSlotSource)
    {
        // 仅允许查询：Aligner / Robot1(Arm1) / Robot2(Arm2) / Stage
        if (enumSlotSource != EnumSlotSource.Aligner
            && enumSlotSource != EnumSlotSource.Robot1
            && enumSlotSource != EnumSlotSource.Robot2
            && enumSlotSource != EnumSlotSource.Stage)
        {
            throw new ArgumentOutOfRangeException(
                nameof(enumSlotSource),
                enumSlotSource,
                "GetWaferSizeByLoaction only supports: Aligner, Robot1(Arm1), Robot2(Arm2), Stage.");
        }
        if (_efemStorageManager == null)
            throw new InvalidOperationException("IEfemStorageManager is not available.");


        var waferPostion = _efemStorageManager.GetWaferPostionByPosition((int)enumSlotSource);

        if (waferPostion == null)
            throw new InvalidOperationException($"Wafer position cache is missing for {enumSlotSource}. Please refresh EFEM station or complete related transfer first.");

        if (waferPostion.WaferSize == default || waferPostion.WaferSize == WaferSize.None)
            throw new InvalidOperationException($"WaferSize is unknown on {enumSlotSource}. WaferName={waferPostion.WaferName ?? "N/A"}.");

        return waferPostion.WaferSize;
    }
    private static bool IsAbnormalSlotState(EnumSlotState state)
    {
        // 仅 NoWafer/Normal 视为正常，其余一律异常（包含 Undefined）
        return state != EnumSlotState.NoWafer
            && state != EnumSlotState.Normal;
    }
    private void AssertLoadPortWafersNormal(EnumSlotSource portNo)
    {
        if (!IsLoadPort(portNo)) return;

        var portIndex = (int)portNo;

        if (_efemStorageManager?.SlotsCache == null ||
            !_efemStorageManager.SlotsCache.TryGetValue(portIndex, out var slots) ||
            slots == null)
        {

            throw new Exception($"LoadPort {portNo} slot cache is missing. Please execute mapping (Map1/Map2) before LP operation.");
        }

        var abnormal = slots
            .Select((state, idx) => new { state, slotNo = idx + 1 })
            .Where(x => IsAbnormalSlotState(x.state))
            .ToList();

        if (abnormal.Count == 0) return;

        var msg = new StringBuilder()
            .AppendLine($"LoadPort {portNo} 的 25 层中存在不正常 wafer，禁止执行取放片动作。")
            .ToString();

        throw new Exception(msg);
    }

    /// <summary>
    /// 自动返回LP时候，优先使用targetSlotItem，如果没有，则根据selectSlotItem的PortNo和MinIndex去缓存里找对应的位置信息，找到后验证状态是否为Empty，如果不满足条件则抛异常
    /// </summary>
    /// <param name="selectSlotItem"></param>
    /// <param name="type"></param>
    /// <param name="targetSlotItem"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    protected virtual SlotItem ResolveReturnTargetSlotItem(SlotItem selectSlotItem, string type, SlotItem targetSlotItem)
    {
        if (targetSlotItem != null)
        {
            return targetSlotItem;
        }

        if (selectSlotItem == null)
        {
            throw new ArgumentNullException(nameof(selectSlotItem));
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            return null;
        }

        if (_efemStorageManager == null)
        {
            throw new InvalidOperationException("IEfemStorageManager is not available.");
        }

        var waferPostionInfo = _efemStorageManager.GetWaferPostionByPosition((int)selectSlotItem.PortNo);
        if (waferPostionInfo == null)
        {
            throw new EfemValidationException(
                $"Can not find wafer source position info for current position {selectSlotItem.PortNo}.");
        }

        var sourcePort = (EnumSlotSource)waferPostionInfo.PortNum;
        var sourceSlot = waferPostionInfo.SlotNum;

        ObservableCollection<SlotSource> slotSources = sourcePort switch
        {
            EnumSlotSource.Port1 => SlotItemList,
            EnumSlotSource.Port2 => SlotItemList2,
            EnumSlotSource.Port3 => this is WaferHandler600ViewModel vm600 ? vm600.SlotItemList3 : null,
            _ => null
        };

        if (slotSources == null)
        {
            throw new InvalidOperationException(
                $"Can not find slot collection for source port {sourcePort}.");
        }

        var result = slotSources
            .Select(x => x?.SlotItem)
            .FirstOrDefault(x => x != null && x.PortNo == sourcePort && x.MinIndex == sourceSlot);

        if (result == null)
        {
            throw new EfemValidationException(
                $"Can not find target slot item. SourcePort={sourcePort}, SourceSlot={sourceSlot}.");
        }
        if (result.Status != EnumSlotStatus.Empty)
        {
            throw new EfemValidationException(
                $"Target slot is not empty. SourcePort={sourcePort}, SourceSlot={sourceSlot}, Status={result.Status}.");
        }
        return result;
    }

    [RelayCommand]
    public void EditStationWaferInfo(SlotItem slotItem)
    {
        if (slotItem == null || slotItem.Status != EnumSlotStatus.Normal)
        {
            return;
        }

        if (slotItem.PortNo != EnumSlotSource.Stage && slotItem.PortNo != EnumSlotSource.Aligner)
        {
            return;
        }
        var waferPostion = _efemStorageManager.GetWaferPostionByPosition((int)slotItem.PortNo);
        var oldWaferName = waferPostion?.WaferName ?? slotItem.Value;
        var oldWaferSize = waferPostion?.WaferSize ?? WaferSize.None;
        var oldPortNum = waferPostion?.PortNum ?? 0;
        var oldSlotNum = waferPostion?.SlotNum ?? 0;

        var vm = new EditWaferInfoViewModel(
            slotItem.PortNo,
            waferPostion?.WaferSize ?? WaferSize.None,
            waferPostion?.PortNum ?? (int)EnumSlotSource.Port1,
            waferPostion?.SlotNum ?? 1,
            includePort3: this is WaferHandler600ViewModel);

        var win = new EditWaferInfoWindow(vm)
        {
            Owner = System.Windows.Application.Current?.MainWindow,
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner
        };

        var result = win.ShowDialog();
        if (result != true)
        {
            return;
        }

        var waferName = $"{vm.SelectedSourcePort}-WS000{vm.SourceSlot}";
        var info = new WaferPostionInfo
        {
            PositionNum = (int)slotItem.PortNo,
            PortNum = (int)vm.SelectedSourcePort,
            SlotNum = vm.SourceSlot,
            WaferSize = vm.SelectedWaferSize,
            WaferName = waferName
        };

        _efemStorageManager.UpsertWaferPostion(info);

        if (slotItem.PortNo == EnumSlotSource.Stage)
        {
            _waferManager.UpdateWaferSize(vm.SelectedWaferSize);
        }

        slotItem.Value = waferName;
        slotItem.Status = EnumSlotStatus.Normal;

        if (slotItem.PortNo == EnumSlotSource.Stage)
        {
            StagePortItem = SlotItemClone(slotItem, EnumSlotSource.Stage);
        }
        else if (slotItem.PortNo == EnumSlotSource.Aligner)
        {
            AlignerPortItem = SlotItemClone(slotItem, EnumSlotSource.Aligner);
        }
        LogHelper.LogInfo($"EditStationWaferInfo updated. " + $"Position={slotItem.PortNo}, " +
                           $"Old[WaferName={oldWaferName}, WaferSize={oldWaferSize}, PortNum={oldPortNum}, SlotNum={oldSlotNum}] -> " +
                           $"New[WaferName={info.WaferName}, WaferSize={info.WaferSize}, PortNum={info.PortNum}, SlotNum={info.SlotNum}]");
    }
}
