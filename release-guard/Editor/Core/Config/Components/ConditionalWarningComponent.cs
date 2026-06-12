using ReleaseGuard.Editor.Core.Config.Renderer;
using UnityEditor;

namespace ReleaseGuard.Editor.Core.Config.Components
{
    public sealed class ConditionalWarningComponent : SettingsComponent
    {
        public string Message { get; init; }
        public SerializedFieldComponent AssociatedField { get; init; }

        public override void Render(SettingsRenderer renderer)
        {
            if (AssociatedField?.Property?.boolValue == true)
                RenderPrimitives.HelpBox(Message, MessageType.Warning);
        }
    }
}