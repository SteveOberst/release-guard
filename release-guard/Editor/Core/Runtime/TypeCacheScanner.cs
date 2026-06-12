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
    /// Type scanning skips package-internal types (including sub-assemblies such as test
    /// assemblies) and test fixtures; registry-level filtering handles disabled ids, duplicates,
    /// and malformed instances.
    /// </summary>
    internal static class TypeCacheScanner
    {
        // -----------------------------------------------------------------
        // TypeCache scanning
        // -----------------------------------------------------------------

        /// <summary>
        /// Yields all concrete, parameterless types derived from <typeparamref name="TBase"/>
        /// that are NOT in the package assembly or any of its sub-assemblies (e.g. test
        /// assemblies whose names share the package assembly name as a prefix) and NOT marked
        /// with a test-fixture attribute.
        /// </summary>
        internal static IEnumerable<Type> ScanDerivedTypes<TBase>(string packageAssemblyName)
            where TBase : class
        {
            var packagePrefix = packageAssemblyName + ".";
            return from type in TypeCache.GetTypesDerivedFrom<TBase>()
                let asmName = type.Assembly.GetName().Name
                where !type.IsAbstract && type.GetConstructor(Type.EmptyTypes) != null
                where asmName != packageAssemblyName && !asmName.StartsWith(packagePrefix, StringComparison.Ordinal)
                where !IsTestFixture(type)
                select type;
        }

        /// <summary>
        /// Returns true for types decorated with <see cref="TestAuditorFixture"/>,
        /// <see cref="TestPostProcessorFixture"/>, <see cref="TestTransformerFixture"/>,
        /// or <see cref="TestReleaseGuardPlugin"/>.
        /// </summary>
        internal static bool IsTestFixture(Type type) =>
            type != null &&
            (type.IsDefined(typeof(TestAuditorFixture), inherit: false) ||
             type.IsDefined(typeof(TestPostProcessorFixture), inherit: false) ||
             type.IsDefined(typeof(TestTransformerFixture), inherit: false) ||
             type.IsDefined(typeof(TestReleaseGuardPlugin), inherit: false));
    }
}