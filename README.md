# GEPREK!

Game 2D top-down **time management + business simulation** berdasarkan GDD Kelompok 4.
Pemain berperan sebagai mahasiswa bisnis yang membangun usaha ayam geprek dari warung
milik ibunya, pindah ke ruko, lalu membuka restoran dengan cabang.

- **Engine:** Unity 6000.5.9f1, URP 2D, Input System baru
- **Scene utama:** `Assets/_Project/Scenes/Main.unity`
- **Dokumen sumber:** `Docs/GDD - Game Design Document - Kelompok 4.pdf`, `Arahan.md`

---

## Cara menjalankan

1. Buka project di Unity Hub.
2. Buka `Assets/_Project/Scenes/Main.unity`.
3. Tekan Play.

Scene ini satu-satunya yang terdaftar di Build Settings, jadi build standalone langsung
masuk ke menu utama.

> **Catatan pengembangan:** jangan me-minimize jendela Unity saat Play — Unity berhenti
> menjalankan frame dan game terlihat seperti membeku.

## Kontrol

| Aksi | Keyboard | Sentuh / mouse |
|---|---|---|
| Jalan | `WASD` atau panah | Joystick kiri bawah (muncul saat disentuh) |
| Interaksi | `E` atau `Spasi` | Tombol bulat merah kanan bawah |
| Geprek (tahan) | Tahan `E` / `Spasi` | Tahan tombol interaksi |
| Lanjut dialog | Tombol apa saja | Klik di mana saja |
| Jeda | `Esc` | Tombol jeda kanan atas |

Objek yang bisa dipakai ditandai **cincin emas di kakinya**, dan kata kerjanya muncul
di kotak petunjuk kanan bawah.

---

## Tata letak dan alur kerja

Dapur dibagi tiga zona yang terbaca langsung dari layar — ditandai pita warna di lantai
dan papan nama di bibir meja kerja:

```
  BAHAN            MASAK                   SAMBAL & PELENGKAP
 ┌────────┐  ┌──────────────────┐  ┌──────────────────────────────────┐
 │ AYAM   │  │ GORENG  GORENG 2 │  │ SAMBAL  S.IJO  TELUR  TAHU  ...  │  ← meja kerja
 │ NASI   │  │ GEPREK           │  │                                  │
 └────────┘  └──────────────────┘  └──────────────────────────────────┘
      [SAMPAH]                [MEJA SAJI]          [KASIR]               ← lantai dapur
 ──────────────────── lorong pelanggan ────────────────────────────────
  [pintu + keset + antrean]   [meja bernomor 1..n]
```

Alur memasak:

```
Kulkas Ayam → Penggorengan → Cobek → Meja Penyajian → Pelanggan
 (ayam mentah)  (ayam goreng)  (geprek)  (+ nasi, sambal, pelengkap)
```

Stasiun pelengkap baru muncul setelah resep yang memakainya terbuka, jadi dapur ikut
tumbuh bersama bisnis.

### Menggoreng punya keputusan, bukan hanya timer

| Waktu angkat | Hasil | Efek |
|---|---|---|
| Di jendela awal setelah matang | **PAS** | kualitas penuh, percikan bintang |
| Setelah jendela lewat | Lumayan | kualitas turun ~18% |
| Kelewat batas aman | **GOSONG** | asap, layar bergetar, hanya bisa dibuang |

---

## Siklus permainan

```
Menu → Cutscene pembuka → Briefing hari → Jam operasional → Laporan harian
                                ^                                  │
                                │                                  v
                        Tidur ← Malam di rumah (Ibu/Ayah/Medsos/Toko/Cabang)
```

Tiap **arc berisi 5 hari**. Di akhir arc ada laporan ke dosen; kalau lulus, cutscene
transisi memindahkan usaha ke lokasi berikutnya.

| Arc | Lokasi | Yang berubah |
|---|---|---|
| 1 — Perintis | **Warung** (16×8) | 1–2 penggorengan, 1 cobek, 5 meja, perabot kayu sederhana |
| 2 — Pengembangan | **Ruko** (21×9) | 2–3 penggorengan, 2 cobek, 8 meja dua baris, perabot modern, karyawan terbuka |
| 3 — Restoran | **Restoran** (26×10) | 3–4 penggorengan, 2 meja penyajian, 12 meja, booth, cabang terbuka |

