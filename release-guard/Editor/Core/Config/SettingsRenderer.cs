using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ReleaseGuard.Editor.Config;
using ReleaseGuard.Editor.Core.Runtime;
using UnityEditor;
using UnityEngine;

// ReSharper disable MemberCanBeMadeStatic.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBeProtected.Global
// ReSharper disable VirtualMemberNeverOverridden.Global
namespace ReleaseGuard.Editor.Core.Config
{
    /// <summary>
    /// Marks a settings sub-object field as its own page in the Project Settings tree.
    /// Apply to public sub-object fields on <see cref="ReleaseGuardSettings"/>; the renderer
    /// discovers these via reflection and generates one SettingsProvider per annotated field.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class SettingsPageAttribute : Attribute
    {
        /// <summary>Sort order for the page within its parent (lower = higher in the list).</summary>
        public int Order { get; }

        /// <summary>Display name shown in the Project Settings sidebar.</summary>
        public string Label { get; }

        /// <summary>One-line description shown at the top of the page.</summary>
        public string Intro { get; }

        /// <summary>Short description shown next to the link on the root overview page.</summary>
        public string Description { get; }

        public SettingsPageAttribute(int order, string label, string intro, string description = null)
        {
            Order = order;
            Label = label;
            Intro = intro;
            Description = description;
        }
    }

    /// <summary>
    /// Marks a field within a settings class as a section heading. Unlike Unity's built-in
    /// <c>[Header]</c> (which extends <see cref="PropertyAttribute"/> and is therefore rendered
    /// as a decorator by <see cref="EditorGUILayout.PropertyField"/>), this attribute is a plain
    /// <see cref="Attribute"/> that Unity's Inspector does not know about. The
    /// <see cref="SettingsRenderer"/> handles it explicitly before every field, ensuring consistent
    /// heading style regardless of whether the field is a list or a scalar.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class SettingsSectionAttribute : Attribute
    {
        public string Header { get; }
        public SettingsSectionAttribute(string header) => Header = header;
    }

    /// <summary>
    /// Applied to a <c>bool</c> field. While the field value is <c>true</c>, the renderer draws
    /// a warning help box directly beneath the field's toggle.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class SettingsConditionalWarningAttribute : Attribute
    {
        public string Message { get; }
        public SettingsConditionalWarningAttribute(string message) => Message = message;
    }

    /// <summary>
    /// Applied to a settings <see cref="ScriptableObject"/> class. Text is shown at the top of
    /// the auto-generated root overview page produced by
    /// <see cref="SettingsRenderer.CreateProviders"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class SettingsIntroAttribute : Attribute
    {
        public string Text { get; }
        public SettingsIntroAttribute(string text) => Text = text;
    }

    /// <summary>
    /// Applied to a string-returning property on a settings <see cref="ScriptableObject"/>.
    /// The renderer shows the value in the "Status" section of the root overview page.
    /// Multiple properties are shown in declaration order.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class SettingsStatusAttribute : Attribute { }

    /// <summary>
    /// Applied to a parameterless instance method on a settings <see cref="ScriptableObject"/>.
    /// The renderer draws a button with the given label in the "Actions" section of the root
    /// overview page. Use <see cref="Order"/> to control button order within the row.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class SettingsActionAttribute : Attribute
    {
        public string Label { get; }
        public int Order { get; }

        public SettingsActionAttribute(string label, int order = 0)
        {
            Label = label;
            Order = order;
        }
    }

    /// <summary>Reflected metadata for a single serializable field on a settings object.</summary>
    public sealed class SettingsField
    {
        internal SettingsField(SerializedProperty property, FieldInfo fieldInfo)
        {
            Property = property;
            FieldInfo = fieldInfo;
        }

        public SerializedProperty Property { get; }
        public FieldInfo FieldInfo { get; }
        public string Name => FieldInfo.Name;
        public string Tooltip => FieldInfo.GetCustomAttribute<TooltipAttribute>()?.tooltip ?? string.Empty;
        public bool IsStringList => Property.isArray && Property.arrayElementType == "string";
        public bool IsExclusionList => FieldInfo.FieldType == typeof(Types.ExclusionList);
    }

