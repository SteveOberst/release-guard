using System;

namespace ReleaseGuard.Editor.Core.Components
{
    /// <summary>
    /// Marks a <see cref="ReleaseGuardComponent"/> subclass as a test-only fixture that must not
    /// be auto-discovered during real component registration.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class TestReleaseGuardComponentFixture : Attribute
    {
    }
}