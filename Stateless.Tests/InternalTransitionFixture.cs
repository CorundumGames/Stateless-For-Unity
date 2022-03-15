using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Stateless.Tests
{
    public class InternalTransitionFixture
    {

        /// <summary>
        /// The expected behaviour of the internal transistion is that the state does not change.
        /// This will fail if the state changes after the trigger has fired.
        /// </summary>
        [Test]
        public void StayInSameStateOneState_Transition()
        {
            var sm = new StateMachine<State, Trigger>(State.A);
            sm.Configure(State.A)
                .InternalTransition(Trigger.X, t => { });

            Assert.AreEqual(State.A, sm.State);
            sm.Fire(Trigger.X);
            Assert.AreEqual(State.A, sm.State);
        }

        [Test]
        public void StayInSameStateTwoStates_Transition()
        {
            var sm = new StateMachine<State, Trigger>(State.A);

            sm.Configure(State.A)
                .InternalTransition(Trigger.X, t => { })
                .Permit(Trigger.Y, State.B);

            sm.Configure(State.B)
                    .InternalTransition(Trigger.X, t => { })
                    .Permit(Trigger.Y, State.A);

            // This should not cause any state changes
            Assert.AreEqual(State.A, sm.State);
            sm.Fire(Trigger.X);
            Assert.AreEqual(State.A, sm.State);

            // Change state to B
            sm.Fire(Trigger.Y);

            // This should also not cause any state changes
            Assert.AreEqual(State.B, sm.State);
            sm.Fire(Trigger.X);
            Assert.AreEqual(State.B, sm.State);
        }
        [Test]
        public void StayInSameSubStateTransitionInSuperstate_Transition()
        {
            var sm = new StateMachine<State, Trigger>(State.B);

            sm.Configure(State.A)
                    .InternalTransition(Trigger.X, t => { });

            sm.Configure(State.B)
                    .SubstateOf(State.A);

            // This should not cause any state changes
            Assert.AreEqual(State.B, sm.State);
            sm.Fire(Trigger.X);
            Assert.AreEqual(State.B, sm.State);
        }
        [Test]
        public void StayInSameSubStateTransitionInSubstate_Transition()
        {
            var sm = new StateMachine<State, Trigger>(State.B);

            sm.Configure(State.A);

            sm.Configure(State.B)
                    .SubstateOf(State.A)
                    .InternalTransition(Trigger.X, t => { });

            // This should not cause any state changes
            Assert.AreEqual(State.B, sm.State);
            sm.Fire(Trigger.X);
            Assert.AreEqual(State.B, sm.State);
        }

        [Test]
        public void StayInSameStateOneState_Action()
        {
            var sm = new StateMachine<State, Trigger>(State.A);
            sm.Configure(State.A)
                .InternalTransition(Trigger.X, () => { });

            Assert.AreEqual(State.A, sm.State);
            sm.Fire(Trigger.X);
            Assert.AreEqual(State.A, sm.State);
        }

        [Test]
        public void StayInSameStateTwoStates_Action()
        {
            var sm = new StateMachine<State, Trigger>(State.A);

            sm.Configure(State.A)
                .InternalTransition(Trigger.X, () => { })
                .Permit(Trigger.Y, State.B);

            sm.Configure(State.B)
                    .InternalTransition(Trigger.X, () => { })
                    .Permit(Trigger.Y, State.A);

            // This should not cause any state changes
            Assert.AreEqual(State.A, sm.State);
            sm.Fire(Trigger.X);
            Assert.AreEqual(State.A, sm.State);

            // Change state to B
            sm.Fire(Trigger.Y);

            // This should also not cause any state changes
            Assert.AreEqual(State.B, sm.State);
            sm.Fire(Trigger.X);
            Assert.AreEqual(State.B, sm.State);
        }
        [Test]
        public void StayInSameSubStateTransitionInSuperstate_Action()
        {
            var sm = new StateMachine<State, Trigger>(State.B);

            sm.Configure(State.A)
                    .InternalTransition(Trigger.X, () => { })
                    .InternalTransition(Trigger.Y, () => { });

            sm.Configure(State.B)
                    .SubstateOf(State.A);

            // This should not cause any state changes
            Assert.AreEqual(State.B, sm.State);
            sm.Fire(Trigger.X);
            Assert.AreEqual(State.B, sm.State);
            sm.Fire(Trigger.Y);
            Assert.AreEqual(State.B, sm.State);
        }
        [Test]
        public void StayInSameSubStateTransitionInSubstate_Action()
        {
            var sm = new StateMachine<State, Trigger>(State.B);

            sm.Configure(State.A);

            sm.Configure(State.B)
                    .SubstateOf(State.A)
                    .InternalTransition(Trigger.X, () => { })
                    .InternalTransition(Trigger.Y, () => { });

            // This should not cause any state changes
            Assert.AreEqual(State.B, sm.State);
            sm.Fire(Trigger.X);
            Assert.AreEqual(State.B, sm.State);
            sm.Fire(Trigger.Y);
            Assert.AreEqual(State.B, sm.State);
        }

        [Test]
        public void AllowTriggerWithTwoParameters()
        {
            var sm = new StateMachine<State, Trigger>(State.B);
            var trigger = sm.SetTriggerParameters<int, string>(Trigger.X);
            const int intParam = 5;
            const string strParam = "Five";
            var callbackInvoked = false;

            sm.Configure(State.B)
                .InternalTransition(trigger, (i, s, transition) =>
                {
                    callbackInvoked = true;
                    Assert.AreEqual(intParam, i);
                    Assert.AreEqual(strParam, s);
                });

            sm.Fire(trigger, intParam, strParam);
            Assert.True(callbackInvoked);
        }

        [Test]
        public void AllowTriggerWithThreeParameters()
        {
            var sm = new StateMachine<State, Trigger>(State.B);
            var trigger = sm.SetTriggerParameters<int, string, bool>(Trigger.X);
            const int intParam = 5;
            const string strParam = "Five";
            var boolParam = true;
            var callbackInvoked = false;

            sm.Configure(State.B)
                .InternalTransition(trigger, (i, s, b, transition) =>
                {
                    callbackInvoked = true;
                    Assert.AreEqual(intParam, i);
                    Assert.AreEqual(strParam, s);
                    Assert.AreEqual(boolParam, b);
                });

            sm.Fire(trigger, intParam, strParam, boolParam);
            Assert.True(callbackInvoked);
        }

        [Test]
        public void ConditionalInternalTransition_ShouldBeReflectedInPermittedTriggers()
        {
            var isPermitted = true;
            var sm = new StateMachine<State, Trigger>(State.A);
            sm.Configure(State.A)
                .InternalTransitionIf(Trigger.X, u => isPermitted, t => { });

            Assert.AreEqual(1, sm.GetPermittedTriggers().ToArray().Length);
            isPermitted = false;
            Assert.AreEqual(0, sm.GetPermittedTriggers().ToArray().Length);
        }

        [Test]
        public void InternalTriggerHandledOnlyOnceInSuper()
        {
            State handledIn = State.C;

            var sm = new StateMachine<State, Trigger>(State.A);

            sm.Configure(State.A)
                .InternalTransition(Trigger.X, () => handledIn = State.A);

            sm.Configure(State.B)
                .SubstateOf(State.A)
                .InternalTransition(Trigger.X, () => handledIn = State.B);

            // The state machine is in state A. It should only be handled in in State A, so handledIn should be equal to State.A
            sm.Fire(Trigger.X);

            Assert.AreEqual(State.A, handledIn);
        }
        [Test]
        public void InternalTriggerHandledOnlyOnceInSub()
        {
            State handledIn = State.C;

            var sm = new StateMachine<State, Trigger>(State.B);

            sm.Configure(State.A)
                .InternalTransition(Trigger.X, () => handledIn = State.A);

            sm.Configure(State.B)
                .SubstateOf(State.A)
                .InternalTransition(Trigger.X, () => handledIn = State.B);

            // The state machine is in state B. It should only be handled in in State B, so handledIn should be equal to State.B
            sm.Fire(Trigger.X);

            Assert.AreEqual(State.B, handledIn);
        }
        [Test]
        public void OnlyOneHandlerExecuted()
        {
            var handled = 0;

            var sm = new StateMachine<State, Trigger>(State.A);

            sm.Configure(State.A)
                .InternalTransition(Trigger.X, () => handled++)
                .InternalTransition(Trigger.Y, () => handled++);

            sm.Fire(Trigger.X);

            Assert.AreEqual(1, handled);

            sm.Fire(Trigger.Y);

            Assert.AreEqual(2, handled);
        }

        [UnityTest]
        public IEnumerator AsyncHandlesNonAsyndActionAsync() => UniTask.ToCoroutine(async () =>
        {
            var handled = false;

            var sm = new StateMachine<State, Trigger>(State.A);

            sm.Configure(State.A)
                .InternalTransition(Trigger.Y, () => handled = true);

            await sm.FireAsync(Trigger.Y);

            Assert.True(handled);
        });
    }
}
