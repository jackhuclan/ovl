using InteVega.CBB.Prism.UI.Extensions.Dialogs;
using Prism.Services.Dialogs;

using Ovl.UI.Recipe.ViewModel;

namespace Ovl.UI.Recipe.Factory;

/// <summary>
/// Default implementation of IAddTestViewFactory that uses the original AddTestView_Default
/// </summary>
public class DefaultAddTestViewFactory : IAddTestViewFactory
{
    public AddTestViewModel CreateViewModel(string defaultName)
    {
        return new AddTestViewModel(defaultName);
    }

    public string GetDialogName()
    {
        return "AddTestView_Default";
    }

    public void ShowDialog(IDialogService dialogService, object viewModel)
    {
        // Use the custom extension method from IDialogServiceExtensions
        dialogService.ShowDialog(GetDialogName(), viewModel);
    }
}
