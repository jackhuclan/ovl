using Bases = Ovl.UI.ViewModels.Bases;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dm.util;
using InteVega.CBB.Prism.UI.Extensions.MessageBox;
using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.Recipe.Interface;
using Marvel.DataModels.Recipe;
using Marvel.Interfaces.Application;
using Marvel.MRO.DataModels.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using InteVega.CBB.ComUI.Helpers;
using InteVega.CBB.Db.Api.Interface;
using Marvel.Application.Services.MRO;

namespace Ovl.UI.Recipe.ViewModel
{
    partial class MRORunParametersViewModel : Bases.ViewModelBase
    {
        protected IMroService? _mroService;
        public RecipeElementsInfo recipeElementsInfo;
        public MroElementInfo mroElementInfo;
        public IServiceProvider _serviceProvider;
        public IMessageBoxService _messageBoxService;

        [ObservableProperty]
        MroRunParameter mroRunParameter = new MroRunParameter();

        public MRORunParametersViewModel(IServiceProvider serviceProvider, RecipeElementsInfo _recipeElementsInfo, MroElementInfo _mroElementInfo)
        {
            _serviceProvider = serviceProvider;
            _messageBoxService = serviceProvider.GetService<IMessageBoxService>();
            _mroService = serviceProvider.GetService<IMroService>();
            mroElementInfo = _mroElementInfo;
            recipeElementsInfo = _recipeElementsInfo;
            mroRunParameter = mroElementInfo.MroRunParameter;
            InitializeData();
        }

        private void InitializeData()
        {
            MeasureOvl = mroRunParameter.IsMeasureOvl;//是否在MRO之后量测OVL
            CompareTests = mroRunParameter.IsCompareTest;//是否比较Test
            OutputTableCreation = mroRunParameter.IsOutputLastCycle;//是否输出上一轮的报表
            SendMroData = mroRunParameter.IsSendToAnalyzer;//是否将数据输入到数据库供Analyzer软件使用
            RecipeNameSuffix = mroRunParameter.RecipeSuffix;
            TestNameSuffix = mroRunParameter.TestSuffix;
            NumberOfRecipes = mroRunParameter.NumberOfRecipes;
            Tests.Clear();
            int ID = 1;
            var canMeasureTests = recipeElementsInfo.Tests.Where(test => test.IsCanMeasure).ToList();
            if (mroRunParameter.TestForRun == null)//初始化时默认是Tests的IsEnable
            {
                foreach (var item in canMeasureTests)
                {
                    TestItem test = new TestItem { Id = ID, Testname = item.Name, Type = item.GetTestType(), IsSelected = item.IsEnable };
                    Tests.Add(test);
                    ID++;
                }
            }
            else
            {
                foreach (var item in canMeasureTests)
                {
                    TestItem test = new TestItem { Id = ID, Testname = item.Name, Type = item.GetTestType(), IsSelected = mroRunParameter.TestForRun.Contains(item.Name) };
                    Tests.Add(test);
                    ID++;
                }
            }
        }

        #region Input Parameters
        [ObservableProperty]
        private bool _measureOvl;

        partial void OnMeasureOvlChanged(bool value)
        {
            mroRunParameter.IsMeasureOvl = value;
        }

        [ObservableProperty]
        private bool _compareTests = true;

        partial void OnCompareTestsChanged(bool value)
        {
            mroRunParameter.IsCompareTest = value;
        }

        [ObservableProperty]
        public ObservableCollection<TestItem> _tests = new ObservableCollection<TestItem>();


        [RelayCommand]
        private void Load()
        {
            FileDialogHelper.OpenFileDialog("excel 文件|*.xls;*.xlsx;*.csv", (file) =>
            {
               MroRunParameter.SBSPath = file;
                //IExcelService es=_serviceProvider.GetService <IExcelService >();
                //SBSDataService.LoadSBSDatas(es, file);
            });
        }

        [RelayCommand]
        private void ClearSBS()
        {
            MroRunParameter.SBSPath = null;
        }
        #endregion

        #region Output Parameters
        [ObservableProperty]
        private int _numberOfRecipes;

        partial void OnNumberOfRecipesChanged(int value)
        {
            mroRunParameter.NumberOfRecipes = value;
        }

        [ObservableProperty]
        private bool _overwriteRecipe = true;
        partial void OnOverwriteRecipeChanged(bool value)
        {
            mroRunParameter.IsCompareTest = value;
        }

        [ObservableProperty]
        private bool _outputTableCreation = true;
        partial void OnOutputTableCreationChanged(bool value)
        {
            mroRunParameter.IsOutputLastCycle = value;
        }

        [ObservableProperty]
        private bool _sendMroData = true;
        partial void OnSendMroDataChanged(bool value)
        {
            mroRunParameter.IsSendToAnalyzer = value;
        }

        [ObservableProperty]
        private string _recipeNameSuffix = "_MRO1";
        partial void OnRecipeNameSuffixChanged(string value)
        {
            mroRunParameter.RecipeSuffix = value;
        }

        [ObservableProperty]
        private string _testNameSuffix = "_MRO1";
        partial void OnTestNameSuffixChanged(string value)
        {
            mroRunParameter.TestSuffix = value;
        }
        #endregion

        public bool Check()
        {
            mroRunParameter.TestForRun = new List<string>();
            mroRunParameter.TestForRun.clear();
            foreach (TestItem item in Tests.Where(a => a.IsSelected == true).ToList())
            {
                mroRunParameter.TestForRun.Add(item.Testname);
            }
            if (mroRunParameter.TestForRun?.FirstOrDefault() == null)
            {
                _messageBoxService.Warning("Please select the test for running MRO.", "MROConfiguration");
                return false;
            }
            return true;
        }

        [RelayCommand]
        public void Save()
        {
            if (!Check()) return;
            mroElementInfo.MroRunParameter = mroRunParameter;
            _mroService.SaveMroConfig(recipeElementsInfo, mroElementInfo);
            _messageBoxService.Success("MroRunParameter Data saved successfully!!!", "MROConfiguration");
        }

        [RelayCommand]
        private void SelectAllTests()
        {
            bool allSelected = Tests.All(t => t.IsSelected);
            foreach (var test in Tests)
            {
                test.IsSelected = !allSelected;
            }
        }
    }

    partial class TestItem : ObservableObject
    {
        [ObservableProperty]
        private int _id;

        [ObservableProperty]
        private string _testname;

        [ObservableProperty]
        private string _type;

        [ObservableProperty]
        private bool _isSelected;
    }
}
