using System.Collections.Generic;
using Geprek.Core;
using Geprek.Data;
using Geprek.Player;
using Geprek.World;
using UnityEngine;

namespace Geprek.Stations
{
    /// <summary>
    /// Sumber bahan tanpa batas: kotak ayam, rice cooker, wadah sambal.
    /// Muncul sendiri hanya kalau bahannya sudah dipakai resep yang terbuka.
    /// </summary>
    public class IngredientSource : StationBase
    {
        [Header("Bahan yang disediakan")]
        [SerializeField] ItemDef item;

        [Tooltip("Sembunyikan stasiun ini sampai ada resep terbuka yang memakainya.")]
        [SerializeField] bool hideUntilNeeded = true;

        bool _available = true;

        protected override void Awake()
        {
            base.Awake();
            CacheRenderers();
        }

        Renderer[] _renderers;

        /// <summary>
        /// Hanya tampilan stasiun yang ikut disembunyikan. Bilah progres punya
        /// aturan tampil sendiri, jadi jangan ikut dinyalakan di sini.
        /// </summary>
        void CacheRenderers()
        {
            // ikut menyertakan MeshRenderer supaya papan nama stasiun yang masih
            // terkunci tidak tertinggal menyala di atas meja kerja
            var list = new List<Renderer>();
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                if (r == null || (itemIcon != null && r == itemIcon)) continue;
                if (highlightGlow != null && r == highlightGlow) continue;   // cincin sorot punya aturan sendiri
                if (r.GetComponentInParent<WorldProgressBar>() != null) continue;
                list.Add(r);
            }
            _renderers = list.ToArray();
        }

        void OnEnable()
        {
            GameEvents.RecipeUnlocked += OnRecipeUnlocked;
            GameEvents.DayStarted += OnDayStarted;
            RefreshAvailability();
        }

        void OnDisable()
        {
            GameEvents.RecipeUnlocked -= OnRecipeUnlocked;
            GameEvents.DayStarted -= OnDayStarted;
        }

        void OnRecipeUnlocked(RecipeDef _) => RefreshAvailability();

        // saat hari dimulai daftar resep sudah pasti terisi, jadi ini titik yang paling andal
        void OnDayStarted(DayPlanDef _, int __) => RefreshAvailability();

        /// <summary>Nyalakan stasiun hanya kalau bahannya relevan dengan resep yang sudah dimiliki.</summary>
        public void RefreshAvailability()
        {
            if (!hideUntilNeeded || item == null || Game == null) { SetAvailable(true); return; }

            bool needed = false;
            foreach (var recipe in Game.Database.recipes)
            {
                if (recipe == null || !Game.Progress.HasRecipe(recipe.id)) continue;
                if (RecipeUses(recipe, item)) { needed = true; break; }
            }
            SetAvailable(needed);
        }

        /// <summary>Resep memakai bahan ini, langsung atau lewat rantai olahan (ayam -> goreng -> geprek).</summary>
        static bool RecipeUses(RecipeDef recipe, ItemDef source)
        {
            foreach (var comp in recipe.components)
            {
                var walk = source;
                int guard = 0;
                while (walk != null && guard++ < 8)
                {
                    if (walk == comp) return true;
                    walk = walk.prepResult != null ? walk.prepResult : walk.cookResult;
                }
            }
            return false;
        }

        void SetAvailable(bool on)
        {
            _available = on;
            if (_renderers == null) CacheRenderers();
            foreach (var r in _renderers)
                if (r != null) r.enabled = on;

            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = on;
        }

        /// <summary>Id bahan yang disediakan. Dipakai karyawan untuk mencari sumber ayam.</summary>
        public string ItemId => item != null ? item.id : null;

        public override bool CanInteract(PlayerCarry carry) =>
            _available && item != null && carry != null && carry.IsEmpty;

        public override string Hint(PlayerCarry carry)
        {
            if (item == null) return stationName;
            if (carry != null && carry.HasItem) return "Tangan penuh";
            return $"Ambil {item.displayName}";
        }

        public override void Interact(PlayerCarry carry)
        {
            if (!CanInteract(carry)) return;
            carry.TryTake(CarriedItem.FromItem(item));
            Sfx(SfxId.Pickup);
        }
    }
}
