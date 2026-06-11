using System.Linq;
using ReleaseGuard.Editor.Config;
using ReleaseGuard.Editor.Core.Audit;
using ReleaseGuard.Editor.Core.DI;
using ReleaseGuard.Editor.Core.Plugins;
using ReleaseGuard.Editor.Core.PostProcessing;
using ReleaseGuard.Editor.Core.Runtime;
using ReleaseGuard.Editor.Core.Transforming;
using UnityEditor;
using UnityEngine;

namespace ReleaseGuard.Editor.UI
{
    /// <summary>
    /// Editor window that runs the release audit on demand and shows the findings, grouped and
    /// filterable by severity, each with a "ping asset" shortcut and a fix hint.
    ///
    /// Advisories (dismissible warnings) include a "Don't show again" button that writes the
    /// advisory id into <see cref="AuditorSettings.suppressedAdvisoryIds"/> and re-runs
    /// the audit so the dismissed item disappears immediately.
    ///
    /// Built with IMGUI (EditorGUILayout) -- the conventional, dependency-free choice for editor
    /// tooling.
    /// </summary>
    public sealed class ReleaseGuardWindow : EditorWindow
    {
        private ReleaseGuardReport _report;
        private Vector2 _scroll;

        private bool _showErrors = true;
        private bool _showWarnings = true;
        private bool _showInfo = true;
        private bool _showAuditors;
        private bool _showPostProcessors;
        private bool _showTransformers;
        private bool _showPlugins;

        [MenuItem("Tools/Release Guard/Audit")]
        public static void ShowWindow()
        {
            var window = GetWindow<ReleaseGuardWindow>("Release Guard");
            window.minSize = new Vector2(560, 380);
            window.Show();
        }

        /// <summary>Open the window and immediately run an audit (handy for menu shortcuts / CI hooks).</summary>
        // ReSharper disable once UnusedMember.Global
        public static void ShowWindowAndRunAudit()
        {
            ShowWindow();
            GetWindow<ReleaseGuardWindow>().RunAudit();
        }

        private void OnGUI()
        {
            DrawToolbar();

            if (_report == null)
            {
                EditorGUILayout.HelpBox(
                    "Run an audit to check this project's release readiness.\n" +
                    "The same checks run automatically before every non-development build.",
                    MessageType.Info);
                return;
            }

            DrawSummary();
            DrawDiscoveredAuditors();
            DrawDiscoveredPostProcessors();
            DrawDiscoveredTransformers();
            DrawDiscoveredPlugins();
            DrawFilters();
            DrawIssues();
        }

        // --- Toolbar ---

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Run Audit", EditorStyles.toolbarButton, GUILayout.Width(90)))
                    RunAudit();

                if (GUILayout.Button("Settings", EditorStyles.toolbarButton, GUILayout.Width(90)))
                    SettingsService.OpenProjectSettings("Project/Release Guard");

                GUILayout.FlexibleSpace();

