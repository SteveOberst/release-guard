using System;
using System.Collections.Generic;
using System.Linq;
using ReleaseGuard.Editor.Core.Audit;
using ReleaseGuard.Editor.Core.Plugins;
using ReleaseGuard.Editor.Core.PostProcessing;
using ReleaseGuard.Editor.Core.Transforming;
using UnityEditor;

namespace ReleaseGuard.Editor.Core.Runtime
{
    /// <summary>
    /// Shared TypeCache scanning helpers used by plugin and registry loading.
    ///
    /// Type scanning skips package-internal types and test fixtures; registry-level filtering
    /// handles disabled ids, duplicates, and malformed instances.
    /// </summary>
    internal static class TypeCacheScanner
    {
        // -----------------------------------------------------------------
        // TypeCache scanning
        // -----------------------------------------------------------------

        /// <summary>
        /// Yields all concrete, public, parameterless types derived from <typeparamref name="TBase"/>
        /// that are NOT in the package assembly and NOT marked with a test-fixture attribute.
        /// </summary>
        internal static IEnumerable<Type> ScanDerivedTypes<TBase>(string packageAssemblyName)
            where TBase : class
        {
            return from type in TypeCache.GetTypesDerivedFrom<TBase>()
                where !type.IsAbstract && type.GetConstructor(Type.EmptyTypes) != null
                where type.Assembly.GetName().Name != packageAssemblyName
                where !IsTestFixture(type)
                select type;
        }

        /// <summary>
        /// Returns true for types decorated with <see cref="TestAuditorFixtureAttribute"/>,
        /// <see cref="TestPostProcessorFixtureAttribute"/>, <see cref="TestTransformerFixtureAttribute"/>,
        /// or <see cref="TestReleaseGuardPluginAttribute"/>.
        /// </summary>
        internal static bool IsTestFixture(Type type) =>
            type != null &&
            (type.IsDefined(typeof(TestAuditorFixtureAttribute), inherit: false) ||
             type.IsDefined(typeof(TestPostProcessorFixtureAttribute), inherit: false) ||
             type.IsDefined(typeof(TestTransformerFixtureAttribute), inherit: false) ||
             type.IsDefined(typeof(TestReleaseGuardPluginAttribute), inherit: false));
    }
}