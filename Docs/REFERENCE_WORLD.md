# Desain referensi, pelanggan, karyawan, dan cerita

Implementasi di `Assets/_Project/Scenes/Main.unity`. Tekan Play, lalu **Mulai Baru** untuk membuka cerita kampus dan percakapan keluarga. Dialog menggunakan klik, Spasi, atau Enter: tekan sekali untuk melengkapi teks yang sedang muncul, lalu tekan lagi untuk melanjutkan.

## Perkembangan tempat usaha

| Tahap | Referensi | Perilaku |
|---|---|---|
| Arc 1 — Warung | Miskin (1).png | Lima meja; kursi dan penggorengan tambahan mengikuti upgrade. |
| Arc 2 — Ruko | Menengah (1).png | Interior kayu, tambahan kapasitas, perekrutan karyawan tersedia. |
| Arc 3 — Restoran | Kaya (1).png | Interior marmer dan pencahayaan hangat; sepuluh tempat duduk. |

Pergantian tempat mengikuti kelulusan laporan arc yang sudah ada. Pembelian upgrade alat/kursi tetap bekerja di tempat usaha saat ini. Area interaksi, penghalang meja, posisi pelanggan, dan jalur karyawan diselaraskan dengan ilustrasi. Lapisan depan meja menutupi kaki karakter yang berjalan di belakangnya.

Gambar referensi asli tetap tersedia. Dekorasi scene lama yang bertumpuk telah diganti. Salinan scene sebelum perubahan disimpan di `ArtSource/Backups/`. Lapisan warna siang/malam yang memutihkan atau mengaburkan interior sudah dihapus: warna ilustrasi tampil langsung tanpa selubung layar.

## Pelanggan dan karyawan

- Atlas berisi delapan karakter dengan arah jalan serta pose duduk dan mengangkat tangan. Gerak jalan, napas, bekerja, dan makan memakai pergantian pose serta gerak transform ringan. Pose duduk berporos pada pinggul; titik duduk diukur per kursi pada masing-masing interior.
- Pelanggan masuk melalui lorong, duduk menghadap meja, memesan, menunggu, makan dengan piring di meja, membayar, lalu pulang. Mereka dapat mengangkat tangan saat menunggu terlalu lama.
- Juru masak memilih pesanan, mengambil ayam, menggoreng, menggeprek, dan menambahkan seluruh bahan resep. Klaim stasiun menjaga pramusaji agar tidak mengambil resep yang belum selesai.
- Pramusaji mengantar makanan ke pelanggan yang cocok. Porsi yang ditinggalkan pelanggan dibersihkan setelah jeda agar meja saji dapat dipakai kembali.
- Kasir berdiri di belakang counter, dengan badan bawah tertutup meja, animasi kerja, dan bonus harga sesuai tingkatnya.
- Toko mendukung rekrut, naik tingkat sampai level 5, dan lepas karyawan; maksimal empat orang. Gaji mengikuti sistem harian yang sudah ada.

## Cerita dan aset

Pembuka menampilkan kelas kewirausahaan di kampus, kemudian percakapan keluarga di rumah. Ilustrasi memakai gerakan kamera halus, fade, dialog bertahap, dan potret Kamu, Ibu, Ayah, serta Dosen yang seragam. Percakapan malam dengan orang tua juga memakai ilustrasi rumah.

Aset bitmap disimpan di `Assets/_Project/Resources/Art/ReferenceWorld/`.

## Pratinjau untuk pengembangan

Dalam Play Mode, menu **Geprek → Preview** membuka tiap tingkat. Pratinjau menggunakan save sementara terpisah dan menulis laporan ke `Logs/reference-playtest-tier*.json`; durasi hari uji dipanjangkan sementara. Keluar Play Mode untuk kembali ke konfigurasi normal.

Menu **Geprek → 5. Pasang Desain Referensi & Karakter** menerapkan ulang desain ke Main. Builder scene juga memanggil installer agar pembangunan ulang scene mempertahankan desain referensi.

## Verifikasi

- Kompilasi Unity 6000.5.9f1 berhasil.
- 11 tes Edit Mode lulus: sembilan regresi gameplay yang sudah ada, satu kasus resep bertopping yang sempat cocok dengan resep dasar, dan satu kasus pose duduk/pesan/makan serta pemulihan arah saat berdiri.
- Sesi Play Mode ruko mencatat empat pelanggan dilayani, omzet Rp145.500, nol salah pesanan, serta seluruh tahap duduk, memesan, makan, membayar, dan pulang.
- Setelah perbaikan posisi duduk, kasir, dan kejernihan layar, sesi restoran mencatat tiga pelanggan dilayani, omzet Rp101.000, dan nol salah pesanan. [Laporan Play Mode](Validation/reference-playmode-restaurant.json); [hasil 11 tes](Validation/reference-editmode-tests.json).
- Pengambilan ayam diuji melalui pemilihan target dan tombol interaksi pemain pada posisi lemari baru.
- Perekrutan karyawan keempat memotong Rp335.000; upgrade level 1 ke 2 memotong Rp45.000; pelepasan mengembalikan jumlah pekerja aktif menjadi tiga. Seluruhnya dijalankan pada save uji.
- Ilustrasi kampus, transisi ke rumah, dialog malam, toko karyawan, dan ketiga interior diperiksa di Game View.
- MCP resmi mengiklankan 160 tools dan berhasil membaca status editor proyek ini. Detail ada di [UNITY_TOOLING.md](UNITY_TOOLING.md).

Cuplikan akhir: [warung](../Screenshots/ReferenceWorld/warung-clean.png), [ruko](../Screenshots/ReferenceWorld/ruko-clean.png), [restoran](../Screenshots/ReferenceWorld/restaurant-clean.png), [kampus](../Screenshots/ReferenceWorld/campus.png), [cutscene rumah](../Screenshots/ReferenceWorld/home-cutscene.png), dan [percakapan malam](../Screenshots/ReferenceWorld/home-night.png). Gambar hasil uji berada di luar Assets agar tidak ikut menjadi aset permainan. Sesi uji sudah ditutup; scene Main tersimpan, durasi hari normal 200 detik, dan save sementara sudah dilepas.

Verifikasi dilakukan di Editor macOS; build perangkat seluler belum diuji.
