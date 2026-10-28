using Microsoft.Extensions.DependencyInjection;
using Ovl.Biz.Services.Measure;

namespace Ovl.Biz.Services;

/// <summary>
/// 量测（Measure/Home）业务服务注册扩展。
/// </summary>
public static class MeasureServiceCollectionExtensions
{
    /// <summary>
    /// 注册量测运行与配方管理的桩实现（无硬件环境可运行；真实实现迁入时替换为同接口注册）。
    /// </summary>
    public static IServiceCollection AddMeasureStubs(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IMainRunService, MeasureRunStub>();
        services.AddSingleton<IRecipeManager, RecipeManagerStub>();
        return services;
    }
}
