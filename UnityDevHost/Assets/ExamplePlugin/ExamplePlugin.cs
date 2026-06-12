using ReleaseGuard.Editor.Core.DI;
using ReleaseGuard.Editor.Core.Plugins;
using ReleaseGuard.Editor.Core.Runtime;
using UnityEditor;

namespace ExamplePlugin
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

    /// <summary>
    /// Registers ExamplePlugin with Release Guard during Editor domain load.
    /// Assembly dependency order guarantees this fires after Release Guard's [InitializeOnLoad],
    /// so the environment is already fully initialized when this runs.
    /// </summary>
    [InitializeOnLoad]
    internal static class ExamplePluginLoader
    {
        static ExamplePluginLoader()
        {
            DI.Resolve<ReleaseGuardEnvironment>().RegisterPlugin(new ExamplePlugin());
        }
    }
}