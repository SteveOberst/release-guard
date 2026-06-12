using System;
using System.Collections.Generic;
using System.Linq;

namespace ReleaseGuard.Editor.Core.Registries
{
    /// <summary>
    /// Registry that keeps items in priority-then-id order using a natively sorted structure.
    /// Deduplicates by id; the first registration for a given id wins.
    ///
    /// Registration guards (added via <see cref="AddRegistrationGuard"/>) are enforced on every
    /// <see cref="Register"/> call regardless of the caller -- built-in loaders, plugin
    /// contributions, and dynamic registrations all go through the same gate.
    /// </summary>
    public sealed class WeightedRegistry<T> : IRegistry<string, T>
        where T : class, IReleaseGuardRegistryItem
    {
        private readonly SortedList<(int priority, string id), T> _sorted = new();
        private readonly Dictionary<string, T> _byId = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<Func<string, T, bool>> _guards = new();
        private IReadOnlyList<T> _itemsCache;

        public IReadOnlyList<T> Items => _itemsCache ??= new List<T>(_sorted.Values);

        public void AddRegistrationGuard(Func<string, T, bool> canRegister)
        {
            if (canRegister != null)
                _guards.Add(canRegister);
        }

        /// <summary>Convenience overload -- extracts the key from <c>item.Id</c>.</summary>
        // ReSharper disable once UnusedMethodReturnValue.Global
        public bool Register(T item) => Register(item?.Id, item);

        public bool Register(string id, T item)
        {
            if (item == null || string.IsNullOrEmpty(id)) return false;
            var normalizedId = id.ToLowerInvariant();

            foreach (var guard in _guards)
                if (!guard(normalizedId, item))
                    return false;

            if (!_byId.TryAdd(normalizedId, item)) return false;

            _sorted[(item.Priority, normalizedId)] = item;
            _itemsCache = null;
            return true;
        }

        public T Get(string id)
        {
            return string.IsNullOrEmpty(id) ? null : _byId.GetValueOrDefault(id.ToLowerInvariant());
        }
    }
}