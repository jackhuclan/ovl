using SqlSugar;

namespace InteVega.CBB.Recipe.Core
{
    [SugarTable("RecipeNode")]
    public class RecipeNode
    {
        public string ID { get; set; }
        public string RecipeID { get; set; }
        public string Centent { get; set; }
        public string CreateDate { get; set; }
        public string CreateUser { get; set; }
        public string UpdateDate { get; set; }
        public string UpdateUser { get; set; }
    }
}
