using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ReleaseGuard.Editor.Core.Config.Attributes;
using ReleaseGuard.Editor.Core.Config.Components;
using ReleaseGuard.Editor.Core.Config.Reader;
using ReleaseGuard.Editor.Core.Config.Types;
using UnityEngine;

namespace ReleaseGuard.Editor.Tests
{
    public sealed class SettingsComponentReaderTests
    {
        private static SettingsComponentReader BuildReader()
        {
            var reader = new SettingsComponentReader();
            BuiltinComponents.RegisterAll(reader);
            return reader;
        }

        private static IReadOnlyList<SettingsComponent> Read(object instance) =>
            BuildReader().Read(instance, "Test", "Test").Children;

        // ---------------------------------------------------------------
        // Built-in type mapping
        // ---------------------------------------------------------------

        [Test]
        public void BoolField_ProducesPrimitiveComponent()
        {
            var children = Read(new OneBool());
            Assert.AreEqual(1, children.Count);
            Assert.IsInstanceOf<PrimitiveComponent>(children[0]);
        }

        [Test]
        public void IntField_ProducesPrimitiveComponent()
        {
            var children = Read(new OneInt());
            Assert.AreEqual(1, children.Count);
            Assert.IsInstanceOf<PrimitiveComponent>(children[0]);
        }

        [Test]
        public void FloatField_ProducesPrimitiveComponent()
        {
            var children = Read(new OneFloat());
            Assert.AreEqual(1, children.Count);
            Assert.IsInstanceOf<PrimitiveComponent>(children[0]);
        }

        [Test]
        public void StringField_ProducesPrimitiveComponent()
        {
            var children = Read(new OneString());
            Assert.AreEqual(1, children.Count);
            Assert.IsInstanceOf<PrimitiveComponent>(children[0]);
        }

        [Test]
        public void EnumField_ProducesPrimitiveComponent()
        {
            var children = Read(new OneEnum());
            Assert.AreEqual(1, children.Count);
            Assert.IsInstanceOf<PrimitiveComponent>(children[0]);
        }

        [Test]
        public void StringListField_ProducesStringListComponent()
        {
            var children = Read(new OneStringList());
            Assert.AreEqual(1, children.Count);
            Assert.IsInstanceOf<StringListComponent>(children[0]);
        }

        [Test]
        public void ExclusionListField_ProducesExclusionListComponent()
        {
            var children = Read(new OneExclusionList());
            Assert.AreEqual(1, children.Count);
            Assert.IsInstanceOf<ExclusionListComponent>(children[0]);
        }

        [Test]
        public void UnknownSerializableType_ProducesGenericSerializedComponent()
        {
            var children = Read(new OneGeneric());
            Assert.AreEqual(1, children.Count);
            Assert.IsInstanceOf<GenericSerializedComponent>(children[0]);
        }

        // ---------------------------------------------------------------
        // Label and Tooltip
        // ---------------------------------------------------------------

        [Test]
        public void DefaultLabel_IsNicifiedFieldName()
        {
            var children = Read(new OneNamed());
            var comp = (SerializedFieldComponent)children[0];
            // "myBoolField" -> "My Bool Field"
            Assert.AreEqual("My Bool Field", comp.DisplayName);
        }

        [Test]
        public void SettingsLabel_OverridesDisplayName()
        {
            var children = Read(new OneLabelOverride());
            var comp = (SerializedFieldComponent)children[0];
            Assert.AreEqual("Custom Label", comp.DisplayName);
        }

        [Test]
        public void SettingsLabel_DoesNotAddExtraComponents()
        {
            var children = Read(new OneLabelOverride());
            Assert.AreEqual(1, children.Count);
        }

        [Test]
        public void Tooltip_IsForwardedToComponent()
        {
            var children = Read(new OneTooltip());
            var comp = (SerializedFieldComponent)children[0];
            Assert.AreEqual("Some tooltip text", comp.Tooltip);
        }

        [Test]
        public void NoTooltip_IsEmptyString()
        {
            var children = Read(new OneBool());
            var comp = (SerializedFieldComponent)children[0];
            Assert.AreEqual(string.Empty, comp.Tooltip);
        }

