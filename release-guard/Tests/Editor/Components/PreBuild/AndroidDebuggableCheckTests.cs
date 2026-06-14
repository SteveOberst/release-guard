using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ReleaseGuard.Editor.Tests
{
    public sealed class AndroidDebuggableCheckTests
    {
        [Test]
        public void Reports_DebuggableTemplates_FromAndroidPluginsFolder()
        {
            var settings = ComponentTestHarness.CreateSettings();
            var relativeFolder = $"Assets/Plugins/Android/ReleaseGuardTests_{Guid.NewGuid():N}";
            var absoluteFolder = Path.Combine(
                Directory.GetParent(Application.dataPath)!.FullName,
                relativeFolder.Replace('/', Path.DirectorySeparatorChar));
            var manifestPath = Path.Combine(absoluteFolder, "AndroidManifest.xml");

            try
            {
                Directory.CreateDirectory(absoluteFolder);
                File.WriteAllText(
                    manifestPath,
                    "<manifest>\n  <application android:debuggable=\"true\" />\n</manifest>");
                AssetDatabase.Refresh();

                var report = ComponentTestHarness.RunPreBuild(settings, BuildTarget.Android);
                var issue = report.Issues.Single(i => i.ComponentId == "android_debuggable");

                StringAssert.Contains(relativeFolder + "/AndroidManifest.xml", issue.Message);
                Assert.AreEqual(relativeFolder + "/AndroidManifest.xml", issue.AssetPath);
            }
            finally
            {
                FileUtil.DeleteFileOrDirectory(relativeFolder);
                FileUtil.DeleteFileOrDirectory(relativeFolder + ".meta");
                AssetDatabase.Refresh();
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }
    }
}
