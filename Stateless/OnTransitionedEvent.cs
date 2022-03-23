using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Stateless
{
    public partial class StateMachine<TState, TTrigger>
    {
        class OnTransitionedEvent
        {
            private event Action<Transition> _onTransitioned;
            private readonly List<Func<Transition, UniTask>> _onTransitionedAsync = new List<Func<Transition, UniTask>>();

            public void Invoke(Transition transition)
            {
                if (_onTransitionedAsync.Count != 0)
                    throw new InvalidOperationException(
                        "Cannot execute asynchronous action specified as OnTransitioned callback. " +
                        "Use asynchronous version of Fire [FireAsync]");

                _onTransitioned?.Invoke(transition);
            }

#if TASKS
            public async UniTask InvokeAsync(Transition transition)
            {
                _onTransitioned?.Invoke(transition);

                foreach (var callback in _onTransitionedAsync)
                    await callback(transition);
            }
#endif

            public void Register(Action<Transition> action)
            {
                _onTransitioned += action;
            }

            public void Register(Func<Transition, UniTask> action)
            {
                _onTransitionedAsync.Add(action);
            }
        }
    }
}
