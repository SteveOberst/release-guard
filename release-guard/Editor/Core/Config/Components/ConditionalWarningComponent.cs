using UnityEditor;

namespace ReleaseGuard.Editor.Core.Config
{
    public sealed class ConditionalWarningComponent : SettingsComponent
    {
        public string                   Message        { get; init; }
        public SerializedFieldComponent AssociatedField { get; init; }

        public override void Render(SettingsRenderer renderer)
        {
            if (AssociatedField?.Property?.boolValue == true)
                renderer.HelpBox(Message, MessageType.Warning);
        }
    }
}
