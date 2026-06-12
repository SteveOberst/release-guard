using System;
using ReleaseGuard.Editor.Core.Config.Components;

namespace ReleaseGuard.Editor.Core.Config.Attributes
{
    /// <summary>
    /// Overrides the display label for a settings field. Without this attribute the label
    /// falls back to <see cref="UnityEditor.ObjectNames.NicifyVariableName"/> applied to the
    /// field name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class SettingsLabelAttribute : InjectPropertyAttribute
    {
        // ReSharper disable once MemberCanBePrivate.Global
        public string Label { get; }
        public SettingsLabelAttribute(string label) => Label = label;

        protected override Type TargetComponentType => typeof(SerializedFieldComponent);

        protected override void Apply(SettingsComponent component)
            => component.DisplayName = Label;
    }
}
