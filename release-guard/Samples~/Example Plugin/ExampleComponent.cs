using ReleaseGuard.Editor.Core.Components;
using ReleaseGuard.Editor.Core.PreBuild;

namespace ReleaseGuard.ExamplePlugin
{
    public sealed class ExampleComponent : ReleaseGuardComponent
    {
        private readonly ExamplePluginSettings _settings;

        public ExampleComponent(ExamplePluginSettings settings) => _settings = settings;

        public override string Id => "com.example.example_component";
        public override string DisplayName => "Example Component";

        public override void Register(ReleaseGuardComponentBinder binder) =>
            binder.OnPreBuild(releaseEvent => Evaluate(releaseEvent.Context));

        private void Evaluate(ReleaseGuardPreBuildContext context)
        {
            if (_settings == null || !_settings.strictMode)
                return;

            context.Report(
                _settings.findingSeverity,
                "Example component fired. Disable Strict Mode in Project Settings > Release Guard > Plugins > Example Plugin to silence this.");
        }
    }
}
