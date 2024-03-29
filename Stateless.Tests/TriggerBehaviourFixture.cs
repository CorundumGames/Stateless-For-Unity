﻿using System;
using NUnit.Framework;

namespace Stateless.Tests
{
    public class TriggerBehaviourFixture
    {
        [Test]
        public void ExposesCorrectUnderlyingTrigger()
        {
            var transitioning = new StateMachine<State, Trigger>.TransitioningTriggerBehaviour(
                Trigger.X, State.C, null);

            Assert.AreEqual(Trigger.X, transitioning.Trigger);
        }

        protected bool False(params object[] args)
        {
            return false;
        }

        [Test]
        public void WhenGuardConditionFalse_GuardConditionsMetIsFalse()
        {
            var transitioning = new StateMachine<State, Trigger>.TransitioningTriggerBehaviour(
                Trigger.X, State.C, new StateMachine<State, Trigger>.TransitionGuard(False));

            Assert.False(transitioning.GuardConditionsMet());
        }

        protected bool True(params object[] args)
        {
            return true;
        }

        [Test]
        public void WhenGuardConditionTrue_GuardConditionsMetIsTrue()
        {
            var transitioning = new StateMachine<State, Trigger>.TransitioningTriggerBehaviour(
                Trigger.X, State.C, new StateMachine<State, Trigger>.TransitionGuard(True));

            Assert.True(transitioning.GuardConditionsMet());
        }

        [Test]
        public void WhenOneOfMultipleGuardConditionsFalse_GuardConditionsMetIsFalse()
        {
            var falseGuard = new[] {
                new Tuple<Func<object[], bool>, string>(args => true, "1"),
                new Tuple<Func<object[], bool>, string>(args => true, "2")
            };

            var transitioning = new StateMachine<State, Trigger>.TransitioningTriggerBehaviour(
                Trigger.X, State.C, new StateMachine<State, Trigger>.TransitionGuard(falseGuard));

            Assert.True(transitioning.GuardConditionsMet());
        }

        [Test]
        public void WhenAllMultipleGuardConditionsFalse_IsGuardConditionsMetIsFalse()
        {
            var falseGuard = new[] {
                new Tuple<Func<object[], bool>, string>(args => false, "1"),
                new Tuple<Func<object[], bool>, string>(args => false, "2")
            };

            var transitioning = new StateMachine<State, Trigger>.TransitioningTriggerBehaviour(
                Trigger.X, State.C, new StateMachine<State, Trigger>.TransitionGuard(falseGuard));

            Assert.False(transitioning.GuardConditionsMet());
        }

        [Test]
        public void WhenAllGuardConditionsTrue_GuardConditionsMetIsTrue()
        {
            var trueGuard = new[] {
                new Tuple<Func<object[], bool>, string>(args => true, "1"),
                new Tuple<Func<object[], bool>, string>(args => true, "2")
            };

            var transitioning = new StateMachine<State, Trigger>.TransitioningTriggerBehaviour(
                Trigger.X, State.C, new StateMachine<State, Trigger>.TransitionGuard(trueGuard));

            Assert.True(transitioning.GuardConditionsMet());
        }
    }
}
