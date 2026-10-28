using System.Threading.Tasks;

namespace InteVega.CBB.Recipe.Ui.ViewModel
{
    /// <summary>
    /// 连接「AI 打开创建配方对话框」与「用户在对话框内点击 Save」的等待器。
    /// AI 工具发出后开始等待，用户在对话框中保存成功 → 拿到配方名称；
    /// 用户直接关闭对话框 → 拿到 null，表示放弃创建。
    /// </summary>
    internal static class RecipeCreationWaiter
    {
        private static readonly object SyncRoot = new();
        private static TaskCompletionSource<string> _pending;

        /// <summary>开始一次等待；若已有未完成的等待，先按"放弃"作废。</summary>
        public static Task<string> Begin()
        {
            lock (SyncRoot)
            {
                _pending?.TrySetResult(null);
                _pending = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
                return _pending.Task;
            }
        }

        /// <summary>用户在对话框中保存成功。</summary>
        public static void Complete(string recipeName)
        {
            lock (SyncRoot)
            {
                _pending?.TrySetResult(recipeName);
                _pending = null;
            }
        }

        /// <summary>对话框被关闭但没有完成创建（点关闭 / 取消）。</summary>
        public static void Cancel()
        {
            lock (SyncRoot)
            {
                _pending?.TrySetResult(null);
                _pending = null;
            }
        }
    }
}
