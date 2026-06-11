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
    }
}
