using IntellVega.CBB.EasyConfig.Json;
using InteVega.CBB.Recipe.Core;

namespace InteVega.CBB.Recipe.Interface
{
    public interface IRecipeServiceDAL
    {
        /// <summary>
        /// 根据ID查询Recipe
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public RecipeInfo GetRecipeById(string id);
        /// <summary>
        /// 查询Recipe文件夹列表
        /// </summary>
        /// <returns></returns>
        public List<RecipeInfo> GetRecipeAll();
        public JsonConfigCollection GetRecipeJsonConfig(string id);
        public bool AddRecipe(RecipeInfo data);
        /// <summary>
        /// 配方参数被修改
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool ModifyRecipe(RecipeInfo data);
        public bool CopeRecipe(RecipeInfo data, string sourceId);
        public bool EditRecipe(List<RecipeInfo> recipeInfos, RecipeInfo data);
        public bool RemoveRecipe(RecipeInfo data);
        /// <summary>
        /// 根据RecipeId查询Recipe文件位置
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public string GetRecipePath(string id);

        public bool ImportRecipe(RecipeInfo recipeInfo,List<RecipeInfo> recipeInfos,string targetPath);
        public bool ExportRecipe(RecipeInfo recipeInfo, string targetPath);

        public List<RecipeContent> ImportRecipeFromExcel(string filePath);

        public bool ExportRecipeToExcel(RecipeInfo recipeInfo, List<RecipeContent> recipeContents, string targetPath);

        public bool IsRecipeConfigMissing(string recipeId);

        public bool RemoveInvalidRecipeById(string recipeId);

        public bool OldPathCopyToNewPath();

    }
}
