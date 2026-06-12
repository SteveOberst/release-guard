using System;
using System.Collections.Generic;
using System.Reflection;
using ReleaseGuard.Editor.Core.Config.Reader;
using ReleaseGuard.Editor.Core.Config.Renderer;
using UnityEditor;
using UnityEngine;

namespace ReleaseGuard.Editor.Core.Config.Components
{
    public sealed class ScreenComponent : ContainerComponent
    {
        public string Path { get; init; }
        public string Description { get; init; }
        public string Intro { get; init; }
        public FieldInfo FieldInfo { get; init; }

        internal Func<ScriptableObject, SettingsRenderer, IEnumerable<SettingsProvider>>
            DynamicProviderResolver { get; init; }

        public override void Render(SettingsRenderer renderer)
        {
            RenderPrimitives.SectionLink(DisplayName, Path, Description);
        }

        internal SettingsProvider CreateProvider(
            ScriptableObject settings,
            SettingsRenderer renderer,
            HashSet<string> keywords = null)
        {
            var fieldName = FieldInfo?.Name;
            var capturedIntro = Intro;
            var capturedChildren = Children;

            return new SettingsProvider(Path, SettingsScope.Project)
            {
                label = DisplayName,
                keywords = keywords ?? new HashSet<string>(),
                guiHandler = _ =>
                {
                    if (settings == null) return;
                    var so = new SerializedObject(settings);
                    so.Update();
                    renderer.DrawPaddedContent(() =>
                    {
                        if (!string.IsNullOrEmpty(capturedIntro))
                            RenderPrimitives.Intro(capturedIntro);
                        Func<string, SerializedProperty> findProp = fieldName != null
                            ? name => so.FindProperty($"{fieldName}.{name}")
                            : name => so.FindProperty(name);
                        SettingsComponentReader.BindProperties(capturedChildren, findProp);
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
                if (child is not ScreenComponent screen) continue;
                foreach (var p in screen.CreateProviders(settings, renderer))
                    yield return p;
            }

            if (DynamicProviderResolver == null) yield break;
            {
                foreach (var p in DynamicProviderResolver(settings, renderer))
                    yield return p;
            }
        }
    }
}