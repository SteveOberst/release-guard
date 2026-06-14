using System;

namespace ReleaseGuard.Editor.Config
{
    [Serializable]
    public sealed class AdvisoryRecord
    {
        public string suppressId;
        public string message;
        public string componentId;
        public string componentDisplayName;
    }
}
