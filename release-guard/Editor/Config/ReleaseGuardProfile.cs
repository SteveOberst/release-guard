using System;
using System.Collections.Generic;

namespace ReleaseGuard.Editor.Config
{
    public enum ActivationStrategy
    {
        IsReleaseBuild,
        IsDevelopmentBuild,
        IsCI,
        IsCIAndDevelopmentBuild,
        UnityBuildProfileNames,
        Always,
    }

    [Serializable]
    public sealed class ProfileActivation
    {
        public ActivationStrategy strategy = ActivationStrategy.IsReleaseBuild;

        /// <summary>Used when <see cref="strategy"/> == <see cref="ActivationStrategy.UnityBuildProfileNames"/>.</summary>
        public List<string> unityBuildProfileNames = new();
    }

    [Serializable]
    public sealed class ReleaseGuardProfile
    {
        /// <summary>Stable kebab-case slug. Set at creation; never changes. Used as file path key.</summary>
        public string id;

        /// <summary>User-visible label. Freely editable; changes are cosmetic only.</summary>
        public string displayName;

        /// <summary>
        /// True for the built-in Release and Development profiles. Blocks deletion and condition
        /// editing in the UI.
        /// </summary>
        public bool isDefault;

        public ProfileActivation activation = new();
    }
}