using System;
using System.IO;

namespace IntellVega.CBB.EasyConfig.Json
{
    /// <summary>
    /// 
    /// </summary>
    public static class BuilderExtensions
    {
  
      
        /// <summary>
        /// 使用json存储配置
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="optionsAction"></param>
        /// <returns></returns>
        public static EasyConfigFactoryBuilder UseJson(this EasyConfigFactoryBuilder builder, Action<JsonBuilderOptions> optionsAction=null)
        {
            var options = new JsonBuilderOptions();
            optionsAction?.Invoke(options);
            // 创建文件夹
            if (!Directory.Exists(options.RootDirectory))
            {
                Directory.CreateDirectory(options.RootDirectory);
            }
            builder.BuildFunc= ()=> new JsonConfigCollectionFactory(options);
            return builder;
        }
    }



}