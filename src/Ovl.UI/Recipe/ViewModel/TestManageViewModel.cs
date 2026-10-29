using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.Prism.UI.Extensions.MessageBox;
using InteVega.CBB.Recipe.Core;
using InteVega.CBB.Recipe.Interface;
using InteVega.CBB.TemplateMatching.Core;
using Marvel.DataModels.Core;
using Marvel.DataModels.Enums;
using Marvel.DataModels.Recipe;
using Marvel.DataModels.Recipe.CD;
using Marvel.DataModels.Recipe.Overlay;
using Microsoft.Extensions.DependencyInjection;
using Prism.Events;
using Prism.Ioc;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;


using Ovl.UI.Recipe.ViewModel;
using Ovl.UI.EventAggregators;
using Ovl.UI.Recipe.Factory;
namespace Ovl.UI.Recipe.ViewModel
{
    public partial class TestManageViewModel: RecipeElementViewModelBase
    {
        ~TestManageViewModel() 
        { }

        private readonly IEventAggregator _eventAggregator;
        IDialogService _dialogService;
        IServiceProvider _serviceProvider;
        [ObservableProperty]
        ObservableCollection<TestManageInfo> _testManageList;
        [ObservableProperty]
        bool _isShowFaceMode = MarvelVersion.CurrentVersion == EnumOMVersion.OM40;
        string messageToken = "RecipeMessage";
        /// <summary>
        /// 构造函数
        /// </summary>
        public TestManageViewModel(IServiceProvider serviceProvider) : base (serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _eventAggregator = serviceProvider.GetService<IEventAggregator>();
            _messageBoxService = serviceProvider.GetService<IMessageBoxService>();
            _dialogService = serviceProvider.GetService<IDialogService>();
            _recipeManager = serviceProvider.GetService<IRecipeManager>();
            _addTestViewFactory = serviceProvider?.GetService<IAddTestViewFactory>();
            RegisterAITools();
        }


        /// <summary>
        /// 注册 AI 工具。本 ViewModel 在 Tests 页签加载时才创建，
        /// 因此 add_test / open_test / train_test / save_test 只在测试管理界面打开后可用。
        /// </summary>
        private void RegisterAITools()
        {
                return;




        }

        /// <summary>
        /// AI 工具 add_test：触发 CreateTest 流程（生成默认名 testN 并弹出模态 Add Test 对话框）。
        /// 模态对话框涉及 WPF 窗口创建，必须投递到 UI 线程且不能阻塞工具线程，
        /// 因此 BeginInvoke 触发后短暂延迟再返回；实际创建由用户在对话框中点 Save 完成。
        /// </summary>
        private async Task<string> addTestByTool()
        {
            if (TestManageList == null)
                return "测试列表尚未加载完成，请先打开测试管理(Tests)界面后重试";

            AppHelper.DispatcherInvoke(() => CreateTestCommand.Execute(null));
            // 等待对话框弹出
            await Task.Delay(2000).ConfigureAwait(false);

            return "Add Test 对话框已打开（已生成默认测试名），请人工在对话框中确认测试名称与类型后点击 Save；保存后该测试会自动添加到列表并打开";
        }

