using System;
using System.Reflection;

namespace ReflectionUtility
{
    /// <summary>
    /// Utility for locating and invoking types or methods by name at runtime.
    /// Intended for plugin interop scenarios where compile-time references are unavailable.
    /// </summary>
    public static class TypeBinder
    {
        /// <summary>
        /// Searches all loaded assemblies for a type with the given full name.
        /// Returns null if no match is found.
        /// </summary>
        public static Type FindType(string fullTypeName)
        {
            if (string.IsNullOrEmpty(fullTypeName))
                return null;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(fullTypeName, throwOnError: false);
                if (type != null)
                    return type;
            }

            return null;
        }

        /// <summary>
        /// Finds a public instance or static method on <paramref name="type"/> by name.
        /// </summary>
        public static MethodInfo FindMethod(Type type, string methodName,
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
        {
            if (type == null || string.IsNullOrEmpty(methodName))
                return null;

            return type.GetMethod(methodName, flags);
        }

        /// <summary>
        /// Invokes a method by name on <paramref name="target"/>. Returns the result or null.
        /// </summary>
        public static object InvokeMethod(object target, string methodName, params object[] args)
        {
            if (target == null || string.IsNullOrEmpty(methodName))
                return null;

            var method = FindMethod(target.GetType(), methodName);
            return method?.Invoke(target, args);
        }
    }
}