    /// <summary>Per-render callbacks for custom widgets that live between fields.</summary>
    public sealed class SettingsRenderOptions
    {
        /// <summary>Invoked after each field is drawn. Inspect <see cref="SettingsField.Name"/>
        /// to target a specific field.</summary>
        public Action<SettingsField> AfterField { get; set; }
    }

    /// <summary>
    /// Layout values used by <see cref="SettingsRenderer"/>. Custom renderers can provide a
    /// different implementation when they need different spacing without hard-coded literals.
    /// </summary>
    public interface ISettingsRendererLayout
    {
        float PageLeftPadding { get; }
        float PageRightPadding { get; }
        float PageTopSpacing { get; }
        float PageBottomSpacing { get; }
        float SectionTopSpacing { get; }
        float RelatedFieldSpacing { get; }
        float ActionButtonHeight { get; }
        float FieldLabelWidth { get; }
        float LineListDefaultMinLines { get; }
        float LineListHeightPadding { get; }
    }

    /// <summary>Default Release Guard settings layout.</summary>
    // ReSharper disable MemberCanBePrivate.Global
    public sealed class DefaultSettingsRendererLayout : ISettingsRendererLayout
    {
        public const float DefaultPageLeftPadding = 10f;
        public const float DefaultPageRightPadding = 4f;
        public const float DefaultPageTopSpacing = 4f;
        public const float DefaultPageBottomSpacing = 12f;
        public const float DefaultSectionTopSpacing = 14f;
        public const float DefaultRelatedFieldSpacing = 8f;
        public const float DefaultActionButtonHeight = 24f;
        public const float DefaultFieldLabelWidth = 220f;
        public const float DefaultLineListMinLines = 3f;
        public const float DefaultLineListHeightPadding = 6f;

        public static readonly DefaultSettingsRendererLayout Instance = new();

        private DefaultSettingsRendererLayout() { }

        public float PageLeftPadding => DefaultPageLeftPadding;
        public float PageRightPadding => DefaultPageRightPadding;
        public float PageTopSpacing => DefaultPageTopSpacing;
        public float PageBottomSpacing => DefaultPageBottomSpacing;
        public float SectionTopSpacing => DefaultSectionTopSpacing;
        public float RelatedFieldSpacing => DefaultRelatedFieldSpacing;
        public float ActionButtonHeight => DefaultActionButtonHeight;
        public float FieldLabelWidth => DefaultFieldLabelWidth;
        public float LineListDefaultMinLines => DefaultLineListMinLines;
        public float LineListHeightPadding => DefaultLineListHeightPadding;
    }
    // ReSharper restore MemberCanBePrivate.Global

    /// <summary>
    /// Contract for a settings renderer. The primary implementation is <see cref="SettingsRenderer"/>;
    /// subclass it to customize field rendering. Implement this interface directly only when you
    /// need complete IMGUI control with no base-class helpers.
    /// </summary>
    public interface ISettingsRenderer
    {
        /// <summary>
        /// Render the settings object's IMGUI. Called inside Unity's Project Settings
        /// <c>guiHandler</c> on every repaint. Implementations must call
        /// <see cref="UnityEditor.SerializedObject.Update"/> and
        /// <see cref="UnityEditor.SerializedObject.ApplyModifiedProperties"/> if they use
        /// the serialized-object API.
        /// </summary>
        void Draw(UnityEngine.Object target);

        /// <summary>
        /// Manages per-type field rendering. Register a custom <see cref="ITypeRenderer"/> via
        /// <c>ComponentRenderer.TypeRenderers.Register(typeof(MyType), new MyRenderer())</c> to
        /// control how a specific C# field type is drawn without subclassing the full renderer.
        /// </summary>
        SettingsComponentRenderer ComponentRenderer { get; }
    }

