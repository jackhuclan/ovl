
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InteVega.CBB.CommonLib.Helpers;
//using InteVega.CBB.Interface.Core;
using InteVega.CBB.Prism.UI.Extensions.MessageBox;
using InteVega.CBB.Recipe.Core;
using InteVega.CBB.Recipe.Interface;
using InteVega.CBB.User.Interface;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace InteVega.CBB.Recipe.Ui.ViewModel
{
    public partial class AddRecipeViewModel : ObservableObject, IDialogAware, INotifyPropertyChanged
    {
        [ObservableProperty]
        public string _windowTitle;
        [ObservableProperty]
        public RecipeInfo _recipeView;
        [ObservableProperty]
        public string _recipeName;
        [ObservableProperty]
        public OperationType _oPType;
        [ObservableProperty]
        public string _parentName;
        [ObservableProperty]
        public string _textBlockTitle;
        public string ParentId { get; set; }

        public int Level { get; set; }
        public Action<RecipeInfo, OperationType> RefreshData;
        IRecipeService _recipeService;
        IMessageBoxService _MessageBoxService;

        /// <summary>本次保存操作真实创建出的配方名称；为空表示未创建（用户放弃 / 其他操作类型）。</summary>
        private string _createdRecipeName;

        public AddRecipeViewModel(IRecipeService recipeService,
            IMessageBoxService MessageBoxService)
        {
            _recipeService = recipeService;
            _MessageBoxService = MessageBoxService;
        }

        [RelayCommand]
        public void SaveRecipe()
        {
            if (string.IsNullOrWhiteSpace(RecipeName))
            {
                _MessageBoxService.ErrorDialog($"Please enter the {_textBlockTitle.ToLower()}！");
                return;
            }
            switch (OPType)
            {
                case OperationType.Add:
                    if (!AddRecipe()) return;
                    break;
                case OperationType.Update:
                    if (!UpdateRecipe()) return;
                    break;
                case OperationType.Delete:
                    break;
                case OperationType.Paste:
                    if (!CopeRecipe()) return;
                    break;
                default:
                    break;
            }
            //OperationType state = OperationType.Add;
            //if (OPType==OperationType.Update)
            //{

            //}
            //else if (OPType == OperationType.Add)
            //{

            //}
            // 记录本次保存是否真的创建了配方：OnDialogClosed 据此判断是"创建成功"还是"用户放弃"
            if (OPType == OperationType.Add)
                _createdRecipeName = RecipeName;

            RaiseRequestClose(new DialogResult());
            RefreshData?.Invoke(RecipeView ?? new RecipeInfo(), OPType);

            // 树已刷新、配方已打开，此时再唤醒等待中的 AI 工具（open_recipe_dialog）
            if (!string.IsNullOrEmpty(_createdRecipeName))
                RecipeCreationWaiter.Complete(_createdRecipeName);
        }
        public bool UpdateRecipe()
        {
            bool result = false;
            List<RecipeInfo> recipeInfos = _recipeService.GetRecipeAll();
            if (RecipeView != null && RecipeView.IsUse)
            {
                _MessageBoxService.ErrorDialog("The current recipe is in use and cannot be modified！");
                return false;
            }
            if (recipeInfos?.Any(a => a.RecipeName == RecipeName) ?? false)
            {
                if (recipeInfos.Any(a => a.RecipeName == RecipeName && a.Id != RecipeView?.Id && a.ParentId == ParentId))
                {
                    _MessageBoxService.ErrorDialog("The same recipe name already exists！");
                    return false;
                }
            }


            if (RecipeName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                _MessageBoxService.ErrorDialog("The path contains special characters！");
                return false;
            }
            if (RecipeView != null)
            {

                RecipeView.RecipeName = RecipeName ?? "";
                if (RecipeView.Level == 0)//代表这个Recipe是文件夹
                {
                    List<RecipeInfo> a = recipeInfos?.Where(recipe => string.Equals(recipe.ParentId, RecipeView.ParentId, StringComparison.Ordinal))?.ToList() ?? new List<RecipeInfo>();
                    foreach (var item in a)
                    {
                        if (item.RecipeName == RecipeView.RecipeName)
                        {
                            _MessageBoxService.ErrorDialog("The path contains Sameple Folder！");
                            return false;
                        }
                    }
                }
                else//代表这个是文件
                {
                    List<RecipeInfo> a = recipeInfos?.Where(recipe => string.Equals(recipe.ParentId, RecipeView.ParentId, StringComparison.Ordinal))?.ToList() ?? new List<RecipeInfo>();
                    foreach (var item in a)
                    {
                        if (item.RecipeName == RecipeView.RecipeName)
                        {
                            _MessageBoxService.ErrorDialog("The path contains Sameple  Recipe！");
                            return false;
                        }
                    }
                }
                RecipeInfo recipeInfo = recipeInfos?.FirstOrDefault(f => f.Id == RecipeView.Id) ?? new RecipeInfo();
                var oldname = recipeInfo.RecipeName;
                recipeInfo.RecipeOldName = oldname;
                recipeInfo.RecipeName = RecipeView?.RecipeName ?? "";
                recipeInfo.UpdateUser = FrmPublic.CurrentLoginUser?.Name;//  "admin";
                recipeInfo.UpdateDate = DateTime.Now;
                LogHelper.LogInfo($"Recipe Operate: Renamed by {FrmPublic.CurrentLoginUser?.Name},old name={oldname},new name={recipeInfo.RecipeName}");
                try
                {
                    if (_recipeService.EditRecipe(recipeInfos, recipeInfo))
                    {
                        _MessageBoxService.Success("Modified recipe successfully！", "RecipeMessage");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _MessageBoxService.Error(ex.Message, "RecipeMessage");
                    LogHelper.LogError(ex);
                }
            }
            _MessageBoxService.ErrorDialog("Failed to save data！");
            return false;
        }
        public bool AddRecipe()
        {
            bool result = false;
            List<RecipeInfo> recipeInfos = _recipeService.GetRecipeAll();
            if (recipeInfos?.Any(a => a.RecipeName == RecipeName && a.ParentId == ParentId && a.Level == Level) ?? false)
            {
                _MessageBoxService.ErrorDialog("The same recipe name already exists！");
                return false;
            }
            if (RecipeName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                _MessageBoxService.ErrorDialog("The path contains special characters！");
                return false;
            }
            if (RecipeView != null)
            {
                RecipeView.ParentId = ParentId;
                RecipeView.ParentName = ParentName;
            }
            else
                RecipeView = new RecipeInfo();
            RecipeView.Id = Guid.NewGuid().ToString();
            RecipeView.RecipeName = RecipeName;
            RecipeView.IsUse = false;
            RecipeView.CreateDate = DateTime.Now;
            RecipeView.UpdateDate = DateTime.Now;
            RecipeView.UpdateUser = FrmPublic.CurrentLoginUser?.Name;
            if (_recipeService.AddRecipe(RecipeView))
            {
                LogHelper.LogInfo($"Recipe Operate: Add by {FrmPublic.CurrentLoginUser?.Name},RecipeName={RecipeView.RecipeName}");
                _MessageBoxService.Success("Add recipe successfully！", "RecipeMessage");
                result = true;
            }
            else
                _MessageBoxService.ErrorDialog("Failed to save data！");
            return result;
        }

        public bool CopeRecipe()
        {
            bool result = false;
            List<RecipeInfo> recipeInfos = _recipeService.GetRecipeAll();
            if (recipeInfos?.Any(a => a.RecipeName == RecipeName && a.ParentId == ParentId) ?? false)
            {
                _MessageBoxService.ErrorDialog("The same recipe name already exists！");
                return false;
            }
            if (RecipeName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                _MessageBoxService.ErrorDialog("The path contains special characters！");
                return false;
            }
            string sourceId = RecipeView.Id;
            RecipeView.RecipeName = RecipeName;

            if (_recipeService.CopeRecipe(RecipeView, sourceId, FrmPublic.CurrentLoginUser?.Name, ParentId))
            {
                LogHelper.LogInfo($"Recipe Operate: Copyed by {FrmPublic.CurrentLoginUser?.Name},RecipeName={RecipeView.RecipeName}");
                _MessageBoxService.Success("Copy recipe successfully！", "RecipeMessage");
                result = true;
            }
            else
                _MessageBoxService.ErrorDialog("Failed to save data！");
            return result;
        }
        [RelayCommand]
        public void Close()
        {
            RaiseRequestClose(new DialogResult());
        }
        public virtual void RaiseRequestClose(IDialogResult dialogResult)
        {
            RequestClose?.Invoke(dialogResult);
        }
        public string Title { get; set; }

        public event Action<IDialogResult> RequestClose;

        public bool CanCloseDialog()
        {
            return true;
        }
        public void OnDialogClosed()
        {
            // 创建成功的分支由 SaveRecipe 在刷新配方树之后调用 Complete；这里只处理"用户放弃创建"
            if (string.IsNullOrEmpty(_createdRecipeName))
                RecipeCreationWaiter.Cancel();
        }
        public void OnDialogOpened(IDialogParameters parameters)
        {
            RecipeView = parameters.GetValue<RecipeInfo>(nameof(RecipeView));
            OPType = parameters.GetValue<OperationType>(nameof(OPType));
            RefreshData = parameters.GetValue<Action<RecipeInfo, OperationType>>(nameof(RefreshData));
            TextBlockTitle = RecipeView.Level == 0 ? "Folder name" : "Recipe name";
            if (RecipeView != null)
            {
                switch (OPType)
                {
                    case OperationType.Add:
                        ParentName = RecipeView.ParentName;
                        ParentId = RecipeView.ParentId;
                        Level = RecipeView.Level;
                        RecipeName = "";
                        WindowTitle = "Add" + (RecipeView.Level == 0 ? "Folder" : "Recipe");
                        break;
                    case OperationType.Update:
                        ParentName = RecipeView.ParentName;
                        RecipeName = RecipeView.RecipeName;
                        WindowTitle = "Edit" + (RecipeView.Level == 0 ? "Folder" : "Recipe");
                        break;
                    case OperationType.Delete:
                        break;
                    case OperationType.Paste:
                        ParentName = RecipeView.ParentName;
                        ParentId = RecipeView.ParentId;
                        Level = RecipeView.Level;
                        RecipeName = RecipeView.RecipeName;
                        WindowTitle = "Copy" + (RecipeView.Level == 0 ? "Folder" : "Recipe");
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
