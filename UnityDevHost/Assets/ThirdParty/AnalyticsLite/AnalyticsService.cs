using System.Collections.Generic;
using UnityEngine;

namespace AnalyticsLite
{
    /// <summary>
    /// Lightweight analytics facade. Batches events and flushes on interval or manual request.
    /// Attach to a persistent scene root that survives scene loads.
    /// </summary>
    public sealed class AnalyticsService : MonoBehaviour
    {
        [SerializeField] private AnalyticsConfig config;
#pragma warning disable CS0414 // assigned but only read inside #if UNITY_EDITOR
        [SerializeField] private bool logToConsoleInEditor = true;
#pragma warning restore CS0414

        public static AnalyticsService Instance { get; private set; }

        private readonly Queue<AnalyticsEvent> _pending = new Queue<AnalyticsEvent>();
        private float _flushTimer;
        private bool _initialized;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            _initialized = config != null && !string.IsNullOrEmpty(config.AppId);
        }

        private void Update()
        {
            if (!_initialized || config == null || !config.BatchEvents)
                return;

            _flushTimer += Time.unscaledDeltaTime;
            if (_flushTimer >= config.FlushIntervalSeconds)
            {
                Flush();
                _flushTimer = 0f;
            }
        }

        public void TrackEvent(string eventName, Dictionary<string, object> properties = null)
        {
            if (!_initialized)
                return;

            _pending.Enqueue(new AnalyticsEvent(eventName, properties));

            if (!config.BatchEvents || _pending.Count >= config.BatchSize)
                Flush();
        }

        public void SetUserProperty(string key, string value)
        {
            /* stub */
        }

        public void Flush()
        {
            while (_pending.Count > 0)
            {
                var evt = _pending.Dequeue();
#if UNITY_EDITOR
                if (logToConsoleInEditor)
                    Debug.Log($"[AnalyticsLite] {evt.Name}");
#endif
            }
        }

        private readonly struct AnalyticsEvent
        {
            public readonly string Name;
            public readonly Dictionary<string, object> Properties;

            public AnalyticsEvent(string name, Dictionary<string, object> properties)
            {
                Name = name;
                Properties = properties;
            }
        }
    }
}