using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ReleaseGuard.Editor.Config
{
    /// <summary>
    /// Project-scoped, profile-independent storage for advisory suppressions.
    /// Backed by <see cref="EditorPrefs"/> keyed on the project path so suppressions
    /// do not bleed across projects. Changes are persisted immediately.
    /// </summary>
    internal static class AdvisorySuppressionStore
    {
        private const string PlaceholderMessage = "(No description recorded)";

        private static readonly string FastKey =
            $"ReleaseGuard.SuppressedAdvisories.{Application.dataPath.GetHashCode():X8}";
        private static readonly string RecordsKey =
            $"ReleaseGuard.SuppressedAdvisoryRecords.{Application.dataPath.GetHashCode():X8}";

        private static HashSet<string> _idsCache;
        private static List<AdvisoryRecord> _recordsCache;

        public static bool IsSuppressed(string id)
        {
            return !string.IsNullOrEmpty(id) && Ids.Contains(id);
        }

        public static void Suppress(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (Ids.Add(id))
                FlushIds();
        }

        public static void Suppress(string id, string message, string componentId, string componentDisplayName)
        {
            if (string.IsNullOrEmpty(id)) return;

            var changed = Ids.Add(id);
            var records = Records;
            var existing = records.FirstOrDefault(r =>
                r != null && string.Equals(r.suppressId, id, StringComparison.OrdinalIgnoreCase));

            if (existing == null)
            {
                records.Add(new AdvisoryRecord
                {
                    suppressId = id,
                    message = string.IsNullOrWhiteSpace(message) ? PlaceholderMessage : message,
                    componentId = string.IsNullOrWhiteSpace(componentId) ? id : componentId,
                    componentDisplayName = string.IsNullOrWhiteSpace(componentDisplayName)
                        ? (string.IsNullOrWhiteSpace(componentId) ? id : componentId)
                        : componentDisplayName
                });
                FlushRecords();
            }
            else
            {
                var recordChanged = false;
                if (!string.IsNullOrWhiteSpace(message) && existing.message != message)
                {
                    existing.message = message;
                    recordChanged = true;
                }

                if (!string.IsNullOrWhiteSpace(componentId) && existing.componentId != componentId)
                {
                    existing.componentId = componentId;
                    recordChanged = true;
                }

                if (!string.IsNullOrWhiteSpace(componentDisplayName) &&
                    existing.componentDisplayName != componentDisplayName)
                {
                    existing.componentDisplayName = componentDisplayName;
                    recordChanged = true;
                }

                if (recordChanged)
                    FlushRecords();
            }

            if (changed)
                FlushIds();
        }

        public static void Unsuppress(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            var idsChanged = Ids.Remove(id);
            var recordsChanged = Records.RemoveAll(r =>
                r != null && string.Equals(r.suppressId, id, StringComparison.OrdinalIgnoreCase)) > 0;

            if (idsChanged)
                FlushIds();
            if (recordsChanged)
                FlushRecords();
        }

        public static IReadOnlyCollection<string> GetAll() => Ids;

        public static IReadOnlyList<AdvisoryRecord> GetAllRecords()
        {
            var byId = new Dictionary<string, AdvisoryRecord>(StringComparer.OrdinalIgnoreCase);

            foreach (var record in Records)
            {
                if (record == null || string.IsNullOrWhiteSpace(record.suppressId))
                    continue;

                byId[record.suppressId] = new AdvisoryRecord
                {
                    suppressId = record.suppressId,
                    message = string.IsNullOrWhiteSpace(record.message) ? PlaceholderMessage : record.message,
                    componentId = string.IsNullOrWhiteSpace(record.componentId) ? record.suppressId : record.componentId,
                    componentDisplayName = string.IsNullOrWhiteSpace(record.componentDisplayName)
                        ? (string.IsNullOrWhiteSpace(record.componentId) ? record.suppressId : record.componentId)
                        : record.componentDisplayName
                };
            }

            foreach (var id in Ids)
            {
                if (byId.ContainsKey(id))
                    continue;

                byId[id] = new AdvisoryRecord
                {
                    suppressId = id,
                    message = PlaceholderMessage,
                    componentId = id,
                    componentDisplayName = id
                };
            }

            return byId.Values
                .OrderBy(r => r.componentDisplayName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(r => r.suppressId, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static void Invalidate()
        {
            _idsCache = null;
            _recordsCache = null;
        }

        private static HashSet<string> Ids
        {
            get
            {
                if (_idsCache != null) return _idsCache;
                _idsCache = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var raw = EditorPrefs.GetString(FastKey, string.Empty);
                if (string.IsNullOrEmpty(raw)) return _idsCache;
                foreach (var id in raw.Split('\n'))
                {
                    var trimmed = id.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                        _idsCache.Add(trimmed);
                }

                return _idsCache;
            }
        }

        private static List<AdvisoryRecord> Records
        {
            get
            {
                if (_recordsCache != null) return _recordsCache;

                var raw = EditorPrefs.GetString(RecordsKey, string.Empty);
                if (string.IsNullOrEmpty(raw))
                {
                    _recordsCache = new List<AdvisoryRecord>();
                    return _recordsCache;
                }

                var parsed = JsonUtility.FromJson<AdvisoryRecordList>(raw);
                _recordsCache = parsed?.records ?? new List<AdvisoryRecord>();
                return _recordsCache;
            }
        }

        private static void FlushIds() =>
            EditorPrefs.SetString(FastKey, string.Join("\n", Ids));

        private static void FlushRecords() =>
            EditorPrefs.SetString(RecordsKey, JsonUtility.ToJson(new AdvisoryRecordList { records = Records }));

        [Serializable]
        private sealed class AdvisoryRecordList
        {
            public List<AdvisoryRecord> records = new();
        }
    }
}
