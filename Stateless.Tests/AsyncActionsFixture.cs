#if TASKS

using System;
using System.Collections;
using System.Threading.Tasks;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Internal;
using UnityEngine.TestTools;

namespace Stateless.Tests
{

    public class AsyncActionsFixture
    {
        [Test]
        public void StateMutatorShouldBeCalledOnlyOnce()
        {
            var state = State.B;
            var count = 0;
            var sm = new StateMachine<State, Trigger>(() => state, (s) => { state = s; count++; });
            sm.Configure(State.B).Permit(Trigger.X, State.C);
            sm.FireAsync(Trigger.X);
            Assert.AreEqual(1, count);
        }

        [UnityTest]
        public IEnumerator SuperStateShouldNotExitOnSubStateTransition_WhenUsingAsyncTriggers() => UniTask.ToCoroutine(async () =>
        {
            // Arrange.
            var sm = new StateMachine<State, Trigger>(State.A);
            var record = new List<string>();

            sm.Configure(State.A)
                .OnEntryAsync(() => UniTask.Run(() => record.Add("Entered state A")))
                .OnExitAsync(() => UniTask.Run(() => record.Add("Exited state A")))
                .Permit(Trigger.X, State.B);

            sm.Configure(State.B) // Our super state.
                .InitialTransition(State.C)
                .OnEntryAsync(() => UniTask.Run(() => record.Add("Entered super state B")))
                .OnExitAsync(() => UniTask.Run(() => record.Add("Exited super state B")));

            sm.Configure(State.C) // Our first sub state.
                .OnEntryAsync(() => UniTask.Run(() => record.Add("Entered sub state C")))
                .OnExitAsync(() => UniTask.Run(() => record.Add("Exited sub state C")))
                .Permit(Trigger.Y, State.D)
                .SubstateOf(State.B);
            sm.Configure(State.D) // Our second sub state.
                .OnEntryAsync(() => UniTask.Run(() => record.Add("Entered sub state D")))
                .OnExitAsync(() => UniTask.Run(() => record.Add("Exited sub state D")))
                .SubstateOf(State.B);


            // Act.
            await sm.FireAsync(Trigger.X);
            await sm.FireAsync(Trigger.Y);

            // Assert.
            Assert.AreEqual("Exited state A", record[0]);
            Assert.AreEqual("Entered super state B", record[1]);
            Assert.AreEqual("Entered sub state C", record[2]);
            Assert.AreEqual("Exited sub state C", record[3]);
            Assert.AreEqual("Entered sub state D", record[4]); // Before the patch the actual result was "Exited super state B"
        });

        [Test]
        public void SuperStateShouldNotExitOnSubStateTransition_WhenUsingSyncTriggers()
        {
            // Arrange.
            var sm = new StateMachine<State, Trigger>(State.A);
            var record = new List<string>();

            sm.Configure(State.A)
                .OnEntry(() => record.Add("Entered state A"))
                .OnExit(() => record.Add("Exited state A"))
                .Permit(Trigger.X, State.B);

            sm.Configure(State.B) // Our super state.
                .InitialTransition(State.C)
                .OnEntry(() => record.Add("Entered super state B"))
                .OnExit(() => record.Add("Exited super state B"));

            sm.Configure(State.C) // Our first sub state.
                .OnEntry(() => record.Add("Entered sub state C"))
                .OnExit(() => record.Add("Exited sub state C"))
                .Permit(Trigger.Y, State.D)
                .SubstateOf(State.B);
            sm.Configure(State.D) // Our second sub state.
                .OnEntry(() => record.Add("Entered sub state D"))
                .OnExit(() => record.Add("Exited sub state D"))
                .SubstateOf(State.B);


            // Act.
            sm.Fire(Trigger.X);
            sm.Fire(Trigger.Y);

            // Assert.
            Assert.AreEqual("Exited state A", record[0]);
            Assert.AreEqual("Entered super state B", record[1]);
            Assert.AreEqual("Entered sub state C", record[2]);
            Assert.AreEqual("Exited sub state C", record[3]);
            Assert.AreEqual("Entered sub state D", record[4]);
        }

