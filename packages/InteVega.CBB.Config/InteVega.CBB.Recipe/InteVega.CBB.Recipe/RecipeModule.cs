using IntellVega.CBB.Module;
using InteVega.CBB.Recipe.Interface;
using Microsoft.Extensions.DependencyInjection;

namespace InteVega.CBB.Recipe
{
    public class RecipeModule : IReleasableModule
    {
        public void Init(IServiceProvider provider)
        {
        }

        public IServiceCollection Register(IServiceCollection services)
        {
            services.AddScoped<IRecipeService, RecipeService>();
            services.AddSingleton<IRecipeManager, RecipeManager>();
            services.AddSingleton<IRecipe, Recipe>();
            services.AddScoped<IRecipeServiceDAL, RecipeServiceNewFileDAL>();
            return services;
        }

        public void Release(IServiceProvider provider)
        {
        }
    }
}
