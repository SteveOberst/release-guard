using System;
using System.Collections.Generic;
using ReleaseGuard.Editor.Config;
using ReleaseGuard.Editor.Core.Build;
using ReleaseGuard.Editor.Core.PostBuild;
using ReleaseGuard.Editor.Core.PreBuild;
using ReleaseGuard.Editor.Core.Runtime;
using ReleaseGuard.Editor.Util;

namespace ReleaseGuard.Editor.Core.Components
{
    public enum ReleaseGuardLifecycleEventKind
    {
        PreBuild = 0,
        Build = 1,
        PostBuild = 2
    }

    public abstract class ReleaseGuardLifecycleEvent
    {
        public abstract ReleaseGuardLifecycleEventKind Kind { get; }
        public abstract ReleaseGuardSettings Settings { get; }

        internal abstract void BeginComponent(ReleaseGuardComponent component);
        internal abstract void SetRegisteredComponents(IReadOnlyList<ReleaseGuardComponent> components);
        internal virtual IDisposable BeginDispatchScope() => NoopScope.Instance;

        internal abstract void HandleComponentException(ReleaseGuardLogger logger, ReleaseGuardComponent component, Exception exception);

        internal virtual void LogCompletion(ReleaseGuardLogger logger)
        {
        }

        private sealed class NoopScope : IDisposable
        {
            public static readonly NoopScope Instance = new();

            public void Dispose()
            {
            }
        }
    }

    public sealed class ReleaseGuardPreBuildEvent : ReleaseGuardLifecycleEvent
    {
        public static ReleaseGuardPreBuildEvent ForBuild(
            ReleaseGuardSettings settings,
            ReleaseGuardConfiguration configuration,
            ReleaseGuardLogger logger,
            UnityEditor.Build.Reporting.BuildReport report)
        {
            return new ReleaseGuardPreBuildEvent(new ReleaseGuardPreBuildContext(
                settings,
                configuration,
                logger,
                report,
                report.summary.platform,
                new List<ReleaseIssue>()));
        }

        public static ReleaseGuardPreBuildEvent ForManualRun(
            ReleaseGuardSettings settings,
            ReleaseGuardConfiguration configuration,
            ReleaseGuardLogger logger,
            UnityEditor.BuildTarget buildTarget)
        {
            return new ReleaseGuardPreBuildEvent(new ReleaseGuardPreBuildContext(
                settings,
                configuration,
                logger,
                buildReport: null,
                buildTarget,
                new List<ReleaseIssue>()));
        }

        public ReleaseGuardPreBuildEvent(ReleaseGuardPreBuildContext context)
        {
            Context = context;
        }

        public override ReleaseGuardLifecycleEventKind Kind => ReleaseGuardLifecycleEventKind.PreBuild;
        public override ReleaseGuardSettings Settings => Context.Settings;
        public ReleaseGuardPreBuildContext Context { get; }
        public bool IsBuildInvocation => Context.BuildReport != null;
        public bool IsManualInvocation => Context.BuildReport == null;
        public ReleaseGuardPreBuildReport Report { get; private set; }

        internal override void BeginComponent(ReleaseGuardComponent component) => Context.BeginComponent(component);

        internal override void SetRegisteredComponents(IReadOnlyList<ReleaseGuardComponent> components) =>
            Report = new ReleaseGuardPreBuildReport(Context.Issues, components);

        internal override IDisposable BeginDispatchScope() => new PreBuildScope();

        internal override void HandleComponentException(ReleaseGuardLogger logger, ReleaseGuardComponent component,
            Exception exception)
        {
            logger.LogException($"Component '{component.Id}' threw and was skipped.", exception);
            Context.Warning(
                $"Component '{component.Id}' failed to run: {exception.Message}",
                fixHint: "This is a bug in the component itself. See the Console for the full stack trace.");
        }

        private sealed class PreBuildScope : IDisposable
        {
            public PreBuildScope()
            {
                MemberInfoUnityPathResolver.BeginPreBuildScope();
            }