                if (_report == null) return;
                var auditorCount = _report.DiscoveredAuditors.Count;
                GUILayout.Label(
                    $"{auditorCount} auditor(s), highest: {_report.HighestSeverity}",
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
                message = "No issues found -- this project looks ready to release.";
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

        // --- Discovered auditors foldout ---

        private void DrawDiscoveredAuditors()
        {
            var auditors = _report.DiscoveredAuditors;
            if (auditors.Count == 0)
                return;

            EditorGUILayout.Space(2);
            _showAuditors = EditorGUILayout.Foldout(
                _showAuditors,
                $"Discovered auditors ({auditors.Count})",
                toggleOnLabelClick: true);

            if (!_showAuditors)
                return;

            EditorGUI.indentLevel++;

            foreach (var auditor in auditors)
            {
                var issueCount = _report.Issues.Count(i => i.AuditorId == auditor.Id);

                using (new EditorGUILayout.HorizontalScope())
                {
                    var prevColor = GUI.contentColor;
                    GUI.contentColor = issueCount > 0
                        ? new Color(1f, 0.75f, 0.35f)
                        : new Color(0.6f, 1f, 0.6f);
                    EditorGUILayout.LabelField("[*]", GUILayout.Width(28));
                    GUI.contentColor = prevColor;

                    EditorGUILayout.LabelField(auditor.Id, GUILayout.Width(180));

                    EditorGUILayout.LabelField(
                        auditor.DisplayName,
                        EditorStyles.miniLabel,
                        GUILayout.MinWidth(120));

                    GUILayout.FlexibleSpace();

                    var countLabel = issueCount > 0 ? $"{issueCount} issue(s)" : "clean";
                    EditorGUILayout.LabelField(countLabel, EditorStyles.miniLabel, GUILayout.Width(70));
                }
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(4);
        }

        // --- Discovered post-processors foldout ---

        private void DrawDiscoveredPostProcessors()
        {
            EditorGUILayout.Space(2);

            _showPostProcessors = EditorGUILayout.Foldout(
                _showPostProcessors,
                "Post-processors (post-build)",
                toggleOnLabelClick: true);

            if (!_showPostProcessors)
                return;

            var postProcessors = DI.Resolve<ReleaseGuardEnvironment>().Registries.PostProcessors.Items;

            if (postProcessors.Count == 0)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField(
                    "No post-processors discovered (auto-discovery may be disabled in Settings).",
                    EditorStyles.miniLabel);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space(4);
                return;
            }

            EditorGUI.indentLevel++;
            foreach (var pp in postProcessors)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(pp.Id, GUILayout.Width(200));
                    EditorGUILayout.LabelField(pp.DisplayName, EditorStyles.miniLabel, GUILayout.MinWidth(120));
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField($"Priority {pp.Priority}", EditorStyles.miniLabel, GUILayout.Width(80));
                }
            }

            EditorGUI.indentLevel--;

            EditorGUILayout.HelpBox(
                "Post-processors run automatically after every release build. " +
                "They are not triggered by the manual 'Run Audit' button.",
                MessageType.None);
            EditorGUILayout.Space(4);
        }

        // --- Discovered transformers foldout ---

        private void DrawDiscoveredTransformers()
        {
            EditorGUILayout.Space(2);

            _showTransformers = EditorGUILayout.Foldout(
                _showTransformers,
                "Transformers (artifact-level)",
                toggleOnLabelClick: true);

            if (!_showTransformers)
                return;

            var transformers = DI.Resolve<ReleaseGuardEnvironment>().Registries.Transformers.Items;

            if (transformers.Count == 0)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField(
                    "No transformers discovered. Derive from ReleaseTransformer to add one.",
                    EditorStyles.miniLabel);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space(4);
                return;
            }

            EditorGUI.indentLevel++;
            foreach (var transformer in transformers)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(transformer.Id, GUILayout.Width(200));
                    EditorGUILayout.LabelField(transformer.DisplayName, EditorStyles.miniLabel,
                        GUILayout.MinWidth(120));
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField($"Priority {transformer.Priority}", EditorStyles.miniLabel,
                        GUILayout.Width(80));
                }
            }

            EditorGUI.indentLevel--;
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

            var plugins = DI.Resolve<ReleaseGuardEnvironment>().Plugins;

            EditorGUI.indentLevel++;

            if (plugins.Count == 0)
            {
                EditorGUILayout.LabelField(
                    "No plugins discovered. Derive from ReleaseGuardPlugin to add one.",
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
                    EditorGUILayout.LabelField($"Auditor: {issue.AuditorId}", EditorStyles.miniLabel);
                    GUILayout.FlexibleSpace();

                    // "Don't show again" button for dismissible advisories.
                    if (!string.IsNullOrEmpty(issue.SuppressId))
                    {
                        if (GUILayout.Button("Don't show again", EditorStyles.miniButton, GUILayout.Width(118)))
                        {
                            DI.Resolve<ReleaseGuardEnvironment>().Settings.SuppressAdvisory(issue.SuppressId);
                            ReleaseGuardStartup.Reload();
                            RunAudit(); // re-run so the dismissed advisory disappears
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

        private void RunAudit()
        {
            _report = DI.Resolve<ReleaseGuardEnvironment>().AuditPipeline.RunInEditor();
            Repaint();
        }

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
