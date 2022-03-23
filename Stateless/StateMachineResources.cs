namespace Stateless {
    public static class StateMachineResources {

        /// <summary>
        /// Parameters for the trigger &apos;{0}&apos; have already been configured.
        /// </summary>
        public const string CannotReconfigureParameters = "Parameters for the trigger '{0}' have already been configured.";

        /// <summary>
        /// No valid leaving transitions are permitted from state &apos;{1}&apos; for trigger &apos;{0}&apos;. Consider ignoring the trigger.
        /// </summary>
        public const string NoTransitionsPermitted = "No valid leaving transitions are permitted from state '{1}' for trigger '{0}'. Consider ignoring the trigger.";

        /// <summary>
        /// Trigger &apos;{0}&apos; is valid for transition from state &apos;{1}&apos; but a guard conditions are not met. Guard descriptions: &apos;{2}&apos;.
        /// </summary>
        public const string NoTransitionsUnmetGuardConditions = "Trigger '{0}' is valid for transition from state '{1}' but a guard conditions are not met. Guard descriptions: '{2}'.";
    }
}
