using System;
using UnityEngine;

namespace AnalyticsLite
{
    [CreateAssetMenu(menuName = "AnalyticsLite/Analytics Config", fileName = "AnalyticsConfig")]
    public sealed class AnalyticsConfig : ScriptableObject
    {
        [Header("Application")] [SerializeField]
        private string appId = string.Empty;

        [SerializeField] private string environmentKey = string.Empty;
        [SerializeField] private string dataCenter = "us-east-1";

        [Header("Event Batching")] [SerializeField]
        private bool batchEvents = true;

        [SerializeField] private int batchSize = 20;
        [SerializeField] private int flushIntervalSeconds = 30;

        [Header("Filtering")] [SerializeField] private string[] allowedEventNames = Array.Empty<string>();
        [SerializeField] private bool allowCustomEvents = true;

        public string AppId => appId;
        public string EnvironmentKey => environmentKey;
        public string DataCenter => dataCenter;
        public bool BatchEvents => batchEvents;
        public int BatchSize => batchSize;
        public int FlushIntervalSeconds => flushIntervalSeconds;
        public string[] AllowedEventNames => allowedEventNames;
        public bool AllowCustomEvents => allowCustomEvents;
    }
}