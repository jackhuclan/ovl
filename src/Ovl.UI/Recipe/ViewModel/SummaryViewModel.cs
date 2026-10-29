using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using InteVega.CBB.Recipe.Core;
using InteVega.CBB.Recipe.Interface;
using Marvel.DataModels.Core;
using Marvel.DataModels.Enums;
using Marvel.DataModels.Recipe;

namespace Ovl.UI.Recipe.ViewModel;

public partial class SummaryViewModel : RecipeElementViewModelBase
{
    ~SummaryViewModel()
    { }
    [ObservableProperty]
    RecipeBaseViewModel _recipeBaseViewModel;
    [ObservableProperty]
    string _waferMapStatus;
    [ObservableProperty]
    string _AlignmentStatus;
    [ObservableProperty]
    string _runParametersStatus;
    [ObservableProperty]
    ObservableCollection<TestInfoBase> _testDataList;
    public SummaryViewModel(IServiceProvider serviceProvider) :base (serviceProvider)
    {

    }
    public override void RequestData(IRecipe data, RecipeElementsInfo recipe, RecipeInfo recipeInfo)
    {
        base.RequestData(data, recipe, recipeInfo);
        RecipeBaseViewModel = new RecipeBaseViewModel();
        if (data != null)
        {
            RecipeBaseViewModel.RecipeName = recipeInfo.RecipeName;
            RecipeBaseViewModel.UpdateDate = recipeInfo.UpdateDate;
            RecipeBaseViewModel.UpdateUser = recipeInfo.UpdateUser;
            RecipeBaseViewModel.CreateDate = recipeInfo.CreateDate;
            RecipeBaseViewModel.CreateUser = recipeInfo.CreateUser;
            RecipeBaseViewModel.Id = recipeInfo.Id;
        }
        WaferMapStatus = recipe.WaferMap.ModuleState == EnumModuleState.SetUp ? "Already set" : "Not set";
        AlignmentStatus = recipe.Alignment.ModuleState == EnumModuleState.SetUp ? "Already set" : "Not set";
        RunParametersStatus = recipe.RunParameter.ModuleState == EnumModuleState.SetUp ? "Already set" : "Not set";
        TestDataList = recipe.Tests;
        //foreach (var item in recipe.Tests)
        //{

        //}
        //_recipeElementsInfo.Tests
    }
    public override void SaveRecipeFile()
    {
        base.SaveRecipeFile(); }
        //public void RequestData(IRecipe data, RecipeInfo recipeInfo)
        //{
        //    RecipeBaseViewModel = new RecipeBaseViewModel();
        //    if (recipeInfo!=null)
        //    {
        //        RecipeBaseViewModel.RecipeName = recipeInfo.RecipeName;
        //        RecipeBaseViewModel.UpdateDate = recipeInfo.UpdateDate;
        //        RecipeBaseViewModel.CreateDate=recipeInfo.CreateDate;
        //        RecipeBaseViewModel.Id = recipeInfo.Id;
        //    }
        //}
    }
