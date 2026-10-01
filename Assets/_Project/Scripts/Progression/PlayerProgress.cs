using System;
using System.Collections.Generic;
using Geprek.Core;
using Geprek.Data;
using UnityEngine;

namespace Geprek.Progression
{
    [Serializable] public class UpgradeEntry { public string id; public int level; }

    /// <summary>
    /// Semua kemajuan pemain yang bertahan antar hari. Objek ini juga yang di-serialize
    /// ke save file, jadi isinya sengaja hanya tipe sederhana.
    /// </summary>
    [Serializable]
    public class PlayerProgress
    {
        public int level = 1;
        public int xp;
        public int money;
        public float reputation = 0.15f;      // 0..1, memengaruhi jumlah pelanggan
        public int arcNumber = 1;
        public int dayInArc = 1;
        public int totalDayCount = 1;
        public int lifetimeRevenue;
        public int lifetimeServed;
        public bool tutorialSeen;

        public List<string> unlockedRecipes = new();
        public List<UpgradeEntry> upgrades = new();
        public List<StaffMember> staff = new();
        public List<BranchInfo> branches = new();
        public List<string> parentTopicsSeen = new();

        // ---- hasil kegiatan medsos semalam, dipakai untuk hari berikutnya ----
        [Tooltip("Tambahan jumlah pelanggan besok, 0.12 berarti +12%.")]
        public float tomorrowCustomerBonus;
        [Tooltip("Pengali harga jual besok. 0.82 berarti diskon 18%.")]
        public float tomorrowPriceScale = 1f;
        [Tooltip("Food vlogger dijadwalkan datang besok.")]
        public bool vloggerTomorrow;

        /// <summary>Kosongkan rencana besok setelah dipakai satu hari.</summary>
        public void ConsumeTomorrowPlan()
        {
            tomorrowCustomerBonus = 0f;
            tomorrowPriceScale = 1f;
            vloggerTomorrow = false;
        }

        public ArcStats arcStats = new();

        // ---------- resep ----------

        public bool HasRecipe(string id) => !string.IsNullOrEmpty(id) && unlockedRecipes.Contains(id);

        public bool UnlockRecipe(RecipeDef recipe)
        {
            if (recipe == null || HasRecipe(recipe.id)) return false;
            unlockedRecipes.Add(recipe.id);
            GameEvents.RaiseRecipeUnlocked(recipe);
            return true;
        }

        // ---------- upgrade ----------

        public int UpgradeLevel(string id)
        {
            for (int i = 0; i < upgrades.Count; i++)
                if (upgrades[i].id == id) return upgrades[i].level;
            return 0;
        }

        public void SetUpgradeLevel(string id, int level)
        {
            for (int i = 0; i < upgrades.Count; i++)
                if (upgrades[i].id == id) { upgrades[i].level = level; return; }
            upgrades.Add(new UpgradeEntry { id = id, level = level });
        }

        /// <summary>Total efek sebuah jenis upgrade, dijumlahkan dari semua level yang dimiliki.</summary>
        public float UpgradeValue(GameDatabase db, UpgradeKind kind)
        {
            float total = 0f;
            for (int i = 0; i < upgrades.Count; i++)
            {
                var def = db.GetUpgrade(upgrades[i].id);
                if (def != null && def.kind == kind) total += def.valuePerLevel * upgrades[i].level;
            }
            return total;
        }

        // ---------- level ----------

        /// <summary>XP yang masih dibutuhkan untuk naik level berikutnya.</summary>
        public int XpToNext(GameConfig cfg)
        {
            if (level >= cfg.maxLevel) return 0;
            return Mathf.Max(0, cfg.XpForLevel(level + 1) - xp);
        }

        public int XpSpanOfCurrentLevel(GameConfig cfg)
        {
            if (level >= cfg.maxLevel) return 1;
            return Mathf.Max(1, cfg.XpForLevel(level + 1) - cfg.XpForLevel(level));
        }

        public int XpIntoCurrentLevel(GameConfig cfg) => Mathf.Max(0, xp - cfg.XpForLevel(level));

        /// <summary>Tambah XP dan naikkan level selama ambangnya terlampaui.</summary>
        public int AddXp(GameConfig cfg, int amount)
        {
            if (amount <= 0) return 0;
            xp += amount;
            int gained = 0;
            while (level < cfg.maxLevel && xp >= cfg.XpForLevel(level + 1))
            {
                level++; gained++;
                GameEvents.RaiseLevelUp(level);
            }
            GameEvents.RaiseXp(xp, XpToNext(cfg), level);
            return gained;
        }

        /// <summary>Kualitas masakan dari level. Level tinggi = porsi lebih bagus = harga lebih baik.</summary>
        public float CookQuality(GameConfig cfg) =>
            Mathf.Clamp01(0.55f + 0.45f * (level - 1) / Mathf.Max(1f, cfg.maxLevel - 1f));

        // ---------- karyawan ----------

        public int StaffCount(StaffRole role)
        {
            int n = 0;
            for (int i = 0; i < staff.Count; i++) if (staff[i].role == role) n++;
            return n;
        }

        /// <summary>Total pemasukan harian dari semua cabang.</summary>
        public int BranchIncome()
        {
            int total = 0;
            for (int i = 0; i < branches.Count; i++) total += branches[i].DailyIncome(reputation);
            return total;
        }

        public int BranchCost()
        {
            int total = 0;
            for (int i = 0; i < branches.Count; i++) total += branches[i].DailyCost;
            return total;
        }

        public int TotalSalary()
        {
            int total = 0;
            for (int i = 0; i < staff.Count; i++) total += staff[i].salary * staff[i].level;
            return total;
        }

        /// <summary>Bonus harga dari kasir yang bertugas.</summary>
        public float CashierPriceBonus()
        {
            float bonus = 0f;
            for (int i = 0; i < staff.Count; i++)
                if (staff[i].role == StaffRole.Cashier) bonus += 0.06f + 0.02f * (staff[i].level - 1);
            return bonus;
        }

        public void AddReputation(float delta)
        {
            reputation = Mathf.Clamp01(reputation + delta);
            GameEvents.RaiseReputation(reputation);
        }
    }
}
