using IntellVega.CBB.EasyConfig;
using IntellVega.CBB.EasyConfig.Json;
using InteVega.CBB.Recipe.Core;
using InteVega.CBB.Recipe.Interface;
using Microsoft.Extensions.DependencyInjection;

namespace InteVega.CBB.Recipe
{
    public class RecipeManager:IRecipeManager
    {
        public string Name { get; }
        /// <summary>
        /// 配置
        /// </summary>
        private readonly IEasyConfigCollection _configCollection;
        private readonly IEasyConfigCollection<RecipeInfo> _serviceProvider;
        IRecipeService _recipeService;
        public RecipeManager(IEasyConfigCollection configCollection,IRecipeService recipeService,IServiceProvider provider)
        {
            _serviceProvider= provider.GetRequiredService<IEasyConfigCollection<RecipeInfo>>();
            _configCollection = configCollection;
            _recipeService = recipeService;
        }

        public IRecipe GetCurrentRecipe(string id)
        {
            JsonConfigCollection jsonConfigCollection = _recipeService.GetRecipeJsonConfig(id);
            return new Recipe(jsonConfigCollection);
        }

        public bool EditRecipe(List<RecipeInfo> recipeInfos, RecipeInfo data)
        {
            return _recipeService.EditRecipe(recipeInfos, data);
        }
        public string GetRecipePath(string id) 
        { 
            return _recipeService.GetRecipePath(id);
        }
    }
}
