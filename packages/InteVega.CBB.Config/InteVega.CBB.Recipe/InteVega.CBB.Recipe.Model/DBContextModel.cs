using InteVega.CBB.Db;
// vendor 适配：marvel 源码用的旧命名空间 InteVega.CBB.Db.Interface 在 nexus 包(1.0.0.7)中
// 已演进为 InteVega.CBB.Db.Api.Interface；SqlSugarContext<TEntity> 直接位于 ns InteVega.CBB.Db 下
using InteVega.CBB.Db.Api.Interface;
using Microsoft.Extensions.DependencyInjection;

namespace InteVega.CBB.Recipe.Model
{
    public static class DBContextModel
    {
        public static IServiceCollection AddInteVegaDbContextDisplayModule(this IServiceCollection services)
        {
            services.AddTransient<IDBContext<RecipeInfo>, SqlSugarContext<RecipeInfo>>();
            return services;
        }
    }
}
