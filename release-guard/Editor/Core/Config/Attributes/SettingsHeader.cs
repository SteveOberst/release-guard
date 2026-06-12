using System;

namespace ReleaseGuard.Editor.Core.Config.Attributes
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class SettingsHeaderAttribute : Attribute
    {
        public SettingsHeaderAttribute(string header)
        {
            Header = header;
        }

        public string Header { get; }
    }
}