using ReleaseGuard.Editor.Core.DI;
using ReleaseGuard.Editor.Core.Plugins;
using ReleaseGuard.Editor.Core.Runtime;
using UnityEditor;

namespace ReleaseGuard.ExamplePlugin
{
    public sealed class ExamplePlugin : ReleaseGuardPlugin
    {
        public override string PluginId => "com.example.example-plugin";
        public override string DisplayName => "Example Plugin";
        public override string Author => "Researchy Development";
        public override System.Type SettingsType => typeof(ExamplePluginSettings);

        public override void Register(PluginRegistrationContext context)
        {
            var settings = GetSettings<ExamplePluginSettings>();
            context.ReleaseGuard.Registries.Auditors.Register(new ExampleAuditor(settings));
        }
    }

    [InitializeOnLoad]
    internal static class ExamplePluginLoader
    {
        static ExamplePluginLoader()
        {
            DI.Resolve<ReleaseGuardEnvironment>().RegisterPlugin(new ExamplePlugin());
        }
    }
}
