using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.Prism.UI.Extensions.MessageBox;
using InteVega.CBB.Recipe;
using InteVega.CBB.Recipe.Core;
using InteVega.CBB.Recipe.Interface;
using InteVega.CBB.Recipe.Ui.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Sample
{
    public partial class MainWindowViewModel : ObservableObject
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
        List<MenuNode> _recipeNodeMenuList;
        IRecipe _recipe;
        RecipeInfo _recipeInfo;
        IMessageBoxService _MessageBoxService;
        IServiceProvider _serviceProvider;
        IRecipeService _recipeService;
        public MainWindowViewModel(IRecipeService recipeService, IMessageBoxService messageBoxService, RecipeMenuViewModel recipeMenuViewModel)
        {
            TabItemView = new ObservableCollection<TabItem>();
            _recipeService = recipeService;
            _MessageBoxService = messageBoxService;
            RecipeMenuViewModel = recipeMenuViewModel;
            RecipeMenuViewModel.OldPathCopyToNewPath();
            RecipeMenuViewModel.OutSideLoadMenuTree();
            RecipeMenuViewModel.ImportRecipeFromExcelEvent += recipeContentPross;
            RecipeMenuViewModel.GetRecipeContentByID+= recipeContentWrite;
            RecipeMenuViewModel.GetRecipe = (node, recipe) => OpenRecipe(node, recipe);
            Width = 350;
            ImgSouce = "pack://application:,,,/InteVega.CBB.Recipe.Ui;Component/Resources/Images/icon_ceblsq@2x.png";

        }
        public void OpenRecipe(IRecipeManager recipeNodeService, RecipeInfo recipeInfo)
        {
            _recipe = recipeNodeService.GetCurrentRecipe(recipeInfo.Id);
            _recipeInfo = recipeInfo;
            RecipeName = recipeInfo?.RecipeName;
            LoadRecipeMenu(recipeInfo.Id);
            //BindRecipeMenu(recipeInfo?.Id);
            //LoadUserContral(TabItemDataList.FirstOrDefault(f => f.IsSelect)?.Path);

        }
        [RelayCommand]
        public void MouseDown()
        {
            if (Width == 0)
            {
                Width = 350;
                ImgSouce = "pack://application:,,,/InteVega.CBB.Recipe.Ui;Component/Resources/Images/icon_ceblsq@2x.png";
            }
            else
            {
                Width = 0;
                ImgSouce = "pack://application:,,,/InteVega.CBB.Recipe.Ui;Component/Resources/Images/icon_ceblzk@2x.png";
            }
        }
        [RelayCommand]
        public void OpenTabItem(string id)
        {
            foreach (var item in TabItemDataList)
            {
                item.IsSelect = item.Id == id ? true : false;
                if (item.IsSelect)
                {
                    LoadUserContral(item.Path);
                }
            }
        }
        public void LoadUserContral(string path)
        {
            TabItemView.Clear();
            if (!string.IsNullOrEmpty(path))
            {
                TabItem _tabItem = new TabItem();
                Frame frame = new Frame() { Source = new Uri(path, UriKind.RelativeOrAbsolute) };
                frame.LoadCompleted -= Frame_LoadCompleted;
                frame.LoadCompleted += Frame_LoadCompleted;
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
                dataContext.RequestData(_recipe, _recipeInfo);
            }
        }
        public void LoadRecipeMenu(string recipeId = "bbd33be1-fd5c-49fe-8dae-6591ae83e0f4")
        {
            _recipeNodeMenuList = new List<MenuNode>();
            _recipeNodeMenuList.Add(NewNode("摘要", "SummaryView", recipeId, true));
            _recipeNodeMenuList.Add(NewNode("Wafer Map", "WaferMapView", recipeId));
            _recipeNodeMenuList.Add(NewNode("Aligment", "AlignmentView", recipeId));
            _recipeNodeMenuList.Add(NewNode("运行参数", "RunParamView", recipeId));
            _recipeNodeMenuList.Add(NewNode("Tests", "TestManageView", recipeId));

            //_recipeNodeMenuList.Add(new RecipeNodeMenu { Id = Guid.NewGuid().ToString(), Name = "摘要", Path = "pack://application:,,,/Marvel.Main;component/Recipe/View/SummaryView.xaml", RecipeId = "adf59abc-6c29-4143-8594-4bd5264a660b", IsSelect = true });
            //_recipeNodeMenuList.Add(new RecipeNodeMenu { Id = Guid.NewGuid().ToString(), Name = "Stage Contral", Path = "pack://application:,,,/Marvel.Main;component/Recipe/View/WaferMapView.xaml", RecipeId = "adf59abc-6c29-4143-8594-4bd5264a660b", IsSelect = false });

        }
        /// <summary>
        /// 处理业务信息。
        /// </summary>
        /// <param name="recipeContents"></param>
        private  void  recipeContentPross(List<RecipeContent> recipeContents)
        {
            if (recipeContents == null || recipeContents.Count == 0)
            {
                return;
            }
            var recipeGroups = recipeContents
               .Where(r => !string.IsNullOrWhiteSpace(r.Id))
               .GroupBy(r => r.Id, StringComparer.OrdinalIgnoreCase);
            foreach (var recipeGroup in recipeGroups)
            {
                string recipeId = recipeGroup.Key;
                string folderPath = _recipeService.GetRecipePath(recipeId);

                if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
                {
                    continue;
                }
                Dictionary<string, object> jsonObject = new Dictionary<string, object>
                {
                    ["Id"] = recipeId
                };

                Dictionary<string, Dictionary<string, object>> typeData = new Dictionary<string, Dictionary<string, object>>(StringComparer.OrdinalIgnoreCase);

                foreach (RecipeContent item in recipeGroup)
                {
                    if (string.IsNullOrWhiteSpace(item.RecipeType))
                    {
                        continue;
                    }

                    if (!typeData.ContainsKey(item.RecipeType))
                    {
                        typeData[item.RecipeType] = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    }

                    foreach (var field in item.Fields ?? new Dictionary<string, object>())
                    {
                        typeData[item.RecipeType][field.Key] = field.Value;
                    }
                }

                jsonObject["Types"] = typeData;

                string jsonFilePath = Path.Combine(folderPath, $"{recipeId}.json");
                JsonHelper.SerializerJsonFile(jsonObject, jsonFilePath);
            }


         }

        private List<RecipeContent> recipeContentWrite(string id)
        {
            List<RecipeContent> result = new List<RecipeContent>();

            if (string.IsNullOrWhiteSpace(id))
            {
                return result;
            }

            string folderPath = _recipeService.GetRecipePath(id);
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            {
                return result;
            }

            string jsonFilePath = Path.Combine(folderPath, $"{id}.json");
            if (!File.Exists(jsonFilePath))
            {
                return result;
            }

            Dictionary<string, object> jsonObject = JsonHelper.DeserializeFile<Dictionary<string, object>>(jsonFilePath);
            if (jsonObject == null || !jsonObject.TryGetValue("Types", out object typesObject) || typesObject == null)
            {
                return result;
            }

            Newtonsoft.Json.Linq.JObject typesJObject = typesObject as Newtonsoft.Json.Linq.JObject;
            if (typesJObject == null)
            {
                typesJObject = Newtonsoft.Json.Linq.JObject.FromObject(typesObject);
            }

            foreach (var typeProperty in typesJObject.Properties())
            {
                Dictionary<string, object> fields = typeProperty.Value.ToObject<Dictionary<string, object>>()
                                                    ?? new Dictionary<string, object>();

                result.Add(new RecipeContent
                {
                    Id = id,
                    RecipeType = typeProperty.Name,
                    Fields = fields
                });
            }

            return result;


        }
        private MenuNode NewNode(string name, string view, string recipeId, bool isSelect = false)
        {
            return new MenuNode
            {
                Id = Guid.NewGuid().ToString(),
                Name = name,
                Path = $"pack://application:,,,/Marvel.Main;component/Recipe/View/{view}.xaml",
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

    }
    public interface IBaseRecipePage
    {
        public void RequestData(IRecipe data, RecipeInfo recipe);
    }
    public partial class MenuNode : ObservableObject
    {

        public string Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public string RecipeId { get; set; }
        public int Index { get; set; }
        [ObservableProperty]
        private bool _isSelect;
        [ObservableProperty]
        public Visibility _visible;
    }
    //public class Recipetest
    //{
    //    public string Id { get; set; }

    //    public string RecipeType { get; set; }

    //    public string RecipeName { get; set; }

    //    public string WaferInfoInch { get; set; }

    //    public string WaferInfoThicknessMm { get; set; }

    //    public string WaferInfoEdgeExclusionMm { get; set; }

    //    public string WaferInfoNotchType { get; set; }

    //    public string WaferInfoDirection { get; set; }
    //    public static Recipetest FromDictionary(string id, string recipeType, Dictionary<string, object> jsonObject)
    //    {
    //        Recipetest result = new Recipetest
    //        {
    //            Id = id,
    //            RecipeType = recipeType
    //        };

    //        if (jsonObject == null || jsonObject.Count == 0)
    //        {
    //            return result;
    //        }
    //        foreach (var item in jsonObject)
    //        {
    //            switch (item.Key)
    //            {
    //                case "Recipe_Name":
    //                case "RecipeName":
    //                case "recipe_name":
    //                case "recipe name":
    //                    result.RecipeName = item.Value?.ToString();
    //                    break;

    //                case "Wafer Info.Inch":
    //                    result.WaferInfoInch = item.Value?.ToString();
    //                    break;

    //                case "Wafer Info.Thickness(mm)":
    //                    result.WaferInfoThicknessMm = item.Value?.ToString();
    //                    break;

    //                case "Wafer Info.Edge_Exclusion(mm)":
    //                    result.WaferInfoEdgeExclusionMm = item.Value?.ToString();
    //                    break;

    //                case "Wafer Info.Notch_type":
    //                    result.WaferInfoNotchType = item.Value?.ToString();
    //                    break;

    //                case "Wafer Info.Direction":
    //                    result.WaferInfoDirection = item.Value?.ToString();
    //                    break;

    //                default:
    //                    result.ExtraFields[item.Key] = item.Value;
    //                    break;
    //            }
    //        }

    //        return result;
    //    }
    //    public Dictionary<string, object> ToDictionary()
    //    {
    //        Dictionary<string, object> result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

    //        if (!string.IsNullOrWhiteSpace(RecipeName))
    //        {
    //            result["Recipe_Name"] = RecipeName;
    //        }

    //        if (!string.IsNullOrWhiteSpace(WaferInfoInch))
    //        {
    //            result["Wafer Info.Inch"] = WaferInfoInch;
    //        }

    //        if (!string.IsNullOrWhiteSpace(WaferInfoThicknessMm))
    //        {
    //            result["Wafer Info.Thickness(mm)"] = WaferInfoThicknessMm;
    //        }

    //        if (!string.IsNullOrWhiteSpace(WaferInfoEdgeExclusionMm))
    //        {
    //            result["Wafer Info.Edge_Exclusion(mm)"] = WaferInfoEdgeExclusionMm;
    //        }

    //        if (!string.IsNullOrWhiteSpace(WaferInfoNotchType))
    //        {
    //            result["Wafer Info.Notch_type"] = WaferInfoNotchType;
    //        }

    //        if (!string.IsNullOrWhiteSpace(WaferInfoDirection))
    //        {
    //            result["Wafer Info.Direction"] = WaferInfoDirection;
    //        }

    //        foreach (var item in ExtraFields)
    //        {
    //            result[item.Key] = item.Value;
    //        }

    //        return result;
    //    }
    //    public static Recipetest FromRecipeContent(RecipeContent recipeContent)
    //    {
    //        if (recipeContent == null)
    //        {
    //            return new Recipetest();
    //        }

    //        return FromDictionary(recipeContent.Id, recipeContent.RecipeType, recipeContent.Fields);
    //    }

    //    public RecipeContent ToRecipeContent()
    //    {
    //        return new RecipeContent
    //        {
    //            Id = Id,
    //            RecipeType = RecipeType,
    //            Fields = ToDictionary()
    //        };
    //    }
    //}
}
