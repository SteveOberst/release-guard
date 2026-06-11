using System.Collections.Generic;
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
    public sealed class ReleaseForbiddenAuditorTests
    {
        // The auditor filters to player-shipping assemblies. The test fixtures live in
        // ReleaseGuard.Editor.Tests which is editor-only and would not ship. Inject this
        // assembly name into the override set so the auditor finds the fixtures during tests.
        private static readonly System.Collections.Generic.HashSet<string> TestShippingSet =
            new System.Collections.Generic.HashSet<string>
            {
                typeof(ForbiddenTypeExample).Assembly.GetName().Name
            };

        [SetUp]
        public void SetUp() => ReleaseForbiddenAuditor.ShippingAssemblyNamesOverride = TestShippingSet;

        [TearDown]
        public void TearDown() => ReleaseForbiddenAuditor.ShippingAssemblyNamesOverride = null;

        private static ReleaseGuardSettings Settings() =>
            ScriptableObject.CreateInstance<ReleaseGuardSettings>();

        private static ReleaseAuditContext Context(ReleaseGuardSettings settings, List<ReleaseIssue> issues)
        {
            var configuration = ReleaseGuardConfiguration.Resolve(settings, report: null);
            return new ReleaseAuditContext(
                settings,
                configuration,
                new ReleaseGuardLogger(false),
                buildReport: null,
                BuildTarget.StandaloneWindows64,
                issues);
        }

        [Test]
        public void Reports_Properties_Marked_ReleaseForbidden()
        {
            var settings = Settings();
            try
            {
                var issues = new List<ReleaseIssue>();

                new ReleaseForbiddenAuditor().Evaluate(Context(settings, issues));

                Assert.IsTrue(
                    issues.Any(i => i.Message.Contains("ForbiddenPropertyExample.ForbiddenFlag")),
                    "Expected the property annotated with [ReleaseForbidden] to be reported.");
            }
            finally { Object.DestroyImmediate(settings); }
        }

        [Test]
        public void Reports_Full_Type_Name_For_Type_Level_Attributes()
        {
            var settings = Settings();
            try
            {
                var issues = new List<ReleaseIssue>();

                new ReleaseForbiddenAuditor().Evaluate(Context(settings, issues));

                var issue = issues.FirstOrDefault(i => i.Message.Contains(nameof(ForbiddenTypeExample)));
                Assert.IsNotNull(issue, "Expected the type annotated with [ReleaseForbidden] to be reported.");
                StringAssert.Contains(typeof(ForbiddenTypeExample).FullName, issue.Message);
            }
            finally { Object.DestroyImmediate(settings); }
        }

        [Test]
        public void DoesNot_Report_Members_Outside_ShippingAssemblies()
        {
            // Verify the filter works: with an empty override set, nothing outside it is reported.
            ReleaseForbiddenAuditor.ShippingAssemblyNamesOverride =
                new System.Collections.Generic.HashSet<string>();

            var settings = Settings();
            try
            {
                var issues = new List<ReleaseIssue>();
                new ReleaseForbiddenAuditor().Evaluate(Context(settings, issues));

                Assert.IsEmpty(issues,
                    "Expected no findings when no assemblies are in the shipping set.");
            }
            finally
            {
                Object.DestroyImmediate(settings);
                // TearDown will reset the override
            }
        }
    }

    [ReleaseForbidden(reason: "Property should be blocked")]
    internal sealed class ForbiddenTypeExample
    {
    }

    internal sealed class ForbiddenPropertyExample
    {
        [ReleaseForbidden(reason: "Property should be blocked")]
        public bool ForbiddenFlag => true;
    }
}
