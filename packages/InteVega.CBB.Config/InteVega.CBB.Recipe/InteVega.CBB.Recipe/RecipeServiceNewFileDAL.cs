using IntellVega.CBB.EasyConfig;
using IntellVega.CBB.EasyConfig.Json;
using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.Recipe.Core;
using InteVega.CBB.Recipe.Interface;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NPOI.POIFS.Crypt.Dsig;
using NPOI.POIFS.Properties;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.DirectoryServices;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.IO.Compression;

namespace InteVega.CBB.Recipe
{
    public class RecipeServiceNewFileDAL : IRecipeServiceDAL
    {
        private const string FirstColumnFieldName = "Recipe_Name";
        /// <summary>
        /// 配置
        /// </summary>
        private readonly IEasyConfigCollection _configCollection;

        public RecipeServiceNewFileDAL(IEasyConfigCollection configCollection)
        {
            _configCollection = configCollection;
        }
        private readonly string _RootDirectory = $"../../data/";
        private readonly string _RootNew = $"../../data/NewRecipes/";
        private readonly string _Root = $"../../data/Recipes/";
        private readonly string _fileName = "Recipe.json";

        private string FilePath { get { return Path .Combine (_RootNew, _fileName); } }

        /// <summary>
        /// 通过ID获取Recipe
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public RecipeInfo GetRecipeById(string id)
        {
           
            List<RecipeInfo> dataList = AtomicJsonFile.Deserialize<List<RecipeInfo>>(FilePath  );
            return dataList.FirstOrDefault(f => f.Id == id);
        }

        /// <summary>
        /// 添加配方
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool AddRecipe(RecipeInfo data)
        {
            List<RecipeInfo> recipeInfos = GetRecipeAll();
            if (recipeInfos == null)
            {
                recipeInfos = new List<RecipeInfo>();
            }
            recipeInfos.Add(data);

            string path = CreateFileWithFolders(_RootNew, data.ParentId);

            // 创建配方文件夹
            if (data.Level == 1)
            {
                path = $"{path}/{data.RecipeName}##{data.Id}";
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
                string recipeJsonPath = Path.Combine(path, $"{data.Id}.json");
                if (!File.Exists(recipeJsonPath))
                {
                    AtomicJsonFile.WriteText(recipeJsonPath, "{}");
                }
            }
            else
            {
                path = $"{path}/{data.RecipeName}";
                Directory.CreateDirectory(path);
            }  
            AtomicJsonFile.Serialize(recipeInfos, FilePath);
            return true;
        }

