# Audit visual Fase 0 — Warung

Audit 24 September 2026, melalui Unity MCP, Unity 6000.5.9f1.
Scene: `Assets/_Project/Scenes/Main.unity`; lokasi: `/Locations/Warung`.

**Status: audit selesai; menunggu “lanjut” untuk Fase 1.** Tidak ada perubahan
pada scene, prefab, script game, atau sprite sumber. Play Mode sudah dihentikan.
Artefak yang ditambahkan hanya laporan, inventaris, dan screenshot audit.

## Bukti sebelum perubahan

- [Kamera saat pertama terhubung, Edit Mode](../Screenshots/VisualAudit_Phase0/current_camera_before.png).
- [Game view Play Mode, menu](../Screenshots/VisualAudit_Phase0/current_play_menu_before.png).
- [Game view Warung, awal jam operasional](../Screenshots/VisualAudit_Phase0/warung_gameplay_before.png).
- [Game view judul pembuka yang menutupi dapur](../Screenshots/VisualAudit_Phase0/warung_title_before.png).

![Warung sebelum perbaikan](../Screenshots/VisualAudit_Phase0/warung_gameplay_before.png)

![Judul pembuka sebelum perbaikan](../Screenshots/VisualAudit_Phase0/warung_title_before.png)

Fase 0 belum memiliki gambar “sesudah perbaikan”. Dua gambar di atas menunjukkan
kondisi awal pada dua state berbeda, bukan perbandingan hasil perubahan.

Screenshot PNG 1600×900. Kamera awal Edit Mode berada di `(1, -0.1000, -10)`,
orthographic size `4.4`, sehingga AYAM terpotong di tepi kiri. Saat jam operasional,
kamera mengikuti pemain dan berada di `(-0.077778, -0.1500, -10)`, size `4.4`;
capture judul menggunakan `(0, -0.1500, -10)`, size `4.4`.
Gunakan state dan posisi kamera yang sesuai ketika mengambil pembanding Fase 1/3.
Metadata ukuran Game view dan camera disimpan pada JSON; jangan mengasumsikan ukuran
panel Editor selalu sama dengan resolusi PNG keluaran.

Untuk audit runtime, `BeginDay()` dijalankan pada progres sementara, tutorial
dilewati, resep gratis awal disiapkan, dan waktu dibekukan pada 09:00.
`NewGame()`/`ContinueGame()` tidak dipanggil; `_sessionStarted` tetap false sehingga
keluar Play Mode tidak menimpa save. Capture judul berasal dari coroutine Opening
asli dengan dialog dipercepat dan Editor dipause tepat pada judul “GEPREK!”.
Pause tersebut hanya untuk menangkap gambar; bukan bukti judul macet secara alami.
Alur memasak–saji–bayar belum diuji pada fase audit ini.

## Cakupan inventaris

- Seluruh `Locations`: **1.100 Transform dan 690 SpriteRenderer**, termasuk lokasi
  tidak aktif, anak stasiun, dekorasi, serta waypoint.
- Warung: **283 Transform dan 179 SpriteRenderer**; 160 renderer memiliki sprite,
  sisanya slot ikon/dish yang memang diisi saat runtime.
- Semua transform lokal/dunia, rotasi, scale lokal/dunia, layer GameObject,
  sorting layer/order, status aktif/enabled, ukuran bounds, asset/sprite,
  sprite rect, pivot pixel/normalisasi, PPU, filter, dan kompresi dicatat.
- TextMesh dunia dan Canvas runtime diinventarisasi terpisah.

Data lengkap:

- [Inventaris Edit Mode](../Screenshots/VisualAudit_Phase0/scene_inventory_edit.json).
- [Inventaris Play Mode](../Screenshots/VisualAudit_Phase0/scene_inventory_play.json).
- [Semua Transform, CSV](../Screenshots/VisualAudit_Phase0/transforms_edit.csv).
- [Semua SpriteRenderer, CSV](../Screenshots/VisualAudit_Phase0/sprites_edit.csv).
- [SpriteRenderer Warung dengan baseline piksel opaque](../Screenshots/VisualAudit_Phase0/warung_sprites.csv).
- [Canvas dan teks runtime](../Screenshots/VisualAudit_Phase0/ui_inventory_play.json).
- [Referensi script ke waypoint/dekorasi](../Screenshots/VisualAudit_Phase0/marker_references.json).
- [State capture judul](../Screenshots/VisualAudit_Phase0/title_capture_state.json).

