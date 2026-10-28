using InteVega.CBB.Recipe.Core;

namespace InteVega.CBB.Recipe.Interface
{
    public interface IRecipeManager
    {
        IRecipe GetCurrentRecipe(string id);
        bool EditRecipe(List<RecipeInfo> recipeInfos, RecipeInfo data);
        string GetRecipePath(string id);
    }
}
