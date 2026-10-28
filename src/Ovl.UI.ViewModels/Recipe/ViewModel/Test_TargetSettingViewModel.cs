using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InteVega.CBB.DomainService.Api.Interfaces;

using Marvel.DataModels.Core;
using Marvel.DataModels.Enums;
using Marvel.DataModels.Recipe;
using Microsoft.Extensions.DependencyInjection;
using Prism.Services.Dialogs;
using System;

using Ovl.UI.ViewModels.Controls;
using Ovl.UI.ViewModels.EventAggregators;
namespace Ovl.UI.ViewModels.Recipe.ViewModel
{
    public partial class Test_TargetSettingViewModel : Test_TrainViewModelBase
    {
        #region field

        /// <summary>
        /// TargetSetting对象-从Recipe内从中获取
        /// </summary>
        private TargetSetting _targetSetting;

        /// <summary>
        /// Target位置保存对象-从Recipe内从中获取
        /// </summary>
        private TargetLocation _targetLocation;

        /// <summary>
        /// TestManageInfo和WaferMap属性对象
        /// </summary>
        private TestInfoAndWaferMap _testInfoAndWaferMap;

        /// <summary>
        /// 当前选中的RowColumnInfo
        /// </summary>
        public RowColumnInfo CurrentRowCol;



        /// <summary>
        /// 是否已保存
        /// </summary>
        private bool _isSubmit;

        [ObservableProperty]
        private EnumR3OrigionPoint orgionPoint;
        [ObservableProperty]
        private WcsPoint r3Vector;
        [ObservableProperty]
        private WaferMapControl waferMapControl;
        [ObservableProperty]
        private double r3Vector_X;
        [ObservableProperty]
        private double r3Vector_Y;
        [ObservableProperty]
        private string changeCameraInfo;


        #endregion
        IVisionService _VisionService;
        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="serviceProvider">依赖注入容器</param>
        public Test_TargetSettingViewModel(IServiceProvider serviceProvider) : base(serviceProvider)
        {

            _VisionService = serviceProvider.GetService<IVisionService>();
        }


        /// <summary>
        /// Dialgo Title
        /// </summary>
        public override string Title => "Target Setting";

        /// <summary>
        /// 打开Dialog时的方法
        /// </summary>
        /// <param name="parameters">参数合集</param>
        public override void OnDialogOpened(IDialogParameters parameters)
        {
            if (parameters.ContainsKey("TargetLocation"))
            {
                _targetLocation = parameters.GetValue<TargetLocation>("TargetLocation");
                OrgionPoint = _targetLocation.OrgionPoint;
                R3Vector_X = _targetLocation.R3Vector.X;
                R3Vector_Y = _targetLocation.R3Vector.Y;
            }
            if (parameters.ContainsKey("TargetSetting"))
            {
                _targetSetting = parameters.GetValue<TargetSetting>("TargetSetting");
            }
            if (parameters.ContainsKey("TestInfoAndWaferMap"))
            {
                _testInfoAndWaferMap = parameters.GetValue<TestInfoAndWaferMap>("TestInfoAndWaferMap");
                WaferMapControl = new WaferMapControl();
                WaferMapControl.Draw(_testInfoAndWaferMap.WaferMapInfo);
            }
        }
        /// <summary>
        /// Submit命令方法
        /// </summary>
        [RelayCommand]
        private void Submit()
        {
            _targetLocation.OrgionPoint = OrgionPoint;
            _targetLocation.R3Vector.X = R3Vector_X;
            _targetLocation.R3Vector.Y = R3Vector_Y;
            _isSubmit = true;
        }

        /// <summary>
        /// 关闭方法
        /// </summary>
        [RelayCommand]
        private void Close()
        {
            var result = new DialogResult(_isSubmit ? ButtonResult.OK : ButtonResult.Cancel, new DialogParameters()
            {
                { "TargetLocation", _targetLocation }
            });
            RaiseRequestClose(result);
        }
    }
}