        // ---------------------------------------------------------------
        // [SettingsHeader] -- Before pass
        // ---------------------------------------------------------------

        [Test]
        public void SettingsHeader_ProducesSectionHeaderBeforeField()
        {
            var children = Read(new OneWithHeader());
            Assert.AreEqual(2, children.Count);
            Assert.IsInstanceOf<SectionHeaderComponent>(children[0]);
            Assert.IsInstanceOf<PrimitiveComponent>(children[1]);
        }

        [Test]
        public void SettingsHeader_SetsHeaderText()
        {
            var children = Read(new OneWithHeader());
            Assert.AreEqual("My Section", ((SectionHeaderComponent)children[0]).Header);
        }

        // ---------------------------------------------------------------
        // [SettingsConditionalWarning] -- After pass
        // ---------------------------------------------------------------

        [Test]
        public void ConditionalWarning_ProducesWarningAfterField()
        {
            var children = Read(new OneWithWarning());
            Assert.AreEqual(2, children.Count);
            Assert.IsInstanceOf<PrimitiveComponent>(children[0]);
            Assert.IsInstanceOf<ConditionalWarningComponent>(children[1]);
        }

        [Test]
        public void ConditionalWarning_SetsMessage()
        {
            var children = Read(new OneWithWarning());
            Assert.AreEqual("This is a warning", ((ConditionalWarningComponent)children[1]).Message);
        }

        [Test]
        public void ConditionalWarning_AssociatesWithPrimaryField()
        {
            var children = Read(new OneWithWarning());
            var primary = (PrimitiveComponent)children[0];
            var warning = (ConditionalWarningComponent)children[1];
            Assert.AreSame(primary, warning.AssociatedField);
        }

        // ---------------------------------------------------------------
        // Field discovery rules
        // ---------------------------------------------------------------

        [Test]
        public void HideInInspectorField_IsExcluded()
        {
            var children = Read(new HiddenField());
            Assert.AreEqual(1, children.Count, "Only the visible field should appear.");
        }

        [Test]
        public void PrivateField_WithoutSerializeField_IsExcluded()
        {
            var children = Read(new PrivateField());
            Assert.AreEqual(1, children.Count, "Only the public field should appear.");
        }

        [Test]
        public void PrivateField_WithSerializeField_IsIncluded()
        {
            var children = Read(new SerializedPrivateField());
            Assert.AreEqual(2, children.Count,
                "Both public and [SerializeField] private should appear.");
        }

        [Test]
        public void NonSerializedInlineComponent_IsIncluded()
        {
            var inst = new NonSerializedInline();
            var children = Read(inst);
            Assert.IsTrue(children.Contains(inst.section),
                "NonSerialized InlineComponent field value must appear in the component list.");
        }

        [Test]
        public void NonSerializedNonComponent_IsExcluded()
        {
            var children = Read(new NonSerializedPrimitive());
            Assert.AreEqual(1, children.Count,
                "[NonSerialized] on a non-SettingsComponent field must exclude it.");
        }

        // ---------------------------------------------------------------
        // Field ordering
        // ---------------------------------------------------------------

        [Test]
        public void Fields_AppearInDeclarationOrder()
        {
            var children = Read(new ThreeFields());
            Assert.AreEqual(3, children.Count);
            Assert.AreEqual("First", children[0].DisplayName);
            Assert.AreEqual("Second", children[1].DisplayName);
            Assert.AreEqual("Third", children[2].DisplayName);
        }

        [Test]
        public void BaseClassFields_AppearBeforeDerivedClassFields()
        {
            var children = Read(new DerivedFields());
            Assert.AreEqual(2, children.Count);
            Assert.AreEqual("Base Field", children[0].DisplayName);
            Assert.AreEqual("Derived Field", children[1].DisplayName);
        }

        // ---------------------------------------------------------------
        // Custom IComponentReader
        // ---------------------------------------------------------------

