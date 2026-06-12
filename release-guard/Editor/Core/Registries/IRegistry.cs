using System;
using System.Collections.Generic;

namespace ReleaseGuard.Editor.Core.Registries
{
    /// <summary>
    /// Read/write registry of keyed values.
    /// </summary>
    public interface IRegistry<TKey, TValue>
    {
        IReadOnlyList<TValue> Items { get; }

        // ReSharper disable once UnusedMethodReturnValue.Global
        bool Register(TKey key, TValue value);

        TValue Get(TKey key);

        /// <summary>
        /// Add a guard that is consulted on every <see cref="Register"/> call.
        /// The guard receives the key and value; return <c>true</c> to allow the registration,
        /// <c>false</c> to silently block it. Multiple guards are AND-ed: all must return
        /// <c>true</c> for the item to be admitted.
        /// </summary>
        void AddRegistrationGuard(Func<TKey, TValue, bool> canRegister);
    }
}