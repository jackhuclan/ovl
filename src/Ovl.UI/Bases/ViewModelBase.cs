using CommunityToolkit.Mvvm.ComponentModel;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ovl.UI.Bases
{
    public partial class ViewModelBase : ObservableObject, IDialogAware
    {
        [ObservableProperty]
        public string _windowTitle;

        public virtual void RaiseRequestClose(IDialogResult dialogResult)
        {
            RequestClose?.Invoke(dialogResult);
        }

        /// <summary>Determines if the dialog can be closed.</summary>
        /// <returns>If <c>true</c> the dialog can be closed. If <c>false</c> the dialog will not close.</returns>
        public virtual bool CanCloseDialog()
        {
            return true;
        }

        /// <summary>Called when the dialog is closed.</summary>
        public virtual void OnDialogClosed()
        {
        }

        /// <summary>Called when the dialog is opened.</summary>
        /// <param name="parameters">The parameters passed to the dialog.</param>
        public virtual void OnDialogOpened(IDialogParameters parameters)
        {

        }

        /// <summary>
        /// The title of the dialog that will show in the window title bar.
        /// </summary>
        public virtual string Title { get; set; } = "";


        /// <summary>
        /// Instructs the <see cref="T:Prism.Services.Dialogs.IDialogWindow" /> to close the dialog.
        /// </summary>
        public event Action<IDialogResult>? RequestClose;

    }




}
