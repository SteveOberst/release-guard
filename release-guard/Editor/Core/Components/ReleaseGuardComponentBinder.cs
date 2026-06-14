using System;
using System.Collections.Generic;
using ReleaseGuard.Editor.Core.Build;
using ReleaseGuard.Editor.Core.PostBuild;
using ReleaseGuard.Editor.Core.PreBuild;

namespace ReleaseGuard.Editor.Core.Components
{
    /// <summary>
    /// Public registration surface for component event handlers. Components call these methods
    /// from <see cref="ReleaseGuardComponent.Register"/> to subscribe to Release Guard lifecycle events.
    /// </summary>
    public sealed class ReleaseGuardComponentBinder
    {
        private readonly List<IReleaseGuardEventListener> _registrations = new();
        private readonly ReleaseGuardComponent _component;

        internal ReleaseGuardComponentBinder(ReleaseGuardComponent component)
        {
            _component = component;
        }

        public void OnPreBuild(
            Action<ReleaseGuardPreBuildEvent> handler,
            int priority = 0)
        {
            if (handler == null) return;
            _registrations.Add(new ReleaseGuardEventListener<ReleaseGuardPreBuildEvent>(
                _component,
                ReleaseGuardLifecycleEventKind.PreBuild,
                priority,
                handler));
        }

        public void OnBuild(
            Action<ReleaseGuardBuildEvent> handler,
            int priority = 0)
        {
            if (handler == null) return;
            _registrations.Add(new ReleaseGuardEventListener<ReleaseGuardBuildEvent>(
                _component,
                ReleaseGuardLifecycleEventKind.Build,
                priority,
                handler));
        }

        public void OnPostBuild(
            Action<ReleaseGuardPostBuildEvent> handler,
            int priority = 0)
        {
            if (handler == null) return;
            _registrations.Add(new ReleaseGuardEventListener<ReleaseGuardPostBuildEvent>(
                _component,
                ReleaseGuardLifecycleEventKind.PostBuild,
                priority,
                handler));
        }

        internal IReadOnlyList<IReleaseGuardEventListener> Build() => _registrations;
    }
}
