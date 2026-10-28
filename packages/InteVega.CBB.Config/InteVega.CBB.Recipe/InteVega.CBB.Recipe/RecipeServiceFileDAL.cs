using IntellVega.CBB.EasyConfig;
using IntellVega.CBB.EasyConfig.Json;
using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.Recipe.Core;
using InteVega.CBB.Recipe.Interface;
using System.IO.Compression;

namespace InteVega.CBB.Recipe
{
    public class RecipeServiceFileDAL : IRecipeServiceDAL
    {
        /// <summary>
        /// 配置
        /// </summary>
        private readonly IEasyConfigCollection _configCollection;

        public RecipeServiceFileDAL(IEasyConfigCollection configCollection)
        {
            _configCollection = configCollection;
        }
        private readonly string _Root = $"../../data/Recipes/";
        private readonly string _fileName = "Recipe.json";
        private readonly string _ExportRoot = $"../../data/Recipes/ExportRecipes/";
        private readonly string _zipRoot = "../../data/Recipes/ExportRecipes1111.zip";
        private readonly string _ExportFileName = "Recipe.json";

        private string FilePath { get { return Path .Combine (_Root , _fileName); } }   
        public RecipeInfo GetRecipeById(string id)
        {
           
            List<RecipeInfo> dataList = AtomicJsonFile.Deserialize<List<RecipeInfo>>(FilePath  );
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
            AtomicJsonFile.Serialize(recipeInfos, FilePath);
            return true;
        }

        public bool ModifyRecipe(RecipeInfo data)
        {
            List<RecipeInfo> recipeInfos = GetRecipeAll();
            if (recipeInfos == null)
            {
                return false;
            }
            for (int i = 0; i < recipeInfos.Count; i++)
            {
                if (recipeInfos[i].Id == data.Id)
                {
                    recipeInfos[i] = data;
                    break;
                }
            }
            AtomicJsonFile.Serialize(recipeInfos, FilePath);
            return true;
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
                    AtomicJsonFile.Serialize(recipeInfos, FilePath);
                    return true;
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
            if (Directory.Exists(_Root) && data.Level != 0)
            {
                string newPath = $"{_Root}/{data.RecipeName}##{data.Id}";
                string oldPath = $"{_Root}/{data.RecipeOldName}##{data.Id}";
                //优先进行全字段匹配，如果未找到，则使用id 进行模糊匹配
                if (!Directory.Exists(oldPath))
                {
                    DirectoryInfo folder = new DirectoryInfo(_Root);
                    var directoryInfo = new DirectoryInfo(_Root).GetDirectories().OrderByDescending(x => x.LastWriteTimeUtc).FirstOrDefault(f => f.Name.Contains($"##{data.Id}"));
                    if (directoryInfo != null)
                        oldPath = $"{_Root}/{directoryInfo.Name}";
                }
                if (!Directory.Exists(oldPath))
                {
                    throw new Exception($"The folder is not exist:{oldPath}");
                }
                if (!string.IsNullOrEmpty(oldPath) && !oldPath.Equals(newPath))
                    Directory.Move(oldPath, newPath);
            }
            data.RecipeOldName = "";
            AtomicJsonFile.Serialize(recipeInfos, FilePath);
            return true;
        }