        [Test]
        public void CustomPrimaryReader_WithLowerPriority_WinsOverBuiltin()
        {
            var reader = new SettingsComponentReader();
            BuiltinComponents.RegisterAll(reader);
            var sentinel = new InlineComponent("custom", _ => { });
            reader.RegisterReader(new PrimaryReader(typeof(bool), priority: 0, sentinel));

            var children = reader.Read(new OneBool(), "Test", "Test").Children;

            Assert.AreEqual(1, children.Count);
            Assert.AreSame(sentinel, children[0],
                "Custom primary reader at priority 0 should win over builtin bool reader at 100.");
        }

        [Test]
        public void BeforeReader_FiresBeforePrimary()
        {
            var reader = new SettingsComponentReader();
            BuiltinComponents.RegisterAll(reader);
            var beforeSentinel = new InlineComponent("before", _ => { });
            reader.RegisterReader(new AttributeReader<TooltipAttribute>(
                ComponentReadOrder.Before, priority: 0, beforeSentinel));

            var children = reader.Read(new OneTooltip(), "Test", "Test").Children;

            Assert.AreEqual(2, children.Count);
            Assert.AreSame(beforeSentinel, children[0]);
            Assert.IsInstanceOf<PrimitiveComponent>(children[1]);
        }

        [Test]
        public void AfterReader_ReceivesPrimaryComponent_InContext()
        {
            SettingsComponent capturedPrimary = null;
            var reader = new SettingsComponentReader();
            BuiltinComponents.RegisterAll(reader);
            reader.RegisterReader(new CapturingReader<TooltipAttribute>(c => capturedPrimary = c));

            reader.Read(new OneTooltip(), "Test", "Test");

            Assert.IsInstanceOf<PrimitiveComponent>(capturedPrimary,
                "After-pass ReadContext.PrimaryComponent must be the primary component produced for the field.");
        }

        // ---------------------------------------------------------------
        // InjectProperty -- TargetComponentType filtering
        // ---------------------------------------------------------------

        [Test]
        public void InjectAttribute_TargetingPrimitive_DoesNotFireOnStringList()
        {
            var children = Read(new PrimitiveTargetOnStringList());
            var comp = children.OfType<StringListComponent>().First();
            Assert.AreNotEqual("Injected", comp.DisplayName,
                "Inject attribute targeting PrimitiveComponent must not mutate a StringListComponent.");
        }

        [Test]
        public void InjectAttribute_TargetingSerializedField_FiresOnAllFieldComponents()
        {
            var children = Read(new SerializedFieldTargetOnStringList());
            var comp = children.OfType<StringListComponent>().First();
            Assert.AreEqual("Injected", comp.DisplayName,
                "Inject attribute targeting SerializedFieldComponent must mutate StringListComponent.");
        }

        // ---------------------------------------------------------------
        // Supporting types -- test settings objects
        // ---------------------------------------------------------------

        private sealed class OneBool
        {
            public bool field;
        }

        private sealed class OneInt
        {
            public int field;
        }

        private sealed class OneFloat
        {
            public float field;
        }

        private sealed class OneString
        {
            public string field;
        }

        private enum SampleEnum
        {
            A,
            B
        }

        private sealed class OneEnum
        {
            public SampleEnum field;
        }

        private sealed class OneStringList
        {
            public List<string> field = new();
        }

        private sealed class OneExclusionList
        {
            public ExclusionList field = new();
        }

        [Serializable]
        private struct SomeStruct
        {
            public int x;
        }

        private sealed class OneGeneric
        {
            public SomeStruct field;
        }

        private sealed class OneNamed
        {
            public bool myBoolField;
        }

        private sealed class OneLabelOverride
        {
            [SettingsLabel("Custom Label")] public bool field;
        }

        private sealed class OneTooltip
        {
            [Tooltip("Some tooltip text")] public bool field;
        }

        private sealed class OneWithHeader
        {
            [SettingsHeader("My Section")] public bool field;
        }

        private sealed class OneWithWarning
        {
            [ConditionalWarning("This is a warning")]
            public bool field;
        }

