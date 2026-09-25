# Fase 2 — Artwork Warung

Scene: `Assets/_Project/Scenes/Main.unity`, lokasi `Locations/Warung`.

[Buka pembanding interaktif dan katalog aset](../Screenshots/VisualAudit_Phase2/comparison.html).

## Hasil

Sembilan PNG transparan dibuat dengan **built-in image_gen.imagegen**, lalu diimpor ke
`Assets/Sprites/Warung`. Palet krem, kayu cokelat, dan merah cabai mengikuti tema dapur.

| Aset | Ukuran PNG | Pemakaian |
| --- | --- | --- |
| `mood_happy.png` | 128 × 128 | Ekspresi puas |
| `mood_waiting.png` | 128 × 128 | Ekspresi menunggu |
| `mood_angry.png` | 128 × 128 | Ekspresi kesal, termasuk status annoyed |
| `order_bubble.png` | 128 × 96 | Latar pesanan pelanggan |
| `patience_frame.png` | 128 × 32 | Bingkai kayu bar kesabaran |
| `patience_fill.png` | 128 × 16 | Isi bar, ditint hijau / emas / merah berdasarkan nilai |
| `spice_shelf.png` | 128 × 128 | Pengganti `WallDecor/RakPelengkap` |
| `menu_board.png` | 128 × 160 | Pengganti `WallDecor/MenuBoard` |
| `day_banner.png` | 512 × 128 | Kartu Canvas untuk judul dan teks hari dinamis |

Semua memakai PPU **128**, Filter **Point**, **Uncompressed**, tanpa mipmap, dan alpha dari PNG.
Atlas lama memakai rect sprite bervariasi, bukan satu ukuran tile tetap; ukuran baru memakai
kelipatan 16 dengan ikon dasar 128 piksel. Pivot dekorasi di tengah bawah, ikon dan panel di tengah.
Banner mempunyai border 9-slice agar bagian tengah bisa menyesuaikan ukuran kartu.

Gambar sumber dan [prompt lengkap](../ArtSource/Generated/WarungPhase2/prompts.json) disimpan
di `ArtSource/Generated/WarungPhase2`. [Manifest ekspor](../ArtSource/Generated/WarungPhase2/export_manifest.json)
mencatat ukuran sumber, crop, dan ukuran final. `Tools/export_warung_art.cs` hanya memangkas
margin transparan dan melakukan nearest-neighbour resampling; tidak menggambar artwork.
Crop memakai siluet dengan alpha minimal 16/255 ditambah margin dua piksel sumber untuk
mengabaikan bintik alpha nyaris tak terlihat. Alpha dalam area hasil tetap dipertahankan.

## Objek dan script yang berubah

- `Customer.prefab`: sprite bubble, tiga ekspresi, bingkai dan isi bar; ikon mood diperbesar
  menjadi scale 0.29. Render overlay bubble memakai order 1000–1004, di atas furnitur/karakter.
  Lebar isi bar 0.55 unit, tetap memakai pengisian dari kiri yang sudah ada.
- `Main.unity`: referensi mood pada tiga `CustomerSpawner` dan `UIRoot` diperbarui karena
  prefab pelanggan dipakai bersama. Rak bumbu baru scale 0.72; papan menu scale 0.50,
  posisi lokal (6.875, 3, 0), supaya keseluruhan papan terlihat di bawah HUD pada kamera Warung.
- `UIRoot.Cutscene.cs`: artwork 9-slice untuk kartu judul, teks judul 26 dan subjudul 15,
  ruang teks disesuaikan ke area krem. Judul/hari tetap teks dinamis di Canvas;
  slide/fade dan durasi dari Fase 1 tetap dipakai.
- `GeprekBuilder.WarungArtwork.cs` baru: pemasangan ke prefab/scene, menu editor
  **Geprek → 4. Pasang Artwork Warung**. Hook pada `GeprekBuilder.Prefabs.cs`,
  `.BusinessParts.cs`, `.Home.cs`, dan `.WarungLayout.cs` mempertahankan artwork saat rebuild.
- `WarungArtworkImporter.cs` baru: menjaga pengaturan import PNG di folder khusus Warung.

Logic kesabaran, resep, antrean, stasiun, dan script gerakan tidak diubah pada fase ini.
Perubahan pengguna yang sudah ada pada awal Fase 2 tetap dipertahankan; hash pembanding
tersimpan di `Screenshots/VisualAudit_Phase2/before_hashes.json`.

## Pembanding dan verifikasi

- Pelanggan: [sebelum](../Screenshots/VisualAudit_Phase2/customers_before.png) →
  [sesudah](../Screenshots/VisualAudit_Phase2/customers_after.png).
  Tiga pelanggan dari prefab asli ditempatkan pada meja yang sama, dengan kesabaran
  90%, 45%, dan 10%. Ini **pratinjau yang disiapkan di Play Mode**, bukan bukti alur gameplay penuh.
- Banner: [sebelum](../Screenshots/VisualAudit_Phase1/warung_title_after.png) →
  [sesudah](../Screenshots/VisualAudit_Phase2/warung_title_after.png).
  Capture mengikuti coroutine intro `Opening` yang sebenarnya; dialog di-advance otomatis.
- Screenshot 1600 × 900, kamera gameplay (-0.07777786, -0.149999857, -10), orthographic size 4.4.
  Intro memakai kamera (0, -0.15, -10), sama seperti capture intro Fase 1.
- [73 pemeriksaan aset/integrasi lulus](../Screenshots/VisualAudit_Phase2/verification.json):
  ukuran, import, transparansi, tiga ekspresi, urutan render bubble, bar pada nilai 0–100%
  serta input di luar rentang, jangkar kiri, batas bingkai, hide/show/reaction, Canvas, dan dekorasi.
- [Trace intro](../Screenshots/VisualAudit_Phase2/title_animation_trace.json) mencatat masuk,
  tampil, keluar, dan kartu otomatis tersembunyi setelah selesai.

**Fase 3 menunggu konfirmasi “lanjut”**: alur ambil bahan → masak → sajikan → bayar
belum dijalankan sebagai verifikasi akhir. Penyimpanan game pengguna tidak diubah oleh pratinjau ini.
