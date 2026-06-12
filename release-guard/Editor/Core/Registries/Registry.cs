using System;
using System.Collections.Generic;
using System.Linq;

namespace ReleaseGuard.Editor.Core.Registries
{
    /// <summary>
    /// General-purpose key-value registry.
    ///
    /// <para>User registrations (via <see cref="Register"/>) take priority over defaults
    /// (via <see cref="RegisterDefault"/>): a user entry for a key overwrites an existing default;
    /// a second user registration for the same key is a silent no-op that returns
    /// <c>false</c>.</para>
    /// </summary>
    public sealed class Registry<TKey, TValue> : IRegistry<TKey, TValue>
    {
        private enum EntryKind
        {
            Default,
            User
        }

        private readonly Dictionary<TKey, (TValue value, EntryKind kind)> _dict;
        private readonly List<Func<TKey, TValue, bool>> _guards = new();

        public Registry(IEqualityComparer<TKey> comparer = null)
        {
            _dict = new Dictionary<TKey, (TValue, EntryKind)>(
                comparer ?? EqualityComparer<TKey>.Default);
        }

        public IReadOnlyList<TValue> Items
        {
            get
            {
                var list = new List<TValue>(_dict.Count);
                list.AddRange(_dict.Values.Select(entry => entry.value));
                return list;
            }
        }

        public void AddRegistrationGuard(Func<TKey, TValue, bool> canRegister)
        {
            if (canRegister != null)
                _guards.Add(canRegister);
        }

        /// <summary>
        /// Register a user entry. Overwrites an existing default for the same key.
        /// Returns <c>false</c> if a user entry for this key already exists (no overwrite)
        /// or if any registration guard rejects it.
        /// </summary>
        public bool Register(TKey key, TValue value)
        {
            if (key == null || value == null) return false;
            foreach (var guard in _guards)
                if (!guard(key, value))
                    return false;
            if (_dict.TryGetValue(key, out var existing) && existing.kind == EntryKind.User)
                return false;
            _dict[key] = (value, EntryKind.User);
            return true;
        }

        /// <summary>
        /// Register a default (lower-priority) entry. Skipped if any entry for the key already
        /// exists. Intended for framework-registered defaults that users can override.
        /// </summary>
        internal bool RegisterDefault(TKey key, TValue value)
        {
            if (key == null || value == null || _dict.ContainsKey(key)) return false;
            _dict[key] = (value, EntryKind.Default);
            return true;
        }

        public TValue Get(TKey key)
        {
            if (key == null) return default;
            return _dict.TryGetValue(key, out var entry) ? entry.value : default;
        }
    }
}