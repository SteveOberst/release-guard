using System.IO;
using ReleaseGuard.Editor.Core.PreBuild;
using ReleaseGuard.Editor.Core.Components;
using UnityEditor;
using UnityEngine;

namespace ReleaseGuard.Editor.Builtins.PreBuild
{
    /// <summary>
    /// Android only: flags explicit <c>debuggable = true</c> declarations in custom Android
    /// templates under <c>Assets/Plugins/Android/</c> (AndroidManifest.xml and *.gradle files).
    ///
    /// <para>A debuggable release APK/AAB allows anyone to attach a debugger, dump memory, and
    /// bypass most client-side protections. Google Play rejects debuggable release uploads
    /// outright, so this finding would otherwise surface as a store rejection after the build.</para>
    ///
    /// <para><b>False-positive posture:</b> only explicit <c>debuggable=true</c> text in
    /// project-owned template files is flagged; commented-out occurrences are ignored (see
    /// <see cref="AndroidDebuggableAnalyzer"/>). Unity's generated manifests are not scanned --
    /// Unity itself sets debuggable from the Development Build flag, which the
    /// <c>development_build</c> component already covers.</para>
    ///
    /// <para>This is an Error (blocking by default) rather than an advisory: an explicit
    /// debuggable=true in a release template is virtually never intended, and shipping it has
    /// hard consequences. Findings carry the file's asset path, so a legitimately debuggable
    /// template (if one exists) can be excluded via the asset-exclusion list.</para>
    /// </summary>
    public sealed class AndroidDebuggableCheck : ReleaseGuardComponent
    {
        public override string Id => "android_debuggable";
        public override string DisplayName => "Android debuggable templates";

        private const string AndroidPluginsFolder = "Assets/Plugins/Android";

        public override void Register(ReleaseGuardComponentBinder binder) =>
            binder.OnPreBuild(releaseEvent => Evaluate(releaseEvent.Context));

        private static void Evaluate(ReleaseGuardPreBuildContext context)
        {
            if (!context.IsForPlatform(BuildTarget.Android)) return;

            var absoluteFolder = Path.Combine(
                Directory.GetParent(Application.dataPath)!.FullName,
                AndroidPluginsFolder);

            if (!Directory.Exists(absoluteFolder))
                return;

            foreach (var file in Directory.EnumerateFiles(absoluteFolder, "*", SearchOption.AllDirectories))
            {
                var fileName = Path.GetFileName(file);
                var isManifest = string.Equals(fileName, "AndroidManifest.xml",
                    System.StringComparison.OrdinalIgnoreCase);
                var isGradle = fileName.EndsWith(".gradle", System.StringComparison.OrdinalIgnoreCase);

                if (!isManifest && !isGradle)
                    continue;

                string content;
                try
                {
                    content = File.ReadAllText(file);
                }
                catch (IOException e)
                {
                    context.Logger.LogVerbose($"[AndroidDebuggable] Could not read '{file}': {e.Message}");
                    continue;
                }

                var findings = isManifest
                    ? AndroidDebuggableAnalyzer.ScanManifest(content)
                    : AndroidDebuggableAnalyzer.ScanGradle(content);

                if (findings.Count == 0)
                    continue;

                var assetPath = ToAssetPath(file);
                foreach (var finding in findings)
                {
                    context.Error(
                        $"'{assetPath}' line {finding.line} sets debuggable=true: \"{finding.lineContent}\". " +
                        "A debuggable release build allows debugger attachment and memory dumping, " +
                        "and Google Play rejects debuggable release uploads.",
                        assetPath,
                        "Remove the debuggable declaration from the template, or gate it to debug " +
                        "build variants only.");
                }
            }
        }

        private static string ToAssetPath(string absolutePath)
        {
            var projectRoot = Directory.GetParent(Application.dataPath)!.FullName.Replace('\\', '/');
            var normalized = absolutePath.Replace('\\', '/');

            return normalized.StartsWith(projectRoot)
                ? normalized[(projectRoot.Length + 1)..]
                : normalized;
        }
    }
}