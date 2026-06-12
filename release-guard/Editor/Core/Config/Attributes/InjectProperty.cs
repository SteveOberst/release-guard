using System;
using ReleaseGuard.Editor.Core.Config.Components;

namespace ReleaseGuard.Editor.Core.Config.Attributes
{
    /// <summary>
    /// Field-level attribute that mutates the primary <see cref="SettingsComponent"/> produced by
    /// the field's reader. <see cref="Reader.SettingsComponentReader"/> calls
    /// <see cref="TryApply"/> after any primary reader runs, so the mechanism works for builtin
    /// and custom readers alike.
    ///
    /// <para>Override <see cref="TargetComponentType"/> to restrict which component type receives
    /// the injection. <see cref="TryApply"/> silently skips components that do not match, so casts
    /// inside <see cref="Apply"/> are always safe. Override <see cref="Apply"/> to mutate the
    /// component.</para>
    ///
    /// <para>Example:<code>
    /// [AttributeUsage(AttributeTargets.Field)]
    /// public sealed class MyAttribute : InjectPropertyAttribute
    /// {
    ///     protected override Type TargetComponentType => typeof(SerializedFieldComponent);
    ///     protected override void Apply(SettingsComponent c) { /* cast c and mutate */ }
    /// }
    /// </code></para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public abstract class InjectPropertyAttribute : Attribute
    {
        /// <summary>
        /// Component type this injection targets. <see cref="TryApply"/> skips the component when
        /// it is not assignable to this type. Defaults to <see cref="SettingsComponent"/> (any).
        /// </summary>
        protected virtual Type TargetComponentType => typeof(SettingsComponent);

        /// <summary>
        /// Mutate <paramref name="component"/>. Only called when <paramref name="component"/> is
        /// assignable to <see cref="TargetComponentType"/>.
        /// </summary>
        protected abstract void Apply(SettingsComponent component);

        internal void TryApply(SettingsComponent component)
        {
            if (TargetComponentType.IsAssignableFrom(component.GetType()))
                Apply(component);
        }
    }
}
