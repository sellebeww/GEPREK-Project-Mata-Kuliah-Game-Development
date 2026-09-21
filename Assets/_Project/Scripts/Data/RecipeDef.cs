using System.Collections.Generic;
using UnityEngine;

namespace Geprek.Data
{
    /// <summary>
    /// Satu menu yang bisa dipesan pelanggan. Cocok kalau kumpulan komponen di
    /// meja penyajian sama persis dengan <see cref="components"/> (urutan bebas).
    /// </summary>
    [CreateAssetMenu(menuName = "Geprek/Recipe", fileName = "Recipe_")]
    public class RecipeDef : ScriptableObject
    {
        [Header("Identitas")]
        public string id = "recipe";
        public string displayName = "Menu";
        [TextArea] public string description;
        public Sprite icon;

        [Header("Komposisi")]
        [Tooltip("Komponen yang harus ditaruh di meja penyajian. Urutan tidak berpengaruh.")]
        public List<ItemDef> components = new();

        [Header("Ekonomi")]
        public int basePrice = 15000;
        public int xpReward = 10;

        [Header("Syarat terbuka")]
        [Tooltip("Level memasak minimal supaya resep ini bisa dibeli/diberikan.")]
        public int unlockLevel = 1;
        [Tooltip("Arc minimal tempat resep ini muncul.")]
        public int unlockArc = 1;
        [Tooltip("Harga membuka resep di toko. 0 = terbuka otomatis / hadiah cerita.")]
        public int unlockCost;
        [Tooltip("Resep ini hadiah dari orang tua, bukan dari toko.")]
        public bool fromParents;

        [Header("Kesulitan")]
        [Tooltip("Pengali kesabaran pelanggan. Menu rumit diberi waktu lebih longgar.")]
        public float patienceMultiplier = 1f;

        /// <summary>True kalau isi <paramref name="onPlate"/> sama persis dengan resep.</summary>
        public bool Matches(IReadOnlyList<ItemDef> onPlate)
        {
            if (onPlate == null || onPlate.Count != components.Count) return false;

            var pool = new List<ItemDef>(components);
            for (int i = 0; i < onPlate.Count; i++)
            {
                int found = pool.IndexOf(onPlate[i]);
                if (found < 0) return false;
                pool.RemoveAt(found);
            }
            return pool.Count == 0;
        }

        /// <summary>Berapa banyak komponen resep yang sudah benar di piring. Untuk UI progres.</summary>
        public int MatchedCount(IReadOnlyList<ItemDef> onPlate)
        {
            if (onPlate == null) return 0;
            var pool = new List<ItemDef>(components);
            int hit = 0;
            for (int i = 0; i < onPlate.Count; i++)
            {
                int found = pool.IndexOf(onPlate[i]);
                if (found >= 0) { pool.RemoveAt(found); hit++; }
            }
            return hit;
        }
    }
}
