#if TASKS

using System;
using Cysharp.Threading.Tasks;

namespace Stateless
{
    public partial class StateMachine<TState, TTrigger>
    {
        internal partial class StateRepresentation
        {
            public void AddActivateAction(Func<UniTask> action, Reflection.InvocationInfo activateActionDescription)
            {
                ActivateActions.Add(new ActivateActionBehaviour.Async(_state, action, activateActionDescription));
            }

            public void AddDeactivateAction(Func<UniTask> action, Reflection.InvocationInfo deactivateActionDescription)
            {
                DeactivateActions.Add(new DeactivateActionBehaviour.Async(_state, action, deactivateActionDescription));
            }

            public void AddEntryAction(TTrigger trigger, Func<Transition, object[], UniTask> action, Reflection.InvocationInfo entryActionDescription)
            {
                if (action == null) throw new ArgumentNullException(nameof(action));

                EntryActions.Add(
                    new EntryActionBehavior.Async((t, args) =>
                    {
                        if (t.Trigger.Equals(trigger))
                            return action(t, args);

                        return TaskResult.Done;
                    },
                    entryActionDescription));
            }

            public void AddEntryAction(Func<Transition, object[], UniTask> action, Reflection.InvocationInfo entryActionDescription)
            {
                EntryActions.Add(
                    new EntryActionBehavior.Async(
                        action,
                        entryActionDescription));
            }

            public void AddExitAction(Func<Transition, UniTask> action, Reflection.InvocationInfo exitActionDescription)
            {
                ExitActions.Add(new ExitActionBehavior.Async(action, exitActionDescription));
            }

            public async UniTask ActivateAsync()
            {
                if (_superstate != null)
                    await _superstate.ActivateAsync();

                await ExecuteActivationActionsAsync();
            }

            public async UniTask DeactivateAsync()
            {
                await ExecuteDeactivationActionsAsync();

                if (_superstate != null)
                    await _superstate.DeactivateAsync();
            }

            async UniTask ExecuteActivationActionsAsync()
            {
                foreach (var action in ActivateActions)
                    await action.ExecuteAsync();
            }

            async UniTask ExecuteDeactivationActionsAsync()
            {
                foreach (var action in DeactivateActions)
                    await action.ExecuteAsync();
            }


            public async UniTask EnterAsync(Transition transition, params object[] entryArgs)
            {
                if (transition.IsReentry)
                {
                    await ExecuteEntryActionsAsync(transition, entryArgs);
                }
                else if (!Includes(transition.Source))
                {
                    if (_superstate != null && transition is not InitialTransition)
                        await _superstate.EnterAsync(transition, entryArgs);

                    await ExecuteEntryActionsAsync(transition, entryArgs);
                }
            }
            
            public async UniTask<Transition> ExitAsync(Transition transition)
            {
                if (transition.IsReentry)
                {
                    await ExecuteExitActionsAsync(transition);
                }
                else if (!Includes(transition.Destination))
                {
                    await ExecuteExitActionsAsync(transition);

                    // Must check if there is a superstate, and if we are leaving that superstate
                    if (_superstate != null)
                    {
                        // Check if destination is within the state list
                        if (IsIncludedIn(transition.Destination))
                        {
                            // Destination state is within the list, exit first superstate only if it is NOT the first
                            if (!_superstate.UnderlyingState.Equals(transition.Destination))
                            {
                                return await _superstate.ExitAsync(transition);
                            }
                        }
                        else
                        {
                            // Exit the superstate as well
                            return await _superstate.ExitAsync(transition);
                        }
                    }
                }
                return transition;
            }

            async UniTask ExecuteEntryActionsAsync(Transition transition, object[] entryArgs)
            {
                foreach (var action in EntryActions)
                    await action.ExecuteAsync(transition, entryArgs);
            }

            async UniTask ExecuteExitActionsAsync(Transition transition)
            {
                foreach (var action in ExitActions)
                    await action.ExecuteAsync(transition);
            }
        }
    }
}

#endif