        [UnityTest]
        public IEnumerator CanFireAsyncEntryAction() => UniTask.ToCoroutine(async () =>
        {
            var sm = new StateMachine<State, Trigger>(State.A);

            sm.Configure(State.A)
                .Permit(Trigger.X, State.B);

            var test = "";
            sm.Configure(State.B)
                .OnEntryAsync(() => UniTask.Run(() => test = "foo"));

            await sm.FireAsync(Trigger.X);

            Assert.AreEqual("foo", test); // Should await action
            Assert.AreEqual(State.B, sm.State); // Should transition to destination state
        });

        [Test]
        public void WhenSyncFireAsyncEntryAction()
        {
            var sm = new StateMachine<State, Trigger>(State.A);

            sm.Configure(State.A)
              .Permit(Trigger.X, State.B);

            sm.Configure(State.B)
              .OnEntryAsync(() => TaskResult.Done);

            Assert.Throws<InvalidOperationException>(() => sm.Fire(Trigger.X));
        }

        [UnityTest]
        public IEnumerator CanFireAsyncExitAction() => UniTask.ToCoroutine(async () =>
        {
            var sm = new StateMachine<State, Trigger>(State.A);

            var test = "";
            sm.Configure(State.A)
                .OnExitAsync(() => UniTask.Run(() => test = "foo"))
                .Permit(Trigger.X, State.B);

            await sm.FireAsync(Trigger.X);

            Assert.AreEqual("foo", test); // Should await action
            Assert.AreEqual(State.B, sm.State); // Should transition to destination state
        });

        [Test]
        public void WhenSyncFireAsyncExitAction()
        {
            var sm = new StateMachine<State, Trigger>(State.A);

            sm.Configure(State.A)
              .OnExitAsync(() => TaskResult.Done)
              .Permit(Trigger.X, State.B);

            Assert.Throws<InvalidOperationException>(() => sm.Fire(Trigger.X));
        }

        [UnityTest]
        public IEnumerator CanFireInternalAsyncAction() => UniTask.ToCoroutine(async () =>
        {
            var sm = new StateMachine<State, Trigger>(State.A);

            var test = "";
            sm.Configure(State.A)
                .InternalTransitionAsync(Trigger.X, () => UniTask.Run(() => test = "foo"));

            await sm.FireAsync(Trigger.X);

            Assert.AreEqual("foo", test); // Should await action
        });

        [Test]
        public void WhenSyncFireInternalAsyncAction()
        {
            var sm = new StateMachine<State, Trigger>(State.A);

            sm.Configure(State.A)
              .InternalTransitionAsync(Trigger.X, () => TaskResult.Done);

            Assert.Throws<InvalidOperationException>(() => sm.Fire(Trigger.X));
        }

        [UnityTest]
        public IEnumerator CanInvokeOnTransitionedAsyncAction() => UniTask.ToCoroutine(async () =>
        {
            var sm = new StateMachine<State, Trigger>(State.A);

            sm.Configure(State.A)
                .Permit(Trigger.X, State.B);

            var test = "";
            sm.OnTransitionedAsync(_ => UniTask.Run(() => test = "foo"));

            await sm.FireAsync(Trigger.X);

            Assert.AreEqual("foo", test); // Should await action
        });

        [UnityTest]
        public IEnumerator CanInvokeOnTransitionCompletedAsyncAction() => UniTask.ToCoroutine(async () =>
        {
            var sm = new StateMachine<State, Trigger>(State.A);

            sm.Configure(State.A)
                .Permit(Trigger.X, State.B);

            var test = "";
            sm.OnTransitionCompletedAsync(_ => UniTask.Run(() => test = "foo"));

            await sm.FireAsync(Trigger.X);

            Assert.AreEqual("foo", test); // Should await action
        });

