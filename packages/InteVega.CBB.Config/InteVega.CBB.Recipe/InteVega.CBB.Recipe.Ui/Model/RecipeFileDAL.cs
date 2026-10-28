using IntellVega.CBB.EasyConfig;
using IntellVega.CBB.EasyConfig.Json;
using InteVega.CBB.Recipe.Core;
using InteVega.CBB.Recipe.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InteVega.CBB.Recipe
{
    public class RecipeFileDAL : IRecipeServiceDAL
    {
        /// <summary>
        /// 配置
        /// </summary>
        private readonly IEasyConfigCollection _configCollection;

        public RecipeFileDAL(IEasyConfigCollection configCollection)
        {
            _configCollection = configCollection;
        }
        private readonly string _Root = $"../../data/Recipes/";
       
        private readonly string _fileName = "Recipe.json";
        private readonly string _ExportRoot = $"../../data/Recipes/ExportRecipes/";
        private readonly string _ExportFileName = "Recipe.json";
        public RecipeInfo GetRecipeById(string id)
        {
            List<RecipeInfo> dataList = JsonHelper.ReadJsonFile<List<RecipeInfo>>(_Root, _fileName);
            return dataList.FirstOrDefault(f => f.Id == id);
        }
        public bool AddRecipe(RecipeInfo data)
        {
            List<RecipeInfo> recipeInfos = GetRecipeAll();
            if (recipeInfos == null)
            {
                recipeInfos = new List<RecipeInfo>();
            }
            recipeInfos.Add(data);
            // 创建配方文件夹
            if (data.Level == 1)
            {
                string path = $"{_Root}/{data.RecipeName}##{data.Id}";
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                if (!Directory.Exists(path + "/Images"))
                {
                    Directory.CreateDirectory(path + "/Images");
                }
                if (!Directory.Exists(path + "/Templates"))
                {
                    Directory.CreateDirectory(path + "/Templates");
                }
            }
            return JsonHelper.WriteJsonFile(_Root, _fileName, recipeInfos);
        }
        public bool CopeRecipe(RecipeInfo data, string sourceId)
        {
            List<RecipeInfo> recipeInfos = GetRecipeAll();
            if (recipeInfos == null)
            {
                recipeInfos = new List<RecipeInfo>();
            }
            recipeInfos.Add(data);
            // 复制文件夹
            if (Directory.Exists(_Root))
            {
                DirectoryInfo folder = new DirectoryInfo(_Root);
                DirectoryInfo directoryInfo = folder.GetDirectories().FirstOrDefault(f => f.Name.Contains($"##{sourceId}"));
                if (directoryInfo != null)
                {
                    string sourcePaht = $"{_Root}/{directoryInfo.Name}";
                    string path = $"{_Root}/{data.RecipeName}##{data.Id}";
                    CopyFolder(sourcePaht, path, sourceId, data.Id + ".json");
                    return JsonHelper.WriteJsonFile(_Root, _fileName, recipeInfos);
                }
            }
            return false;
        }
        private static int CopyFolder(string sourceFolder, string destFolder, string sourceId, string newFileName)
        {
            try
            {
                if (!System.IO.Directory.Exists(destFolder))
                {
                    System.IO.Directory.CreateDirectory(destFolder);
                }
                string[] files = System.IO.Directory.GetFiles(sourceFolder);
                foreach (string file in files)
                {
                    string name = System.IO.Path.GetFileName(file);
                    string dest = System.IO.Path.Combine(destFolder, name);
                    if (name == (sourceId + ".json"))
                    {
                        dest = System.IO.Path.Combine(destFolder, newFileName);
                    }
                    System.IO.File.Copy(file, dest, true);//复制文件
                }
                string[] folders = System.IO.Directory.GetDirectories(sourceFolder);
                foreach (string folder in folders)
                {
                    string name = System.IO.Path.GetFileName(folder);
                    string dest = System.IO.Path.Combine(destFolder, name);
                    CopyFolder(folder, dest, sourceId, newFileName);//构建目标路径,递归复制文件
                }
                return 1;
            }
            catch (Exception e)
            {

                return 0;
            }
        }
        public bool EditRecipe(List<RecipeInfo> recipeInfos, RecipeInfo data)
        {
            string newPath = $"{_Root}/{data.RecipeName}##{data.Id}";

            if (Directory.Exists(_Root) && data.Level == 0)
            {
                DirectoryInfo folder = new DirectoryInfo(_Root);
                DirectoryInfo directoryInfo = new DirectoryInfo(_Root).GetDirectories().FirstOrDefault(f => f.Name.Contains($"##{data.Id}"));
                if (directoryInfo != null && directoryInfo.Exists)
                {
                    string oldPath = $"{_Root}/{directoryInfo.Name}";
                    Directory.Move(oldPath, newPath);
                }
            }
            return JsonHelper.WriteJsonFile(_Root, _fileName, recipeInfos);
        }

        public JsonConfigCollection GetRecipeJsonConfig(string id)
        {
            JsonConfigCollection configCollection = null;
            if (Directory.Exists(_Root))
            {
                DirectoryInfo folder = new DirectoryInfo(_Root);
                DirectoryInfo directoryInfo = folder.GetDirectories().FirstOrDefault(w => w.Name.Contains($"##{id}"));
                if (directoryInfo != null)
                {
                    var filePath = $"{_Root}/{directoryInfo?.Name}/{id}.json";
                    configCollection = new JsonConfigCollection(filePath, id, true);

                }
            }
            return configCollection;
        }
        public bool RemoveRecipe(RecipeInfo data)
        {
            List<RecipeInfo> recipeInfoList = GetRecipeAll();
            List<RecipeInfo> resultData = new List<RecipeInfo>();
            GetRemoveRecipe(recipeInfoList, data, resultData);
            resultData.Add(recipeInfoList.FirstOrDefault(f => f.Id == data.Id));
            foreach (var item in resultData)
            {
                recipeInfoList.Remove(item);
            }
            if (Directory.Exists(_Root))
            {
                DirectoryInfo folder = new DirectoryInfo(_Root);
                DirectoryInfo[] directoryInfos = folder.GetDirectories();
                foreach (var item in directoryInfos)
                {
                    if (resultData.Any(r => item.Name.Contains($"##{r.Id}")))
                    {
                        item.Delete(true);
                    }
                }
            }
            return JsonHelper.WriteJsonFile(_Root, _fileName, recipeInfoList);
        }
        public bool ImportRecipe(RecipeInfo recipeInfo,List<RecipeInfo> recipeInfos,string targetPath)
        {
            return false;
        }
        private void CopyDirectory(string sourceDir, string targetDir)
        {
            DirectoryInfo dir = new DirectoryInfo(sourceDir);
            DirectoryInfo[] dirs = dir.GetDirectories();

            // If the source directory does not exist, throw an exception.
            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException($"Source directory does not exist or could not be found: {sourceDir}");
            }

            // If the destination directory does not exist, create it.
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = Path.Combine(targetDir, file.Name);
                file.CopyTo(tempPath, false);
            }

            // If copying subdirectories, copy them and their contents to the new location.
            foreach (DirectoryInfo subdir in dirs)
            {
                string tempPath = Path.Combine(targetDir, subdir.Name);
                CopyDirectory(subdir.FullName, tempPath);
            }
        }

        private void GetChildRecipe(RecipeInfo recipeInfo, List<RecipeInfo> recipeAll, ref List<RecipeInfo> recipeInfos)
        {
            if (recipeInfo.Level == 0)
            {
                foreach (var item in recipeAll)
                {
                    if (recipeInfo.Id == item.ParentId)
                    {
                        recipeInfos.Add(item);
                        if (item.Level == 0)
                        {
                            GetChildRecipe(item, recipeAll, ref recipeInfos);
                        }
                    }
                }
            }
        }
        public bool ExportRecipe(RecipeInfo recipeInfo, string targetPath) 
        {
            return false;
        }
        private void GetRemoveRecipe(List<RecipeInfo> recipeInfoList, RecipeInfo data, List<RecipeInfo> resultData)
        {
            foreach (var item in recipeInfoList.Where(f => f.ParentId == data.Id).ToList())
            {
                resultData.Add(item);
                GetRemoveRecipe(recipeInfoList, item, resultData);
            }
        }
        public List<RecipeInfo> GetRecipeAll()
        {
            List<RecipeInfo> dataList = JsonHelper.ReadJsonFile<List<RecipeInfo>>(_Root, _fileName);
            return dataList;
        }


        public TRecipeNode Get<TRecipeNode>(string name)
        {
            return _configCollection.Get<TRecipeNode>(name);

        }
        public TRecipeNode Get<TRecipeNode>()
        {
            return _configCollection.Get<TRecipeNode>();

        }
        public void Set<TRecipeNode>(TRecipeNode config, bool saveToFile = false)
        {
            _configCollection.Set(config);
            if (saveToFile)
            {
                _configCollection.Commit();
            }
        }
        public void Set<TRecipeNode>(string name, TRecipeNode config, bool saveToFile = false)
        {
            _configCollection.Set(name, config);
            if (saveToFile)
            {
                _configCollection.Commit();
            }
        }
    }
}
