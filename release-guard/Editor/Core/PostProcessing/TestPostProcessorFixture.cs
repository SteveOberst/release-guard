using System;

namespace ReleaseGuard.Editor.Core.PostProcessing
{
    /// <summary>
    /// Marks a <see cref="ReleasePostProcessor"/> subclass as a test fixture that must never
    /// appear in a real post-processor run. Registry loading excludes types with this
    /// attribute from TypeCache scans and plugin contributions.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class TestPostProcessorFixture : Attribute
    {
    }
}