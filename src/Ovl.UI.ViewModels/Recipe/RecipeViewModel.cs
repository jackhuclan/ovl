using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Ovl.Biz.Services.Measure;
using Ovl.Biz.Services.Recipe;
using Ovl.UI.ViewModels.Dialogs;
using Ovl.UI.ViewModels.Navigation;

namespace Ovl.UI.ViewModels.Recipe;

/// <summary>配方菜单树节点（对应原 InteVega.CBB.Recipe.Ui MenuTreeModel 的绑定面子集）。</summary>
public sealed partial class RecipeMenuNode : ObservableObject
{
    public RecipeMenuNode(string id, string name, int level, bool isFolder)
    {
        Id = id;
        Name = name;
        Level = level;
        IsFolder = isFolder;
    }

    public string Id { get; }

    public string Name { get; private set; }

    public int Level { get; }

    /// <summary>是否目录节点。</summary>
    public bool IsFolder { get; }

    [ObservableProperty]
    private bool _isExpanded = true;

    [ObservableProperty]
    private bool _isSelected;

    /// <summary>展开/收起按钮文本（与原版 TreeExpandButtonText 绑定一致）。</summary>
    public string TreeExpandButtonText => IsExpanded ? "−" : "+";

    public ObservableCollection<RecipeMenuNode> Children { get; } = [];

    partial void OnIsExpandedChanged(bool value) => OnPropertyChanged(nameof(TreeExpandButtonText));

    /// <summary>按关键字过滤子树；返回本节点是否保留。</summary>
    internal bool Filter(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            foreach (var child in Children)
            {
                child.Filter(string.Empty);
            }

            return true;
        }

        var selfMatch = Name.Contains(keyword, StringComparison.OrdinalIgnoreCase);
        var anyChildMatch = false;
        foreach (var child in Children)
        {
            if (child.Filter(keyword))
            {
                anyChildMatch = true;
            }
        }

        IsExpanded = !string.IsNullOrWhiteSpace(keyword);
        return selfMatch || anyChildMatch;
    }
}

/// <summary>
/// Recipe 页面 ViewModel（对应原 Marvel.Main.Recipe.ViewModel.RecipeWindowViewModel +
/// InteVega.CBB.Recipe.Ui RecipeMenuViewModel 的绑定面）。
///
/// 迁移策略：原版 RecipeWindow 为弹窗（ShowDialog），按需求改为 Sidebar 右侧内嵌页面；
/// 布局沿用原版两栏结构（左配方树 + 右内容区，可折叠）。深度依赖厂内配方服务器
/// 与 HalconDotNet 的 Test_Train*/AlignmentView* 系列（约 1.2 万行）留待 M4b，
/// 其入口在内容区页签中先行占位。
/// </summary>
public sealed partial class RecipeViewModel : ObservableObject, IPageViewModel
{
    private readonly IRecipeTreeService _treeService;
    private readonly IRecipeManager _recipeManager;
    private readonly IDialogService _dialogService;
    private readonly ILogger<RecipeViewModel>? _logger;

    public RecipeViewModel(
        IRecipeTreeService treeService,
        IRecipeManager recipeManager,
        IDialogService dialogService,
        ILogger<RecipeViewModel>? logger = null)
    {
        _treeService = treeService;
        _recipeManager = recipeManager;
        _dialogService = dialogService;
        _logger = logger;
        LoadTree();
        RecipeName = _recipeManager.CurrentRecipeName;
    }

    /// <summary>页面标题（导航服务写入）。</summary>
    public string Title { get; set; } = "Recipe";

    /// <summary>配方菜单树。</summary>
    public ObservableCollection<RecipeMenuNode> MenuTree { get; } = [];

    [ObservableProperty]
    private RecipeMenuNode? _selectedNode;

    [ObservableProperty]
    private string _searchRecipeName = string.Empty;

    [ObservableProperty]
    private bool _isShowSearchImg = true;

    [ObservableProperty]
    private string _treeExpandButtonText = "−";

    /// <summary>左栏宽度（与原版 Width 绑定一致，支持折叠）。</summary>
    [ObservableProperty]
    private GridLength _width = new(260, GridUnitType.Pixel);

    /// <summary>是否允许本地操作（原版由 EAP 远程状态控制）。</summary>
    [ObservableProperty]
    private bool _isLocalOperationAllowed = true;

    // ── Basic information summary（与原版 RecipeBaseView 绑定一致） ──

    [ObservableProperty]
    private string _recipeName = string.Empty;

    [ObservableProperty]
    private string _createUser = "demo";

    [ObservableProperty]
    private DateTime _createDate = DateTime.Today.AddDays(-30);

    [ObservableProperty]
    private string _updateUser = "demo";

    [ObservableProperty]
    private DateTime _updateDate = DateTime.Today;

    [ObservableProperty]
    private ImageSource? _imgSouce;

    [RelayCommand]
    private void Loaded()
    {
        _logger?.LogDebug("Recipe 页面已加载");
    }

    [RelayCommand]
    private void SelectItem(RecipeMenuNode? node)
    {
        if (node is null)
        {
            return;
        }

        SelectedNode = node;
        if (!node.IsFolder)
        {
            RecipeName = node.Name;
        }
    }

    [RelayCommand]
    private void ToggleTreeExpand()
    {
        var target = !(SelectedNode?.IsExpanded ?? true);
        foreach (var root in MenuTree)
        {
            SetExpanded(root, target);
        }

        TreeExpandButtonText = target ? "−" : "+";
        OnPropertyChanged(nameof(TreeExpandButtonText));
    }

    [RelayCommand]
    private void SearchRecipe()
    {
        foreach (var root in MenuTree)
        {
            root.Filter(SearchRecipeName);
        }
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchRecipeName = string.Empty;
        SearchRecipeCommand.Execute(null);
    }

    [RelayCommand]
    private void MouseDown()
    {
        // 折叠/展开左栏（原版 imgMenuStrip 收起按钮）
        Width = Width.Value <= 1 ? new GridLength(260, GridUnitType.Pixel) : new GridLength(0);
    }

    [RelayCommand]
    private void OpenRecipe()
    {
        if (SelectedNode is null || SelectedNode.IsFolder)
        {
            _dialogService.Warn("请先选择一个配方。");
            return;
        }

        _recipeManager.LoadAsync(SelectedNode.Name).ConfigureAwait(true);
        RecipeName = SelectedNode.Name;
        _logger?.LogInformation("打开配方（桩）：{Recipe}", SelectedNode.Name);
    }

    private void LoadTree()
    {
        MenuTree.Clear();
        foreach (var node in _treeService.GetTree())
        {
            MenuTree.Add(MapNode(node));
        }
    }

    private RecipeMenuNode MapNode(RecipeTreeNode source)
    {
        var node = new RecipeMenuNode(source.Id, source.Name, source.Level, source.Type == RecipeNodeType.Folder);
        foreach (var child in source.Children)
        {
            node.Children.Add(MapNode(child));
        }

        return node;
    }

    private static void SetExpanded(RecipeMenuNode node, bool expanded)
    {
        node.IsExpanded = expanded;
        foreach (var child in node.Children)
        {
            SetExpanded(child, expanded);
        }
    }
}