        private sealed class HiddenField
        {
            [HideInInspector] public bool hidden;
            public bool visible;
        }

#pragma warning disable 0169
        private sealed class PrivateField
        {
            private bool privateField;
            public bool publicField;
        }
#pragma warning restore 0169

        private sealed class SerializedPrivateField
        {
            [SerializeField] private bool serializedPrivate;
            public bool publicField;
        }

        private sealed class NonSerializedInline
        {
            [NonSerialized] public InlineComponent section;
            public bool flagField;

            public NonSerializedInline()
            {
                section = new InlineComponent("Section", _ => { });
            }
        }

        private sealed class NonSerializedPrimitive
        {
            [NonSerialized] public bool nonSerializedBool;
            public bool normalField;
        }

        private sealed class ThreeFields
        {
            [SettingsLabel("First")] public bool first;
            [SettingsLabel("Second")] public bool second;
            [SettingsLabel("Third")] public bool third;
        }

        private class BaseFields
        {
            [SettingsLabel("Base Field")] public bool baseField;
        }

        private sealed class DerivedFields : BaseFields
        {
            [SettingsLabel("Derived Field")] public bool derivedField;
        }

        // Inject attribute targeting PrimitiveComponent -- should not fire on StringListComponent
        [AttributeUsage(AttributeTargets.Field)]
        private sealed class PrimitiveTargetInject : InjectProperty
        {
            protected override Type TargetComponentType => typeof(PrimitiveComponent);

            protected override void Apply(SettingsComponent component) =>
                component.DisplayName = "Injected";
        }

        // Inject attribute targeting SerializedFieldComponent -- should fire on StringListComponent
        [AttributeUsage(AttributeTargets.Field)]
        private sealed class SerializedFieldTargetInject : InjectProperty
        {
            protected override Type TargetComponentType => typeof(SerializedFieldComponent);

            protected override void Apply(SettingsComponent component) =>
                component.DisplayName = "Injected";
        }

        private sealed class PrimitiveTargetOnStringList
        {
            [PrimitiveTargetInject] public List<string> field = new();
        }

        private sealed class SerializedFieldTargetOnStringList
        {
            [SerializedFieldTargetInject] public List<string> field = new();
        }

        // ---------------------------------------------------------------
        // Supporting types -- custom readers
        // ---------------------------------------------------------------

        private sealed class PrimaryReader : IComponentReader
        {
            private readonly Type _type;
            private readonly SettingsComponent _returns;
            public ComponentReadOrder Order => ComponentReadOrder.Primary;
            public int Priority { get; }

            public PrimaryReader(Type type, int priority, SettingsComponent returns)
            {
                _type = type;
                Priority = priority;
                _returns = returns;
            }

            public bool CanRead(object source) =>
                source is FieldInfo fi && fi.FieldType == _type;

            public IEnumerable<SettingsComponent> Read(object source, ReadContext context)
            {
                yield return _returns;
            }
        }

        private sealed class AttributeReader<TAttr> : IComponentReader where TAttr : Attribute
        {
            private readonly SettingsComponent _returns;
            public ComponentReadOrder Order { get; }
            public int Priority { get; }

            public AttributeReader(ComponentReadOrder order, int priority, SettingsComponent returns)
            {
                Order = order;
                Priority = priority;
                _returns = returns;
            }

            public bool CanRead(object source) => source is TAttr;

            public IEnumerable<SettingsComponent> Read(object source, ReadContext context)
            {
                yield return _returns;
            }
        }

        private sealed class CapturingReader<TAttr> : IComponentReader where TAttr : Attribute
        {
            private readonly Action<SettingsComponent> _onRead;
            public ComponentReadOrder Order => ComponentReadOrder.After;
            public int Priority => 0;

            public CapturingReader(Action<SettingsComponent> onRead) => _onRead = onRead;

            public bool CanRead(object source) => source is TAttr;

            public IEnumerable<SettingsComponent> Read(object source, ReadContext context)
            {
                _onRead(context.PrimaryComponent);
                yield break;
            }
        }
    }
}