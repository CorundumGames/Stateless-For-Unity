using System;
using Cysharp.Threading.Tasks;

namespace Stateless
{
    public partial class StateMachine<TState, TTrigger>
    {
        internal abstract class DeactivateActionBehaviour
        {
            readonly TState _state;

            protected DeactivateActionBehaviour(TState state, Reflection.InvocationInfo actionDescription)
            {
                _state = state;
                Description = actionDescription ?? throw new ArgumentNullException(nameof(actionDescription));
            }

            internal Reflection.InvocationInfo Description { get; }

            public abstract void Execute();
            public abstract UniTask ExecuteAsync();

            public class Sync : DeactivateActionBehaviour
            {
                readonly Action _action;

                public Sync(TState state, Action action, Reflection.InvocationInfo actionDescription)
                    : base(state, actionDescription)
                {
                    _action = action;
                }

                public override void Execute()
                {
                    _action();
                }

                public override UniTask ExecuteAsync()
                {
                    Execute();
                    return TaskResult.Done;
                }
            }

            public class Async : DeactivateActionBehaviour
            {
                readonly Func<UniTask> _action;

                public Async(TState state, Func<UniTask> action, Reflection.InvocationInfo actionDescription)
                    : base(state, actionDescription)
                {
                    _action = action;
                }

                public override void Execute()
                {
                    throw new InvalidOperationException(
                        $"Cannot execute asynchronous action specified in OnDeactivateAsync for '{_state}' state. " +
                         "Use asynchronous version of Deactivate [DeactivateAsync]");
                }

                public override UniTask ExecuteAsync()
                {
                    return _action();
                }
            }
        }
    }
}
