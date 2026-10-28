using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InteVega.CBB.Recipe.Core
{
    /// <summary>
    /// 此类用于存储配方内容，包含配方ID、配方名称和配方字段内容。配方字段内容以键值对的形式存储，其中键为字段名称，值为字段值。该类可以用于在内存中表示配方数据，方便进行配方的读取、修改和保存等操作。
    /// </summary>
    public class RecipeContent
    {
        /// <summary>
        /// Gets or sets the unique identifier for a recipe.
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// 标记reicpename，方便在内存中区分不同的配方内容
        /// </summary>
        public string RecipeName { get; set; }
        /// <summary>
        /// 配方的类型，配方类型可以用来区分不同类型的配方，例如：工艺配方、设备配方、原料配方等。根据实际需求，可以定义不同的配方类型，以便在使用时进行分类和管理。    
        /// </summary>
        public string RecipeType { get; set; }
        /// <summary>
        /// 读取配方时，配方名称可以用来标识配方的名称，方便用户识别和管理配方。配方名称应该具有一定的描述性，以便用户能够快速理解配方的内容和用途。    
        /// </summary>
        public Dictionary<string, object> Fields { get; set; } = new Dictionary<string, object>();
    }
}