## Posisi dan ukuran stasiun

Tabel berikut memakai posisi **world root stasiun**, scale **renderer badan**.
Root stasiun memiliki scale `(1,1,1)`, badan local position `(0,0,0)`, Z=0,
rotation=0. Semua badan pada tabel memakai sorting layer `Default` dan pivot
normalisasi `(0.5,0)` (bawah-tengah). Scale Z badan=1.

| Label / hierarchy Kitchen | X | Y | Scale badan XY | Order badan | Sprite rect px |
|---|---:|---:|---:|---:|---|
| AYAM / SumberAyam | -7.0813 | 2.0500 | 0.85 | -205 | fridge_small 101×125 |
| NASI / SumberNasi | -5.8460 | 2.0500 | 0.85 | -205 | rice_cooker 110×118 |
| GORENG / Penggorengan1 | -4.1080 | 2.0500 | 0.85 | -205 | fryer 109×132 |
| GORENG 2 / Penggorengan2 | -2.8727 | 2.0500 | 0.85 | -205 | fryer 109×132 |
| GEPREK / Cobek1 | -1.6375 | 2.0500 | 0.85 | -205 | cobek_station 120×118 |
| SAMBAL / SumberSambalMerah | 0.5027 | 2.0500 | 0.85 | -205 | counter_sambal 132×119 |
| S. IJO / SumberSambalIjo | 1.7380 | 2.0500 | 0.85 | -205 | cobek_stand 105×142 |
| TELUR / SumberTelur | 2.9733 | 2.0500 | 0.85 | -205 | prep_table 137×112 |
| TAHU / SumberTahu | 4.2085 | 2.0500 | 0.85 | -205 | cabinet_meat 118×118 |
| LALAPAN / SumberLalapan | 5.4438 | 2.0500 | 0.85 | -205 | veg_basket 137×120 |
| KEJU / SumberKeju | 6.6791 | 2.0500 | 0.85 | -205 | display_case 139×137 |
| SAMPAH / TempatSampah | -4.6273 | 0.3500 | 0.8925 | -35 | bin_blue 98×137 |
| PREP / MejaPrep/Meja | -1.7652 | 0.3500 | 0.85 | -35 | prep_table 137×112 |
| MEJA SAJI / MejaPenyajian | 2.5280 | 0.3500 | 1.00 | -35 | warming_table 146×98 |
| CUCI PIRING / AreaCuci/Wastafel | 4.1140 | 0.3500 | 0.80 | -35 | sink_double; lihat CSV |
| KASIR / Kasir | 5.7000 | 0.3500 | 0.95 | -35 | counter 142×106 |

## (a) Baseline dan alignment

**Tidak ditemukan stasiun utama dapur yang naik/turun sendiri pada baseline kaki.**
Kesebelas badan countertop memiliki Y=2.05. Pengukuran alpha >127 pada sprite sumber
memberi baseline visual yang sama, **Y≈2.0633** (padding bawah dua piksel).
GORENG dan GORENG 2 identik posisi Y, scale, pivot, serta sprite.
Tinggi paling atas sprite berbeda karena bentuk alatnya; itu bukan bukti baseline salah.
Permukaan kerja di dalam gambar perlu dinilai secara visual, bukan disamakan dengan
ujung atas kulkas, rice cooker, atau ulekan.

Yang perlu dirapikan:

1. **Grid horizontal antarzona tidak seragam.** Jarak dalam zona ≈1.2353 unit,
   NASI→GORENG ≈1.7380, GEPREK→SAMBAL ≈2.1402. `SlotsIn()` menghitung slot per zona
   dengan margin relatif, bukan satu grid bersama. Fase 1 perlu mempertahankan ruang
   interaksi/collider sambil menyamakan grid dan jarak yang disengaja.
