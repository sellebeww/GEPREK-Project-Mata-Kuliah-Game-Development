using System;
using System.Collections.Generic;
using UnityEngine;

namespace Geprek.Core
{
    [Serializable] public class RecipeSold { public string recipeId; public int count; }

    /// <summary>Rekap satu hari kerja. Dipakai layar laporan harian dan laporan arc.</summary>
    [Serializable]
    public class DayStats
    {
        public int dayNumber;
        public int arcNumber;
        public int targetRevenue;

        public int revenue;
        public int served;
        public int perfect;
        public int late;
        public int wrongOrders;
        public int abandoned;   // pelanggan kabur karena kehabisan sabar
        public int turnedAway;  // pelanggan pulang karena tidak kebagian tempat
        public int wasted;      // porsi/bahan yang dibuang
        public int xpGained;
        public int expenses;
        [Tooltip("Pemasukan dari cabang di luar penjualan di tempat.")]
        public int branchIncome;

        public float satisfactionSum;

        /// <summary>Berapa porsi tiap menu terjual hari ini. Dipakai target "jual N menu X".</summary>
        public List<RecipeSold> sold = new();

        public int SoldOf(string recipeId)
        {
            if (string.IsNullOrEmpty(recipeId)) return 0;
            for (int i = 0; i < sold.Count; i++)
                if (sold[i].recipeId == recipeId) return sold[i].count;
            return 0;
        }

        public void RecordSale(string recipeId)
        {
            if (string.IsNullOrEmpty(recipeId)) return;
            for (int i = 0; i < sold.Count; i++)
                if (sold[i].recipeId == recipeId) { sold[i].count++; return; }
            sold.Add(new RecipeSold { recipeId = recipeId, count = 1 });
        }

        /// <summary>Jumlah bintang 0..3. Dipakai di laporan harian sebagai nilai performa.</summary>
        public int stars;

        public int TotalCustomers => served + abandoned + turnedAway;
        public int Profit => revenue - expenses;
        public bool TargetMet => revenue >= targetRevenue;

        public float AverageSatisfaction => served <= 0 ? 0f : satisfactionSum / served;

        /// <summary>Nilai huruf untuk laporan, dari omzet + kepuasan.</summary>
        public string Grade
        {
            get
            {
                if (TotalCustomers == 0) return "-";
                float ratio = targetRevenue <= 0 ? 1f : (float)revenue / targetRevenue;
                float score = ratio * 0.6f + AverageSatisfaction * 0.4f;
                if (score >= 1.15f) return "A";
                if (score >= 0.95f) return "B";
                if (score >= 0.75f) return "C";
                if (score >= 0.55f) return "D";
                return "E";
            }
        }

        public void Reset(int day, int arc, int target)
        {
            dayNumber = day; arcNumber = arc; targetRevenue = target;
            revenue = served = perfect = late = wrongOrders = 0;
            abandoned = turnedAway = wasted = xpGained = expenses = branchIncome = 0;
            satisfactionSum = 0f;
            stars = 0;
            sold.Clear();
        }
    }

    /// <summary>Rekap kumulatif satu arc, dipakai saat lapor ke dosen.</summary>
    [Serializable]
    public class ArcStats
    {
        public int arcNumber;
        public int revenue;
        public int served;
        public int lost;
        public float satisfactionSum;
        public int daysPlayed;

        public float AverageSatisfaction => served <= 0 ? 0f : satisfactionSum / served;

        public void Accumulate(DayStats day)
        {
            arcNumber = day.arcNumber;
            revenue += day.revenue;
            served += day.served;
            lost += day.abandoned + day.turnedAway;
            satisfactionSum += day.satisfactionSum;
            daysPlayed++;
        }

        public void Reset(int arc)
        {
            arcNumber = arc; revenue = served = lost = daysPlayed = 0; satisfactionSum = 0f;
        }
    }
}
