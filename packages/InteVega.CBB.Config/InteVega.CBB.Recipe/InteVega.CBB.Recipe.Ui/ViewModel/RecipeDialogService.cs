using InteVega.CBB.Recipe.Ui.View;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InteVega.CBB.Common;
using InteVega.CBB.Recipe.Core;
using InteVega.CBB.Recipe.Ui.Interface;

namespace InteVega.CBB.Recipe.Ui.ViewModel
{
    public class RecipeDialogService : IRecipeWindow
    {
        IDialogService _DialogService;
        public RecipeDialogService(IDialogService dialogService)
        {
            _DialogService = dialogService;
        }
        private void ShowDialog(RecipeInfo recipeInfo,Action<RecipeInfo,OperationType> callback, OperationType operationType) 
        {
            var dialogParameters = new DialogParameters
            {
                {nameof(AddRecipeViewModel.RecipeView), recipeInfo},
                {nameof(AddRecipeViewModel.OPType), operationType},
                {nameof(AddRecipeViewModel.RefreshData), callback},
            };
            DialogParameters keyValuePairs = new DialogParameters();
            
            _DialogService.ShowDialog(nameof(AddRecipe), dialogParameters, _ => { });
        }
        public void OpenWindow(RecipeInfo recipeInfo,Action<RecipeInfo, OperationType> callback, OperationType operationType)
        {
            ShowDialog(recipeInfo,callback, operationType);
        }
        public void OpenImportWindow(RecipeInfo recipeInfo, string filePath, Action<RecipeInfo> callback)
        {
            var dialogParameters = new DialogParameters
            {
                {nameof(ImportRecipeViewModel._recipeInfo), recipeInfo},
                {nameof(ImportRecipeViewModel._filePath), filePath},
                {nameof(AddRecipeViewModel.RefreshData), callback},
            };
            DialogParameters keyValuePairs = new DialogParameters();

            _DialogService.ShowDialog(nameof(ImportRecipe), dialogParameters, _ => { });
        }
    }
}
