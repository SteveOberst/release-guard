using ReleaseGuard.Editor.Core.Audit;
using ReleaseGuard.Editor.Core.PostProcessing;
using ReleaseGuard.Editor.Core.Transforming;

namespace ReleaseGuard.Editor.Core.Registries
{
    /// <summary>Runtime registries for every Release Guard registry item type.</summary>
    public sealed class ReleaseGuardRegistries
    {
        internal ReleaseGuardRegistries()
        {
            Auditors = new WeightedRegistry<ReleaseAuditor>();
            PostProcessors = new WeightedRegistry<ReleasePostProcessor>();
            Transformers = new WeightedRegistry<ReleaseTransformer>();
        }

        public WeightedRegistry<ReleaseAuditor> Auditors { get; }
        public WeightedRegistry<ReleasePostProcessor> PostProcessors { get; }
        public WeightedRegistry<ReleaseTransformer> Transformers { get; }
    }
}