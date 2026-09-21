using Geprek.Core;
using UnityEngine;

namespace Geprek.Data
{
    /// <summary>Satu barang di toko upgrade. Bisa dibeli berulang sampai maxLevel.</summary>
    [CreateAssetMenu(menuName = "Geprek/Upgrade", fileName = "Upgrade_")]
    public class UpgradeDef : ScriptableObject
    {
        public string id = "upgrade";
        public string displayName = "Upgrade";
        [TextArea] public string description;
        public Sprite icon;

        public UpgradeKind kind = UpgradeKind.MoveSpeed;

        [Header("Harga")]
        public int baseCost = 20000;
        [Tooltip("Harga level berikutnya = baseCost * costGrowth^levelSekarang.")]
        public float costGrowth = 1.6f;

        [Header("Efek")]
        [Tooltip("Besar efek per level. Artinya tergantung kind: persen, detik, atau jumlah slot.")]
        public float valuePerLevel = 0.1f;
        public int maxLevel = 3;

        [Header("Syarat")]
        public int unlockArc = 1;

        public int CostAt(int currentLevel) =>
            Mathf.RoundToInt(baseCost * Mathf.Pow(costGrowth, Mathf.Max(0, currentLevel)));
    }
}
