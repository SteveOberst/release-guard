using System.Collections.Generic;
using NUnit.Framework;
using ReleaseGuard;
using ReleaseGuard.Editor.Core.Runtime;

namespace ReleaseGuard.Editor.Tests
{
    public sealed class ReleaseGuardPreBuildReportTests
    {
        private static ReleaseIssue Issue(ReleaseIssueSeverity severity) =>
            new ReleaseIssue("component", severity, "message");

        private static ReleaseGuardPreBuildReport Report(params ReleaseIssueSeverity[] severities)
        {
            var issues = new List<ReleaseIssue>();
            foreach (var s in severities)
                issues.Add(Issue(s));
            return new ReleaseGuardPreBuildReport(issues);
        }

        [Test]
        public void Empty_HasNoIssuesOrErrors()
        {
            var r = Report();
            Assert.IsFalse(r.HasIssues);
            Assert.IsFalse(r.HasErrors);
            Assert.AreEqual(0, r.InfoCount + r.WarningCount + r.ErrorCount);
            Assert.AreEqual(ReleaseIssueSeverity.Info, r.HighestSeverity);
        }

        [Test]
        public void Counts_AreGroupedBySeverity()
        {
            var r = Report(
                ReleaseIssueSeverity.Info,
                ReleaseIssueSeverity.Warning,
                ReleaseIssueSeverity.Warning,
                ReleaseIssueSeverity.Error);

            Assert.AreEqual(1, r.InfoCount);
            Assert.AreEqual(2, r.WarningCount);
            Assert.AreEqual(1, r.ErrorCount);
            Assert.IsTrue(r.HasErrors);
            Assert.AreEqual(ReleaseIssueSeverity.Error, r.HighestSeverity);
        }

        [Test]
        public void HighestSeverity_IsWarning_WhenNoErrors()
        {
            var r = Report(ReleaseIssueSeverity.Info, ReleaseIssueSeverity.Warning);
            Assert.AreEqual(ReleaseIssueSeverity.Warning, r.HighestSeverity);
            Assert.IsFalse(r.HasErrors);
        }

        [Test]
        public void ShouldFailBuild_RespectsThreshold()
        {
            var warnOnly = Report(ReleaseIssueSeverity.Warning);
            Assert.IsFalse(warnOnly.ShouldFailBuild(ReleaseIssueSeverity.Error)); // below threshold
            Assert.IsTrue(warnOnly.ShouldFailBuild(ReleaseIssueSeverity.Warning)); // at threshold
            Assert.IsTrue(warnOnly.ShouldFailBuild(ReleaseIssueSeverity.Info)); // above threshold
        }

        [Test]
        public void CountAtOrAbove_CountsInclusive()
        {
            var r = Report(
                ReleaseIssueSeverity.Info,
                ReleaseIssueSeverity.Warning,
                ReleaseIssueSeverity.Error);

            Assert.AreEqual(3, r.CountAtOrAbove(ReleaseIssueSeverity.Info));
            Assert.AreEqual(2, r.CountAtOrAbove(ReleaseIssueSeverity.Warning));
            Assert.AreEqual(1, r.CountAtOrAbove(ReleaseIssueSeverity.Error));
        }

        [Test]
        public void NullArguments_AreTreatedAsEmpty()
        {
            var r = new ReleaseGuardPreBuildReport(null);
            Assert.IsNotNull(r.Issues);
            Assert.IsNotNull(r.RegisteredComponents);
            Assert.IsFalse(r.HasIssues);
        }
    }
}

