using System;
using System.Collections.Generic;
using System.Reflection;
using ReleaseGuard.Editor.Core.Config.Attributes;
using ReleaseGuard.Editor.Core.Config.Components;
using ReleaseGuard.Editor.Core.Config.Types;
using UnityEditor;
using UnityEngine;

namespace ReleaseGuard.Editor.Core.Config.Reader
{
    internal static class BuiltinComponents
    {
        internal static void RegisterAll(SettingsComponentReader reader)
        {
            reader.RegisterReader(new DynamicContainerFieldReader());
            reader.RegisterReader(new ContainerFieldReader());
            reader.RegisterReader(new ExclusionListFieldReader());
            reader.RegisterReader(new StringListFieldReader());
            reader.RegisterReader(new InlineComponentFieldReader());
            reader.RegisterReader(new BoolFieldReader());
            reader.RegisterReader(new IntFieldReader());
            reader.RegisterReader(new FloatFieldReader());
            reader.RegisterReader(new StringFieldReader());
            reader.RegisterReader(new EnumFieldReader());
            reader.RegisterReader(new GenericFieldReader());
            reader.RegisterReader(new SectionHeaderReader());
            reader.RegisterReader(new ConditionalWarningReader());
        }

        private static string Label(FieldInfo fi) =>
            ObjectNames.NicifyVariableName(fi.Name);

        private static string Tip(FieldInfo fi)
        {
            return fi.GetCustomAttribute<TooltipAttribute>()?.tooltip ?? string.Empty;
        }

        private static PrimitiveComponent Primitive(FieldInfo fi)
        {
            return new PrimitiveComponent
            {
                DisplayName = Label(fi),
                Tooltip = Tip(fi),
                FieldInfo = fi
            };
        }

        // ---------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------

        private static SettingsContainer GetContainerAttr(Type type, string callerName)
        {
            var attr = type.GetCustomAttribute<SettingsContainer>(false);
            if (attr == null)
                throw new InvalidOperationException(
                    $"{type.Name} implements ISettingsContainer but is missing " +
                    "[SettingsContainer] or [SettingsPage]. " +
                    $"(required by {callerName})");
            return attr;
        }

        // ---------------------------------------------------------------
        // Primary readers -- field types
        // ---------------------------------------------------------------

        private sealed class DynamicContainerFieldReader : IComponentReader
        {
            public ComponentReadOrder Order => ComponentReadOrder.Primary;
            public int Priority => 10;

            public bool CanRead(object source)
            {
                return source is FieldInfo fi &&
                       typeof(IDynamicSettingsPage).IsAssignableFrom(fi.FieldType);
            }

            public IEnumerable<SettingsComponent> Read(object source, ReadContext context)
            {
                var fi = (FieldInfo)source;
                var type = fi.FieldType;
                var attr = GetContainerAttr(type, nameof(IDynamicSettingsPage));
                var childPath = $"{context.ParentPath}/{context.ContainerIndex} {attr.Label}";
                var children = context.Reader.ReadChildren(type, childPath);

                var capturedFi = fi;
                var capturedReader = context.Reader;

                yield return new ScreenComponent
                {
                    DisplayName = attr.Label,
                    Description = attr.Description,
                    Intro = (attr as SettingsPage)?.Intro ?? string.Empty,
                    Path = childPath,
                    FieldInfo = fi,
                    Children = children,
                    DynamicProviderResolver = (parentInstance, renderer) =>
                    {
                        var container = (IDynamicSettingsPage)capturedFi.GetValue(parentInstance);
                        return container.ResolveChildren(capturedReader, childPath, renderer);
                    }
                };
            }
        }

        private sealed class ContainerFieldReader : IComponentReader
        {
            public ComponentReadOrder Order => ComponentReadOrder.Primary;
            public int Priority => 20;

            public bool CanRead(object source)
            {
                return source is FieldInfo fi &&
                       typeof(ISettingsContainer).IsAssignableFrom(fi.FieldType) &&
                       !typeof(IDynamicSettingsPage).IsAssignableFrom(fi.FieldType);
            }

            public IEnumerable<SettingsComponent> Read(object source, ReadContext context)
            {
                var fi = (FieldInfo)source;
                var type = fi.FieldType;
                var attr = GetContainerAttr(type, type.Name);

                if (typeof(ISettingsPage).IsAssignableFrom(type))
                {
                    var childPath = $"{context.ParentPath}/{context.ContainerIndex} {attr.Label}";
                    var children = context.Reader.ReadChildren(type, childPath);
                    yield return new ScreenComponent
                    {
                        DisplayName = attr.Label,
                        Description = attr.Description,
                        Intro = (attr as SettingsPage)?.Intro ?? string.Empty,
                        Path = childPath,
                        FieldInfo = fi,
                        Children = children
                    };
                }
                else
                {
                    var children = context.Reader.ReadChildren(type, context.ParentPath);
                    yield return new SectionGroupComponent
                    {
                        DisplayName = attr.Label,
                        FieldInfo = fi,
                        Children = children
                    };
                }
            }
        }

        private sealed class ExclusionListFieldReader : IComponentReader
        {
            public ComponentReadOrder Order => ComponentReadOrder.Primary;
            public int Priority => 30;

            public bool CanRead(object source)
            {
                return source is FieldInfo fi && fi.FieldType == typeof(ExclusionList);
            }

