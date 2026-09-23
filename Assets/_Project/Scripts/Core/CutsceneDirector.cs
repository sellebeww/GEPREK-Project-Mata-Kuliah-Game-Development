using System;
using System.Collections;
using System.Collections.Generic;
using Geprek.Data;
using Geprek.World;
using UnityEngine;

namespace Geprek.Core
{
    public enum CutsceneId { Opening, ToArc2, ToArc3, Ending, Tutorial }

    /// <summary>
    /// Satu perintah dalam adegan. Isinya sengaja sederhana supaya naskahnya
    /// bisa dibaca berurutan di satu berkas, tanpa perlu editor timeline.
    /// </summary>
    public abstract class Beat { }

    public class BeatLine : Beat
    {
        public string speaker, text;
        public Sprite portrait;
    }

    public class BeatTitle : Beat
    {
        public string title, subtitle;
        public float hold = 2.2f;
    }

    public class BeatWait : Beat { public float seconds; }
    public class BeatFade : Beat { public bool toBlack; public float duration = 0.45f; }
    public class BeatLocation : Beat { public LocationId location; }
    public class BeatFocus : Beat { public Vector3? point; }

    /// <summary>
    /// Pemutar adegan cerita. Menjalankan daftar perintah satu per satu dan
    /// menunggu pemain menekan lanjut pada tiap dialog. UI yang menggambar,
    /// kelas ini hanya mengatur urutannya.
    /// </summary>
    public class CutsceneDirector : MonoBehaviour
    {
        [SerializeField] LocationManager locations;
        [SerializeField] CameraFollow cameraFollow;
        [SerializeField] GameDatabase database;

        public event Action<string, string, Sprite> LineShown;   // pembicara, isi, potret
        public event Action<string, string> TitleShown;
        public event Action TitleHidden;
        public event Action<bool, float> FadeRequested;          // ke hitam?, durasi
        public event Action<bool> CutsceneActiveChanged;

        public bool IsPlaying { get; private set; }

        bool _advance;

        /// <summary>Dipanggil UI saat pemain menekan lanjut.</summary>
        public void Advance() => _advance = true;

        public void Play(CutsceneId id, Action onComplete)
        {
            if (IsPlaying) return;
            StartCoroutine(Run(Script(id), onComplete));
        }

        IEnumerator Run(IEnumerable<Beat> beats, Action onComplete)
        {
            IsPlaying = true;
            CutsceneActiveChanged?.Invoke(true);

            foreach (var beat in beats)
            {
                switch (beat)
                {
                    case BeatFade f:
                        FadeRequested?.Invoke(f.toBlack, f.duration);
                        yield return new WaitForSecondsRealtime(f.duration);
                        break;

                    case BeatLocation l:
                        locations?.Show(l.location, snapPlayer: true);
                        break;

                    case BeatFocus fo:
                        cameraFollow?.FocusOn(fo.point);
                        cameraFollow?.SnapToTarget();
                        break;

                    case BeatTitle t:
                        TitleShown?.Invoke(t.title, t.subtitle);
                        yield return new WaitForSecondsRealtime(t.hold);
                        TitleHidden?.Invoke();
                        break;

                    case BeatWait w:
                        yield return new WaitForSecondsRealtime(w.seconds);
                        break;

                    case BeatLine line:
                        LineShown?.Invoke(line.speaker, line.text, line.portrait);
                        _advance = false;
                        // tunggu pemain menekan lanjut, dengan batas aman supaya tidak macet
                        float guard = 0f;
                        while (!_advance && guard < 30f)
                        {
                            guard += Time.unscaledDeltaTime;
                            yield return null;
                        }
                        break;
                }
            }

            cameraFollow?.FocusOn(null);
            IsPlaying = false;
            CutsceneActiveChanged?.Invoke(false);
            onComplete?.Invoke();
        }

        // ---------------------------------------------------------------- naskah

        Sprite Face(CharacterSkin skin) => skin != null ? skin.GetIdle(Facing.Down) : null;

