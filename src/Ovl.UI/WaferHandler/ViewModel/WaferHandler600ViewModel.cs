using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.CommunicationApi.Exceptions;
using InteVega.CBB.ComUI.Helpers;
using InteVega.CBB.DomainService.Api.Models;
using InteVega.CBB.EfemApi;
using InteVega.CBB.EfemApi.AlignParam;
using InteVega.CBB.EfemApi.Positions;
using InteVega.CBB.EfemApi.Status;
using Marvel.DataModels.Enums;
using Microsoft.Extensions.Logging;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Ovl.UI.WaferHandler.ViewModel
{
    public partial class WaferHandler600ViewModel : WaferHandlerViewModel
    {
        public override string JitImageSource =>
           "pack://application:,,,/Marvel.Main;Component/Resources/Images/img_jit@3x.png";
        [ObservableProperty]
        ObservableCollection<SlotSource> _slotItemList3;


        [ObservableProperty]
        SlotItem _robotPortItem3;


        [ObservableProperty]
        string _currentWaferSize3;

        WaferSize _waferSize3;


        [ObservableProperty]
        bool _portItem3IsEnabled;


        [ObservableProperty]
        bool port3Enb;

        public WaferHandler600ViewModel(IServiceProvider serviceProvider, ILoggerFactory? loggerFactory) : base(serviceProvider)
        {
            _waferOrientaion = 0;// 默认180
            WaferOrientaion = _efemStorageManager.ReadCacheDirection();
            Load();
        }



        protected override WaferSize GetWaferSize(EnumSlotSource source)
        {
            switch (source)
            {
                case EnumSlotSource.Port1:
                    return _waferSize1;
                case EnumSlotSource.Port2:
                    return _waferSize2;
                case EnumSlotSource.Port3:
                    return _waferSize3;

                default:
                    return _efemStorageManager.GetWaferPostionByPosition((int)source).WaferSize;
            }
        }

        /// <summary>
        /// 刷新料盒工站
        /// </summary>
        protected override void BindSoltData()
        {
            base.BindSoltData();
            SlotItemList3 = new ObservableCollection<SlotSource>();

            SetSlotData(EnumSlotSource.Port3,
                _efemStorageManager.SlotsCache.ContainsKey(3) ?
                _efemStorageManager.SlotsCache[3] : new List<EnumSlotState>(),
                slotItem => SlotItemList3.Add(new SlotSource { SlotItem = slotItem }));
        }



        protected override void SetWaferSize(int port, WaferSize warfersize)
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
            else if (port == 3)
            {
                CurrentWaferSize3 = warfersize.ToString();
                _waferSize3 = warfersize;
            }
            else
            {
                throw new Exception("The port num is not supported");
            }
        }



        public override void LoadWaferSize()
        {
            base.LoadWaferSize();
            LoadWaferSize(3, (b) => Port3Enb = b);
        }

        public override EnumSlotStatus GetShowEnumSlotStatus(EnumSlotState enumSlotState)
        {

            EnumSlotStatus enumSlotStatus = base.GetShowEnumSlotStatus(enumSlotState);
            switch (enumSlotState)
            {
                case EnumSlotState.TooThin:
                    enumSlotStatus = EnumSlotStatus.Tilted;
                    break;

                default:
                    break;
            }
            return enumSlotStatus;
        }




        /// <summary>
        /// 更新port2晶圆状态
        /// </summary>
        [RelayCommand]
        public void Map3()
        {
            Task.Run(() =>
            {
                ProgressDialogHelper.ShowDialog("Map3 Loading...", () =>
                {
                    Logger?.LogDebug($"Map3 Loading...");
                    GetSlotDataStatus(EnumSlotSource.Port3, SlotItemList3, isEnable => PortItem3IsEnabled = isEnable);
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
        public void Load3()
        {
            Task.Run(() =>
            {
                ProgressDialogHelper.ShowDialog("Load3 Loading...", () =>
                {
                    _ = WorkOnThread(() =>
                    {
                        Logger?.LogDebug($"Load3 Loading...");
                        GetSlotDataStatus(EnumSlotSource.Port3, SlotItemList3, isEnable => PortItem3IsEnabled = isEnable);
                        return string.Empty;
                    }, "Load completed！"); //初始化完成
                    return true;
                });
            });
            
        }

        [RelayCommand]
        public void Unload3()
        {
            Task.Run(() =>
            {
                ProgressDialogHelper.ShowDialog("Uninstalling...", () =>
                {
                    _ = WorkOnThread(() =>
                    {
                        _efemService.Close((int)EnumSlotSource.Port3);
                        _efemService.UnlockPort((int)EnumSlotSource.Port3);
                        return string.Empty;
                    }, "Unload completed！"); //初始化完成
                    return true;
                });
            });
        }




        /// <summary>
        /// 拖拽到Port
        /// </summary>
        /// <param name="selectSlotItem">拿料位的Block块前端对象</param>
        /// <param name="targetSlotItem">放料位的Block块前端对象，Port1或Port2</param>
        /// <returns></returns>
        protected override bool HandleDefaultCase(SlotItem selectSlotItem, SlotItem targetSlotItem)
        {
            bool result = PortTransHandler(SlotItemList, selectSlotItem, targetSlotItem);
            result = result ? result : PortTransHandler(SlotItemList2, selectSlotItem, targetSlotItem);
            result = result ? result : PortTransHandler(SlotItemList3, selectSlotItem, targetSlotItem);
            if (result)
            {
                selectSlotItem.Value = "";
                selectSlotItem.Status = EnumSlotStatus.Empty;
            }
            return result;
        }



    }
}
