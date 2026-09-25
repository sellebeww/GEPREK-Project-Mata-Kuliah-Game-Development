# Fase 3 — Verifikasi akhir Warung

[Screenshot sebelum–sesudah dan rekaman tahap gameplay](../Screenshots/VisualAudit_Phase3/comparison.html).

## Pengujian gameplay

Scene `Main`, lokasi `Locations/Warung`, Hari 1. Pengujian memakai input joystick/tombol
virtual yang sama dengan kontrol sentuh game. Pemain bergerak melalui `PlayerController`
dan `Rigidbody2D`, dengan collision dan pemilihan target aktif. Tidak ada teleport pemain,
penyuntikan barang, pemaksaan status pelanggan, atau percepatan timer pada alur lengkap.
Resep pembuka gratis diaktifkan pada progres sementara, seperti awal permainan;
NewGame/ContinueGame dan penyimpanan tidak dipanggil.

Alur yang diselesaikan:

1. Berjalan ke AYAM dan mengambil ayam mentah.
2. Memasukkan ayam ke GORENG, menunggu timer asli 5 detik, mengangkat ayam matang.
3. Memasukkan ayam ke GEPREK, menahan tombol sampai selesai, mengambil hasil.
4. Menaruh ayam geprek dan nasi di MEJA SAJI, mengangkat Geprek Original.
5. Mengantar ke pelanggan yang datang dan memesan secara alami.
6. Memastikan satu penjualan tercatat, uang dan omzet naik **Rp18.000**.
7. Mengamati pelanggan senang → makan → bayar → keluar; tempat duduk dilepas.
8. Berjalan ke KASIR dan membaca rekap satu penjualan.
9. Mengamati kesabaran menurun, ekspresi menunggu/kesal berubah, dan antrean terbentuk secara alami.

Sesuai implementasi game, uang dibukukan ketika pesanan yang benar diterima.
Animasi `Paying` menyusul setelah makan; KASIR menampilkan rekap, bukan memungut uang.
Tidak ada perubahan aturan transaksi pada fase ini.

[Trace run akhir](../Screenshots/VisualAudit_Phase3/runs/final/result.json) mencatat input,
barang bawaan, perpindahan, status pelanggan, payout, pemeriksaan, dan sampel posisi.
[Driver pengujian](../Tools/phase3_gameplay.cs) berjalan dalam memori melalui Unity MCP
`run_script`, di luar folder Assets. Run membekukan waktu setelah selesai, sebelum hari tutup.

## Masalah runtime yang ditemukan dan diperbaiki

| Temuan | Perbaikan |
| --- | --- |
| Bar GORENG/GEPREK pada Y lokal 1.42 tertutup HUD atas | Bar diturunkan ke Y lokal 0.72; ikon bahan dipindahkan ke (0.55, 0.53), di samping alat |
| Sprite ulekan menutup bagian tengah bar yang sudah diturunkan | `SortingGroup` pada meter memastikan bingkai/isi tampil di depan detail alat |
| Meter stasiun masih memakai bentuk lama | GORENG, GORENG 2, dan GEPREK memakai PNG bingkai/isi dari Fase 2 |
| Ikon laporan menimpa awal kalimat omzet | Panel toast diperlebar 520 → 600; teks mendapat ruang kiri khusus untuk ikon dan tetap satu baris dengan font terbaca |

Perubahan produk pada fase ini:

- `Assets/_Project/Editor/GeprekBuilder.WarungArtwork.cs`: posisi, artwork, dan sorting meter kerja;
  pemasangan ulang dan rebuild scene mempertahankan koreksi ini.
- `Assets/_Project/Scenes/Main.unity`: perubahan pada `Penggorengan1/Bar`, `Penggorengan2/Bar`,
  `Cobek1/Bar`, dan ketiga `ItemIcon` terkait.
- `Assets/_Project/Scripts/UI/UIRoot.Hud.cs`: jarak ikon/teks serta penyesuaian ukuran teks toast.

PNG, Customer.prefab, dan script gameplay lainnya tidak berubah pada fase ini.
Baseline hash dan [daftar perubahan](../Screenshots/VisualAudit_Phase3/changed_files.json)
tersimpan di folder verifikasi; perubahan pengguna dari sebelum pekerjaan tetap dipertahankan.

## Checklist akhir

- [x] Baseline 11 slot counter sejajar di Y 2.0625; grid 1/16, interval X 1.375, badan scale 0.85.
- [x] Pasangan kursi merah seragam, termasuk meja upgrade; nomor meja di depan papan.
- [x] Cone/panah/jam dekor yang dinonaktifkan tidak tampil; waypoint antrean tetap terhubung.
  Penanda antrean hanya outline lantai transparan.
- [x] Judul dan hari memakai Canvas; kartu masuk/keluar dan kembali tersembunyi.
- [x] Sembilan aset PNG mempertahankan ukuran final, PPU 128, Point, tanpa kompresi/mipmap.
- [x] Bar kerja terlihat di bawah HUD, tidak ditutup detail alat; ikon bahan juga berada di luar HUD atas.
- [x] Alur bahan → masak → geprek → susun → sajikan → bayar → rekap berhasil.
- [x] Posisi stasiun tetap selama gameplay; ekspresi dan bar kesabaran berubah mengikuti timer.
- [x] Hash save game tetap sama sebelum dan sesudah pengujian.

Pengujian akhir meliputi **20 pemeriksaan alur gameplay** dan
[91 pemeriksaan layout/UI/aset](../Screenshots/VisualAudit_Phase3/verification.json).
Seluruh **111 pemeriksaan lulus** pada run akhir (42.53 detik waktu gameplay).
Pemeriksaan tambahan memakai perpindahan sementara untuk mengecek titik pendekatan
dan toggle upgrade, lalu mengembalikannya; ini terpisah dari run input penuh di atas.

Overview akhir memakai kamera dan sudut yang sama dengan Fase 0:
(-0.07777786, -0.149999857, -10), orthographic size 4.4, screenshot 1600 × 900.
Selama gameplay, pergeseran kecil kamera mengikuti pemain sebagaimana perilaku game.
Pembanding judul memakai capture intro Fase 2; banner tidak berubah pada Fase 3 dan
transisinya diperiksa kembali. Run awal/intermediate disimpan untuk membuktikan masalah
sebelum koreksi; folder `runs/final` berisi hasil yang dipakai untuk penilaian akhir.

Ruang lingkup verifikasi: satu siklus Geprek Original di Warung Hari 1, antrean dan perubahan
kesabaran, serta checklist visual di atas. Ini bukan pengujian seluruh resep, arc, atau resolusi layar.

## Kondisi akhir editor

Play Mode dihentikan; `Main` aktif, tersimpan, dan tidak dirty. Kompilasi berhasil,
Console Unity saat selesai menunjukkan 0 error dan 0 warning; `git diff --check` lulus.
Hash save diperiksa ulang setelah keluar Play Mode dan tetap sama.
[Validasi artefak](../Screenshots/VisualAudit_Phase3/artifact_verification.json) memastikan
15 screenshot akhir berukuran 1600 × 900 dan semua tautan pembanding tersedia.
[Status editor](../Screenshots/VisualAudit_Phase3/final_editor_status.json) mencatat hasil akhir;
buffer MCP masih menyimpan satu error pipeline lama dari sebelum fase ini, terpisah dari Console Unity saat ini.
