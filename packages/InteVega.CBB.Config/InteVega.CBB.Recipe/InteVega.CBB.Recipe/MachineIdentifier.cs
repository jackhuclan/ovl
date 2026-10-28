using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Management;
using System.Security.Cryptography;
using System.Text;


namespace InteVega.CBB.Recipe
{
    public static class MachineIdentifier
    {
        // Lazy<T> 会在第一次访问 Value 属性时才执行传入的委托
        private static readonly Lazy<string> _lazyDeviceId =
            new Lazy<string>(GetUniqueDeviceIdInternal);

        // 对外公开的属性：第一次访问时会触发加载，之后直接返回缓存值
        public static string DeviceId => _lazyDeviceId.Value;

        static string GetUniqueDeviceIdInternal()
        {
            string cpuId = "";
            string motherboardId = "";
            string diskId = "";

            // 获取CPU ID
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor");
                foreach (ManagementObject obj in searcher.Get())
                {
                    cpuId = obj["ProcessorId"]?.ToString();
                    break;
                }
            }
            catch { }

            // 获取主板序列号
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BaseBoard");
                foreach (ManagementObject obj in searcher.Get())
                {
                    motherboardId = obj["SerialNumber"]?.ToString();
                    break;
                }
            }
            catch { }

            // 获取第一个硬盘序列号
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_DiskDrive WHERE Index=0");
                foreach (ManagementObject obj in searcher.Get())
                {
                    diskId = obj["SerialNumber"]?.ToString();
                    break;
                }
            }
            catch { }

            // 组合信息并生成MD5哈希
            string combined = $"{cpuId}{motherboardId}{diskId}".Trim();
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(combined);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "");
            }
        }
    }
}
