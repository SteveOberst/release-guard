using System;
using System.Collections.Generic;

namespace ReleaseGuard.Editor.Core.Registries
{
    /// <summary>
    /// Registry that keeps items in priority-then-id order using a natively sorted structure.
    /// Deduplicates by id; the first registration for a given id wins.
    ///
    /// Callers are responsible for filtering disabled and test-fixture items before calling
    /// Register — the registry itself has no knowledge of project settings or test state.
    /// </summary>
    public sealed class WeightedRegistry<T> : IRegistry<string, T>
        where T : class, IReleaseGuardRegistryItem
    {
        private readonly SortedList<(int priority, string id), T> _sorted = new();
        private readonly Dictionary<string, T> _byId = new(StringComparer.OrdinalIgnoreCase);
        private IReadOnlyList<T> _itemsCache;

        public IReadOnlyList<T> Items => _itemsCache ??= new List<T>(_sorted.Values);

        /// <summary>Convenience overload — extracts the key from <c>item.Id</c>.</summary>
        // ReSharper disable once UnusedMethodReturnValue.Global
        public bool Register(T item) => Register(item?.Id, item);

        public bool Register(string id, T item)
        {
            if (item == null || string.IsNullOrEmpty(id)) return false;
            var normalizedId = id.ToLowerInvariant();
            if (!_byId.TryAdd(normalizedId, item)) return false;

            _sorted[(item.Priority, normalizedId)] = item;
            _itemsCache = null;
            return true;
        }

        public T Get(string id)
        {
            return string.IsNullOrEmpty(id) ? null : _byId.GetValueOrDefault(id.ToLowerInvariant());
        }

        internal void Purge(Func<string, bool> shouldRemove)
        {
            var toRemove = new List<string>();
            foreach (var id in _byId.Keys)
                if (shouldRemove(id))
                    toRemove.Add(id);

            foreach (var id in toRemove)
            {
                var item = _byId[id];
                _byId.Remove(id);
                _sorted.Remove((item.Priority, id));
            }

            if (toRemove.Count > 0)
                _itemsCache = null;
        }
    }
}
