using System;

namespace ReleaseGuard.Editor.Core.Config.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SettingsContainer : Attribute
    {
        protected SettingsContainer(string label, string description = "")
        {
            Label = label;
            Description = description;
        }

        public string Label { get; }
        public string Description { get; }
    }
}