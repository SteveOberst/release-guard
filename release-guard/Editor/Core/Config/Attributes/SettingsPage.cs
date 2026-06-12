using System;

namespace ReleaseGuard.Editor.Core.Config.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class SettingsPage : SettingsContainer
    {
        public SettingsPage(string label, string intro, string description = "")
            : base(label, description)
        {
            Intro = intro;
        }

        public string Intro { get; }
    }
}