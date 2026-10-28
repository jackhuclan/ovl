using IntellVega.CBB.Module;
using InteVega.CBB.Recipe.Interface;
using InteVega.CBB.Recipe.Ui.Interface;
using InteVega.CBB.Recipe.Ui.View;
using InteVega.CBB.Recipe.Ui.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InteVega.CBB.Recipe.Ui
{
    public static class RecipeContainerModel
    {

        public static void AddRecipeModule(this IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterDialog<AddRecipe,AddRecipeViewModel>();
            containerRegistry.RegisterDialog<ImportRecipe,ImportRecipeViewModel>();
            containerRegistry.RegisterSingleton<IRecipeWindow, RecipeDialogService>();
        }
    }
    [Depend(typeof(RecipeModule))]
    public class ReccipeUIModule : IReleasableModule
    {
        public void Init(IServiceProvider provider)
        {

        }

        public IServiceCollection Register(IServiceCollection services)
        {

            services.AddTransient<RecipeMenuViewModel>();
            services.AddTransient<RecipeWinViewModel>();

            return services;
        }

        public void Release(IServiceProvider provider)
        {
        }
    }


}
