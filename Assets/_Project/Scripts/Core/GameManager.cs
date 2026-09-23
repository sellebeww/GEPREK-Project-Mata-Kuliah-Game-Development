using System.Collections;
using System.Collections.Generic;
using Geprek.Customers;
using Geprek.Data;
using Geprek.Progression;
using Geprek.Save;
using Geprek.World;
using UnityEngine;

namespace Geprek.Core
{
    /// <summary>
    /// Pengatur alur permainan: state, siklus hari, uang, XP, dan perpindahan arc.
    /// Semua sistem lain memanggil ke sini, bukan sebaliknya.
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {
        [Header("Data")]
        [SerializeField] GameDatabase database;

        [Header("Referensi scene")]
        [SerializeField] LocationManager locations;
        [SerializeField] CutsceneDirector cutscene;

        /// <summary>
        /// Spawner milik tempat usaha yang sedang aktif. Tiap lokasi punya spawner,
        /// kursi, dan meja penyajiannya sendiri, jadi referensinya dicari saat dibutuhkan
        /// bukan dipasang mati ke satu lokasi.
        /// </summary>
        CustomerSpawner spawner => locations != null && locations.ActiveBusiness != null
                                   ? locations.ActiveBusiness.Spawner : null;

        public LocationManager Locations => locations;
        public BusinessLocation ActiveBusiness => locations != null ? locations.ActiveBusiness : null;

        public GameDatabase Database => database;
        public GameConfig Config => database != null ? database.config : null;
        public PlayerProgress Progress { get; private set; } = new();
        public DayStats Today { get; private set; } = new();
        public GameState State { get; private set; } = GameState.Boot;

        /// <summary>Progres hari 0..1. Hanya berjalan saat jam operasional.</summary>
        public float DayProgress { get; private set; }

        public ArcDef CurrentArc => database != null ? database.GetArc(Progress.arcNumber) : null;
        public DayPlanDef CurrentDay => CurrentArc != null ? CurrentArc.GetDay(Progress.dayInArc - 1) : null;
        public bool IsOperating => State == GameState.DayOperating;

        GameState _stateBeforePause;
        Coroutine _closingRoutine;
        bool _lunchRush;

        /// <summary>
        /// Jadi true begitu NewGame/ContinueGame benar-benar berjalan. Tanpa ini,
        /// menutup game dari menu utama (Progress masih kosong bawaan) ikut menimpa
        /// save file yang sudah ada dengan data kosong lewat OnApplicationQuit.
        /// </summary>
        bool _sessionStarted;

        /// <summary>True selama jam sibuk hari ini. Spawner dan UI membacanya.</summary>
        public bool IsLunchRush => _lunchRush;

        // ---- hasil kegiatan medsos semalam, dikunci untuk hari ini ----
        public float TodayCustomerBonus { get; private set; }
        public float TodayPriceScale { get; private set; } = 1f;
        public bool TodayVlogger { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            Application.targetFrameRate = 60;
        }

        void Start()
        {
            SetState(GameState.MainMenu);
            locations?.Show(LocationId.Warung, snapPlayer: true);
        }

        void Update()
        {
            if (State != GameState.DayOperating) return;

            float len = Mathf.Max(1f, Config.dayLengthSeconds);
            DayProgress = Mathf.Clamp01(DayProgress + Time.deltaTime / len);
            GameEvents.RaiseDayProgress(DayProgress);
            UpdateLunchRush();

            if (DayProgress >= 1f) BeginClosing();
        }

        // ---------------------------------------------------------------- state

        void SetState(GameState next)
        {
            if (State == next) return;
            var prev = State;
            State = next;
            GameEvents.RaiseState(prev, next);
        }

        public void TogglePause()
        {
            if (State == GameState.Paused) Resume();
            else Pause();
        }

        public void Pause()
        {
            if (State == GameState.Paused) return;
            _stateBeforePause = State;
            Time.timeScale = 0f;
            SetState(GameState.Paused);
        }

        public void Resume()
        {
            if (State != GameState.Paused) return;
            Time.timeScale = 1f;
            SetState(_stateBeforePause);
        }

        // ---------------------------------------------------------------- mulai permainan

        public bool HasSave => SaveSystem.HasSave;

        public void NewGame()
        {
            _sessionStarted = true;
            SaveSystem.Delete();
            Progress = new PlayerProgress { money = Config.startingMoney };
            Progress.arcStats.Reset(1);

            // resep pembuka: semua yang gratis di arc 1
            foreach (var r in database.recipes)
                if (r != null && r.unlockArc <= 1 && r.unlockCost <= 0 && !r.fromParents && r.unlockLevel <= 1)
                    Progress.unlockedRecipes.Add(r.id);

            GameEvents.RaiseMoney(Progress.money, 0);
            GameEvents.RaiseXp(Progress.xp, Progress.XpToNext(Config), Progress.level);
            GameEvents.RaiseReputation(Progress.reputation);

            // permainan baru dibuka dengan adegan pembuka; melanjutkan save langsung ke briefing
            PlayCutsceneThen(CutsceneId.Opening, () => SetState(GameState.DayBriefing));
        }

        /// <summary>Jalankan adegan lalu lanjutkan. Kalau sistem adegan tidak ada, langsung lanjut.</summary>
        void PlayCutsceneThen(CutsceneId id, System.Action next)
        {
            if (cutscene == null) { next(); return; }
            SetState(GameState.Cutscene);
            cutscene.Play(id, next);
        }

        public void ContinueGame()
        {
            var loaded = SaveSystem.Load();
            if (loaded == null) { NewGame(); return; }
            _sessionStarted = true;
            Progress = loaded;
            GameEvents.RaiseMoney(Progress.money, 0);
            GameEvents.RaiseXp(Progress.xp, Progress.XpToNext(Config), Progress.level);
            GameEvents.RaiseReputation(Progress.reputation);
            SetState(GameState.DayBriefing);
        }

        public void SaveNow() => SaveSystem.Save(Progress);

        public void BackToMenu()
        {
            Time.timeScale = 1f;
            StopAllCoroutines();
            spawner?.StopDay();
            spawner?.ClearAll();
            SetState(GameState.MainMenu);
        }

        // ---------------------------------------------------------------- siklus hari

        public void BeginDay()
        {
            var plan = CurrentDay;
            if (plan == null) { Debug.LogError("[Geprek] Rencana hari tidak ditemukan."); return; }

            locations?.Show(CurrentArc.businessLocation, snapPlayer: true);

            // hari pertama: Ibu tunjukkan alur dapur dulu sebelum jam operasional jalan
            if (!Progress.tutorialSeen)
            {
                Progress.tutorialSeen = true;
                PlayCutsceneThen(CutsceneId.Tutorial, () => StartOperatingHours(plan));
            }
            else
            {
                StartOperatingHours(plan);
            }
        }

        void StartOperatingHours(DayPlanDef plan)
        {
            DayProgress = 0f;
            _lunchRush = false;

            // rencana dari medsos semalam dikunci sekarang, lalu dikosongkan
            TodayCustomerBonus = Progress.tomorrowCustomerBonus;
            TodayPriceScale = Mathf.Max(0.1f, Progress.tomorrowPriceScale);
            TodayVlogger = Progress.vloggerTomorrow;
            Progress.ConsumeTomorrowPlan();

            Today.Reset(Progress.totalDayCount, Progress.arcNumber, plan.targetRevenue);

            ActiveBusiness?.RefreshFryers(ExtraFryers);
            SetState(GameState.DayOperating);

            GameEvents.RaiseDayStarted(plan, Progress.totalDayCount);
            GameEvents.RaiseDayProgress(0f);

            spawner?.StartDay(plan, Progress, database);
        }

        void BeginClosing()
        {
            if (State == GameState.DayClosing) return;
            SetLunchRush(false);
            SetState(GameState.DayClosing);
            spawner?.StopDay();
            GameEvents.RaiseToast("Jam operasional selesai. Selesaikan pesanan terakhir!");
            _closingRoutine = StartCoroutine(WaitForLastCustomers());
        }

        IEnumerator WaitForLastCustomers()
        {
            float t = 0f;
            while (t < Config.closingGraceSeconds)
            {
                if (spawner == null || !spawner.HasActiveCustomers) break;
                t += Time.deltaTime;
                yield return null;
            }
            spawner?.SendEveryoneHome();
            yield return new WaitForSeconds(0.6f);
            EndDay();
        }

        void EndDay()
        {
            if (_closingRoutine != null) { StopCoroutine(_closingRoutine); _closingRoutine = null; }

            // cabang menyetor hasil harian sebelum biaya dihitung
            Today.branchIncome = Progress.BranchIncome();
            if (Today.branchIncome > 0)
            {
                Today.revenue += Today.branchIncome;
                AddMoney(Today.branchIncome, silent: true);
            }

            Today.expenses = Config.dailyIngredientCost + Progress.TotalSalary() + Progress.BranchCost();
            AddMoney(-Today.expenses, silent: true);
            Today.stars = EvaluateStars();

            Progress.lifetimeRevenue += Today.revenue;
            Progress.lifetimeServed += Today.served;
            Progress.arcStats.Accumulate(Today);

            SetState(GameState.DayReport);
            GameEvents.RaiseDayEnded(Today);
            SaveNow();
        }

        /// <summary>Dipanggil tombol "Lanjut" di laporan harian.</summary>
        public void GoHomeForNight()
        {
            locations?.Show(LocationId.Home, snapPlayer: true);
            SetState(GameState.NightHome);
        }

        /// <summary>Dipanggil dari kasur. Menutup hari dan menyiapkan hari berikutnya.</summary>
        public void Sleep()
        {
            Progress.totalDayCount++;
            Progress.dayInArc++;
            SaveNow();

            var arc = CurrentArc;
            if (arc != null && Progress.dayInArc > arc.DayCount)
            {
                SetState(GameState.ArcReport);
                bool passed = Progress.arcStats.revenue >= arc.arcRevenueTarget
                              && Progress.arcStats.AverageSatisfaction >= arc.minSatisfaction;
                GameEvents.RaiseArcEnded(arc, Progress.arcStats, passed);
            }
            else
            {
                SetState(GameState.DayBriefing);
            }
        }

        /// <summary>Dipanggil tombol di laporan arc.</summary>
        public void AdvanceAfterArcReport(bool passed)
        {
            var arc = CurrentArc;
            if (passed && database.GetArc(Progress.arcNumber + 1) != null)
            {
                Progress.arcNumber++;
                Progress.dayInArc = 1;
                Progress.arcStats.Reset(Progress.arcNumber);
                SaveNow();

                var scene = Progress.arcNumber == 2 ? CutsceneId.ToArc2 : CutsceneId.ToArc3;
                PlayCutsceneThen(scene, () => SetState(GameState.DayBriefing));
                return;
            }

            if (passed)
            {
                SaveNow();
                PlayCutsceneThen(CutsceneId.Ending, () => SetState(GameState.GameOver));
                return;
            }

            // belum lulus: ulangi arc dengan hari yang direset, uang tetap
            Progress.dayInArc = 1;
            Progress.arcStats.Reset(Progress.arcNumber);
            GameEvents.RaiseToast("Target belum tercapai. Coba lagi arc ini.");

            SaveNow();
            SetState(GameState.DayBriefing);
        }

        // ---------------------------------------------------------------- uang & xp

        public void AddMoney(int delta, bool silent = false)
        {
            Progress.money = Mathf.Max(0, Progress.money + delta);
            GameEvents.RaiseMoney(Progress.money, silent ? 0 : delta);
        }

        public bool TrySpend(int amount)
        {
            if (amount > Progress.money) return false;
            AddMoney(-amount);
            return true;
        }

        public void AddXp(int amount) => Progress.AddXp(Config, amount);

        // ---------------------------------------------------------------- catatan hari ini

        public void RecordServed(ServeOutcome outcome, int pay, float satisfaction, int xp, RecipeDef recipe = null)
        {
            switch (outcome)
            {
                case ServeOutcome.Perfect: Today.perfect++; break;
                case ServeOutcome.Late: Today.late++; break;
            }
            Today.served++;
            Today.revenue += pay;
            Today.satisfactionSum += satisfaction;
            Today.xpGained += xp;
            if (recipe != null) Today.RecordSale(recipe.id);

            AddMoney(pay);
            AddXp(xp);

            if (outcome == ServeOutcome.Perfect)
                Progress.AddReputation(Config.reputationGainPerPerfect);

            GameEvents.RaiseCustomerResolved(outcome, pay);
        }

        public void RecordLost(ServeOutcome outcome)
        {
            if (outcome == ServeOutcome.NoSeat) Today.turnedAway++;
            else Today.abandoned++;

            Progress.AddReputation(-Config.reputationLossPerAbandon);
            GameEvents.RaiseCustomerResolved(outcome, 0);
        }

        public void RecordWrongOrder() => Today.wrongOrders++;
        public void RecordWaste() => Today.wasted++;

        // ---------------------------------------------------------------- bantuan

        /// <summary>Daftar resep yang sudah terbuka dan boleh dipesan hari ini.</summary>
        public List<RecipeDef> AvailableRecipes(int maxComplexity = 0)
        {
            var list = new List<RecipeDef>();
            foreach (var r in database.recipes)
            {
                if (r == null || !Progress.HasRecipe(r.id)) continue;
                if (maxComplexity > 0 && r.components.Count > maxComplexity) continue;
                list.Add(r);
            }
            return list;
        }

        public float PlayerMoveSpeed =>
            Config.baseMoveSpeed * (1f + Progress.UpgradeValue(database, UpgradeKind.MoveSpeed));

        public float CookSpeedMultiplier =>
            1f + Progress.UpgradeValue(database, UpgradeKind.CookSpeed);

        public float PrepSpeedMultiplier =>
            1f + Progress.UpgradeValue(database, UpgradeKind.PrepSpeed);

        public float PriceBonus =>
            Progress.UpgradeValue(database, UpgradeKind.PriceBonus) + Progress.CashierPriceBonus();

        public float PatienceBonus => Progress.UpgradeValue(database, UpgradeKind.PatienceBonus);

        public int ExtraFryers => Mathf.RoundToInt(Progress.UpgradeValue(database, UpgradeKind.ExtraFryer));
        public int ExtraSeats => Mathf.RoundToInt(Progress.UpgradeValue(database, UpgradeKind.ExtraSeat));


        // ---------------------------------------------------------------- jam sibuk & target

        /// <summary>
        /// Jam sibuk makan siang: pelanggan datang lebih rapat dan sedikit lebih buru-buru,
        /// tapi potensi pemasukannya juga naik. Ditandai lewat event supaya UI bisa
        /// memunculkan spanduk dan spawner menyesuaikan tempo.
        /// </summary>
        void UpdateLunchRush()
        {
            var plan = CurrentDay;
            if (plan == null || !plan.hasLunchRush) { SetLunchRush(false); return; }

            bool active = DayProgress >= plan.rushStart && DayProgress < plan.rushEnd;
            SetLunchRush(active);
        }

        void SetLunchRush(bool active)
        {
            if (_lunchRush == active) return;
            _lunchRush = active;
            GameEvents.RaiseLunchRush(active);
        }

        /// <summary>Daftar target tambahan hari ini. Kosong kalau harinya hanya soal omzet.</summary>
        public System.Collections.Generic.List<DayObjective> TodayObjectives =>
            CurrentDay != null ? CurrentDay.objectives : new System.Collections.Generic.List<DayObjective>();

        /// <summary>Semua target tambahan tercapai?</summary>
        public bool AllObjectivesMet()
        {
            var list = TodayObjectives;
            for (int i = 0; i < list.Count; i++)
                if (!list[i].IsMet(Today)) return false;
            return true;
        }

        /// <summary>
        /// Bintang 1 untuk target omzet, bintang 2 kalau semua target tambahan tercapai,
        /// bintang 3 kalau kepuasan rata-rata juga tinggi.
        /// </summary>
        int EvaluateStars()
        {
            int stars = 0;
            if (Today.TargetMet) stars++;
            if (stars > 0 && AllObjectivesMet()) stars++;
            if (stars > 1 && Today.served > 0 && Today.AverageSatisfaction >= 0.8f) stars++;
            return stars;
        }

        void OnApplicationPause(bool paused) { if (paused && _sessionStarted) SaveNow(); }
        void OnApplicationQuit() { if (_sessionStarted) SaveNow(); }
    }
}
