using System.Text;
using Marvel.DataModels.Enums;
using Marvel.DataModels.Recipe;
using Marvel.Interfaces.Application;
using Marvel.MRO.DataModels.Models;
using Marvel.MRO.DataModels.Results;
using Microsoft.Extensions.DependencyInjection;

namespace Ovl.UI.Recipe.ViewModel;

internal class RunParamViewModelTest
{
    public static void TestCreatMroRecipes(IServiceProvider serviceProvider, RecipeElementsInfo recipe)
    {
        IMroService mroService = serviceProvider.GetService<IMroService>();
        var mroResult = new List<MroRecipeResult>();
        foreach (var test in recipe.Tests)
        {
            if (test is TestInfo_Overlay to)
            {
                mroResult.Add(new MroRecipeResult() { Rank = 1, TestName = test.Name, Filter = 0, FocusCorrection = 0.5, BW = 0, Direction = EnumOptimizeType.X , Layer = EnumFocusBasedOn.Inner , NA =0, Polarizer =0});
                mroResult.Add(new MroRecipeResult() { Rank = 1, TestName = test.Name, Filter = 1, FocusCorrection = 1.5, BW = 0, Direction = EnumOptimizeType.X, Layer = EnumFocusBasedOn.Outer , NA = 0, Polarizer = 0 });
                mroResult.Add(new MroRecipeResult() { Rank = 2, TestName = test.Name, Filter = 2, FocusCorrection = -0.5, BW = 0, Direction = EnumOptimizeType.X, Layer = EnumFocusBasedOn.Inner, NA = 1, Polarizer = 0 });
                mroResult.Add(new MroRecipeResult() { Rank = 2, TestName = test.Name, Filter = 3, FocusCorrection = -1.5, BW = 0, Direction = EnumOptimizeType.X, Layer = EnumFocusBasedOn.Outer, NA = 1, Polarizer = 0 });
                mroResult.Add(new MroRecipeResult() { Rank = 3, TestName = test.Name, Filter = 4, FocusCorrection = 2.5, BW = 0, Direction = EnumOptimizeType.X, Layer = EnumFocusBasedOn.Inner, NA = 2, Polarizer = 0 });
                mroResult.Add(new MroRecipeResult() { Rank = 3, TestName = test.Name, Filter = 5, FocusCorrection = 3.5, BW = 0, Direction = EnumOptimizeType.X, Layer = EnumFocusBasedOn.Outer, NA = 2, Polarizer = 0 });
                if(test is TestInfo_AIM_XYS testXYS)
                {
                    mroResult.Add(new MroRecipeResult() { Rank = 1, TestName = test.Name, Filter = 0, FocusCorrection = 0.5, BW = 0, Direction = EnumOptimizeType.Y, Layer = EnumFocusBasedOn.Inner, NA = 0, Polarizer = 1 });
                    mroResult.Add(new MroRecipeResult() { Rank = 1, TestName = test.Name, Filter = 1, FocusCorrection = 1.5, BW = 0, Direction = EnumOptimizeType.Y, Layer = EnumFocusBasedOn.Outer, NA = 0, Polarizer = 1 });
                    mroResult.Add(new MroRecipeResult() { Rank = 2, TestName = test.Name, Filter = 2, FocusCorrection = -0.5, BW = 0, Direction = EnumOptimizeType.Y, Layer = EnumFocusBasedOn.Inner, NA = 1, Polarizer = 1 });
                    mroResult.Add(new MroRecipeResult() { Rank = 2, TestName = test.Name, Filter = 3, FocusCorrection = -1.5, BW = 0, Direction = EnumOptimizeType.Y, Layer = EnumFocusBasedOn.Outer, NA = 1, Polarizer = 1 });
                    mroResult.Add(new MroRecipeResult() { Rank = 3, TestName = test.Name, Filter = 4, FocusCorrection = 2.5, BW = 0, Direction = EnumOptimizeType.Y, Layer = EnumFocusBasedOn.Inner, NA = 2, Polarizer = 1 });
                    mroResult.Add(new MroRecipeResult() { Rank = 3, TestName = test.Name, Filter = 5, FocusCorrection = 3.5, BW = 0, Direction = EnumOptimizeType.Y, Layer = EnumFocusBasedOn.Outer, NA = 2, Polarizer = 1 });
                }
            }
        }
        //mroService.CreateMroRecipes(recipe, mroResult, new MroRunParameter() { RecipeSuffix ="Mro"});
    }
}
