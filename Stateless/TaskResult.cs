using Cysharp.Threading.Tasks;

namespace Stateless
{
    internal static class TaskResult
    {
        internal static readonly UniTask Done = UniTask.CompletedTask;
    }
}
