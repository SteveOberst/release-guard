using ReleaseGuard.Editor.Core.Components;

namespace ReleaseGuard.Editor.Core.Registries
{
    /// <summary>Runtime registries for Release Guard components.</summary>
    public sealed class ReleaseGuardRegistries
    {
        internal ReleaseGuardRegistries()
        {
            Components = new WeightedRegistry<ReleaseGuardComponent>();
        }

        public WeightedRegistry<ReleaseGuardComponent> Components { get; }
    }
}