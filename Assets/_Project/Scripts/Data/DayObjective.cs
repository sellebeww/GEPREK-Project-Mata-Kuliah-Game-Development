using System;
using Geprek.Core;
using UnityEngine;

namespace Geprek.Data
{
    /// <summary>
    /// Satu target tambahan di luar omzet. Dipakai supaya tiap hari punya tantangan
    /// yang berbeda, bukan sekadar bertahan sampai jam tutup.
    /// </summary>
    [Serializable]
    public class DayObjective
    {
        public ObjectiveKind kind = ObjectiveKind.ServeCustomers;

        [Tooltip("Jumlah yang diminta. Untuk Satisfaction dipakai sebagai persen (0-100).")]
        public int amount = 5;

        [Tooltip("Hanya untuk SellRecipe.")]
        public RecipeDef recipe;

        /// <summary>Kalimat target yang ditampilkan di HUD dan laporan.</summary>
        public string Describe() => kind switch
        {
            ObjectiveKind.ServeCustomers => $"Layani {amount} pelanggan",
            ObjectiveKind.Satisfaction   => $"Jaga kepuasan di atas {amount}%",
            ObjectiveKind.SellRecipe     => $"Jual {amount} {(recipe != null ? recipe.displayName : "menu")}",
            ObjectiveKind.MaxAbandon     => amount == 0 ? "Jangan sampai ada pelanggan kabur"
                                                        : $"Maksimal {amount} pelanggan kabur",
            ObjectiveKind.PerfectOrders  => $"Capai {amount} pelayanan sempurna",
            _ => "Target"
        };

        /// <summary>Kemajuan saat ini terhadap target, untuk ditampilkan sebagai "3/5".</summary>
        public string Progress(DayStats s) => kind switch
        {
            ObjectiveKind.ServeCustomers => $"{s.served}/{amount}",
            ObjectiveKind.Satisfaction   => $"{Mathf.RoundToInt(s.AverageSatisfaction * 100f)}%/{amount}%",
            ObjectiveKind.SellRecipe     => $"{s.SoldOf(recipe != null ? recipe.id : null)}/{amount}",
            ObjectiveKind.MaxAbandon     => $"{s.abandoned + s.turnedAway}/{amount}",
            ObjectiveKind.PerfectOrders  => $"{s.perfect}/{amount}",
            _ => ""
        };

        public bool IsMet(DayStats s) => kind switch
        {
            ObjectiveKind.ServeCustomers => s.served >= amount,
            ObjectiveKind.Satisfaction   => s.served > 0 && s.AverageSatisfaction * 100f >= amount,
            ObjectiveKind.SellRecipe     => s.SoldOf(recipe != null ? recipe.id : null) >= amount,
            ObjectiveKind.MaxAbandon     => s.abandoned + s.turnedAway <= amount,
            ObjectiveKind.PerfectOrders  => s.perfect >= amount,
            _ => false
        };

        /// <summary>
        /// Target "jangan sampai" hanya bisa dinilai setelah hari selesai; sebelum itu
        /// statusnya masih bisa berubah, jadi tidak ditandai lulus lebih awal.
        /// </summary>
        public bool IsFailable => kind == ObjectiveKind.MaxAbandon;
    }
}
