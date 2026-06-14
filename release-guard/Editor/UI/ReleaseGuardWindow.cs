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
            window.minSize = new Vector2(560, 380);
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
                if (GUILayout.Button("Run Checks", EditorStyles.toolbarButton, GUILayout.Width(90)))
                    RunPreBuildChecks();

                if (GUILayout.Button("Settings", EditorStyles.toolbarButton, GUILayout.Width(90)))
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

            EditorGUILayout.Space(2);
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
                        ? new Color(1f, 0.75f, 0.35f)
                        : new Color(0.6f, 1f, 0.6f);
                    EditorGUILayout.LabelField("[*]", GUILayout.Width(28));
                    GUI.contentColor = prevColor;

                    EditorGUILayout.LabelField(component.Id, GUILayout.Width(180));

                    EditorGUILayout.LabelField(
                        component.DisplayName,
                        EditorStyles.miniLabel,
                        GUILayout.MinWidth(120));

                    EditorGUILayout.LabelField(phases, EditorStyles.miniLabel, GUILayout.Width(180));
                    GUILayout.FlexibleSpace();

                    var countLabel = issueCount > 0 ? $"{issueCount} issue(s)" : "clean";
                    EditorGUILayout.LabelField(countLabel, EditorStyles.miniLabel, GUILayout.Width(70));
                }
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.HelpBox(
                "Run Checks dispatches the pre-build event without an active BuildReport. Build and post-build subscriptions run only during real builds.",
                MessageType.None);
            EditorGUILayout.Space(4);
        }

        // --- Discovered plugins foldout ---

        private void DrawDiscoveredPlugins()
        {
            EditorGUILayout.Space(2);

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
                        EditorGUILayout.LabelField(plugin.PluginId, GUILayout.Width(220));
                        EditorGUILayout.LabelField(plugin.DisplayName, EditorStyles.miniLabel, GUILayout.MinWidth(120));
                        GUILayout.FlexibleSpace();
                        if (!string.IsNullOrEmpty(plugin.Author))
                            EditorGUILayout.LabelField(plugin.Author, EditorStyles.miniLabel, GUILayout.Width(120));
                    }
                }
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(4);
        }

        // --- Severity filters ---

        private void DrawFilters()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Show:", GUILayout.Width(40));
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
                EditorGUILayout.Space(4);
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
                        if (GUILayout.Button("Don't show again", EditorStyles.miniButton, GUILayout.Width(118)))
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
                        GUILayout.Button("Ping asset", EditorStyles.miniButton, GUILayout.Width(80)))
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
            var environment = ReleaseGuardDI.Resolve<ReleaseGuardEnvironment>();
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
            var asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            if (asset == null)
                return;

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }
    }
}
