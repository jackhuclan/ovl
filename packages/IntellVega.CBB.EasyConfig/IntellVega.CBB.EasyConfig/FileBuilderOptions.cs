using System;
using System.IO;

namespace IntellVega.CBB.EasyConfig
{
    /// <summary>
    /// 基于文件实现时，builder的配置
    /// </summary>
    public class FileBuilderOptions : BuilderOptions
    {
        /// <summary>
        /// json 配置文件的根目录
        /// </summary>
        public string RootDirectory { get; private set; } = "./Configs/";

        /// <summary>
        /// 修改后自动提交
        /// </summary>
        public bool AutoCommit { get; protected set; } = true;

        /// <summary>
        /// 设置根目录
        /// </summary>
        /// <param name="rootDirectory"></param>
        /// <returns></returns>

        public FileBuilderOptions SetRootDirectory(string rootDirectory)
        {
        
            // 补齐目录结束字符/ ，不做文件夹路径合法验证。
            if (rootDirectory.Length > 0)
            {
                var lastChar = rootDirectory.Substring(rootDirectory.Length - 1);
                // 路径结尾符丢失，补齐.
                if (lastChar != "/" && lastChar != @"\")
                {
                    RootDirectory = rootDirectory + "/";
                }
                else
                {
                    RootDirectory = rootDirectory;
                }
            }
            return this;

        }
        /// <summary>
        /// 自动提交设置
        /// </summary>
        /// <param name="autoCommit"></param>
        /// <returns></returns>
        public FileBuilderOptions SetAutoCommit(bool autoCommit)
        {
            AutoCommit = autoCommit;
            return this;
        }

     
    }
}