Ketiganya adalah peta terpisah yang dibangun dari satu pembangun yang sama
(`BusinessSpec`), jadi alur kerjanya konsisten sementara skalanya benar-benar berubah.

---

## Sistem yang berjalan

**Waktu & suasana**
- Satu hari operasional = 200 detik nyata, ditampilkan sebagai jam 09:00–17:00.
- Warna layar bergeser pelan dari pagi yang sejuk → siang terang → sore menghangat.
- **Jam sibuk** di tengah hari: pelanggan datang lebih rapat, kesabaran sedikit turun.
  Rentangnya ditandai di bilah jam supaya bisa diantisipasi.

**Pelanggan**
- 9 tipe dengan kesabaran, bayaran, dan bobot kemunculan berbeda.
- Alur: `masuk → antre → duduk → pesan → tunggu → senang/marah → makan → bayar → pulang`.
- Saat duduk mereka sesekali menoleh dengan jeda acak per orang; kecepatan jalannya juga
  divariasikan, jadi ruangan tidak terasa mati.
- **Food Vlogger** adalah tamu istimewa yang hanya datang kalau diundang lewat media
  sosial. Dilayani sempurna → reputasi melonjak; dibiarkan kecewa → reputasi turun.

**Target per hari**
- Selain omzet, tiap hari punya target tambahan: layani sekian pelanggan, jaga kepuasan
  di atas sekian persen, jual sekian porsi menu tertentu, batasi pelanggan yang kabur,
  atau capai sekian pelayanan sempurna.
- Laporan harian memberi **bintang 1–3**: omzet tercapai, semua target tambahan tercapai,
  dan kepuasan rata-rata di atas 80%.

**Ekonomi & progresi**
- XP dari tiap pesanan menaikkan level memasak → kualitas porsi → harga jual.
- Reputasi naik dari pelayanan sempurna dan promosi, turun saat pelanggan kabur.
- 7 upgrade: kecepatan jalan, kompor, ulekan, penggorengan tambahan, meja tambahan,
  kenyamanan, dan branding.
- 8 resep: 2 gratis, 4 dibeli di toko, 2 hadiah dari Ibu.

**Media sosial (malam)**
| Pilihan | Biaya | Efek besok |
|---|---|---|
| Posting foto | gratis | reputasi +3% |
| Promosi berbayar | Rp12.000 | pelanggan +12% |
| Umumkan diskon | gratis | pelanggan +25%, harga jual −18% |
| Undang food vlogger | Rp40.000 | tamu istimewa datang |

**Karyawan (Arc 2)**
- Tiga peran dengan tugas nyata: **Juru Masak** mengurus rantai ayam sampai geprek siap
  di meja penyajian, **Pramusaji** mengantar porsi jadi ke pelanggan yang paling mendesak,
  **Kasir** menjaga kasir dan menaikkan harga jual.
- Tiap karyawan punya tingkat, kecepatan, dan gaji harian yang dipotong saat tutup.

**Cabang (Arc 3)**
- Cabang tidak dimainkan langsung; setorannya masuk tiap hari, besarnya tergantung
  tingkat cabang, jumlah karyawan yang ditempatkan, dan reputasi.
- Bisa ditingkatkan dan ditambah karyawannya lewat monitor di rumah.

**Cerita**
- Cutscene pembuka (~30 detik): tugas dari dosen → pulang → Ibu menawarkan warungnya →
  kartu judul → hari pertama.
- Cutscene transisi Arc 1→2 dan Arc 2→3, serta penutup setelah arc terakhir.
- Dialog dengan Ibu (resep), Ayah (modal & saran), dan handphone.

**Umpan balik visual**
- Uap dan cipratan minyak di penggorengan, asap saat mulai gosong, debu tiap tumbukan
  ulekan, percikan bintang untuk hasil sempurna.
- Teks mengambang di tempat kejadian: `+Rp18.000`, `PERFECT`, `GOSONG!`, `VIRAL!`.
- Getaran kamera saat ayam gosong, sentakan skala saat porsi lengkap.

**Audio berlapis**
- Musik: menu, siang, malam.
- Ambience: dengung dapur (tetap), gumam pelanggan (kerasnya mengikuti keramaian),
  suara jalanan saat malam di rumah.
- 15 efek suara untuk memasak, melayani, uang, upgrade, dan notifikasi.

