//using InteVega.CBB.Interface.Core;
using IntellVega.CBB.EasyConfig.Json;
using InteVega.CBB.Recipe.Core;
using InteVega.CBB.Recipe.Interface;

namespace InteVega.CBB.Recipe
{
    public class RecipeService:IRecipeService
    {
        IRecipeServiceDAL _recipeDAL;
        public RecipeService(IRecipeServiceDAL recipeDAL) 
        { 
            _recipeDAL = recipeDAL;
        }
        public RecipeInfo GetRecipeById(string id) 
        {
           return _recipeDAL.GetRecipeById(id);
        }
        public bool AddRecipe(RecipeInfo data)
        {
            return _recipeDAL.AddRecipe(data);
        }
        public bool CopeRecipe(RecipeInfo data, string sourceRecipeId, string userName, string parentId)
        {
            data.Id = Guid.NewGuid().ToString();
            data.IsUse = false;
            data.CreateDate = DateTime.Now;
            data.CreateUser = userName;
            data.UpdateUser = userName;
            data.UpdateDate = DateTime.Now;
            data.ParentId = parentId;
            return _recipeDAL.CopeRecipe(data, sourceRecipeId);
        }
        public bool EditRecipe(List<RecipeInfo> recipeInfos, RecipeInfo data)
        {
            return _recipeDAL.EditRecipe(recipeInfos, data);
        }
        public JsonConfigCollection GetRecipeJsonConfig(string id)
        {
            return _recipeDAL.GetRecipeJsonConfig(id);
        }
        public bool RemoveRecipe(RecipeInfo data)
        {
           return _recipeDAL.RemoveRecipe(data);
        }
        public string GetRecipePath(string id) 
        { 
            return _recipeDAL.GetRecipePath(id);
        }
        public bool ImportRecipe(RecipeInfo recipeInfo,List<RecipeInfo> recipeInfos, string targetPath) 
        {
            return _recipeDAL.ImportRecipe(recipeInfo,recipeInfos,targetPath);
        }
        public bool ExportRecipe(RecipeInfo recipeInfo, string targetPath) 
        {
            return _recipeDAL.ExportRecipe(recipeInfo,targetPath);
        }
        public List<RecipeInfo> GetRecipeAll() 
        {
           return _recipeDAL.GetRecipeAll();
        }

        public bool OldPathCopyToNewPath()
        {
            return _recipeDAL.OldPathCopyToNewPath();
        }

        public bool ExportRecipeToExcel(RecipeInfo recipeInfo, List<RecipeContent> recipeContents, string targetPath)
        {
            return _recipeDAL.ExportRecipeToExcel(recipeInfo, recipeContents, targetPath);  
        }

        public List<RecipeContent> ImportRecipeFromExcel(string filePath)
        {
            return _recipeDAL.ImportRecipeFromExcel(filePath);
        }
        public bool RemoveInvalidRecipeById(string recipeId)
        {
            return _recipeDAL.RemoveInvalidRecipeById(recipeId);
        }

        public bool IsRecipeConfigMissing(string recipeId)
        {
            return _recipeDAL.IsRecipeConfigMissing(recipeId);
        }
    }
}
