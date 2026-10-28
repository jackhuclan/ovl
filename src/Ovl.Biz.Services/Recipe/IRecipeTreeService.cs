namespace Ovl.Biz.Services.Recipe;

/// <summary>配方树节点类型。</summary>
public enum RecipeNodeType
{
    /// <summary>目录。</summary>
    Folder,

    /// <summary>配方文件。</summary>
    Recipe,
}

/// <summary>配方树节点。</summary>
public sealed record RecipeTreeNode(
    string Id,
    string Name,
    int Level,
    RecipeNodeType Type,
    IReadOnlyList<RecipeTreeNode> Children);

/// <summary>
/// 配方树服务接口（对应原 InteVega.CBB.Recipe.Ui 的 RecipeMenuViewModel 数据源：
/// 原版从配方服务器/本地目录读取目录树，桩实现返回内存演示树）。
/// 真实实现（conf/Recipe 目录扫描）在 M4b 接入。
/// </summary>
public interface IRecipeTreeService
{
    /// <summary>获取配方目录树根集合。</summary>
    IReadOnlyList<RecipeTreeNode> GetTree(CancellationToken cancellationToken = default);
}

/// <summary>
/// <see cref="IRecipeTreeService"/> 桩实现：内存演示目录树。
/// </summary>
public sealed class RecipeTreeStub : IRecipeTreeService
{
    public IReadOnlyList<RecipeTreeNode> GetTree(CancellationToken cancellationToken = default)
    {
        List<RecipeTreeNode> DemoRecipes(string prefix) =>
        [
            new($"{prefix}_OVL", $"{prefix}_OVL", 2, RecipeNodeType.Recipe, []),
            new($"{prefix}_CD", $"{prefix}_CD", 2, RecipeNodeType.Recipe, []),
            new($"{prefix}_TIS", $"{prefix}_TIS", 2, RecipeNodeType.Recipe, []),
        ];

        return
        [
            new("root", "Recipe", 0, RecipeNodeType.Folder,
            [
                new("public", "Public", 1, RecipeNodeType.Folder, DemoRecipes("Public")),
                new("prod", "Production", 1, RecipeNodeType.Folder, DemoRecipes("Prod")),
                new("debug", "Debug", 1, RecipeNodeType.Folder, DemoRecipes("Debug")),
            ]),
        ];
    }
}
