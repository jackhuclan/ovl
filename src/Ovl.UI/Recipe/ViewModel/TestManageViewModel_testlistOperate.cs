using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.CommonLib.Helpers.Reflection;
using InteVega.CBB.Prism.UI.Extensions.Dialogs;
using InteVega.CBB.Recipe.Interface;
using Marvel.DataModels.Enums;
using Marvel.DataModels.Recipe;
using Marvel.DataModels.Recipe.CD;
using Marvel.DataModels.Recipe.Overlay;
using Marvel.DataModels.Recipe40;
using Marvel.DataModels.Recipe40.Rules;
using Microsoft.Extensions.DependencyInjection;
using Prism.Ioc;
using Prism.Services.Dialogs;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

using Ovl.UI.Recipe.View;
using Ovl.UI.ViewModels.Recipe.Factory;
using Ovl.UI.ViewModels.Recipe.ViewModel;
using Ovl.UI.ViewModels.EventAggregators;
namespace Ovl.UI.Recipe.ViewModel
{
    partial class TestManageViewModel: RecipeElementViewModelBase
    {
        /// <summary>
        /// Job队列发生改变时通知事件
        /// </summary>
        public Action ActionCollectionChanged;

        private readonly IAddTestViewFactory _addTestViewFactory;
        private readonly IContainerProvider _containerProvider;

        [ObservableProperty]
        private TestManageInfo selectTestRowData;
        [ObservableProperty]
        private bool _isOvlTest;
        //[ObservableProperty]
        //private bool nameEditFlag=true;
        [ObservableProperty]
        private bool cellFocusFlag;
        [RelayCommand]
        private void CreateTest()
        {
            string name = IdentityHelper.NewName("test", TestManageList, x => x.Name);

            // Use factory to create appropriate view model
            var viewModel = _addTestViewFactory?.CreateViewModel(name) ?? new Ovl.UI.ViewModels.Recipe.ViewModel.AddTestViewModel(name);

            viewModel.Title = "Add Test";
            viewModel.FunCheckName = () =>
            {
                try
                {
                    IsEnable = false;
                    var testInfo = viewModel.Test;
                    if (string.IsNullOrWhiteSpace(testInfo.Name))
                    {
                        _messageBoxService.ErrorDialog("The test name cannot be empty！");
                        return false;
                    }
                    if (TestManageList.FirstOrDefault(x => testInfo.Name.Equals(x.Name, StringComparison.OrdinalIgnoreCase)) != null)
                    {
                        _messageBoxService.ErrorDialog("A test with the same name exists！");
                        return false;
                    }
                    if (!AddTestTypeConsistencyRule.CanCloseAddTestDialog(testInfo))
                    {
                        return false;
                    }
                    string id = IdentityHelper.NewGuid(TestManageList, x => x.Id);
                    testInfo.Id = id;
                    testInfo.Createtime = DateTime.Now;
                    testInfo.IsLoadedModels = true;
                    AddTest(testInfo, true);
                    ActionCollectionChanged?.Invoke();
                    // 删除AddTest时自动保存到文件，统一改为手动点击Save时保存
                    // Save();
                    return true;
                }
                catch (Exception ex)
                {
                    _messageBoxService.ErrorDialog(ex.Message);
                    return false;
                }
                finally
                {
                    IsEnable = true;
                }
            };
            _addTestViewFactory.ShowDialog(_dialogService, viewModel);
        }
        public void OpenTest(TestManageInfo testInfo)
        {
            if (testInfo == null) return;
            SelectTestRowData = testInfo;
            var test = SelectTestRowData.TestInfo;

            IsOvlTest = test is TestInfo_CD || test is TestInfo40 ? false : true;
            if (test is TestInfo_BiB tb && test.DataCulling is DataCulling_Overlay datacullingOvl && datacullingOvl.DamageCulling == null)
            {
                //只有Bar 和 Frame类型的标记支持 Damage 量测
                if (tb.TrainSetting_BiB.InnerType == EnumTargetType.Bar || tb.TrainSetting_BiB.InnerType == EnumTargetType.Frame)
                {
                    datacullingOvl.DamageCulling = new DamageCulling();
                }
            }
            TestInfoAndWaferMap testInfoAndWaferMap = new TestInfoAndWaferMap() { TestInfo = testInfo, WaferMapInfo = RecipeElementsInfo.WaferMap };
            AppHelper.DispatcherInvoke(() => { _eventAggregator.GetEvent<TestInfoAndWaferMapEventAggregators>().Publish(testInfoAndWaferMap); });

        }

