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
            DynamicProviderResolver { get; set; }

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

            var so = settings != null ? new SerializedObject(settings) : null;

            return new SettingsProvider(Path, SettingsScope.Project)
            {
                label = DisplayName,
                keywords = keywords ?? new HashSet<string>(),
                guiHandler = _ =>
                {
                    if (settings == null || so == null) return;
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

        internal SettingsProvider CreateProvider(
            Func<ScriptableObject> settingsGetter,
            SettingsRenderer renderer,
            HashSet<string> keywords = null)
        {
            var fieldName = FieldInfo?.Name;
            var capturedIntro = Intro;
            var capturedChildren = Children;

            ScriptableObject lastSettings = null;
            SerializedObject cachedSo = null;

            return new SettingsProvider(Path, SettingsScope.Project)
            {
                label = DisplayName,
                keywords = keywords ?? new HashSet<string>(),
                guiHandler = _ =>
                {
                    var settings = settingsGetter();
                    if (settings == null) return;
                    if (settings != lastSettings)
                    {
                        cachedSo?.Dispose();
                        lastSettings = settings;
                        cachedSo = new SerializedObject(settings);
                    }

                    cachedSo.Update();
                    renderer.DrawPaddedContent(() =>
                    {
                        if (!string.IsNullOrEmpty(capturedIntro))
                            RenderPrimitives.Intro(capturedIntro);
                        Func<string, SerializedProperty> findProp = fieldName != null
                            ? name => cachedSo.FindProperty($"{fieldName}.{name}")
                            : name => cachedSo.FindProperty(name);
                        SettingsComponentReader.BindProperties(capturedChildren, findProp);
                        foreach (var child in capturedChildren)
                            child.Render(renderer);
                    });
                    if (cachedSo.ApplyModifiedProperties())
                        renderer.OnSettingsChanged();
                }
            };
        }

        internal IEnumerable<SettingsProvider> CreateProviders(
            Func<ScriptableObject> settingsGetter,
            SettingsRenderer renderer)
        {
            yield return CreateProvider(settingsGetter, renderer);
            foreach (var child in Children)
            {
                if (child is not ScreenComponent screen) continue;
                foreach (var p in screen.CreateProviders(settingsGetter, renderer))
                    yield return p;
            }

            if (DynamicProviderResolver == null) yield break;
            var current = settingsGetter();
            if (current != null)
                foreach (var p in DynamicProviderResolver(current, renderer))
                    yield return p;
        }
    }
}