            public IEnumerable<SettingsComponent> Read(object source, ReadContext context)
            {
                var fi = (FieldInfo)source;
                yield return new ExclusionListComponent
                {
                    DisplayName = Label(fi),
                    Tooltip = Tip(fi),
                    FieldInfo = fi
                };
            }
        }

        private sealed class StringListFieldReader : IComponentReader
        {
            public ComponentReadOrder Order => ComponentReadOrder.Primary;
            public int Priority => 40;

            public bool CanRead(object source)
            {
                return source is FieldInfo fi && fi.FieldType == typeof(List<string>);
            }

            public IEnumerable<SettingsComponent> Read(object source, ReadContext context)
            {
                var fi = (FieldInfo)source;
                yield return new StringListComponent
                {
                    DisplayName = Label(fi),
                    Tooltip = Tip(fi),
                    FieldInfo = fi
                };
            }
        }

        private sealed class InlineComponentFieldReader : IComponentReader
        {
            public ComponentReadOrder Order => ComponentReadOrder.Primary;
            public int Priority => 50;

            public bool CanRead(object source)
            {
                return source is FieldInfo fi &&
                       typeof(SettingsComponent).IsAssignableFrom(fi.FieldType);
            }

            public IEnumerable<SettingsComponent> Read(object source, ReadContext context)
            {
                var fi = (FieldInfo)source;
                if (context.Instance == null) yield break;
                if (fi.GetValue(context.Instance) is SettingsComponent component)
                    yield return component;
            }
        }

        private sealed class BoolFieldReader : IComponentReader
        {
            public ComponentReadOrder Order => ComponentReadOrder.Primary;
            public int Priority => 100;

            public bool CanRead(object source)
            {
                return source is FieldInfo fi && fi.FieldType == typeof(bool);
            }

            public IEnumerable<SettingsComponent> Read(object source, ReadContext context)
            {
                yield return Primitive((FieldInfo)source);
            }
        }

        private sealed class IntFieldReader : IComponentReader
        {
            public ComponentReadOrder Order => ComponentReadOrder.Primary;
            public int Priority => 110;

            public bool CanRead(object source)
            {
                return source is FieldInfo fi && fi.FieldType == typeof(int);
            }

            public IEnumerable<SettingsComponent> Read(object source, ReadContext context)
            {
                yield return Primitive((FieldInfo)source);
            }
        }

        private sealed class FloatFieldReader : IComponentReader
        {
            public ComponentReadOrder Order => ComponentReadOrder.Primary;
            public int Priority => 120;

            public bool CanRead(object source)
            {
                return source is FieldInfo fi && fi.FieldType == typeof(float);
            }

            public IEnumerable<SettingsComponent> Read(object source, ReadContext context)
            {
                yield return Primitive((FieldInfo)source);
            }
        }

        private sealed class StringFieldReader : IComponentReader
        {
            public ComponentReadOrder Order => ComponentReadOrder.Primary;
            public int Priority => 130;

            public bool CanRead(object source)
            {
                return source is FieldInfo fi && fi.FieldType == typeof(string);
            }

            public IEnumerable<SettingsComponent> Read(object source, ReadContext context)
            {
                yield return Primitive((FieldInfo)source);
            }
        }

        private sealed class EnumFieldReader : IComponentReader
        {
            public ComponentReadOrder Order => ComponentReadOrder.Primary;
            public int Priority => 140;

            public bool CanRead(object source)
            {
                return source is FieldInfo fi && fi.FieldType.IsEnum;
            }

            public IEnumerable<SettingsComponent> Read(object source, ReadContext context)
            {
                yield return Primitive((FieldInfo)source);
            }
        }

        private sealed class GenericFieldReader : IComponentReader
        {
            public ComponentReadOrder Order => ComponentReadOrder.Primary;
            public int Priority => int.MaxValue;

            public bool CanRead(object source)
            {
                return source is FieldInfo;
            }

            public IEnumerable<SettingsComponent> Read(object source, ReadContext context)
            {
                var fi = (FieldInfo)source;
                yield return new GenericSerializedComponent
                {
                    DisplayName = Label(fi),
                    Tooltip = Tip(fi),
                    FieldInfo = fi
                };
            }
        }

        // ---------------------------------------------------------------
        // Before/After readers -- attribute types
        // ---------------------------------------------------------------

        private sealed class SectionHeaderReader : IComponentReader
        {
            public ComponentReadOrder Order => ComponentReadOrder.Before;
            public int Priority => 10;

            public bool CanRead(object source)
            {
                return source is SettingsHeader;
            }

            public IEnumerable<SettingsComponent> Read(object source, ReadContext context)
            {
                yield return new SectionHeaderComponent
                {
                    Header = ((SettingsHeader)source).Header
                };
            }
        }

        private sealed class ConditionalWarningReader : IComponentReader
        {
            public ComponentReadOrder Order => ComponentReadOrder.After;
            public int Priority => 10;

            public bool CanRead(object source)
            {
                return source is ConditionalWarning;
            }

            public IEnumerable<SettingsComponent> Read(object source, ReadContext context)
            {
                var attr = (ConditionalWarning)source;
                yield return new ConditionalWarningComponent
                {
                    Message = attr.Message,
                    AssociatedField = context.PrimaryComponent as SerializedFieldComponent
                };
            }
        }
    }
}