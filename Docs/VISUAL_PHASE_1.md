# Fase 1 — Penempatan, alignment, dan UI Warung

**Selesai. Menunggu “lanjut” sebelum Fase 2.** Perubahan tersimpan di
`Assets/_Project/Scenes/Main.unity`; Unity kembali ke Edit Mode.

[Buka pembanding interaktif sebelum–sesudah](../Screenshots/VisualAudit_Phase1/comparison.html).

| Gameplay sebelum | Gameplay sesudah |
|---|---|
| ![Sebelum](../Screenshots/VisualAudit_Phase0/warung_gameplay_before.png) | ![Sesudah](../Screenshots/VisualAudit_Phase1/warung_gameplay_after.png) |

| Judul sebelum | Judul sesudah |
|---|---|
| ![Sebelum](../Screenshots/VisualAudit_Phase0/warung_title_before.png) | ![Sesudah](../Screenshots/VisualAudit_Phase1/warung_title_after.png) |

## Perubahan objek

| Objek/kelompok | Hasil |
|---|---|
| 11 stasiun countertop, termasuk slot upgrade | Grid dasar 1/16 unit, jarak X 1.375 unit, baseline Y=2.0625, badan scale 0.85 dan pivot bawah-tengah |
| CounterTop, CounterBlock, pita zona | Mengikuti baseline dan batas zona baru, tanpa mengubah referensi stasiun |
| SAMPAH, PREP, MEJA SAJI, CUCI PIRING, KASIR | X=-5.5, -2.75, 0, 2.75, 5.5; Y=0.375; jarak sama 2.75 unit |
| JamDinding, pembatas antrean, papan MASUK yang berulang | Disembunyikan; objek tetap dapat ditemukan di Hierarchy |
| QueueMark0..2 | Menggunakan sprite outline `fx_ring` yang sudah ada, alpha 0.18; tidak lagi memakai ikon panah |
| Queue0..2 | Posisi dan referensi `queueSpots` dipertahankan untuk logic pelanggan |
| Meja1..5/StoolL, StoolR | Kedua kursi merah dengan sprite dan ukuran sama; termasuk meja hasil upgrade |
| Meja1..5/NomorMeja/Label | Angka order 295, papan 294; proporsi glyph kembali 1:1 |
| Penggorengan2 | Teks, badan, dan collider ikut terkunci/terbuka; bar, highlight, dan sizzle tidak menyala sendiri saat unlock |
| RakMinum, Dispenser | Satu kolom X=6.875 dengan jarak vertikal; papan menu dipindahkan ke dinding |
| RakPiring, tanaman, sampah tamu, KardusSupplier | Dekor bawah diberi jarak; pivot KardusSupplier dikompensasi agar bagian bawah sejajar |
| DetailKasir | Nota dan receh ditempatkan di atas meja kasir |
| AlatMakan, OrderRail | Dekor tanpa fungsi gameplay disembunyikan agar tidak mengambang di sela meja |
| PlayerSpawn | Digeser ke celah antarmeja kerja, X=-4.125, agar pemain awal tidak menutup papan PREP |
| UI/Cutscene/TitleCard | Card Canvas 560×64 pada bilah sinematik atas, slide/fade masuk dan keluar masing-masing 0.28 detik |

Judul sebelumnya **sudah** berada di Canvas. Fase ini memperbaiki tata letak dan
animasinya. Label hari HUD tetap tersedia; judul/nama hari pembuka tidak lagi
menutupi area dapur. Tampilan card menggunakan elemen UI yang sudah ada; PNG baru
dan penggantian art pelanggan/progress bar tetap menunggu Fase 2.

Inventaris sesudah: [layout_after.json](../Screenshots/VisualAudit_Phase1/layout_after.json).
Rincian per properti: [object_changes.csv](../Screenshots/VisualAudit_Phase1/object_changes.csv).

## Script yang diubah

- `Editor/GeprekBuilder.WarungLayout.cs` (baru): satu definisi layout Warung untuk
  builder dan menu `Geprek/3. Rapikan Visual Warung`. Dapat diterapkan ulang tanpa
  membuat duplikat; aplikasi pada scene dibungkus satu Undo group.