        [UnityTest]
        public IEnumerator WillInvokeSyncOnTransitionedIfRegisteredAlongWithAsyncAction() => UniTask.ToCoroutine(async () =>
        {
            var sm = new StateMachine<State, Trigger>(State.A);

            sm.Configure(State.A)
                .Permit(Trigger.X, State.B);

            var test1 = "";
            var test2 = "";
            sm.OnTransitioned(_ => test1 = "foo1");
            sm.OnTransitionedAsync(_ => UniTask.Run(() => test2 = "foo2"));

            await sm.FireAsync(Trigger.X);

            Assert.AreEqual("foo1", test1);
            Assert.AreEqual("foo2", test2);
        });

        [UnityTest]
        public IEnumerator WillInvokeSyncOnTransitionCompletedIfRegisteredAlongWithAsyncAction() => UniTask.ToCoroutine(async () =>
        {
            var sm = new StateMachine<State, Trigger>(State.A);

            sm.Configure(State.A)
                .Permit(Trigger.X, State.B);

            var test1 = "";
            var test2 = "";
            sm.OnTransitionCompleted(_ => test1 = "foo1");
            sm.OnTransitionCompletedAsync(_ => UniTask.Run(() => test2 = "foo2"));

            await sm.FireAsync(Trigger.X);

            Assert.AreEqual("foo1", test1);
            Assert.AreEqual("foo2", test2);
        });

        [Test]
        public void WhenSyncFireAsyncOnTransitionedAction()
        {
            var sm = new StateMachine<State, Trigger>(State.A);

            sm.Configure(State.A)
              .Permit(Trigger.X, State.B);

            sm.OnTransitionedAsync(_ => TaskResult.Done);

            Assert.Throws<InvalidOperationException>(() => sm.Fire(Trigger.X));
        }

        [Test]
        public void WhenSyncFireAsyncOnTransitionCompletedAction()
        {
            var sm = new StateMachine<State, Trigger>(State.A);

            sm.Configure(State.A)
              .Permit(Trigger.X, State.B);

            sm.OnTransitionCompletedAsync(_ => TaskResult.Done);

            Assert.Throws<InvalidOperationException>(() => sm.Fire(Trigger.X));
        }

        [UnityTest]
        public IEnumerator CanInvokeOnUnhandledTriggerAsyncAction() => UniTask.ToCoroutine(async () =>
        {
            var sm = new StateMachine<State, Trigger>(State.A);

            sm.Configure(State.A)
                .Permit(Trigger.X, State.B);

            var test = "";
            sm.OnUnhandledTriggerAsync((s, t, u) => UniTask.Run(() => test = "foo"));

            await sm.FireAsync(Trigger.Z);

            Assert.AreEqual("foo", test); // Should await action
        });

        [Test]
        public void WhenSyncFireOnUnhandledTriggerAsyncTask()
        {
            var sm = new StateMachine<State, Trigger>(State.A);

            sm.Configure(State.A)
                .Permit(Trigger.X, State.B);

            sm.OnUnhandledTriggerAsync((s, t) => TaskResult.Done);

            Assert.Throws<InvalidOperationException>(() => sm.Fire(Trigger.Z));
        }

        [Test]
        public void WhenSyncFireOnUnhandledTriggerAsyncAction()
        {
            var sm = new StateMachine<State, Trigger>(State.A);

            sm.Configure(State.A)
              .Permit(Trigger.X, State.B);

            sm.OnUnhandledTriggerAsync((s, t, u) => TaskResult.Done);

            Assert.Throws<InvalidOperationException>(() => sm.Fire(Trigger.Z));
        }

        [UnityTest]
        public IEnumerator WhenActivateAsync() => UniTask.ToCoroutine(async () =>
        {
            var sm = new StateMachine<State, Trigger>(State.A);

            var activated = false;
            sm.Configure(State.A)
                .OnActivateAsync(() => UniTask.Run(() => activated = true));

            await sm.ActivateAsync();

            Assert.True(activated); // Should await action
        });

        [UnityTest]
        public IEnumerator WhenDeactivateAsync() => UniTask.ToCoroutine(async () =>
        {
            var sm = new StateMachine<State, Trigger>(State.A);

            var deactivated = false;
            sm.Configure(State.A)
                .OnDeactivateAsync(() => UniTask.Run(() => deactivated = true));

            await sm.ActivateAsync();
            await sm.DeactivateAsync();

            Assert.True(deactivated); // Should await action
        });

