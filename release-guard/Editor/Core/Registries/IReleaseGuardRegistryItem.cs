namespace ReleaseGuard.Editor.Core.Registries
{
    /// <summary>
    /// Shared identity contract for items stored in Release Guard registries.
    /// </summary>
    // ReSharper disable UnusedMemberInSuper.Global
    public interface IReleaseGuardRegistryItem
    {
        /// <summary>Stable, unique id (snake_case). Used for logging, disabling, and deduplication.</summary>
        string Id { get; }

        /// <summary>Human-friendly name shown in the Release Guard window.</summary>
        string DisplayName { get; }
    }
    // ReSharper enable UnusedMemberInSuper.Global
}