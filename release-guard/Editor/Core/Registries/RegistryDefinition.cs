using System;
using System.Collections.Generic;
using ReleaseGuard.Editor.Core.Runtime;

namespace ReleaseGuard.Editor.Core.Registries
{
    internal interface IRegistryDefinition
    {
        bool AutoDiscover { get; }
        void RegisterBuiltIns();
        void RegisterDiscovered(RegistryLoader loader);
    }

    internal sealed class RegistryDefinition<T> : IRegistryDefinition
        where T : class, IReleaseGuardRegistryItem
    {
        private readonly WeightedRegistry<T> _registry;
        private readonly IReadOnlyList<T> _builtIns;
        private readonly Func<string, bool> _isDisabled;
        private readonly ReleaseGuardLogger _logger;

        public RegistryDefinition(
            WeightedRegistry<T> registry,
            IReadOnlyList<T> builtIns,
            bool autoDiscover,
            Func<string, bool> isDisabled,
            ReleaseGuardLogger logger)
        {
            _registry = registry;
            _builtIns = builtIns;
            AutoDiscover = autoDiscover;
            _isDisabled = isDisabled ?? (_ => false);
            _logger = logger;
        }

        public bool AutoDiscover { get; }

        public void RegisterBuiltIns()
        {
            foreach (var b in _builtIns)
            {
                if (_isDisabled(b.Id))
                {
                    _logger.LogVerbose($"Built-in '{b.Id}' is disabled in settings; skipping.");
                    continue;
                }

                _registry.Register(b.Id, b);
            }
        }

        public void RegisterDiscovered(RegistryLoader loader) =>
            loader.RegisterDiscovered(_registry, _isDisabled);
    }
}