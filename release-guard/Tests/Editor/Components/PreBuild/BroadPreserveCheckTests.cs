using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ReleaseGuard.Editor.Tests
{
    public sealed class BroadPreserveCheckTests
    {
        private const string TempFolder = "Assets/ReleaseGuardBroadPreserveTests";

        private static string ProjectRoot =>
            Directory.GetParent(Application.dataPath)!.FullName;

        [Test]
        public void Reports_BroadLinkXmlRules()
        {
            var settings = ComponentTestHarness.CreateSettings();
            var folderAbsolutePath = Path.Combine(ProjectRoot, TempFolder);
            var linkXmlAbsolutePath = Path.Combine(folderAbsolutePath, "link.xml");
            try
            {
                Directory.CreateDirectory(folderAbsolutePath);
                File.WriteAllText(
                    linkXmlAbsolutePath,
                    "<linker><assembly fullname=\"Game.Runtime\" preserve=\"all\" /></linker>");
                AssetDatabase.Refresh();

                var report = ComponentTestHarness.RunPreBuild(settings);
                var issue = report.Issues.Single(i => i.ComponentId == "broad_preserve");

                StringAssert.Contains("preserves the entire assembly", issue.Message);
                Assert.AreEqual($"{TempFolder}/link.xml", issue.AssetPath);
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
        public void DoesNotReport_TargetedLinkXmlRules()
        {
            var settings = ComponentTestHarness.CreateSettings();
            var folderAbsolutePath = Path.Combine(ProjectRoot, TempFolder);
            var linkXmlAbsolutePath = Path.Combine(folderAbsolutePath, "link.xml");
            try
            {
                Directory.CreateDirectory(folderAbsolutePath);
                File.WriteAllText(
                    linkXmlAbsolutePath,
                    "<linker><assembly fullname=\"Game.Runtime\" preserve=\"nothing\"><type fullname=\"Game.Runtime.Player\" preserve=\"all\" /></assembly></linker>");
                AssetDatabase.Refresh();

                var report = ComponentTestHarness.RunPreBuild(settings);
                Assert.IsFalse(report.Issues.Any(i => i.ComponentId == "broad_preserve"));
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
