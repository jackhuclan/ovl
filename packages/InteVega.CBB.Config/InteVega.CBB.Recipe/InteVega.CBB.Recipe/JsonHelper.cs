using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InteVega.CBB.Recipe
{
    public class JsonHelper
    {
        /// <summary>
        /// 读取Json
        /// </summary>
        /// <returns></returns>
        public static T ReadJsonFile<T>(string root,string fileName)
        {
            T dataResult =default;
            string data = string.Empty;
            if (File.Exists(root+ fileName))
            {
                using (var sw = new StreamReader(root + fileName, true))
                {
                    data = sw.ReadToEnd();
                    sw.Close();
                }
                if (!string.IsNullOrEmpty(data))
                    dataResult = JsonConvert.DeserializeObject<T>(data);
            }
            return dataResult;
        }
        /// <summary>
        /// 写入Json
        /// </summary>
        /// <param name="ex"></param>
        public static bool WriteJsonFile<T>(string root,string fileName,T data)
        {
            if (!Directory.Exists(root))
            {
                Directory.CreateDirectory(root);
            }
            string filePath=root+ fileName;
            if (!File.Exists(filePath))
            {
                File.CreateText(filePath).Close();
            }
            if (data != null)
            {
                string jsonString = JsonConvert.SerializeObject(data);
                using (var sw = new StreamWriter(filePath, false))
                {
                    
                    sw.Write(jsonString);
                    sw.Close();
                }
            }
            return true;

        }
    }
}
