using IntellVega.CBB.EasyConfig.Json;
using InteVega.CBB.Db.Api.Interface;
using InteVega.CBB.Recipe.Core;
using InteVega.CBB.Recipe.Interface;

namespace InteVega.CBB.Recipe
{
    public class RecipeServiceDbDAL : IRecipeServiceDAL
    {
        private IDBContext<RecipeInfo> _dbContext;
        public RecipeServiceDbDAL(IDBContext<RecipeInfo> dbcontext) 
        {
            _dbContext=dbcontext;
        }

        public bool AddRecipe(RecipeInfo data)
        {
            return _dbContext.Insert(data);            
        }

        public bool ModifyRecipe(RecipeInfo data)
        {
            throw new NotImplementedException();
        }

        public bool CopeRecipe(RecipeInfo data, string sourceId)
        {
            //查询SourceId
            return _dbContext.Equals(data);
        }

        public bool EditRecipe(List<RecipeInfo> recipeInfos, RecipeInfo data)
        {
            return _dbContext.Update(data);
        }


        public bool ExportRecipe(RecipeInfo recipeInfo, string targetPath)
        {
            throw new NotImplementedException();
        }

        public List<RecipeInfo> GetRecipeAll()
        {
            return _dbContext.GetAll();
            //throw new NotImplementedException();
        }

        public RecipeInfo GetRecipeById(string id)
        {
            return _dbContext.Get(w => w.Id == id);
        }

        public JsonConfigCollection GetRecipeJsonConfig(string id)
        {
            throw new NotImplementedException();
            //JsonConfigCollection jsonConfigCollection = new JsonConfigCollection();
        }

        public string GetRecipePath(string id)
        {
            return "";
        }

        public bool ImportRecipe(RecipeInfo recipeInfo,List<RecipeInfo> recipeInfos, string targetPath)
        {
            throw new NotImplementedException();
        }

        public bool RemoveRecipe(RecipeInfo data)
        {
            return _dbContext.Delete(data);
        }

        public bool OldPathCopyToNewPath()
        {
            throw new NotImplementedException();
        }

        public bool ImportRecipeFromExcel(string filePath)
        {
            throw new NotImplementedException();
        }

        public bool ExportRecipeToExcel(RecipeInfo recipeInfo, List<RecipeContent> recipeContents, string targetPath)
        {
            throw new NotImplementedException();
        }

        List<RecipeContent> IRecipeServiceDAL.ImportRecipeFromExcel(string filePath)
        {
            throw new NotImplementedException();
        }

        public bool RemoveInvalidRecipeById(string recipeId)
        {
            throw new NotImplementedException();
        }

        public bool IsRecipeConfigMissing(string recipeId)
        {
            throw new NotImplementedException();
        }
    }
}
