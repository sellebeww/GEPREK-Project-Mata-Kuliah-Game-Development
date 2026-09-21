using System.Collections.Generic;
using Geprek.Core;
using UnityEngine;

namespace Geprek.Data
{
    /// <summary>
    /// Benda yang sedang dipegang pemain atau tergeletak di stasiun. Bisa berupa bahan
    /// tunggal (<see cref="def"/>) atau porsi jadi (<see cref="recipe"/>).
    /// </summary>
    public class CarriedItem
    {
        public ItemDef def;
        public RecipeDef recipe;

        /// <summary>Kualitas 0..1, menentukan bonus harga. Dipengaruhi level memasak.</summary>
        public float quality = 1f;

        /// <summary>Komponen yang sudah ditumpuk, hanya dipakai piring setengah jadi.</summary>
        public readonly List<ItemDef> components = new();

        public bool IsDish => recipe != null;
        public bool IsEmpty => def == null && recipe == null && components.Count == 0;

        public Sprite Icon => recipe != null ? recipe.icon : def != null ? def.icon : null;

        public string DisplayName =>
            recipe != null ? recipe.displayName : def != null ? def.displayName : "-";

        public bool IsTrashOnly => def != null && (def.stage == ItemStage.Burnt || def.stage == ItemStage.Dirty);

        public static CarriedItem FromItem(ItemDef item, float quality = 1f) =>
            new() { def = item, quality = quality };

        public static CarriedItem FromRecipe(RecipeDef recipe, float quality = 1f) =>
            new() { recipe = recipe, quality = quality };

        public CarriedItem Clone()
        {
            var c = new CarriedItem { def = def, recipe = recipe, quality = quality };
            c.components.AddRange(components);
            return c;
        }
    }
}