        static Beat Line(string who, string what, Sprite face) =>
            new BeatLine { speaker = who, text = what, portrait = face };

        static Beat Title(string t, string sub, float hold = 2.4f) =>
            new BeatTitle { title = t, subtitle = sub, hold = hold };

        static Beat Fade(bool toBlack, float d = 0.45f) => new BeatFade { toBlack = toBlack, duration = d };
        static Beat Wait(float s) => new BeatWait { seconds = s };
        static Beat Go(LocationId l) => new BeatLocation { location = l };
        static Beat Focus(Vector3? p) => new BeatFocus { point = p };

        IEnumerable<Beat> Script(CutsceneId id) => id switch
        {
            CutsceneId.Opening => Opening(),
            CutsceneId.ToArc2 => ToArc2(),
            CutsceneId.ToArc3 => ToArc3(),
            CutsceneId.Ending => Ending(),
            CutsceneId.Tutorial => Tutorial(),
            _ => System.Array.Empty<Beat>()
        };

        /// <summary>Cari objek stasiun yang sedang aktif di lokasi sekarang, kalau ada.</summary>
        static Vector3? PosOf(string name)
        {
            var go = GameObject.Find(name);
            return go != null ? go.transform.position : (Vector3?)null;
        }

        IEnumerable<Beat> Tutorial()
        {
            var ibu = Face(database != null ? database.motherSkin : null);

            // Warung muat penuh di satu layar pada zoom normal, jadi Focus() ke tiap
            // stasiun tidak akan terlihat bergerak (kamera sudah mentok batas ruangan).
            // Zoom masuk sementara supaya perpindahan antar stasiun benar-benar terlihat,
            // lalu kembalikan ukurannya di akhir.
            var cam = cameraFollow != null ? cameraFollow.GetComponent<Camera>() : null;
            float originalSize = cam != null ? cam.orthographicSize : 0f;
            cameraFollow?.SetSize(2.6f);

            yield return Line("Ibu", "Sebelum ibu tinggal, ibu tunjukkan dulu alurnya ya. Sekali ini saja.", ibu);

            yield return Focus(PosOf("SumberAyam"));
            yield return Line("Ibu", "Ambil AYAM MENTAH dari sini dulu.", ibu);

            yield return Focus(PosOf("Penggorengan1"));
            yield return Line("Ibu", "Taruh di PENGGORENGAN, tahan tombolnya. Angkat pas sudah matang -- kelamaan sedikit saja bisa gosong.", ibu);

            yield return Focus(PosOf("Cobek1"));
            yield return Line("Ibu", "Ayam matang dibawa ke sini, ke COBEK. Tahan tombolnya untuk diulek jadi geprek.", ibu);

            yield return Focus(PosOf("SumberNasi"));
            yield return Line("Ibu", "Jangan lupa NASI dan sambal atau pelengkap lain sesuai pesanan pelanggan.", ibu);

            yield return Focus(PosOf("MejaPenyajian"));
            yield return Line("Ibu", "Susun semuanya di MEJA PENYAJIAN sampai lengkap sesuai resep yang dipesan.", ibu);

            yield return Focus(PosOf("Meja1"));
            yield return Line("Ibu", "Antar piring yang sudah jadi ke meja pelanggan yang cocok pesanannya. Lihat gelembung di atas kepala mereka.", ibu);

            yield return Focus(PosOf("Kasir"));
            yield return Line("Ibu", "Kalau sudah selesai makan, mereka bayar sendiri di sini.", ibu);

            cameraFollow?.SetSize(originalSize);
            yield return Focus(null);
            yield return Line("Ibu", "Segitu saja. Sisanya... coba sendiri. Ibu tunggu di rumah, semangat ya!", ibu);
        }

