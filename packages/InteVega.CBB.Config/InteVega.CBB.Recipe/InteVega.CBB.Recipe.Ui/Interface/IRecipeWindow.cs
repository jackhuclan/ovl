using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InteVega.CBB.Common;
using InteVega.CBB.Recipe.Core;

namespace InteVega.CBB.Recipe.Ui.Interface
{
    public interface IRecipeWindow
    {
        /// <summary>
        /// 弹窗
        /// </summary>
        /// <param name="recipeInfo">添加，修改，粘贴父级目录</param>
        /// <param name="callback">回调刷新</param>
        /// <param name="operationType"></param>
        /// <param name="PasteRecipeInfo">粘贴的配方</param>
        public void OpenWindow(RecipeInfo recipeInfo, Action<RecipeInfo, OperationType> callback, OperationType operationType);
        /// <summary>
        /// 导入弹窗
        /// </summary>
        /// <param name="recipeInfo">添加，修改，粘贴父级目录</param>
        /// <param name="callback">回调刷新</param>
        /// <param name="operationType"></param>
        /// <param name="PasteRecipeInfo">粘贴的配方</param>
        public void OpenImportWindow(RecipeInfo recipeInfo, string filePath, Action<RecipeInfo> callback);
    }
}
