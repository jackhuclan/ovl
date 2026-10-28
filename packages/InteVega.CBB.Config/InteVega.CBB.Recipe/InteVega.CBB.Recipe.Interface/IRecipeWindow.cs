using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InteVega.CBB.Common;
using InteVega.CBB.Recipe.Core;

namespace InteVega.CBB.Recipe.Interface
{
    public interface IRecipeWindow
    {
        public void OpenWindow(RecipeInfo recipeInfo,Action<RecipeInfo, OperationType> callback, bool isEdit);
    }
}
