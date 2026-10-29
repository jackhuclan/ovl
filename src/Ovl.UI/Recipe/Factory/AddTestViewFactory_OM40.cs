using InteVega.CBB.Prism.UI.Extensions.Dialogs;
using Prism.Services.Dialogs;
using System;

using Ovl.UI.Recipe.ViewModel;
namespace Ovl.UI.Recipe.Factory
{
    /// <summary>
    /// Implementation of IAddTestViewFactory for OM40 that uses AddTestView_OM40
    /// </summary>
    public class OM40AddTestViewFactory : IAddTestViewFactory
    {
        public AddTestViewModel CreateViewModel(string defaultName)
        {
            return new AddTestViewModel40(defaultName);
        }

        public string GetDialogName()
        {
            return "AddTestView_OM40";
        }

        public void ShowDialog(IDialogService dialogService, object viewModel)
        {
            // Use the custom extension method from IDialogServiceExtensions
            dialogService.ShowDialog(GetDialogName(), viewModel);
        }
    }
}
