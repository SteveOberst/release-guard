using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using ReleaseGuard.Editor.Builtins.Auditor;
using ReleaseGuard.Editor.Config;
using ReleaseGuard.Editor.Core.Audit;
using ReleaseGuard.Editor.Core.Runtime;
using UnityEditor;
using UnityEngine;

namespace ReleaseGuard.Editor.Tests
{
    public sealed class BuiltInReleaseHardeningAuditorTests
    {
        private const string TempFolder = "Assets/ReleaseGuardBroadPreserveTests";

        private static ReleaseGuardSettings Settings() =>
            ScriptableObject.CreateInstance<ReleaseGuardSettings>();

        private static ReleaseAuditContext Context(ReleaseGuardSettings settings, List<ReleaseIssue> issues)
        {
            settings.general.skipOnDevelopmentBuilds = false;
            var configuration = ReleaseGuardConfiguration.Resolve(settings, report: null);
            return new ReleaseAuditContext(
                settings,
                configuration,
                new ReleaseGuardLogger(false),
                buildReport: null,
                BuildTarget.StandaloneWindows64,
                issues);
        }

        private static void Evaluate(
            ReleaseAuditor auditor,
            ReleaseGuardSettings settings,
            List<ReleaseIssue> issues)
        {
            var context = Context(settings, issues);
            context.BeginAuditor(auditor);
            auditor.Evaluate(context);
        }

        private static string ProjectRoot =>
            Directory.GetParent(Application.dataPath)!.FullName;

        [Test]
        public void ScriptDebuggingAuditor_Reports_WhenScriptDebuggingIsEnabled()
        {
            var settings = Settings();
            var issues = new List<ReleaseIssue>();
            var originalDevelopment = EditorUserBuildSettings.development;
            var originalAllowDebugging = EditorUserBuildSettings.allowDebugging;
            try
            {
                EditorUserBuildSettings.development = true;
                EditorUserBuildSettings.allowDebugging = true;

                Evaluate(new ScriptDebuggingAuditor(), settings, issues);

                Assert.AreEqual(1, issues.Count);
                StringAssert.Contains("Script debugging is enabled", issues[0].Message);
            }
            finally
            {
                EditorUserBuildSettings.development = originalDevelopment;
                EditorUserBuildSettings.allowDebugging = originalAllowDebugging;
                Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void ProfilerConnectionAuditor_Reports_WhenProfilerConnectionIsEnabled()
        {
            var settings = Settings();
            var issues = new List<ReleaseIssue>();
            var originalDevelopment = EditorUserBuildSettings.development;
            var originalConnectProfiler = EditorUserBuildSettings.connectProfiler;
            try
            {
                EditorUserBuildSettings.development = true;
                EditorUserBuildSettings.connectProfiler = true;

                Evaluate(new ProfilerConnectionAuditor(), settings, issues);

                Assert.AreEqual(1, issues.Count);
                StringAssert.Contains("Autoconnect Profiler is enabled", issues[0].Message);
            }
            finally
            {
                EditorUserBuildSettings.development = originalDevelopment;
                EditorUserBuildSettings.connectProfiler = originalConnectProfiler;
                Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void BroadPreserveAuditor_Reports_BroadLinkXmlRules()
        {
            var settings = Settings();
            var issues = new List<ReleaseIssue>();
            var folderAbsolutePath = Path.Combine(ProjectRoot, TempFolder);
            var linkXmlAbsolutePath = Path.Combine(folderAbsolutePath, "link.xml");
            try
            {
                Directory.CreateDirectory(folderAbsolutePath);
                File.WriteAllText(
                    linkXmlAbsolutePath,
                    "<linker><assembly fullname=\"Game.Runtime\" preserve=\"all\" /></linker>");
                AssetDatabase.Refresh();

                Evaluate(new BroadPreserveAuditor(), settings, issues);

                Assert.AreEqual(1, issues.Count);
                StringAssert.Contains("preserves the entire assembly", issues[0].Message);
                Assert.AreEqual($"{TempFolder}/link.xml", issues[0].AssetPath);
            }
            finally
            {
                FileUtil.DeleteFileOrDirectory(TempFolder);
                FileUtil.DeleteFileOrDirectory($"{TempFolder}.meta");
                AssetDatabase.Refresh();
                Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void BroadPreserveAuditor_DoesNotReport_TargetedLinkXmlRules()
        {
            var settings = Settings();
            var issues = new List<ReleaseIssue>();
            var folderAbsolutePath = Path.Combine(ProjectRoot, TempFolder);
            var linkXmlAbsolutePath = Path.Combine(folderAbsolutePath, "link.xml");
            try
            {
                Directory.CreateDirectory(folderAbsolutePath);
                File.WriteAllText(
                    linkXmlAbsolutePath,
                    "<linker><assembly fullname=\"Game.Runtime\" preserve=\"nothing\"><type fullname=\"Game.Runtime.Player\" preserve=\"all\" /></assembly></linker>");
                AssetDatabase.Refresh();

                Evaluate(new BroadPreserveAuditor(), settings, issues);

                Assert.IsEmpty(issues);
            }
            finally
            {
                FileUtil.DeleteFileOrDirectory(TempFolder);
                FileUtil.DeleteFileOrDirectory($"{TempFolder}.meta");
                AssetDatabase.Refresh();
                Object.DestroyImmediate(settings);
            }
        }
    }
}