            public void Dispose()
            {
                MemberInfoUnityPathResolver.EndPreBuildScope();
            }
        }
    }

    public sealed class ReleaseGuardBuildEvent : ReleaseGuardLifecycleEvent
    {
        public static ReleaseGuardBuildEvent ForBuild(
            ReleaseGuardSettings settings,
            UnityEditor.Build.Reporting.BuildReport report)
        {
            return new ReleaseGuardBuildEvent(
                ReleaseGuardBuildContext.ForBuild(settings, report, new List<ReleaseGuardBuildLog>()));
        }

        public ReleaseGuardBuildEvent(ReleaseGuardBuildContext context)
        {
            Context = context;
        }

        public override ReleaseGuardLifecycleEventKind Kind => ReleaseGuardLifecycleEventKind.Build;
        public override ReleaseGuardSettings Settings => Context.Settings;
        public ReleaseGuardBuildContext Context { get; }
        public ReleaseGuardBuildResult Result { get; private set; }

        internal override void BeginComponent(ReleaseGuardComponent component) => Context.BeginComponent(component);

        internal override void SetRegisteredComponents(IReadOnlyList<ReleaseGuardComponent> components) =>
            Result = new ReleaseGuardBuildResult(Context.LogEntries, components);

        internal override void HandleComponentException(ReleaseGuardLogger logger, ReleaseGuardComponent component,
            Exception exception)
        {
            logger.LogException($"Component '{component.Id}' threw and was skipped.", exception);
            Context.Error($"Component '{component.Id}' failed: {exception.Message}");
        }

        internal override void LogCompletion(ReleaseGuardLogger logger)
        {
            if (Result.HasErrors)
                logger.LogWarning($"Build pipeline completed with {Result.ErrorCount} error(s).");
            else if (Result.HasWarnings)
                logger.LogWarning($"Build pipeline completed with {Result.WarningCount} warning(s).");
            else
                logger.LogVerbose($"Build pipeline completed: {Result.InfoCount} info entry/entries.");
        }
    }

    public sealed class ReleaseGuardPostBuildEvent : ReleaseGuardLifecycleEvent
    {
        public static ReleaseGuardPostBuildEvent ForBuild(
            ReleaseGuardSettings settings,
            UnityEditor.Build.Reporting.BuildReport report)
        {
            return new ReleaseGuardPostBuildEvent(
                ReleaseGuardPostBuildContext.ForBuild(settings, report, new List<ReleaseGuardPostBuildLog>()));
        }

        public ReleaseGuardPostBuildEvent(ReleaseGuardPostBuildContext context)
        {
            Context = context;
        }

        public override ReleaseGuardLifecycleEventKind Kind => ReleaseGuardLifecycleEventKind.PostBuild;
        public override ReleaseGuardSettings Settings => Context.Settings;
        public ReleaseGuardPostBuildContext Context { get; }
        public ReleaseGuardPostBuildResult Result { get; private set; }

        internal override void BeginComponent(ReleaseGuardComponent component) => Context.BeginComponent(component);

        internal override void SetRegisteredComponents(IReadOnlyList<ReleaseGuardComponent> components) =>
            Result = new ReleaseGuardPostBuildResult(Context.LogEntries, components);

        internal override void HandleComponentException(ReleaseGuardLogger logger, ReleaseGuardComponent component,
            Exception exception)
        {
            logger.LogException($"Component '{component.Id}' threw and was skipped.", exception);
            Context.Error($"Component '{component.Id}' failed: {exception.Message}");
        }

        internal override void LogCompletion(ReleaseGuardLogger logger)
        {
            if (Result.HasErrors)
                logger.LogWarning($"Post-build pipeline completed with {Result.ErrorCount} error(s).");
            else if (Result.HasWarnings)
                logger.LogWarning($"Post-build pipeline completed with {Result.WarningCount} warning(s).");
            else
                logger.LogVerbose($"Post-build pipeline completed: {Result.InfoCount} info entry/entries.");
        }
    }
}