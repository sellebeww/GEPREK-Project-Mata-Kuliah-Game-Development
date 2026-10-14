# Prompt generasi ikon yang kurang

Status: **ikon di bawah belum dibuat atau diimpor.** Dokumen ini hanya berisi kebutuhan dan prompt.

## Gaya acuan (dari `Assets/_Project/Art/Sprites/Sign.png`)

- Sheet 1254×1254, grid 8×8, tiap ikon sekitar 100–145 px.
- Pixel-art "chunky" (piksel besar terlihat), outline cokelat tua tebal sekitar 1 piksel-besar.
- Warna jenuh hangat: merah cabai, hijau daun, kuning emas, krem. Satu highlight terang dan satu bayangan gelap per bidang.
- Latar transparan, satu objek per ikon, di tengah, tanpa teks, tanpa bayangan jatuh.

## Yang kurang

| # | Ikon | Dipakai di | Masalah sekarang |
|---|---|---|---|
| 1 | Sepatu kerja | Toko, upgrade "Sepatu Kerja" | Memakai grafik batang naik (`growth`) yang tidak berhubungan dengan sepatu |
| 2 | Label diskon | Medsos, aksi "Umumkan Diskon" | Memakai `growth` yang sama, sehingga dua kartu berbeda tampil identik |
| 3 | Kamera vlogger | Medsos, aksi "Undang Food Vlogger" | Memakai bintang generik (`star`) yang juga dipakai di tempat lain |

## Prompt (salin utuh)

Awalan gaya, tempel di depan setiap prompt:

```
Pixel art game UI icon, chunky visible pixels, thick dark-brown outline, saturated warm colors
(chili red, leaf green, golden yellow, cream), one bright highlight and one dark shade per color
area, centered single object, transparent background, no text, no drop shadow, no background
elements, flat front view, matching a cute Indonesian food-stall business sim.
Canvas 128x128 pixels, object fills about 85% of the canvas.
```

**1. Sepatu kerja**
```
[awalan gaya] A single sporty sneaker seen from the side, red upper with white sole and white
laces, small yellow speed lines behind the heel to suggest fast walking.
```

**2. Label diskon**
```
[awalan gaya] A hanging price tag with a string loop, chili-red tag body, cream-colored percent
sign "%" in the middle drawn as a simple symbol, slightly tilted 15 degrees, small golden sparkle
at one corner.
```

**3. Kamera vlogger**
```
[awalan gaya] A small handheld video camera, dark grey body with a big blue lens, a red
recording dot on top, and a tiny golden star badge at the upper right corner.
```

## Cara membuat konsisten

- Generate 4 variasi per ikon, pilih yang paling dekat dengan ikon lain di `Sign.png`.
- Simpan sebagai PNG 128×128, RGBA, tanpa latar putih. Cek tepi: tidak boleh ada halo.
- Jika hasilnya anti-aliased (halus), kecilkan ke 64×64 dengan *nearest neighbour*, lalu besarkan lagi ke 128×128 dengan *nearest neighbour*. Ini mengembalikan kesan piksel tegas.
- Pastikan ketiganya terbaca jelas pada ukuran 56×56 px, yaitu ukuran ikon di kartu toko.

## Cara memasang di Unity (belum dikerjakan)

1. Taruh file di `Assets/_Project/Art/UI/Icons/` dengan nama `shoe.png`, `discount.png`, `vlogger.png`.
2. Import: Texture Type = Sprite (2D and UI), Filter Mode = Point (no filter), Compression = None, Pixels Per Unit = 100.
3. Di `GeprekBuilder.cs` tambahkan pemuat ikon dari folder tersebut. Lalu:
   - `GeprekBuilder.Data.cs`: upgrade `sepatu` memakai `shoe`.
   - `GeprekBuilder.Home.cs`: tambahkan field ikon baru untuk `discount` dan `vlogger`.
   - `UIRoot.Social.cs`: kartu diskon memakai `icons.discount`, kartu vlogger memakai `icons.vlogger`.
4. Jalankan builder untuk data (upgrade) dan cek di Play Mode bahwa tiap kartu punya ikon berbeda.

## Opsional (prioritas rendah)

Upgrade lain (kompor, ulekan, penggorengan, meja, kipas, branding) memakai sprite perabot dari dunia permainan. Terbaca jelas tetapi gayanya lebih "ilustrasi" daripada ikon UI. Jika ingin seragam, buat enam ikon bergaya sama dengan tiga di atas: kompor gas dengan api biru, ulekan batu dengan sambal merah, penggorengan dengan ayam, meja kayu, kipas angin, dan spanduk warung.
