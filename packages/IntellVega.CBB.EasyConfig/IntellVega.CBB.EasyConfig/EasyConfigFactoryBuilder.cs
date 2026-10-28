using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System;
using System.IO;
using IntellVega.CBB.EasyConfig.Json;
using System.Text.RegularExpressions;

namespace IntellVega.CBB.EasyConfig
{

    /// <summary>
    /// 配置工厂创建器，跟IOC容器配合使用，创建不同的工厂，比如json工厂，LiteDb工厂，XML工厂
    /// </summary>
    public class EasyConfigFactoryBuilder
    {

        /// <summary>
        /// 
        /// </summary>
        public EasyConfigFactoryBuilder()
        {
       
        }
     
       


        /// <summary>
        /// 可以替换实现不同的IEasyConfigCollectionFactory
        /// </summary>
        public Func<IEasyConfigCollectionFactory> BuildFunc;
        /// <summary>
        /// 创建工厂
        /// 如果没有特殊指定，则使用json工厂
        /// </summary>
        /// <returns></returns>
        public IEasyConfigCollectionFactory Build()
        {
            if (BuildFunc != null)
            {
                return BuildFunc?.Invoke();
            }
            return BuildDefaultCollectionFactory();
        }

        private IEasyConfigCollectionFactory BuildDefaultCollectionFactory()
        {
            this.UseJson();
            return Build();
        }
    }
}