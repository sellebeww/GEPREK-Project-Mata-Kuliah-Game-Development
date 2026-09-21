using System.Collections;
using System.Collections.Generic;
using Geprek.Core;
using UnityEngine;

namespace Geprek.World
{
    /// <summary>
    /// Satu tempat untuk semua umpan balik visual kecil: uap, asap, percikan,
    /// getaran kamera, dan sentakan skala. Sistem lain memanggil ke sini supaya
    /// efeknya konsisten dan gampang dimatikan kalau perlu.
    /// </summary>
    public class JuiceDirector : MonoBehaviour
    {
        static JuiceDirector _instance;

        /// <summary>
        /// Field statis bisa kehilangan isinya saat Editor memuat ulang domain,
        /// sementara objeknya sendiri masih hidup di scene. Karena itu pencarian
        /// ulang dipakai sebagai jaring pengaman, sama seperti Singleton lain.
        /// </summary>
        public static JuiceDirector Instance
        {
            get
            {
                if (_instance == null) _instance = FindAnyObjectByType<JuiceDirector>(FindObjectsInactive.Include);
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("Sprite efek")]
        [SerializeField] Sprite puffSprite;
        [SerializeField] Sprite sparkSprite;
        [SerializeField] Sprite dotSprite;

        [Header("Kolam partikel")]
        [SerializeField] int poolSize = 48;

        [Header("Kamera")]
        [SerializeField] CameraFollow cameraFollow;

        readonly List<Puff> _pool = new();
        readonly HashSet<Transform> _punching = new();
        int _next;

        void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(this); return; }
            _instance = this;

            for (int i = 0; i < poolSize; i++)
            {
                var go = new GameObject($"Puff{i}");
                go.transform.SetParent(transform, false);
                _pool.Add(go.AddComponent<Puff>());
            }
        }

        void OnDestroy() { if (_instance == this) _instance = null; }

