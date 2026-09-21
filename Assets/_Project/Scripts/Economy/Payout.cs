using Geprek.Core;
using Geprek.Data;
using UnityEngine;

namespace Geprek.Economy
{
    /// <summary>Perhitungan bayaran satu pesanan, dikumpulkan di satu tempat agar mudah di-balance.</summary>
    public static class Payout
    {
        /// <summary>
        /// Nilai akhir sebuah pesanan.
        /// </summary>
        /// <param name="patienceLeft">Sisa kesabaran pelanggan 0..1 saat dilayani.</param>
        /// <param name="quality">Kualitas masakan 0..1 dari level pemain.</param>
        /// <param name="priceBonus">Bonus harga dari upgrade, misalnya 0.15 untuk +15%.</param>
        public static int Compute(GameConfig cfg, RecipeDef recipe, CustomerTypeDef type,
                                  float patienceLeft, float quality, float priceBonus,
                                  out ServeOutcome outcome, out float satisfaction,
                                  float priceScale = 1f)
        {
            float timeBonus;
            if (patienceLeft >= cfg.perfectThreshold) { outcome = ServeOutcome.Perfect; timeBonus = cfg.perfectPayBonus; }
            else if (patienceLeft >= cfg.goodThreshold) { outcome = ServeOutcome.Good; timeBonus = cfg.goodPayBonus; }
            else { outcome = ServeOutcome.Late; timeBonus = cfg.latePayBonus; }

            satisfaction = Mathf.Clamp01(patienceLeft * 0.75f + quality * 0.25f);

            float pay = recipe.basePrice
                        * timeBonus
                        * (0.75f + 0.35f * quality)
                        * (1f + priceBonus)
                        * Mathf.Max(0.1f, priceScale)          // diskon yang diumumkan semalam
                        * (type != null ? type.payMultiplier : 1f);

            return Mathf.Max(0, Mathf.RoundToInt(pay / 500f) * 500);
        }
    }
}
