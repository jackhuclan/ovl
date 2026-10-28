using Prism.Services.Dialogs;
using System;

using Ovl.UI.ViewModels.Recipe.ViewModel;
namespace Ovl.UI.ViewModels.Recipe.Factory
{
    /// <summary>
    /// Factory interface for creating AddTest views and view models
    /// </summary>
    public interface IAddTestViewFactory
    {
        /// <summary>
        /// 创建AddTestViewModel或AddTestViewModel40
        /// </summary>
        AddTestViewModel CreateViewModel(string defaultName);
        
        /// <summary>
        /// 获取dialog名称
        /// </summary>
        string GetDialogName();
        
        /// <summary>
        /// 展示dialog
        /// </summary>
        /// <param name="dialogService">dialog通用接口</param>
        /// <param name="viewModel">viewmodel实例</param>
        void ShowDialog(IDialogService dialogService, object viewModel);
    }
}