        Puff Rent()
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                var p = _pool[(_next + i) % _pool.Count];
                if (p != null && !p.Busy) { _next = (_next + i + 1) % _pool.Count; return p; }
            }
            return null;   // semua sedang dipakai: efek ini dilewati saja
        }

        // ---------------------------------------------------------------- efek dapur

        /// <summary>Uap tipis dari penggorengan yang sedang panas.</summary>
        public void Steam(Vector3 position)
        {
            var p = Rent(); if (p == null) return;
            var jitter = new Vector3(Random.Range(-0.12f, 0.12f), 0f, 0f);
            p.Play(puffSprite, position + jitter, 800,
                   new Color(1f, 1f, 1f, 0.6f), new Color(1f, 1f, 1f, 0f),
                   0.6f, 1.9f,
                   new Vector3(Random.Range(-0.15f, 0.15f), Random.Range(0.55f, 0.85f), 0f),
                   Vector3.zero, Random.Range(1.0f, 1.5f));
        }

        /// <summary>Asap gelap: penanda ayam mulai gosong.</summary>
        public void Smoke(Vector3 position)
        {
            var p = Rent(); if (p == null) return;
            p.Play(puffSprite, position + new Vector3(Random.Range(-0.15f, 0.15f), 0f, 0f), 800,
                   new Color(0.22f, 0.20f, 0.19f, 0.85f), new Color(0.3f, 0.28f, 0.27f, 0f),
                   0.7f, 2.6f,
                   new Vector3(Random.Range(-0.25f, 0.25f), Random.Range(0.7f, 1.1f), 0f),
                   Vector3.zero, Random.Range(0.9f, 1.3f), Random.Range(-40f, 40f));
        }

        /// <summary>Cipratan minyak kecil saat bahan masuk penggorengan.</summary>
        public void OilSplash(Vector3 position, int count = 6)
        {
            for (int i = 0; i < count; i++)
            {
                var p = Rent(); if (p == null) return;
                float a = Random.Range(20f, 160f) * Mathf.Deg2Rad;
                float speed = Random.Range(1.2f, 2.4f);
                p.Play(dotSprite, position, 810,
                       new Color(1f, 0.85f, 0.42f, 0.95f), new Color(1f, 0.75f, 0.3f, 0f),
                       0.55f, 0.25f,
                       new Vector3(Mathf.Cos(a) * speed, Mathf.Sin(a) * speed, 0f),
                       new Vector3(0f, -5.5f, 0f), Random.Range(0.35f, 0.55f));
            }
        }

        /// <summary>Percikan bintang: hasil sempurna, upgrade, resep baru.</summary>
        public void Sparkle(Vector3 position, Color color, int count = 5, float spread = 0.45f)
        {
            for (int i = 0; i < count; i++)
            {
                var p = Rent(); if (p == null) return;
                var offset = new Vector3(Random.Range(-spread, spread), Random.Range(-spread * 0.6f, spread), 0f);
                p.Play(sparkSprite, position + offset, 820,
                       color, new Color(color.r, color.g, color.b, 0f),
                       Random.Range(0.2f, 0.4f), Random.Range(0.8f, 1.15f),
                       new Vector3(0f, Random.Range(0.3f, 0.8f), 0f), Vector3.zero,
                       Random.Range(0.45f, 0.75f), Random.Range(-120f, 120f));
            }
        }

        /// <summary>Debu kecil di kaki, dipakai saat menggeprek.</summary>
        public void Impact(Vector3 position)
        {
            var p = Rent(); if (p == null) return;
            p.Play(puffSprite, position, 805,
                   new Color(0.95f, 0.85f, 0.7f, 0.65f), new Color(0.95f, 0.85f, 0.7f, 0f),
                   0.35f, 1.3f, Vector3.zero, Vector3.zero, 0.3f);
        }

        // ---------------------------------------------------------------- popup & kamera

        /// <summary>Teks mengambang di atas titik dunia. UI yang menggambarnya.</summary>
        public void Popup(string text, Sprite icon, Vector3 worldPosition, Color color)
            => GameEvents.RaisePopup(text, icon, worldPosition, color);

        public void Shake(float amount, float duration) => cameraFollow?.Shake(amount, duration);

        /// <summary>Sentakan skala singkat, untuk menandai objek yang baru berubah.</summary>
        public void Punch(Transform target, float amount = 0.18f, float duration = 0.22f)
        {
            if (target == null || !isActiveAndEnabled) return;
            // sentakan yang menumpuk akan merekam skala dasar yang sudah berubah,
            // sehingga objek bisa tertinggal dalam ukuran salah. Satu objek satu sentakan.
            if (!_punching.Add(target)) return;
            StartCoroutine(PunchRoutine(target, amount, duration));
        }

        IEnumerator PunchRoutine(Transform target, float amount, float duration)
        {
            Vector3 baseScale = target.localScale;
            float t = 0f;
            while (t < duration && target != null)
            {
                t += Time.deltaTime;
                float k = t / duration;
                // naik cepat lalu kembali, sedikit memantul
                float curve = Mathf.Sin(k * Mathf.PI) * (1f - k * 0.35f);
                target.localScale = baseScale * (1f + amount * curve);
                yield return null;
            }
            if (target != null) target.localScale = baseScale;
            _punching.Remove(target);
        }

        // ---------------------------------------------------------------- pintasan statis

        public static void DoSteam(Vector3 p) => Instance?.Steam(p);
        public static void DoSmoke(Vector3 p) => Instance?.Smoke(p);
        public static void DoOil(Vector3 p, int n = 6) => Instance?.OilSplash(p, n);
        public static void DoSparkle(Vector3 p, Color c, int n = 5, float spread = 0.45f) => Instance?.Sparkle(p, c, n, spread);
        public static void DoImpact(Vector3 p) => Instance?.Impact(p);
        public static void DoPopup(string text, Sprite icon, Vector3 p, Color c) => Instance?.Popup(text, icon, p, c);
        public static void DoShake(float a, float d) => Instance?.Shake(a, d);
        public static void DoPunch(Transform t, float a = 0.18f, float d = 0.22f) => Instance?.Punch(t, a, d);
    }
}
