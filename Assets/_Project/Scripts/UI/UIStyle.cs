using UnityEngine;

namespace Geprek.UI
{
    /// <summary>Palet dan ukuran baku UI, dikumpulkan supaya tampilan konsisten.</summary>
    public static class UIStyle
    {
        // warna utama diambil dari nuansa sprite: cokelat kayu, merah sambal, krem nasi
        public static readonly Color Ink = new(0.16f, 0.12f, 0.10f);
        public static readonly Color InkSoft = new(0.36f, 0.30f, 0.26f);
        public static readonly Color Cream = new(0.98f, 0.95f, 0.88f);
        public static readonly Color Panel = new(0.99f, 0.96f, 0.90f, 1f);
        public static readonly Color PanelDark = new(0.20f, 0.15f, 0.13f, 0.95f);
        public static readonly Color Wood = new(0.55f, 0.36f, 0.22f);
        public static readonly Color Chili = new(0.85f, 0.26f, 0.22f);
        public static readonly Color ChiliDark = new(0.68f, 0.18f, 0.15f);
        public static readonly Color Leaf = new(0.31f, 0.65f, 0.35f);
        public static readonly Color Gold = new(0.97f, 0.76f, 0.24f);
        public static readonly Color Sky = new(0.30f, 0.56f, 0.80f);
        public static readonly Color Dim = new(0f, 0f, 0f, 0.62f);
        public static readonly Color NightTint = new(0.16f, 0.18f, 0.38f, 0.42f);

        // skala tipografi baku: pakai konstanta ini, jangan angka mentah
        public const int FontDisplay = 76;  // logo menu utama
        public const int FontTitle = 44;    // nilai/huruf besar (grade, bintang)
        public const int FontHeading = 26;  // judul panel, tombol utama
        public const int FontLead = 22;     // sorotan: nama pesanan, dialog, tombol sekunder
        public const int FontBody = 18;     // teks isi dan tombol biasa
        public const int FontSmall = 15;    // keterangan
        public const int FontCaption = 13;  // catatan kecil, kredit, batas bawah auto-fit

        static Font _font;

        /// <summary>Font bawaan Unity. Nama aset berbeda antar versi, jadi dicoba berurutan.</summary>
        public static Font Font
        {
            get
            {
                if (_font != null) return _font;
                string[] candidates = { "LegacyRuntime.ttf", "Arial.ttf" };
                foreach (var name in candidates)
                {
                    _font = Resources.GetBuiltinResource<Font>(name);
                    if (_font != null) return _font;
                }
                _font = Font.CreateDynamicFontFromOSFont("Helvetica", FontBody);
                return _font;
            }
        }
    }
}
