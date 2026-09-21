using System;
using Geprek.Data;
using UnityEngine;

namespace Geprek.Core
{
    /// <summary>
    /// Papan pengumuman global. Sistem gameplay menembakkan event di sini, UI mendengarkan.
    /// Dipakai supaya UI tidak perlu tahu kelas gameplay mana pun secara langsung.
    /// </summary>
    public static class GameEvents
    {
        public static event Action<GameState, GameState> StateChanged;      // lama, baru
        public static event Action<int, int> MoneyChanged;                   // total, selisih
        public static event Action<int, int, int> XpChanged;                 // xp, xpUntukLevelBerikut, level
        public static event Action<int> LevelUp;
        public static event Action<float> ReputationChanged;
        public static event Action<float> DayProgressChanged;                // 0..1
        public static event Action<bool> LunchRushChanged;                   // true saat jam sibuk mulai

        public static event Action<DayPlanDef, int> DayStarted;              // rencana, nomor hari global
        public static event Action<DayStats> DayEnded;
        public static event Action<ArcDef, ArcStats, bool> ArcEnded;         // arc, rekap, lulus

        public static event Action<ServeOutcome, int> CustomerResolved;      // hasil, uang diterima
        public static event Action<int, int> QueueChanged;                   // duduk, mengantre

        public static event Action<RecipeDef> RecipeUnlocked;
        public static event Action<UpgradeDef, int> UpgradePurchased;
        public static event Action StaffChanged;
        public static event Action<CarriedItem> CarryChanged;                // null = tangan kosong
        public static event Action<string, Sprite> Toast;

        /// <summary>Teks mengambang di posisi dunia: "+Rp18.000", "PERFECT", dan sejenisnya.</summary>
        public static event Action<string, Sprite, Vector3, Color> Popup;

        public static void RaiseState(GameState from, GameState to) => StateChanged?.Invoke(from, to);
        public static void RaiseMoney(int total, int delta) => MoneyChanged?.Invoke(total, delta);
        public static void RaiseXp(int xp, int need, int level) => XpChanged?.Invoke(xp, need, level);
        public static void RaiseLevelUp(int level) => LevelUp?.Invoke(level);
        public static void RaiseReputation(float rep) => ReputationChanged?.Invoke(rep);
        public static void RaiseDayProgress(float t) => DayProgressChanged?.Invoke(t);
        public static void RaiseLunchRush(bool active) => LunchRushChanged?.Invoke(active);
        public static void RaiseDayStarted(DayPlanDef plan, int dayNumber) => DayStarted?.Invoke(plan, dayNumber);
        public static void RaiseDayEnded(DayStats stats) => DayEnded?.Invoke(stats);
        public static void RaiseArcEnded(ArcDef arc, ArcStats stats, bool passed) => ArcEnded?.Invoke(arc, stats, passed);
        public static void RaiseCustomerResolved(ServeOutcome o, int pay) => CustomerResolved?.Invoke(o, pay);
        public static void RaiseQueue(int seated, int queued) => QueueChanged?.Invoke(seated, queued);
        public static void RaiseRecipeUnlocked(RecipeDef r) => RecipeUnlocked?.Invoke(r);
        public static void RaiseUpgradePurchased(UpgradeDef u, int lvl) => UpgradePurchased?.Invoke(u, lvl);
        public static void RaiseStaffChanged() => StaffChanged?.Invoke();
        public static void RaiseCarryChanged(CarriedItem item) => CarryChanged?.Invoke(item);
        public static void RaiseToast(string msg, Sprite icon = null) => Toast?.Invoke(msg, icon);
        public static void RaisePopup(string text, Sprite icon, Vector3 world, Color color)
            => Popup?.Invoke(text, icon, world, color);

        /// <summary>
        /// Event statis bertahan antar sesi play di Editor. Bersihkan sebelum scene dimuat,
        /// bukan dari Awake sebuah manager: urutan Awake antar objek tidak dijamin, dan
        /// membersihkan di sana bisa menghapus langganan objek yang sudah sempat mendaftar.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay() => ClearAll();

        public static void ClearAll()
        {
            StateChanged = null; MoneyChanged = null; XpChanged = null; LevelUp = null; LunchRushChanged = null;
            ReputationChanged = null; DayProgressChanged = null;
            DayStarted = null; DayEnded = null; ArcEnded = null;
            CustomerResolved = null; QueueChanged = null;
            RecipeUnlocked = null; UpgradePurchased = null; CarryChanged = null; Toast = null; Popup = null; StaffChanged = null;
        }
    }
}
