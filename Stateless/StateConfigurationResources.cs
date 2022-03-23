namespace Stateless
{
    internal static class StateConfigurationResources {
        /// <summary>
        /// Permit() (and PermitIf()) require that the destination state is not equal to the source state. To accept a trigger without changing state, use either Ignore() or PermitReentry().
        /// </summary>
        internal const string SelfTransitionsEitherIgnoredOrReentrant =
            "Permit() (and PermitIf()) require that the destination state is not equal to the source state. To accept a trigger without changing state, use either Ignore() or PermitReentry().";
    }
}