        public JsonConfigCollection GetRecipeJsonConfig(string id)
        {
            List<RecipeInfo> recipeInfos = GetRecipeAll();
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
                    if (resultData.Any(r => item.Name.Contains($"##{r?.Id}")))
                    {
                        item.Delete(true);
                    }
                }
            }
            AtomicJsonFile.Serialize(recipeInfoList, FilePath);
            return true;
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
            List<RecipeInfo> dataList = AtomicJsonFile.Deserialize<List<RecipeInfo>>(FilePath);
            return dataList;
        }

        public string GetRecipePath(string id) 
        {
            string path = string.Empty;
            if (Directory.Exists(_Root))
            {
                DirectoryInfo folder = new DirectoryInfo(_Root);
                DirectoryInfo[] directoryInfos = folder.GetDirectories();
                path=directoryInfos?.FirstOrDefault(_=>_.Name.Contains($"##{id}"))?.FullName;
            }
            return path;
        }

        public bool ImportRecipe(RecipeInfo recipeInfo, List<RecipeInfo> recipeInfos, string targetPath)
        {
            bool result = false;
            List<RecipeInfo> sourceRecipe = GetRecipeAll()?.ToList();


            //删除当前目录下 ID 重复的Recipe
            recipeInfos.ForEach((r) =>
            {
                var Id = r.NewId ?? r.Id;
                if (sourceRecipe.Any(s => s.Id == Id && s.ParentId==recipeInfo.Id))
                    sourceRecipe.Remove(sourceRecipe.FirstOrDefault(s => s.Id == Id));
            });
            //重新指定上层文件夹
            recipeInfos.Where(_ => !recipeInfos.Any(r => r.Id == _.ParentId)).ToList().ForEach(r =>
            {
                r.ParentId = recipeInfo.Id;
                r.ParentName = recipeInfo.RecipeName;
            });

            #region ID重复的，修改ID
            recipeInfos.ForEach(r =>
            {
                if (sourceRecipe.Any(_ => _.Id == r.Id) && string.IsNullOrWhiteSpace(r.NewId))
                {
                    r.NewId = Guid.NewGuid().ToString();
                }

            });
            #endregion

            #region 重名修改
            recipeInfos.ForEach(r =>
            {
                if (sourceRecipe.Any(_ => _.RecipeName == r.RecipeName && _.ParentId==recipeInfo.Id))
                {
                    int num = 0;
                    do
                    {
                        num++;
                        if (!sourceRecipe.Any(_ => _.RecipeName == r.RecipeName + $"({num})"))
                        {
                            r.NewName = r.RecipeName + $"({num})"; break;
                        }
                    } while (num > 0);
                    foreach (var item in recipeInfos.Where(_ => _.ParentId == r.Id))
                    {
                        item.ParentName = r.NewName;
                    }
                }

            });
            #endregion

            try
            {
                recipeInfos.Where(_ => _.Level == 1).ToList().ForEach(_ =>
                {
                    string id = _.NewId ?? _.Id;
                    string recipeName = _.NewName ?? _.RecipeName;
                    string sourceDir = $"{targetPath}\\{_.RecipeName}##{_.Id}";
                    string targetDir = $"{_Root}\\{recipeName}##{id}";
                    if (string.IsNullOrWhiteSpace(_.NewId))
                        CopyDirectory(sourceDir, targetDir);
                    else
                        CopyDirectory(sourceDir, targetDir, _.Id, _.NewId);
                });
                result = true;
            }
            catch (Exception ex)
            {
                result = false;
            }
            if (result)
            {
                recipeInfos.ForEach((r) =>
                {
                    r.Id = r.NewId ?? r.Id;
                    r.RecipeName = r.NewName ?? r.RecipeName;
                    sourceRecipe.Add(r);
                });
                AtomicJsonFile.Serialize(sourceRecipe , FilePath);
                return true;
            }
            return result;
        }

        public bool ExportRecipe(RecipeInfo recipeInfo,string targetPath)
        {
            bool result = false;
            string path = targetPath+ $"/Recipe{DateTime.Now.ToString("yyyyMMddHHmmss")}/";
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
            #region 遍历出需要导出的Recipe
            List<RecipeInfo> recipeAll = GetRecipeAll()?.ToList();
            List<RecipeInfo> recipeInfos = new List<RecipeInfo>();
            if (recipeInfo.ParentId != null)
                recipeInfos.Add(recipeInfo);
            GetChildRecipe(recipeInfo, recipeAll, ref recipeInfos);
            //保存到文件中
             
            AtomicJsonFile.Serialize(recipeInfos, Path.Combine (path ,_fileName ));
            //return true;
            #endregion

            if (Directory.Exists(_Root))
            {
                foreach (var item in recipeInfos)
                {
                    DirectoryInfo folder = new DirectoryInfo(_Root);
                    DirectoryInfo directoryInfo = folder.GetDirectories().FirstOrDefault(w => w.Name.Contains($"##{item.Id}"));
                    if (directoryInfo != null)
                    {
                        var dirPath = $"{_Root}/{directoryInfo?.Name}";
                        var exportPath = $"{path}/{directoryInfo?.Name}";
                        CopyDirectory(dirPath, exportPath);
                    }
                }
                result = true;
            }
            //CompressFolder(path, _zipRoot);

            return result;
        }
        private void CopyDirectory(string sourceDir, string targetDir,string oldId="",string newId="")
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
                if (file.Name==oldId+".json")
                {
                    tempPath = Path.Combine(targetDir, newId+".json");
                }
                
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

        /// <summary>
        /// 压缩
        /// </summary>
        /// <param name="folderPath"></param>
        /// <param name="zipPath"></param>
        public void CompressFolder(string folderPath, string zipPath)
        {
            if (Directory.Exists(folderPath))
            {
                // 创建Zip文件
                if (File.Exists(zipPath)) File.Delete(zipPath);
                using (ZipArchive zipArchive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
                {
                    
                    // 获取文件夹中的所有文件
                    var files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);
                    List<string> fileList= GetAllFilesAndDirectories(folderPath);
                    foreach (var file in fileList)
                    {
                        // 获取文件相对于文件夹的路径
                        var entryName = file.Substring(file.LastIndexOf("\\") + 1);
                        // 添加文件到Zip文件
                        if (entryName.IndexOf(".")>0)
                        {
                            zipArchive.CreateEntryFromFile(file, entryName);
                        }
                        else
                        {

                            zipArchive.CreateEntry(entryName+"/");
                        }                        
                    }
                }
            }
        }
        public void CreateZipWithEmptyFolder(string zipPath, string folderToCompress)
        {
            using (FileStream zipToOpen = new FileStream(zipPath, FileMode.Create))
            {
                using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create))
                {
                    var directoryPath = folderToCompress;
                    var directoryName = Path.GetFileName(directoryPath);

                    // 创建一个表示空文件夹的ZipArchiveEntry
                    ZipArchiveEntry entry = archive.CreateEntry(directoryName + "/");

                    // 将所有文件添加到压缩文件
                    foreach (var filePath in Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories))
                    {
                        string fileContent = File.ReadAllText(filePath);
                        string fileName = filePath.Replace(directoryPath, "").TrimStart('\\');
                        ZipArchiveEntry fileEntry = archive.CreateEntryFromFile(filePath, fileName);
                    }
                }
            }
        }
        public static List<string> GetAllFilesAndDirectories(string path)
        {
            DirectoryInfo directory = new DirectoryInfo(path);
            List<string> fileList = new List<string>();

            // 获取所有文件夹和文件
            FileSystemInfo[] fileSystemInfos = directory.GetFileSystemInfos();

            foreach (FileSystemInfo fileSystemInfo in fileSystemInfos)
            {
                fileList.Add(fileSystemInfo.FullName);
                if (fileSystemInfo is DirectoryInfo)
                {
                    // 如果是文件夹，则递归获取
                    fileList.AddRange(GetAllFilesAndDirectories(fileSystemInfo.FullName));
                }
            }

            return fileList;
        }
        /// <summary>
        /// 解压
        /// </summary>
        /// <param name="zipPath"></param>
        /// <param name="extractPath"></param>
        public void ExtractFolder(string zipPath, string extractPath)
        {
            if (File.Exists(zipPath))
            {
                // 确保输出目录存在
                Directory.CreateDirectory(extractPath);

                // 打开Zip文件
                using (ZipArchive zipArchive = ZipFile.OpenRead(zipPath))
                {
                    // 遍历Zip文件中的所有条目
                    foreach (var entry in zipArchive.Entries)
                    {
                        // 提取文件到指定目录
                        entry.ExtractToFile(Path.Combine(extractPath, entry.FullName), overwrite: true);
                    }
                }
            }
        }

        public bool OldPathCopyToNewPath()
        {
            throw new NotImplementedException();
        }

        public bool ImportRecipeFromExcel(string filePath)
        {
            throw new NotImplementedException();
        }

        public bool ExportRecipeToExcel(RecipeInfo recipeInfo, List<RecipeContent> recipeContents, string targetPath)
        {
            throw new NotImplementedException();
        }

        List<RecipeContent> IRecipeServiceDAL.ImportRecipeFromExcel(string filePath)
        {
            throw new NotImplementedException();
        }

        public bool RemoveInvalidRecipeById(string recipeId)
        {
            throw new NotImplementedException();
        }

        public bool IsRecipeConfigMissing(string recipeId)
        {
            throw new NotImplementedException();
        }
    }
}
