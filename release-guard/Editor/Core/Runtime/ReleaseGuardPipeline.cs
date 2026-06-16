using System;
using System.Collections.Generic;
using System.Linq;
using ReleaseGuard.Editor.Config;
using ReleaseGuard.Editor.Core.Components;

namespace ReleaseGuard.Editor.Core.Runtime
{
    /// <summary>
    /// Unified dispatcher for Release Guard lifecycle events. Root hooks construct concrete
    /// event objects and hand them to this pipeline; listener resolution, ordering, filtering,
    /// and exception handling are centralized here.
    /// </summary>
    public sealed class ReleaseGuardPipeline
    {
        private readonly ReleaseGuardEnvironment _releaseGuard;

        internal ReleaseGuardPipeline(ReleaseGuardEnvironment releaseGuard)
        {
            _releaseGuard = releaseGuard;
        }

        public TEvent Dispatch<TEvent>(TEvent releaseEvent)
            where TEvent : ReleaseGuardLifecycleEvent
        {
            var listeners = ResolveListeners<TEvent>(releaseEvent.Settings);
            _releaseGuard.Logger.LogVerbose(
                $"Registered {listeners.Count} {DescribeEvent(releaseEvent.Kind)} handler(s) for the current run.");

            using (releaseEvent.BeginDispatchScope())
            {
                foreach (var listener in listeners)
                {
                    try
                    {
                        releaseEvent.BeginComponent(listener.Component);
                        listener.Handler(releaseEvent);
                    }
                    catch (Exception e)
                    {
                        releaseEvent.HandleComponentException(_releaseGuard.Logger, listener.Component, e);
                    }
                }
            }

            releaseEvent.SetRegisteredComponents(
                listeners
                    .Select(listener => listener.Component)
                    .GroupBy(component => component.Id, StringComparer.OrdinalIgnoreCase)
                    .Select(group => group.First())
                    .ToList());
            releaseEvent.LogCompletion(_releaseGuard.Logger);

            return releaseEvent;
        }

        public TResult DispatchWithResult<TEvent, TResult>(
            TEvent releaseEvent,
            Func<TEvent, TResult> resultSelector)
            where TEvent : ReleaseGuardLifecycleEvent
        {
            return resultSelector == null
                ? throw new ArgumentNullException(nameof(resultSelector))
                : resultSelector(Dispatch(releaseEvent));
        }

        private List<ReleaseGuardEventListener<TEvent>> ResolveListeners<TEvent>(ReleaseGuardSettings settings)
            where TEvent : ReleaseGuardLifecycleEvent
        {
            return _releaseGuard.EventBus.GetListeners<TEvent>()
                .Where(listener => !settings.IsComponentDisabled(listener.Component.Id))
                .ToList();
        }

        private static string DescribeEvent(ReleaseGuardLifecycleEventKind eventKind)
        {
            return eventKind switch
            {
                ReleaseGuardLifecycleEventKind.PreBuild => "pre-build",
                ReleaseGuardLifecycleEventKind.Build => "build",
                ReleaseGuardLifecycleEventKind.PostBuild => "post-build",
                _ => eventKind.ToString()
            };
        }
    }
}