using System;
using System.Reflection;

namespace ReflectionUtility
{
    /// <summary>
    /// Extension methods for accessing private or protected fields on arbitrary objects.
    /// Useful for testing and for bridging with legacy code that does not expose public API.
    /// </summary>
    public static class ReflectionExtensions
    {
        private const BindingFlags InstanceAll =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        /// <summary>
        /// Reads the value of a named field from <paramref name="obj"/> by reflection.
        /// Returns <c>default(T)</c> if the field is not found.
        /// </summary>
        public static T GetFieldValue<T>(this object obj, string fieldName)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));

            var field = obj.GetType().GetField(fieldName, InstanceAll);
            if (field == null)
                return default;

            return field.GetValue(obj) is T value ? value : default;
        }

        /// <summary>
        /// Sets the value of a named field on <paramref name="obj"/> by reflection.
        /// Does nothing if the field is not found.
        /// </summary>
        public static void SetFieldValue(this object obj, string fieldName, object value)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));

            var field = obj.GetType().GetField(fieldName, InstanceAll);
            field?.SetValue(obj, value);
        }

        /// <summary>
        /// Reads the value of a named property from <paramref name="obj"/> by reflection.
        /// Returns <c>default(T)</c> if the property is not found.
        /// </summary>
        public static T GetPropertyValue<T>(this object obj, string propertyName)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));

            var prop = obj.GetType().GetProperty(propertyName, InstanceAll);
            if (prop == null)
                return default;

            return prop.GetValue(obj) is T value ? value : default;
        }
    }
}