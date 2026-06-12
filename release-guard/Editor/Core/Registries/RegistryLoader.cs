using System;
using System.Collections.Generic;
using ReleaseGuard.Editor.Core.Runtime;

namespace ReleaseGuard.Editor.Core.Registries
{
    internal sealed class RegistryLoader
    {
        private readonly TypeCacheActivator _activator;
        private readonly ReleaseGuardLogger _logger;

        public RegistryLoader(TypeCacheActivator activator, ReleaseGuardLogger logger)
        {
            _activator = activator;
            _logger = logger;
        }

        public void Load(IEnumerable<IRegistryDefinition> definitions)
        {
            foreach (var definition in definitions)
            {
                // Built-ins always register (subject to per-item isDisabled check).
                // AutoDiscover controls only TypeCache discovery of custom implementations.
                definition.RegisterBuiltIns();
                if (definition.AutoDiscover)
                    definition.RegisterDiscovered(this);
            }
        }

        public void RegisterDiscovered<T>(WeightedRegistry<T> registry, Func<string, bool> isDisabled)
            where T : class, IReleaseGuardRegistryItem
        {
            foreach (var (item, typeName) in _activator.CreateDerived<T>("registry item"))
            {
                if (TypeCacheScanner.IsTestFixture(item.GetType()))
                {
                    _logger.LogVerbose($"Registry item '{typeName}' is a test fixture; skipped.");
                    continue;
                }

                var id = item.Id;
                if (string.IsNullOrEmpty(id))
                {
                    _logger.LogWarning($"Registry item '{typeName}' has an empty Id; skipped.");
                    continue;
                }

                if (isDisabled(id))
                {
                    _logger.LogVerbose($"Registry item '{id}' is disabled in settings; skipping.");
                    continue;
                }

                registry.Register(id, item);
            }
        }
    }
}