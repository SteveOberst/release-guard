using System;
using ReleaseGuard.Editor.Core.Config.Renderer;

namespace ReleaseGuard.Editor.Core.Config.Components
{
    public sealed class InlineComponent : SettingsComponent
    {
        private readonly Action<SettingsRenderer> _draw;

        public InlineComponent(string displayName, Action<SettingsRenderer> draw)
        {
            DisplayName = displayName;
            _draw = draw;
        }

        public override void Render(SettingsRenderer renderer)
        {
            _draw?.Invoke(renderer);
        }
    }
}