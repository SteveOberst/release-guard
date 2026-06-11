namespace ReleaseGuard.Editor.Core.Config
{
    /// <summary>
    /// Renders a single settings field of a specific C# type in the Project Settings IMGUI.
    ///
    /// <para>Built-in implementations handle <see cref="Config.Types.ExclusionList"/> and
    /// <c>List&lt;string&gt;</c> fields. Register a custom renderer via
    /// <see cref="ISettingsRenderer.ComponentRenderer"/> to handle your own field types:</para>
    /// <code>
    /// // In a SettingsRenderer subclass constructor:
    /// ComponentRenderer.TypeRenderers.Register(typeof(MyCustomType), new MyTypeRenderer());
    /// </code>
    /// </summary>
    public interface ITypeRenderer
    {
        /// <summary>Draw the field IMGUI using <paramref name="renderer"/> as the layout host.</summary>
        void Render(SettingsField field, SettingsRenderer renderer);
    }
}