2. **KardusSupplier memiliki pivot pusat**, `(0.5,0.5)`, sementara dekor lantai besar
   memakai pivot bawah-tengah. Root Y=-3.85 tampak sebaris dengan RakPiring, tetapi
   batas bawah sprite sekitar -4.185, dibanding RakPiring -3.85 dan tanaman -3.90.
   Perbedaan baseline visual ini perlu disesuaikan dari bounds/pivot.
3. **JamDinding** berada di `(-7.30,1.75)`, order -1790: muncul rendah di bawah/dekat
   kulkas AYAM, bukan di area dinding. Tidak ditemukan referensi gameplay serialized
   ataupun lookup nama pada script runtime; kandidat disembunyikan/dihapus dari dekor.
4. **OrderRail** di atas MEJA SAJI berada sekitar Y=1.97, order -1800: tampak sebagai
   tiga kertas menggantung di ruang countertop yang kosong pada awal permainan.
   Ini dekor rel pesanan, bukan panah debug. Perlu ditata bersama stasiun penyajian.
5. **Pojok kanan padat.** Jarak pusat SAMPAH→PREP≈2.862 dan PREP→MEJA SAJI≈4.293,
   sedangkan MEJA SAJI→CUCI PIRING dan CUCI PIRING→KASIR masing-masing≈1.586.
   MenuBoard–RakMinum, RakMinum–Dispenser, serta RakPiring–TanamanKanan memiliki
   perpotongan bounds sprite (masing-masing ≈0.396×0.434, 0.174×0.209, 0.149×0.656
   unit). Bounds mencakup area transparan; ini indikator kepadatan, bukan ukuran
   piksel opaque yang saling menutup. Screenshot memperlihatkan cluster rapat.

## (b) Scale dan konsistensi kelas ukuran

**Tidak ada outlier scale signifikan pada enam stasiun utama yang diminta.**
Semuanya 0.85; dua penggorengan identik. Meja island menggunakan 0.80–1.00,
namun tinggi opaque PREP≈0.717, SAJI≈0.734, KASIR≈0.757, CUCI≈0.838 unit.
Perbedaan scale transform itu sebagian mengompensasi ukuran sprite sumber;
menyamakan semua angka scale justru belum tentu menyamakan ukuran visual.

- **Kursi:** seluruh StoolL memakai `stool_red`, StoolR `stool_teal`, scale XY≈0.72,
  pivot `(0.5,0)`. Warna ditentukan builder dan tidak dipakai sebagai penanda VIP
  oleh logika Seat. Pasangannya perlu diseragamkan sesuai permintaan.
- **Papan nomor meja:** scale `(0.22,0.62,1)` ikut diwariskan ke TextMesh angka,
  membuat angka terkompres secara horizontal. Ini masalah rasio label, di samping
  masalah sorting di bawah.
- Meja kayu dan bundar bergantian memakai scale 0.9; perbedaan tinggi sprite
  sekitar 10%, bukan outlier scale acak. Tidak perlu menyimpulkan semuanya salah ukuran.

## (c) Sorting, UI, dan kebocoran visual

| Objek | Bukti | Implikasi Fase 1 |
|---|---|---|
| Meja1..5/NomorMeja/Label | TextMesh order **0**, Table **292**, NomorMeja **294**; kotak nomor tampak kosong | Angka harus digambar di atas papan, dan skala teks dipisahkan dari peregangan papan |
| Penggorengan2/NamePlate/Label | Body dan NamePlate disabled saat belum upgrade; TextMesh tetap terlihat | `UpgradeToggle` hanya menonaktifkan SpriteRenderer saat array visuals kosong; sembunyikan teks bersama visual terkunci |
| /UI/Cutscene/TitleCard | Sudah **Canvas ScreenSpaceOverlay**, order **10000**, di tengah layar | Tata ulang menjadi banner/card dengan slide/fade; tidak perlu migrasi dari world-space |
| Judul GEPREK! / Hari 1 — Awal Mula | Capture intro asli menutup area dapur; `BeatTitle.hold=2.8`, lalu `TitleHidden` | Overlap terkonfirmasi saat judul aktif; judul macet permanen belum terbukti |
| /UI/Hud/.../Day | Sudah teks Canvas HUD di atas layar | Bedakan label hari HUD yang memang tetap ada dari banner perkenalan hari |
| CustomerSpawner/PembatasAwal, PembatasAkhir | Sprite `bollard`, order 115, layer GameObject Default | Penampilan mirip kerucut jalan terlihat di Game view; hanya dekor dengan SortingByY, bukan waypoint |
| CustomerSpawner/QueueMark0..2 | Sprite `floor_queue`, order -1993, alpha 0.55 | Marker lantai berupa cincin/panah kecil; bisa dihaluskan tanpa membuang titik antrean |
| CustomerSpawner/Queue0..2 | Transform tanpa SpriteRenderer; direferensikan `queueSpots` | Pertahankan logic waypoint; visual marker merupakan objek saudara terpisah |