**Lain-lain**
- Simpan otomatis ke JSON di `Application.persistentDataPath/geprek_save.json`.
- Kontrol sentuh (joystick + tombol) dan keyboard, sesuai target platform di GDD.

---

## Tangkapan layar

Ada di folder `Screenshots/`.

---

## Peta folder

```
Assets/_Project/
├── Art/
│   ├── Sprites/      6 sheet hasil potong (416 sprite bernama)
│   └── UI/           lantai, dinding, meja kerja, efek, bilah, balon (dibuat prosedural)
├── Audio/
│   ├── SFX/          15 efek suara
│   └── BGM/          3 musik + 3 ambience
├── Data/             ScriptableObject: item, resep, upgrade, pelanggan, hari, arc, config
├── Prefabs/          Player, Customer, Staff
├── Scenes/Main.unity
├── Scripts/
│   ├── Core/         GameManager, state, event bus, statistik harian, cutscene
│   ├── Data/         definisi ScriptableObject + target harian
│   ├── Player/       gerak, animasi, bawaan, interaksi, input
│   ├── Stations/     penggorengan, cobek, meja saji, sumber bahan, kasir, sampah, malam
│   ├── Customers/    pelanggan, spawner, kursi, balon pesanan
│   ├── Progression/  kemajuan, malam hari, medsos, karyawan, cabang
│   ├── Economy/      perhitungan bayaran
│   ├── Save/         simpan & muat
│   ├── UI/           seluruh antarmuka (dibangun lewat kode)
│   └── World/        lokasi, kamera, urutan gambar per-y, efek partikel
└── Editor/           alat build: potong sprite + bangun data/prefab/scene
```

---

## Membangun ulang isi game

Dua menu di menubar Unity, dijalankan berurutan **saat tidak sedang Play**:

1. **`Geprek/1. Setup Sprite Sheets`** — mengatur import setting (Point filter, PPU 128,
   tanpa kompresi) dan memotong keenam sheet memakai daftar kotak di `Tools/slices.json`.
2. **`Geprek/2. Build Everything`** — membuat ulang seluruh aset data, prefab, dan scene,
   termasuk ketiga lokasi usaha dan rumah.

Keduanya *idempotent*: aman dijalankan berkali-kali. Artinya isi game diubah lewat kode
di `Assets/_Project/Editor/` lalu dibangun ulang, bukan ditata manual di Inspector.

`Tools/verify_logic.cs` berisi **55 pemeriksaan logika** — pencocokan resep, perhitungan
bayaran, kurva XP, target harian, jam sibuk, tiga lokasi arc, gaji karyawan, setoran
cabang, dan efek diskon. Jalankan dengan:

```
unity cmd eval_file --file Tools/verify_logic.cs
```

## Menyetel keseimbangan

- `Assets/_Project/Data/Config/GameConfig.asset` — panjang hari, kesabaran dasar, ambang
  penilaian, modal awal, kurva XP, biaya promosi.
- `Assets/_Project/Data/Levels/Day_A*_*.asset` — target omzet, target tambahan, jumlah
  pelanggan, kecepatan kedatangan, dan rentang jam sibuk per hari.
- Ukuran dan isi tiap lokasi ada di `BusinessSpec` (`GeprekBuilder.Scene.cs`).

Mengubah aset lewat Inspector langsung berpengaruh; mengubah `BusinessSpec` perlu
menjalankan ulang build.

---

## Catatan & batasan

- **Aset asli** ada di `ArtSource/` (di luar `Assets/`, tidak ikut di-import). Sprite yang
  dipakai game adalah versi yang sudah dibersihkan: halo semi-transparan sisa AI dibuang,
  alpha dinormalkan, warna tepi dirambatkan keluar.
- **Audio dibuat prosedural** (sintesis sederhana), bukan rekaman. Cukup untuk prototipe;
  timpa file di `Audio/SFX` dan `Audio/BGM` dengan nama sama kalau mau diganti.
- **Kontrol keyboard/joystick belum diuji lewat input sungguhan** — pengujian otomatis
  menggerakkan pemain lewat kode. Rasa kontrolnya sebaiknya dicek dengan bermain normal.
- **Cabang bersifat pasif**: menghasilkan setoran harian, bukan peta yang bisa dimasuki.
- **Foto keluarga** sebagai penanda kenangan baru muncul di lokasi ber-`modernDecor`
  (ruko dan restoran); versi di rumah belum berpindah mengikuti arc.
