using Stateless.Reflection;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Stateless.Tests
{
    public class ReflectionFixture
    {
        static readonly string UserDescription = "UserDescription";

        bool IsTrue()
        {
            return true;
        }

        void OnEntry()
        {

        }

        void OnEntryInt(int i)
        {

        }

        void OnEntryIntInt(int i, int j)
        {

        }

        void OnEntryIntIntInt(int i, int j, int k)
        {

        }

        void OnEntryTrans(StateMachine<State, Trigger>.Transition trans)
        {
        }

        void OnEntryIntTrans(int i, StateMachine<State, Trigger>.Transition trans)
        {
        }

        void OnExit()
        {

        }

        UniTask OnActivateAsync()
        {
            return TaskResult.Done;
        }

        void OnActivate()
        {
        }

        UniTask OnEntryTransAsync(StateMachine<State, Trigger>.Transition trans)
        {
            return TaskResult.Done;
        }

        UniTask OnEntryAsync()
        {
            return TaskResult.Done;
        }

        UniTask OnDeactivateAsync()
        {
            return TaskResult.Done;
        }

        void OnDeactivate()
        {
        }

        void OnExitTrans(StateMachine<State, Trigger>.Transition trans)
        {
        }

        UniTask OnExitAsync()
        {
            return TaskResult.Done;
        }

        UniTask OnExitTransAsync(StateMachine<State, Trigger>.Transition trans)
        {
            return TaskResult.Done;
        }

        bool Permit()
        {
            return true;
        }

        [Test]
        public void SimpleTransition_Binding()
        {
            var sm = new StateMachine<State, Trigger>(State.A);

            sm.Configure(State.A)
                .Permit(Trigger.X, State.B);

            StateMachineInfo inf = sm.GetInfo();

            Assert.AreEqual(typeof(Trigger), inf.TriggerType);
            Assert.AreEqual(2, inf.States.Count());
            var binding = inf.States.Single(s => (State)s.UnderlyingState == State.A);

            Assert.That(binding.UnderlyingState, Is.InstanceOf<State>());
            Assert.AreEqual(State.A, (State)binding.UnderlyingState);
            //
            Assert.Zero(binding.Substates.Count());
            Assert.Null(binding.Superstate);
            Assert.Zero(binding.EntryActions.Count());
            Assert.Zero(binding.ExitActions.Count());
            //
            Assert.AreEqual(1, binding.FixedTransitions.Count());
            foreach (FixedTransitionInfo trans in binding.FixedTransitions)
            {
                Assert.That(trans.Trigger.UnderlyingTrigger, Is.InstanceOf<Trigger>());
                Assert.AreEqual(Trigger.X, (Trigger)trans.Trigger.UnderlyingTrigger);
                //
                Assert.That(trans.DestinationState.UnderlyingState, Is.InstanceOf<State>());
                Assert.AreEqual(State.B, (State)trans.DestinationState.UnderlyingState);
                Assert.Zero(trans.GuardConditionsMethodDescriptions.Count());
            }
            Assert.Zero(binding.IgnoredTriggers.Count());
            Assert.Zero(binding.DynamicTransitions.Count());
        }

        [Test]
        public void TwoSimpleTransitions_Binding()
        {
            var sm = new StateMachine<State, Trigger>(State.A);

            sm.Configure(State.A)
                .Permit(Trigger.X, State.B)
                .Permit(Trigger.Y, State.C);

            StateMachineInfo inf = sm.GetInfo();

            Assert.True(inf.StateType == typeof(State));
            Assert.AreEqual(inf.TriggerType, typeof(Trigger));
            Assert.AreEqual(inf.States.Count(), 3);
            var binding = inf.States.Single(s => (State)s.UnderlyingState == State.A);

            Assert.True(binding.UnderlyingState is State);
            Assert.AreEqual(State.A, (State)binding.UnderlyingState); // Binding state value mismatch
            //
            Assert.AreEqual(0, binding.Substates.Count()); //  Binding substate count mismatch"
            Assert.AreEqual(null, binding.Superstate);
            Assert.AreEqual(0, binding.EntryActions.Count()); //  Binding entry actions count mismatch
            Assert.AreEqual(0, binding.ExitActions.Count());
            //
            Assert.AreEqual(2, binding.FixedTransitions.Count()); // Transition count mismatch
            //
            bool haveXB = false;
            bool haveYC = false;
            foreach (FixedTransitionInfo trans in binding.FixedTransitions)
            {
                Assert.True(trans.Trigger.UnderlyingTrigger is Trigger);
                //
                Assert.True(trans.DestinationState.UnderlyingState is State);
                Assert.AreEqual(0, trans.GuardConditionsMethodDescriptions.Count());
                //
                // Can't make assumptions about which trigger/destination comes first in the list
                if ((Trigger)trans.Trigger.UnderlyingTrigger == Trigger.X)
                {
                    Assert.AreEqual(State.B, (State)trans.DestinationState.UnderlyingState);
                    Assert.False(haveXB);
                    haveXB = true;
                }
                else if ((Trigger)trans.Trigger.UnderlyingTrigger == Trigger.Y)
                {
                    Assert.AreEqual(State.C, (State)trans.DestinationState.UnderlyingState);
                    Assert.False(haveYC);
                    haveYC = true;
                }
                else
                    throw new NUnitException("Failed.");
            }
            Assert.True(haveXB && haveYC);
            //
            Assert.AreEqual(0, binding.IgnoredTriggers.Count());
            Assert.AreEqual(0, binding.DynamicTransitions.Count());
        }

        [Test]
        public void WhenDiscriminatedByAnonymousGuard_Binding()
        {
            bool anonymousGuard() => true;

            var sm = new StateMachine<State, Trigger>(State.A);

            sm.Configure(State.A)
                .PermitIf(Trigger.X, State.B, anonymousGuard);

            StateMachineInfo inf = sm.GetInfo();

            Assert.True(inf.StateType == typeof(State));
            Assert.AreEqual(inf.TriggerType, typeof(Trigger));
            Assert.AreEqual(inf.States.Count(), 2);
            var binding = inf.States.Single(s => (State)s.UnderlyingState == State.A);

            Assert.True(binding.UnderlyingState is State);
            Assert.AreEqual(State.A, (State)binding.UnderlyingState);
            //
            Assert.AreEqual(0, binding.Substates.Count());
            Assert.AreEqual(null, binding.Superstate);
            Assert.AreEqual(0, binding.EntryActions.Count());
            Assert.AreEqual(0, binding.ExitActions.Count());
            //
            Assert.AreEqual(1, binding.FixedTransitions.Count());
            foreach (FixedTransitionInfo trans in binding.FixedTransitions)
            {
                Assert.True(trans.Trigger.UnderlyingTrigger is Trigger);
                Assert.AreEqual(Trigger.X, (Trigger)trans.Trigger.UnderlyingTrigger);
                //
                Assert.True(trans.DestinationState.UnderlyingState is State);
                Assert.AreEqual(State.B, (State)trans.DestinationState.UnderlyingState);
                //
                Assert.NotZero(trans.GuardConditionsMethodDescriptions.Count());
            }
            Assert.AreEqual(0, binding.IgnoredTriggers.Count());
            Assert.AreEqual(0, binding.DynamicTransitions.Count());
        }

        [Test]
        public void WhenDiscriminatedByAnonymousGuardWithDescription_Binding()
        {
            bool anonymousGuard() => true;

            var sm = new StateMachine<State, Trigger>(State.A);

            sm.Configure(State.A)
                .PermitIf(Trigger.X, State.B, anonymousGuard, "description");

            StateMachineInfo inf = sm.GetInfo();

            Assert.True(inf.StateType == typeof(State));
            Assert.AreEqual(inf.TriggerType, typeof(Trigger));
            Assert.AreEqual(inf.States.Count(), 2);
            var binding = inf.States.Single(s => (State)s.UnderlyingState == State.A);

            Assert.True(binding.UnderlyingState is State);
            Assert.AreEqual(State.A, (State)binding.UnderlyingState);
            //
            Assert.AreEqual(0, binding.Substates.Count());
            Assert.AreEqual(null, binding.Superstate);
            Assert.AreEqual(0, binding.EntryActions.Count());
            Assert.AreEqual(0, binding.ExitActions.Count());
            //
            Assert.AreEqual(1, binding.FixedTransitions.Count());
            foreach (FixedTransitionInfo trans in binding.FixedTransitions)
            {
                Assert.True(trans.Trigger.UnderlyingTrigger is Trigger);
                Assert.AreEqual(Trigger.X, (Trigger)trans.Trigger.UnderlyingTrigger);
                //
                Assert.True(trans.DestinationState.UnderlyingState is State);
                Assert.AreEqual(State.B, (State)trans.DestinationState.UnderlyingState);
                //
                Assert.AreEqual(1, trans.GuardConditionsMethodDescriptions.Count());
                Assert.AreEqual("description", trans.GuardConditionsMethodDescriptions.First().Description);
            }
            Assert.AreEqual(0, binding.IgnoredTriggers.Count());
            Assert.AreEqual(0, binding.DynamicTransitions.Count());
        }

        [Test]
        public void WhenDiscriminatedByNamedDelegate_Binding()
        {
            var sm = new StateMachine<State, Trigger>(State.A);

            sm.Configure(State.A)
                .PermitIf(Trigger.X, State.B, IsTrue);

            StateMachineInfo inf = sm.GetInfo();

            Assert.True(inf.StateType == typeof(State));
            Assert.AreEqual(inf.TriggerType, typeof(Trigger));
            Assert.AreEqual(inf.States.Count(), 2);
            var binding = inf.States.Single(s => (State)s.UnderlyingState == State.A);

            Assert.True(binding.UnderlyingState is State);
            Assert.AreEqual(State.A, (State)binding.UnderlyingState);
            //
            Assert.AreEqual(0, binding.Substates.Count());
            Assert.AreEqual(null, binding.Superstate);
            Assert.AreEqual(0, binding.EntryActions.Count());
            Assert.AreEqual(0, binding.ExitActions.Count());
            //
            Assert.AreEqual(1, binding.FixedTransitions.Count());
            foreach (FixedTransitionInfo trans in binding.FixedTransitions)
            {
                Assert.True(trans.Trigger.UnderlyingTrigger is Trigger);
                Assert.AreEqual(Trigger.X, (Trigger)trans.Trigger.UnderlyingTrigger);
                //
                Assert.True(trans.DestinationState.UnderlyingState is State);
                Assert.AreEqual(State.B, (State)trans.DestinationState.UnderlyingState);
                //
                Assert.AreEqual(1, trans.GuardConditionsMethodDescriptions.Count());
                Assert.AreEqual("IsTrue", trans.GuardConditionsMethodDescriptions.First().Description);
            }
            Assert.AreEqual(0, binding.IgnoredTriggers.Count());
            Assert.AreEqual(0, binding.DynamicTransitions.Count());
        }

        [Test]
        public void WhenDiscriminatedByNamedDelegateWithDescription_Binding()
        {
            var sm = new StateMachine<State, Trigger>(State.A);

            sm.Configure(State.A)
                .PermitIf(Trigger.X, State.B, IsTrue, "description");

            StateMachineInfo inf = sm.GetInfo();

            Assert.True(inf.StateType == typeof(State));
            Assert.AreEqual(inf.TriggerType, typeof(Trigger));
            Assert.AreEqual(inf.States.Count(), 2);
            var binding = inf.States.Single(s => (State)s.UnderlyingState == State.A);

            Assert.True(binding.UnderlyingState is State);
            Assert.AreEqual(State.A, (State)binding.UnderlyingState);
            //
            Assert.AreEqual(0, binding.Substates.Count());
            Assert.AreEqual(null, binding.Superstate);
            Assert.AreEqual(0, binding.EntryActions.Count());
            Assert.AreEqual(0, binding.ExitActions.Count());
            //
            Assert.AreEqual(1, binding.FixedTransitions.Count());
            foreach (FixedTransitionInfo trans in binding.FixedTransitions)
            {
                Assert.True(trans.Trigger.UnderlyingTrigger is Trigger);
                Assert.AreEqual(Trigger.X, (Trigger)trans.Trigger.UnderlyingTrigger);
                //
                Assert.True(trans.DestinationState.UnderlyingState is State);
                Assert.AreEqual(State.B, (State)trans.DestinationState.UnderlyingState);
                //
                Assert.AreEqual(1, trans.GuardConditionsMethodDescriptions.Count());
                Assert.AreEqual("description", trans.GuardConditionsMethodDescriptions.First().Description);
            }
            Assert.AreEqual(0, binding.IgnoredTriggers.Count());
            Assert.AreEqual(0, binding.DynamicTransitions.Count());
        }

        [Test]
        public void DestinationStateIsDynamic_Binding()
        {
            var sm = new StateMachine<State, Trigger>(State.A);
            sm.Configure(State.A)
                .PermitDynamic(Trigger.X, () => State.B);

            StateMachineInfo inf = sm.GetInfo();

            Assert.True(inf.StateType == typeof(State));
            Assert.AreEqual(inf.TriggerType, typeof(Trigger));
            Assert.AreEqual(inf.States.Count(), 1);
            var binding = inf.States.Single(s => (State)s.UnderlyingState == State.A);

            Assert.True(binding.UnderlyingState is State);
            Assert.AreEqual(State.A, (State)binding.UnderlyingState);
            //
            Assert.AreEqual(0, binding.Substates.Count());
            Assert.AreEqual(null, binding.Superstate);
            Assert.AreEqual(0, binding.EntryActions.Count());
            Assert.AreEqual(0, binding.ExitActions.Count());
            //
            Assert.AreEqual(0, binding.FixedTransitions.Count()); // Binding transition count mismatch
            Assert.AreEqual(0, binding.IgnoredTriggers.Count());
            Assert.AreEqual(1, binding.DynamicTransitions.Count()); // Dynamic transition count mismatch
            foreach (DynamicTransitionInfo trans in binding.DynamicTransitions)
            {
                Assert.True(trans.Trigger.UnderlyingTrigger is Trigger);
                Assert.AreEqual(Trigger.X, (Trigger)trans.Trigger.UnderlyingTrigger);
                Assert.AreEqual(0, trans.GuardConditionsMethodDescriptions.Count());
            }
        }

        [Test]
        public void DestinationStateIsCalculatedBasedOnTriggerParameters_Binding()
        {
            var sm = new StateMachine<State, Trigger>(State.A);
            var trigger = sm.SetTriggerParameters<int>(Trigger.X);
            sm.Configure(State.A)
                .PermitDynamic(trigger, i => i == 1 ? State.B : State.C);

            StateMachineInfo inf = sm.GetInfo();

            Assert.True(inf.StateType == typeof(State));
            Assert.AreEqual(inf.TriggerType, typeof(Trigger));
            Assert.AreEqual(inf.States.Count(), 1);
            var binding = inf.States.Single(s => (State)s.UnderlyingState == State.A);

            Assert.True(binding.UnderlyingState is State);
            Assert.AreEqual(State.A, (State)binding.UnderlyingState);
            //
            Assert.AreEqual(0, binding.Substates.Count());
            Assert.AreEqual(null, binding.Superstate);
            Assert.AreEqual(0, binding.EntryActions.Count());
            Assert.AreEqual(0, binding.ExitActions.Count());
            //
            Assert.AreEqual(0, binding.FixedTransitions.Count()); // Binding transition count mismatch"
            Assert.AreEqual(0, binding.IgnoredTriggers.Count());
            Assert.AreEqual(1, binding.DynamicTransitions.Count()); // Dynamic transition count mismatch
            foreach (DynamicTransitionInfo trans in binding.DynamicTransitions)
            {
                Assert.True(trans.Trigger.UnderlyingTrigger is Trigger);
                Assert.AreEqual(Trigger.X, (Trigger)trans.Trigger.UnderlyingTrigger);
                Assert.AreEqual(0, trans.GuardConditionsMethodDescriptions.Count());
            }
        }

        [Test]
        public void OnEntryWithAnonymousActionAndDescription_Binding()
        {
            var sm = new StateMachine<State, Trigger>(State.A);

            sm.Configure(State.A)
                .OnEntry(() => { }, "enteredA");

            StateMachineInfo inf = sm.GetInfo();

            Assert.True(inf.StateType == typeof(State));
            Assert.AreEqual(inf.States.Count(), 1);
            var binding = inf.States.Single(s => (State)s.UnderlyingState == State.A);

            Assert.True(binding.UnderlyingState is State);
            Assert.AreEqual(State.A, (State)binding.UnderlyingState);
            //
            Assert.AreEqual(0, binding.Substates.Count());
            Assert.AreEqual(null, binding.Superstate);
            Assert.AreEqual(1, binding.EntryActions.Count());
            foreach (ActionInfo entryAction in binding.EntryActions)
            {
                Assert.AreEqual("enteredA", entryAction.Method.Description);
            }
            Assert.AreEqual(0, binding.ExitActions.Count());
            //
            Assert.AreEqual(0, binding.FixedTransitions.Count()); // Binding count mismatch
            Assert.AreEqual(0, binding.IgnoredTriggers.Count());
            Assert.AreEqual(0, binding.DynamicTransitions.Count()); // Dynamic transition count mismatch
        }

        [Test]
        public void OnEntryWithNamedDelegateActionAndDescription_Binding()
        {
            var sm = new StateMachine<State, Trigger>(State.A);

            sm.Configure(State.A)
                .OnEntry(OnEntry, "enteredA");

            StateMachineInfo inf = sm.GetInfo();

            Assert.True(inf.StateType == typeof(State));
            Assert.AreEqual(inf.States.Count(), 1);
            var binding = inf.States.Single(s => (State)s.UnderlyingState == State.A);

            Assert.True(binding.UnderlyingState is State);
            Assert.AreEqual(State.A, (State)binding.UnderlyingState);
            //
            Assert.AreEqual(0, binding.Substates.Count());
            Assert.AreEqual(null, binding.Superstate);
            //
            Assert.AreEqual(1, binding.EntryActions.Count());
            foreach (ActionInfo entryAction in binding.EntryActions)
                Assert.AreEqual("enteredA", entryAction.Method.Description);
            Assert.AreEqual(0, binding.ExitActions.Count());
            //
            Assert.AreEqual(0, binding.FixedTransitions.Count()); // Binding count mismatch
            Assert.AreEqual(0, binding.IgnoredTriggers.Count());
            Assert.AreEqual(0, binding.DynamicTransitions.Count()); // Dynamic transition count mismatch
        }

        [Test]
        public void OnExitWithAnonymousActionAndDescription_Binding()
        {
            var sm = new StateMachine<State, Trigger>(State.A);

            sm.Configure(State.A)
                .OnExit(() => { }, "exitA");

            StateMachineInfo inf = sm.GetInfo();

            Assert.True(inf.StateType == typeof(State));
            Assert.AreEqual(inf.States.Count(), 1);
            var binding = inf.States.Single(s => (State)s.UnderlyingState == State.A);

            Assert.True(binding.UnderlyingState is State);
            Assert.AreEqual(State.A, (State)binding.UnderlyingState);
            //
            Assert.AreEqual(0, binding.Substates.Count());
            Assert.AreEqual(null, binding.Superstate);
            //
            Assert.AreEqual(0, binding.EntryActions.Count());
            Assert.AreEqual(1, binding.ExitActions.Count());
            foreach (InvocationInfo exitAction in binding.ExitActions)
                Assert.AreEqual("exitA", exitAction.Description);
            //
            Assert.AreEqual(0, binding.FixedTransitions.Count()); // Binding count mismatch
            Assert.AreEqual(0, binding.IgnoredTriggers.Count());
            Assert.AreEqual(0, binding.DynamicTransitions.Count()); // Dynamic transition count mismatch
        }

        [Test]
        public void OnExitWithNamedDelegateActionAndDescription_Binding()
        {
            var sm = new StateMachine<State, Trigger>(State.A);

            sm.Configure(State.A)
                .OnExit(OnExit, "exitA");

            StateMachineInfo inf = sm.GetInfo();

            Assert.True(inf.StateType == typeof(State));
            Assert.AreEqual(inf.States.Count(), 1);
            var binding = inf.States.Single(s => (State)s.UnderlyingState == State.A);

            Assert.True(binding.UnderlyingState is State);
            Assert.AreEqual(State.A, (State)binding.UnderlyingState);
            //
            Assert.AreEqual(0, binding.Substates.Count());
            Assert.AreEqual(null, binding.Superstate);
            //
            Assert.AreEqual(0, binding.EntryActions.Count());
            Assert.AreEqual(1, binding.ExitActions.Count());
            foreach (InvocationInfo entryAction in binding.ExitActions)
                Assert.AreEqual("exitA", entryAction.Description);
            //
            Assert.AreEqual(0, binding.FixedTransitions.Count()); // Binding count mismatch
            Assert.AreEqual(0, binding.IgnoredTriggers.Count());
            Assert.AreEqual(0, binding.DynamicTransitions.Count()); // Dynamic transition count mismatch
        }

        [Test]
        public void TransitionWithIgnore_Binding()
        {
            var sm = new StateMachine<State, Trigger>(State.A);

            sm.Configure(State.A)
                .Ignore(Trigger.Y)
                .Permit(Trigger.X, State.B);

            StateMachineInfo inf = sm.GetInfo();

            Assert.True(inf.StateType == typeof(State));
            Assert.AreEqual(inf.TriggerType, typeof(Trigger));
            Assert.AreEqual(inf.States.Count(), 2);
            var binding = inf.States.Single(s => (State)s.UnderlyingState == State.A);

            Assert.True(binding.UnderlyingState is State);
            Assert.AreEqual(State.A, (State)binding.UnderlyingState);
            //
            Assert.AreEqual(1, binding.FixedTransitions.Count()); // Transition count mismatch"
            foreach (FixedTransitionInfo trans in binding.FixedTransitions)
            {
                Assert.True(trans.Trigger.UnderlyingTrigger is Trigger);
                Assert.AreEqual(Trigger.X, (Trigger)trans.Trigger.UnderlyingTrigger);
                //
                Assert.True(trans.DestinationState.UnderlyingState is State);
                Assert.AreEqual(State.B, (State)trans.DestinationState.UnderlyingState);
                Assert.AreEqual(0, trans.GuardConditionsMethodDescriptions.Count());
            }
            //
            Assert.AreEqual(1, binding.IgnoredTriggers.Count()); //  Ignored triggers count mismatch
            foreach (IgnoredTransitionInfo ignore in binding.IgnoredTriggers)
            {
                Assert.True(ignore.Trigger.UnderlyingTrigger is Trigger);
                Assert.AreEqual(Trigger.Y, (Trigger)ignore.Trigger.UnderlyingTrigger); // Ignored trigger value mismatch
            }
            //
            Assert.AreEqual(0, binding.Substates.Count());
            Assert.AreEqual(null, binding.Superstate);
            Assert.AreEqual(0, binding.EntryActions.Count());
            Assert.AreEqual(0, binding.ExitActions.Count());
            Assert.AreEqual(0, binding.DynamicTransitions.Count()); // Dynamic transition count mismatch
        }

        void VerifyMethodNames(IEnumerable<InvocationInfo> methods, string prefix, string body, State state, InvocationInfo.Timing timing)
        {
            Assert.AreEqual(1, methods.Count());
            InvocationInfo method = methods.First();

            if (state == State.A)
                Assert.AreEqual(prefix + body + ((timing == InvocationInfo.Timing.Asynchronous) ? "Async" : ""), method.Description);
            else if (state == State.B)
                Assert.AreEqual(UserDescription + "B-" + body, method.Description);
            else if (state == State.C)
                Assert.AreEqual(InvocationInfo.DefaultFunctionDescription, method.Description);
            else if (state == State.D)
                Assert.AreEqual(UserDescription + "D-" + body, method.Description);

            Assert.AreEqual(timing == InvocationInfo.Timing.Asynchronous, method.IsAsync);
        }

        void VerifyMethodNameses(IEnumerable<InvocationInfo> methods, string prefix, string body, State state,
            InvocationInfo.Timing timing, HashSet<string> suffixes)
        {
            Assert.AreEqual(suffixes.Count, methods.Count());

            foreach (InvocationInfo method in methods)
            {
                Debug.WriteLine("Method description is \"" + method.Description + "\"");
                //
                bool matches = false;
                foreach (string suffix in suffixes)
                {
                    if (state == State.A)
                    {
                        matches = (method.Description == (prefix + body
                            + ((timing == InvocationInfo.Timing.Asynchronous) ? "Async" : "" + suffix)));
                    }
                    else if (state == State.B)
                        matches = (UserDescription + "B-" + body + suffix == method.Description);
                    else if (state == State.C)
                        matches = (InvocationInfo.DefaultFunctionDescription == method.Description);
                    else if (state == State.D)
                        matches = (UserDescription + "D-" + body + suffix == method.Description);
                    //
                    if (matches)
                    {
                        suffixes.Remove(suffix);
                        break;
                    }
                }
                if (!matches)
                    Debug.WriteLine("No match for \"" + method.Description + "\"");
                Assert.True(matches);
                //
                Assert.AreEqual(timing == InvocationInfo.Timing.Asynchronous, method.IsAsync);
            }
        }

        [Test]
        public void ReflectionMethodNames()
        {
            var sm = new StateMachine<State, Trigger>(State.A);

            sm.Configure(State.A)
                .OnActivate(OnActivate)
                .OnEntry(OnEntry)
                .OnExit(OnExit)
                .OnDeactivate(OnDeactivate);
            sm.Configure(State.B)
                .OnActivate(OnActivate, UserDescription + "B-Activate")
                .OnEntry(OnEntry, UserDescription + "B-Entry")
                .OnExit(OnExit, UserDescription + "B-Exit")
                .OnDeactivate(OnDeactivate, UserDescription + "B-Deactivate");
            sm.Configure(State.C)
                .OnActivate(() => OnActivate())
                .OnEntry(() => OnEntry())
                .OnExit(() => OnExit())
                .OnDeactivate(() => OnDeactivate());
            sm.Configure(State.D)
                .OnActivate(() => OnActivate(), UserDescription + "D-Activate")
                .OnEntry(() => OnEntry(), UserDescription + "D-Entry")
                .OnExit(() => OnExit(), UserDescription + "D-Exit")
                .OnDeactivate(() => OnDeactivate(), UserDescription + "D-Deactivate");

            StateMachineInfo inf = sm.GetInfo();

            foreach (StateInfo stateInfo in inf.States)
            {
                VerifyMethodNames(stateInfo.ActivateActions, "On", "Activate", (State)stateInfo.UnderlyingState, InvocationInfo.Timing.Synchronous);
                VerifyMethodNames(stateInfo.EntryActions.Select(x => x.Method), "On", "Entry", (State)stateInfo.UnderlyingState, InvocationInfo.Timing.Synchronous);
                VerifyMethodNames(stateInfo.ExitActions, "On", "Exit", (State)stateInfo.UnderlyingState, InvocationInfo.Timing.Synchronous);
                VerifyMethodNames(stateInfo.DeactivateActions, "On", "Deactivate", (State)stateInfo.UnderlyingState, InvocationInfo.Timing.Synchronous);
            }

            // --------------------------------------------------------

            // New StateMachine, new tests: entry and exit, functions that take the transition as an argument
            sm = new StateMachine<State, Trigger>(State.A);

            sm.Configure(State.A)
                .OnEntry(OnEntryTrans)
                .OnExit(OnExitTrans);
            sm.Configure(State.B)
                .OnEntry(OnEntryTrans, UserDescription + "B-EntryTrans")
                .OnExit(OnExitTrans, UserDescription + "B-ExitTrans");
            sm.Configure(State.C)
                .OnEntry(t => OnEntryTrans(t))
                .OnExit(t => OnExitTrans(t));
            sm.Configure(State.D)
                .OnEntry(t => OnEntryTrans(t), UserDescription + "D-EntryTrans")
                .OnExit(t => OnExitTrans(t), UserDescription + "D-ExitTrans");

            inf = sm.GetInfo();

            foreach (StateInfo stateInfo in inf.States)
            {
                VerifyMethodNames(stateInfo.EntryActions.Select(x => x.Method), "On", "EntryTrans", (State)stateInfo.UnderlyingState, InvocationInfo.Timing.Synchronous);
                VerifyMethodNames(stateInfo.ExitActions, "On", "ExitTrans", (State)stateInfo.UnderlyingState, InvocationInfo.Timing.Synchronous);
            }

            // --------------------------------------------------------

            sm = new StateMachine<State, Trigger>(State.A);

            var triggerX = sm.SetTriggerParameters<int>(Trigger.X);
            var triggerY = sm.SetTriggerParameters<int, int>(Trigger.Y);
            var triggerZ = sm.SetTriggerParameters<int, int, int>(Trigger.Z);

            sm.Configure(State.A)
                .OnEntryFrom(Trigger.X, OnEntry)
                .OnEntryFrom(Trigger.Y, OnEntryTrans)
                .OnEntryFrom(triggerX, OnEntryInt)
                .OnEntryFrom(triggerX, OnEntryIntTrans)
                .OnEntryFrom(triggerY, OnEntryIntInt)
                .OnEntryFrom(triggerZ, OnEntryIntIntInt);
            sm.Configure(State.B)
                .OnEntryFrom(Trigger.X, OnEntry, UserDescription + "B-Entry")
                .OnEntryFrom(Trigger.Y, OnEntryTrans, UserDescription + "B-EntryTrans")
                .OnEntryFrom(triggerX, OnEntryInt, UserDescription + "B-EntryInt")
                .OnEntryFrom(triggerX, OnEntryIntTrans, UserDescription + "B-EntryIntTrans")
                .OnEntryFrom(triggerY, OnEntryIntInt, UserDescription + "B-EntryIntInt")
                .OnEntryFrom(triggerZ, OnEntryIntIntInt, UserDescription + "B-EntryIntIntInt");
            sm.Configure(State.C)
                .OnEntryFrom(Trigger.X, () => OnEntry())
                .OnEntryFrom(Trigger.Y, trans => OnEntryTrans(trans))
                .OnEntryFrom(triggerX, i => OnEntryInt(i))
                .OnEntryFrom(triggerX, (i, trans) => OnEntryIntTrans(i, trans))
                .OnEntryFrom(triggerY, (i, j) => OnEntryIntInt(i, j))
                .OnEntryFrom(triggerZ, (i, j, k) => OnEntryIntIntInt(i, j, k));
            sm.Configure(State.D)
                .OnEntryFrom(Trigger.X, () => OnEntry(), UserDescription + "D-Entry")
                .OnEntryFrom(Trigger.Y, trans => OnEntryTrans(trans), UserDescription + "D-EntryTrans")
                .OnEntryFrom(triggerX, i => OnEntryInt(i), UserDescription + "D-EntryInt")
                .OnEntryFrom(triggerX, (i, trans) => OnEntryIntTrans(i, trans), UserDescription + "D-EntryIntTrans")
                .OnEntryFrom(triggerY, (i, j) => OnEntryIntInt(i, j), UserDescription + "D-EntryIntInt")
                .OnEntryFrom(triggerZ, (i, j, k) => OnEntryIntIntInt(i, j, k), UserDescription + "D-EntryIntIntInt");

            inf = sm.GetInfo();

            foreach (StateInfo stateInfo in inf.States)
            {
                VerifyMethodNameses(stateInfo.EntryActions.Select(x => x.Method), "On", "Entry", (State)stateInfo.UnderlyingState,
                    InvocationInfo.Timing.Synchronous,
                    new HashSet<string> { "", "Trans", "Int", "IntTrans", "IntInt", "IntIntInt" });
            }

            /*
            public StateConfiguration OnEntryFrom<TArg0, TArg1>(TriggerWithParameters<TArg0, TArg1> trigger, Action<TArg0, TArg1, Transition> entryAction, string entryActionDescription = null)
            public StateConfiguration OnEntryFrom<TArg0, TArg1, TArg2>(TriggerWithParameters<TArg0, TArg1, TArg2> trigger, Action<TArg0, TArg1, TArg2, Transition> entryAction, string entryActionDescription = null)
             */
        }

        [Test]
        public void ReflectionMethodNamesAsync()
        {
            var sm = new StateMachine<State, Trigger>(State.A);

            sm.Configure(State.A)
                .OnActivateAsync(OnActivateAsync)
                .OnEntryAsync(OnEntryAsync)
                .OnExitAsync(OnExitAsync)
                .OnDeactivateAsync(OnDeactivateAsync);
            sm.Configure(State.B)
                .OnActivateAsync(OnActivateAsync, UserDescription + "B-Activate")
                .OnEntryAsync(OnEntryAsync, UserDescription + "B-Entry")
                .OnExitAsync(OnExitAsync, UserDescription + "B-Exit")
                .OnDeactivateAsync(OnDeactivateAsync, UserDescription + "B-Deactivate");
            sm.Configure(State.C)
                .OnActivateAsync(() => OnActivateAsync())
                .OnEntryAsync(() => OnEntryAsync())
                .OnExitAsync(() => OnExitAsync())
                .OnDeactivateAsync(() => OnDeactivateAsync());
            sm.Configure(State.D)
                .OnActivateAsync(() => OnActivateAsync(), UserDescription + "D-Activate")
                .OnEntryAsync(() => OnEntryAsync(), UserDescription + "D-Entry")
                .OnExitAsync(() => OnExitAsync(), UserDescription + "D-Exit")
                .OnDeactivateAsync(() => OnDeactivateAsync(), UserDescription + "D-Deactivate");

            StateMachineInfo inf = sm.GetInfo();

            foreach (StateInfo stateInfo in inf.States)
            {
                VerifyMethodNames(stateInfo.ActivateActions, "On", "Activate", (State)stateInfo.UnderlyingState, InvocationInfo.Timing.Asynchronous);
                VerifyMethodNames(stateInfo.EntryActions.Select(x => x.Method), "On", "Entry", (State)stateInfo.UnderlyingState, InvocationInfo.Timing.Asynchronous);
                VerifyMethodNames(stateInfo.ExitActions, "On", "Exit", (State)stateInfo.UnderlyingState, InvocationInfo.Timing.Asynchronous);
                VerifyMethodNames(stateInfo.DeactivateActions, "On", "Deactivate", (State)stateInfo.UnderlyingState, InvocationInfo.Timing.Asynchronous);
            }

            // New StateMachine, new tests: entry and exit, functions that take the transition as an argument
            sm = new StateMachine<State, Trigger>(State.A);

            sm.Configure(State.A)
                .OnEntryAsync(OnEntryTransAsync)
                .OnExitAsync(OnExitTransAsync);
            sm.Configure(State.B)
                .OnEntryAsync(OnEntryTransAsync, UserDescription + "B-EntryTrans")
                .OnExitAsync(OnExitTransAsync, UserDescription + "B-ExitTrans");
            sm.Configure(State.C)
                .OnEntryAsync(t => OnEntryTransAsync(t))
                .OnExitAsync(t => OnExitTransAsync(t));
            sm.Configure(State.D)
                .OnEntryAsync(t => OnEntryTransAsync(t), UserDescription + "D-EntryTrans")
                .OnExitAsync(t => OnExitTransAsync(t), UserDescription + "D-ExitTrans");

            inf = sm.GetInfo();

            foreach (StateInfo stateInfo in inf.States)
            {
                VerifyMethodNames(stateInfo.EntryActions.Select(x => x.Method), "On", "EntryTrans", (State)stateInfo.UnderlyingState, InvocationInfo.Timing.Asynchronous);
                VerifyMethodNames(stateInfo.ExitActions, "On", "ExitTrans", (State)stateInfo.UnderlyingState, InvocationInfo.Timing.Asynchronous);
            }
            /*
            public StateConfiguration OnEntryFromAsync(TTrigger trigger, Func<Task> entryAction, string entryActionDescription = null)
            public StateConfiguration OnEntryFromAsync(TTrigger trigger, Func<Transition, Task> entryAction, string entryActionDescription = null)
            public StateConfiguration OnEntryFromAsync<TArg0>(TriggerWithParameters<TArg0> trigger, Func<TArg0, Task> entryAction, string entryActionDescription = null)
            public StateConfiguration OnEntryFromAsync<TArg0>(TriggerWithParameters<TArg0> trigger, Func<TArg0, Transition, Task> entryAction, string entryActionDescription = null)
            public StateConfiguration OnEntryFromAsync<TArg0, TArg1>(TriggerWithParameters<TArg0, TArg1> trigger, Func<TArg0, TArg1, Task> entryAction, string entryActionDescription = null)
            public StateConfiguration OnEntryFromAsync<TArg0, TArg1>(TriggerWithParameters<TArg0, TArg1> trigger, Func<TArg0, TArg1, Transition, Task> entryAction, string entryActionDescription = null)
            public StateConfiguration OnEntryFromAsync<TArg0, TArg1, TArg2>(TriggerWithParameters<TArg0, TArg1, TArg2> trigger, Func<TArg0, TArg1, TArg2, Task> entryAction, string entryActionDescription = null)
            public StateConfiguration OnEntryFromAsync<TArg0, TArg1, TArg2>(TriggerWithParameters<TArg0, TArg1, TArg2> trigger, Func<TArg0, TArg1, TArg2, Transition, Task> entryAction, string entryActionDescription = null)
            */
        }

        State NextState()
        {
            return State.D;
        }

        [Test]
        public void TransitionGuardNames()
        {
            var sm = new StateMachine<State, Trigger>(State.A);

            sm.Configure(State.A)
                .PermitIf(Trigger.X, State.B, Permit);
            sm.Configure(State.B)
                .PermitIf(Trigger.X, State.C, Permit, UserDescription + "B-Permit");
            sm.Configure(State.C)
                .PermitIf(Trigger.X, State.B, () => Permit());
            sm.Configure(State.D)
                .PermitIf(Trigger.X, State.C, () => Permit(), UserDescription + "D-Permit");

            StateMachineInfo inf = sm.GetInfo();

            foreach (StateInfo stateInfo in inf.States)
            {
                Assert.AreEqual(1, stateInfo.Transitions.Count());
                TransitionInfo transInfo = stateInfo.Transitions.First();
                Assert.AreEqual(1, transInfo.GuardConditionsMethodDescriptions.Count());
                VerifyMethodNames(transInfo.GuardConditionsMethodDescriptions, "", "Permit", (State)stateInfo.UnderlyingState, InvocationInfo.Timing.Synchronous);
            }


            // --------------------------------------------------------

            sm = new StateMachine<State, Trigger>(State.A);

            sm.Configure(State.A)
                .PermitDynamicIf(Trigger.X, NextState, Permit);
            sm.Configure(State.B)
                .PermitDynamicIf(Trigger.X, NextState, Permit, UserDescription + "B-Permit");
            sm.Configure(State.C)
                .PermitDynamicIf(Trigger.X, NextState, () => Permit());
            sm.Configure(State.D)
                .PermitDynamicIf(Trigger.X, NextState, () => Permit(), UserDescription + "D-Permit");

            inf = sm.GetInfo();

            foreach (StateInfo stateInfo in inf.States)
            {
                Assert.AreEqual(1, stateInfo.Transitions.Count());
                TransitionInfo transInfo = stateInfo.Transitions.First();
                Assert.AreEqual(1, transInfo.GuardConditionsMethodDescriptions.Count());
                VerifyMethodNames(transInfo.GuardConditionsMethodDescriptions, "", "Permit", (State)stateInfo.UnderlyingState, InvocationInfo.Timing.Synchronous);
            }

            /*
           public IgnoredTriggerBehaviour(TTrigger trigger, Func<bool> guard, string description = null)
               : base(trigger, new TransitionGuard(guard, description))
            public InternalTriggerBehaviour(TTrigger trigger, Func<bool> guard)
                : base(trigger, new TransitionGuard(guard, "Internal Transition"))
            public TransitioningTriggerBehaviour(TTrigger trigger, TState destination, Func<bool> guard = null, string guardDescription = null)
                : base(trigger, new TransitionGuard(guard, guardDescription))

            public StateConfiguration PermitReentryIf(TTrigger trigger, Func<bool> guard, string guardDescription = null)

            public StateConfiguration PermitDynamicIf<TArg0>(TriggerWithParameters<TArg0> trigger, Func<TArg0, TState> destinationStateSelector, Func<bool> guard, string guardDescription = null)
            public StateConfiguration PermitDynamicIf<TArg0, TArg1>(TriggerWithParameters<TArg0, TArg1> trigger, Func<TArg0, TArg1, TState> destinationStateSelector, Func<bool> guard, string guardDescription = null)
            public StateConfiguration PermitDynamicIf<TArg0, TArg1, TArg2>(TriggerWithParameters<TArg0, TArg1, TArg2> trigger, Func<TArg0, TArg1, TArg2, TState> destinationStateSelector, Func<bool> guard, string guardDescription = null)

            StateConfiguration InternalPermit(TTrigger trigger, TState destinationState, string guardDescription)
            StateConfiguration InternalPermitDynamic(TTrigger trigger, Func<object[], TState> destinationStateSelector, string guardDescription)
             */
        }
    }
}