    /// <summary>
    /// Canonical IMGUI renderer for Release Guard settings objects. Inherits layout and
    /// list-field helpers from <see cref="SettingsRenderPrimitives"/> and delegates per-field
    /// type dispatch to <see cref="SettingsComponentRenderer"/>. Subclass to customize the
    /// overview page or individual field rendering.
    /// </summary>
    public class SettingsRenderer : SettingsRenderPrimitives, ISettingsRenderer
    {
        private readonly ExclusionListRenderer _exclusionListRenderer = new();

        public SettingsRenderer(ISettingsRendererLayout layout = null) : base(layout)
        {
            ComponentRenderer = new SettingsComponentRenderer();
        }

        /// <summary>Shared instance — used by Release Guard's built-in settings pages.</summary>
        public static SettingsRenderer Default { get; } = new();

        /// <inheritdoc/>
        public SettingsComponentRenderer ComponentRenderer { get; }

        // ------------------------------------------------------------------
        // Provider tree generation
        // ------------------------------------------------------------------

        /// <summary>
        /// Generates a <see cref="SettingsProvider"/> for the root overview page and one for each
        /// field on <paramref name="settings"/> annotated with <see cref="SettingsPageAttribute"/>,
        /// ordered by <see cref="SettingsPageAttribute.Order"/>.
        /// </summary>
        public IEnumerable<SettingsProvider> CreateProviders(
            string rootPath,
            ScriptableObject settings)
        {
            var pages = SettingsReflection.DiscoverPages(settings.GetType());
            yield return CreateOverviewProvider(rootPath, settings, pages);
            foreach (var (field, attr) in pages)
                yield return CreateLeafProvider(rootPath, settings, field, attr);
        }

        private SettingsProvider CreateOverviewProvider(
            string rootPath,
            ScriptableObject settings,
            IReadOnlyList<(FieldInfo field, SettingsPageAttribute attr)> pages)
        {
            var renderer = this;
            var capturedRoot = rootPath;
            var capturedSettings = settings;
            var capturedPages = pages;

            return new SettingsProvider(rootPath, SettingsScope.Project)
            {
                label = rootPath.Split('/').Last(),
                guiHandler = _ => renderer.DrawPaddedContent(
                    () => renderer.DrawOverview(capturedRoot, capturedSettings, capturedPages)),
                keywords = CreateOverviewKeywords()
            };
        }

        /// <summary>
        /// Draws the content of the root overview page. Override to provide a custom layout.
        /// The default implementation reads <see cref="SettingsIntroAttribute"/>,
        /// <see cref="SettingsStatusAttribute"/>, and <see cref="SettingsActionAttribute"/> from
        /// the settings type and draws intro text, a status section, section links, and an actions
        /// row.
        /// </summary>
        protected virtual void DrawOverview(
            string rootPath,
            ScriptableObject settings,
            IReadOnlyList<(FieldInfo field, SettingsPageAttribute attr)> pages)
        {
            var type = settings.GetType();
            var introText = type.GetCustomAttribute<SettingsIntroAttribute>()?.Text;
            var statusProps = SettingsReflection.DiscoverStatusProperties(type);
            var actionMethods = SettingsReflection.DiscoverActionMethods(type);

            if (!string.IsNullOrEmpty(introText))
                Intro(introText);

            if (statusProps.Count > 0)
            {
                Section("Status");
                foreach (var prop in statusProps)
                    Label(prop.GetValue(settings)?.ToString() ?? string.Empty);
            }

            Section("Sections");
            foreach (var (_, attr) in pages)
                SectionLink(attr.Label, PagePath(rootPath, attr), attr.Description ?? string.Empty);

            if (actionMethods.Count > 0)
            {
                Section("Actions");
                Row(() =>
                {
                    foreach (var (method, attr) in actionMethods)
                    {
                        var capturedMethod = method;
                        ActionButton(attr.Label, () => capturedMethod.Invoke(settings, null));
                    }
                });
            }
        }

