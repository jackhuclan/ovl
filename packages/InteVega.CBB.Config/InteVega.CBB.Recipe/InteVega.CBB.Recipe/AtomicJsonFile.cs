using InteVega.CBB.CommonLib.Helpers;
using Newtonsoft.Json;
using System.Text;

namespace InteVega.CBB.Recipe
{
    /// <summary>
    /// 断电安全的 JSON 文件读写：先写完整临时文件并刷盘，再原子替换目标文件，并保留 .bak。
    /// 避免直接 WriteAllText/覆盖写入时断电导致 Recipe.json 被截断或清空。
    /// </summary>
    public static class AtomicJsonFile
    {
        public static void Serialize<T>(T data, string filePath)
        {
            EnsurePath(filePath);

            var json = JsonConvert.SerializeObject(data, Formatting.Indented);
            WriteText(filePath, json);
        }

        public static void WriteText(string filePath, string contents)
        {
            EnsurePath(filePath);
            contents ??= string.Empty;

            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var tempPath = filePath + ".tmp";
            var backupPath = filePath + ".bak";

            using (var stream = new FileStream(
                       tempPath,
                       FileMode.Create,
                       FileAccess.Write,
                       FileShare.None,
                       bufferSize: 4096,
                       FileOptions.WriteThrough))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
            {
                writer.Write(contents);
                writer.Flush();
                stream.Flush(flushToDisk: true);
            }

            if (File.Exists(filePath))
            {
                // Windows 上 Replace 对已存在目标文件是原子替换，并生成备份。
                File.Replace(tempPath, filePath, backupPath, ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(tempPath, filePath);
            }

            TryDelete(tempPath);
        }

        /// <summary>
        /// 读取 JSON；主文件缺失或损坏时自动尝试 .bak，成功则尽量把备份恢复为主文件。
        /// </summary>
        public static T Deserialize<T>(string filePath)
        {
            EnsurePath(filePath);

            if (TryDeserialize<T>(filePath, out var value))
            {
                return value;
            }

            var backupPath = filePath + ".bak";
            if (TryDeserialize<T>(backupPath, out value))
            {
                try
                {
                    File.Copy(backupPath, filePath, overwrite: true);
                    LogHelper.LogInfo($"AtomicJsonFile restored from backup. Path={filePath}");
                }
                catch (Exception ex)
                {
                    LogHelper.LogError(ex, $"AtomicJsonFile restore from backup failed. Path={filePath}");
                }

                return value;
            }

            return default;
        }

        private static bool TryDeserialize<T>(string path, out T value)
        {
            value = default;
            try
            {
                if (!File.Exists(path))
                {
                    return false;
                }

                var text = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(text))
                {
                    return false;
                }

                value = JsonConvert.DeserializeObject<T>(text);
                return value != null;
            }
            catch (Exception ex)
            {
                LogHelper.LogError(ex, $"AtomicJsonFile deserialize failed. Path={path}");
                return false;
            }
        }

        private static void EnsurePath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path is null or empty.", nameof(filePath));
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // ignore cleanup failure
            }
        }
    }
}
