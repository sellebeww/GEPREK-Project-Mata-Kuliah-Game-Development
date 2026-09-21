using System;
using System.Collections.Generic;
using Geprek.Core;
using UnityEngine;

namespace Geprek.Audio
{
    [Serializable]
    public class SfxEntry
    {
        public SfxId id;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 0.8f;
        [Tooltip("Variasi nada supaya suara berulang tidak membosankan.")]
        [Range(0f, 0.3f)] public float pitchJitter = 0.06f;
    }

    /// <summary>
    /// Pemutar suara sederhana dengan kolam AudioSource. Aman dipanggil walau
    /// klip belum diisi: panggilan tanpa klip diabaikan diam-diam.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField] List<SfxEntry> sfx = new();
        [SerializeField] AudioSource musicSource;
        [SerializeField] AudioClip dayMusic;
        [SerializeField] AudioClip nightMusic;
        [SerializeField] AudioClip menuMusic;

        [Header("Ambience berlapis")]
        [Tooltip("Dengung dapur, selalu terdengar tipis saat jam operasional.")]
        [SerializeField] AudioSource kitchenSource;
        [SerializeField] AudioClip kitchenAmbience;
        [Tooltip("Gumam pelanggan, kerasnya mengikuti jumlah orang di ruangan.")]
        [SerializeField] AudioSource crowdSource;
        [SerializeField] AudioClip crowdAmbience;
        [Tooltip("Suara jalanan, dipakai saat malam di rumah.")]
        [SerializeField] AudioClip streetAmbience;
        [SerializeField, Range(0f, 1f)] float ambienceVolume = 0.5f;
        [SerializeField, Range(0f, 1f)] float musicVolume = 0.35f;
        [SerializeField, Range(0f, 1f)] float sfxVolume = 0.9f;
        [SerializeField] int voiceCount = 8;

        readonly Dictionary<SfxId, SfxEntry> _map = new();
        AudioSource[] _voices;
        int _next;

        public float MusicVolume
        {
            get => musicVolume;
            set { musicVolume = Mathf.Clamp01(value); if (musicSource != null) musicSource.volume = musicVolume; }
        }

        public float SfxVolume { get => sfxVolume; set => sfxVolume = Mathf.Clamp01(value); }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            foreach (var e in sfx) if (e != null) _map[e.id] = e;

            _voices = new AudioSource[Mathf.Max(1, voiceCount)];
            for (int i = 0; i < _voices.Length; i++)
            {
                var src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 0f;
                _voices[i] = src;
            }

            if (musicSource != null)
            {
                musicSource.loop = true;
                musicSource.volume = musicVolume;
            }

            foreach (var src in new[] { kitchenSource, crowdSource })
            {
                if (src == null) continue;
                src.loop = true;
                src.playOnAwake = false;
                src.volume = 0f;
            }
        }

        /// <summary>Atur dua lapis ambience sekaligus. Kirim null untuk mematikan salah satunya.</summary>
        void SetAmbience(AudioClip baseLayer, AudioClip crowdLayer)
        {
            Apply(kitchenSource, baseLayer, ambienceVolume * 0.8f);
            Apply(crowdSource, crowdLayer, 0f);      // volumenya diatur mengikuti keramaian
            _crowdTarget = crowdLayer != null ? _crowdTarget : 0f;

            static void Apply(AudioSource src, AudioClip clip, float volume)
            {
                if (src == null) return;
                if (clip == null) { src.Stop(); src.clip = null; src.volume = 0f; return; }
                if (src.clip != clip) { src.clip = clip; src.Play(); }
                src.volume = volume;
            }
        }

        void OnEnable()
        {
            GameEvents.StateChanged += OnStateChanged;
            GameEvents.QueueChanged += OnQueueChanged;
        }

        void OnDisable()
        {
            GameEvents.StateChanged -= OnStateChanged;
            GameEvents.QueueChanged -= OnQueueChanged;
        }

        /// <summary>Keramaian terdengar lebih penuh saat ruangan memang ramai.</summary>
        void OnQueueChanged(int seated, int queued)
        {
            _crowdTarget = Mathf.Clamp01((seated + queued) / 6f);
        }

        float _crowdTarget, _crowdCurrent;

        void Update()
        {
            if (crowdSource == null) return;
            _crowdCurrent = Mathf.MoveTowards(_crowdCurrent, _crowdTarget, Time.unscaledDeltaTime * 0.6f);
            crowdSource.volume = _crowdCurrent * ambienceVolume * 0.9f;
        }
        void OnDestroy() { if (Instance == this) Instance = null; }

        void OnStateChanged(GameState from, GameState to)
        {
            switch (to)
            {
                case GameState.MainMenu:
                    PlayMusic(menuMusic);
                    SetAmbience(null, null);
                    break;

                case GameState.DayOperating:
                case GameState.DayClosing:
                    PlayMusic(dayMusic);
                    SetAmbience(kitchenAmbience, crowdAmbience);
                    break;

                case GameState.NightHome:
                    PlayMusic(nightMusic);
                    SetAmbience(streetAmbience, null);
                    break;

                case GameState.Cutscene:
                    SetAmbience(null, null);
                    break;
            }
        }

        public void PlayMusic(AudioClip clip)
        {
            if (musicSource == null || clip == null || musicSource.clip == clip) return;
            musicSource.clip = clip;
            musicSource.volume = musicVolume;
            musicSource.Play();
        }

        public static void Play(SfxId id) => Instance?.PlaySfx(id);

        public void PlaySfx(SfxId id)
        {
            if (!_map.TryGetValue(id, out var entry) || entry.clip == null || _voices == null) return;

            var src = _voices[_next];
            _next = (_next + 1) % _voices.Length;

            src.clip = entry.clip;
            src.volume = entry.volume * sfxVolume;
            src.pitch = 1f + UnityEngine.Random.Range(-entry.pitchJitter, entry.pitchJitter);
            src.Play();
        }
    }
}
