// vendor 适配：原 marvel 中 OperationType 定义于 InteVega.CBB.Recipe.Ui（Prism 工程，本次未搬入），
// 为避免为 4 行枚举引入 Prism，在此原样落位（命名空间保持 InteVega.CBB.Recipe 不变）。
namespace InteVega.CBB.Recipe
{
    public enum OperationType
    {
        Add = 1,
        Update = 2,
        Delete = 3,
        Paste = 4,
    }
}
