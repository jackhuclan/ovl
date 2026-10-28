using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InteVega.CBB.Common;
using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.ComUI.Event;
using InteVega.CBB.Prism.UI.Extensions.MessageBox;
using InteVega.CBB.Recipe.Core;
using InteVega.CBB.Recipe.Interface;
using InteVega.CBB.Recipe.Ui.Interface;
using InteVega.CBB.User.Interface;
using Microsoft.Win32;
using Prism.Events;
using Prism.Services.Dialogs;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace InteVega.CBB.Recipe.Ui.ViewModel
{
    public partial class RecipeMenuViewModel : ObservableObject
    {
        [ObservableProperty]
        public MenuTreeModel _treeModel;
        [ObservableProperty]
        public Visibility _isShowSearchImg;
        [ObservableProperty]
        public Visibility _isShowClearImg;
        public Action<IRecipeManager, RecipeInfo> GetRecipe;
        public Action<RecipeInfo> DeletedRecipeEvent;
        public Action<RecipeInfo> CopyRecipeEvent;
        public Action<List<RecipeContent>> ImportRecipeFromExcelEvent;
        /// <summary>
        /// 通过ID获取RecipeContent的委托方法，供外部调用
        /// </summary>
        public Func<string, List<RecipeContent>> GetRecipeContentByID;
        private RecipeProofJobEvent _recipeProofJobEvent;
        [ObservableProperty]
        public string _searchRecipeName;
        [ObservableProperty]
        public string _currentSelectId;
        [ObservableProperty]
        public string _treeExpandButtonText = "Collapse";

        public Visibility IsShowContextMenu;
        public List<RecipeInfo> _recipeInfoList;
        private List<RecipeInfo> _currentTreeRecipeInfoList;
        public UserInfo UserInfo { get; set; }
        RecipeInfo _copeRecipeInfo;
        IRecipeService _recipeService;
        IRecipeManager _recipeManager;
        IMessageBoxService _MessageBoxService;
        IRecipeWindow _recipeWindow;

        public RecipeMenuViewModel(IRecipeService recipeService,
            IRecipeManager recipeManager,
            IMessageBoxService messageBoxService,
            IRecipeWindow recipeWindow,
            IEventAggregator aggregator)
        {
            _MessageBoxService = messageBoxService;
            _recipeService = recipeService;
            _recipeManager = recipeManager;
            _recipeWindow = recipeWindow;
            IsShowSearchImg = Visibility.Visible;
            IsShowClearImg = Visibility.Hidden;
            _recipeProofJobEvent = aggregator.GetEvent<RecipeProofJobEvent>();
            //List<RecipeInfo> recipeInfoList = _recipeService.GetRecipeAll()?.ToList();
            //LoadMenuTree(recipeInfoList);

            // vendor 适配：IAIToolService（Marvel.Agent.Core）相关 AI 工具注册代码已按需求移除
        }

        #region 加载菜单和子菜单
        private const int DialogOpenWaitMilliseconds = 15000;

        /// <summary>等待用户在创建配方对话框中点击 Save 的最长时间。</summary>
        private const int RecipeDialogTimeoutMilliseconds = 5 * 60 * 1000;

        /// <summary>
        /// AI 工具 open_recipe_dialog：按名称选中文件夹并打开创建配方对话框，
        /// 然后等待用户自己在对话框中填写名称并点击 Save。
        /// Prism 的 IDialogService.ShowDialog 是模态阻塞调用且涉及 WPF 窗口创建，必须回到 UI 线程执行；
        /// 工具线程不能被它阻塞，因此用 BeginInvoke 投递，再用 RecipeCreationWaiter 等用户保存结果。
        /// </summary>
        private async Task<string> openRecipeDialogByFolderName([Description("文件夹名称")] string folderName)
        {
            if (TreeModel?.Children == null)
                return "配方菜单尚未加载完成，请先打开配方管理界面后重试";

            var keyword = NormalizeKeyword(folderName);
            if (keyword.Length == 0)
                return "请明确告诉我文件夹名称是什么";

            var list = new List<MenuTreeModel>();
            SelectMenuTreeModels(TreeModel.Children, keyword, list);

            if (list.Count > 1)
                return $"找到多个文件夹，请告诉唯一的文件夹名称：{Environment.NewLine}{FormatRecipeNames(list)}";

            if (list.Count == 0)
                return "没有找到文件夹，请明确告诉我文件夹名称是什么";

            var node = list[0];
            var folder = node.RecipeInfo?.RecipeName;
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null)
                return "界面尚未就绪，无法打开创建配方对话框";

            // 先挂上等待器再打开对话框，避免用户操作过快错过结果
            var created = RecipeCreationWaiter.Begin();

            // 模态对话框会阻塞 UI 线程，这里只投递不等待
            _ = dispatcher.BeginInvoke(new Action(() =>
            {
                SelectItem(node);
                node.AddRecipe();
            }), DispatcherPriority.Background);

            // 一直等到用户在对话框中点 Save（成功）或关掉对话框（放弃）
            var finished = await Task.WhenAny(created, Task.Delay(RecipeDialogTimeoutMilliseconds))
                                     .ConfigureAwait(false);

            if (finished != created)
                return $"已在文件夹{folder}下打开创建配方对话框，但还没等到保存结果。请提醒用户在对话框中填写配方名称并点击 Save，保存完成后告知。";

            var newRecipeName = await created.ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(newRecipeName))
                return $"用户关闭了创建配方对话框，没有在文件夹{folder}下创建配方。请询问用户是否重新创建。";

            return $"已在文件夹{folder}下成功创建配方{newRecipeName}，配方已自动打开。是否继续跳转到 WaferMap 界面填写晶圆信息？";
        }

        private async Task<string> selectRecipeByName([Description("配方名称")] string recipeName)
        {
            if (TreeModel?.Children == null)
                return "配方菜单尚未加载完成，请先打开配方管理界面后重试";

            var keyword = NormalizeKeyword(recipeName);
            if (keyword.Length == 0)
                return "请明确告诉我配方名称是什么";

            var list = new List<MenuTreeModel>();
            SelectMenuTreeModels(TreeModel.Children, keyword, list);

            if (list.Count > 1)
                return $"找到多个配方，请告诉唯一的配方名称：{Environment.NewLine}{FormatRecipeNames(list)}";

            if (list.Count == 0)
                return "没有找到配方，请明确告诉我配方名称是什么";

            var node = list[0];
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null)
                return "界面尚未就绪，无法打开配方";

            _ = dispatcher.BeginInvoke(new Action(() => MouseDouble(node)), DispatcherPriority.Background);
            await Task.Delay(DialogOpenWaitMilliseconds).ConfigureAwait(false);

            return $"找到了您需要的配方{node.RecipeInfo?.RecipeName}";
        }

        private static string NormalizeKeyword(string value)
            => (value ?? string.Empty).Trim().Trim('"', '\'', '“', '”');

        private static string FormatRecipeNames(IEnumerable<MenuTreeModel> models)
            => string.Join(Environment.NewLine, models.Select(x => $"- {x.RecipeInfo?.RecipeName}"));

        private void SelectMenuTreeModels(ObservableCollection<MenuTreeModel> treeModelList, string recipeName, List<MenuTreeModel> list)
        {
            foreach (var item in treeModelList)
            {
                if (string.Compare(item.RecipeInfo.RecipeName, recipeName, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    list.Add(item);
                }

                if (item.Children.Count > 0)
                    SelectMenuTreeModels(item.Children, recipeName, list);
            }
        }

        public void OutSideLoadMenuTree()
        {
            _recipeInfoList = _recipeService.GetRecipeAll()?.ToList();
            LoadMenuTree(_recipeInfoList);
        }

        public void OldPathCopyToNewPath()
        {
            _recipeInfoList = _recipeService.GetRecipeAll()?.ToList();
            if (_recipeInfoList == null || _recipeInfoList.Count <= 1)
            {
                var analyzer = new DiskSpaceAnalyzer();

                // 示例1：分析文件夹能否拷贝到D盘
                //Console.WriteLine("=== 示例1: 分析文件夹到D盘 ===");
                var result1 = analyzer.AnalyzeCopyOperation(
                    sourceFolder: $"../../data/Recipes/",  // 源文件夹路径
                    targetDrive: "E:",                         // 目标驱动器
                    bufferSizeMB: 200                          // 缓冲区200MB
                );

                if (result1.CanCopy)
                {
                    _recipeService.OldPathCopyToNewPath();
                }
                else
                {
                    _MessageBoxService.ErrorDialog("Insufficient disk space！Copying error！");
                }
                _recipeInfoList = _recipeService.GetRecipeAll()?.ToList();
                LoadMenuTree(_recipeInfoList);
            }
        }
        private void LoadMenuTree(List<RecipeInfo> recipeInfoList)
        {

            if (recipeInfoList == null)
            {
                recipeInfoList = new List<RecipeInfo>();
                RecipeInfo recipeInfo = new RecipeInfo
                {
                    Id = Guid.NewGuid().ToString(),
                    RecipeName = "Recipes",
                    IsUse = false,
                    CreateDate = DateTime.Now,
                    CreateUser = UserInfo?.Name,
                    UpdateDate = DateTime.Now,
                    UpdateUser = UserInfo?.Name,
                };
                recipeInfoList.Add(recipeInfo);
                _recipeService.AddRecipe(recipeInfo);
            }
            if (recipeInfoList != null)
            {
                _currentTreeRecipeInfoList = recipeInfoList.ToList();
                TreeModel = new MenuTreeModel(_recipeWindow);
                foreach (var item in recipeInfoList.Where(r => string.IsNullOrWhiteSpace(r.ParentId)).OrderBy(o => o.Index))
                {
                    MenuTreeModel treeMenu = new MenuTreeModel(_recipeWindow);
                    treeMenu.Id = item.Id;
                    treeMenu.Name = item.RecipeName;
                    treeMenu.RecipeInfo = item;
                    treeMenu.Level = item.Level;
                    treeMenu.IsShowContextMenu = IsShowContextMenu;
                    treeMenu.IsSelect = CurrentSelectId == item.Id ? 1 : 0;
                    treeMenu.IsExpanded = true;
                    treeMenu.IsChildrenLoaded = true;
                    treeMenu.IsShowAddRecipe = item.Level == 0 ? Visibility.Visible : Visibility.Collapsed;
                    treeMenu.IsShowAddCopy = item.Level == 1 ? Visibility.Visible : Visibility.Collapsed;
                    treeMenu.IsShowDelete = Visibility.Collapsed;
                    treeMenu.Type = recipeInfoList.Any(r => r.ParentId == item.Id) ? OperationType.Add : OperationType.Update;
                    treeMenu.UserInfo = UserInfo;
                    treeMenu.MoveCompleteAction = result => MoveComplete(result);
                    treeMenu.CopyRecipeEvent = r => CopyRecipe(r);
                    treeMenu.PasteRecipeEvent = (r) => PasteRecipe(r);
                    treeMenu.Refresh = (r, state) => RefreshData(r, state);
                    treeMenu.CheckMove = (target, move) => CheckMove(target, move);
                    treeMenu.DeleteCallback = r => DeleteRecipte(r);
                    treeMenu.ImportRecipeEvent = (r) => ImportRecipe(r);
                    treeMenu.ExportRecipeEvent = (r) => ExportRecipe(r);
                    treeMenu.ImportRecipeFromExcelEvent = (r) => ImportRecipeFromExcel(r);
                    treeMenu.ExportRecipeToExcelEvent = (r) => ExportRecipeToExcel(r);
                    TreeModel.Children.Add(treeMenu);
                    BindChildTreeMenu(treeMenu, recipeInfoList, item.Id, 1);
                }
                RestoreCurrentSelectState();
                if (!_isTreeCollapsed)
                {
                    ExpandAllNodes();
                }
            }
        }
        private void BindChildTreeMenu(MenuTreeModel treeMode, List<RecipeInfo> recipeInfoList, string parentId, int remainingDepth = 1)
        {
            if (remainingDepth <= 0)
            {
                treeMode.LoadChildrenAction = node => LoadDirectChildren(node);
                if (recipeInfoList.Any(r => r.ParentId == parentId))
                {
                    treeMode.Children.Add(new MenuTreeModel(_recipeWindow)
                    {
                        Name = string.Empty,
                        Parent = treeMode,
                        IsPlaceholder = true,
                        IsShowContextMenu = Visibility.Collapsed
                    });
                }
                return;
            }

            if (recipeInfoList.Any(r => r.ParentId == parentId))
            {
                foreach (RecipeInfo childItem in recipeInfoList.Where(r => r.ParentId == parentId).ToList())
                {
                    MenuTreeModel childMenu = new MenuTreeModel(_recipeWindow);
                    childMenu.Id = childItem.Id;
                    childMenu.Name = childItem.RecipeName;
                    childMenu.RecipeInfo = childItem;
                    childMenu.Parent = treeMode;
                    childMenu.Level = childItem.Level;
                    childMenu.IsShowContextMenu = IsShowContextMenu;
                    childMenu.IsSelect = CurrentSelectId == childItem.Id ? 1 : 0;
                    childMenu.IsExpanded = false;
                    childMenu.IsShowAddRecipe = childItem.Level == 0 ? Visibility.Visible : Visibility.Collapsed;
                    childMenu.IsShowAddCopy = childItem.Level == 1 ? Visibility.Visible : Visibility.Collapsed;
                    childMenu.UserInfo = UserInfo;
                    childMenu.Type = recipeInfoList.Any(r => r.ParentId == childItem.Id) ? OperationType.Add : OperationType.Update;
                    childMenu.CopyRecipeEvent = r => CopyRecipe(r);
                    childMenu.PasteRecipeEvent = (r) => PasteRecipe(r);
                    childMenu.OpenRecipeEvent = (r) => OpenRecipe(r);
                    childMenu.Refresh = (r, state) => RefreshData(r, state);
                    childMenu.DeleteCallback = r => DeleteRecipte(r);
                    childMenu.MoveCompleteAction = result => MoveComplete(result);
                    childMenu.CheckMove = (target, move) => CheckMove(target, move);
                    childMenu.ImportRecipeEvent = (r) => ImportRecipe(r);
                    childMenu.ExportRecipeEvent = (r) => ExportRecipe(r);
                    childMenu.ImportRecipeFromExcelEvent = (r) => ImportRecipeFromExcel(r);
                    childMenu.ExportRecipeToExcelEvent = (r) => ExportRecipeToExcel(r);
                    treeMode.Children.Add(childMenu);
                    BindChildTreeMenu(childMenu, recipeInfoList, childItem.Id, remainingDepth - 1);
                }
            }
        }
        #endregion

        #region 查询
        /// <summary>
        /// 检索功能
        /// </summary>
        [RelayCommand]
        public void SearchRecipe(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                IsShowSearchImg = Visibility.Visible;
                IsShowClearImg = Visibility.Hidden;
                List<RecipeInfo> recipeInfos = _recipeService.GetRecipeAll()?.ToList();
                LoadMenuTree(recipeInfos);
            }
            else
            {
                IsShowSearchImg = Visibility.Hidden;
                IsShowClearImg = Visibility.Visible;
                List<RecipeInfo> dataList = _recipeService.GetRecipeAll()?.ToList();
                List<RecipeInfo> recipeInfos = dataList.Where(r => r.RecipeName.Contains(value, StringComparison.OrdinalIgnoreCase)).ToList();
                if (recipeInfos != null)
                {
                    //List<RecipeInfo> result = new List<RecipeInfo>();
                    //foreach (var item in recipeInfos)
                    //{
                    //    SearchParentRecipe(item,dataList, result);
                    //}
                    //LoadMenuTree(result);

                    // 执行搜索并构建完整的树形结果
                    List<RecipeInfo> searchResult = PerformSearch(recipeInfos, value);
                    foreach (var item in recipeInfos)
                    {
                        SearchParentRecipe(item, dataList, searchResult);
                    }
                    // 加载到界面
                    LoadMenuTree(searchResult);
                }
            }
        }
        [RelayCommand]
        public void ClearSearch()
        {
            SearchRecipeName = "";
            IsShowSearchImg = Visibility.Visible;
            IsShowClearImg = Visibility.Hidden;
            SearchRecipe("");
        }

        private bool _isTreeCollapsed;

        [RelayCommand]
        public void ToggleTreeExpand()
        {
            _isTreeCollapsed = !_isTreeCollapsed;
            TreeExpandButtonText = _isTreeCollapsed ? "Expand" : "Collapse";

            if (_isTreeCollapsed)
            {
                CollapseBelowRoot();
                return;
            }

            ExpandAllNodes();
        }

        private void ExpandAllNodes()
        {
            if (TreeModel?.Children == null)
            {
                return;
            }

            foreach (MenuTreeModel item in TreeModel.Children)
            {
                ExpandNode(item);
            }
        }

        private void ExpandNode(MenuTreeModel node)
        {
            if (node == null)
            {
                return;
            }

            LoadDirectChildren(node);

            if (node.Children.Count == 0)
            {
                return;
            }

            if (!node.IsExpanded)
            {
                node.IsExpanded = true;
            }

            foreach (MenuTreeModel child in node.Children)
            {
                ExpandNode(child);
            }
        }

        private void CollapseBelowRoot()
        {
            if (TreeModel?.Children == null)
            {
                return;
            }

            foreach (MenuTreeModel root in TreeModel.Children)
            {
                if (root == null || root.Children.Count == 0)
                {
                    continue;
                }

                if (!root.IsExpanded)
                {
                    root.IsExpanded = true;
                }

                foreach (MenuTreeModel child in root.Children)
                {
                    if (child.Children.Count > 0 && child.IsExpanded)
                    {
                        child.IsExpanded = false;
                    }

                    CollapseLoadedDescendants(child);
                }
            }
        }

        private void CollapseLoadedDescendants(MenuTreeModel node)
        {
            if (node == null || node.Children.Count == 0)
            {
                return;
            }

            foreach (MenuTreeModel child in node.Children)
            {
                CollapseLoadedDescendants(child);

                if (!child.IsPlaceholder && child.Children.Count > 0 && child.IsExpanded)
                {
                    child.IsExpanded = false;
                }
            }
        }

        private void LoadDirectChildren(MenuTreeModel node)
        {
            if (node == null || node.IsChildrenLoaded || _currentTreeRecipeInfoList == null)
            {
                return;
            }

            node.IsChildrenLoaded = true;
            node.Children.Clear();
            BindChildTreeMenu(node, _currentTreeRecipeInfoList, node.Id, 1);
        }

        private void RestoreCurrentSelectState()
        {
            if (TreeModel?.Children == null || string.IsNullOrWhiteSpace(CurrentSelectId))
            {
                return;
            }

            ExpandPathToCurrentSelect();

            foreach (MenuTreeModel item in TreeModel.Children)
            {
                if (ExpandToNode(item, CurrentSelectId))
                {
                    return;
                }
            }
        }

        private bool ExpandToNode(MenuTreeModel node, string targetId)
        {
            if (node == null)
            {
                return false;
            }

            node.IsSelect = node.Id == targetId ? 1 : 0;

            if (node.Id == targetId)
            {
                node.IsExpanded = true;
                return true;
            }

            foreach (MenuTreeModel child in node.Children)
            {
                if (ExpandToNode(child, targetId))
                {
                    node.IsExpanded = true;
                    return true;
                }
            }

            return false;
        }

        private void ExpandPathToCurrentSelect()
        {
            if (_currentTreeRecipeInfoList == null)
            {
                return;
            }

            RecipeInfo current = _currentTreeRecipeInfoList.FirstOrDefault(r => r.Id == CurrentSelectId);
            if (current == null)
            {
                return;
            }

            Stack<string> path = new Stack<string>();
            while (current != null)
            {
                path.Push(current.Id);
                current = string.IsNullOrWhiteSpace(current.ParentId)
                    ? null
                    : _currentTreeRecipeInfoList.FirstOrDefault(r => r.Id == current.ParentId);
            }

            ObservableCollection<MenuTreeModel> levelNodes = TreeModel.Children;
            MenuTreeModel currentNode = null;
            while (path.Count > 0 && levelNodes != null)
            {
                string id = path.Pop();
                currentNode = levelNodes.FirstOrDefault(n => n.Id == id);
                if (currentNode == null)
                {
                    return;
                }

                currentNode.IsExpanded = true;
                LoadDirectChildren(currentNode);
                levelNodes = currentNode.Children;
            }
        }
        private void SearchParentRecipe(RecipeInfo recipeInfo, List<RecipeInfo> dataList, List<RecipeInfo> result)
        {
            if (recipeInfo != null && recipeInfo.ParentId != null)
            {
                RecipeInfo parentRecipe = dataList.FirstOrDefault(d => d.Id == recipeInfo.ParentId);
                if (!result.Any(r => r.Id == parentRecipe.Id))
                    result.Add(dataList.FirstOrDefault(d => d.Id == recipeInfo.ParentId));
                if (!string.IsNullOrWhiteSpace(parentRecipe.ParentId))
                {
                    SearchParentRecipe(parentRecipe, dataList, result);
                }
            }
        }
        #endregion

        /// <summary>
        /// 选中事件
        /// </summary>
        /// <param name="obj"></param>
        [RelayCommand]
        public void SelectItem(object obj)
        {
            if (obj != null && TreeModel != null)
            {
                MenuTreeModel menuTreeModel = obj as MenuTreeModel;
                foreach (var item in TreeModel.Children)
                {
                    SetMenuSelectStatic(TreeModel.Children, menuTreeModel?.RecipeInfo);
                }
                CurrentSelectId = menuTreeModel.Id;
            }
        }
        [RelayCommand]
        public void MouseDouble(object obj)
        {
            if (obj != null && TreeModel != null)
            {
                MenuTreeModel menuTreeModel = obj as MenuTreeModel;
                if (menuTreeModel.Level == 1)
                {
                    //IRecipe recipeNodeService = _recipeManager.GetCurrentRecipe(menuTreeModel.Id);

                    //if (recipeNodeService != null)
                    if (_recipeManager != null)
                    {
                        GetRecipe?.Invoke(_recipeManager, menuTreeModel.RecipeInfo);
                    }
                    else
                    {
                        _MessageBoxService.ErrorDialog("The current recipe file does not exist！");
                    }
                }
            }
        }
        public void SetMenuSelectStatic(ObservableCollection<MenuTreeModel> treeModelList, RecipeInfo recipeInfo)
        {
            foreach (var item in treeModelList)
            {
                item.IsSelect = 0;
                if (item.Type == OperationType.Update || item.Type == OperationType.Delete)
                {
                    if (item.Id == recipeInfo.Id)
                        item.Type = OperationType.Delete;
                    else
                        item.Type = OperationType.Update;
                }
                if (item.Children.Count != 0)
                    SetMenuSelectStatic(item.Children, recipeInfo);
            }
        }
        #region 菜单刷新
        public void RefreshData(RecipeInfo recipeInfo, OperationType state)
        {
            if (TreeModel != null && recipeInfo != null)
            {
                SetPasteIsEnabled(TreeModel, false);
                _copeRecipeInfo = null;
                AddOrUpdateMenu(TreeModel.Children, null, recipeInfo, state);
                if (state == OperationType.Add || state == OperationType.Update || state == OperationType.Paste)
                    OpenRecipe(recipeInfo);
            }
        }
        /// <summary>
        /// 添加or编辑时更新菜单
        /// </summary>
        /// <param name="treeModelList"></param>
        /// <param name="recipeInfo"></param>
        /// <param name="state"></param>
        public void AddOrUpdateMenu(ObservableCollection<MenuTreeModel> treeModelList, ObservableCollection<MenuTreeModel> parentTreeModelList, RecipeInfo recipeInfo, OperationType state)
        {
            bool result = true;
            //添加
            if (state == OperationType.Add)
            {
                MenuTreeModel treeMenu = new MenuTreeModel(_recipeWindow);
                treeMenu.Id = recipeInfo.Id;
                treeMenu.Name = recipeInfo.RecipeName;
                treeMenu.RecipeInfo = recipeInfo;
                treeMenu.Type = OperationType.Update;
                treeMenu.Level = recipeInfo.Level;
                treeMenu.IsShowAddRecipe = recipeInfo.Level == 0 ? Visibility.Visible : Visibility.Collapsed;
                treeMenu.IsShowAddCopy = recipeInfo.Level == 1 ? Visibility.Visible : Visibility.Collapsed;
                treeMenu.MoveCompleteAction = result => MoveComplete(result);
                treeMenu.CopyRecipeEvent = r => CopyRecipe(r);
                treeMenu.PasteRecipeEvent = (r) => PasteRecipe(r);
                treeMenu.Refresh = (r, state) => RefreshData(r, state);
                treeMenu.CheckMove = (target, move) => CheckMove(target, move);
                treeMenu.DeleteCallback = r => DeleteRecipte(r);
                treeMenu.ImportRecipeEvent = (r) => ImportRecipe(r);
                treeMenu.ExportRecipeEvent = (r) => ExportRecipe(r);
                treeMenu.ImportRecipeFromExcelEvent = (r) => ImportRecipeFromExcel(r);
                treeMenu.ExportRecipeToExcelEvent = (r) => ExportRecipeToExcel(r);
                if (!string.IsNullOrWhiteSpace(recipeInfo.ParentId))
                {
                    if (treeModelList.Any(t => t.Id == recipeInfo.ParentId))
                    {
                        MenuTreeModel treeModel = treeModelList.FirstOrDefault(t => t.Id == recipeInfo.ParentId);
                        if (treeModel != null)
                        {
                            treeMenu.Parent = treeModel;
                            treeModel.Type = OperationType.Add;
                            treeModel.Children.Add(treeMenu);
                            result = false;
                        }
                    }
                }
                else
                {
                    treeModelList.Add(treeMenu);
                    result = false;
                }
            }
            else if (state == OperationType.Update && treeModelList.Any(t => t.Id == recipeInfo.Id))
            {
                MenuTreeModel menuTreeModel = treeModelList.FirstOrDefault(t => t.Id == recipeInfo.Id);
                menuTreeModel.Name = recipeInfo.RecipeName;
                menuTreeModel.RecipeInfo = recipeInfo;
                result = false;
            }
            else if (state == OperationType.Delete && treeModelList.Any(t => t.Id == recipeInfo.Id))
            {
                treeModelList.Remove(treeModelList?.FirstOrDefault(t => t.Id == recipeInfo.Id) ?? new MenuTreeModel(_recipeWindow));
                if (parentTreeModelList != null)
                {
                    MenuTreeModel treeModel = parentTreeModelList.FirstOrDefault(t => t.Id == recipeInfo.ParentId);
                    if (treeModel != null && treeModel.Children.Count <= 0)
                        treeModel.Type = OperationType.Update;
                }
                result = false;
            }
            if (result)
            {
                foreach (var item in treeModelList ?? new ObservableCollection<MenuTreeModel>())
                {
                    if (item.Children.Count != 0)
                    {
                        AddOrUpdateMenu(item.Children, treeModelList, recipeInfo, state);
                    }
                }
            }
        }
        #endregion

        #region 删除，移动，复制，粘贴，校验移动，导入，导出
        /// <summary>
        /// 删除配方
        /// </summary>
        /// <param name="recipeInfo"></param>
        public bool DeleteRecipte(RecipeInfo recipeInfo)
        {
            bool result = false;
            if (recipeInfo.IsUse)
            {
                _MessageBoxService.ErrorDialog("The current recipe is in use and cannot be deleted!");
                return false;
            }


            string recipeStr = recipeInfo.Level == 0 ? "folder" : "recipe";
            recipeStr += $": {recipeInfo.RecipeName}";
            var isdel = _MessageBoxService.YesNoCancelDialog($"Are you sure you want to delete {recipeStr}?", "Warning");
            if (isdel == ButtonResult.Yes)
            {
                _recipeProofJobEvent.Publish(recipeInfo);
                if (_recipeService.RemoveRecipe(recipeInfo))
                {
                    result = true;
                    _MessageBoxService.Success($"The {recipeStr} deleted successfully！", "RecipeMessage");
                }
                else
                {
                    _MessageBoxService.ErrorDialog($"Failed to delete {recipeStr}！");
                }
            }
            if (result)
            {
                DeletedRecipeEvent?.Invoke(recipeInfo);
            }
            return result;
        }
        public void MoveComplete(MenuTreeModel menuTreeModel)
        {
            if (menuTreeModel.RecipeInfo != null)
            {
                List<RecipeInfo> recipeInfos = _recipeService.GetRecipeAll();

                if (recipeInfos.Any(_ => _.ParentId == menuTreeModel.Parent.Id && _.RecipeName == menuTreeModel.RecipeInfo.RecipeName))
                {
                    _MessageBoxService.ErrorDialog("The same recipe name already exists！");
                    return;
                }

                RecipeInfo recipeInfo = recipeInfos?.FirstOrDefault(f => f.Id == menuTreeModel.RecipeInfo.Id) ?? new RecipeInfo();
                var oldParent = recipeInfo.ParentName;
                recipeInfo.ParentId = menuTreeModel.Parent.Id;
                recipeInfo.ParentName = menuTreeModel.Parent.Name;
                recipeInfo.UpdateDate = DateTime.Now;
                recipeInfo.UpdateUser = FrmPublic.CurrentLoginUser?.Name;
                _recipeService.EditRecipe(recipeInfos, recipeInfo);
                RefreshData(recipeInfo, OperationType.Update);
                LogHelper.LogInfo($"Recipe Operate: Move  by {FrmPublic.CurrentLoginUser?.Name}, recipe name={recipeInfo.RecipeName}, from {oldParent} to {recipeInfo.ParentName} ");
            }
        }
        /// <summary>
        /// 复制
        /// </summary>
        public void CopyRecipe(RecipeInfo recipeInfo)
        {
            if (recipeInfo == null)
            {
                _MessageBoxService.WarningDialog("Please select the recipe you want to copy!");
                return;
            }
            else if (recipeInfo.Level == 0)
            {
                _MessageBoxService.WarningDialog("Folder cannot be copied！");
                return;
            }
            _copeRecipeInfo = recipeInfo;
            SetPasteIsEnabled(TreeModel, true);
        }
        private void SetPasteIsEnabled(MenuTreeModel treeModel, bool isEnable)
        {
            treeModel.PasteIsEnabled = true;
            if (treeModel.Children.Count > 0)
            {
                foreach (var item in treeModel.Children)
                {
                    item.PasteIsEnabled = true;
                    SetPasteIsEnabled(item, isEnable);
                }
            }
        }
        /// <summary>
        /// 粘贴
        /// </summary>
        /// <param name="recipeInfo"></param>
        public void PasteRecipe(RecipeInfo recipeInfo)
        {
            if (_copeRecipeInfo == null)
            {
                _MessageBoxService.WarningDialog("Please select the recipe to paste!");
                return;
            }
            else if (recipeInfo.Level == 1)
            {
                _MessageBoxService.WarningDialog("Do not paste the recipe onto the recipe!");
                return;
            }
            RecipeInfo newRecipe = new RecipeInfo
            {
                Id = _copeRecipeInfo.Id,
                ParentId = recipeInfo.Id,
                ParentName = recipeInfo.RecipeName,
                Level = _copeRecipeInfo.Level,
                RecipeName = _copeRecipeInfo.RecipeName + "_Copy",
                CreateDate = DateTime.Now,
                CreateUser = UserInfo?.Name
            };

            _recipeWindow.OpenWindow(newRecipe, (r, state) => { RefreshData(r, OperationType.Add); CopyRecipeEvent?.Invoke(r); }, OperationType.Paste);
        }
        /// <summary>
        /// 移动配方前校验
        /// </summary>
        /// <param name="targetRecipe"></param>
        /// <param name="moveRecipe"></param>
        /// <returns></returns>
        public bool CheckMove(RecipeInfo targetRecipe, RecipeInfo moveRecipe, List<RecipeInfo> recipeInfos = null)
        {
            List<RecipeInfo> recipeInfoList = recipeInfos;
            if (recipeInfos == null)
            {
                recipeInfoList = _recipeService.GetRecipeAll()?.ToList();
            }
            if (recipeInfoList.Any(_ => _.ParentId == targetRecipe.Id && _.RecipeName == moveRecipe.RecipeName))
            {
                _MessageBoxService.ErrorDialog("The same recipe name already exists！");
                return false;
            }

            foreach (var item in recipeInfoList.Where(f => f.ParentId == moveRecipe.Id).ToList())
            {
                if (item.Id == targetRecipe.Id)
                {
                    return false;
                }
                return CheckMove(targetRecipe, item, recipeInfos);
            }
            return true;
        }
        /// <summary>
        /// 打开
        /// </summary>
        /// <param name="recipeInfo"></param>
        public void OpenRecipe(RecipeInfo recipeInfo)
        {
            //IRecipe recipeNodeService = _recipeManager.GetCurrentRecipe(recipeInfo.Id);
            //if (recipeNodeService != null)
            if (recipeInfo.Level == 0) return;
            if (_recipeManager != null)
            {
                GetRecipe?.Invoke(_recipeManager, recipeInfo);
            }
            else
            {
                _MessageBoxService.ErrorDialog("The current recipe file does not exist！");
            }
        }
        /// <summary>
        /// 导入
        /// </summary>
        /// <param name="recipeInfo"></param>
        public void ImportRecipe(RecipeInfo recipeInfo)
        {
            System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (folderBrowserDialog.SelectedPath == null)
                {
                    _MessageBoxService.ErrorDialog("Please select the recipe folder to import!");// ("请选择要导入的配方文件夹！");
                    return;
                }
                DirectoryInfo folder = new DirectoryInfo(folderBrowserDialog.SelectedPath);

                DirectoryInfo[] directoryList = folder.GetDirectories();
                FileInfo[] fileList = folder.GetFiles();
                if (fileList == null || fileList.Length <= 0)
                {
                    _MessageBoxService.ErrorDialog("The selected import file is incorrect!");// ("选择的导入文件不正确！");
                    return;
                }
                FileInfo fileInfo = fileList.ToList().FirstOrDefault(f => f.FullName.ToLower().Contains("recipe.json"));
                if (fileInfo == null)
                {
                    _MessageBoxService.ErrorDialog("The selected import file is incorrect!");// ("选择的导入文件不正确！");
                    return;
                }
                List<RecipeInfo> targetRecipeList = JsonHelper.DeserializeFile<List<RecipeInfo>>(fileInfo.FullName);
                int dataCount = targetRecipeList.Where(_ => _.Level == 1 && directoryList.Any(d => d.Name == (_.RecipeName + "##" + _.Id))).ToList().Count;
                if (dataCount != directoryList.Count())
                {
                    _MessageBoxService.ErrorDialog("The imported recipe data is abnormal!");// ("导入的配方数据有异常！");
                    return;
                }
                List<RecipeInfo> sourceRecipeList = _recipeService.GetRecipeAll();

                //删除存在重复的文件夹
                //foreach (var item in sourceRecipeList.Where(_=>_.Level==0))
                //{
                //    RecipeInfo remove= targetRecipeList.FirstOrDefault(_ => _.Id == item.Id && _.Level == 0);
                //    targetRecipeList.Remove(remove);
                //}
                if (sourceRecipeList.Any(_ => targetRecipeList.Any(t => t.Id == _.Id && t.ParentId == _.ParentId) && _.ParentId == recipeInfo.Id))
                {
                    _recipeWindow.OpenImportWindow(recipeInfo, folderBrowserDialog.SelectedPath, (r) => { });
                }
                else
                {
                    _recipeService.ImportRecipe(recipeInfo, targetRecipeList, folderBrowserDialog.SelectedPath);
                }
                CurrentSelectId = recipeInfo?.Id;
                OutSideLoadMenuTree();
            }
        }
        /// <summary>
        /// 导出
        /// </summary>
        /// <param name="recipeInfo"></param>
        public void ExportRecipe(RecipeInfo recipeInfo)
        {
            System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                bool result = _recipeService.ExportRecipe(recipeInfo, folderBrowserDialog.SelectedPath);
                if (result)
                {
                    _MessageBoxService.SuccessDialog("Recipe exported successfully！");
                }
                else
                {
                    _MessageBoxService.ErrorDialog("Recipe export failed！");
                }
            }
        }

        /// <summary>
        /// 从Excel导入Recipe
        /// </summary>
        /// <param name="recipeInfo"></param>
        public void ImportRecipeFromExcel(RecipeInfo recipeInfo)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select Excel file",
                Filter = "Excel Files|*.xlsx;*.xls",
                CheckFileExists = true,
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() != true)
            {
                return;
            }

            string filePath = openFileDialog.FileName;
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                _MessageBoxService.ErrorDialog("Please select a valid Excel file!");
                return;
            }

            try
            {
                // TODO: 调用Excel导入逻辑
                List<RecipeContent> importList = _recipeService.ImportRecipeFromExcel(filePath);
                if (importList == null || importList.Count == 0)
                {
                    _MessageBoxService.WarningDialog("No valid recipe data was found in the Excel file.");
                    return;
                }

                List<RecipeContent> validImportList = importList
                                                      .Where(r => r != null)
                                                      .ToList();

                List<RecipeContent> waferContents = validImportList
                    .Where(r => string.Equals(r.RecipeType, "Wafer_info", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (waferContents.Count == 0)
                {
                    _MessageBoxService.WarningDialog("No valid Wafer_info data was found in the Excel file.");
                    return;
                }
                List<RecipeInfo> sourceRecipeList = _recipeService.GetRecipeAll() ?? new List<RecipeInfo>();

                HashSet<string> failedRecipeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                List<string> duplicateNamesInFile = new List<string>();
                List<string> duplicateNamesInCurrentFolder = new List<string>();

                MarkDuplicateNamesInImportFile(waferContents, duplicateNamesInFile, failedRecipeNames);

                if (waferContents.Count == 0)
                {
                    _MessageBoxService.WarningDialog("No valid Wafer_info data was found in the Excel file.");
                    return;
                }

                var waferGroups = waferContents
                                     .Select(x => new
                                     {
                                         Content = x,
                                         RecipeName = string.IsNullOrWhiteSpace(GetRecipeNameFromContent(x)) ? x.RecipeName : GetRecipeNameFromContent(x)
                                     })
                                     .Where(x => !string.IsNullOrWhiteSpace(x.RecipeName))
                                     .GroupBy(x => x.RecipeName, StringComparer.OrdinalIgnoreCase)
                                     .ToList();

                foreach (var group in waferGroups)
                {
                    if (failedRecipeNames.Contains(group.Key))
                    {
                        continue;
                    }

                    bool existsInCurrentFolder = sourceRecipeList.Any(r =>
                        r.ParentId == recipeInfo.Id &&
                        string.Equals(r.RecipeName, group.Key, StringComparison.OrdinalIgnoreCase));

                    if (existsInCurrentFolder)
                    {
                        failedRecipeNames.Add(group.Key);
                        duplicateNamesInCurrentFolder.Add(group.Key);
                    }
                }
                List<RecipeInfo> createdRecipeInfos = new List<RecipeInfo>();
                HashSet<string> successRecipeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                Dictionary<string, string> recipeNameIdMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (var group in waferGroups)
                {
                    string recipeName = group.Key;
                    if (string.IsNullOrWhiteSpace(recipeName) || failedRecipeNames.Contains(recipeName))
                    {
                        continue;
                    }

                    string newRecipeId = Guid.NewGuid().ToString();

                    RecipeInfo newRecipe = new RecipeInfo
                    {
                        Id = newRecipeId,
                        RecipeName = recipeName,
                        ParentId = recipeInfo.Id,
                        ParentName = recipeInfo.RecipeName,
                        Level = 1,
                        IsUse = false,
                        CreateDate = DateTime.Now,
                        UpdateDate = DateTime.Now,
                        CreateUser = UserInfo?.Name,
                        UpdateUser = UserInfo?.Name
                    };

                    bool addResult = _recipeService.AddRecipe(newRecipe);
                    if (!addResult)
                    {
                        failedRecipeNames.Add(recipeName);
                        continue;
                    }

                    createdRecipeInfos.Add(newRecipe);
                    sourceRecipeList.Add(newRecipe);
                    successRecipeNames.Add(recipeName);
                    recipeNameIdMap[recipeName] = newRecipe.Id;
                }

                if (createdRecipeInfos.Count == 0)
                {
                    List<string> failReasons = new List<string>();

                    if (duplicateNamesInFile.Count > 0)
                    {
                        failReasons.Add($"Duplicate Recipe_Name in Excel: {string.Join(", ", duplicateNamesInFile.Distinct(StringComparer.OrdinalIgnoreCase))}");
                    }

                    if (duplicateNamesInCurrentFolder.Count > 0)
                    {
                        failReasons.Add($"Recipe_Name already exists in current folder: {string.Join(", ", duplicateNamesInCurrentFolder.Distinct(StringComparer.OrdinalIgnoreCase))}");
                    }

                    if (failReasons.Count == 0 && failedRecipeNames.Count > 0)
                    {
                        failReasons.Add($"Failed recipe names: {string.Join(", ", failedRecipeNames.Distinct(StringComparer.OrdinalIgnoreCase))}");
                    }

                    _MessageBoxService.WarningDialog(failReasons.Count > 0
                        ? string.Join(Environment.NewLine, failReasons)
                        : "No new recipe folders were created.");

                    return;
                }


                List<RecipeContent> finalImportList = validImportList
                                                       .Where(r =>
                                                       {
                                                           string recipeName = GetRecipeNameFromContent(r);
                                                           return !string.IsNullOrWhiteSpace(recipeName) &&
                                                           successRecipeNames.Contains(recipeName) &&
                                                           !failedRecipeNames.Contains(recipeName);
                                                       })
                                                      .ToList();

                foreach (RecipeContent content in finalImportList)
                {
                    string recipeName = GetRecipeNameFromContent(content);
                    if (!string.IsNullOrWhiteSpace(recipeName) &&
                        recipeNameIdMap.TryGetValue(recipeName, out string newRecipeId))
                    {
                        content.Id = newRecipeId;
                        content.RecipeName = recipeName;
                    }
                }

                ImportRecipeFromExcelEvent?.Invoke(finalImportList);
                CurrentSelectId = recipeInfo?.Id;
                OutSideLoadMenuTree();

                List<string> messageList = new List<string> { $"Imported {createdRecipeInfos.Count} recipe folders successfully." };

                if (duplicateNamesInFile.Count > 0)
                {
                    messageList.Add($"Duplicate Recipe_Name in Excel: {string.Join(", ", duplicateNamesInFile.Distinct(StringComparer.OrdinalIgnoreCase))}");
                }

                if (duplicateNamesInCurrentFolder.Count > 0)
                {
                    messageList.Add($"Recipe_Name already exists in current folder: {string.Join(", ", duplicateNamesInCurrentFolder.Distinct(StringComparer.OrdinalIgnoreCase))}");
                }

                if (messageList.Count > 1)
                {
                    _MessageBoxService.WarningDialog(string.Join(Environment.NewLine, messageList));
                }
                else
                {
                    _MessageBoxService.SuccessDialog(messageList[0]);
                }
            }
            catch (Exception ex)
            {
                _MessageBoxService.ErrorDialog($"Import recipe from Excel failed! {ex.Message}");
            }
        }

        /// <summary>
        /// 导出Recipe到Excel
        /// </summary>
        /// <param name="recipeInfo"></param>
        public void ExportRecipeToExcel(RecipeInfo recipeInfo)
        {
            if (recipeInfo == null || string.IsNullOrWhiteSpace(recipeInfo.Id))
            {
                _MessageBoxService.WarningDialog("The selected recipe is invalid.");
                return;
            }

            List<RecipeInfo> needExportRecipes = GetExportRecipes(recipeInfo);
            if (!ValidateRecipeFilesForExport(needExportRecipes))
            {
                return;
            }

            if (!ValidateDuplicateRecipeNamesForExport(needExportRecipes))
            {
                return;
            }


            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Title = "Save Excel file",
                Filter = "Excel Files|*.xlsx",
                DefaultExt = "xlsx",
                AddExtension = true,
                FileName = $"{recipeInfo?.RecipeName ?? "Recipe"}.xlsx",
                OverwritePrompt = false,
                CreatePrompt = false
            };

            if (saveFileDialog.ShowDialog() != true)
            {
                return;
            }

            string filePath = saveFileDialog.FileName;
            if (string.IsNullOrWhiteSpace(filePath))
            {
                _MessageBoxService.ErrorDialog("Please select a valid export path!");
                return;
            }
            if (IsFileOpened(filePath))
            {
                _MessageBoxService.WarningDialog("The target Excel file is currently open. Please close it and try again.");
                return;
            }
            try
            {
                // TODO: 调用Excel导出逻辑
                if (GetRecipeContentByID == null)
                {
                    _MessageBoxService.ErrorDialog("GetRecipeContentByID delegate is not configured.");
                    return;
                }
                List<RecipeContent> recipeContents = new List<RecipeContent>();
                if (recipeInfo.Level == 1)
                {
                    recipeContents.AddRange(GetRecipeContentByID.Invoke(recipeInfo.Id) ?? new List<RecipeContent>());
                }
                else
                {
                    List<RecipeInfo> allRecipes = _recipeService.GetRecipeAll() ?? new List<RecipeInfo>();
                    List<RecipeInfo> childRecipes = new List<RecipeInfo>();
                    CollectChildRecipes(recipeInfo, allRecipes, childRecipes);

                    foreach (RecipeInfo childRecipe in childRecipes.Where(r => r.Level == 1))
                    {
                        var recipeContent = GetRecipeContentByID.Invoke(childRecipe.Id) ?? new List<RecipeContent>();
                        if (recipeContent.Count == 0)
                        {
                            _MessageBoxService.WarningDialog($"No recipe content found for export. Recipe name: {childRecipe.RecipeName}");
                            return;
                        }
                        recipeContents.AddRange(GetRecipeContentByID.Invoke(childRecipe.Id) ?? new List<RecipeContent>());
                    }
                }
                if (recipeContents.Count == 0)
                {
                    _MessageBoxService.WarningDialog("No recipe content found for export.");
                    return;
                }

                if (File.Exists(filePath))
                {
                    ButtonResult result1 = _MessageBoxService.YesNoCancelDialog(
                        $"The Excel file already exists:\r\n{filePath}\r\n" +
                        $"Do you want to write into the existing file?\r\n" +
                        $"Yes: Continue writing\r\n" +
                        $"No: Replace file\r\n" +
                        $"Cancel: Cancel export", "Confirm Export");

                    if (result1 == ButtonResult.No)
                    {
                        File.Delete(filePath);
                    }
                    else if (result1 == ButtonResult.Cancel || result1 == ButtonResult.None)
                    {
                        return;

                    }
                }
                bool result = _recipeService.ExportRecipeToExcel(recipeInfo, recipeContents, filePath);
                if (result)
                {
                    _MessageBoxService.SuccessDialog($"Selected path: {filePath}");
                }
                else
                {
                    _MessageBoxService.ErrorDialog("Export recipe to Excel failed!");
                }
            }
            catch (Exception ex)
            {
                _MessageBoxService.ErrorDialog($"Export recipe to Excel failed! {ex.Message}");
            }
        }
        #endregion

        #region excle导入导出相关私有方法

        private static string GetRecipeNameFromContent(RecipeContent content)
        {
            if (content == null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(content.RecipeName))
            {
                return content.RecipeName.Trim();
            }

            if (content.Fields == null || content.Fields.Count == 0)
            {
                return string.Empty;
            }

            string[] candidateKeys = { "Recipe_Name", "RecipeName", "recipe_name", "recipe name" };

            foreach (string key in candidateKeys)
            {
                if (content.Fields.TryGetValue(key, out object value) &&
                    value != null &&
                    !string.IsNullOrWhiteSpace(value.ToString()))
                {
                    return value.ToString().Trim();
                }
            }

            return string.Empty;
        }
        private void CollectChildRecipes(RecipeInfo parentRecipe, List<RecipeInfo> allRecipes, List<RecipeInfo> result)
        {
            if (parentRecipe == null || allRecipes == null || result == null)
            {
                return;
            }

            foreach (RecipeInfo item in allRecipes.Where(r => r.ParentId == parentRecipe.Id).ToList())
            {
                result.Add(item);
                if (item.Level == 0)
                {
                    CollectChildRecipes(item, allRecipes, result);
                }
            }
        }
        private static bool IsFileOpened(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return false;
            }

            try
            {
                using FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                return false;
            }
            catch (IOException)
            {
                return true;
            }
        }

        /// <summary>
        /// 检查Excel文件内部的 Wafer_info 是否存在同名 Recipe_Name。
        /// 如果同一个Excel文件中多个Wafer_info名称相同，则这些记录全部标记为失败。
        /// </summary>
        private static void MarkDuplicateNamesInImportFile(List<RecipeContent> waferContents, List<string> duplicateNamesInFile, HashSet<string> failedRecipeNames)
        {
            var duplicatedNamesInFile = waferContents
                .Select(x => new
                {
                    Content = x,
                    RecipeName = string.IsNullOrWhiteSpace(GetRecipeNameFromContent(x)) ? x.RecipeName : GetRecipeNameFromContent(x)
                })
                .GroupBy(x => x.RecipeName, StringComparer.OrdinalIgnoreCase)
                .Where(g => !string.IsNullOrWhiteSpace(g.Key) && g.Count() > 1)
                .ToList();

            foreach (var duplicatedGroup in duplicatedNamesInFile)
            {
                failedRecipeNames.Add(duplicatedGroup.Key);
                duplicateNamesInFile.Add(duplicatedGroup.Key);
            }
        }

        /// <summary>
        /// 执行搜索并返回完整的树形结构
        /// </summary>
        private List<RecipeInfo> PerformSearch(List<RecipeInfo> allRecipes, string keyword)
        {
            // 步骤1：获取所有直接匹配的项
            var directMatches = allRecipes
                .Where(r => r.RecipeName.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (directMatches.Count == 0)
            {
                return new List<RecipeInfo>(); // 无匹配结果
            }

            // 步骤2：构建完整的树形结果
            var resultSet = new HashSet<RecipeInfo>(new RecipeInfoEqualityComparer());

            foreach (var match in directMatches)
            {
                // 添加当前匹配项
                resultSet.Add(match);

                if (match.Level == 0) // 如果是文件夹
                {
                    // 添加文件夹的所有子项
                    AddAllDescendants(match, _recipeService.GetRecipeAll(), resultSet);
                }

                // 添加所有父级路径，保持树形结构
                AddAllAncestors(match, allRecipes, resultSet);
            }

            return resultSet.ToList();
        }

        /// <summary>
        /// 用于HashSet去重的比较器
        /// </summary>
        private class RecipeInfoEqualityComparer : IEqualityComparer<RecipeInfo>
        {
            public bool Equals(RecipeInfo x, RecipeInfo y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (x is null || y is null) return false;
                return x.Id == y.Id;
            }

            public int GetHashCode(RecipeInfo obj)
            {
                return obj.Id?.GetHashCode() ?? 0;
            }
        }

        /// <summary>
        /// 获取导入或导出所需的配方列表，包含所有子项（如果是文件夹）并且仅返回Level为1的配方
        /// </summary>
        /// <param name="recipeInfo"></param>
        /// <returns></returns>
        private List<RecipeInfo> GetExportRecipes(RecipeInfo recipeInfo)
        {
            List<RecipeInfo> exportRecipes = new List<RecipeInfo>();
            if (recipeInfo == null || string.IsNullOrWhiteSpace(recipeInfo.Id))
            {
                return exportRecipes;
            }

            if (recipeInfo.Level == 1)
            {
                exportRecipes.Add(recipeInfo);
                return exportRecipes;
            }

            List<RecipeInfo> allRecipes = _recipeService.GetRecipeAll() ?? new List<RecipeInfo>();
            CollectChildRecipes(recipeInfo, allRecipes, exportRecipes);
            return exportRecipes.Where(r => r.Level == 1).ToList();
        }

        /// <summary>
        /// 验证导出配方列表中的每个配方文件是否存在
        /// </summary>
        /// <param name="recipes"></param>
        /// <returns></returns>
        private bool ValidateRecipeFilesForExport(List<RecipeInfo> recipes)
        {
            if (recipes == null || recipes.Count == 0)
            {
                _MessageBoxService.WarningDialog("No valid recipe was found for export.");
                return false;
            }

            List<string> missingRecipeNames = recipes
                .Where(r => r != null && _recipeService.IsRecipeConfigMissing(r.Id))
                .Select(r => $"{r.RecipeName} ({r.Id})")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (missingRecipeNames.Count > 0)
            {
                _MessageBoxService.WarningDialog(
                    $"The following recipe files do not exist:{Environment.NewLine}{string.Join(Environment.NewLine, missingRecipeNames)}");
                return false;
            }

            return true;
        }

        private bool ValidateDuplicateRecipeNamesForExport(List<RecipeInfo> recipes)
        {
            if (recipes == null || recipes.Count == 0)
            {
                return true;
            }

            var duplicateNames = recipes
                .Where(r => r != null && !string.IsNullOrWhiteSpace(r.RecipeName))
                .GroupBy(r => r.RecipeName.Trim(), StringComparer.Ordinal)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateNames.Count == 0)
            {
                return true;
            }

            _MessageBoxService.WarningDialog(
                $"Duplicate recipe names were found. Export has been canceled.{Environment.NewLine}" +
                string.Join(Environment.NewLine, duplicateNames));

            return false;
        }
        #endregion
        /// <summary>
        /// 递归添加所有子项
        /// </summary>
        private void AddAllDescendants(RecipeInfo folder, List<RecipeInfo> allRecipes, HashSet<RecipeInfo> resultSet)
        {
            // 获取直接子项
            var children = allRecipes
                .Where(r => r.ParentId == folder.Id)
                .ToList();

            foreach (var child in children)
            {
                if (resultSet.Add(child)) // 避免重复添加
                {
                    if (child.Level == 0) // 如果子项也是文件夹
                    {
                        AddAllDescendants(child, allRecipes, resultSet);
                    }
                }
            }
        }

        /// <summary>
        /// 递归添加所有父级
        /// </summary>
        private void AddAllAncestors(RecipeInfo item, List<RecipeInfo> allRecipes, HashSet<RecipeInfo> resultSet)
        {
            if (string.IsNullOrWhiteSpace(item.ParentId))
                return;

            var parent = allRecipes.FirstOrDefault(r => r.Id == item.ParentId);
            if (parent != null && resultSet.Add(parent))
            {
                // 递归向上查找
                AddAllAncestors(parent, allRecipes, resultSet);
            }
        }


    }
    public partial class MenuTreeModel : ObservableObject
    {
        public event PropertyChangedEventHandler PropertyChanged;
        IRecipeWindow _recipeWindow;
        public MenuTreeModel(IRecipeWindow recipeWindow)
        {
            _recipeWindow = recipeWindow;
        }
        [ObservableProperty]
        public string _id;
        [ObservableProperty]
        public string _name;
        [ObservableProperty]
        public int _level;
        [ObservableProperty]
        public OperationType? _type;
        [ObservableProperty]
        public Visibility _isShowAddRecipe = Visibility.Visible;
        [ObservableProperty]
        public Visibility _isShowAddCopy = Visibility.Visible;
        [ObservableProperty]
        public Visibility _isShowDelete = Visibility.Visible;
        [ObservableProperty]
        public RecipeInfo _recipeInfo;
        [ObservableProperty]
        public int _isSelect = 0;
        [ObservableProperty]
        public bool _pasteIsEnabled = false;
        [ObservableProperty]
        public bool _isExpanded = true;
        partial void OnIsExpandedChanged(bool value)
        {
            if (value)
            {
                LoadChildrenAction?.Invoke(this);
            }
        }

        public bool IsChildrenLoaded { get; set; }
        public bool IsPlaceholder { get; set; }
        public Action<MenuTreeModel> LoadChildrenAction { get; set; }
        [ObservableProperty]
        Visibility _isShowContextMenu = Visibility.Visible;
        /// <summary>
        /// 父节点
        /// </summary>
        public MenuTreeModel Parent { get; set; }
        public UserInfo UserInfo { get; set; }
        /// <summary>
        /// 子节点集合
        /// </summary>
        public ObservableCollection<MenuTreeModel> Children { get; set; } = new ObservableCollection<MenuTreeModel>();

        [ObservableProperty]
        public bool _isAddMove;
        [ObservableProperty]
        public bool _isAddInMove;
        private bool m_isAdd = false;

        /// <summary>
        /// 拖动到目标节点时增加到同级目录标志
        /// </summary>
        public bool IsAdd
        {
            get { return m_isAdd; }
            set
            {
                if (m_isAdd != value)
                {
                    m_isAdd = value;
                    IsAddMove = value;
                    if (m_isAdd)
                    {
                        IsAddIn = false;
                    }
                    if (PropertyChanged != null)
                    {
                        PropertyChanged.Invoke(this, new PropertyChangedEventArgs("IsAdd"));
                    }
                }
            }
        }
        private bool m_isAddin = false;
        /// <summary>
        /// 拖动到目标节点时增加到子目录标志
        /// </summary>
        public bool IsAddIn
        {
            get { return m_isAddin; }
            set
            {
                if (m_isAddin != value)
                {
                    m_isAddin = value;
                    IsAddInMove = value;
                    if (m_isAddin)
                    {
                        m_isAdd = false;
                        IsAddMove = false;
                    }
                    //触发事件
                    if (PropertyChanged != null)
                    {
                        PropertyChanged.Invoke(this, new PropertyChangedEventArgs("IsAddIn"));
                    }
                }
            }
        }
        /// <summary>
        /// 刷新
        /// </summary>
        public Action<RecipeInfo, OperationType> Refresh { get; set; }
        /// <summary>
        /// 删除
        /// </summary>
        public Func<RecipeInfo, bool> DeleteCallback { get; set; }
        /// <summary>
        /// 移动完成后保存
        /// </summary>
        public Action<MenuTreeModel> MoveCompleteAction { get; set; }
        /// <summary>
        /// 复制
        /// </summary>
        public Action<RecipeInfo> CopyRecipeEvent { get; set; }
        /// <summary>
        /// 粘贴
        /// </summary>
        public Action<RecipeInfo> PasteRecipeEvent { get; set; }
        /// <summary>
        /// 打开
        /// </summary>
        public Action<RecipeInfo> OpenRecipeEvent { get; set; }
        /// <summary>
        /// 导入
        /// </summary>
        public Action<RecipeInfo> ImportRecipeEvent { get; set; }
        /// <summary>
        /// 导出
        /// </summary>
        public Action<RecipeInfo> ExportRecipeEvent { get; set; }

        /// <summary>
        /// Import from excel 
        /// </summary>
        public Action<RecipeInfo> ImportRecipeFromExcelEvent { get; set; }


        /// <summary>
        /// Import to excel 
        /// </summary>
        public Action<RecipeInfo> ExportRecipeToExcelEvent { get; set; }

        public Func<RecipeInfo, RecipeInfo, bool> CheckMove { get; set; }

        [RelayCommand]
        public void AddRecipe()
        {
            _recipeWindow.OpenWindow(new RecipeInfo()
            {
                ParentId = Id,
                ParentName = Name,
                Level = 1,
                CreateUser = UserInfo?.Name,
                UpdateUser = UserInfo?.Name,
            }, Refresh, OperationType.Add);
        }

        public void AddRecipe(RecipeInfo recipeInfo)
        {
            _recipeWindow.OpenWindow(recipeInfo, Refresh, OperationType.Add);
        }

        [RelayCommand]
        public void AddFolder()
        {
            _recipeWindow.OpenWindow(new RecipeInfo()
            {
                ParentId = Id,
                ParentName = Name,
                Level = 0
            }, Refresh, OperationType.Add);
        }
        [RelayCommand]
        public void UpdateRecipe()
        {
            _recipeWindow.OpenWindow(RecipeInfo, Refresh, OperationType.Update);
        }
        [RelayCommand]
        public void DeleteRecipe()
        {
            if (DeleteCallback?.Invoke(RecipeInfo) ?? false)
            {
                Refresh?.Invoke(RecipeInfo, OperationType.Delete);
            }
        }
        [RelayCommand]
        public void CopyRecipe()
        {
            CopyRecipeEvent?.Invoke(RecipeInfo);
        }
        [RelayCommand]
        public void PasteRecipe()
        {
            PasteRecipeEvent?.Invoke(RecipeInfo);
        }
        [RelayCommand]
        public void OpenRecipe()
        {
            OpenRecipeEvent?.Invoke(RecipeInfo);
        }
        [RelayCommand]
        public void ImportRecipe()
        {
            ImportRecipeEvent?.Invoke(RecipeInfo);
        }
        [RelayCommand]
        public void ExportRecipe()
        {
            ExportRecipeEvent?.Invoke(RecipeInfo);
        }

        [RelayCommand]
        public void ImportRecipeFromExcel()
        {
            ImportRecipeFromExcelEvent?.Invoke(RecipeInfo);
        }

        [RelayCommand]
        public void ExportRecipeToExcel()
        {
            ExportRecipeToExcelEvent?.Invoke(RecipeInfo);
        }
    }
}
