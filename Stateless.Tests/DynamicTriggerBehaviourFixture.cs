using System.Linq;
using NUnit.Framework;

namespace Stateless.Tests
{
    public class DynamicTriggerBehaviour
    {
        [Test]
        public void DestinationStateIsDynamic()
        {
            var sm = new StateMachine<State, Trigger>(State.A);
            sm.Configure(State.A)
                .PermitDynamic(Trigger.X, () => State.B);

            sm.Fire(Trigger.X);

            Assert.AreEqual(State.B, sm.State);
        }

        [Test]
        public void DestinationStateIsCalculatedBasedOnTriggerParameters()
        {
            var sm = new StateMachine<State, Trigger>(State.A);
            var trigger = sm.SetTriggerParameters<int>(Trigger.X);
            sm.Configure(State.A)
                .PermitDynamic(trigger, i => i == 1 ? State.B : State.C);

            sm.Fire(trigger, 1);

            Assert.AreEqual(State.B, sm.State);
        }

        [Test]
        public void Sdfsf()
        {
            var sm = new StateMachine<State, Trigger>(State.A);
            var trigger = sm.SetTriggerParameters<int>(Trigger.X);
            sm.Configure(State.A)
                .PermitDynamicIf(trigger, (i) => i == 1 ? State.C : State.B, (i) => i == 1 ? true : false);

            // Should not throw
            sm.GetPermittedTriggers().ToList();

            sm.Fire(trigger, 1);
        }
    }
}
