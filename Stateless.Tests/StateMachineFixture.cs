using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Stateless.Tests
{
    public class StateMachineFixture
    {
        const string
            StateA = "A", StateB = "B", StateC = "C",
            TriggerX = "X", TriggerY = "Y";

        int _numCalls = 0;

        void CountCalls()
        {
            _numCalls++;
        }

        [Test]
        public void CanUseReferenceTypeMarkers()
        {
            RunSimpleTest(
                new[] { StateA, StateB, StateC },
                new[] { TriggerX, TriggerY });
        }

        [Test]
        public void CanUseValueTypeMarkers()
        {
            RunSimpleTest(
                Enum.GetValues(typeof(State)).Cast<State>(),
                Enum.GetValues(typeof(Trigger)).Cast<Trigger>());
        }

        void RunSimpleTest<TState, TTransition>(IEnumerable<TState> states, IEnumerable<TTransition> transitions)
        {
            var a = states.First();
            var b = states.Skip(1).First();
            var x = transitions.First();

            var sm = new StateMachine<TState, TTransition>(a);

            sm.Configure(a)
                .Permit(x, b);

            sm.Fire(x);

            Assert.AreEqual(b, sm.State);
        }

        [Test]
        public void InitialStateIsCurrent()
        {
            var initial = State.B;
            var sm = new StateMachine<State, Trigger>(initial);
            Assert.AreEqual(initial, sm.State);
        }

        [Test]
        public void StateCanBeStoredExternally()
        {
            var state = State.B;
            var sm = new StateMachine<State, Trigger>(() => state, s => state = s);
            sm.Configure(State.B).Permit(Trigger.X, State.C);
            Assert.AreEqual(State.B, sm.State);
            Assert.AreEqual(State.B, state);
            sm.Fire(Trigger.X);
            Assert.AreEqual(State.C, sm.State);
            Assert.AreEqual(State.C, state);
        }

        [Test]
        public void StateMutatorShouldBeCalledOnlyOnce()
        {
            var state = State.B;
            var count = 0;
            var sm = new StateMachine<State, Trigger>(() => state, (s) => { state = s; count++; });
            sm.Configure(State.B).Permit(Trigger.X, State.C);
            sm.Fire(Trigger.X);
            Assert.AreEqual(1, count);
        }

        [Test]
        public void SubstateIsIncludedInCurrentState()
        {
            var sm = new StateMachine<State, Trigger>(State.B);
            sm.Configure(State.B).SubstateOf(State.C);

            Assert.AreEqual(State.B, sm.State);
            Assert.True(sm.IsInState(State.C));
        }

        [Test]
        public void WhenInSubstate_TriggerIgnoredInSuperstate_RemainsInSubstate()
        {
            var sm = new StateMachine<State, Trigger>(State.B);

            sm.Configure(State.B)
                .SubstateOf(State.C);

            sm.Configure(State.C)
                .Ignore(Trigger.X);

            sm.Fire(Trigger.X);

            Assert.AreEqual(State.B, sm.State);
        }

        [Test]
        public void PermittedTriggersIncludeSuperstatePermittedTriggers()
        {
            var sm = new StateMachine<State, Trigger>(State.B);

            sm.Configure(State.A)
                .Permit(Trigger.Z, State.B);

            sm.Configure(State.B)
                .SubstateOf(State.C)
                .Permit(Trigger.X, State.A);

            sm.Configure(State.C)
                .Permit(Trigger.Y, State.A);

            var permitted = sm.GetPermittedTriggers();

            Assert.True(permitted.Contains(Trigger.X));
            Assert.True(permitted.Contains(Trigger.Y));
            Assert.False(permitted.Contains(Trigger.Z));
        }

        [Test]
        public void PermittedTriggersAreDistinctValues()
        {
            var sm = new StateMachine<State, Trigger>(State.B);

            sm.Configure(State.B)
                .SubstateOf(State.C)
                .Permit(Trigger.X, State.A);

            sm.Configure(State.C)
                .Permit(Trigger.X, State.B);

            var permitted = sm.GetPermittedTriggers();
            Assert.AreEqual(1, permitted.Count());
            Assert.AreEqual(Trigger.X, permitted.First());
        }

        [Test]
        public void AcceptedTriggersRespectGuards()
        {
            var sm = new StateMachine<State, Trigger>(State.B);

            sm.Configure(State.B)
                .PermitIf(Trigger.X, State.A, () => false);

            Assert.AreEqual(0, sm.GetPermittedTriggers().Count());
        }

        [Test]
        public void AcceptedTriggersRespectMultipleGuards()
        {
            var sm = new StateMachine<State, Trigger>(State.B);

            sm.Configure(State.B)
                .PermitIf(Trigger.X, State.A,
                    new Tuple<Func<bool>, string>(() => true, "1"),
                    new Tuple<Func<bool>, string>(() => false, "2"));

            Assert.AreEqual(0, sm.GetPermittedTriggers().Count());
        }

        [Test]
        public void WhenDiscriminatedByGuard_ChoosesPermitedTransition()
        {
            var sm = new StateMachine<State, Trigger>(State.B);

            sm.Configure(State.B)
                .PermitIf(Trigger.X, State.A, () => false)
                .PermitIf(Trigger.X, State.C, () => true);

            sm.Fire(Trigger.X);

            Assert.AreEqual(State.C, sm.State);
        }

        [Test]
        public void WhenDiscriminatedByMultiConditionGuard_ChoosesPermitedTransition()
        {
            var sm = new StateMachine<State, Trigger>(State.B);

            sm.Configure(State.B)
                .PermitIf(Trigger.X, State.A,
                    new Tuple<Func<bool>, string>(() => true, "1"),
                    new Tuple<Func<bool>, string>(() => false, "2"))
                .PermitIf(Trigger.X, State.C,
                    new Tuple<Func<bool>, string>(() => true, "1"),
                    new Tuple<Func<bool>, string>(() => true, "2"));

            sm.Fire(Trigger.X);

            Assert.AreEqual(State.C, sm.State);
        }

        [Test]
        public void WhenTriggerIsIgnored_ActionsNotExecuted()
        {
            var sm = new StateMachine<State, Trigger>(State.B);

            bool fired = false;

            sm.Configure(State.B)
                .OnEntry(t => fired = true)
                .Ignore(Trigger.X);

            sm.Fire(Trigger.X);

            Assert.False(fired);
        }

        [Test]
        public void IfSelfTransitionPermited_ActionsFire()
        {
            var sm = new StateMachine<State, Trigger>(State.B);

            bool fired = false;

            sm.Configure(State.B)
                .OnEntry(t => fired = true)
                .PermitReentry(Trigger.X);

            sm.Fire(Trigger.X);

            Assert.True(fired);
        }

        [Test]
        public void ImplicitReentryIsDisallowed()
        {
            var sm = new StateMachine<State, Trigger>(State.B);

            Assert.Throws<ArgumentException>(() => sm.Configure(State.B)
               .Permit(Trigger.X, State.B));
        }

        [Test]
        public void TriggerParametersAreImmutableOnceSet()
        {
            var sm = new StateMachine<State, Trigger>(State.B);
            sm.SetTriggerParameters<string, int>(Trigger.X);
            Assert.Throws<InvalidOperationException>(() => sm.SetTriggerParameters<string>(Trigger.X));
        }

        [Test]
        public void ExceptionThrownForInvalidTransition()
        {
            var sm = new StateMachine<State, Trigger>(State.A);
            var exception = Assert.Throws<InvalidOperationException>(() => sm.Fire(Trigger.X));
            Assert.AreEqual(exception.Message, "No valid leaving transitions are permitted from state 'A' for trigger 'X'. Consider ignoring the trigger.");
        }

        [Test]
        public void ExceptionThrownForInvalidTransitionMentionsGuardDescriptionIfPresent()
        {
            // If guard description is empty then method name of guard is used
            // so I have skipped empty description test.
            const string guardDescription = "test";

            var sm = new StateMachine<State, Trigger>(State.A);
            sm.Configure(State.A).PermitIf(Trigger.X, State.B, () => false, guardDescription);
            var exception = Assert.Throws<InvalidOperationException>(() => sm.Fire(Trigger.X));
            Assert.AreEqual(typeof(InvalidOperationException), exception.GetType());
        }

        [Test]
        public void ExceptionThrownForInvalidTransitionMentionsMultiGuardGuardDescriptionIfPresent()
        {
            var sm = new StateMachine<State, Trigger>(State.A);
            sm.Configure(State.A).PermitIf(Trigger.X, State.B,
                new Tuple<Func<bool>, string>(() => false, "test1"),
                new Tuple<Func<bool>, string>(() => false, "test2"));

            var exception = Assert.Throws<InvalidOperationException>(() => sm.Fire(Trigger.X));
            Assert.AreEqual(typeof(InvalidOperationException), exception.GetType());
        }

        [Test]
        public void ParametersSuppliedToFireArePassedToEntryAction()
        {
            var sm = new StateMachine<State, Trigger>(State.B);

            var x = sm.SetTriggerParameters<string, int>(Trigger.X);

            sm.Configure(State.B)
                .Permit(Trigger.X, State.C);

            string entryArgS = null;
            int entryArgI = 0;

            sm.Configure(State.C)
                .OnEntryFrom(x, (s, i) =>
                {
                    entryArgS = s;
                    entryArgI = i;
                });

            var suppliedArgS = "something";
            var suppliedArgI = 42;

            sm.Fire(x, suppliedArgS, suppliedArgI);

            Assert.AreEqual(suppliedArgS, entryArgS);
            Assert.AreEqual(suppliedArgI, entryArgI);
        }

        [Test]
        public void WhenAnUnhandledTriggerIsFired_TheProvidedHandlerIsCalledWithStateAndTrigger()
        {
            var sm = new StateMachine<State, Trigger>(State.B);

            State? state = null;
            Trigger? trigger = null;
            sm.OnUnhandledTrigger((s, t, u) =>
            {
                state = s;
                trigger = t;
            });

            sm.Fire(Trigger.Z);

            Assert.AreEqual(State.B, state);
            Assert.AreEqual(Trigger.Z, trigger);
        }

        [Test]
        public void WhenATransitionOccurs_TheOnTransitionedEventFires()
        {
            var sm = new StateMachine<State, Trigger>(State.B);

            sm.Configure(State.B)
                .Permit(Trigger.X, State.A);

            StateMachine<State, Trigger>.Transition transition = null;
            sm.OnTransitioned(t => transition = t);

            sm.Fire(Trigger.X);

            Assert.NotNull(transition);
            Assert.AreEqual(Trigger.X, transition.Trigger);
            Assert.AreEqual(State.B, transition.Source);
            Assert.AreEqual(State.A, transition.Destination);
            Assert.AreEqual(new object[0], transition.Parameters);
        }

        [Test]
        public void WhenATransitionOccurs_TheOnTransitionCompletedEventFires()
        {
            var sm = new StateMachine<State, Trigger>(State.B);

            sm.Configure(State.B)
                .Permit(Trigger.X, State.A);

            StateMachine<State, Trigger>.Transition transition = null;
            sm.OnTransitionCompleted(t => transition = t);

            sm.Fire(Trigger.X);

            Assert.NotNull(transition);
            Assert.AreEqual(Trigger.X, transition.Trigger);
            Assert.AreEqual(State.B, transition.Source);
            Assert.AreEqual(State.A, transition.Destination);
            Assert.AreEqual(new object[0], transition.Parameters);
        }

        /// <summary>
        /// The expected ordering is OnExit, OnTransitioned, OnEntry, OnTransitionCompleted
        /// </summary>
        [Test]
        public void TheOnTransitionedEventFiresBeforeTheOnEntryEventAndOnTransitionCompletedFiresAfterwards()
        {
            var sm = new StateMachine<State, Trigger>(State.B);
            var expectedOrdering = new List<string> { "OnExit", "OnTransitioned", "OnEntry", "OnTransitionCompleted" };
            var actualOrdering = new List<string>();

            sm.Configure(State.B)
                .Permit(Trigger.X, State.A)
                .OnExit(() => actualOrdering.Add("OnExit"));

            sm.Configure(State.A)
                .OnEntry(() => actualOrdering.Add("OnEntry"));

            sm.OnTransitioned(t => actualOrdering.Add("OnTransitioned"));
            sm.OnTransitionCompleted(t => actualOrdering.Add("OnTransitionCompleted"));

            sm.Fire(Trigger.X);

            Assert.AreEqual(expectedOrdering.Count, actualOrdering.Count);
            for (int i = 0; i < expectedOrdering.Count; i++)
            {
                Assert.AreEqual(expectedOrdering[i], actualOrdering[i]);
            }
        }

        [Test]
        public void WhenATransitionOccurs_WithAParameterizedTrigger_TheOnTransitionedEventFires()
        {
            var sm = new StateMachine<State, Trigger>(State.B);
            var triggerX = sm.SetTriggerParameters<string>(Trigger.X);

            sm.Configure(State.B)
                .Permit(Trigger.X, State.A);

            StateMachine<State, Trigger>.Transition transition = null;
            sm.OnTransitioned(t => transition = t);

            string parameter = "the parameter";
            sm.Fire(triggerX, parameter);

            Assert.NotNull(transition);
            Assert.AreEqual(Trigger.X, transition.Trigger);
            Assert.AreEqual(State.B, transition.Source);
            Assert.AreEqual(State.A, transition.Destination);
            Assert.AreEqual(1, transition.Parameters.Count());
            Assert.AreEqual(parameter, transition.Parameters[0]);
        }

        [Test]
        public void WhenATransitionOccurs_WithAParameterizedTrigger_TheOnTransitionCompletedEventFires()
        {
            var sm = new StateMachine<State, Trigger>(State.B);
            var triggerX = sm.SetTriggerParameters<string>(Trigger.X);

            sm.Configure(State.B)
                .Permit(Trigger.X, State.A);

            StateMachine<State, Trigger>.Transition transition = null;
            sm.OnTransitionCompleted(t => transition = t);

            string parameter = "the parameter";
            sm.Fire(triggerX, parameter);

            Assert.NotNull(transition);
            Assert.AreEqual(Trigger.X, transition.Trigger);
            Assert.AreEqual(State.B, transition.Source);
            Assert.AreEqual(State.A, transition.Destination);
            Assert.AreEqual(1, transition.Parameters.Count());
            Assert.AreEqual(parameter, transition.Parameters[0]);
        }

        [Test]
        public void WhenATransitionOccurs_WithAParameterizedTrigger_WithMultipleParameters_TheOnTransitionedEventFires()
        {
            var sm = new StateMachine<State, Trigger>(State.B);
            var triggerX = sm.SetTriggerParameters<string, int, bool>(Trigger.X);

            sm.Configure(State.B)
                .Permit(Trigger.X, State.A);

            StateMachine<State, Trigger>.Transition transition = null;
            sm.OnTransitioned(t => transition = t);

            string firstParameter = "the parameter";
            int secondParameter = 99;
            bool thirdParameter = true;
            sm.Fire(triggerX, firstParameter, secondParameter, thirdParameter);

            Assert.NotNull(transition);
            Assert.AreEqual(Trigger.X, transition.Trigger);
            Assert.AreEqual(State.B, transition.Source);
            Assert.AreEqual(State.A, transition.Destination);
            Assert.AreEqual(3, transition.Parameters.Count());
            Assert.AreEqual(firstParameter, transition.Parameters[0]);
            Assert.AreEqual(secondParameter, transition.Parameters[1]);
            Assert.AreEqual(thirdParameter, transition.Parameters[2]);
        }

        [Test]
        public void WhenATransitionOccurs_WithAParameterizedTrigger_WithMultipleParameters_TheOnTransitionCompletedEventFires()
        {
            var sm = new StateMachine<State, Trigger>(State.B);
            var triggerX = sm.SetTriggerParameters<string, int, bool>(Trigger.X);

            sm.Configure(State.B)
                .Permit(Trigger.X, State.A);

            StateMachine<State, Trigger>.Transition transition = null;
            sm.OnTransitionCompleted(t => transition = t);

            string firstParameter = "the parameter";
            int secondParameter = 99;
            bool thirdParameter = true;
            sm.Fire(triggerX, firstParameter, secondParameter, thirdParameter);

            Assert.NotNull(transition);
            Assert.AreEqual(Trigger.X, transition.Trigger);
            Assert.AreEqual(State.B, transition.Source);
            Assert.AreEqual(State.A, transition.Destination);
            Assert.AreEqual(3, transition.Parameters.Count());
            Assert.AreEqual(firstParameter, transition.Parameters[0]);
            Assert.AreEqual(secondParameter, transition.Parameters[1]);
            Assert.AreEqual(thirdParameter, transition.Parameters[2]);
        }

        [Test]
        public void DirectCyclicConfigurationDetected()
        {
            var sm = new StateMachine<State, Trigger>(State.A);

            Assert.Throws(typeof(ArgumentException), () => { sm.Configure(State.A).SubstateOf(State.A); });
        }

        [Test]
        public void NestedCyclicConfigurationDetected()
        {
            var sm = new StateMachine<State, Trigger>(State.A);
            sm.Configure(State.B).SubstateOf(State.A);

            Assert.Throws(typeof(ArgumentException), () => { sm.Configure(State.A).SubstateOf(State.B); });
        }

        [Test]
        public void NestedTwoLevelsCyclicConfigurationDetected()
        {
            var sm = new StateMachine<State, Trigger>(State.A);
            sm.Configure(State.B).SubstateOf(State.A);
            sm.Configure(State.C).SubstateOf(State.B);

            Assert.Throws(typeof(ArgumentException), () => { sm.Configure(State.A).SubstateOf(State.C); });
        }

        [Test]
        public void DelayedNestedCyclicConfigurationDetected()
        {
            // Set up two states and substates, then join them
            var sm = new StateMachine<State, Trigger>(State.A);
            sm.Configure(State.B).SubstateOf(State.A);

            sm.Configure(State.C);
            sm.Configure(State.A).SubstateOf(State.C);

            Assert.Throws(typeof(ArgumentException), () => { sm.Configure(State.C).SubstateOf(State.B); });
        }

        [Test]
        public void IgnoreVsPermitReentry()
        {
            var sm = new StateMachine<State, Trigger>(State.A);

            sm.Configure(State.A)
                .OnEntry(CountCalls)
                .PermitReentry(Trigger.X)
                .Ignore(Trigger.Y);

            _numCalls = 0;

            sm.Fire(Trigger.X);
            sm.Fire(Trigger.Y);

            Assert.AreEqual(1, _numCalls);
        }

        [Test]
        public void IgnoreVsPermitReentryFrom()
        {
            var sm = new StateMachine<State, Trigger>(State.A);

            sm.Configure(State.A)
                .OnEntryFrom(Trigger.X, CountCalls)
                .OnEntryFrom(Trigger.Y, CountCalls)
                .PermitReentry(Trigger.X)
                .Ignore(Trigger.Y);

            _numCalls = 0;

            sm.Fire(Trigger.X);
            sm.Fire(Trigger.Y);

            Assert.AreEqual(1, _numCalls);
        }

        [Test]
        public void IfSelfTransitionPermited_ActionsFire_InSubstate()
        {
            var sm = new StateMachine<State, Trigger>(State.A);

            bool onEntryStateBfired = false;
            bool onExitStateBfired = false;
            bool onExitStateAfired = false;

            sm.Configure(State.B)
                .OnEntry(t => onEntryStateBfired = true)
                .PermitReentry(Trigger.X)
                .OnExit(t => onExitStateBfired = true);

            sm.Configure(State.A)
                .SubstateOf(State.B)
                .OnExit(t => onExitStateAfired = true);

            sm.Fire(Trigger.X);

            Assert.AreEqual(State.B, sm.State);
            Assert.True(onEntryStateBfired);
            Assert.True(onExitStateBfired);
            Assert.True(onExitStateAfired);
        }

        [Test]
        public void TransitionWhenParameterizedGuardTrue()
        {
            var sm = new StateMachine<State, Trigger>(State.A);
            var x = sm.SetTriggerParameters<int>(Trigger.X);
            sm.Configure(State.A).PermitIf(x, State.B, i => i == 2);
            sm.Fire(x, 2);

            Assert.AreEqual(sm.State, State.B);
        }

        [Test]
        public void ExceptionWhenParameterizedGuardFalse()
        {
            var sm = new StateMachine<State, Trigger>(State.A);
            var x = sm.SetTriggerParameters<int>(Trigger.X);
            sm.Configure(State.A).PermitIf(x, State.B, i => i == 3);

            Assert.Throws<InvalidOperationException>(() => sm.Fire(x, 2));
        }

        [Test]
        public void TransitionWhenBothParameterizedGuardClausesTrue()
        {
            var sm = new StateMachine<State, Trigger>(State.A);
            var x = sm.SetTriggerParameters<int>(Trigger.X);
            var positiveGuard = Tuple.Create(new Func<int, bool>(o => o == 2), "Positive Guard");
            var negativeGuard = Tuple.Create(new Func<int, bool>(o => o != 3), "Negative Guard");
            sm.Configure(State.A).PermitIf(x, State.B, positiveGuard, negativeGuard);
            sm.Fire(x, 2);

            Assert.AreEqual(sm.State, State.B);
        }

        [Test]
        public void ExceptionWhenBothParameterizedGuardClausesFalse()
        {
            var sm = new StateMachine<State, Trigger>(State.A);
            var x = sm.SetTriggerParameters<int>(Trigger.X);
            // Create Two guards that both must be true
            var positiveGuard = Tuple.Create(new Func<int, bool>(o => o == 2), "Positive Guard");
            var negativeGuard = Tuple.Create(new Func<int, bool>(o => o != 3), "Negative Guard");
            sm.Configure(State.A).PermitIf(x, State.B, positiveGuard, negativeGuard);

            Assert.Throws<InvalidOperationException>(() => sm.Fire(x, 3));
        }

        [Test]
        public void TransitionWhenGuardReturnsTrueOnTriggerWithMultipleParameters()
        {
            var sm = new StateMachine<State, Trigger>(State.A);
            var x = sm.SetTriggerParameters<string, int>(Trigger.X);
            sm.Configure(State.A).PermitIf(x, State.B, (s, i) => s == "3" && i == 3);
            sm.Fire(x, "3", 3);
            Assert.AreEqual(sm.State, State.B);
        }

        [Test]
        public void ExceptionWhenGuardFalseOnTriggerWithMultipleParameters()
        {
            var sm = new StateMachine<State, Trigger>(State.A);
            var x = sm.SetTriggerParameters<string, int>(Trigger.X);
            sm.Configure(State.A).PermitIf(x, State.B, (s, i) => s == "3" && i == 3);
            Assert.AreEqual(sm.State, State.A);

            Assert.Throws<InvalidOperationException>(() => sm.Fire(x, "2", 2));
            Assert.Throws<InvalidOperationException>(() => sm.Fire(x, "3", 2));
            Assert.Throws<InvalidOperationException>(() => sm.Fire(x, "2", 3));
        }

        [Test]
        public void TransitionWhenPermitIfHasMultipleExclusiveGuards()
        {
            var sm = new StateMachine<State, Trigger>(State.A);
            var x = sm.SetTriggerParameters<int>(Trigger.X);
            sm.Configure(State.A)
                .PermitIf(x, State.B, i => i == 3)
                .PermitIf(x, State.C, i => i == 2);
            sm.Fire(x, 3);
            Assert.AreEqual(sm.State, State.B);
        }

        [Test]
        public void ExceptionWhenPermitIfHasMultipleNonExclusiveGuards()
        {
            var sm = new StateMachine<State, Trigger>(State.A);
            var x = sm.SetTriggerParameters<int>(Trigger.X);
            sm.Configure(State.A).PermitIf(x, State.B, i => i % 2 == 0)  // Is Even
                .PermitIf(x, State.C, i => i == 2);

            Assert.Throws<InvalidOperationException>(() => sm.Fire(x, 2));
        }

        [Test]
        public void TransitionWhenPermitDyanmicIfHasMultipleExclusiveGuards()
        {
            var sm = new StateMachine<State, Trigger>(State.A);
            var x = sm.SetTriggerParameters<int>(Trigger.X);
            sm.Configure(State.A)
                .PermitDynamicIf(x, i => i == 3 ? State.B : State.C, i => i == 3 || i == 5)
                .PermitDynamicIf(x, i => i == 2 ? State.C : State.D, i => i == 2 || i == 4);
            sm.Fire(x, 3);
            Assert.AreEqual(sm.State, State.B);
        }

        [Test]
        public void ExceptionWhenPermitDyanmicIfHasMultipleNonExclusiveGuards()
        {
            var sm = new StateMachine<State, Trigger>(State.A);
            var x = sm.SetTriggerParameters<int>(Trigger.X);
            sm.Configure(State.A).PermitDynamicIf(x, i => i == 4 ? State.B : State.C, i => i % 2 == 0)
                .PermitDynamicIf(x, i => i == 2 ? State.C : State.D, i => i == 2);

            Assert.Throws<InvalidOperationException>(() => sm.Fire(x, 2));
        }

        [Test]
        public void TransitionWhenPermitIfHasMultipleExclusiveGuardsWithSuperStateTrue()
        {
            var sm = new StateMachine<State, Trigger>(State.B);
            var x = sm.SetTriggerParameters<int>(Trigger.X);
            sm.Configure(State.A).PermitIf(x, State.D, i => i == 3);
            {
                sm.Configure(State.B).SubstateOf(State.A).PermitIf(x, State.C, i => i == 2);
            }
            sm.Fire(x, 3);
            Assert.AreEqual(sm.State, State.D);
        }

        [Test]
        public void TransitionWhenPermitIfHasMultipleExclusiveGuardsWithSuperStateFalse()
        {
            var sm = new StateMachine<State, Trigger>(State.B);
            var x = sm.SetTriggerParameters<int>(Trigger.X);
            sm.Configure(State.A).PermitIf(x, State.D, i => i == 3);
            {
                sm.Configure(State.B).SubstateOf(State.A).PermitIf(x, State.C, i => i == 2);
            }
            sm.Fire(x, 2);
            Assert.AreEqual(sm.State, State.C);
        }

        [Test]
        public void TransitionWhenPermitReentryIfParameterizedGuardTrue()
        {
            var sm = new StateMachine<State, Trigger>(State.A);
            var x = sm.SetTriggerParameters<int>(Trigger.X);
            sm.Configure(State.A)
                .PermitReentryIf(x, i => i == 3);
            sm.Fire(x, 3);
            Assert.AreEqual(sm.State, State.A);
        }

        [Test]
        public void TransitionWhenPermitReentryIfParameterizedGuardFalse()
        {
            var sm = new StateMachine<State, Trigger>(State.A);
            var x = sm.SetTriggerParameters<int>(Trigger.X);
            sm.Configure(State.A)
                .PermitReentryIf(x, i => i == 3);

            Assert.Throws<InvalidOperationException>(() => sm.Fire(x, 2));
        }

        [Test]
        public void NoTransitionWhenIgnoreIfParameterizedGuardTrue()
        {
            var sm = new StateMachine<State, Trigger>(State.A);
            var x = sm.SetTriggerParameters<int>(Trigger.X);
            sm.Configure(State.A).IgnoreIf(x, i => i == 3);
            sm.Fire(x, 3);

            Assert.AreEqual(sm.State, State.A);
        }

        [Test]
        public void ExceptionWhenIgnoreIfParameterizedGuardFalse()
        {
            var sm = new StateMachine<State, Trigger>(State.A);
            var x = sm.SetTriggerParameters<int>(Trigger.X);
            sm.Configure(State.A).IgnoreIf(x, i => i == 3);

            Assert.Throws<InvalidOperationException>(() => sm.Fire(x, 2));
        }

        /// <summary>
        /// Verifies guard clauses are only called one time during a transition evaluation.
        /// </summary>
        [Test]
        public void GuardClauseCalledOnlyOnce()
        {
            var sm = new StateMachine<State, Trigger>(State.A);
            int i = 0;

            sm.Configure(State.A).PermitIf(Trigger.X, State.B, () =>
            {
                ++i;
                return true;
            });

            sm.Fire(Trigger.X);

            Assert.AreEqual(1, i);
        }
        [Test]
        public void NoExceptionWhenPermitIfHasMultipleExclusiveGuardsBothFalse()
        {
            var sm = new StateMachine<State, Trigger>(State.A);
            bool onUnhandledTriggerWasCalled = false;
            sm.OnUnhandledTrigger((s, t) => { onUnhandledTriggerWasCalled = true; });  // NEVER CALLED
            int i = 0;
            sm.Configure(State.A)
                .PermitIf(Trigger.X, State.B, () => i == 2)
                .PermitIf(Trigger.X, State.C, () => i == 1);

            sm.Fire(Trigger.X);  // THROWS EXCEPTION

            Assert.True(onUnhandledTriggerWasCalled, "OnUnhandledTrigger was called");
            Assert.AreEqual(sm.State, State.A);
        }

        [Test]
        public void TransitionToSuperstateDoesNotExitSuperstate()
        {
            StateMachine<State, Trigger> sm = new StateMachine<State, Trigger>(State.B);

            bool superExit = false;
            bool superEntry = false;
            bool subExit = false;

            sm.Configure(State.A)
                .OnEntry(() => superEntry = true)
                .OnExit(() => superExit = true);

            sm.Configure(State.B)
                .SubstateOf(State.A)
                .Permit(Trigger.Y, State.A)
                .OnExit(() => subExit = true);

            sm.Fire(Trigger.Y);

            Assert.True(subExit);
            Assert.False(superEntry);
            Assert.False(superExit);
        }

        [Test]
        public void OnExitFiresOnlyOnceReentrySubstate()
        {
            var sm = new StateMachine<State, Trigger>(State.A);

            int exitB = 0;
            int exitA = 0;
            int entryB = 0;
            int entryA = 0;

            sm.Configure(State.A)
                .SubstateOf(State.B)
                .OnEntry(() => entryA++)
                .PermitReentry(Trigger.X)
                .OnExit(() => exitA++);

            sm.Configure(State.B)
                .OnEntry(() => entryB++)
                .OnExit(() => exitB++);

            sm.Fire(Trigger.X);

            Assert.AreEqual(0, exitB);
            Assert.AreEqual(0, entryB);
            Assert.AreEqual(1, exitA);
            Assert.AreEqual(1, entryA);
        }

        [Test]
        public void WhenConfigurePermittedTransitionOnTriggerWithoutParameters_ThenStateMachineCanFireTrigger()
        {
            var trigger = Trigger.X;
            var sm = new StateMachine<State, Trigger>(State.A);
            sm.Configure(State.A).Permit(trigger, State.B);
            Assert.True(sm.CanFire(trigger));
        }
        [Test]
        public void WhenConfigurePermittedTransitionOnTriggerWithoutParameters_ThenStateMachineCanEnumeratePermittedTriggers()
        {
            var trigger = Trigger.X;
            var sm = new StateMachine<State, Trigger>(State.A);
            sm.Configure(State.A).Permit(trigger, State.B);
            Assert.That(sm.PermittedTriggers, Is.EquivalentTo(new [] {trigger}));
        }


        [Test]
        public void WhenConfigurePermittedTransitionOnTriggerWithParameters_ThenStateMachineCanFireTrigger()
        {
            var trigger = Trigger.X;
            var sm = new StateMachine<State, Trigger>(State.A);
            sm.Configure(State.A).Permit(trigger, State.B);
            sm.SetTriggerParameters<string>(trigger);
            Assert.True(sm.CanFire(trigger));
        }
        [Test]
        public void WhenConfigurePermittedTransitionOnTriggerWithParameters_ThenStateMachineCanEnumeratePermittedTriggers()
        {
            var trigger = Trigger.X;
            var sm = new StateMachine<State, Trigger>(State.A);
            sm.Configure(State.A).Permit(trigger, State.B);
            sm.SetTriggerParameters<string>(trigger);
            Assert.That(sm.PermittedTriggers, Is.EquivalentTo(new [] {trigger}));
        }

        [Test]
        public void WhenConfigureInternalTransitionOnTriggerWithoutParameters_ThenStateMachineCanFireTrigger()
        {
            var trigger = Trigger.X;
            var sm = new StateMachine<State, Trigger>(State.A);
            sm.Configure(State.A).InternalTransition(trigger, (_) => { });
            Assert.True(sm.CanFire(trigger));
        }

        [Test]
        public void WhenConfigureInternalTransitionOnTriggerWithoutParameters_ThenStateMachineCanEnumeratePermittedTriggers()
        {
            var trigger = Trigger.X;
            var sm = new StateMachine<State, Trigger>(State.A);
            sm.Configure(State.A).InternalTransition(trigger, (_) => { });
            Assert.That(sm.PermittedTriggers, Is.EquivalentTo(new [] {trigger}));
        }

        [Test]
        public void WhenConfigureInternalTransitionOnTriggerWithParameters_ThenStateMachineCanFireTrigger()
        {
            var trigger = Trigger.X;
            var sm = new StateMachine<State, Trigger>(State.A);
            sm.Configure(State.A).InternalTransition(sm.SetTriggerParameters<string>(trigger), (arg, _) => { });
            Assert.True(sm.CanFire(trigger));
        }

        [Test]
        public void WhenConfigureInternalTransitionOnTriggerWithParameters_ThenStateMachineCanEnumeratePermittedTriggers()
        {
            var trigger = Trigger.X;
            var sm = new StateMachine<State, Trigger>(State.A);
            sm.Configure(State.A).InternalTransition(sm.SetTriggerParameters<string>(trigger), (arg, _) => { });
            Assert.That(sm.PermittedTriggers, Is.EquivalentTo(new [] {trigger}));
        }

        [Test]
        public void WhenConfigureConditionallyPermittedTransitionOnTriggerWithParameters_ThenStateMachineCanFireTrigger()
        {
            var trigger = Trigger.X;
            var sm = new StateMachine<State, Trigger>(State.A);
            sm.Configure(State.A).PermitIf(sm.SetTriggerParameters<string>(trigger), State.B, _ => true);
            Assert.True(sm.CanFire(trigger));
        }

        [Test]
        public void WhenConfigureConditionallyPermittedTransitionOnTriggerWithParameters_ThenStateMachineCanEnumeratePermittedTriggers()
        {
            var trigger = Trigger.X;
            var sm = new StateMachine<State, Trigger>(State.A);
            sm.Configure(State.A).PermitIf(sm.SetTriggerParameters<string>(trigger), State.B, _ => true);
            Assert.That(sm.PermittedTriggers, Is.EquivalentTo(new [] {trigger}));
        }

        [Test]
        public void PermittedTriggersIncludeAllDefinedTriggers()
        {
            var sm = new StateMachine<State, Trigger>(State.A);
            sm.Configure(State.A)
                .Permit(Trigger.X, State.B)
                .InternalTransition(Trigger.Y, _ => { })
                .Ignore(Trigger.Z);
            Assert.That(sm.PermittedTriggers, Is.SupersetOf(new[] { Trigger.X, Trigger.Y, Trigger.Z }));
        }

        [Test]
        public void PermittedTriggersExcludeAllUndefinedTriggers()
        {
            var sm = new StateMachine<State, Trigger>(State.A);
            sm.Configure(State.A)
                .Permit(Trigger.X, State.B);
            Assert.That(sm.PermittedTriggers, Does.Not.Contains(Trigger.Y).And.Not.Contains(Trigger.Z));
        }

        [Test]
        public void PermittedTriggersIncludeAllInheritedTriggers()
        {
            State superState = State.A,
                    subState = State.B,
                    otherState = State.C;
            Trigger superStateTrigger = Trigger.X,
                    subStateTrigger = Trigger.Y;

            StateMachine<State, Trigger> hsm(State initialState)
                => new StateMachine<State, Trigger>(initialState)
                        .Configure(superState)
                        .Permit(superStateTrigger, otherState)
                    .Machine
                        .Configure(subState)
                        .SubstateOf(superState)
                        .Permit(subStateTrigger, otherState)
                    .Machine;

            var hsmInSuperstate = hsm(superState);
            var hsmInSubstate = hsm(subState);

            Assert.That(hsmInSubstate.PermittedTriggers, Is.SupersetOf(hsmInSuperstate.PermittedTriggers));
            Assert.That(hsmInSubstate.PermittedTriggers, Contains.Item(superStateTrigger));
            Assert.That(hsmInSuperstate.PermittedTriggers, Contains.Item(superStateTrigger));
            Assert.That(hsmInSubstate.PermittedTriggers, Contains.Item(subStateTrigger));
            Assert.That(hsmInSuperstate.PermittedTriggers, Does.Not.Contains(subStateTrigger));
        }

        [Test]
        public void CanFire_GetUnmetGuardDescriptionsIfGuardFails()
        {
            const string guardDescription = "Guard failed";
            var sm = new StateMachine<State, Trigger>(State.A);
            sm.Configure(State.A)
              .PermitIf(Trigger.X, State.B, ()=> false, guardDescription);

            bool result = sm.CanFire(Trigger.X, out ICollection<string> unmetGuards);

            Assert.False(result);
            Assert.True(unmetGuards?.Count == 1);
            Assert.That(unmetGuards, Contains.Item(guardDescription));
        }

        [Test]
        public void CanFire_GetNullUnmetGuardDescriptionsIfInvalidTrigger()
        {
            var sm = new StateMachine<State, Trigger>(State.A);
            bool result = sm.CanFire(Trigger.X, out ICollection<string> unmetGuards);

            Assert.False(result);
            Assert.Null(unmetGuards);
        }

        [Test]
        public void CanFire_GetEmptyUnmetGuardDescriptionsIfValidTrigger()
        {
            var sm = new StateMachine<State, Trigger>(State.A);
            sm.Configure(State.A).Permit(Trigger.X, State.B);
            bool result = sm.CanFire(Trigger.X, out ICollection<string> unmetGuards);

            Assert.True(result);
            Assert.True(unmetGuards?.Count == 0);
        }

        [Test]
        public void CanFire_GetEmptyUnmetGuardDescriptionsIfGuardPasses()
        {
            var sm = new StateMachine<State, Trigger>(State.A);
            sm.Configure(State.A).PermitIf(Trigger.X, State.B, () => true, "Guard passed");
            bool result = sm.CanFire(Trigger.X, out ICollection<string> unmetGuards);

            Assert.True(result);
            Assert.True(unmetGuards?.Count == 0);
        }

    }
}
