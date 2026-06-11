using ReleaseGuard.Editor.Core.Audit;

namespace ExamplePlugin
{
    public sealed class ExampleAuditor : ReleaseAuditor
    {
        private readonly ExamplePluginSettings _settings;

        public ExampleAuditor(ExamplePluginSettings settings) => _settings = settings;

        public override string Id          => "com.example.example_auditor";
        public override string DisplayName => "Example Auditor";

        public override bool ShouldRun(ReleaseAuditContext context) =>
            _settings != null && _settings.strictMode;

        public override void Evaluate(ReleaseAuditContext context)
        {
            context.Report(
                _settings?.findingSeverity ?? ReleaseGuard.ReleaseIssueSeverity.Warning,
                "Example auditor fired — disable Strict Mode in Project Settings → Release Guard → Plugins → Example Plugin to silence this.");
        }
    }
}
