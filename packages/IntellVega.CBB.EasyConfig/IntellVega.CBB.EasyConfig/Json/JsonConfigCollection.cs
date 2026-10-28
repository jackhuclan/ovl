using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace IntellVega.CBB.EasyConfig.Json
{
    /// <summary>
    /// 
    /// </summary>
    public class JsonConfigCollection: IEasyConfigCollection
    {
        /// <inheritdoc />
        public string Category { get; }
        /// <summary>
        /// 文件路径
        /// </summary>
        private readonly string _filePath;

        /// <summary>
        /// key 配置名，value 配置对应的json字符串
        /// </summary>
      
        /// <summary>
        /// 用于保存、读取文件时的锁
        /// </summary>
        private readonly object _locker = new object();

        private ConcurrentDictionary<string, object> _configDic;

        private bool _autoCommit;
        /// <summary>
        /// 当前新做的修改已经全部提交
        /// </summary>
        private bool _committed = true;

        private bool _ready = false;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="category"></param>
        /// <param name="autoCommit"></param>
        public JsonConfigCollection(string filePath,string category,bool autoCommit)
        {
            _filePath = filePath;
            Category=category;
            _autoCommit= autoCommit;
            ReLoad();
        }

        
        /// <inheritdoc />
        public TConfig Get<TConfig>()
        {
            return GetConfig<TConfig>(typeof(TConfig).Name);
        }

        /// <summary>
        /// 读未提交，允许脏读
        /// </summary>
        /// <typeparam name="TConfig"></typeparam>
        /// <param name="id"></param>
        /// <returns></returns>
        public TConfig Get<TConfig>(string id)
        {
            return GetConfig<TConfig>(GetKey(typeof(TConfig),id));
        }

        private TConfig GetConfig<TConfig>(string key)
        {
            EnsureReady();
            _configDic.TryGetValue(key, out var config);
            if (config is TConfig tc)
                return tc;

            // 读取字符串
            return config == null
                ? default(TConfig)
                : JsonConvert.DeserializeObject<TConfig>(JsonConvert.SerializeObject(config));
        }

        /// <inheritdoc />
        public bool Set<TConfig>(TConfig config)
        {
            // 里氏替换情况下，TConfig是父类or接口，而config有强类型。
            // 使用config的强类型作为key。
            return SetConfig<TConfig>(config.GetType().Name, config);


        }

        /// <summary>
        /// set方法允许并发，由内部的线程安全字典保障，且不涉及持久化。
        /// </summary>
        /// <typeparam name="TConfig"></typeparam>
        /// <param name="id"></param>
        /// <param name="config"></param>
        public bool Set<TConfig>(string id, TConfig config)
        {
            return SetConfig<TConfig>(GetKey(config.GetType(),id), config);
        }

        private bool SetConfig<TConfig>(string key, TConfig config)
        {
            EnsureReady();
            // string value = JsonConvert.SerializeObject(config);
            _configDic.AddOrUpdate(key, config, (k, v) => config);
            // 有修改，则需要提交。
            _committed = false;
            if (_autoCommit)
            {
                Commit();
            }
            return true;
        }

        private string GetKey(Type t, string id)
        {
            return $"{t.Name}.{id}";
        }

      

        JsonSerializerSettings _autoTypeSetting = new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Auto };
        
        /// <inheritdoc />
        public void ReLoad()
        {
            lock (_locker)
            {
                // 不存在则创建文件，
                if (!File.Exists(_filePath))
                {
                    File.Create(_filePath).Close();
                    // 创建完成就关闭文件
                    // using (File.Create(_filePath))
                    // {
                    //
                    // }
                    _configDic = new ConcurrentDictionary<string, object>();
                }
                else
                {
                    try
                    {
                        // 读取文本，转化为字典
                        var s = File.ReadAllText(_filePath);
                        _configDic = JsonConvert.DeserializeObject<ConcurrentDictionary<string, object>>(s,_autoTypeSetting) ?? new ConcurrentDictionary<string, object>();
                    }
                    catch
                    {
                        throw new UnExpectedJsonException();
                    }
                }

                // 重载后无需提交
                _committed = true;
                _ready=true;
            }
        }

        /// <inheritdoc />
        public void Commit()
        {

            lock (_locker)
            {
                // 若当前修改都已经提交，或者字典为空则直接返回。实际上字典为空时_commited都是true。
                if (_committed || _configDic == null || _configDic.IsEmpty)
                    return;
                try
                {
                    //增加Fomatting和JsonSerializerSettings参数
                    //使json文件可以自动换行显示，提升文件可读性
                    //反序列化时可以自动反序列化成实际类型的对象
                    var s = JsonConvert.SerializeObject(_configDic, Formatting.Indented, _autoTypeSetting);
                    // 先改标记，失败后再改回去。
                    // 如果写在io之后，可能会因为io写入的过程中，set方法被调用，修改了字典，并把标记修改为false，
                    // 等io之后把标记改为true，导致实际字典内容改了，标记为已提交。
                    _committed = true;
                    // 重新写入整个字典内容
                    File.WriteAllText(_filePath, s);
                }
                catch
                {
                    _committed = false;
                    throw;
                }

            }
        }
        /// <summary>
        /// 等待约2秒，如果还没准备好，则抛出异常。
        /// </summary>
        /// <exception cref="Exception"></exception>
        private void EnsureReady()
        {
            if (!_ready)
            {
                int waitCount = 5;
                // 还没准备好，且还有等待次数，则等一会
                while (waitCount > 0 && !_ready)
                {
                    // 每次等200ms
                    Thread.Sleep(200);
                    waitCount--;
                }
                // 出循环时还没准备好,抛出异常
                if(!_ready)
                {
                    throw new TimeoutException("正在重新加载所有配置，等待超时");
                }
            }
        }

        public IEnumerable<object> GetAll()
        {
            EnsureReady();
            return _configDic.Values;
        }
    }

}