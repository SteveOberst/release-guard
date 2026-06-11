using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace RemoteConfigKit
{
    /// <summary>
    /// Fetches remote configuration at runtime and merges it with local defaults.
    /// Falls back to <see cref="localDefaults"/> if the network request fails or times out.
    /// </summary>
    public sealed class RemoteConfigClient : MonoBehaviour
    {
        [SerializeField] private string configUrl = string.Empty;
        [SerializeField] private TextAsset localDefaults;
        [SerializeField] private float pollIntervalMinutes = 15f;
        [SerializeField] private float requestTimeoutSeconds = 10f;
        [SerializeField] private bool applyLocally = true;

        private string _activeJson;
        private float _pollTimer;

        private void Start()
        {
            ApplyLocalDefaults();
            if (!string.IsNullOrEmpty(configUrl))
                StartCoroutine(FetchRoutine(null));
        }

        private void Update()
        {
            if (string.IsNullOrEmpty(configUrl) || pollIntervalMinutes <= 0f)
                return;

            _pollTimer += Time.unscaledDeltaTime;
            if (_pollTimer >= pollIntervalMinutes * 60f)
            {
                _pollTimer = 0f;
                FetchRemoteConfig(null);
            }
        }

        public void ApplyLocalDefaults()
        {
            if (localDefaults != null)
                _activeJson = localDefaults.text;
        }

        public void FetchRemoteConfig(Action<bool> callback)
        {
            StartCoroutine(FetchRoutine(callback));
        }

        public T GetValue<T>(string key, T defaultValue = default)
        {
            // Simplified: in production this would deserialize _activeJson and look up key.
            return defaultValue;
        }

        private IEnumerator FetchRoutine(Action<bool> callback)
        {
            using var request = UnityWebRequest.Get(configUrl);
            request.timeout = (int)requestTimeoutSeconds;
            yield return request.SendWebRequest();

            var success = request.result == UnityWebRequest.Result.Success;
            if (success && applyLocally)
                _activeJson = request.downloadHandler.text;

            callback?.Invoke(success);
        }
    }
}
