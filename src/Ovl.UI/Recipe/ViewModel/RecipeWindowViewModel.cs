using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.ComUI.Event;
using InteVega.CBB.ComUI.Helpers;
using InteVega.CBB.DomainService.Api.Interfaces;
using InteVega.CBB.DomainService.Api.Models;
using InteVega.CBB.Prism.UI.Extensions.MessageBox;
using InteVega.CBB.Recipe.Core;
using InteVega.CBB.Recipe.Interface;
using InteVega.CBB.Recipe.Ui.ViewModel;
using InteVega.CBB.User.Interface;
using Marvel.Application.Services.Pm;
using Marvel.DataModels.Core;
using Marvel.DataModels.Recipe;
using Marvel.Interfaces.Application;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

using Ovl.UI.Recipe.View;
using Ovl.UI.ViewModels.Bases;
using Ovl.UI.ViewModels.Interface;
namespace Ovl.UI.Recipe.ViewModel;

public partial class RecipeWindowViewModel : ObservableObject, IDisposable
{
    [ObservableProperty]
    string _recipeName;
    [ObservableProperty]
    RecipeMenuViewModel _recipeMenuViewModel;
    [ObservableProperty]
    ObservableCollection<TabItem> _tabItemView;
    [ObservableProperty]
    int _selectIndex;
    [ObservableProperty]
    ObservableCollection<MenuNode> _tabItemDataList;
    [ObservableProperty]
    int _width;
    [ObservableProperty]
    public string _imgSouce;
    [ObservableProperty]
    Visibility _isShowContent = Visibility.Collapsed;
    [ObservableProperty]
    bool _isLocalOperationAllowed = true;
    List<MenuNode> _recipeNodeMenuList;
    IRecipe _recipe;
    RecipeInfo _recipeInfo;
    Frame _currentFrame;
    IMessageBoxService _messageBoxService;
    IAlignmentService _alignmentService;
    private readonly IPmAutoTriggerService? _pmAutoTriggerService;
    private readonly Dispatcher _dispatcher;
    private readonly RecipeProofJobEvent _recipeProofJobEvent;
    public static Task CopyOldRecipes;
    private readonly IRecipeService _recipeService;
    private readonly IEapControlService? _eapControlService;
    private readonly Dispatcher _uiDispatcher;

    public RecipeWindowViewModel(IServiceProvider serviceProvider,
        RecipeMenuViewModel recipeMenuViewModel)
    {
        _recipeService = serviceProvider.GetRequiredService<IRecipeService>();
        _eapControlService = serviceProvider.GetService<IEapControlService>();
        _dispatcher = Dispatcher.CurrentDispatcher;
        _alignmentService = serviceProvider.GetRequiredService<IAlignmentService>();
        _messageBoxService = serviceProvider.GetRequiredService<IMessageBoxService>();
        _pmAutoTriggerService = serviceProvider.GetService<IPmAutoTriggerService>();
        _uiDispatcher = Dispatcher.CurrentDispatcher;

        TabItemView = new ObservableCollection<TabItem>();
        RecipeMenuViewModel = recipeMenuViewModel;
        recipeMenuViewModel.UserInfo = FrmPublic.CurrentLoginUser;
        InteVega.CBB.ComUI.Helpers.ProgressDialogHelper.ShowDialog("Copy old recipes to new path !!!please later...", () =>
        {
            if (CopyOldRecipes == null)
            {
                CopyOldRecipes = new Task(() =>
                {
                    RecipeMenuViewModel.OldPathCopyToNewPath();
                });
                CopyOldRecipes.Start();
            }
            CopyOldRecipes.Wait();
            return true;
        });
        RecipeMenuViewModel.OutSideLoadMenuTree();
        RecipeMenuViewModel.GetRecipe = (node, recipe) => OpenRecipe(node, recipe);
        var recipeExcelContentConverter = new RecipeExcelContentConverter(serviceProvider);

        recipeMenuViewModel.GetRecipeContentByID = recipeExcelContentConverter.GetRecipeContentsById;
        recipeMenuViewModel.ImportRecipeFromExcelEvent = recipeExcelContentConverter.ImportRecipeFromExcelContents;
        recipeMenuViewModel.CopyRecipeEvent = r => SaveCopyRecipe(r);
        recipeMenuViewModel.DeletedRecipeEvent += (recipe) =>
        {
            if (recipe?.Id == _recipeInfo?.Id)
                ShowNoRecipe();
        };
        Width = 350;
        ImgSouce = "pack://application:,,,/InteVega.CBB.Recipe.Ui;Component/Resources/Images/icon_ceblsq@2x.png";

        TabItemView.Clear();
        TabItem _tabItem = new TabItem();
        Border border = new Border();
        TextBlock textBlock = new TextBlock();
        textBlock.Text = "Please select the recipe!";//请选择配方
        _tabItem.Content = textBlock;
        _tabItem.Visibility = Visibility.Collapsed;
        SelectIndex = 0;
        TabItemView.Add(_tabItem);

        if (_eapControlService != null)
        {
            _eapControlService.StateChanged += OnEapControlStateChanged;
            ApplyEapSnapshot(_eapControlService.Snapshot);
        }



    }

