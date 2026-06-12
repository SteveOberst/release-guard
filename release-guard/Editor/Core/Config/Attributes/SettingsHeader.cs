using System;

namespace ReleaseGuard.Editor.Core.Config.Attributes
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class SettingsHeader : Attribute
    {
        public SettingsHeader(string header)
        {
            Header = header;
        }

        public string Header { get; }
    }
}