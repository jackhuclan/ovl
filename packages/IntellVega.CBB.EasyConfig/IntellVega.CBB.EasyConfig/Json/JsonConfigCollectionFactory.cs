namespace IntellVega.CBB.EasyConfig.Json
{
    /// <summary>
    /// json配置集构造器
    /// </summary>
    public class JsonConfigCollectionFactory : FileConfigCollectionFactory
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="categoryOptions"></param>
        public JsonConfigCollectionFactory(JsonBuilderOptions categoryOptions) : base(categoryOptions)
        {

        }
        /// <inheritdoc />
        public override IEasyConfigCollection Create(string category)
        {
            return new JsonConfigCollection(GetFilePath(category),category,FileBuilderOptions.AutoCommit);
        }

        /// <summary>
        /// 根据category，生成文件路径
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        private string GetFilePath(string category)
        {
            return  $"{FileBuilderOptions.RootDirectory}{category}.json";
        }
       
    }

}