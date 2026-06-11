namespace ReleaseGuard.Editor.Util
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEditor;

    internal static class MemberInfoUnityPathResolver
    {
        // Populated once per audit; cleared between audits by the caller.
        // ReSharper disable once InconsistentNaming
        private static Dictionary<Type, string> s_cache;

        // Called by ReleaseGuardExecutor before starting an audit so each run
        // gets a fresh scan rather than stale data from a previous run.
        internal static void BeginAudit()
        {
            s_cache = BuildCache();
        }

        internal static void EndAudit()
        {
            s_cache = null;
        }

        // ReSharper disable once UnusedMember.Global
        public static string GetAssetPath(MemberInfo memberInfo)
        {
            var type = memberInfo switch
            {
                Type t => t,
                MethodInfo method => method.DeclaringType,
                FieldInfo field => field.DeclaringType,
                PropertyInfo prop => prop.DeclaringType,
                _ => memberInfo.DeclaringType
            };

            if (type == null)
                return null;

            // Fall back to a single live scan if called outside an audit
            // (e.g. from the audit window without the executor lifecycle).
            var map = s_cache ?? BuildCache();
            return map.GetValueOrDefault(type);
        }

        private static Dictionary<Type, string> BuildCache()
        {
            var map = new Dictionary<Type, string>();
            var guids = AssetDatabase.FindAssets("t:MonoScript");

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);

                if (script == null)
                    continue;

                var t = script.GetClass();
                if (t != null)
                    map.TryAdd(t, path);
            }

            return map;
        }
    }
}