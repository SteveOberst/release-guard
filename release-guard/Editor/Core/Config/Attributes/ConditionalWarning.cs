using System;

namespace ReleaseGuard.Editor.Core.Config.Attributes
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ConditionalWarning : Attribute
    {
        public ConditionalWarning(string message)
        {
            Message = message;
        }

        public string Message { get; }
    }
}