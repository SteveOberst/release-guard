using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ReleaseGuard.Editor.Core.Config
{
    public sealed class ScreenComponent : ContainerComponent
    {
        public string    Path        { get; init; }
        public string    Description { get; init; }
        public string    Intro       { get; init; }
        public FieldInfo FieldInfo   { get; init; }

        internal Func<ScriptableObject, SettingsRenderer, IEnumerable<SettingsProvider>>
            DynamicProviderResolver { get; init; }

        public override void Render(SettingsRenderer renderer)
            => SettingsRenderPrimitives.SectionLink(DisplayName, Path, Description);

        internal SettingsProvider CreateProvider(
            ScriptableObject settings,
            SettingsRenderer renderer,
            HashSet<string> keywords = null)
        {
            var fieldName        = FieldInfo?.Name;
            var capturedPath     = Path;
            var capturedLabel    = DisplayName;
            var capturedIntro    = Intro;
            var capturedChildren = Children;

            return new SettingsProvider(capturedPath, SettingsScope.Project)
            {
                label      = capturedLabel,
                keywords   = keywords,
                guiHandler = _ =>
                {
                    if (settings == null) return;
                    var so = new SerializedObject(settings);
                    so.Update();
                    renderer.DrawPaddedContent(() =>
                    {
                        if (!string.IsNullOrEmpty(capturedIntro))
                            renderer.Intro(capturedIntro);
                        Func<string, SerializedProperty> findProp = fieldName != null
                            ? name => so.FindProperty($"{fieldName}.{name}")
                            : name => so.FindProperty(name);
                        renderer.ComponentRenderer.ComponentReader.BindProperties(capturedChildren, findProp);
                        foreach (var child in capturedChildren)
                            child.Render(renderer);
                    });
                    if (so.ApplyModifiedProperties())
                        renderer.OnSettingsChanged();
                }
            };
        }

        internal IEnumerable<SettingsProvider> CreateProviders(
            ScriptableObject settings,
            SettingsRenderer renderer)
        {
            yield return CreateProvider(settings, renderer);
            foreach (var child in Children)
            {
                if (child is ScreenComponent screen)
                    foreach (var p in screen.CreateProviders(settings, renderer))
                        yield return p;
            }
            if (DynamicProviderResolver != null)
                foreach (var p in DynamicProviderResolver(settings, renderer))
                    yield return p;
        }
    }
}
