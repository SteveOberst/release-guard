using System;
using System.Reflection;
using ReleaseGuard.Editor.Core.Registries;
using UnityEditor;

namespace ReleaseGuard.Editor.Core.Config
{
    /// <summary>
    /// Manages per-type field rendering within a settings page. Owns the
    /// <see cref="TypeRenderers"/> registry and handles the full rendering lifecycle for each
    /// individual settings field: section headings, type dispatch (registry lookup →
    /// <see cref="ITypeRenderer"/> or fallback to <c>PropertyField</c>), conditional warnings,
    /// and post-field callbacks.
    ///
    /// <para>Accessed via <see cref="ISettingsRenderer.ComponentRenderer"/>. Register a custom
    /// renderer for your own field types:</para>
    /// <code>
    /// ComponentRenderer.TypeRenderers.Register(typeof(MyType), new MyTypeRenderer());
    /// </code>
    /// </summary>
    public sealed class SettingsComponentRenderer
    {
        private readonly Registry<Type, ITypeRenderer> _typeRenderers;

        public SettingsComponentRenderer()
        {
            _typeRenderers = new Registry<Type, ITypeRenderer>();
            BuiltinTypeRenderers.RegisterAll(_typeRenderers);
        }

        /// <summary>
        /// Per-type field renderers. Register a custom <see cref="ITypeRenderer"/> keyed by the
        /// C# type to control how a specific field type is drawn without subclassing the full
        /// renderer. Built-in entries for <c>ExclusionList</c> and <c>List&lt;string&gt;</c> are
        /// registered as defaults and can be overridden.
        /// </summary>
        public IRegistry<Type, ITypeRenderer> TypeRenderers => _typeRenderers;

        internal void RenderField(SettingsField field, SettingsRenderer renderer, SettingsRenderOptions options)
        {
            var section = field.FieldInfo.GetCustomAttribute<SettingsSectionAttribute>();
            if (section != null)
                renderer.Section(section.Header);

            var typeRenderer = _typeRenderers.Get(field.FieldInfo.FieldType);
            if (typeRenderer != null)
                typeRenderer.Render(field, renderer);
            else
                EditorGUILayout.PropertyField(field.Property, includeChildren: true);

            var warning = field.FieldInfo.GetCustomAttribute<SettingsConditionalWarningAttribute>();
            if (warning != null && field.Property.boolValue)
                renderer.HelpBox(warning.Message, MessageType.Warning);

            options?.AfterField?.Invoke(field);
        }
    }
}