        private void TrainTest(TestInfoBase testInfo)
        {

            var parameters = new DialogParameters
            {
                { nameof(TestInfoBase),testInfo},
                { nameof(WaferMapInfo),RecipeElementsInfo.WaferMap}
            };
            Test_TrainViewModelBase.NotifySaveAction = null;
            Test_TrainViewModelBase.NotifySaveAction -= NotifySave;
            Test_TrainViewModelBase.NotifySaveAction += NotifySave;
            var testViewName = GetTrainViewName(testInfo);

            AppHelper.DispatcherInvoke(() =>
           {
               _dialogService.ShowDialog(testViewName, parameters, result =>
               {
                   if (result.Result == ButtonResult.OK)
                   {
                       if (result.Parameters.ContainsKey("modifiedTestInfo"))
                       {
                           var testinfo = result.Parameters.GetValue<TestInfoBase>("modifiedTestInfo");

                           bool isClose = result.Parameters.GetValue<bool>("isClose");
                           if (!isClose)
                           {
                               Save();
                           }
                       }
                   }
               });
           });
        }

        private static string GetTrainViewName(TestInfoBase testInfo)
        {
            return testInfo switch
            {
                TestInfo40 test40 => GetTrainViewName40(test40),
                TestInfo_CD => "Test_Train_CD_View",
                TestInfo_AIM_XYS => "Test_TrainXYSView",
                TestInfo_BiB => "Test_TrainBiBView",
                _ => string.Empty,
            };
        }

        private static string GetTrainViewName40(TestInfo40 test40)
        {
            if (UseCrossFovTrainView(test40))
                return "Test_Train40ViewModel_CrossFovView";

            if (test40.IsDoubleFace)
                return nameof(Test_Train_DoubleFaceControl);

            return nameof(Test_Train_SingleFaceControl);
        }

        private static bool UseCrossFovTrainView(TestInfo40 test40)
        {
            return test40.IsEffectiveCrossFov || (!test40.IsDoubleFace && test40.CrossFov);
        }

        private void CopyTest(TestManageInfo testInfo)
        {
            Task.Run(() =>
            {
                try
                {
                    IsEnable = false;
                    //if (selectTestRowData == null) return;
                    var newTes = (TestInfoBase)CloneHelper.ReflectionClone(testInfo.TestInfo);
                    newTes.Id = IdentityHelper.NewGuid(TestManageList, x => x.Id);
                    newTes.Name = IdentityHelper.NewName(testInfo.Name, TestManageList, x => x.Name);
                    string oldTestName = testInfo.Name;
                    string newTestName = newTes.Name;
                    var _recipePath = _recipeManager.GetRecipePath(_recipeElementsInfo.Id);
                    string oldTestFolder = Path.Combine(_recipePath, oldTestName);
                    string newTestFolder = Path.Combine(_recipePath, newTestName);
                    if (Directory.Exists(oldTestFolder))
                    {
                        FileHelper.CopyAllFiles(oldTestFolder, newTestFolder);
                    }
                    AppHelper.DispatcherInvoke(() =>
                    {
                        //_recipeElementsInfo.Tests.Add(newTes);
                        //_recipeElementsInfo.Tests = JsonHelper.Deserialize<ObservableCollection<TestInfoBase>>(JsonHelper.Serialize(_recipeElementsInfo.Tests));

                        //LoadTestManage();
                        //var newTestManageInfo = TestManageList.FirstOrDefault(x => x.Id == newTes.Id);
                        //OpenTest(newTestManageInfo);

                        AddTest(newTes, true);

                    });
                    ActionCollectionChanged?.Invoke();
                    Save();
                }
                catch (Exception ex)
                {
                    _messageBoxService.Error(ex.Message, messageToken);
                }
                finally
                {
                    IsEnable = true;
                }
            });
        }

