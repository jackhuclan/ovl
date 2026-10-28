using System.Collections.Generic;

namespace IntellVega.CBB.EasyConfig
{
    
    /// <summary>
    /// 
    /// </summary>
    public interface IEasyConfigCollection
    {

        /// <summary>
        /// 配置集分类，可以当作配置集名称，具有唯一性，通常关联到文件。
        /// </summary>
        string Category { get; }
        /// <summary>
        /// 放弃所有更改，重新加载所有配置
        /// 重载中，不允许提交，不允许同时重载。
        /// 重载中，不允许读，不允许写。
        /// </summary>
        void ReLoad();

        /// <summary>
        /// 提交更改。
        /// 提交过程中，允许读写，写的内容不会持久化。
        /// 提交过程中，不允许重载，不允许同时提交
        /// </summary>
        void Commit();
        
        /// <summary>
        /// 读取配置，默认按类名获取
        /// </summary>
        TConfig Get<TConfig>();

        /// <summary>
        /// 读取配置，若不存在则返回空。
        /// </summary>
        TConfig Get<TConfig>(string id);
        
        /// <summary>
        /// 设置,成功返回true，失败返回false。
        /// </summary>
        bool Set<TConfig>(TConfig config);


        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="config"></param>
        /// <typeparam name="TConfig"></typeparam>
        bool Set<TConfig>(string id,TConfig config);


        /// <summary>
        /// 获取所有配置
        /// </summary>
        /// <returns></returns>
        IEnumerable<object> GetAll();

    }
    
}