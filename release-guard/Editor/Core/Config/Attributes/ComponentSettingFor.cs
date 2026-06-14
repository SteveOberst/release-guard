using System;

namespace ReleaseGuard.Editor.Core.Config.Attributes
{
    /// <summary>
    /// Marks a field in <c>ComponentSettings</c> as the settings for a specific component.
    /// These fields are suppressed from the Components page top-level view and instead shown
    /// inline when the user expands that component's row in the component list.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    internal sealed class ComponentSettingFor : Attribute
    {
        public ComponentSettingFor(string componentId)
        {
            ComponentId = componentId;
        }

        public string ComponentId { get; }
    }
}