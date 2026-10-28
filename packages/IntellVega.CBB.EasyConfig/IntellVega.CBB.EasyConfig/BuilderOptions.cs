using System.Collections.Generic;
using System;

namespace IntellVega.CBB.EasyConfig
{
    /// <summary>
    /// 日志工厂构造器的设置选项
    /// </summary>
    public class BuilderOptions
    {
        /// <summary>
        /// 默认的配置集名称，默认是"EasyConfig"
        /// </summary>
        public string DefaultCategory { get; protected set; } = "EasyConfig";

        /// <summary>
        /// 配置类型和名称的对应关系
        /// key: Type, value: Category
        /// </summary>
        protected readonly Dictionary<Type, string> PathDic = new Dictionary<Type, string>();

   
     
        /// <summary>
        /// 设置配置类型和category的对应关系，不设置认为同名
        /// </summary>
        /// <typeparam name="TCategory"></typeparam>
        /// <param name="category"></param>
        /// <returns></returns>
        public BuilderOptions SetCategory<TCategory>(string category)
        {
            if (PathDic.ContainsKey(typeof(TCategory)))
            {
                PathDic[typeof(TCategory)] = category;
            }
            else
            {
                PathDic.Add(typeof(TCategory), category);
            }
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TCategory"></typeparam>
        /// <returns></returns>
        public string GetCategory<TCategory>()
        {
            if (!PathDic.TryGetValue(typeof(TCategory), out var category))
            {
                category = DefaultCategory;
            }
            return category;
        }
        /// <summary>
        /// 设置默认的Category
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        public BuilderOptions SetDefaultCategory(string category)
        {
            DefaultCategory = category;
            return this;
        }

    }
}