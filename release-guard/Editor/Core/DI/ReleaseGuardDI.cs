using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

// ReSharper disable UnusedMember.Global
namespace ReleaseGuard.Editor.Core.DI
{
    public enum ServiceLifetime
    {
        Singleton,
        Transient
    }

    // ReSharper disable once InconsistentNaming
    public static class ReleaseGuardDI
    {
        private static volatile Container _container = new();

        public static void Configure(Action<Container> configure)
        {
            if (configure is null) throw new ArgumentNullException(nameof(configure));

            Clear();

            _container = new Container();
            configure(_container);
        }

        public static void Use(Container externalContainer)
        {
            if (externalContainer is null) throw new ArgumentNullException(nameof(externalContainer));
            Clear();
            _container = externalContainer;
        }

        public static T Resolve<T>()
        {
            return _container.Resolve<T>();
        }

        public static object Resolve(Type serviceType)
        {
            return _container.Resolve(serviceType);
        }

        public static void RegisterSingleton<TService, TImplementation>()
            where TImplementation : TService
        {
            _container.RegisterSingleton<TService, TImplementation>();
        }

        public static void RegisterSingleton<TService>()
        {
            _container.RegisterSingleton<TService>();
        }

        public static void RegisterTransient<TService, TImplementation>()
            where TImplementation : TService
        {
            _container.RegisterTransient<TService, TImplementation>();
        }

        public static void RegisterTransient<TService>()
        {
            _container.RegisterTransient<TService>();
        }

        public static void RegisterInstance<TService>(TService instance)
            where TService : notnull
        {
            _container.RegisterInstance(instance);
        }

        public static void Clear()
        {
            if (_container is null)
            {
                return;
            }

            _container.Dispose();
            _container = null;
        }
    }

    public sealed class Container : IDisposable
    {
        private readonly Dictionary<Type, ServiceDescriptor> _services = new();
        private readonly Dictionary<Type, object> _singletons = new();
        private readonly Stack<Type> _resolutionStack = new();

        private bool _disposed;

        public void RegisterSingleton<TService, TImplementation>()
            where TImplementation : TService
        {
            Register(typeof(TService), typeof(TImplementation), ServiceLifetime.Singleton);
        }

        public void RegisterSingleton<TService>()
        {
            Register(typeof(TService), typeof(TService), ServiceLifetime.Singleton);
        }

        public void RegisterTransient<TService, TImplementation>()
            where TImplementation : TService
        {
            Register(typeof(TService), typeof(TImplementation), ServiceLifetime.Transient);
        }

        public void RegisterTransient<TService>()
        {
            Register(typeof(TService), typeof(TService), ServiceLifetime.Transient);
        }

        public void RegisterInstance<TService>(TService instance)
            where TService : notnull
        {
            ThrowIfDisposed();

            var serviceType = typeof(TService);

            _services[serviceType] = new ServiceDescriptor(
                serviceType,
                instance.GetType(),
                ServiceLifetime.Singleton
            );

            _singletons[serviceType] = instance;
        }

        public T Resolve<T>()
        {
            return (T)Resolve(typeof(T));
        }

        public object Resolve(Type serviceType)
        {
            ThrowIfDisposed();

            if (_resolutionStack.Contains(serviceType))
            {
                var chain = string.Join(
                    " -> ",
                    _resolutionStack.Reverse().Select(type => type.Name)
                );

                throw new InvalidOperationException(
                    $"Circular dependency detected: {chain} -> {serviceType.Name}"
                );
            }

            _resolutionStack.Push(serviceType);

            try
            {
                return ResolveInternal(serviceType);
            }
            finally
            {
                _resolutionStack.Pop();
            }
        }

        private object ResolveInternal(Type serviceType)
        {
            if (_singletons.TryGetValue(serviceType, out var singleton))
            {
                return singleton;
            }

            if (_services.TryGetValue(serviceType, out var descriptor))
            {
                var instance = CreateInstance(descriptor.ImplementationType);

                if (descriptor.Lifetime == ServiceLifetime.Singleton)
                {
                    _singletons[serviceType] = instance;
                }

                return instance;
            }

            if (!serviceType.IsAbstract && !serviceType.IsInterface)
            {
                return CreateInstance(serviceType);
            }

            throw new InvalidOperationException(
                $"No service registered for type '{serviceType.FullName}'."
            );
        }

        private object CreateInstance(Type implementationType)
        {
            var constructors = implementationType
                .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .OrderByDescending(constructor => constructor.GetParameters().Length)
                .ToArray();

            if (constructors.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Type '{implementationType.FullName}' has no public constructor."
                );
            }

            foreach (var constructor in constructors)
            {
                var parameters = constructor.GetParameters();

                try
                {
                    var arguments = parameters
                        .Select(parameter => Resolve(parameter.ParameterType))
                        .ToArray();

                    return Activator.CreateInstance(implementationType, arguments)
                           ?? throw new InvalidOperationException(
                               $"Failed to create instance of '{implementationType.FullName}'."
                           );
                }
                catch
                {
                    // Try another constructor.
                    // A production container would preserve and report these errors.
                }
            }

            throw new InvalidOperationException(
                $"Could not resolve constructor dependencies for '{implementationType.FullName}'."
            );
        }

        private void Register(Type serviceType, Type implementationType, ServiceLifetime lifetime)
        {
            ThrowIfDisposed();

            if (!serviceType.IsAssignableFrom(implementationType))
            {
                throw new ArgumentException(
                    $"Type '{implementationType.FullName}' is not assignable to '{serviceType.FullName}'."
                );
            }

            _services[serviceType] = new ServiceDescriptor(
                serviceType,
                implementationType,
                lifetime
            );
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(Container));
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            foreach (var singleton in _singletons.Values)
            {
                if (singleton is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            _services.Clear();
            _singletons.Clear();
            _resolutionStack.Clear();

            _disposed = true;
        }

        private sealed record ServiceDescriptor(
            // ReSharper disable once NotAccessedPositionalProperty.Local
            Type ServiceType,
            Type ImplementationType,
            ServiceLifetime Lifetime
        );
    }
}
// ReSharper enableUnusedMember.Global