    private void OnEapControlStateChanged(object? sender, EapControlSnapshot snapshot)
    {
        _dispatcher.BeginInvoke(() => ApplyEapSnapshot(snapshot));
    }

    private void ApplyEapSnapshot(EapControlSnapshot snapshot)
    {
        IsLocalOperationAllowed = !snapshot.IsRemote;
    }

    private void ShowNoRecipe()
    {
        IsShowContent = Visibility.Collapsed;
        TabItemDataList?.Clear();
        RecipeName = string.Empty;
    }
    bool IsSaveRecipe = false;
    public void SaveCopyRecipe(RecipeInfo recipeInfo)
    {
        if (_eapControlService?.Snapshot.IsRemote == true)
        {
            _messageBoxService.WarningDialog(
                "Recipe editing is unavailable while EAP is in Remote mode.");
            return;
        }

        IsSaveRecipe = true;
    }
    public void OpenRecipe(IRecipeManager recipeNodeService, RecipeInfo recipeInfo)
    {
        ProgressDialogHelper.ShowDialog("Loading recipe...", () =>
        {
            try
            {
                if (_recipeService.IsRecipeConfigMissing(recipeInfo.Id))
                {
                    _dispatcher.Invoke(() =>
                    {
                        var result = _messageBoxService.AskDialog("The selected recipe configuration file does not exist. Do you want to delete this recipe information?");
                        if (result == Prism.Services.Dialogs.ButtonResult.OK)
                        {
                            _recipeService.RemoveInvalidRecipeById(recipeInfo.Id);
                            RecipeMenuViewModel.OutSideLoadMenuTree();
                        }

                        ShowNoRecipe();
                    });

                    return true;
                }
                _recipe = recipeNodeService.GetCurrentRecipe(recipeInfo.Id);

                _recipeInfo = recipeInfo;
                if (string.IsNullOrWhiteSpace(_recipeInfo.CreateUser))
                {
                    _recipeInfo.CreateUser = _recipeInfo.UpdateUser;
                    _recipeInfo.CreateDate = _recipeInfo.UpdateDate;
                }


                var _recipeElementsInfo = _recipe.Get<RecipeElementsInfo>();
                if (_recipeElementsInfo == null)
                {
                    _recipeElementsInfo = new RecipeElementsInfo() { Id = _recipeInfo.Id };
                }
                else
                {
                    _recipeElementsInfo.Id = _recipeInfo.Id;
                }
                _recipeElementsInfo.Name = _recipeInfo.RecipeName;
                ExecutePmCheckForRecipeOpen(_recipeElementsInfo);
                _alignmentService.Init(_recipeElementsInfo);
                _dispatcher.Invoke(() =>
                {
                    RecipeName = recipeInfo?.RecipeName;
                    LoadRecipeMenu(recipeInfo.Id);
                    BindRecipeMenu(recipeInfo?.Id);
                    LoadUserContral(TabItemDataList.FirstOrDefault(f => f.IsSelect)?.Path);
                });
            }
            catch (Exception ex)
            {
                string err = "Failed to open recipe! Please make sure that the file exists and that the file format is not corrupted";
                LogHelper.LogError(err);
                LogHelper.LogError(ex);
                try
                {
                    string recipeFile = Path.Combine(@"..\..\data\recipes", recipeInfo.RecipeName + "##" + recipeInfo.Id, recipeInfo.Id + ".json");
                    string jsonstr = File.ReadAllText(recipeFile);

                    var recipeJsonInfo = JsonConvert.DeserializeObject<RecipeElementsInfo>(jsonstr);
                }
                catch (Exception e)
                {
                    _messageBoxService.ErrorDialog(err);
                    LogHelper.LogError(e);
                    return false;
                }

                _dispatcher.Invoke(() =>
                {
                    ShowNoRecipe();
                    _messageBoxService.ErrorDialog(err);
                });

            }
            return true;
        });
    }

    private void ExecutePmCheckForRecipeOpen(RecipeElementsInfo recipe)
    {
        try
        {
            if (_pmAutoTriggerService == null)
            {
                LogHelper.LogError("IPmAutoTriggerService is not registered, skipping recipe-open PM check.");
                return;
            }

            if (_pmAutoTriggerService.ShouldExecutePmForRecipe(recipe))
            {
                LogHelper.LogInfo("Recipe-open PM check: PM execution required.");
                _pmAutoTriggerService.ExecutePmForRecipe(recipe);
                LogHelper.LogInfo("Recipe-open PM check: PM execution completed.");
            }
            else
            {
                LogHelper.LogInfo("Recipe-open PM check: PM execution not required.");
            }
        }
        catch (Exception ex)
        {
            LogHelper.LogError(ex, "Error occurred during recipe-open PM check.");
        }
    }