        IEnumerable<Beat> Opening()
        {
            var dosen = Face(database != null ? database.lecturerSkin : null);
            var ibu = Face(database != null ? database.motherSkin : null);
            var aku = Face(database != null ? database.playerSkin : null);

            yield return Fade(true, 0.01f);
            yield return Title("KAMPUS", "Mata kuliah kewirausahaan", 2.4f);

            yield return Line("Dosen", "Tugas semester ini sederhana. Bangun bisnis yang benar-benar jalan.\nBukan proposal, bukan presentasi.", dosen);
            yield return Line("Dosen", "Saya mau lihat laporannya tiap akhir periode. Angkanya harus nyata.", dosen);
            yield return Line("Kamu", "Bisnis... modalnya dari mana?", aku);

            yield return Go(LocationId.Home);
            yield return Fade(false, 0.6f);
            yield return Wait(0.5f);

            yield return Line("Ibu", "Kok bengong dari tadi? Ada apa?", ibu);
            yield return Line("Kamu", "Dapat tugas bikin bisnis, Bu. Bingung mulai dari mana.", aku);
            yield return Line("Ibu", "Warung ibu di depan masih ada. Sudah lama nggak dipakai.", ibu);
            yield return Line("Ibu", "Mau belajar bisnis? Mulai saja dari sini.", ibu);

            yield return Fade(true, 0.5f);
            yield return Go(LocationId.Warung);
            yield return Focus(new Vector3(0f, 0.4f, 0f));
            yield return Fade(false, 0.7f);
            yield return Wait(1.2f);

            yield return Title("GEPREK!", "Hari 1 — Awal Mula", 2.8f);
            yield return Focus(null);
        }

        IEnumerable<Beat> ToArc2()
        {
            var ibu = Face(database != null ? database.motherSkin : null);
            var aku = Face(database != null ? database.playerSkin : null);

            yield return Fade(true, 0.45f);
            yield return Go(LocationId.Warung);
            yield return Focus(new Vector3(0f, -1.2f, 0f));
            yield return Fade(false, 0.55f);
            yield return Wait(0.8f);

            yield return Line("Ibu", "Tadi ada empat orang pulang karena nggak kebagian tempat.", ibu);
            yield return Line("Ibu", "Kalau terus seperti ini, tempat ini tidak akan cukup.", ibu);
            yield return Line("Kamu", "Uangnya sudah lumayan, Bu. Mungkin sudah waktunya cari ruko.", aku);

            yield return Fade(true, 0.5f);
            yield return Title("LOKASI BARU TERBUKA", "RUKO", 2.6f);
            yield return Title("ARC 2", "Pengembangan", 2.2f);
            yield return Focus(null);
        }

        IEnumerable<Beat> ToArc3()
        {
            var ayah = Face(database != null ? database.fatherSkin : null);
            var aku = Face(database != null ? database.playerSkin : null);

            yield return Fade(true, 0.45f);
            yield return Go(LocationId.Home);
            yield return Fade(false, 0.55f);
            yield return Wait(0.6f);

            yield return Line("Handphone", "\"Laporan bisnismu sudah layak untuk ekspansi.\"", null);
            yield return Line("Ayah", "Angkanya sudah stabil tiga periode. Itu tandanya bukan kebetulan.", ayah);
            yield return Line("Kamu", "Berarti sekarang bukan soal masak lagi ya, Yah.", aku);
            yield return Line("Ayah", "Betul. Sekarang soal mengatur orang dan menjaga ritmenya.", ayah);

            yield return Fade(true, 0.5f);
            yield return Title("KESEMPATAN BARU", "RESTORAN", 2.6f);
            yield return Title("ARC 3", "Restoran", 2.2f);
        }

        IEnumerable<Beat> Ending()
        {
            var ibu = Face(database != null ? database.motherSkin : null);
            var dosen = Face(database != null ? database.lecturerSkin : null);

            yield return Fade(true, 0.45f);
            yield return Go(LocationId.Warung);
            yield return Fade(false, 0.6f);
            yield return Wait(0.8f);

            yield return Line("Dosen", "Laporanmu paling tebal di kelas. Dan isinya bukan karangan.", dosen);
            yield return Line("Ibu", "Warung ini dulu cuma punya dua meja. Sekarang lihat...", ibu);

            yield return Title("GEPREK!", "Tamat", 3f);
        }

    }
}
