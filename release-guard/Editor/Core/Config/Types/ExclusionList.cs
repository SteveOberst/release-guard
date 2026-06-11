using System;
using System.Collections.Generic;

namespace ReleaseGuard.Editor.Core.Config.Types
{
    /// <summary>
    /// A serializable list of gitignore-style exclusion patterns. Fields of this type are
    /// automatically rendered by <see cref="SettingsRenderer"/> as a multi-line text area with a
    /// collapsible live "Preview matching assets" foldout below them.
    /// </summary>
    [Serializable]
    public sealed class ExclusionList
    {
        public List<string> patterns = new();
    }
}
