using System;
using System.Collections.Generic;
using UnityEngine;

namespace AttackSurfaceFixture.Game.Core
{
    /// <summary>
    /// Lightweight service registry. Systems register themselves at startup and resolve
    /// dependencies by type without coupling to specific MonoBehaviour components.
    ///
    /// <b>IL2CPP note:</b> <c>typeof(T)</c> is evaluated at AOT compile time to a direct
    /// metadata token  -- it does not call <c>Assembly.GetTypes()</c> or any dynamic
    /// discovery at runtime. The <c>Dictionary&lt;Type, object&gt;</c> lookup uses only
    /// the runtime type table baked into the IL2CPP binary. This pattern is fully safe
    /// under managed stripping because every registered type is directly referenced
    /// in game code and therefore visible to the linker.
    ///
    /// Prefer this over full-fat DI containers that scan assemblies for attributes: those
    /// containers require either broad <c>[Preserve]</c> rules or explicit registrations  --
    /// you might as well write the explicit registrations here and skip the container.
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> Services = new();

        /// <summary>Registers <paramref name="instance"/> under the key <typeparamref name="TService"/>.</summary>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="instance"/> is null.</exception>
        public static void Register<TService>(TService instance) where TService : class
        {
            Services[typeof(TService)] = instance ?? throw new ArgumentNullException(nameof(instance),
                $"[ServiceLocator] Cannot register a null instance for {typeof(TService).Name}.");
        }

        /// <summary>
        /// Returns the registered instance for <typeparamref name="TService"/>, or logs an error
        /// and returns null if the service has not been registered.
        /// </summary>
        public static TService Get<TService>() where TService : class
        {
            if (Services.TryGetValue(typeof(TService), out var svc))
                return (TService)svc;

            Debug.LogError($"[ServiceLocator] No service registered for '{typeof(TService).Name}'. " +
                           "Did the owning MonoBehaviour register in Awake before the caller's Start?");
            return null;
        }

        /// <summary>Tries to resolve <typeparamref name="TService"/> without logging errors.</summary>
        public static bool TryGet<TService>(out TService service) where TService : class
        {
            if (Services.TryGetValue(typeof(TService), out var svc))
            {
                service = (TService)svc;
                return true;
            }

            service = null;
            return false;
        }

        /// <summary>Removes the registration for <typeparamref name="TService"/>.</summary>
        public static void Unregister<TService>() where TService : class
            => Services.Remove(typeof(TService));

        /// <summary>Clears all registrations. Call between scenes or during test teardown.</summary>
        public static void Clear() => Services.Clear();
    }
}
