using System.Collections.Generic;
using UnityEngine;

namespace Geprek.Data
{
    /// <summary>
    /// Satu titik kumpul semua aset data. Dipakai runtime untuk mencari definisi
    /// berdasarkan id (misalnya saat memuat save file).
    /// </summary>
    [CreateAssetMenu(menuName = "Geprek/Game Database", fileName = "GameDatabase")]
    public class GameDatabase : ScriptableObject
    {
        public GameConfig config;

        [Header("Isi dunia")]
        public List<ItemDef> items = new();
        public List<RecipeDef> recipes = new();
        public List<UpgradeDef> upgrades = new();
        public List<CustomerTypeDef> customerTypes = new();
        public List<ArcDef> arcs = new();

        [Header("Karakter")]
        public CharacterSkin playerSkin;
        public CharacterSkin motherSkin;
        public CharacterSkin fatherSkin;
        public CharacterSkin lecturerSkin;
        public List<CharacterSkin> staffSkins = new();

        Dictionary<string, ItemDef> _itemById;
        Dictionary<string, RecipeDef> _recipeById;
        Dictionary<string, UpgradeDef> _upgradeById;

        public ItemDef GetItem(string id)
        {
            _itemById ??= Build(items, i => i.id);
            return id != null && _itemById.TryGetValue(id, out var v) ? v : null;
        }

        public RecipeDef GetRecipe(string id)
        {
            _recipeById ??= Build(recipes, r => r.id);
            return id != null && _recipeById.TryGetValue(id, out var v) ? v : null;
        }

        public UpgradeDef GetUpgrade(string id)
        {
            _upgradeById ??= Build(upgrades, u => u.id);
            return id != null && _upgradeById.TryGetValue(id, out var v) ? v : null;
        }

        public ArcDef GetArc(int arcNumber)
        {
            for (int i = 0; i < arcs.Count; i++)
                if (arcs[i] != null && arcs[i].arcNumber == arcNumber) return arcs[i];
            return arcs.Count > 0 ? arcs[0] : null;
        }

        static Dictionary<string, T> Build<T>(List<T> list, System.Func<T, string> key) where T : Object
        {
            var map = new Dictionary<string, T>();
            for (int i = 0; i < list.Count; i++)
                if (list[i] != null) map[key(list[i])] = list[i];
            return map;
        }

        void OnValidate() { _itemById = null; _recipeById = null; _upgradeById = null; }
    }
}
