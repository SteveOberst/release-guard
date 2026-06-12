using System;
using ReleaseGuard.Editor.Core.Audit;
using ReleaseGuard.Editor.Core.PostProcessing;
using ReleaseGuard.Editor.Core.Transforming;

namespace ReleaseGuard.Editor.Core.Plugins
{
    /// <summary>
    /// Marks a <see cref="ReleaseGuardPlugin"/> subclass as a test-only fixture that must not
    /// be picked up by plugin auto-discovery during real pipeline runs.
    ///
    /// Apply this to plugin classes defined in test assemblies when you want the whole plugin
    /// to be invisible to plugin discovery at all times. For the common case where a plugin
    /// should be discovered but its <em>contributions</em> should be hidden, mark those
    /// individual <see cref="ReleaseAuditor"/> subclasses with
    /// <see cref="TestAuditorFixture"/>, <see cref="ReleasePostProcessor"/>
    /// subclasses with <see cref="TestPostProcessorFixture"/>, or
    /// <see cref="ReleaseTransformer"/> subclasses with <see cref="TestTransformerFixture"/>
    /// instead.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class TestReleaseGuardPlugin : Attribute
    {
    }
}