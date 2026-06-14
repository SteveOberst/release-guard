using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ReleaseGuard.Editor.Tests
{
    public sealed class StackTraceTypeCheckTests
    {
        [Test]
        public void Reports_WhenAnyChannelUsesFull()
        {
            var originalTypes = new Dictionary<LogType, StackTraceLogType>();
            var settings = ComponentTestHarness.CreateSettings();
            try
            {
                foreach (LogType logType in Enum.GetValues(typeof(LogType)))
                    originalTypes[logType] = PlayerSettings.GetStackTraceLogType(logType);

                PlayerSettings.SetStackTraceLogType(LogType.Log, StackTraceLogType.Full);

                var report = ComponentTestHarness.RunPreBuild(settings);
                var issue = report.Issues.Single(i => i.SuppressId == "stack_trace_type.full");

                StringAssert.Contains("full stack trace collection", issue.Message);
            }
            finally
            {
                foreach (var pair in originalTypes)
                    PlayerSettings.SetStackTraceLogType(pair.Key, pair.Value);
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }
    }
}
