using System;
using ReleaseGuard.Editor.Core.Audit;
using ReleaseGuard.Editor.Core.Plugins;

namespace ReleaseGuard.Editor.Core.Audit
{
    /// <summary>
    /// Marks a <see cref="ReleaseAuditor"/> subclass as a test-only fixture that must not be
    /// registered during real audit runs.
    ///
    /// Apply this to every fixture auditor defined in test assemblies. The attribute keeps
    /// the mechanism explicit and portable regardless of how the test assembly is named.
    ///
    /// Note: plugin types can also carry <see cref="TestReleaseGuardPlugin"/> to
    /// exclude the entire plugin from auto-discovery. Auditors contributed by non-fixture
    /// plugins are individually checked for this attribute so fixture auditors cannot slip
    /// through via an untagged plugin.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class TestAuditorFixture : Attribute
    {
    }
}