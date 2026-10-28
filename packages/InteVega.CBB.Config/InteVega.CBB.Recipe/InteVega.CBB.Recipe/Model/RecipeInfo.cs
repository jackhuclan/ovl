using Newtonsoft.Json;
using SqlSugar;

namespace InteVega.CBB.Recipe.Core
{
    [SugarTable("RecipeInfo")]
    public class RecipeInfo
    {
        /// <summary>
        /// 主键
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// 配方名称
        /// </summary>
        public string RecipeName { get; set; }
        [JsonIgnore]
        public string RecipeOldName { get; set; }
        /// <summary>
        /// 顺序
        /// </summary>
        public int Index { get; set; }
        /// <summary>
        /// 0：文件夹，1：配方
        /// </summary>
        public int Level { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; }
        /// <summary>
        /// 上级配方Id
        /// </summary>
        public string ParentId { get; set; }
        /// <summary>
        /// 上级配方Name
        /// </summary>
        public string ParentName { get; set; }
        ///// <summary>
        ///// 配方类别
        ///// </summary>
        //public string RecipeType { get; set; }
        /// <summary>
        /// 是否使用
        /// </summary>
        public bool IsUse { get; set; }
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateDate { get; set; }
        public DateTime UpdateDate { get; set; }
        public string CreateUser { get; set; }
        public string UpdateUser { get; set; }
        /// <summary>
        /// 版本号
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// 硬件id ，在导入、导出时判断是否是同一个机台
        /// </summary>
        public string HardwareId { get; set; }

        /// <summary>
        /// 配方导入时间
        /// </summary>
        public DateTime ImportTime { get; set; }

        [JsonIgnore]
        public string NewId { get; set; }
        [JsonIgnore]
        public string NewName { get; set; }
    }
}