Kamera memiliki cullingMask=-1 (semua layer). Semua SpriteRenderer Warung yang
diinventarisasi memakai sorting layer `Default`; pemisahan kedalaman memakai angka
order dan `SortingByY`. Script sorting tersebut hanya menangani SpriteRenderer,
sehingga TextMesh anak tidak ikut urutan dinamis kelompoknya. Label nama stasiun
tetap boleh berada di dunia; yang diminta menjadi banner adalah judul/perkenalan hari.

Bilah hijau di atas banyak stasiun terlihat pada screenshot **Edit Mode** awal,
tetapi hilang pada Play Mode lewat `WorldProgressBar.Awake()`. Jangan menyatakan
semuanya sebagai debug yang bocor saat gameplay. Tidak ditemukan GameObject
panah debug terpisah; bentuk panah yang terkonfirmasi berasal dari tekstur QueueMark.

## Acuan aset untuk Fase 2

Semua 160 sprite terpasang pada Warung: **PPU 128, Filter Mode Point,
Texture Compression Uncompressed**. Gaya utama yang terlihat: tampak atas-miring,
outline gelap, kayu cokelat hangat, aksen merah/kuning, benda dapur abu-abu.

| Aset existing | Ukuran px |
|---|---|
| mood_happy / neutral / annoyed / angry (sheet Sign) | 119×118 / 118×119 / 118×118 / 117×118 |
| bubble | 150×130 |
| bar_bg / bar_fill | 136×24 / 128×16 |
| plaque / label_strip | 160×44 / 160×30 |
| fryer / cobek_station / counter_sambal | 109×132 / 120×118 / 132×119 |

Sprite existing dipotong rapat dengan ukuran bervariasi; **tidak ada satu kelipatan
ukuran frame yang sudah berlaku pada semua aset**. Acuan yang pasti adalah PPU 128
dan filter Point. Rekomendasi saat Fase 2 disetujui: kanvas ikon 128×128, dekorasi
berbasis 128 px sesuai proporsi, serta ukuran bar/card kelipatan 8/16, dengan
ukuran dunia dan ketebalan outline dicocokkan ke screenshot. Ini usulan, belum aset
baru. Progress bar sekarang memang sprite buatan proyek melalui WorldProgressBar,
bukan komponen progress bar default Unity; tampilannya tetap dapat diperbaiki.

## Sumber yang relevan untuk fase berikutnya

- `Assets/_Project/Editor/GeprekBuilder.Business.cs`: Layout, slot/grid, posisi stasiun.
- `Assets/_Project/Editor/GeprekBuilder.BusinessParts.cs`: dekor, kursi, plakat, nomor meja, marker antrean.
- `Assets/_Project/Scripts/Stations/UpgradeToggle.cs`: kebocoran teks stasiun terkunci.
- `Assets/_Project/Scripts/World/SortingByY.cs` dan `WorldLabel.cs`: urutan renderer/teks.
- `Assets/_Project/Scripts/UI/UIRoot.Cutscene.cs`: judul Canvas dan animasi banner.
- `Assets/_Project/Scripts/UI/UIRoot.Hud.cs`: label hari HUD.
- `Assets/_Project/Editor/GeprekBuilder.Prefabs.cs`, `Customers/OrderBubble.cs`,
  `World/WorldProgressBar.cs`: bubble pelanggan dan bar untuk Fase 2.

Perubahan nanti harus mempertimbangkan scene builder bersama Warung/Ruko/Restaurant;
menggeser scene saja akan hilang saat Build Everything dijalankan. Fase 0 belum
menjalankan Build Everything, mengubah aset, atau melakukan perbaikan apa pun.
