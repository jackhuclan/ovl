using IntellVega.CBB.EasyConfig;
using InteVega.CBB.Db.Api.Interface;
using InteVega.CBB.Recipe.Core;

namespace InteVega.CBB.Recipe
{
    public class RecipeDB
    {
        /// <summary>
        /// 配置
        /// </summary>
        private readonly IEasyConfigCollection _configCollection;
        private readonly IDBContext<RecipeNode> _dbContext;

        public RecipeDB(IEasyConfigCollection easyConfigCollection, IDBContext<RecipeNode> dBContext)
        {
            _configCollection = easyConfigCollection;
            _dbContext = dBContext;
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
