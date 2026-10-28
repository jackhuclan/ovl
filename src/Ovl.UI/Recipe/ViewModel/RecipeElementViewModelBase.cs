using Bases = Ovl.UI.ViewModels.Bases;
using CommunityToolkit.Mvvm.ComponentModel;
using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.Recipe;
using InteVega.CBB.Recipe.Core;
using InteVega.CBB.Recipe.Interface;
using Marvel.DataModels.Recipe;
using Marvel.DB.Interface;
using Microsoft.Extensions.DependencyInjection;
using System;
using Marvel.Application.Extentions;
using InteVega.CBB.Prism.UI.Extensions.MessageBox;
using InteVega.CBB.User.Interface;

using Ovl.UI.ViewModels.Interface;
namespace Ovl.UI.Recipe.ViewModel
{
    /// <summary>
    /// 配方界面ViewModel基类
    /// </summary>
    public partial class RecipeElementViewModelBase : Bases.ViewModelBase, IBaseRecipePage
    {
        protected readonly IServiceProvider _serviceProvider;
        public RecipeElementViewModelBase(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _recipeDal = serviceProvider.GetService<IRecipeServiceDAL>();
            _messageBoxService = serviceProvider.GetService<IMessageBoxService>();
        }
        IMessageBoxService _messageBoxService;

        IRecipeServiceDAL _recipeDal;
        /// <summary>
        /// 配方数据管理对象，负责配方文件的读写
        /// </summary>
        IRecipe _recipe;

        RecipeInfo _recipeInfo;
        /// <summary>
        /// 配方信息对象
        /// </summary>
        protected RecipeElementsInfo _recipeElementsInfo;
        /// <summary>
        /// 获取当前配方信息对象
        /// </summary>
        public RecipeElementsInfo RecipeElementsInfo
        {
            get
            { return _recipeElementsInfo; }
        }

        [ObservableProperty]
        bool _isEnable = true;
        /// <summary>
        /// 请求配方数据
        /// </summary>
        /// <param name="data">配方数据管理对象</param>
        /// <param name="recipe">配方基础信息</param>
        public virtual void RequestData(IRecipe data, RecipeElementsInfo recipeElements, RecipeInfo recipeInfo)
        {
            _recipe = data;
            _recipeInfo = recipeInfo;
            _recipeElementsInfo = recipeElements;
        }


        /// <summary>
        /// 保存配方到文件，并且更新修改时间
        /// </summary>
        public virtual void SaveRecipeFile()
        {
            var result = _recipeElementsInfo.CheckTisParams(prod: _serviceProvider);
            if (!result.ret)
            {
                _messageBoxService.ErrorDialog(result.msgs);
                return;
            }

            _recipeInfo.UpdateUser = FrmPublic.CurrentLoginUser?.Name;
            _recipeInfo.UpdateDate = DateTime.Now;
            _recipeElementsInfo.DataVersion = typeof(RecipeElementsInfo).Assembly.GetName().Version.ToString();

            _recipe.Set(_recipeElementsInfo, true);
            _recipeDal.ModifyRecipe(_recipeInfo);
            LogHelper.LogInfo($"Recipe Operate: Saved by {FrmPublic.CurrentLoginUser?.Name},RecipeName={_recipeInfo.RecipeName} At RecipeElementViewModelBase");
            //  _measureDataDBService.InsertOrUpdateTestInfo(_recipeElementsInfo);
        }

    }
}