        /// <summary>
        /// Search keywords for the root overview page. Override to add domain-specific terms.
        /// </summary>
        protected virtual HashSet<string> CreateOverviewKeywords() => new();

        private SettingsProvider CreateLeafProvider(
            string rootPath,
            ScriptableObject settings,
            FieldInfo field,
            SettingsPageAttribute attr)
        {
            var fieldName = field.Name;
            var path = PagePath(rootPath, attr);
            var renderer = this;

            return new SettingsProvider(path, SettingsScope.Project)
            {
                label = attr.Label,
                guiHandler = _ =>
                {
                    if (settings == null) return;
                    var so = new SerializedObject(settings);
                    so.Update();
                    renderer.DrawPaddedContent(() =>
                    {
                        if (!string.IsNullOrEmpty(attr.Intro))
                            renderer.Intro(attr.Intro);
                        renderer.Render(so.FindProperty(fieldName));
                    });
                    if (so.ApplyModifiedProperties())
                        renderer.OnSettingsChanged();
                }
            };
        }

        protected static string PagePath(string rootPath, SettingsPageAttribute attr) =>
            $"{rootPath}/{attr.Order} {attr.Label}";

        // ------------------------------------------------------------------
        // Rendering
        // ------------------------------------------------------------------

        /// <summary>
        /// Renders all visible serialized properties of <paramref name="target"/>.
        /// </summary>
        public void Draw(UnityEngine.Object target)
        {
            if (target is null)
                return;
            var serialized = new SerializedObject(target);
            serialized.Update();
            DrawSerializedObject(serialized);
            if (serialized.ApplyModifiedProperties())
                OnSettingsChanged();
        }

        public virtual void DrawSerializedObject(SerializedObject so) => Render(so);

        /// <summary>
        /// Called after <see cref="Draw"/> applies modified properties. Default: reloads the
        /// Release Guard environment. Override to customize or suppress.
        /// </summary>
        protected virtual void OnSettingsChanged() => ReleaseGuardStartup.Reload();

        /// <summary>Renders all serializable fields of <paramref name="so"/>.</summary>
        protected void Render(SerializedObject so, SettingsRenderOptions options = null)
        {
            if (so is null) return;
            foreach (var field in SettingsReflection.ReadFields(so.targetObject.GetType(), so.FindProperty))
                RenderField(field, options);
        }

        /// <summary>
        /// Renders all serializable fields of the sub-object at <paramref name="prop"/>.
        /// </summary>
        protected void Render(SerializedProperty prop, SettingsRenderOptions options = null)
        {
            if (prop is null) return;
            var parentField = SettingsReflection.FieldForProperty(
                prop.serializedObject.targetObject.GetType(),
                prop.propertyPath);
            if (parentField is null) return;
            foreach (var field in SettingsReflection.ReadFields(parentField.FieldType, prop.FindPropertyRelative))
                RenderField(field, options);
        }

        private void RenderField(SettingsField field, SettingsRenderOptions options) =>
            ComponentRenderer.RenderField(field, this, options);

        // ------------------------------------------------------------------
        // Exclusion list field
        // Delegates list rendering to the inherited RenderLineList helper and
        // preview state to ExclusionListRenderer.
        // ------------------------------------------------------------------

        /// <summary>
        /// Draws an <see cref="Types.ExclusionList"/> field: a multiline pattern text area
        /// followed by a collapsible "Preview matching assets" foldout.
        /// </summary>
        public void ExclusionListField(SettingsField field)
        {
            var patternsProp = field.Property.FindPropertyRelative("patterns");
            RenderLineList(patternsProp, field.Property.displayName, field.Tooltip,
                Layout.LineListDefaultMinLines);
            _exclusionListRenderer.DrawPreview(patternsProp);
        }

        /// <summary>
        /// Draws a standalone "Preview matching assets" foldout for the given pattern-array
        /// property. Useful when building custom exclusion list widgets.
        /// </summary>
        public void ExclusionPreview(SerializedProperty patternsProp) =>
            _exclusionListRenderer.DrawPreview(patternsProp);
    }
}
