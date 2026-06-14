using System;
using UnityEngine;

namespace ReleaseGuard.Editor.Core.Components
{
    /// <summary>
    /// Base class for per-component settings stored in <see cref="ReleaseGuard.Editor.Core.Config.Types.ComponentToggleList"/>.
    /// Holds the enabled toggle that is shared by every component entry.
    /// Components with additional configurable fields extend this class.
    /// </summary>
    [Serializable]
    public class ReleaseGuardComponentSettings
    {
        [HideInInspector]
        public string componentId;

        [Tooltip("When disabled, this component is skipped in every build phase.")]
        public bool enabled = true;
    }
}
