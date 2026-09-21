using Geprek.Core;
using UnityEngine;

namespace Geprek.Data
{
    /// <summary>
    /// Definisi satu benda yang bisa dipegang pemain: bahan mentah, bahan matang,
    /// komponen porsi, atau piring kotor. Porsi jadi dipegang lewat <see cref="RecipeDef"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Geprek/Item", fileName = "Item_")]
    public class ItemDef : ScriptableObject
    {
        [Header("Identitas")]
        public string id = "item";
        public string displayName = "Item";
        public Sprite icon;

        [Header("Sifat")]
        public ItemStage stage = ItemStage.Raw;

        [Tooltip("Boleh langsung dipakai sebagai komponen piring tanpa diolah dulu.")]
        public bool isPlatingComponent;

        [Header("Pengolahan")]
        [Tooltip("Hasil setelah digoreng di penggorengan. Kosongkan kalau tidak bisa digoreng.")]
        public ItemDef cookResult;

        [Tooltip("Hasil kalau kelamaan di penggorengan.")]
        public ItemDef burnResult;

        [Tooltip("Hasil setelah diulek/digeprek di cobek. Kosongkan kalau tidak bisa digeprek.")]
        public ItemDef prepResult;

        [Tooltip("Lama menggoreng dalam detik.")]
        public float cookTime = 5f;

        [Tooltip("Sisa waktu aman sebelum gosong, dihitung setelah matang.")]
        public float burnGrace = 6f;

        [Tooltip("Berapa kali ketuk / berapa detik menahan untuk menggeprek.")]
        public float prepTime = 2.5f;

        public bool CanCook => cookResult != null;
        public bool CanPrep => prepResult != null;
    }
}
