using System;

namespace ReleaseGuard.Editor.Core.Config.Attributes
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class SettingsConditionalWarningAttribute : Attribute
    {
        public SettingsConditionalWarningAttribute(string message)
        {
            Message = message;
        }

        public string Message { get; }
    }
}