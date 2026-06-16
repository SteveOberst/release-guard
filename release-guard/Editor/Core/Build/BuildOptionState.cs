using UnityEditor;

namespace ReleaseGuard.Editor.Core.Build
{
    internal static class BuildOptionState
    {
        public static bool IsDevelopmentBuild(BuildOptions options) =>
            (options & BuildOptions.Development) != 0;

        public static bool IsScriptDebuggingEnabled(BuildOptions options) =>
            (options & BuildOptions.AllowDebugging) != 0;

        public static bool IsScriptDebuggingEnabledInEditor() =>
            EditorUserBuildSettings.allowDebugging;

        public static bool IsProfilerConnectionEnabled(BuildOptions options) =>
            (options & BuildOptions.ConnectWithProfiler) != 0;

        public static bool IsProfilerConnectionEnabledInEditor() =>
            EditorUserBuildSettings.connectProfiler;
    }
}