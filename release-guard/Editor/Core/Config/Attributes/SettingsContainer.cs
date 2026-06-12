using System;

namespace ReleaseGuard.Editor.Core.Config.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SettingsContainerAttribute : Attribute
    {
        protected SettingsContainerAttribute(string label, string description = "")
        {
            Label = label;
            Description = description;
        }

        public string Label { get; }
        public string Description { get; }
    }
}