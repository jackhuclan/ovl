namespace IntellVega.CBB.EasyConfig
{

    /// <summary>
    /// 默认提供基于文件实现配置存储的基类
    /// </summary>
    public abstract class FileConfigCollectionFactory : IEasyConfigCollectionFactory
    {
        /// <summary>
        /// 
        /// </summary>
        protected readonly FileBuilderOptions FileBuilderOptions;


        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        protected FileConfigCollectionFactory(FileBuilderOptions options)
        {
            FileBuilderOptions = options;
        }


        /// <inheritdoc />
        public virtual IEasyConfigCollection Create()
        {
            return Create(FileBuilderOptions.DefaultCategory);
        }
        /// <inheritdoc />
        public abstract IEasyConfigCollection Create(string collectionName);

        /// <inheritdoc />
        public IEasyConfigCollection Create<T>()
        {
            var category = FileBuilderOptions.GetCategory<T>();
            return Create(category);
        }
    }
}