using CommunityToolkit.Mvvm.ComponentModel;
using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.DomainService.Api.Interfaces;
using InteVega.CBB.DomainService.Api.Models;
using InteVega.CBB.EfemApi;
using InteVega.CBB.Prism.UI.Extensions.Dialogs;
using InteVega.CBB.Prism.UI.Extensions.MessageBox;
using Megarobo.Ovl.Service.IntfFn.fn;
using Microsoft.Extensions.DependencyInjection;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

using Ovl.UI.WaferHandler.View;
namespace Ovl.UI.WaferHandler.ViewModel
{
    public class WaferHandlerViewModelBase : ObservableObject
    {
        protected readonly IWaferPresenceManager _waferManager;
        protected readonly IMessageBoxService _messageBoxService;
        protected readonly IDialogService _dialogService;
        protected readonly IEfemStorageManager _efemStorageManager;

        public WaferHandlerViewModelBase(IServiceProvider serviceProvider)
        {
            _waferManager = serviceProvider.GetService<IWaferPresenceManager>();
            _messageBoxService = serviceProvider.GetService<IMessageBoxService>();
            _dialogService = serviceProvider.GetRequiredService<IDialogService>();
            _efemStorageManager = serviceProvider.GetService<IEfemStorageManager>();   
        }


        private bool IsWaferLoadedByEFEM()
        {
            LogHelper.LogError($"IsWaferLoadedByEFEM StageMoveEnable = {_waferManager.StageMoveEnable}, LoadingMode = {_waferManager.LoadingMode}");
            return _waferManager.StageMoveEnable == true && _waferManager.LoadingMode == LoadingMode.EFEM;
        }
        protected async Task JudgeWaferLoadeByEFEMAsync()
        {

            if (IsWaferLoadedByEFEM())
            {
                return;
            }
            var msg = "There is a risk of wafer chipping when unloading EFEM!\r\n" +
                            "The wafer is not continuously vacuum-held or is not loaded by the EFEM.\r\n\r\n" +
                             "Please confirm the wafer size on LoadLock(Stage):\r\n\r\n";

            var waferSizeTexts = GetSupportedWaferSizeTexts();
            if (waferSizeTexts.Length == 0)
            {
                throw new Exception("No supported wafer sizes found in system configuration.");
            }
            var selectedSize = await SelectWaferSizeAsync(msg, waferSizeTexts);
            _waferManager.UpdateWaferSize(selectedSize);
            _waferManager.UpdateLoadingMode(LoadingMode.EFEM);
            _waferManager.SaveCacheToDisk();
            _efemStorageManager.UpdataOrAddWafer_Stage(selectedSize);

        }

        private Task<WaferSize> SelectWaferSizeAsync(string prompt, string[] waferSizes)
        {
            if (System.Windows.Application.Current == null)
                throw new InvalidOperationException("Application.Current is null.");

            var tcs = new TaskCompletionSource<WaferSize>(TaskCreationOptions.RunContinuationsAsynchronously);
            _ = System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var win = new SelectWaferSize(prompt, waferSizes)
                {
                    Owner = System.Windows. Application.Current.MainWindow,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Topmost = true
                };

                var ok = win.ShowDialog();
                if (ok == true && win.SelectedWaferSize.HasValue)
                {
                    tcs.TrySetResult(win.SelectedWaferSize.Value);
                }
                else
                {
                    // 按你的需求：窗口不能关闭，必须选择一个；理论上不会走到这里
                    tcs.TrySetException(new Exception("Wafer size is not selected."));
                }
            });

            return tcs.Task;
        }
        private string[] GetSupportedWaferSizeTexts()
        {
            // 以 IWaferPresenceManager 的配置为准：它内部就是从 IServiceVaccum.GetChuckTypes() 初始化出来的
           // var infos = _waferManager.GetWaferSizeInfos() ?? new List<WaferSizeInfo>();

            //var sizes = infos
            //    .Where(w => w != null)
               
            //    .Distinct()

            //    .ToArray();

            //return sizes;
            return new[]
          {
                "6 inch",
                "8 inch",
                "12 inch"
            };
        }

        //protected async Task JudgeWaferLoadeByEFEMAsync()
        //{
        //    var tcs = new TaskCompletionSource<bool>();

        //    await Task.Run(() =>
        //    {
        //        AppHelper.DispatcherInvoke(() =>
        //        {
        //            if (!IsWaferLoadedByEFEM())
        //            {
        //                LogHelper.LogError("Wafer Chipping Risk!");
        //                _messageBoxService.ErrorDialog("There is a risk of wafer chipping when unloading EFEM! The wafer is not continuously vacuum-held or is not loaded by the EFEM, please think carefully!");
        //            }
        //            tcs.SetResult(true);
        //        });
        //    });

        //    await tcs.Task; // 等待 UI 操作完成
        //}
    }
}
