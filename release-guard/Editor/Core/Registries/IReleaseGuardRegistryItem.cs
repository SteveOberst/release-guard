namespace ReleaseGuard.Editor.Core.Registries
{
    /// <summary>
    /// Shared identity contract for items stored in Release Guard registries
    /// (auditors, post-processors, and transformers).
    /// </summary>
    // ReSharper disable UnusedMemberInSuper.Global
    public interface IReleaseGuardRegistryItem
    {
        /// <summary>Stable, unique id (snake_case). Used for logging, disabling, and deduplication.</summary>
        string Id { get; }

        /// <summary>Human-friendly name shown in the audit window.</summary>
        string DisplayName { get; }

        /// <summary>Execution order. Lower values run first.</summary>
        int Priority { get; }
    }
    // ReSharper enable UnusedMemberInSuper.Global
}