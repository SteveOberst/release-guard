using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace ReleaseGuard.Editor.Tests
{
    public sealed class ScriptingBackendCheckTests
    {
        [Test]
        public void Reports_WhenBackendIsNotIl2Cpp()
        {
            var settings = ComponentTestHarness.CreateSettings();
            var namedTarget = NamedBuildTarget.FromBuildTargetGroup(BuildTargetGroup.Standalone);
            var originalBackend = PlayerSettings.GetScriptingBackend(namedTarget);
            var nonIl2CppBackend = Enum
                .GetValues(typeof(ScriptingImplementation))
                .Cast<ScriptingImplementation>()
                .FirstOrDefault(backend => backend != ScriptingImplementation.IL2CPP);
            try
            {
                if (nonIl2CppBackend == ScriptingImplementation.IL2CPP)
                    Assert.Inconclusive("No non-IL2CPP scripting backend is available in this Unity version.");

                PlayerSettings.SetScriptingBackend(namedTarget, nonIl2CppBackend);

                var report = ComponentTestHarness.RunPreBuild(settings, BuildTarget.StandaloneWindows64);
                var issue = report.Issues.Single(i => i.ComponentId == "scripting_backend");

                StringAssert.Contains("not IL2CPP", issue.Message);
            }
            finally
            {
                PlayerSettings.SetScriptingBackend(namedTarget, originalBackend);
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }
    }
}