    [RelayCommand]
    public void MouseDown()
    {
        SwitchRecipeView(Width != 0);
    }
    void SwitchRecipeView(bool hide)
    {
        if (hide)
        {
            Width = 0;
            ImgSouce = "pack://application:,,,/InteVega.CBB.Recipe.Ui;Component/Resources/Images/icon_ceblzk@2x.png";
        }
        else
        {
            Width = 350;
            ImgSouce = "pack://application:,,,/InteVega.CBB.Recipe.Ui;Component/Resources/Images/icon_ceblsq@2x.png";
        }
    }
    [RelayCommand]
    public void OpenTabItem(string id)
    {
        //是否隐藏左侧的树形配方结构
        bool isHideRecipeTree = true;
        foreach (var item in TabItemDataList)
        {
            item.IsSelect = item.Id == id ? true : false;
            if (item.IsSelect)
            {
                LoadUserContral(item.Path);
                if (item.Path.Contains(nameof(SummaryView)))
                {
                    isHideRecipeTree = false;
                }

            }
        }
        SwitchRecipeView(isHideRecipeTree);
    }
    public void LoadUserContral(string path)
    {
        IsShowContent = Visibility.Visible;
        TabItemView.Clear();

        if (_currentFrame != null)
        {
            _currentFrame.LoadCompleted -= Frame_LoadCompleted;
            _currentFrame = null;
        }

        if (!string.IsNullOrWhiteSpace(path))
        {
            TabItem _tabItem = new TabItem();
            Frame frame = new Frame() { Source = new Uri(path, UriKind.RelativeOrAbsolute) };
            frame.LoadCompleted += Frame_LoadCompleted;
            _currentFrame = frame;
            _tabItem.Content = frame;
            _tabItem.Visibility = Visibility.Collapsed;
            SelectIndex = 0;
            TabItemView.Add(_tabItem);
        }
    }
    private void Frame_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
    {
        var dataContext = ((FrameworkElement)e.Content).DataContext as IBaseRecipePage;
        if (dataContext != null)
        {
            dataContext.RequestData(_recipe, _alignmentService.RecipeElementsInfo, _recipeInfo);
            if (IsSaveRecipe)
            {
                dataContext.SaveRecipeFile();
                IsSaveRecipe = false;
            }
        }
    }
    public void LoadRecipeMenu(string recipeId)
    {

        _recipeNodeMenuList = new List<MenuNode>();

        _recipeNodeMenuList.Add(NewNode("Summary", "SummaryView", recipeId, true));
        _recipeNodeMenuList.Add(NewNode("Wafer Map", "WaferMapView", recipeId));
        _recipeNodeMenuList.Add(NewNode("Alignment", MarvelVersion.DicViews[EnumOMVersionViews.AlignmentView], recipeId));
        _recipeNodeMenuList.Add(NewNode("Tests", "TestManageView", recipeId));
        _recipeNodeMenuList.Add(NewNode("Recipe-Run Parameters", "RunParamView", recipeId));

        //_recipeNodeMenuList.Add(new RecipeNodeMenu { Id = Guid.NewGuid().ToString(), Name = "摘要", Path = "pack://application:,,,/Ovl.UI;component/Recipe/View/SummaryView.xaml", RecipeId = "adf59abc-6c29-4143-8594-4bd5264a660b", IsSelect = true });
        //_recipeNodeMenuList.Add(new RecipeNodeMenu { Id = Guid.NewGuid().ToString(), Name = "Stage Contral", Path = "pack://application:,,,/Ovl.UI;component/Recipe/View/WaferMapView.xaml", RecipeId = "adf59abc-6c29-4143-8594-4bd5264a660b", IsSelect = false });

    }

    private MenuNode NewNode(string name, string view, string recipeId, bool isSelect = false)
    {
        return new MenuNode
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Path = $"pack://application:,,,/Ovl.UI;component/Recipe/View/{view}.xaml",
            RecipeId = recipeId,
            IsSelect = isSelect
        };
    }

    public void BindRecipeMenu(string id)
    {
        TabItemDataList = new ObservableCollection<MenuNode>();
        foreach (var item in _recipeNodeMenuList.Where(r => r.RecipeId == id).ToList())
        {
            TabItemDataList.Add(new MenuNode
            {
                Id = item.Id,
                Name = item.Name,
                Path = item.Path,
                RecipeId = item.RecipeId,
                IsSelect = item.IsSelect
            });
        }
    }

    public void Dispose()
    {
        if (_eapControlService != null)
            _eapControlService.StateChanged -= OnEapControlStateChanged;

        if (_currentFrame != null)
        {
            _currentFrame.LoadCompleted -= Frame_LoadCompleted;
            _currentFrame = null;
        }
    }
}




public partial class TabItemView : ObservableObject
{
    [ObservableProperty]
    public object _tabItem;
}
