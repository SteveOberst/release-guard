using ReleaseGuard.Editor.Config;
using ReleaseGuard.Editor.Core.Config;

namespace ReleaseGuard.Editor.Core.Plugins
{
    /// <summary>
    /// Base class for plugin settings assets. Subclass this to declare your plugin's
    /// configurable fields. The framework stores one asset per plugin at
    /// Assets/ReleaseGuard/Plugins/{pluginId}.asset and generates a Project Settings
    /// sub-page for it automatically.
    ///
    /// <para><b>Canonical rendering:</b> by default the framework renders every visible
    /// serialized property using the same field and list widgets as the built-in Release Guard
    /// pages. Override <see cref="Renderer"/> to supply a custom <see cref="ISettingsRenderer"/>
    /// when the default is not expressive enough.
    /// For simple customization (intro text, custom section order), subclass
    /// <see cref="SettingsRenderer"/> and override
    /// <see cref="SettingsRenderer.DrawSerializedObject"/>. For full control, implement
    /// <see cref="ISettingsRenderer"/> directly.</para>
    ///
    /// <para><b>List fields:</b> <c>List&lt;string&gt;</c> fields are rendered as multiline
    /// text areas (one entry per line). <c>ExclusionList</c> fields additionally show a live
    /// "Preview matching assets" foldout. All other serialized types get a standard
    /// PropertyField.</para>
    ///
    /// <para><b>Section headings:</b> use <c>[SettingsSection("Heading")]</c> on fields to add
    /// bold section headings that work consistently for all field types.</para>
    /// </summary>
    public abstract class ReleaseGuardPluginSettings : UnityEngine.ScriptableObject
    {
        /// <summary>
        /// Optional custom renderer for this settings object. Return <c>null</c> to use
        /// <see cref="SettingsRenderer.Default"/>, which renders all serialized fields with the
        /// same canonical widgets as Release Guard's built-in settings pages.
        /// </summary>
        public virtual ISettingsRenderer Renderer => null;
    }
}