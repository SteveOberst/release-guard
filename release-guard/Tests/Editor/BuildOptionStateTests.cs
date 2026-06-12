using NUnit.Framework;
using ReleaseGuard.Editor.Util;
using UnityEditor;
using UnityEditor.Build;

namespace ReleaseGuard.Editor.Tests
{
    public sealed class BuildOptionStateTests
    {
        [Test]
        public void Detects_ScriptDebugging_FromBuildOptions()
        {
            Assert.IsTrue(BuildOptionState.IsScriptDebuggingEnabled(BuildOptions.AllowDebugging));
            Assert.IsFalse(BuildOptionState.IsScriptDebuggingEnabled(BuildOptions.None));
        }

        [Test]
        public void Detects_ProfilerConnection_FromBuildOptions()
        {
            Assert.IsTrue(BuildOptionState.IsProfilerConnectionEnabled(BuildOptions.ConnectWithProfiler));
            Assert.IsFalse(BuildOptionState.IsProfilerConnectionEnabled(BuildOptions.None));
        }
    }
}