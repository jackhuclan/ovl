using System;

namespace IntellVega.CBB.EasyConfig.Json
{
    /// <summary>
    /// 
    /// </summary>
    public class UnExpectedJsonException : Exception
    {
       
        /// <summary>
        /// 
        /// </summary>
        public UnExpectedJsonException()
            : base($"配置文件的格式不符合标准。")
        {

        }
    }
}