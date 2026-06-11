using System;
using System.Collections.Generic;
using ReleaseGuard.Editor.Core.Config.Types;
using ReleaseGuard.Editor.Core.Registries;

namespace ReleaseGuard.Editor.Core.Config
{
    internal static class BuiltinTypeRenderers
    {
        internal static void RegisterAll(Registry<Type, ITypeRenderer> registry)
        {
            registry.RegisterDefault(typeof(ExclusionList), new ExclusionListTypeRenderer());
            registry.RegisterDefault(typeof(List<string>), new StringListTypeRenderer());
        }

        private sealed class ExclusionListTypeRenderer : ITypeRenderer
        {
            public void Render(SettingsField field, SettingsRenderer renderer) =>
                renderer.ExclusionListField(field);
        }

        private sealed class StringListTypeRenderer : ITypeRenderer
        {
            public void Render(SettingsField field, SettingsRenderer renderer) =>
                renderer.LineListField(field.Property, field.Tooltip);
        }
    }
}
