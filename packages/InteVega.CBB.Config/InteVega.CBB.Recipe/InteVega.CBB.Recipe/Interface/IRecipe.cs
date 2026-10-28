namespace InteVega.CBB.Recipe.Interface
{
    public interface IRecipe
    {
        /// <summary>
        /// 获取指定类型的配置
        /// </summary>
        /// <typeparam name="TConfig"></typeparam>
        /// <returns></returns>
        TRecipeNode Get<TRecipeNode>();

        /// <summary>
        /// 获取指定类型的配置
        /// </summary>
        /// <typeparam name="TConfig"></typeparam>
        /// <returns></returns>
        TRecipeNode Get<TRecipeNode>(string key);
        /// <summary>
        /// 更新配置
        /// </summary>
        /// <typeparam name="TConfig"></typeparam>
        /// <param name="config"></param>
        void Set<TRecipeNode>(TRecipeNode config, bool saveToFile = false);

        /// <summary>
        /// 更新配置
        /// </summary>
        /// <typeparam name="TConfig"></typeparam>
        /// <param name="config"></param>
        void Set<TRecipeNode>(string key, TRecipeNode config, bool saveToFile = false);
    }
}