        private void RenameTest(TestManageInfo testManageInfo)
        {
            var test = testManageInfo.TestInfo;

            // Use factory to create appropriate view model
            var viewModel = _addTestViewFactory?.CreateViewModel(test.Name) ?? new Ovl.UI.ViewModels.Recipe.ViewModel.AddTestViewModel(test.Name);

            viewModel.Title = $"Rename Test: {test.Name}";
            viewModel.IsRename = true;
            viewModel.FunCheckName = () =>
                {
                    try
                    {
                        IsEnable = false;
                        var newTestName = viewModel.Test.Name;
                        if (string.IsNullOrWhiteSpace(newTestName))
                        {
                            _messageBoxService.ErrorDialog("The test name cannot be empty！");
                            return false;
                        }
                        if (TestManageList.FirstOrDefault(x => x != testManageInfo && newTestName.Equals(x.Name, StringComparison.OrdinalIgnoreCase)) != null)
                        {
                            _messageBoxService.ErrorDialog("A test with the same name exists！");
                            return false;
                        }
                        string oldTestName = testManageInfo.TestInfo.Name;
                        var _recipePath = _recipeManager.GetRecipePath(_recipeElementsInfo.Id);
                        string oldTestFolder = Path.Combine(_recipePath, oldTestName);
                        string newTestFolder = Path.Combine(_recipePath, newTestName);
                        FileHelper.MoveFolder(oldTestFolder, newTestFolder, true);
                        testManageInfo.TestInfo.Name = newTestName;
                        testManageInfo.Name = newTestName;
                        ActionCollectionChanged?.Invoke();
                        Save();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        _messageBoxService.ErrorDialog(ex.Message);
                        return false;
                    }
                    finally
                    {
                        IsEnable = true;
                    }
                };
            _addTestViewFactory.ShowDialog(_dialogService, viewModel);
        }
        //partial void OnSelectTestRowDataChanged(TestInfo? oldValue, TestInfo newValue)
        //{
        //    Save();
        //}

        private void DeleteTest(TestManageInfo testInfo)
        {
            //if (selectTestRowData == null) return;
            ButtonResult buttonResult= _messageBoxService.YesNoCancelDialog($"Are you sure you want to delete {testInfo.Name}?", "Delete prompt");
            if (buttonResult == ButtonResult.Yes)
            {
                var exist = TestManageList.FirstOrDefault(x => x.Id == testInfo.Id);
                if (exist != null)
                {
                    var folder = _recipeManager.GetRecipePath(_recipeElementsInfo.Id);
                    folder = Path.Combine(folder, testInfo.Name);
                    if (Directory.Exists(folder))
                    {
                        Directory.Delete(folder, true);
                    }
                    TestManageList.Remove(testInfo);
                    ActionCollectionChanged?.Invoke();
                    Save();
                }
            }
        }
        [RelayCommand]
        private void ClearTests()
        {
            ButtonResult buttonResult = _messageBoxService.YesNoCancelDialog($"Are you sure you want to clear all tests?", "Clear prompt");
            if (buttonResult == ButtonResult.Yes)
            {
                TestManageList.Clear();
                SelectTestRowData = null;
                ActionCollectionChanged?.Invoke();
              
                Save();
            }
        }
        [RelayCommand]
        private void MouseDouble(TestManageInfo testManageInfo) 
        {
            OpenTest(testManageInfo);
        }
        [RelayCommand]
        private void SaveRename(TestManageInfo testManageInfo) 
        {
            testManageInfo.IsShowButton= Visibility.Collapsed;
            testManageInfo.NameEditFlag = true;
            Save();
        }
        [RelayCommand]
        public void EnableTest(TestManageInfo testManageInfo)
        {
            testManageInfo.IsEnable = true;
            testManageInfo.TestInfo.IsEnable = testManageInfo.IsEnable;
            //Save();
        }
        [RelayCommand]
        public void DisableTest(TestManageInfo testManageInfo)
        {
            testManageInfo.IsEnable = false;
            testManageInfo.TestInfo.IsEnable = false;
            //Save();
        }
        private void NotifySave(TestInfoBase modifiedTestInfo,string messageToken)
        {
            //selectTestRowData = modifiedTestInfo;
            //Save();
            SaveRecipeFiles(messageToken);
        }
    }

}
