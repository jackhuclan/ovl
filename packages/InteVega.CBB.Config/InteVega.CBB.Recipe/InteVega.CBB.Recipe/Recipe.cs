using IntellVega.CBB.EasyConfig;
using InteVega.CBB.Recipe.Interface;

namespace InteVega.CBB.Recipe
{
    public class Recipe:IRecipe
    {
        /// <summary>
        /// 配置
        /// </summary>
        private readonly IEasyConfigCollection _configCollection;

        public Recipe(IEasyConfigCollection configCollection)
        {
            _configCollection = configCollection;
        }

        public TRecipeNode Get<TRecipeNode>(string name)
        {
            return _configCollection.Get<TRecipeNode>(name);

        }
        public TRecipeNode Get<TRecipeNode>()
        {
            return _configCollection.Get<TRecipeNode>();

        }
        public void Set<TRecipeNode>(TRecipeNode config, bool saveToFile = false)
        {
            _configCollection.Set(config);
            if (saveToFile)
            {
                _configCollection.Commit();
            }
        }
        public void Set<TRecipeNode>(string name, TRecipeNode config, bool saveToFile = false)
        {
            _configCollection.Set(name, config);
            if (saveToFile)
            {
                _configCollection.Commit();
            }
        }
    }
}
