using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ReleaseGuard.Editor.Core.Config
{
    /// <summary>
    /// Abstract base class providing IMGUI layout and list-field helpers for settings renderers.
    /// All layout values are driven by an <see cref="ISettingsRendererLayout"/> instance supplied
    /// at construction time. Subclass via <see cref="SettingsRenderer"/> rather than directly.
    /// </summary>
    public abstract class SettingsRenderPrimitives
    {
        private GUIStyle _lineListStyle;

        protected SettingsRenderPrimitives(ISettingsRendererLayout layout = null)
        {
            Layout = layout ?? DefaultSettingsRendererLayout.Instance;
        }

        // ReSharper disable once MemberCanBePrivate.Global
        protected ISettingsRendererLayout Layout { get; }

        private GUIStyle LineListStyle =>
            _lineListStyle ??= new GUIStyle(EditorStyles.textArea) { wordWrap = false };

        // ------------------------------------------------------------------
        // Layout primitives
        // ------------------------------------------------------------------

        public void DrawPaddedContent(Action draw)
        {
            var prevLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = Layout.FieldLabelWidth;
            try
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(Layout.PageLeftPadding);
                    using (new EditorGUILayout.VerticalScope())
                    {
                        EditorGUILayout.Space(Layout.PageTopSpacing);
                        draw();
                        EditorGUILayout.Space(Layout.PageBottomSpacing);
                    }
                    GUILayout.Space(Layout.PageRightPadding);
                }
            }
            finally
            {
                EditorGUIUtility.labelWidth = prevLabelWidth;
            }
        }

        protected void Intro(string text) =>
            EditorGUILayout.HelpBox(text, MessageType.None);

        public void HelpBox(string text, MessageType type) =>
            EditorGUILayout.HelpBox(text, type);

        public void Label(string text) =>
            EditorGUILayout.LabelField(text);

        public void Row(Action draw)
        {
            using (new EditorGUILayout.HorizontalScope())
                draw();
        }

        public void Section(string title)
        {
            EditorGUILayout.Space(Layout.SectionTopSpacing);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        }

        public void RelatedFieldSpacing() =>
            EditorGUILayout.Space(Layout.RelatedFieldSpacing);

        public void SectionLink(string name, string path, string description)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(name, GUILayout.ExpandWidth(false)))
                    SettingsService.OpenProjectSettings(path);
                EditorGUILayout.LabelField(description, EditorStyles.miniLabel);
            }
        }

        public void ActionButton(string label, Action onClick)
        {
            if (GUILayout.Button(label, GUILayout.Height(Layout.ActionButtonHeight), GUILayout.ExpandWidth(false)))
                onClick();
        }

        public HashSet<string> Keywords(params string[] keywords) => new(keywords);

        // ------------------------------------------------------------------
        // List field rendering
        // ------------------------------------------------------------------

        /// <summary>
        /// Draws a multiline text area for a serialized string array, one entry per line.
        /// Uses <see cref="SerializedProperty.displayName"/> as the label.
        /// </summary>
        public virtual void LineListField(SerializedProperty prop, string hint) =>
            RenderLineList(prop, prop.displayName, hint, Layout.LineListDefaultMinLines);

        /// <summary>
        /// Draws a multiline text area for a serialized string array with an explicit minimum
        /// line height.
        /// </summary>
        public virtual void LineListField(SerializedProperty prop, string hint, float minLines) =>
            RenderLineList(prop, prop.displayName, hint, minLines);

        /// <param name="displayName">Label to show above the text area (overrides
        /// <see cref="SerializedProperty.displayName"/>).</param>
        protected void RenderLineList(SerializedProperty prop, string displayName, string hint, float minLines)
        {
            EditorGUILayout.LabelField(new GUIContent(displayName, hint));

            EditorGUI.BeginChangeCheck();
            var edited = EditorGUILayout.TextArea(JoinLines(prop), LineListStyle,
                GUILayout.MinHeight(EditorGUIUtility.singleLineHeight * minLines +
                                    Layout.LineListHeightPadding));
            if (EditorGUI.EndChangeCheck())
                WriteLines(prop, edited);

            if (!string.IsNullOrEmpty(hint))
                EditorGUILayout.LabelField(hint, EditorStyles.miniLabel);
        }

        private static string JoinLines(SerializedProperty listProp)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < listProp.arraySize; i++)
            {
                if (i > 0) sb.Append('\n');
                sb.Append(listProp.GetArrayElementAtIndex(i).stringValue);
            }
            return sb.ToString();
        }

        private static void WriteLines(SerializedProperty listProp, string text)
        {
            var lines = (text ?? string.Empty)
                .Replace("\r\n", "\n").Replace('\r', '\n')
                .Split('\n');

            if (lines.Length == 1 && lines[0].Length == 0)
                lines = Array.Empty<string>();

            listProp.arraySize = lines.Length;
            for (var i = 0; i < lines.Length; i++)
                listProp.GetArrayElementAtIndex(i).stringValue = lines[i];
        }
    }
}