        [Test]
        public void WhenSyncActivateAsyncOnActivateAction()
        {
            var sm = new StateMachine<State, Trigger>(State.A);

            sm.Configure(State.A)
              .OnActivateAsync(() => TaskResult.Done);

            Assert.Throws<InvalidOperationException>(() => sm.Activate());
        }

        [Test]
        public void WhenSyncDeactivateAsyncOnDeactivateAction()
        {
            var sm = new StateMachine<State, Trigger>(State.A);

            sm.Configure(State.A)
              .OnDeactivateAsync(() => TaskResult.Done);

            sm.Activate();

            Assert.Throws<InvalidOperationException>(() => sm.Deactivate());
        }

        [UnityTest]
        public IEnumerator IfSelfTransitionPermited_ActionsFire_InSubstate_async() => UniTask.ToCoroutine(async () =>
        {
            var sm = new StateMachine<State, Trigger>(State.A);

            bool onEntryStateBfired = false;
            bool onExitStateBfired = false;
            bool onExitStateAfired = false;

            sm.Configure(State.B)
                .OnEntryAsync(t => UniTask.Run(() => onEntryStateBfired = true))
                .PermitReentry(Trigger.X)
                .OnExitAsync(t => UniTask.Run(() => onExitStateBfired = true));

            sm.Configure(State.A)
                .SubstateOf(State.B)
                .OnExitAsync(t => UniTask.Run(() => onExitStateAfired = true));

            await sm.FireAsync(Trigger.X);

            Assert.AreEqual(State.B, sm.State);
            Assert.True(onExitStateAfired);
            Assert.True(onExitStateBfired);
            Assert.True(onEntryStateBfired);
        });

        [UnityTest]
        public IEnumerator TransitionToSuperstateDoesNotExitSuperstate() => UniTask.ToCoroutine(async () =>
        {
            StateMachine<State, Trigger> sm = new StateMachine<State, Trigger>(State.B);

            bool superExit = false;
            bool superEntry = false;
            bool subExit = false;

            sm.Configure(State.A)
                .OnEntryAsync(t => UniTask.Run(() => superEntry = true))
                .OnExitAsync(t => UniTask.Run(() => superExit = true));

            sm.Configure(State.B)
                .SubstateOf(State.A)
                .Permit(Trigger.Y, State.A)
                .OnExitAsync(t => UniTask.Run(() => subExit = true));

            await sm.FireAsync(Trigger.Y);

            Assert.True(subExit);
            Assert.False(superEntry);
            Assert.False(superExit);
        });

        [UnityTest]
        public IEnumerator IgnoredTriggerMustBeIgnoredAsync() => UniTask.ToCoroutine(async () =>
        {
            bool nullRefExcThrown = false;
            var stateMachine = new StateMachine<State, Trigger>(State.B);
            stateMachine.Configure(State.A)
                .Permit(Trigger.X, State.C);

            stateMachine.Configure(State.B)
                .SubstateOf(State.A)
                .Ignore(Trigger.X);

            try
            {
                // >>> The following statement should not throw a NullReferenceException
                await stateMachine.FireAsync(Trigger.X);
            }
            catch (NullReferenceException)
            {
                nullRefExcThrown = true;
            }

            Assert.False(nullRefExcThrown);
        });

        [Test]
        public void VerifyNotEnterSuperstateWhenDoingInitialTransition()
        {
            var sm = new StateMachine<State, Trigger>(State.A);

            sm.Configure(State.A)
                .Permit(Trigger.X, State.B);

            sm.Configure(State.B)
                .InitialTransition(State.C)
                .OnEntry(() => sm.Fire(Trigger.Y))
                .Permit(Trigger.Y, State.D);

            sm.Configure(State.C)
                .SubstateOf(State.B)
                .Permit(Trigger.Y, State.D);

            sm.FireAsync(Trigger.X);

            Assert.AreEqual(State.D, sm.State);
        }
    }
}

#endif
