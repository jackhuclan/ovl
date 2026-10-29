using InteVega.CBB.Recipe.Core;
using InteVega.CBB.Recipe.Interface;
using Marvel.DataModels.Recipe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ovl.UI.Interface
{
    public interface IBaseRecipePage
    {
        public void RequestData(IRecipe data, RecipeElementsInfo recipe, RecipeInfo recipeInfo);
        /// <summary>
        /// 获取当前配方信息对象
        /// </summary>
        public RecipeElementsInfo RecipeElementsInfo { get; }
        public void SaveRecipeFile();
    }
}
