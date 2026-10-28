using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InteVega.CBB.Recipe.Model
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
        /// <summary>
        /// 顺序
        /// </summary>
        public int SortNum { get; set; }
        /// <summary>
        /// 层级
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
        /// 上级配方Id
        /// </summary>
        public string ParentName { get; set; }
        /// <summary>
        /// 配方类别
        /// </summary>
        public string RecipeType { get; set; }
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
    }
}
