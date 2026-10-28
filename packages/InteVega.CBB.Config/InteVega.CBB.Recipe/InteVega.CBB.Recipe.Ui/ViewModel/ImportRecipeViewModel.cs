using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.Prism.UI.Extensions.MessageBox;
using InteVega.CBB.Recipe.Core;
using InteVega.CBB.Recipe.Interface;
using InteVega.CBB.Recipe.Ui.Interface;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InteVega.CBB.Recipe.Ui.ViewModel
{
    public partial class ImportRecipeViewModel : ObservableObject, IDialogAware, INotifyPropertyChanged
    {
        public RecipeInfo _recipeInfo;
        public string _filePath;
        public Action<RecipeInfo> _refreshData;
        [ObservableProperty]
        ObservableCollection<ImportRecipeInfo> _recipeList;
        IRecipeService _recipeService;
        IMessageBoxService _messageBoxService;
        List<RecipeInfo> _targetRecipes;
        public ImportRecipeViewModel(IRecipeService recipeService, IMessageBoxService messageBoxService)
        {
            _recipeService = recipeService;
            _messageBoxService= messageBoxService;
            
        }
        public void LoadRecipe()
        {
            DirectoryInfo folder = new DirectoryInfo(_filePath);
            FileInfo[] fileList = folder.GetFiles();        
            FileInfo fileInfo=fileList.ToList().FirstOrDefault(f => f.FullName.ToLower().Contains("recipe.json"));
            _targetRecipes = JsonHelper.DeserializeFile <List<RecipeInfo>>(fileInfo.FullName);
            List<RecipeInfo> sourceRecipeList = _recipeService.GetRecipeAll();
            //删除存在重复的文件夹
            foreach (var item in sourceRecipeList.Where(_ => _.Level == 0))
            {
                RecipeInfo remove = _targetRecipes.FirstOrDefault(_ => _.Id == item.Id && _.Level == 0);
                _targetRecipes.Remove(remove);
            }
            RecipeList = new ObservableCollection<ImportRecipeInfo>();
            int num= 1;
            foreach (RecipeInfo Recipe in sourceRecipeList.Where(_ => _targetRecipes.Any(t => t.Id == _.Id) && _.Level==1))
            {
                RecipeList.Add(new ImportRecipeInfo
                {
                    Index = num,
                    TargetRecipeInfo = _targetRecipes.FirstOrDefault(r => r.Id == Recipe.Id),
                    IsTargetCheck = false,
                    SourceRecipeInfo = Recipe,
                    IsSourceCheck=false
                }) ;
                num++;
            }
        }
        [RelayCommand]
        public void ImportRecipe() 
        {
            foreach (ImportRecipeInfo item in RecipeList)
            {
                //导入新的
                if (item.IsSourceCheck == item.IsTargetCheck && item.IsSourceCheck)
                {
                    //重新指定ID
                    //item.TargetRecipeInfo.Id;
                    item.TargetRecipeInfo.NewId=Guid.NewGuid().ToString();
                }
                //导入并删除
                else if (!item.IsSourceCheck && item.IsTargetCheck)
                {
                    _recipeService.RemoveRecipe(item.SourceRecipeInfo);
                }
                else
                {
                    _targetRecipes.Remove(item.TargetRecipeInfo);
                }
            }
            bool result= _recipeService.ImportRecipe(_recipeInfo, _targetRecipes, _filePath);
            if (result)
            {
                RaiseRequestClose(new DialogResult());
            }
            else 
            {
                _messageBoxService.ErrorDialog("Import failed！");
            }
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

        }
        public void OnDialogOpened(IDialogParameters parameters)
        {
            _recipeInfo = parameters.GetValue<RecipeInfo>(nameof(_recipeInfo));
            _filePath = parameters.GetValue<string>(nameof(_filePath));
            _refreshData = parameters.GetValue<Action<RecipeInfo>>(nameof(_refreshData));
            Title = "Recipe import conflict occurred";
            LoadRecipe();
        }
    }

    public partial class ImportRecipeInfo :ObservableObject
    {
        [ObservableProperty]
        public int _index;             
        [ObservableProperty]
        public RecipeInfo _sourceRecipeInfo;
        [ObservableProperty]
        public bool _isSourceCheck;
        [ObservableProperty]
        public RecipeInfo _targetRecipeInfo;
        [ObservableProperty]
        public bool _isTargetCheck;
    }
}