- `Editor/GeprekBuilder.Business.cs`: menjalankan layout Warung setelah objek dibangun.
- `Editor/GeprekBuilder.BusinessParts.cs`: proporsi papan dan angka nomor meja.
- `Editor/GeprekBuilder.Scene.cs`: posisi awal pemain sesuai lorong layout baru.
- `Scripts/World/WorldLabel.cs`: teks mengikuti sorting layer/order papan induknya.
- `Scripts/Stations/UpgradeToggle.cs`: toggle visual mencakup seluruh subtree,
  termasuk TextMesh, sambil menjaga status renderer efek yang diatur script lain.
- `Scripts/Core/CutsceneDirector.cs`: mengirim durasi judul ke UI.
- `Scripts/UI/UIRoot.Cutscene.cs`: layout card, CanvasGroup, animasi masuk/keluar,
  serta cleanup judul saat dialog atau cutscene selesai.

Path script di atas relatif terhadap `Assets/_Project/`. Penataan objek dibatasi
ke Warung; perbaikan perilaku WorldLabel dan UpgradeToggle berlaku pada komponen
yang sama di lokasi lain. Build Everything akan mempertahankan layout baru.
Perubahan pengguna yang sebelumnya ada pada PlayerController, PlayerInteractor,
CookStation, PrepStation, dan UIRoot.Menus tetap utuh, diverifikasi dengan hash.

## Verifikasi

**69 pemeriksaan lulus, 0 gagal**, melalui Unity MCP di Play Mode:

- Seluruh baseline/grid/scale stasiun dan jarak slot seragam.
- Semua pasangan kursi cocok; angka meja berada di depan papan dengan proporsi benar.
- Pembatas/jam tersembunyi, waypoint tetap terhubung, marker lantai transparan.
- Penggorengan upgrade diuji locked → unlocked → locked, termasuk teks dan collider.
- Delapan titik pendekatan (AYAM, NASI, GORENG, GORENG 2, GEPREK, SAMBAL, SAJI, KASIR)
  diuji melalui `PlayerInteractor.Scan()` dan pemeriksaan benturan kapsul pemain.
- Banner diuji pada fase tersembunyi → masuk → tampil → keluar → hilang. Bounds
  saat tampil seluruhnya berada dalam bilah sinematik atas.

Pengujian pendekatan memindahkan pemain lewat kode dan memakai barang bawaan yang
sesuai untuk pemilihan target; bukan uji kontrol keyboard atau alur memasak lengkap.
Alur bahan → masak → saji → bayar tetap menjadi pekerjaan Fase 3.

Intro Opening asli juga dijalankan dengan dialog dipercepat: **140 sampel animasi**
terekam, capture masuk/tampil/keluar berhasil, coroutine selesai, dan title card
otomatis tidak aktif. Tidak ada NewGame/ContinueGame atau penulisan save game.

- [Hasil 69 pemeriksaan](../Screenshots/VisualAudit_Phase1/verification.json).
- [Trace intro aktual](../Screenshots/VisualAudit_Phase1/title_animation_trace.json).
- [Script verifikasi](../Tools/verify_visual_phase1.cs).
- [Script capture intro](../Tools/capture_title_phase1.cs).

Kompilasi Unity lulus; Console saat akhir verifikasi memiliki 0 error dan 0 warning.
`git diff --check` lulus. Buffer historis MCP masih memuat timeout refresh dari
Fase 0; tidak ada error baru pada pengujian Fase 1.

Screenshot sebelum/sesudah sama-sama 1600×900 dengan orthographic size 4.4.
Gameplay menggunakan kamera `(-0.077778,-0.15,-10)`, judul `(0,-0.15,-10)`, sama
dengan referensi Fase 0. Posisi awal pemain berbeda karena termasuk perbaikan layout.
Screenshot judul baru diambil dari Game-view encoder Unity MCP selama animasi
berjalan normal, tanpa mempause judul untuk capture.
