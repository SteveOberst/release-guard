using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ReleaseGuard.Editor.Core.Config.Attributes;
using ReleaseGuard.Editor.Core.Config.Components;
using UnityEditor;
using UnityEngine;

namespace ReleaseGuard.Editor.Core.Config.Reader
{
    public sealed class SettingsComponentReader
    {
        private readonly Dictionary<(Type, string), IReadOnlyList<SettingsComponent>> _cache = new();
        private readonly List<IComponentReader> _readers = new();

        public void RegisterReader(IComponentReader reader)
        {
            var idx = _readers.Count;
            for (var i = 0; i < _readers.Count; i++)
            {
                if (CompareReaders(reader, _readers[i]) >= 0) continue;
                idx = i;
                break;
            }

            _readers.Insert(idx, reader);
        }

        private static int CompareReaders(IComponentReader a, IComponentReader b)
        {
            var orderDiff = (int)a.Order - (int)b.Order;
            return orderDiff != 0 ? orderDiff : a.Priority.CompareTo(b.Priority);
        }

        public ScreenComponent Read(object instance, string rootPath, string rootLabel)
        {
            var type = instance.GetType();
            var pageAttr = type.GetCustomAttribute<SettingsPage>();

            var context = new ReadContext
            {
                Reader = this,
                ParentPath = rootPath,
                Instance = instance
            };

            var children = ProcessFields(type, context, true);

            return new ScreenComponent
            {
                DisplayName = rootLabel,
                Path = rootPath,
                Intro = pageAttr?.Intro ?? string.Empty,
                Description = pageAttr?.Description ?? string.Empty,
                Children = children
            };
        }

        internal IReadOnlyList<SettingsComponent> ReadChildren(Type type, string parentPath)
        {
            var key = (type, parentPath);
            if (_cache.TryGetValue(key, out var cached))
                return cached;

            var context = new ReadContext
            {
                Reader = this,
                ParentPath = parentPath
            };

            var result = ProcessFields(type, context, false);
            _cache[key] = result;
            return result;
        }

        private IReadOnlyList<SettingsComponent> ProcessFields(
            Type type, ReadContext context, bool includeNonSerialized)
        {
            var result = new List<SettingsComponent>();
            var containerIndex = 0;

            foreach (var fi in GetFields(type, includeNonSerialized))
            {
                var isContainer = typeof(ISettingsContainer).IsAssignableFrom(fi.FieldType);
                if (isContainer) containerIndex++;

                var fieldCtx = new ReadContext
                {
                    Reader = context.Reader,
                    ParentPath = context.ParentPath,
                    ContainerIndex = isContainer ? containerIndex : 0,
                    Instance = context.Instance
                };

                var attrs = Attribute.GetCustomAttributes(fi);

                // Before pass
                foreach (var attr in attrs)
                foreach (var r in _readers.Where(r => r.Order == ComponentReadOrder.Before && r.CanRead(attr)))
                    result.AddRange(r.Read(attr, fieldCtx));

                // Primary pass
                SettingsComponent primary = null;
                var primaryComponents = from r in _readers
                    where r.Order == ComponentReadOrder.Primary && r.CanRead(fi)
                    select r.Read(fi, fieldCtx).ToList();

                foreach (var produced in primaryComponents)
                {
                    if (produced.Count > 0)
                    {
                        primary = produced[0];
                        result.AddRange(produced);
                    }

                    break;
                }

                // Apply InjectProperty injections -- runs for every reader, builtin or custom.
                if (primary != null)
                    foreach (var injectAttr in attrs.OfType<InjectProperty>())
                        injectAttr.TryApply(primary);

                // After pass (only if there is something to decorate)
                if (attrs.Length <= 0) continue;
                var afterCtx = new ReadContext
                {
                    Reader = context.Reader,
                    ParentPath = context.ParentPath,
                    ContainerIndex = fieldCtx.ContainerIndex,
                    Instance = context.Instance,
                    PrimaryComponent = primary
                };
                foreach (var attr in attrs)
                {
                    var readers = _readers.Where(r => r.Order == ComponentReadOrder.After && r.CanRead(attr));
                    foreach (var r in readers)
                        result.AddRange(r.Read(attr, afterCtx));
                }
            }

            return result;
        }

        public static void BindProperties(
            IReadOnlyList<SettingsComponent> components,
            Func<string, SerializedProperty> findProperty)
        {
            if (components == null) return;
            foreach (var c in components)
                switch (c)
                {
                    case SerializedFieldComponent sfc:
                        sfc.Property = findProperty(sfc.FieldInfo.Name);
                        break;
                    case SectionGroupComponent sgc when sgc.FieldInfo != null && sgc.Children != null:
                        var prefix = sgc.FieldInfo.Name;
                        BindProperties(sgc.Children, name => findProperty($"{prefix}.{name}"));
                        break;
                }
        }

        private static IEnumerable<FieldInfo> GetFields(Type type, bool includeNonSerialized)
        {
            var hierarchy = new Stack<Type>();
            for (var t = type; t != null && t != typeof(object); t = t.BaseType)
                hierarchy.Push(t);

            while (hierarchy.Count > 0)
            {
                var fields = hierarchy.Pop()
                    .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
                               BindingFlags.DeclaredOnly)
                    .OrderBy(f => f.MetadataToken);

                foreach (var f in fields)
                    if (includeNonSerialized)
                    {
                        if (IsSerializableField(f) || IsNonSerializedComponent(f))
                            yield return f;
                    }
                    else
                    {
                        if (IsSerializableField(f))
                            yield return f;
                    }
            }
        }

        private static bool IsSerializableField(FieldInfo f)
        {
            if (f.IsStatic || f.IsInitOnly || f.IsNotSerialized) return false;
            if (f.GetCustomAttribute<HideInInspector>() != null) return false;
            return f.IsPublic || f.GetCustomAttribute<SerializeField>() != null;
        }

        private static bool IsNonSerializedComponent(FieldInfo f)
        {
            return f.IsNotSerialized && typeof(SettingsComponent).IsAssignableFrom(f.FieldType);
        }
    }
}