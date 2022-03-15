using System;
using NUnit.Framework;

namespace Stateless.Tests
{
    public class TransitioningTriggerBehaviourFixture
    {
        [Test]
        public void TransitionsToDestinationState()
        {
            var transtioning = new StateMachine<State, Trigger>.TransitioningTriggerBehaviour(Trigger.X, State.C, null);
            Assert.True(transtioning.ResultsInTransitionFrom(State.B, Array.Empty<object>(), out State destination));
            Assert.AreEqual(State.C, destination);
        }
    }
}
