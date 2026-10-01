using UnityEngine;

namespace Geprek.Data
{
    /// <summary>Semua angka penyetelan game dikumpulkan di sini supaya gampang di-balance.</summary>
    [CreateAssetMenu(menuName = "Geprek/Game Config", fileName = "GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("Waktu")]
        [Tooltip("Lama satu hari operasional dalam detik nyata.")]
        public float dayLengthSeconds = 210f;
        public int openHour = 9;
        public int closeHour = 17;
        [Tooltip("Batas waktu setelah tutup untuk menghabiskan pelanggan sisa.")]
        public float closingGraceSeconds = 45f;

        [Header("Pemain")]
        public float baseMoveSpeed = 4.2f;
        [Tooltip("Jarak maksimal untuk bisa berinteraksi dengan stasiun.")]
        public float interactRadius = 1.15f;

        [Header("Pelanggan")]
        [Tooltip("Kesabaran dasar pelanggan dalam detik, sebelum semua pengali.")]
        public float basePatience = 34f;
        [Tooltip("Lama pelanggan makan sebelum pergi.")]
        public float eatDuration = 4f;
        [Tooltip("Maksimal pelanggan mengantre saat semua kursi penuh.")]
        public int maxQueue = 3;

        [Header("Penilaian")]
        [Tooltip("Sisa kesabaran di atas nilai ini dihitung pelayanan sempurna.")]
        public float perfectThreshold = 0.6f;
        [Tooltip("Sisa kesabaran di atas nilai ini dihitung pelayanan baik.")]
        public float goodThreshold = 0.25f;
        public float perfectPayBonus = 1.35f;
        public float goodPayBonus = 1.0f;
        public float latePayBonus = 0.7f;
        [Tooltip("Penalti reputasi saat pelanggan kabur.")]
        public float reputationLossPerAbandon = 0.06f;
        [Tooltip("Tambahan reputasi saat pelayanan sempurna.")]
        public float reputationGainPerPerfect = 0.02f;

        [Header("Ekonomi")]
        public int startingMoney = 50000;
        [Tooltip("Biaya bahan harian yang dipotong otomatis saat tutup.")]
        public int dailyIngredientCost = 8000;

        [Header("Progresi")]
        [Tooltip("XP yang dibutuhkan untuk naik ke level 2. Level berikutnya dikali growth.")]
        public int xpForLevel2 = 60;
        public float xpGrowth = 1.45f;
        public int maxLevel = 12;

        [Header("Promosi malam")]
        public int promoCost = 12000;
        [Tooltip("Tambahan reputasi tiap kali promosi medsos berhasil.")]
        public float promoReputationGain = 0.12f;

        [Header("Reputasi")]
        [Tooltip("Reputasi dipakai sebagai pengali jumlah pelanggan harian.")]
        public float reputationToCustomerBonus = 0.5f;

        public int XpForLevel(int level)
        {
            if (level <= 1) return 0;
            float need = xpForLevel2;
            int total = 0;
            for (int i = 2; i <= level; i++)
            {
                total += Mathf.RoundToInt(need);
                need *= xpGrowth;
            }
            return total;
        }
    }
}
