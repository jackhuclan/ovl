using CommunityToolkit.Mvvm.ComponentModel;
using InteVega.CBB.Common;
using InteVega.CBB.CommonLib.Helpers.Reflection;
using InteVega.CBB.Prism.UI.Extensions.MessageBox;
using Marvel.DataModels.Recipe;
using InteVega.CBB.DomainService.Api.Interfaces;
using Marvel.Interfaces.Application;
using System.Collections.ObjectModel;
using Marvel.DataModels.Core;

namespace Ovl.UI.ViewModels.Recipe.ViewModel
{
    /// <summary>
    /// OM40 专用的聚焦参数设置对话框 ViewModel
    /// 只显示 CoarseFocus 面板，不显示 FineFocus 面板
    /// </summary>
    public partial class Test_FocusTrainSettingDialogViewModel_OM40 : Test_FocusTrainSettingDialogViewModel
    {
        public Test_FocusTrainSettingDialogViewModel_OM40(
            IMessageBoxService messageBoxService,
            ITestService testService)
            : base(messageBoxService, testService)
        {
        }

        /// <summary>
        /// 重写创建属性 ViewModel 方法，只创建 CoarseFocusInfo 的属性
        /// OM40 只使用 Z 轴进行聚焦，没有 PZT 精聚焦功能
        /// </summary>
        protected override void CreatePropertyViewModels(FocusInfo focusInfo)
        {
            _clonedFocusInfo = CloneHelper.ReflectionClone(focusInfo) as FocusInfo;

            // 只为 CourseFocusInfo 创建属性 ViewModel
            var coarsePropertyModels = PropertyViewModelFactory.CreatePropertyModels(
                _clonedFocusInfo.CourseFocusInfo
            );

            PropertyViewModels = new ObservableCollection<PropertyViewModel>(coarsePropertyModels);
        }
    }
}