        /// <summary>
        /// 未调用
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 复制配方
        /// </summary>
        /// <param name="data"></param>
        /// <param name="sourceId"></param>
        /// <returns></returns>
        public bool CopeRecipe(RecipeInfo data, string sourceId)
        {
            List<RecipeInfo> recipeInfos = GetRecipeAll();
            if (recipeInfos == null)
            {
                recipeInfos = new List<RecipeInfo>();
            }
            recipeInfos.Add(data);
            var folderPath ="/"+ EnsureFolderPathExists(sourceId, GetRecipeAll());
            var CopyToPath = "/" + EnsureFolderPathExists(data.ParentId, GetRecipeAll());
            //folderPath =ExtractRecipePathWithoutRoot(folderPath);
            folderPath = _RootNew + folderPath;
            CopyToPath = _RootNew + CopyToPath;
            // 复制文件夹
            if (Directory.Exists(folderPath))
            {
                DirectoryInfo folder = new DirectoryInfo(folderPath);
                DirectoryInfo directoryInfo = folder.GetDirectories().FirstOrDefault(f => f.Name.Contains($"##{sourceId}"));
                if (directoryInfo != null)
                {
                    string sourcePaht = $"{folderPath}/{directoryInfo.Name}";
                    string path = $"{CopyToPath}{data.RecipeName}##{data.Id}";
                    CopyFolder(sourcePaht, path, sourceId, data.Id + ".json");
                    AtomicJsonFile.Serialize(recipeInfos, FilePath);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 复制文件夹
        /// </summary>
        /// <param name="sourceFolder"></param>
        /// <param name="destFolder"></param>
        /// <param name="sourceId"></param>
        /// <param name="newFileName"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 重命名
        /// </summary>
        /// <param name="recipeInfos"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public bool EditRecipe(List<RecipeInfo> recipeInfos, RecipeInfo data)
        {
            var folderPath = EnsureFolderPathExists(data.Id, GetRecipeAll());
            if ((Directory.Exists( _RootNew + folderPath) && data.Level != 0) || (folderPath == "" && data.Level != 0))//如果是文件 且有路径 或者路径为空且是文件
            {
                string newName = $"{data.RecipeName}##{data.Id}";
                string oldPath = $"{folderPath}/{data.RecipeOldName}##{data.Id}";
                RenameFolder(_RootNew + oldPath, newName);
            }
            else//文件夹重命名
            {
                if (folderPath == "" && data.RecipeOldName == "Recipes")//Recipes不允许更改
                {
                    return false;
                }
                RenameFolder(_RootNew + folderPath, data.RecipeName);
            }
            data.RecipeOldName = "";
            AtomicJsonFile.Serialize(recipeInfos, FilePath);
            return true;
        }

        public JsonConfigCollection GetRecipeJsonConfig(string id)
        {
            List<RecipeInfo> recipeInfos = GetRecipeAll();
            JsonConfigCollection configCollection = null;
            var folderPath = EnsureFolderPathExists(id, GetRecipeAll());
            folderPath = _RootNew + folderPath;
            if (Directory.Exists(folderPath))
            {
                DirectoryInfo folder = new DirectoryInfo(folderPath);
                DirectoryInfo directoryInfo = folder.GetDirectories().FirstOrDefault(w => w.Name.Contains($"##{id}"));
                if (directoryInfo != null)
                {
                    var filePath = $"{folderPath}{directoryInfo?.Name}/{id}.json";
                    configCollection = new JsonConfigCollection(filePath, id, true);
                }
            }
            return configCollection;
        }

        /// <summary>
        /// 删除recipe
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool RemoveRecipe(RecipeInfo data)
        {
            List<RecipeInfo> recipeInfoList = GetRecipeAll();
            List<RecipeInfo> resultData = new List<RecipeInfo>();
            GetRemoveRecipe(recipeInfoList, data, resultData);
            resultData.Add(recipeInfoList.FirstOrDefault(f => f.Id == data.Id));
            var folderPath = "/" + EnsureFolderPathExists(data.Id, GetRecipeAll());
            folderPath = ExtractRecipePathWithoutRoot(folderPath);
            folderPath = _RootNew + folderPath;
            foreach (var item in resultData)
            {
                recipeInfoList.Remove(item);
            }
            if (Directory.Exists(folderPath))
            {
                if (data.Level == 0)//删除的是文件夹 直接把子目录的文件夹删除
                {
                    string path = EnsureFolderPathExists(data.Id, GetRecipeAll());
                    SafeDeleteFolder(folderPath + path);
                }
                else
                {
                    string parentPath = EnsureFolderPathExists(data.Id, GetRecipeAll());
                    DirectoryInfo folder = new DirectoryInfo(folderPath + parentPath);
                    DirectoryInfo[] directoryInfos = folder.GetDirectories();
                    foreach (var item in directoryInfos)
                    {
                        if (resultData.Any(r => item.Name.Contains($"##{r?.Id}")))
                        {
                            item.Delete(true);
                        }
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

        /// <summary>
        /// 获取所有的配方
        /// </summary>
        /// <returns></returns>
        public List<RecipeInfo> GetRecipeAll()
        {
            List<RecipeInfo> dataList = AtomicJsonFile.Deserialize<List<RecipeInfo>>(FilePath);
            return dataList;
        }

        /// <summary>
        /// 获取配方地址
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public string GetRecipePath(string id) 
        {
            string path = string.Empty;
            var folderPath = EnsureFolderPathExists(id, GetRecipeAll());
            folderPath = _RootNew + folderPath;
            if (Directory.Exists(folderPath))
            {
                DirectoryInfo folder = new DirectoryInfo(folderPath);
                DirectoryInfo[] directoryInfos = folder.GetDirectories();
                path=directoryInfos?.FirstOrDefault(_=>_.Name.Contains($"##{id}"))?.FullName;
            }
            return path;
        }

        /// <summary>
        /// 导入配方
        /// </summary>
        /// <param name="recipeInfo"></param>
        /// <param name="recipeInfos"></param>
        /// <param name="targetPath"></param>
        /// <returns></returns>
        public bool ImportRecipe(RecipeInfo recipeInfo, List<RecipeInfo> recipeInfos, string targetPath)
        {
            bool result = false;
            List<RecipeInfo> sourceRecipe = GetRecipeAll()?.ToList();
            List<RecipeInfo> sourceRecipe2 = GetRecipeAll()?.ToList();//用来判断重命名
            string  hardwareId = MachineIdentifier.DeviceId;
            //删除当前目录下 ID 重复的Recipe
            recipeInfos.ForEach((r) =>
            {
                var Id = r.NewId ?? r.Id;
                if (sourceRecipe.Any(s => s.Id == Id && s.ParentId == recipeInfo.Id))
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
                if (r.HardwareId != hardwareId)
                {
                    r.ImportTime = DateTime.Now;
                }
                else
                {
                    r.HardwareId = "";
                }
            });
            #endregion


            #region 重名修改
            recipeInfos.ForEach(r =>
            {
                if (sourceRecipe2.Any(_ => _.RecipeName == r.RecipeName && _.ParentId==recipeInfo.Id && _.Level == r.Level))
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
                sourceRecipe2.Add(r);
            });
            #endregion

            try
            {
                recipeInfos.Where(_ => _.Level == 1).ToList().ForEach(_ =>
                {
                    string id = _.NewId ?? _.Id;
                    string recipeName = _.NewName ?? _.RecipeName;
                    string Path = EnsureFolderPathExists(_.Id, recipeInfos);
                    string sourceDir = $"{targetPath}\\{_.RecipeName}##{_.Id}";
                    string targetDir = "";
                    if (recipeInfo.RecipeName == "Recipes")//代表的是跟目录
                    {
                        targetDir = $"{_RootNew}\\{Path}\\{recipeName}##{id}";
                    }
                    else
                    {
                        string TreePath = EnsureFolderPathExists(recipeInfo.Id, sourceRecipe);//当前要拷贝的文件夹母目录
                        targetDir = $"{_RootNew}{TreePath}\\{Path}\\{recipeName}##{id}";
                    }
                   
                    if (string.IsNullOrWhiteSpace(_.NewId))
                    {
                        CopyDirectory(sourceDir, targetDir);
                       // UpdateRecipeJsonId(System.IO.Path.Combine(targetDir, _.NewId + ".json"), _.NewId);

                    }            
                    else
                    {
                        CopyDirectory(sourceDir, targetDir, _.Id, _.NewId);
                        UpdateRecipeJsonId(System.IO.Path.Combine(targetDir, _.NewId + ".json"), _.NewId);
                    }
                                     
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

        /// <summary>
        /// 导出配方
        /// </summary>
        /// <param name="recipeInfo"></param>
        /// <param name="targetPath"></param>
        /// <returns></returns>
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
            if (recipeInfo.ParentId != null && recipeInfo.Level == 1)
                recipeInfos.Add(recipeInfo);
            GetChildRecipe(recipeInfo, recipeAll, ref recipeInfos);
            recipeInfos = recipeInfos.Where(a => a.Level == 1).ToList();
            string hardwareId = MachineIdentifier.DeviceId;
            foreach (var ri in recipeInfos)
            {
                ri.HardwareId = hardwareId;
            }
            
            AtomicJsonFile.Serialize(recipeInfos, Path.Combine (path ,_fileName ));
            #endregion

            if (Directory.Exists(_RootDirectory))
            {
                foreach (var item in recipeInfos)
                {
                    var folderPath = EnsureFolderPathExists(item.Id, GetRecipeAll());
                    folderPath = _RootNew + folderPath;
                    DirectoryInfo folder = new DirectoryInfo(folderPath);
                    DirectoryInfo directoryInfo = folder.GetDirectories().FirstOrDefault(w => w.Name.Contains($"##{item.Id}"));
                    if (directoryInfo != null)
                    {
                        var dirPath = $"{folderPath}/{directoryInfo?.Name}";
                        var exportPath = $"{path}/{directoryInfo?.Name}";
                        CopyDirectory(dirPath, exportPath);
                    }
                }
                result = true;
            }
            return result;
        }

        /// <summary>
        /// 复制文件夹到新的位置
        /// </summary>
        /// <param name="sourceDir"></param>
        /// <param name="targetDir"></param>
        /// <param name="oldId"></param>
        /// <param name="newId"></param>
        /// <exception cref="DirectoryNotFoundException"></exception>
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

        private static void UpdateRecipeJsonId(string recipeJsonPath, string newId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(newId))
                {
                    LogHelper.LogInfo($"Update recipe json id skipped, newId is empty. RecipeJsonPath={recipeJsonPath}");
                    return;
                }

                if (!File.Exists(recipeJsonPath))
                {
                    LogHelper.LogInfo($"Update recipe json id skipped, file not found. RecipeJsonPath={recipeJsonPath}, NewId={newId}");
                    return;
                }

                string json = File.ReadAllText(recipeJsonPath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    LogHelper.LogInfo($"Update recipe json id skipped, json is empty. RecipeJsonPath={recipeJsonPath}, NewId={newId}");
                    return;
                }

                JObject recipeJson = JObject.Parse(json);
                if (recipeJson["RecipeElementsInfo"] is not JObject recipeElementsInfo)
                {
                    LogHelper.LogInfo($"Update recipe json id skipped, RecipeElementsInfo node not found. RecipeJsonPath={recipeJsonPath}, NewId={newId}");
                    return;
                }

                JToken idToken = recipeElementsInfo["Id"];
                if (idToken == null)
                {
                    LogHelper.LogInfo($"RecipeElementsInfo.Id not found, add new Id. RecipeJsonPath={recipeJsonPath}, NewId={newId}");
                    return;
                }
                else
                {
                    string oldId = idToken.ToString();
                    recipeElementsInfo["Id"] = newId;
                    LogHelper.LogInfo($"RecipeElementsInfo.Id updated. RecipeJsonPath={recipeJsonPath}, OldId={oldId}, NewId={newId}");
                }

                AtomicJsonFile.WriteText(recipeJsonPath, recipeJson.ToString(Formatting.Indented));
            }
            catch (Exception ex)
            {
                LogHelper.LogError(ex, $"Update recipe json id failed. RecipeJsonPath={recipeJsonPath}, NewId={newId}");
            }
        }

        /// <summary>
        /// 获取子配方
        /// </summary>
        /// <param name="recipeInfo"></param>
        /// <param name="recipeAll"></param>
        /// <param name="recipeInfos"></param>
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

        /// <summary>
        /// 旧配方拷贝到新配方
        /// </summary>
        /// <returns></returns>
        public bool OldPathCopyToNewPath()
        {
            try
            {
                #region 遍历出需要导出的Recipe
                string path = Path.Combine(_Root, _fileName);
                List<RecipeInfo> recipeAll = AtomicJsonFile.Deserialize<List<RecipeInfo>>(path);
                List<RecipeInfo> recipeInfos = new List<RecipeInfo>();
                recipeInfos = recipeAll;
                //GetChildRecipe(null, recipeAll, ref recipeInfos);
                #endregion

                if (Directory.Exists(_RootNew))
                {
                    AtomicJsonFile.Serialize(recipeAll, Path.Combine(_RootNew, _fileName));//保存Recipe.json到新目录下
                    foreach (var item in recipeAll)
                    {
                        var folderPath = EnsureFolderOldPathExists(item.Id, recipeAll);
                        DirectoryInfo folder = new DirectoryInfo(_Root);
                        DirectoryInfo directoryInfo = folder.GetDirectories().FirstOrDefault(w => w.Name.Contains($"##{item.Id}"));
                        if (directoryInfo != null)
                        {
                            var dirPath = $"{folder}/{directoryInfo?.Name}";
                            var exportPath = $"{_RootNew}/{ExtractRecipePathWithoutRoot("/"+folderPath)}/{directoryInfo?.Name}";
                            CopyDirectory(dirPath, exportPath);
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// 递归旧文件夹recipes路径
        /// </summary>
        /// <param name="targetParentId">目标父级ID</param>
        /// <returns>文件夹路径字符串</returns>
        private string EnsureFolderOldPathExists(string targetParentId, List<RecipeInfo> recipeInfos)
        {
            var pathStack = new Stack<string>();
            BuildFolderPath(targetParentId, pathStack, recipeInfos);

            // 从根目录开始创建文件夹
            var currentPath = "";
            var pathList = pathStack.ToList();
            if (pathList.Contains("Recipes"))
            {
                pathList.Remove("Recipes");
            }
            string sPath = _Root;
            foreach (var folderName in pathList)
            {
                sPath += folderName + "/";
            }
            currentPath = sPath;
            return currentPath;
        }

        /// <summary>
        /// 创建文件并递归创建上级文件夹
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="parentId">父级ID</param>
        /// <returns>创建的文件对象</returns>
        public string CreateFileWithFolders(string fileName, string parentId)
        {
            // 1. 递归创建或获取文件夹路径
            var folderPath = fileName + EnsureFolderPathExists(parentId, GetRecipeAll());

            return folderPath;
        }

        /// <summary>
        /// 确保文件夹路径存在（递归创建）
        /// </summary>
        /// <param name="targetParentId">目标父级ID</param>
        /// <returns>文件夹路径字符串</returns>
        private string EnsureFolderPathExists(string targetParentId, List<RecipeInfo> recipeInfos,string ParentName = "")
        {
            var pathStack = new Stack<string>();
            BuildFolderPath(targetParentId, pathStack, recipeInfos);

            // 从根目录开始创建文件夹
            var currentPath = "";
            var pathList = pathStack.ToList();
            if (pathList.Contains("Recipes"))
            {
                pathList.Remove("Recipes");
            }
            if (ParentName != "" && pathList.Contains(ParentName))
            {
                pathList.Remove(ParentName);
            }
            string sPath = "";
            foreach (var folderName in pathList)
            {
                sPath += folderName + "/";
            }
            currentPath = sPath;
            return currentPath;
        }

        /// <summary>
        /// 递归构建文件夹路径
        /// </summary>
        /// <param name="parentId">当前父级ID</param>
        /// <param name="pathStack">路径栈</param>
        private void BuildFolderPath(string parentId, Stack<string> pathStack, List<RecipeInfo> recipeInfos)
        {
            if (string.IsNullOrEmpty(parentId)) return;

            var parentItem = recipeInfos.FirstOrDefault(x => x.Id == parentId);
            if (parentItem == null) return;

            // 如果父级是文件夹，添加到路径中
            if (parentItem.Level == 0)
            {
                pathStack.Push(parentItem.RecipeName);
            }

            // 递归向上查找
            if (!string.IsNullOrEmpty(parentItem.ParentId))
            {
                BuildFolderPath(parentItem.ParentId, pathStack, recipeInfos);
            }
        }

        /// <summary>
        /// 重载：不包含Recipes本身
        /// </summary>
        /// <param name="fullPath"></param>
        /// <returns></returns>
        public static string ExtractRecipePathWithoutRoot(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath))
                return string.Empty;

            string searchPattern = @"/Recipes/";
            int recipeIndex = fullPath.IndexOf(searchPattern, StringComparison.OrdinalIgnoreCase);

            if (recipeIndex == -1)
                return string.Empty;

            // 跳过Recipes\，提取后面的路径
            int startIndex = recipeIndex + searchPattern.Length;
            return fullPath.Substring(startIndex);
        }

        /// <summary>
        /// 重载：不包含NewRecipes本身
        /// </summary>
        /// <param name="fullPath"></param>
        /// <returns></returns>
        public static string ExtractNewRecipesPathWithoutRoot(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath))
                return string.Empty;

            string searchPattern = @"\NewRecipes\";
            int recipeIndex = fullPath.IndexOf(searchPattern, StringComparison.OrdinalIgnoreCase);

            if (recipeIndex == -1)
                return string.Empty;

            // 跳过Recipes\，提取后面的路径
            int startIndex = recipeIndex + searchPattern.Length;
            return fullPath.Substring(startIndex);
        }

        /// <summary>
        /// 安全删除文件夹，处理各种异常情况
        /// </summary>
        public static DeleteResult SafeDeleteFolder(string folderPath, bool recursive = true)
        {
            var result = new DeleteResult { FolderPath = folderPath };

            try
            {
                // 1. 验证路径
                if (string.IsNullOrWhiteSpace(folderPath))
                {
                    result.Error = "文件夹路径不能为空";
                    return result;
                }

                if (!Directory.Exists(folderPath))
                {
                    result.Warning = "文件夹不存在";
                    result.IsDeleted = true; // 不存在也算删除成功
                    return result;
                }

                // 2. 检查权限
                if (!HasDeletePermission(folderPath))
                {
                    result.Error = "没有删除权限";
                    return result;
                }

                // 3. 删除文件夹
                Directory.Delete(folderPath, recursive);

                result.IsDeleted = true;
                result.Message = "删除成功";
                result.DeletedTime = DateTime.Now;
            }
            catch (IOException ex)
            {
                result.Error = $"文件被占用: {ex.Message}";
            }
            catch (UnauthorizedAccessException ex)
            {
                result.Error = $"权限不足: {ex.Message}";
            }
            catch (Exception ex)
            {
                result.Error = $"删除失败: {ex.Message}";
            }

            return result;
        }

        private static bool HasDeletePermission(string folderPath)
        {
            try
            {
                // 尝试创建测试文件来检查权限
                var testFile = Path.Combine(folderPath, "__test_delete_permission.tmp");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 基础文件夹重命名
        /// </summary>
        public static bool RenameFolder(string folderPath, string newName)
        {
            //try
            //{
                DirectoryInfo dirInfo = new DirectoryInfo(folderPath);

                if (!dirInfo.Exists)
                    throw new DirectoryNotFoundException($"文件夹不存在: {folderPath}");

                string newPath = Path.Combine(dirInfo.Parent.FullName, newName);

                // 使用MoveTo方法
                dirInfo.MoveTo(newPath);

                return true;
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine($"❌ 重命名失败: {ex.Message}");
            //    return false;
            //}
        }

        public bool ExportRecipeToExcel(RecipeInfo recipeInfo, List<RecipeContent> recipeContents, string targetPath)
        {
            try
            {
                if (recipeInfo == null || string.IsNullOrWhiteSpace(targetPath))
                {
                    return false;
                }

                if (recipeContents == null || recipeContents.Count == 0)
                {
                    return false;
                }

                IWorkbook workbook;
                if (File.Exists(targetPath))
                {
                    using FileStream readStream = new FileStream(targetPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    workbook = WorkbookFactory.Create(readStream);
                }
                else
                {
                    workbook = new XSSFWorkbook();
                }

                var sheetGroups = recipeContents
                    .Where(x => x != null && !string.IsNullOrWhiteSpace(x.RecipeType))
                    .GroupBy(x => x.RecipeType, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                foreach (var sheetGroup in sheetGroups)
                {
                    string sheetName = GetSafeSheetName(sheetGroup.Key);
                    ISheet sheet = workbook.GetSheet(sheetName);

                    List<string> fieldKeys = sheetGroup
                                           .SelectMany(x => x.Fields?.Keys ?? Enumerable.Empty<string>())
                                           .Where(x => !string.Equals(x, FirstColumnFieldName, StringComparison.OrdinalIgnoreCase))
                                           .Distinct(StringComparer.OrdinalIgnoreCase)
                                           .ToList();

                    List<string> expectedHeaders = new List<string> { FirstColumnFieldName };
                    expectedHeaders.AddRange(fieldKeys);

                    bool rewriteSheet = false;
                    bool needSetColumnWidth = false;

                    if (sheet != null)
                    {
                        IRow headerRow = sheet.GetRow(0);
                        if (headerRow == null)
                        {
                            rewriteSheet = true;
                        }
                        else
                        {
                            List<string> existingHeaders = ReadHeaderRow(headerRow);
                            rewriteSheet = !AreHeadersEqual(existingHeaders, expectedHeaders);
                        }
                    }

                    if (sheet == null)
                    {
                        sheet = workbook.CreateSheet(sheetName);
                        WriteHeaderRow(sheet, expectedHeaders);
                        WriteSheetData(sheet, sheetGroup.ToList(), fieldKeys, 1);
                        needSetColumnWidth = true;
                    }
                    else if (rewriteSheet)
                    {
                        int sheetIndex = workbook.GetSheetIndex(sheet);
                        workbook.RemoveSheetAt(sheetIndex);
                        workbook.CreateSheet(sheetName);
                        workbook.SetSheetOrder(sheetName, sheetIndex);

                        sheet = workbook.GetSheet(sheetName);

                        WriteHeaderRow(sheet, expectedHeaders);
                        WriteSheetData(sheet, sheetGroup.ToList(), fieldKeys, 1);
                        needSetColumnWidth = true;
                    }
                    else
                    {
                        if (sheet.GetRow(0) == null)
                        {
                            WriteHeaderRow(sheet, expectedHeaders);
                            WriteSheetData(sheet, sheetGroup.ToList(), fieldKeys, 1);
                            needSetColumnWidth = true;
                        }
                        else
                        {
                            int startRowIndex = sheet.LastRowNum + 1;
                            if (startRowIndex < 1)
                            {
                                startRowIndex = 1;
                            }

                            WriteSheetData(sheet, sheetGroup.ToList(), fieldKeys, startRowIndex);
                        }
                    }

                    if (needSetColumnWidth)
                    {
                        SetColumnWidths(sheet, expectedHeaders, sheetGroup.ToList(), fieldKeys);
                    }
                }

                using FileStream writeStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None);
                workbook.Write(writeStream);
                return true;
            }
            catch
            {
                return false;
            }

        }

        public List<RecipeContent> ImportRecipeFromExcel(string filePath)
        {
            List<RecipeContent> result = new List<RecipeContent>();

            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return result;
            }
            try
            {
                using FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                IWorkbook workbook = WorkbookFactory.Create(stream);

                for (int sheetIndex = 0; sheetIndex < workbook.NumberOfSheets; sheetIndex++)
                {
                    ISheet sheet = workbook.GetSheetAt(sheetIndex);
                    if (sheet == null)
                    {
                        continue;
                    }
                    IRow headerRow = sheet.GetRow(sheet.FirstRowNum);
                    if (headerRow == null)
                    {
                        throw new Exception($"Sheet [{sheet.SheetName}] header row is empty.");
                    }
                    string firstHeader = GetCellValue(headerRow.GetCell(0))?.Trim();
                    if (!IsFirstColumnHeader(firstHeader))
                    {
                        throw new Exception($"Sheet [{sheet.SheetName}] format is invalid. The first cell of the first row must contain {FirstColumnFieldName}.");
                    }

                    List<string> headers = new List<string>();
                    for (int colIndex = 0; colIndex < headerRow.LastCellNum; colIndex++)
                    {
                        string header = GetCellValue(headerRow.GetCell(colIndex))?.Trim();
                        headers.Add(header ?? string.Empty);
                    }
                    for (int rowIndex = sheet.FirstRowNum + 1; rowIndex <= sheet.LastRowNum; rowIndex++)
                    {
                        IRow dataRow = sheet.GetRow(rowIndex);
                        if (dataRow == null)
                        {
                            continue;
                        }

                        object recipeIdValue = GetCellObjectValue(dataRow.GetCell(0));
                        if (recipeIdValue == null || string.IsNullOrWhiteSpace(recipeIdValue.ToString()))
                        {
                            continue;
                        }

                        RecipeContent recipeContent = new RecipeContent
                        {
                            RecipeType = sheet.SheetName
                        };
                        recipeContent.Fields[FirstColumnFieldName] = recipeIdValue.ToString();

                        for (int colIndex = 1; colIndex < headers.Count; colIndex++)
                        {
                            string fieldName = headers[colIndex];
                            if (string.IsNullOrWhiteSpace(fieldName))
                            {
                                continue;
                            }

                            object fieldValue = GetCellObjectValue(dataRow.GetCell(colIndex));
                            recipeContent.Fields[fieldName] = fieldValue;
                        }

                        result.Add(recipeContent);
                    }
                }
            }
            catch (IOException ex)
            {
                throw new Exception($"Excel file is in use or cannot be accessed: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to import Excel: {ex.Message}", ex);
            }
            return result;
        }

        public bool RemoveInvalidRecipeById(string recipeId)
        {
            if (string.IsNullOrWhiteSpace(recipeId))
            {
                return false;
            }

            List<RecipeInfo> recipeInfos = GetRecipeAll();
            if (recipeInfos == null || recipeInfos.Count == 0)
            {
                return false;
            }

            RecipeInfo recipeInfo = recipeInfos.FirstOrDefault(x => x.Id == recipeId);
            if (recipeInfo == null)
            {
                return false;
            }

            return RemoveRecipe(recipeInfo);
        }

        #region  excel helper methods

        private static bool IsFirstColumnHeader(string header)
        {
            if (string.IsNullOrWhiteSpace(header))
            {
                return false;
            }

            string normalizedHeader = header
                .Trim()
                .Replace("_", string.Empty)
                .Replace(" ", string.Empty)
                .Replace("-", string.Empty);

            string normalizedFieldName = FirstColumnFieldName
                .Replace("_", string.Empty)
                .Replace(" ", string.Empty)
                .Replace("-", string.Empty);

            return string.Equals(normalizedHeader, normalizedFieldName, StringComparison.OrdinalIgnoreCase);
        }
        private static string GetCellValue(ICell cell)
        {
            if (cell == null)
            {
                return string.Empty;
            }

            return cell.CellType switch
            {
                CellType.String => cell.StringCellValue?.Trim() ?? string.Empty,
                CellType.Numeric => DateUtil.IsCellDateFormatted(cell)
                    ? string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:yyyy-MM-dd HH:mm:ss}", cell.DateCellValue)
                    : cell.NumericCellValue.ToString(System.Globalization.CultureInfo.InvariantCulture),
                CellType.Boolean => cell.BooleanCellValue.ToString(),
                CellType.Formula => GetFormulaCellStringValue(cell),
                CellType.Blank => string.Empty,
                _ => cell.ToString()?.Trim() ?? string.Empty
            };
        }
        private static object GetCellObjectValue(ICell cell)
        {
            if (cell == null)
            {
                return string.Empty;
            }

            return cell.CellType switch
            {
                CellType.String => cell.StringCellValue?.Trim() ?? string.Empty,
                CellType.Numeric => DateUtil.IsCellDateFormatted(cell)
                    ? cell.DateCellValue
                    : cell.NumericCellValue,
                CellType.Boolean => cell.BooleanCellValue,
                CellType.Formula => GetFormulaCellObjectValue(cell),
                CellType.Blank => string.Empty,
                _ => cell.ToString()?.Trim() ?? string.Empty
            };
        }
        private static string GetFormulaCellStringValue(ICell cell)
        {
            return cell.CachedFormulaResultType switch
            {
                CellType.String => cell.StringCellValue?.Trim() ?? string.Empty,
                CellType.Numeric => DateUtil.IsCellDateFormatted(cell)
                    ? string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:yyyy-MM-dd HH:mm:ss}", cell.DateCellValue)
                    : cell.NumericCellValue.ToString(System.Globalization.CultureInfo.InvariantCulture),
                CellType.Boolean => cell.BooleanCellValue.ToString(),
                CellType.Blank => string.Empty,
                _ => cell.ToString()?.Trim() ?? string.Empty
            };
        }
        private static object GetFormulaCellObjectValue(ICell cell)
        {
            return cell.CachedFormulaResultType switch
            {
                CellType.String => cell.StringCellValue?.Trim() ?? string.Empty,
                CellType.Numeric => DateUtil.IsCellDateFormatted(cell)
                    ? cell.DateCellValue
                    : cell.NumericCellValue,
                CellType.Boolean => cell.BooleanCellValue,
                CellType.Blank => string.Empty,
                _ => cell.ToString()?.Trim() ?? string.Empty
            };
        }
        private static string GetSafeSheetName(string sheetName)
        {
            if (string.IsNullOrWhiteSpace(sheetName))
            {
                return "Sheet";
            }

            char[] invalidChars = new[] { '\\', '/', '?', '*', '[', ']', ':' };
            string result = sheetName.Trim();

            foreach (char c in invalidChars)
            {
                result = result.Replace(c, '_');
            }

            if (result.Length > 31)
            {
                result = result.Substring(0, 31);
            }

            return string.IsNullOrWhiteSpace(result) ? "Sheet" : result;
        }
        private static List<string> ReadHeaderRow(IRow headerRow)
        {
            List<string> headers = new List<string>();
            if (headerRow == null)
            {
                return headers;
            }

            for (int i = 0; i < headerRow.LastCellNum; i++)
            {
                headers.Add(GetCellValue(headerRow.GetCell(i))?.Trim() ?? string.Empty);
            }

            while (headers.Count > 0 && string.IsNullOrWhiteSpace(headers[^1]))
            {
                headers.RemoveAt(headers.Count - 1);
            }

            return headers;
        }
        private static bool AreHeadersEqual(List<string> existingHeaders, List<string> expectedHeaders)
        {
            if (existingHeaders == null || expectedHeaders == null)
            {
                return false;
            }

            if (existingHeaders.Count != expectedHeaders.Count)
            {
                return false;
            }

            for (int i = 0; i < expectedHeaders.Count; i++)
            {
                if (!string.Equals(existingHeaders[i]?.Trim(), expectedHeaders[i]?.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }
        private static void ClearSheet(ISheet sheet)
        {
            for (int rowIndex = sheet.LastRowNum; rowIndex >= 0; rowIndex--)
            {
                IRow row = sheet.GetRow(rowIndex);
                if (row != null)
                {
                    sheet.RemoveRow(row);
                }
            }

            if (sheet.LastRowNum > 0)
            {
                sheet.ShiftRows(1, sheet.LastRowNum, -sheet.LastRowNum);
            }
        }
        private static void WriteHeaderRow(ISheet sheet, List<string> headers)
        {
            IRow headerRow = sheet.CreateRow(0);
            for (int i = 0; i < headers.Count; i++)
            {
                headerRow.CreateCell(i).SetCellValue(headers[i]);
            }
        }
        private static void WriteSheetData(ISheet sheet, List<RecipeContent> contents, List<string> fieldKeys, int startRowIndex)
        {
            int rowIndex = startRowIndex;

            foreach (RecipeContent content in contents)
            {
                IRow row = sheet.CreateRow(rowIndex++);

                object firstColumnValue = string.Empty;
                if (content.Fields != null &&
                    content.Fields.TryGetValue(FirstColumnFieldName, out object fieldValue) &&
                    fieldValue != null)
                {
                    firstColumnValue = fieldValue;
                }

                SetCellValue(row.CreateCell(0), firstColumnValue);

                for (int i = 0; i < fieldKeys.Count; i++)
                {
                    string key = fieldKeys[i];
                    object value = null;

                    if (content.Fields != null && content.Fields.TryGetValue(key, out object currentFieldValue))
                    {
                        value = currentFieldValue;
                    }

                    SetCellValue(row.CreateCell(i + 1), value);
                }
            }
        }
        private static void SetCellValue(ICell cell, object value)
        {
            if (cell == null)
            {
                return;
            }

            if (value == null)
            {
                cell.SetCellValue(string.Empty);
                return;
            }
            switch (value)
            {
                case DateTime dateTime:
                    cell.SetCellValue(dateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                    break;
                case bool boolValue:
                    cell.SetCellValue(boolValue);
                    break;
                case byte byteValue:
                    cell.SetCellValue(byteValue);
                    break;
                case short shortValue:
                    cell.SetCellValue(shortValue);
                    break;
                case int intValue:
                    cell.SetCellValue(intValue);
                    break;
                case long longValue:
                    cell.SetCellValue(Convert.ToDouble(longValue));
                    break;
                case float floatValue:
                    cell.SetCellValue(floatValue);
                    break;
                case double doubleValue:
                    cell.SetCellValue(doubleValue);
                    break;
                case decimal decimalValue:
                    cell.SetCellValue(Convert.ToDouble(decimalValue));
                    break;
                default:
                    cell.SetCellValue(value.ToString());
                    break;
            }
        }
        private static void SetColumnWidths(ISheet sheet, List<string> headers, List<RecipeContent> contents, List<string> fieldKeys)
        {
            if (sheet == null || headers == null || headers.Count == 0)
            {
                return;
            }

            for (int columnIndex = 0; columnIndex < headers.Count; columnIndex++)
            {
                int maxLength = headers[columnIndex]?.Length ?? 0;

                if (columnIndex == 0)
                {
                    foreach (RecipeContent content in contents)
                    {
                        int length = 0;
                        if (content?.Fields != null &&
                            content.Fields.TryGetValue(FirstColumnFieldName, out object value) &&
                            value != null)
                        {
                            length = value.ToString()?.Length ?? 0;
                        }

                        if (length > maxLength)
                        {
                            maxLength = length;
                        }
                    }
                }
                else
                {
                    string key = fieldKeys[columnIndex - 1];
                    foreach (RecipeContent content in contents)
                    {
                        if (content?.Fields != null &&
                            content.Fields.TryGetValue(key, out object value) &&
                            value != null)
                        {
                            int length = value.ToString()?.Length ?? 0;
                            if (length > maxLength)
                            {
                                maxLength = length;
                            }
                        }
                    }
                }

                int width = Math.Min(Math.Max((maxLength + 2) * 256, 10 * 256), 80 * 256);
                sheet.SetColumnWidth(columnIndex, width);
            }
        }

        public bool IsRecipeConfigMissing(string recipeId)
        {
            if (string.IsNullOrWhiteSpace(recipeId))
            {
                return true;
            }

            List<RecipeInfo> recipeInfos = GetRecipeAll();
            if (recipeInfos == null || recipeInfos.Count == 0)
            {
                return true;
            }

            RecipeInfo recipeInfo = recipeInfos.FirstOrDefault(x => x.Id == recipeId);
            if (recipeInfo == null)
            {
                return true;
            }

            string recipePath = GetRecipePath(recipeId);
            if (string.IsNullOrWhiteSpace(recipePath) || !Directory.Exists(recipePath))
            {
                return true;
            }

            string recipeJsonPath = Path.Combine(recipePath, $"{recipeId}.json");
            return !File.Exists(recipeJsonPath);
        }
        #endregion
    }
}
