using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ReleaseGuard.Editor.Core.Config
{
    /// <summary>
    /// Reflection utilities for settings object introspection. Used by
    /// <see cref="SettingsRenderer"/> to discover pages, serializable fields, and decorated
    /// members without keeping static helpers inside the renderer.
    /// </summary>
    internal static class SettingsReflection
    {
        internal static IReadOnlyList<(FieldInfo field, SettingsPageAttribute attr)> DiscoverPages(Type type)
        {
            return type
                .GetFields(BindingFlags.Instance | BindingFlags.Public)
                .Select(f => (field: f, attr: f.GetCustomAttribute<SettingsPageAttribute>()))
                .Where(x => x.attr != null)
                .OrderBy(x => x.attr.Order)
                .ToList();
        }

        internal static IReadOnlyList<PropertyInfo> DiscoverStatusProperties(Type type)
        {
            return type
                .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(p => p.GetCustomAttribute<SettingsStatusAttribute>() != null)
                .ToList();
        }

        internal static IReadOnlyList<(MethodInfo method, SettingsActionAttribute attr)> DiscoverActionMethods(Type type)
        {
            return type
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Select(m => (method: m, attr: m.GetCustomAttribute<SettingsActionAttribute>()))
                .Where(x => x.attr != null)
                .OrderBy(x => x.attr.Order)
                .ToList();
        }

        internal static IEnumerable<SettingsField> ReadFields(
            Type type,
            Func<string, SerializedProperty> findProperty)
        {
            return from field in SerializableFields(type)
                let property = findProperty(field.Name)
                where property is not null
                select new SettingsField(property, field);
        }

        private static IEnumerable<FieldInfo> SerializableFields(Type type)
        {
            var hierarchy = new Stack<Type>();
            for (var current = type; current is not null && current != typeof(object); current = current.BaseType)
                hierarchy.Push(current);

            while (hierarchy.Count > 0)
            {
                foreach (var field in hierarchy.Pop().GetFields(
                             BindingFlags.Instance |
                             BindingFlags.Public |
                             BindingFlags.NonPublic |
                             BindingFlags.DeclaredOnly))
                {
                    if (IsSerializableField(field))
                        yield return field;
                }
            }
        }

        private static bool IsSerializableField(FieldInfo field)
        {
            if (field.IsStatic || field.IsInitOnly || field.IsNotSerialized) return false;
            if (field.GetCustomAttribute<HideInInspector>() is not null) return false;
            return field.IsPublic || field.GetCustomAttribute<SerializeField>() is not null;
        }

        internal static FieldInfo FieldForProperty(Type rootType, string propertyPath)
        {
            var fieldName = propertyPath.Split('.')[0];
            return rootType.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }
    }
}
