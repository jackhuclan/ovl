
//using InteVega.CBB.Interface.Core;
using IntellVega.CBB.EasyConfig.Json;
using InteVega.CBB.Recipe.Core;

namespace InteVega.CBB.Recipe.Interface
{
    public interface IRecipeService
    {
        public RecipeInfo GetRecipeById(string id);
        public List<RecipeInfo> GetRecipeAll();
        public JsonConfigCollection GetRecipeJsonConfig(string id);
        public bool AddRecipe(RecipeInfo data);
        public bool CopeRecipe(RecipeInfo data, string sourceRecipeId, string userName, string parentId);
        public bool EditRecipe(List<RecipeInfo> recipeInfos, RecipeInfo data);
        public bool RemoveRecipe(RecipeInfo data);
        public string GetRecipePath(string id);
        public bool ImportRecipe(RecipeInfo recipeInfo, List<RecipeInfo> recipeInfos, string targetPath);
        public bool ExportRecipe(RecipeInfo recipeInfo, string targetPath);

        public List<RecipeContent> ImportRecipeFromExcel(string filePath);

        public bool ExportRecipeToExcel(RecipeInfo recipeInfo, List<RecipeContent> recipeContents, string targetPath);

        public bool IsRecipeConfigMissing(string recipeId);

        public bool RemoveInvalidRecipeById(string recipeId);

        public bool OldPathCopyToNewPath();
    }
}
