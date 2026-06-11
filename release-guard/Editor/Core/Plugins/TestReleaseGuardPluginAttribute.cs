using System;
using ReleaseGuard.Editor.Core.Audit;

namespace ReleaseGuard.Editor.Core.Plugins
{
    /// <summary>
    /// Marks a <see cref="ReleaseGuardPlugin"/> subclass as a test-only fixture that must not
    /// be picked up by plugin auto-discovery during real audit or transform runs.
    ///
    /// Apply this to plugin classes defined in test assemblies when you want the whole plugin
    /// to be invisible to plugin discovery at all times. For the common case where a plugin
    /// should be discovered but its <em>contributions</em> should be hidden, mark those
    /// individual <see cref="ReleaseAuditor"/> subclasses with
    /// <see cref="TestAuditorFixtureAttribute"/> or <see cref="ReleaseTransformer"/> subclasses
    /// with <see cref="TestTransformerFixtureAttribute"/> instead.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class TestReleaseGuardPluginAttribute : Attribute
    {
    }
}