using ReleaseGuard.Editor.Core.Components;
using ReleaseGuard.Editor.Core.PreBuild;

namespace ExamplePlugin
{
    public sealed class ExampleAuditor : ReleaseGuardComponent
    {
        private readonly ExamplePluginSettings _settings;

        public ExampleAuditor(ExamplePluginSettings settings) => _settings = settings;

        public override string Id => "com.example.example_auditor";
        public override string DisplayName => "Example Auditor";

        public override void Register(ReleaseGuardComponentBinder binder) =>
            binder.OnPreBuild(releaseEvent => Evaluate(releaseEvent.Context));

        private void Evaluate(ReleaseGuardPreBuildContext context)
        {
            if (_settings is null || !_settings.strictMode)
                return;

            context.Report(
                _settings.findingSeverity,
                "Example auditor fired. Disable Strict Mode in Project Settings > Release Guard > Plugins > Example Plugin to silence this.");
        }
    }
}
