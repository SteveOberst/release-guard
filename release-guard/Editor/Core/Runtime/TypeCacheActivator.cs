using System;
using System.Collections.Generic;

namespace ReleaseGuard.Editor.Core.Runtime
{
    internal sealed class TypeCacheActivator
    {
        private readonly string _packageAssemblyName;
        private readonly ReleaseGuardLogger _logger;

        public TypeCacheActivator(string packageAssemblyName, ReleaseGuardLogger logger)
        {
            _packageAssemblyName = packageAssemblyName;
            _logger = logger;
        }

        public IEnumerable<(T Instance, string TypeName)> CreateDerived<T>(string label)
            where T : class
        {
            foreach (var type in TypeCacheScanner.ScanDerivedTypes<T>(_packageAssemblyName))
            {
                T instance;
                try
                {
                    instance = (T)Activator.CreateInstance(type);
                }
                catch (Exception e)
                {
                    _logger.LogException($"Failed to instantiate {label} '{type.FullName}'.", e);
                    continue;
                }

                yield return (instance, type.FullName);
            }
        }
    }
}