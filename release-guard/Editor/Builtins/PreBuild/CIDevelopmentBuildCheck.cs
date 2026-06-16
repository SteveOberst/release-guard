using ReleaseGuard.Editor.Core.PreBuild;
using ReleaseGuard.Editor.Core.Components;
using ReleaseGuard.Editor.Core.Build;

namespace ReleaseGuard.Editor.Builtins.PreBuild
{
    /// <summary>
    /// Blocks a Development Build that is running in a CI environment. CI production jobs should
    /// never ship dev builds; this component ensures a misconfigured CI pipeline is caught early.
    /// </summary>
    public sealed class CIDevelopmentBuildCheck : ReleaseGuardComponent
    {
        public override string Id => "ci_development_build";
        public override string DisplayName => "Development build in CI";

        public override void Register(ReleaseGuardComponentBinder binder) =>
            binder.OnPreBuild(releaseEvent => Evaluate(releaseEvent.Context));

        private static void Evaluate(ReleaseGuardPreBuildContext context)
        {
            if (!context.IsDevelopmentBuild) return;
            var env = BuildEnvironmentDetector.Detect();
            if (!env.IsCI) return;

            context.Error(
                $"Development Build is running in CI ({env.Environment}). " +
                "CI production jobs should build without the Development flag.",
                fixHint: "Remove BuildOptions.Development from your CI build method, " +
                         "or route the build through the Development profile intentionally.");
        }
    }
}