using System;

namespace ReleaseGuard.Editor.Core.Config.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class SettingsPageAttribute : SettingsContainerAttribute
    {
        public SettingsPageAttribute(string label, string intro, string description = "")
            : base(label, description)
        {
            Intro = intro;
        }

        public string Intro { get; }
    }
}