        /// <summary>
        /// 按名称查找测试：先精确匹配（忽略大小写），找不到再做包含匹配。
        /// 返回匹配结果与提示信息；未找到或重名时返回候选/错误说明。
        /// </summary>
        private List<TestManageInfo> FindTestsByName(string testName, out string error)
        {
            error = null;
            var keyword = testName?.Trim();
            if (string.IsNullOrEmpty(keyword))
            {
                error = "请明确告诉我测试名称是什么";
                return null;
            }

            var list = TestManageList
                .Where(x => string.Equals(x.Name, keyword, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (list.Count == 0)
            {
                list = TestManageList
                    .Where(x => x.Name != null && x.Name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
            }

            if (list.Count == 0)
                error = "没有找到测试，请明确告诉我测试名称是什么";
            else if (list.Count > 1)
                error = $"找到多个测试，请告诉唯一的测试名称：{Environment.NewLine}{string.Join(Environment.NewLine, list.Select(x => "- " + x.Name))}";

            return list;
        }

        /// <summary>
        /// AI 工具 open_test：按名称打开(选中)一个测试。
        /// </summary>
        private string openTestByName([Description("测试名称")] string testName)
        {
            if (TestManageList == null)
                return "测试列表尚未加载完成，请先打开测试管理(Tests)界面后重试";

            var list = FindTestsByName(testName, out var error);
            if (error != null)
                return error;

            var found = list[0];
            AppHelper.DispatcherInvoke(() => OpenTest(found));
            return $"已打开测试{found.Name}";
        }

        /// <summary>
        /// AI 工具 train_test：按名称打开测试的训练界面。
        /// TrainTest 会弹出模态训练对话框，必须投递到 UI 线程且不能阻塞工具线程，
        /// 因此 BeginInvoke 触发后短暂延迟再返回；训练由用户人工完成。
        /// </summary>
        private async Task<string> trainTestByName([Description("测试名称")] string testName)
        {
            if (TestManageList == null)
                return "测试列表尚未加载完成，请先打开测试管理(Tests)界面后重试";

            var list = FindTestsByName(testName, out var error);
            if (error != null)
                return error;

            var found = list[0];
            AppHelper.DispatcherInvoke(() => TrainTest(found.TestInfo));
            // 等待训练对话框弹出
            await Task.Delay(2000).ConfigureAwait(false);

            return $"已打开测试{found.Name}的训练界面，请人工完成训练与保存后关闭对话框，完成后回复我继续";
        }

        /// <summary>
        /// AI 工具 save_test：保存配方中的测试配置，返回真实保存结果。
        /// </summary>
        private string saveTestByTool()
        {
            var ok = SaveRecipeFiles();
            return ok
                ? "测试配置保存成功"
                : "测试配置保存失败，请检查界面上是否有报错弹窗并处理后重试";
        }

        IRecipeManager _recipeManager;

        /// <summary>
        /// 创建默认模板
        /// </summary>
        /// <returns></returns>
        private List<SingleTemplateMatchingContext> CreateNewModels(string name)
        {
            // List<(ParamCreateBase creatingParam, ParamMatchBase matchingParam)> ModelParams = new() { (new ParamCreateShape(), new ParamMatchShape()), (new ParamCreateNcc(), new ParamMatchNcc()) };
            SingleTemplateMatchingContext singleTemplateMatchingContext = new SingleTemplateMatchingContext() { Name = name };
            //  singleTemplateMatchingContext.ModelParams.AddRange(ModelParams);
            return new List<SingleTemplateMatchingContext>
                { singleTemplateMatchingContext };

        }

        private void AddTest(TestInfoBase test, bool isOpen = false)
        {
            EnumTargetType inner_type = default;
            EnumTargetType outer_type = default;

            if (test is TestInfo_BiB testInfo_Bib)
            {
                inner_type = testInfo_Bib.TrainSetting_BiB.InnerType;
                outer_type = testInfo_Bib.TrainSetting_BiB.OuterType;
            }
            else if(test is TestInfo_AIM_XYS testXYS)
            { 
                inner_type =testXYS .TrainSetting_XYS.InnerType;
                outer_type =testXYS.TrainSetting_XYS.OuterType;
            }
            else if (test is TestInfo_CD testInfo_CD)
            {
                inner_type = testInfo_CD.TrainSetting_CD.Type;
                outer_type = testInfo_CD.TrainSetting_CD.Type;

            }

            if (test is TestInfo_Overlay to)
            {
                var testTrainSetting = to.TrainSetting as TrainSettings_Overlay;
                if (testTrainSetting.OverlayMark_InnerModels == null)
                {
                    testTrainSetting.OverlayMark_InnerModels = CreateNewModels("Inner Model");
                }
                if (testTrainSetting.OverlayMark_OuterModels == null)
                {
                    testTrainSetting.OverlayMark_OuterModels = CreateNewModels("Outer Model");
                }
            }

            var testManageInfo = new TestManageInfo
            {
                Id = test.Id,
                Name = test.Name,

                InnerType = inner_type,
                OuterType = outer_type,
                TestInfo = test,
               
                OpenTestCallback = (t) => OpenTest(t),
                TrainTestCallback = (t) => TrainTest(t),
                CopyTestCallback = (t) => CopyTest(t),
                RenameTestCallback = (t) => RenameTest(t),
                EnableTestCallback = (t) => EnableTest(t),
                DisableTestCallback = (t) => DisableTest(t),
                DeleteTestCallback = (t) => DeleteTest(t),
            };
            TestManageList.Add(testManageInfo);
            if (isOpen)
                OpenTest(testManageInfo);
        }
        void LoadTestManage() 
        {
            SelectTestRowData = null;
            TestManageList = new ObservableCollection<TestManageInfo>();
            if (_recipeElementsInfo?.Tests != null)
            {
                for (int i = 0; i < _recipeElementsInfo.Tests.Count; i++)
                {
                    var test = _recipeElementsInfo.Tests[i];
                    if (test is Marvel.DataModels.Recipe40.TestInfo40 test40)
                        test40.EnsureCrossFovTrainSettings();
                    AddTest(test);
                }
            }
            ActionCollectionChanged?.Invoke();
        }
 
        IMessageBoxService _messageBoxService;

        public override void RequestData(IRecipe data, RecipeElementsInfo recipe, RecipeInfo recipeInfo)
        {
            base.RequestData(data, recipe,recipeInfo);
            //TestInfoList = _recipeElementsInfo.Tests;
            LoadTestManage();
        }
        [RelayCommand]
        public void Save()
        {
            SaveRecipeFiles();
        }
        /// <summary>
        /// 保存测试配置。返回是否保存成功（AI 工具 save_test 依赖该结果）。
        /// </summary>
        public bool SaveRecipeFiles(string msgToken=null)
        {
            try
            {
                //foreach (var item in TestManageList)
                //{
                //    foreach (var target in item.TestInfo.TargetSetting.TargetPoints2)
                //    {
                //        target.R2 = target.R3Vector + _recipeElementsInfo.WaferMap.LlcR1;
                //    }
                //}

                AppHelper.DispatcherInvoke(() =>
                {
                    _eventAggregator.GetEvent<SaveEventAggregator>().Publish();
                    _recipeElementsInfo.Tests = new ObservableCollection<TestInfoBase>(TestManageList.Select(x => x.TestInfo).ToList());


                });
                SaveRecipeFile();
                _messageBoxService.Success("Data saved successfully！", msgToken ?? messageToken);
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.LogError(ex, "Failed to save data！");
                _messageBoxService.ErrorDialog("Failed to save data！");
                return false;
            }
        }
    }
}
