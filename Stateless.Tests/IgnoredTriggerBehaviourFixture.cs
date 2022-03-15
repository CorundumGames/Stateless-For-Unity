using System;
using NUnit.Framework;

namespace Stateless.Tests
{
    public class IgnoredTriggerBehaviourFixture
    {
        [Test]
        public void StateRemainsUnchanged()
        {
            var ignored = new StateMachine<State, Trigger>.IgnoredTriggerBehaviour(Trigger.X, null);
            Assert.False(ignored.ResultsInTransitionFrom(State.B, Array.Empty<object>(), out _));
        }

        [Test]
        public void ExposesCorrectUnderlyingTrigger()
        {
            var ignored = new StateMachine<State, Trigger>.IgnoredTriggerBehaviour(
                Trigger.X, null);

            Assert.AreEqual(Trigger.X, ignored.Trigger);
        }

        protected bool False(params object[] args)
        {
            return false;
        }

        [Test]
        public void WhenGuardConditionFalse_IsGuardConditionMetIsFalse()
        {
            var ignored = new StateMachine<State, Trigger>.IgnoredTriggerBehaviour(
                Trigger.X, new StateMachine<State, Trigger>.TransitionGuard(False));

            Assert.False(ignored.GuardConditionsMet());
        }

        protected bool True(params object[] args)
        {
            return true;
        }

        [Test]
        public void WhenGuardConditionTrue_IsGuardConditionMetIsTrue()
        {
            var ignored = new StateMachine<State, Trigger>.IgnoredTriggerBehaviour(
                Trigger.X, new StateMachine<State, Trigger>.TransitionGuard(True));

            Assert.True(ignored.GuardConditionsMet());
        }
        [Test]
        public void IgnoredTriggerMustBeIgnoredSync()
        {
            bool internalActionExecuted = false;
            var stateMachine = new StateMachine<State, Trigger>(State.B);
            stateMachine.Configure(State.A)
                .Permit(Trigger.X, State.C);

            stateMachine.Configure(State.B)
                .SubstateOf(State.A)
                .Ignore(Trigger.X);

            try
            {
                // >>> The following statement should not execute the internal action
                stateMachine.Fire(Trigger.X);
            }
            catch (NullReferenceException)
            {
                internalActionExecuted = true;
            }

            Assert.False(internalActionExecuted);
        }

        [Test]
        public void IgnoreIfTrueTriggerMustBeIgnored()
        {
            var stateMachine = new StateMachine<State, Trigger>(State.B);
            stateMachine.Configure(State.A)
                .Permit(Trigger.X, State.C);

            stateMachine.Configure(State.B)
                .SubstateOf(State.A)
                .IgnoreIf(Trigger.X, () => true);

                stateMachine.Fire(Trigger.X);

            Assert.AreEqual(State.B, stateMachine.State);
        }
        [Test]
        public void IgnoreIfFalseTriggerMustNotBeIgnored()
        {
            var stateMachine = new StateMachine<State, Trigger>(State.B);
            stateMachine.Configure(State.A)
                .Permit(Trigger.X, State.C);

            stateMachine.Configure(State.B)
                .SubstateOf(State.A)
                .IgnoreIf(Trigger.X, () => false);

            stateMachine.Fire(Trigger.X);

            Assert.AreEqual(State.C, stateMachine.State);
        }
    }
}
