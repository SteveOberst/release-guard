using System;
using System.Reflection;

namespace ReleaseGuard.Editor.Core.Build
{
    /// <summary>
    /// Reads the active Unity Build Profile name (Unity 6+). Build Profiles are Unity's
    /// established way to keep multiple named build configurations ("Production", "Staging", etc.).
    ///
    /// Done via reflection on purpose: the package still compiles on editors that predate Build
    /// Profiles, where this simply returns <c>null</c> (meaning "classic platform settings").
    /// </summary>
    internal static class BuildProfileResolver
    {
        public static string GetActiveProfileName()
        {
            try
            {
                // UnityEditor.Build.Profile.BuildProfile.GetActiveBuildProfile() : BuildProfile
                var profileType = Type.GetType("UnityEditor.Build.Profile.BuildProfile, UnityEditor");
                var getActive =
                    profileType?.GetMethod("GetActiveBuildProfile", BindingFlags.Public | BindingFlags.Static);
                if (getActive == null)
                    return null;

                // BuildProfile derives from ScriptableObject, so it carries a .name.
                var active = getActive.Invoke(null, null) as UnityEngine.Object;
                return active != null ? active.name : null;
            }
            catch
            {
                return null;
            }
        }
    }
}