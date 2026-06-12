using System;
using ReleaseGuard.Editor.Core.Audit;

namespace ReleaseGuard.Editor.Core.Transforming
{
    /// <summary>
    /// Marks a <see cref="ReleaseTransformer"/> subclass as a test-only fixture that must not
    /// be registered during real transform runs.
    ///
    /// Mirrors <see cref="TestAuditorFixture"/> for the transformer pipeline.
    /// All fixture attributes are checked by registry loading.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class TestTransformerFixture : Attribute
    {
    }
}