using System;
using System.Linq;
using System.Collections.Generic;
using ReleaseGuard.Editor.Core.Components;
using ReleaseGuard.Editor.Core.PreBuild;
using ReleaseGuard.Editor.Config;
using ReleaseGuard.Editor.Core.DI;
using ReleaseGuard.Editor.Core.Plugins;
using ReleaseGuard.Editor.Core.Runtime;
using UnityEditor;
using UnityEngine;

namespace ReleaseGuard.Editor.UI
{
    /// <summary>
    /// Editor window that runs the pre-build checks on demand and shows the findings, grouped and
    /// filterable by severity, each with a "ping asset" shortcut and a fix hint.
    ///
    /// Advisories (dismissible warnings) include a "Don't show again" button that writes the
    /// advisory id plus display context into <see cref="AdvisorySuppressionStore"/>
    /// (profile-independent) and re-runs the checks so the dismissed item disappears immediately.
    ///
    /// Built with IMGUI (EditorGUILayout) -- the conventional, dependency-free choice for editor
    /// tooling.
    /// </summary>
    public sealed class ReleaseGuardWindow : EditorWindow
    {
        private static class Layout
        {
            public const float MinWidth = 560f;
            public const float MinHeight = 380f;
            public const float ToolbarButtonWidth = 90f;
            public const float TightSpacing = 2f;
            public const float SectionSpacing = 4f;
            public const float StatusIconWidth = 28f;
            public const float ComponentIdWidth = 180f;
            public const float ComponentNameMinWidth = 120f;
            public const float PhasesWidth = 180f;
            public const float IssueCountWidth = 70f;
            public const float PluginIdWidth = 220f;
            public const float PluginAuthorWidth = 120f;
            public const float FilterLabelWidth = 40f;
            public const float DismissButtonWidth = 118f;
            public const float PingButtonWidth = 80f;

            public static readonly Color IssueIndicatorColor = new Color(1f, 0.75f, 0.35f);
            public static readonly Color CleanIndicatorColor = new Color(0.6f, 1f, 0.6f);
        }

        private ReleaseGuardPreBuildReport _report;
        private Vector2 _scroll;

        private bool _showErrors = true;
        private bool _showWarnings = true;
        private bool _showInfo = true;
        private bool _showComponents;
        private bool _showPlugins;

        [MenuItem("Tools/Release Guard/Pre-Build Checks")]
        public static void ShowWindow()
        {
            var window = GetWindow<ReleaseGuardWindow>("Release Guard");
            window.minSize = new Vector2(Layout.MinWidth, Layout.MinHeight);
            window.Show();
        }

        /// <summary>Open the window and immediately run the checks (handy for menu shortcuts / CI hooks).</summary>
        // ReSharper disable once UnusedMember.Global
        public static void ShowWindowAndRunChecks()
        {
            ShowWindow();
            GetWindow<ReleaseGuardWindow>().RunPreBuildChecks();
        }

        private void OnGUI()
        {
            DrawToolbar();

            if (_report == null)
            {
                EditorGUILayout.HelpBox(
                    "Run the pre-build checks to inspect this project's release readiness.\n" +
                    "The same checks run automatically before every non-development build.",
                    MessageType.Info);
                return;
            }

            DrawSummary();
            DrawRegisteredComponents();
            DrawDiscoveredPlugins();
            DrawFilters();
            DrawIssues();
        }

