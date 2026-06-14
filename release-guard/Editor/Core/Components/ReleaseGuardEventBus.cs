using System;
using System.Collections.Generic;
using System.Linq;
using ReleaseGuard.Editor.Core.Runtime;

namespace ReleaseGuard.Editor.Core.Components
{
    internal sealed class ReleaseGuardEventBus
    {
        private readonly SortedList<ReleaseGuardEventListenerKey, IReleaseGuardEventListener> _listeners = new();
        private int _nextSequence;

        public static ReleaseGuardEventBus Build(
            IReadOnlyList<ReleaseGuardComponent> components,
            Func<string, ReleaseGuardComponentSettings> settingsLookup,
            ReleaseGuardLogger logger)
        {
            var bus = new ReleaseGuardEventBus();
            foreach (var component in components)
                bus.RegisterComponent(component, settingsLookup, logger);
            return bus;
        }

        public void RegisterComponent(
            ReleaseGuardComponent component,
            Func<string, ReleaseGuardComponentSettings> settingsLookup,
            ReleaseGuardLogger logger)
        {
            try
            {
                component.Initialize(settingsLookup);
                var binder = new ReleaseGuardComponentBinder(component);
                component.Register(binder);

                foreach (var listener in binder.Build())
                {
                    var key = new ReleaseGuardEventListenerKey(
                        listener.EventKind,
                        listener.Priority,
                        component.Id,
                        _nextSequence++);
                    _listeners.Add(key, listener);
                }
            }
            catch (Exception e)
            {
                logger.LogException(
                    $"Component '{component.Id}' ({component.GetType().FullName}) threw during Register().",
                    e);
            }
        }

        public IEnumerable<ReleaseGuardEventListener<TEvent>> GetListeners<TEvent>()
            where TEvent : ReleaseGuardLifecycleEvent
        {
            foreach (var entry in _listeners.Values)
            {
                if (entry is ReleaseGuardEventListener<TEvent> typed)
                    yield return typed;
            }
        }

        public IEnumerable<ReleaseGuardLifecycleEventKind> GetSubscribedEvents(string componentId)
        {
            return _listeners.Values
                .Where(entry => string.Equals(entry.Component.Id, componentId, StringComparison.OrdinalIgnoreCase))
                .Select(entry => entry.EventKind)
                .Distinct()
                .ToList();
        }
    }

    internal readonly struct ReleaseGuardEventListenerKey : IComparable<ReleaseGuardEventListenerKey>
    {
        public ReleaseGuardEventListenerKey(
            ReleaseGuardLifecycleEventKind eventKind,
            int priority,
            string componentId,
            int sequence)
        {
            EventKind = eventKind;
            Priority = priority;
            ComponentId = componentId;
            Sequence = sequence;
        }

        public ReleaseGuardLifecycleEventKind EventKind { get; }
        public int Priority { get; }
        public string ComponentId { get; }
        public int Sequence { get; }

        public int CompareTo(ReleaseGuardEventListenerKey other)
        {
            var eventCompare = EventKind.CompareTo(other.EventKind);
            if (eventCompare != 0) return eventCompare;

            var priorityCompare = Priority.CompareTo(other.Priority);
            if (priorityCompare != 0) return priorityCompare;

            var idCompare = string.Compare(ComponentId, other.ComponentId, StringComparison.OrdinalIgnoreCase);
            if (idCompare != 0) return idCompare;

            return Sequence.CompareTo(other.Sequence);
        }
    }

    internal interface IReleaseGuardEventListener
    {
        ReleaseGuardComponent Component { get; }
        ReleaseGuardLifecycleEventKind EventKind { get; }
        int Priority { get; }
    }

    internal sealed class ReleaseGuardEventListener<TEvent> : IReleaseGuardEventListener
        where TEvent : ReleaseGuardLifecycleEvent
    {
        public ReleaseGuardEventListener(
            ReleaseGuardComponent component,
            ReleaseGuardLifecycleEventKind eventKind,
            int priority,
            Action<TEvent> handler)
        {
            Component = component;
            EventKind = eventKind;
            Priority = priority;
            Handler = handler;
        }

        public ReleaseGuardComponent Component { get; }
        public ReleaseGuardLifecycleEventKind EventKind { get; }
        public int Priority { get; }
        public Action<TEvent> Handler { get; }
    }
}