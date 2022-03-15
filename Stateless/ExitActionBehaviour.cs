using System;
using Cysharp.Threading.Tasks;

namespace Stateless
{
    public partial class StateMachine<TState, TTrigger>
    {
        internal abstract class ExitActionBehavior
        {
            public abstract void Execute(Transition transition);
            public abstract UniTask ExecuteAsync(Transition transition);

            protected ExitActionBehavior(Reflection.InvocationInfo actionDescription)
            {
                Description = actionDescription ?? throw new ArgumentNullException(nameof(actionDescription));
            }

            internal Reflection.InvocationInfo Description { get; }

            public class Sync : ExitActionBehavior
            {
                readonly Action<Transition> _action;

                public Sync(Action<Transition> action, Reflection.InvocationInfo actionDescription) : base(actionDescription)
                {
                    _action = action;
                }

                public override void Execute(Transition transition)
                {
                    _action(transition);
                }

                public override UniTask ExecuteAsync(Transition transition)
                {
                    Execute(transition);
                    return TaskResult.Done;
                }
            }

            public class Async : ExitActionBehavior
            {
                readonly Func<Transition, UniTask> _action;

                public Async(Func<Transition, UniTask> action, Reflection.InvocationInfo actionDescription) : base(actionDescription)
                {
                    _action = action;
                }

                public override void Execute(Transition transition)
                {
                    throw new InvalidOperationException(
                        $"Cannot execute asynchronous action specified in OnExit event for '{transition.Source}' state. " +
                         "Use asynchronous version of Fire [FireAsync]");
                }

                public override UniTask ExecuteAsync(Transition transition)
                {
                    return _action(transition);
                }
            }
        }
    }
}
