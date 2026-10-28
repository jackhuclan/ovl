using IntellVega.CBB.EasyConfig;
using IntellVega.CBB.EasyConfig.Json;
using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.Recipe.Core;
using InteVega.CBB.Recipe.Interface;
using NPOI.POIFS.Crypt.Dsig;
using NPOI.POIFS.Properties;
using System.DirectoryServices;
using System.IO;
using System.IO.Compression;

namespace InteVega.CBB.Recipe
{
    public class DiskSpaceAnalyzer
    {
        /// <summary>
        /// 分析磁盘空间并判断能否拷贝文件夹
        /// </summary>
        /// <param name="sourceFolder">源文件夹路径</param>
        /// <param name="targetDrive">目标驱动器（如："C:"）</param>
        /// <param name="bufferSizeMB">缓冲区大小（MB，默认为100MB）</param>
        /// <returns>分析结果</returns>
        public CopyAnalysisResult AnalyzeCopyOperation(string sourceFolder, string targetDrive, long bufferSizeMB = 100)
        {
            try
            {
                // 验证输入参数
                if (!Directory.Exists(sourceFolder))
                {
                    return new CopyAnalysisResult
                    {
                        CanCopy = false,
                        ErrorMessage = $"源文件夹不存在: {sourceFolder}"
                    };
                }

                if (!DriveExists(targetDrive))
                {
                    return new CopyAnalysisResult
                    {
                        CanCopy = false,
                        ErrorMessage = $"目标驱动器不存在: {targetDrive}"
                    };
                }

                // 1. 计算源文件夹大小
                long sourceFolderSize = CalculateFolderSize(sourceFolder);
                double sourceSizeGB = BytesToGB(sourceFolderSize);

                // 2. 获取目标磁盘空间信息
                DriveInfo targetDriveInfo = new DriveInfo(targetDrive);
                long availableFreeSpace = targetDriveInfo.AvailableFreeSpace;
                long totalSize = targetDriveInfo.TotalSize;
                double availableSpaceGB = BytesToGB(availableFreeSpace);
                double totalSizeGB = BytesToGB(totalSize);

                // 3. 计算所需缓冲区空间
                long bufferSizeBytes = bufferSizeMB * 1024 * 1024;
                long requiredSpace = sourceFolderSize + bufferSizeBytes;

                // 4. 判断是否可拷贝
                bool canCopy = availableFreeSpace >= requiredSpace;

                // 5. 计算使用百分比和建议
                double usedPercentage = (double)(totalSize - availableFreeSpace) / totalSize * 100;
                string recommendation = GetCopyRecommendation(canCopy, availableFreeSpace, requiredSpace, usedPercentage);

                return new CopyAnalysisResult
                {
                    CanCopy = canCopy,
                    SourceFolderSize = sourceFolderSize,
                    SourceSizeGB = sourceSizeGB,
                    AvailableSpace = availableFreeSpace,
                    AvailableSpaceGB = availableSpaceGB,
                    TotalSpaceGB = totalSizeGB,
                    RequiredSpace = requiredSpace,
                    RequiredSpaceGB = BytesToGB(requiredSpace),
                    BufferSizeMB = bufferSizeMB,
                    DriveName = targetDriveInfo.Name,
                    DriveFormat = targetDriveInfo.DriveFormat,
                    DriveType = targetDriveInfo.DriveType.ToString(),
                    UsedPercentage = usedPercentage,
                    Recommendation = recommendation
                };
            }
            catch (Exception ex)
            {
                return new CopyAnalysisResult
                {
                    CanCopy = false,
                    ErrorMessage = $"分析过程中发生错误: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 计算文件夹大小（递归计算）
        /// </summary>
        private long CalculateFolderSize(string folderPath)
        {
            long size = 0;

            try
            {
                // 计算所有文件的大小
                string[] files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);

                foreach (string file in files)
                {
                    try
                    {
                        FileInfo fileInfo = new FileInfo(file);
                        if (fileInfo.Exists)
                        {
                            size += fileInfo.Length;
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // 跳过无权限访问的文件
                        continue;
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                // 跳过无权限访问的文件夹
            }
            catch (DirectoryNotFoundException)
            {
                // 文件夹不存在
            }

            return size;
        }

        /// <summary>
        /// 检查驱动器是否存在
        /// </summary>
        private bool DriveExists(string driveLetter)
        {
            try
            {
                var drives = DriveInfo.GetDrives();
                return drives.Any(d => d.Name.StartsWith(driveLetter, StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 字节转换为GB
        /// </summary>
        private double BytesToGB(long bytes)
        {
            return bytes / 1024.0 / 1024.0 / 1024.0;
        }

        /// <summary>
        /// 获取拷贝建议
        /// </summary>
        private string GetCopyRecommendation(bool canCopy, long availableSpace, long requiredSpace, double usedPercentage)
        {
            if (!canCopy)
            {
                long missingSpace = requiredSpace - availableSpace;
                double missingSpaceGB = BytesToGB(missingSpace);

                if (usedPercentage > 90)
                {
                    return $"磁盘空间严重不足！需要清理 {missingSpaceGB:F2} GB 空间，或选择其他驱动器。";
                }
                else if (usedPercentage > 80)
                {
                    return $"磁盘空间不足。需要额外 {missingSpaceGB:F2} GB 空间，建议清理临时文件或选择其他驱动器。";
                }
                else
                {
                    return $"空间不足。还需要 {missingSpaceGB:F2} GB 可用空间。";
                }
            }
            else
            {
                if (usedPercentage > 90)
                {
                    return $"可以拷贝，但磁盘空间即将耗尽！拷贝后使用率将达 {usedPercentage:F1}%，建议尽快清理空间。";
                }
                else if (usedPercentage > 80)
                {
                    return $"可以拷贝，但磁盘空间较紧张。拷贝后使用率约为 {usedPercentage:F1}%。";
                }
                else
                {
                    return "可以安全拷贝，磁盘空间充足。";
                }
            }
        }
    }

    /// <summary>
    /// 拷贝分析结果
    /// </summary>
    public class CopyAnalysisResult
    {
        public bool CanCopy { get; set; }
        public long SourceFolderSize { get; set; }          // 源文件夹大小（字节）
        public double SourceSizeGB { get; set; }            // 源文件夹大小（GB）
        public long AvailableSpace { get; set; }            // 可用空间（字节）
        public double AvailableSpaceGB { get; set; }        // 可用空间（GB）
        public double TotalSpaceGB { get; set; }            // 总空间（GB）
        public long RequiredSpace { get; set; }             // 所需空间（字节）
        public double RequiredSpaceGB { get; set; }         // 所需空间（GB）
        public long BufferSizeMB { get; set; }              // 缓冲区大小（MB）
        public string DriveName { get; set; }               // 驱动器名称
        public string DriveFormat { get; set; }             // 文件系统格式
        public string DriveType { get; set; }               // 驱动器类型
        public double UsedPercentage { get; set; }          // 磁盘使用百分比
        public string Recommendation { get; set; }          // 建议
        public string ErrorMessage { get; set; }            // 错误信息

        /// <summary>
        /// 格式化显示结果
        /// </summary>
        public override string ToString()
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                return $"错误: {ErrorMessage}";
            }

            return $@"=== 磁盘空间分析报告 ===
源文件夹大小: {SourceSizeGB:F2} GB
目标驱动器: {DriveName} ({DriveType}, {DriveFormat})
驱动器总空间: {TotalSpaceGB:F2} GB
当前可用空间: {AvailableSpaceGB:F2} GB
磁盘使用率: {UsedPercentage:F1}%
所需空间: {RequiredSpaceGB:F2} GB (含{BufferSizeMB}MB缓冲区)
拷贝状态: {(CanCopy ? "✅ 可以拷贝" : "❌ 无法拷贝")}
建议: {Recommendation}";
        }
    }

    public class DeleteResult
    {
        public string FolderPath { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedTime { get; set; }
        public string Message { get; set; }
        public string Error { get; set; }
        public string Warning { get; set; }
    }

}
