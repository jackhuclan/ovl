using Microsoft.Extensions.Logging;

namespace Ovl.Biz.Services.Measure;

/// <summary>
/// <see cref="IRecipeManager"/> 的桩实现：仅维护当前配方名（内存态），供 Home 页绑定。
/// 真实实现（conf/Recipe.*.json 读写 + 原 TIS 配方体系适配）在 M4 迁入。
/// </summary>
public sealed class RecipeManagerStub : IRecipeManager
{
    private readonly ILogger<RecipeManagerStub>? _logger;

    public RecipeManagerStub(ILogger<RecipeManagerStub>? logger = null)
    {
        _logger = logger;
    }

    public string CurrentRecipeName { get; private set; } = string.Empty;

    public Task LoadAsync(string recipeName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recipeName);
        CurrentRecipeName = recipeName;
        _logger?.LogInformation("加载配方（桩）：{RecipeName}", recipeName);
        return Task.CompletedTask;
    }
}
