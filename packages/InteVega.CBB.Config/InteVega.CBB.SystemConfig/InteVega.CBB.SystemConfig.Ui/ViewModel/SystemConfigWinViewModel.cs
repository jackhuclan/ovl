using CommunityToolkit.Mvvm.ComponentModel;
using InteVega.CBB.SystemConfig.Interface;
using InteVega.CBB.SystemConfig.Ui.View;
using Prism.Services.Dialogs;
using System;
using System.ComponentModel;

namespace InteVega.CBB.SystemConfig.Ui.ViewModel
{
    public partial class SystemConfigWinViewModel: ObservableObject, IDialogAware, INotifyPropertyChanged
    {
        [ObservableProperty]
        PropertyControlViewModel _propertyControlViewModel;
        public SystemConfigWinViewModel(PropertyControlViewModel propertyControlViewModel,IDialogSystemWindow dialogSystemWindow)
        {
            _dialogSystemWindow = dialogSystemWindow;
            _propertyControlViewModel = propertyControlViewModel;
            _propertyControlViewModel.CloseAction = CloseWin;
           
        }

        IDialogSystemWindow _dialogSystemWindow;
        public void CloseWin(bool isOk)
        {
            if (isOk && _dialogSystemWindow?.FuncWindowClosing!=null )
            {
                var result = _dialogSystemWindow.FuncWindowClosing();
                if (result != null)
                {
                    _propertyControlViewModel.SelectPropertyConfig = result;
                    return;
                }
            }
            if (isOk)
            {
                RaiseRequestClose(new DialogResult());
            }
        }
        public virtual void RaiseRequestClose(IDialogResult dialogResult)
        {
            RequestClose.Invoke(dialogResult);
        }
        public string Title => "Settings";

        public event Action<IDialogResult> RequestClose;

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
            
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            
        }
    }



    public class DialogSystemWindow : IDialogSystemWindow
    {
        IDialogService _DialogService;

        public Func<ISystemConfig> FuncWindowClosing { get ; set ; }
        public event EventHandler WindowClosing;
        public DialogSystemWindow(IDialogService dialogService)
        {
            _DialogService = dialogService;
        }
        private void ShowDialog()
        {
            var dialogParameters = new DialogParameters
            {
            };
            DialogParameters keyValuePairs = new DialogParameters();

            //_DialogService.ShowDialog(nameof(SystemConfigWin), dialogParameters, _ => { });
            _DialogService.ShowDialog(nameof(SystemConfigWin), dialogParameters, result =>
            {
                    OnWindowClosing();
            });
        } 
        protected virtual void OnWindowClosing()
        {
            WindowClosing?.Invoke(this, EventArgs.Empty);
        }
        public void OpenWindow()
        {
            ShowDialog();
        }
    }
    public interface IDialogSystemWindow
    {
        /// <summary>
        /// 系统窗口关闭时的数据验证事件，如果数据验证失败，则不需要关闭弹窗
        /// </summary>
        Func<ISystemConfig> FuncWindowClosing { get; set; }
        public void OpenWindow();
        event EventHandler WindowClosing;
    }
}
