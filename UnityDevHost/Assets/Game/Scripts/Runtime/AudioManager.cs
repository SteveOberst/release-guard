using System.Collections.Generic;
using AttackSurfaceFixture.Game.Core;
using UnityEngine;

namespace AttackSurfaceFixture.Game.Runtime
{
    /// <summary>
    /// Minimal audio manager. Registers named clips in the Inspector and provides
    /// a typed string-key API for playing SFX and controlling music.
    ///
    /// <b>IL2CPP note:</b> The <c>Dictionary&lt;string, NamedClip&gt;</c> lookup here
    /// is a plain hashtable operation  -- it does <em>not</em> call <c>Assembly.GetTypes()</c>
    /// or any runtime reflection. String keys in a Dictionary are safe under any stripping
    /// level because the dictionary itself is a concrete, directly-referenced type.
    ///
    /// The source pool avoids <c>AudioSource.PlayOneShot</c> on the main source, which
    /// prevents later calls from interrupting earlier ones and gives you independent
    /// volume and pitch control per channel.
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        [System.Serializable]
        private struct NamedClip
        {
            public string    key;
            public AudioClip clip;
            [Range(0f, 1f)]
            public float     volume;
        }

        [SerializeField] private NamedClip[] registeredClips = System.Array.Empty<NamedClip>();
        [SerializeField] private int         poolSize         = 8;
        [SerializeField] private AudioSource musicSource;

        public static AudioManager Instance { get; private set; }

        private readonly Dictionary<string, NamedClip> _index = new Dictionary<string, NamedClip>();
        private AudioSource[] _pool;
        private int           _poolCursor;

        // -- Unity lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            BuildPool();
            IndexClips();
            ServiceLocator.Register(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<AudioManager>();
        }

        // -- Public API

        /// <summary>Plays a one-shot SFX identified by <paramref name="clipKey"/>. Silent if unknown.</summary>
        public void Play(string clipKey)
        {
            if (!_index.TryGetValue(clipKey, out var nc) || nc.clip == null) return;
            var src = NextPoolSlot();
            src.clip   = nc.clip;
            src.volume = nc.volume > 0f ? nc.volume : 1f;
            src.pitch  = 1f;
            src.Play();
        }

        /// <summary>Plays <paramref name="clipKey"/> as the looping background music track.</summary>
        public void PlayMusic(string clipKey)
        {
            if (musicSource == null) return;
            if (!_index.TryGetValue(clipKey, out var nc) || nc.clip == null) return;
            if (musicSource.clip == nc.clip && musicSource.isPlaying) return;
            musicSource.clip   = nc.clip;
            musicSource.loop   = true;
            musicSource.volume = nc.volume > 0f ? nc.volume : 0.7f;
            musicSource.Play();
        }

        public void StopMusic()         => musicSource?.Stop();
        public void PauseMusic()        => musicSource?.Pause();
        public void UnpauseMusic()      => musicSource?.UnPause();

        public void SetMusicVolume(float v)
        {
            if (musicSource != null) musicSource.volume = Mathf.Clamp01(v);
        }

        // -- Private helpers

        private void BuildPool()
        {
            _pool = new AudioSource[poolSize];
            for (var i = 0; i < poolSize; i++)
            {
                var child = new GameObject($"SFXSource_{i:00}")
                {
                    transform = { parent = transform }
                };
                var src            = child.AddComponent<AudioSource>();
                src.playOnAwake    = false;
                src.spatialBlend   = 0f;   // 2-D sound
                _pool[i]           = src;
            }
        }

        private void IndexClips()
        {
            foreach (var nc in registeredClips)
                if (!string.IsNullOrEmpty(nc.key))
                    _index[nc.key] = nc;
        }

        private AudioSource NextPoolSlot()
        {
            var src     = _pool[_poolCursor];
            _poolCursor = (_poolCursor + 1) % _pool.Length;
            return src;
        }
    }
}
