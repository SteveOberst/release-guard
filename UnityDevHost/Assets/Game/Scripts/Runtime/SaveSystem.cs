using AttackSurfaceFixture.Game.Core;
using AttackSurfaceFixture.Game.Data;
using UnityEngine;

namespace AttackSurfaceFixture.Game.Runtime
{
    /// <summary>
    /// Persists and loads <see cref="PlayerData"/> via <c>JsonUtility</c> and <c>PlayerPrefs</c>.
    ///
    /// <b>Why JsonUtility is IL2CPP-safe:</b><br/>
    /// <c>JsonUtility.ToJson / FromJson</c> generates C++ serialization stubs for all
    /// <c>[Serializable]</c> types at IL2CPP build time  -- it does <em>not</em> call
    /// <c>System.Reflection</c> at runtime. Fields are read/written through those stubs,
    /// so managed stripping cannot remove them even at the highest stripping level.
    ///
    /// <b>Production hardening checklist</b> (outside the scope of this demo):
    /// <list type="bullet">
    ///   <item>Replace PlayerPrefs with AES-GCM encrypted file I/O.</item>
    ///   <item>Append an HMAC to detect save-file tampering.</item>
    ///   <item>Version the save format and handle migration gracefully.</item>
    ///   <item>Never store secrets (session tokens, receipt data) in the save file.</item>
    /// </list>
    /// </summary>
    public sealed class SaveSystem : MonoBehaviour
    {
        // Versioned key so a schema change lets us detect and migrate old saves.
        private const string SaveKey = "com.attacksurfacefixture.player_data.v1";

        public static SaveSystem Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            ServiceLocator.Register(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<SaveSystem>();
        }

        /// <summary>
        /// Loads persisted player data, or returns a default dataset if no save exists.
        /// Never returns null.
        /// </summary>
        public PlayerData Load()
        {
            var json = PlayerPrefs.GetString(SaveKey, string.Empty);
            if (string.IsNullOrEmpty(json))
                return PlayerData.CreateDefault();

            var data = JsonUtility.FromJson<PlayerData>(json);
            return data ?? PlayerData.CreateDefault();
        }

        /// <summary>Writes <paramref name="data"/> to persistent storage immediately.</summary>
        public void Save(PlayerData data)
        {
            if (data == null) return;
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        /// <summary>Deletes the current save file. Useful for debug reset flows.</summary>
        public void DeleteSave()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
        }
    }
}