        // --- Toolbar ---

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Run Checks", EditorStyles.toolbarButton,
                        GUILayout.Width(Layout.ToolbarButtonWidth)))
                    RunPreBuildChecks();

                if (GUILayout.Button("Settings", EditorStyles.toolbarButton,
                        GUILayout.Width(Layout.ToolbarButtonWidth)))
                    SettingsService.OpenProjectSettings("Project/Release Guard");

                GUILayout.FlexibleSpace();

                if (_report == null) return;
                var componentCount = _report.RegisteredComponents.Count;
                GUILayout.Label(
                    $"{componentCount} component(s), highest: {_report.HighestSeverity}",
                    EditorStyles.miniLabel);
            }
        }

        // --- Summary ---

        private void DrawSummary()
        {
            string message;
            MessageType type;

            if (!_report.HasIssues)
            {
                message = "No issues found. This project looks ready to release.";
                type = MessageType.Info;
            }
            else
            {
                message =
                    $"{_report.ErrorCount} error(s), " +
                    $"{_report.WarningCount} warning(s), " +
                    $"{_report.InfoCount} info.";

                type = _report.HasErrors ? MessageType.Error
                    : _report.WarningCount > 0 ? MessageType.Warning
                    : MessageType.Info;
            }

            EditorGUILayout.HelpBox(message, type);
        }

        // --- Registered components foldout ---

        private void DrawRegisteredComponents()
        {
            var environment = ReleaseGuardDI.Resolve<ReleaseGuardEnvironment>();
            var components = environment.Components.Items;
            if (components.Count == 0)
                return;

            EditorGUILayout.Space(Layout.TightSpacing);
            _showComponents = EditorGUILayout.Foldout(
                _showComponents,
                $"Registered components ({components.Count})",
                toggleOnLabelClick: true);

            if (!_showComponents)
                return;

            EditorGUI.indentLevel++;

            foreach (var component in components)
            {
                var issueCount = _report.Issues.Count(i => i.ComponentId == component.Id);
                var phases = DescribePhases(component.Id, environment);

                using (new EditorGUILayout.HorizontalScope())
                {
                    var prevColor = GUI.contentColor;
                    GUI.contentColor = issueCount > 0
                        ? Layout.IssueIndicatorColor
                        : Layout.CleanIndicatorColor;
                    EditorGUILayout.LabelField("[*]", GUILayout.Width(Layout.StatusIconWidth));
                    GUI.contentColor = prevColor;

                    EditorGUILayout.LabelField(component.Id, GUILayout.Width(Layout.ComponentIdWidth));

                    EditorGUILayout.LabelField(
                        component.DisplayName,
                        EditorStyles.miniLabel,
                        GUILayout.MinWidth(Layout.ComponentNameMinWidth));

                    EditorGUILayout.LabelField(phases, EditorStyles.miniLabel, GUILayout.Width(Layout.PhasesWidth));
                    GUILayout.FlexibleSpace();

                    var countLabel = issueCount > 0 ? $"{issueCount} issue(s)" : "clean";
                    EditorGUILayout.LabelField(countLabel, EditorStyles.miniLabel,
                        GUILayout.Width(Layout.IssueCountWidth));
                }
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.HelpBox(
                "Run Checks dispatches the pre-build event without an active BuildReport. Build and post-build subscriptions run only during real builds.",
                MessageType.None);
            EditorGUILayout.Space(Layout.SectionSpacing);
        }

        // --- Discovered plugins foldout ---

        private void DrawDiscoveredPlugins()
        {
            EditorGUILayout.Space(Layout.TightSpacing);

            _showPlugins = EditorGUILayout.Foldout(
                _showPlugins,
                "Plugins",
                toggleOnLabelClick: true);

            if (!_showPlugins)
                return;

            var plugins = ReleaseGuardDI.Resolve<ReleaseGuardEnvironment>().Plugins;

            EditorGUI.indentLevel++;

            if (plugins.Count == 0)
            {
                EditorGUILayout.LabelField(
                    "No plugins registered. Register one explicitly or enable plugin auto-discovery.",
                    EditorStyles.miniLabel);
            }
            else
            {
                foreach (var plugin in plugins)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(plugin.PluginId, GUILayout.Width(Layout.PluginIdWidth));
                        EditorGUILayout.LabelField(plugin.DisplayName, EditorStyles.miniLabel,
                            GUILayout.MinWidth(Layout.ComponentNameMinWidth));
                        GUILayout.FlexibleSpace();
                        if (!string.IsNullOrEmpty(plugin.Author))
                            EditorGUILayout.LabelField(plugin.Author, EditorStyles.miniLabel,
                                GUILayout.Width(Layout.PluginAuthorWidth));
                    }
                }
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(Layout.SectionSpacing);
        }

        // --- Severity filters ---

        private void DrawFilters()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Show:", GUILayout.Width(Layout.FilterLabelWidth));
                _showErrors = GUILayout.Toggle(_showErrors, $"Errors ({_report.ErrorCount})", EditorStyles.miniButton);
                _showWarnings = GUILayout.Toggle(_showWarnings, $"Warnings ({_report.WarningCount})",
                    EditorStyles.miniButton);
                _showInfo = GUILayout.Toggle(_showInfo, $"Info ({_report.InfoCount})", EditorStyles.miniButton);
                GUILayout.FlexibleSpace();
            }
        }

        // --- Issue list ---

        private void DrawIssues()
        {
            using var scrollScope = new EditorGUILayout.ScrollViewScope(_scroll);
            _scroll = scrollScope.scrollPosition;

            // Most serious first.
            var visible = _report.Issues
                .Where(IsVisible)
                .OrderByDescending(i => i.Severity);

            var any = false;
            foreach (var issue in visible)
            {
                any = true;
                DrawIssue(issue);
                EditorGUILayout.Space(Layout.SectionSpacing);
            }

            if (!any && _report.HasIssues)
                EditorGUILayout.HelpBox("No issues match the current filters.", MessageType.None);
        }

        private bool IsVisible(ReleaseIssue issue) => issue.Severity switch
        {
            ReleaseIssueSeverity.Error => _showErrors,
            ReleaseIssueSeverity.Warning => _showWarnings,
            _ => _showInfo
        };

        private void DrawIssue(ReleaseIssue issue)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.HelpBox(issue.Message, ToMessageType(issue.Severity));

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"Component: {issue.ComponentId}", EditorStyles.miniLabel);
                    GUILayout.FlexibleSpace();

                    // "Don't show again" button for dismissible advisories.
                    if (!string.IsNullOrEmpty(issue.SuppressId))
                    {
                        if (GUILayout.Button("Don't show again", EditorStyles.miniButton,
                                GUILayout.Width(Layout.DismissButtonWidth)))
                        {
                            var environment = ReleaseGuardDI.Resolve<ReleaseGuardEnvironment>();
                            var displayName = environment.Components.Items
                                .FirstOrDefault(c => c.Id == issue.ComponentId)?.DisplayName ?? issue.ComponentId;
                            environment.Settings.SuppressAdvisory(
                                issue.SuppressId,
                                issue.Message,
                                issue.ComponentId,
                                displayName);
                            ReleaseGuardStartup.Reload();
                            RunPreBuildChecks(); // re-run so the dismissed advisory disappears
                            return; // DrawIssue may no longer be safe to continue; bail out
                        }
                    }

                    if (!string.IsNullOrEmpty(issue.AssetPath) &&
                        GUILayout.Button("Ping asset", EditorStyles.miniButton,
                            GUILayout.Width(Layout.PingButtonWidth)))
                        PingAsset(issue.AssetPath);
                }

                if (!string.IsNullOrEmpty(issue.AssetPath))
                    EditorGUILayout.LabelField("Asset", issue.AssetPath, EditorStyles.miniLabel);

                if (!string.IsNullOrEmpty(issue.FixHint))
                    EditorGUILayout.LabelField("Fix", issue.FixHint, EditorStyles.wordWrappedLabel);
            }
        }

        // --- Helpers ---

        private void RunPreBuildChecks()
        {
            ReleaseGuardEnvironment environment;
            try
            {
                environment = ReleaseGuardDI.Resolve<ReleaseGuardEnvironment>();
            }
            catch (InvalidOperationException)
            {
                // Container was cleared (e.g. by a domain-reload event) and startup hasn't re-run yet.
                ReleaseGuardStartup.Reload();
                environment = ReleaseGuardDI.Resolve<ReleaseGuardEnvironment>();
            }

            var configuration = ReleaseGuardConfiguration.Resolve(environment.Settings, report: null);
            _report = environment.Pipeline.DispatchWithResult(
                ReleaseGuardPreBuildEvent.ForManualRun(
                    environment.Settings,
                    configuration,
                    environment.Logger,
                    EditorUserBuildSettings.activeBuildTarget),
                releaseEvent => releaseEvent.Report);
            Repaint();
        }

        private static string DescribePhases(string componentId, ReleaseGuardEnvironment environment)
        {
            var phases = environment.EventBus.GetSubscribedEvents(componentId)
                .Select(DescribePhase)
                .ToList();
            return string.Join(", ", phases);
        }

        private static string DescribePhase(ReleaseGuardLifecycleEventKind releaseEvent) => releaseEvent switch
        {
            ReleaseGuardLifecycleEventKind.PreBuild => "pre-build",
            ReleaseGuardLifecycleEventKind.Build => "build",
            ReleaseGuardLifecycleEventKind.PostBuild => "post-build",
            _ => releaseEvent.ToString()
        };

        private static MessageType ToMessageType(ReleaseIssueSeverity severity) => severity switch
        {
            ReleaseIssueSeverity.Error => MessageType.Error,
            ReleaseIssueSeverity.Warning => MessageType.Warning,
            _ => MessageType.Info
        };

        private static void PingAsset(string assetPath)
        {
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            if (asset == null)
                return;

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }
    }
}