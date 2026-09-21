using System.Collections.Generic;
using Geprek.Core;
using Geprek.Data;
using UnityEngine;

namespace Geprek.Customers
{
    /// <summary>
    /// Mengatur kedatangan pelanggan: kapan muncul, dapat kursi mana, dan antrean
    /// saat penuh. Juga pemilik jalur jalan yang dipakai semua pelanggan.
    /// </summary>
    public class CustomerSpawner : MonoBehaviour
    {
        [Header("Prefab & titik")]
        [SerializeField] Customer customerPrefab;
        [SerializeField] Transform doorPoint;
        [SerializeField] Transform exitPoint;
        [SerializeField] Transform customerParent;

        [Header("Jalur")]
        [Tooltip("Ketinggian lorong yang selalu bebas perabot. Pelanggan berjalan lewat sini.")]
        [SerializeField] float laneY = -1.6f;

        [Header("Kursi & antrean")]
        [SerializeField] List<Seat> seats = new();
        [SerializeField] List<Transform> queueSpots = new();

        [Header("Ikon reaksi")]
        [SerializeField] Sprite moodHappy;
        [SerializeField] Sprite moodNeutral;
        [SerializeField] Sprite moodAngry;
        [SerializeField] Sprite heartIcon;
        [SerializeField] Sprite coinIcon;

        readonly List<Customer> _active = new();
        readonly List<Customer> _queue = new();

        GameManager _game;
        GameDatabase _db;
        DayPlanDef _plan;

        bool _running;
        float _spawnTimer;
        int _spawnedCount;
        int _plannedCount;

        CustomerTypeDef _specialPending;   // tamu istimewa yang dijadwalkan hari ini
        int _specialAt = -1;               // dimunculkan pada pelanggan ke-berapa

        public bool HasActiveCustomers => _active.Count > 0;

        /// <summary>Pengali kesabaran hari ini, ikut turun saat jam sibuk.</summary>
        public float PatienceMultiplier
        {
            get
            {
                float baseValue = _plan != null ? _plan.patienceMultiplier : 1f;
                bool rush = _game != null && _game.IsLunchRush && _plan != null && _plan.hasLunchRush;
                return rush ? baseValue * _plan.rushPatienceScale : baseValue;
            }
        }
        public int SeatedCount { get { int n = 0; foreach (var s in seats) if (s != null && s.Occupant != null) n++; return n; } }

        // ------------------------------------------------------------- hari

        public void StartDay(DayPlanDef plan, Progression.PlayerProgress progress, GameDatabase db)
        {
            _plan = plan;
            _db = db;
            _game = GameManager.Instance;

            RefreshSeatUnlocks();
            ClearAll();

            // reputasi menambah jumlah pelanggan hari ini
            float repBonus = 1f + progress.reputation * _game.Config.reputationToCustomerBonus;
            float promoBonus = 1f + _game.TodayCustomerBonus;      // hasil promosi/diskon semalam
            _plannedCount = Mathf.Max(1, Mathf.RoundToInt(plan.totalCustomers * repBonus * promoBonus));
            _spawnedCount = 0;
            _specialPending = _game.TodayVlogger ? FindSpecialType() : null;
            _specialAt = _specialPending != null ? Mathf.Max(2, Mathf.RoundToInt(_plannedCount * 0.35f)) : -1;
            _spawnTimer = 2.5f;
            _running = true;
        }

        public void StopDay() => _running = false;

        public void SendEveryoneHome()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
                if (_active[i] != null) _active[i].ForceLeave();
            _queue.Clear();
        }

        public void ClearAll()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
                if (_active[i] != null) Destroy(_active[i].gameObject);
            _active.Clear();
            _queue.Clear();
            foreach (var s in seats) if (s != null) s.Free();
            RaiseQueueEvent();
        }

        /// <summary>Kursi hasil upgrade dinyalakan sesuai jumlah yang sudah dibeli.</summary>
        public void RefreshSeatUnlocks()
        {
            int extra = _game != null ? _game.ExtraSeats : 0;
            int used = 0;
            foreach (var seat in seats)
            {
                if (seat == null) continue;
                if (!seat.IsUpgradeSeat) { seat.SetUnlocked(true); continue; }
                bool on = used < extra;
                seat.SetUnlocked(on);
                if (on) used++;
            }
        }

        void Update()
        {
            if (!_running || _game == null) return;
            if (_spawnedCount >= _plannedCount) return;

            _spawnTimer -= Time.deltaTime;
            if (_spawnTimer > 0f) return;

            SpawnOne();

            float t = _game.DayProgress;
            float interval = Mathf.Lerp(_plan.spawnIntervalStart, _plan.spawnIntervalEnd, t);

            // jam sibuk merapatkan kedatangan, jadi antrean benar-benar terasa
            if (_game.IsLunchRush && _plan.hasLunchRush) interval *= _plan.rushSpawnScale;

            _spawnTimer = Mathf.Max(1.2f, interval * Random.Range(0.85f, 1.15f));
        }

        // ------------------------------------------------------------- spawn

        /// <summary>Jenis tamu istimewa pertama yang tersedia di database.</summary>
        CustomerTypeDef FindSpecialType()
        {
            if (_db == null) return null;
            foreach (var t in _db.customerTypes)
                if (t != null && t.isSpecialGuest) return t;
            return null;
        }

        void SpawnOne()
        {
            if (customerPrefab == null || doorPoint == null) return;

            CustomerTypeDef type;
            if (_specialPending != null && _spawnedCount >= _specialAt)
            {
                type = _specialPending;
                _specialPending = null;
                GameEvents.RaiseToast($"{type.displayName} datang! Layani dengan sempurna.", moodHappy);
                Audio.AudioManager.Play(SfxId.Levelup);
            }
            else
            {
                type = PickType();
            }
            var customer = Instantiate(customerPrefab, doorPoint.position,
                                       Quaternion.identity, customerParent != null ? customerParent : transform);
            customer.Init(this, type, _game);
            _active.Add(customer);
            _spawnedCount++;

            var seat = FindFreeSeat();
            if (seat != null)
            {
                customer.GoToSeat(seat, PathToSeat(doorPoint.position, seat));
            }
            else if (_queue.Count < _game.Config.maxQueue && _queue.Count < queueSpots.Count)
            {
                var spot = queueSpots[_queue.Count];
                _queue.Add(customer);
                customer.GoToQueue(spot.position, new[] { LanePoint(doorPoint.position), LanePoint(spot.position), spot.position });
            }
            else
            {
                customer.TurnAway();
            }

            RaiseQueueEvent();
        }

        CustomerTypeDef PickType()
        {
            if (_db == null || _db.customerTypes.Count == 0) return null;

            int arc = _game != null ? _game.Progress.arcNumber : 1;
            float total = 0f;
            foreach (var t in _db.customerTypes)
                if (t != null && !t.isSpecialGuest && t.minArc <= arc) total += Mathf.Max(0f, t.spawnWeight);

            if (total <= 0f) return _db.customerTypes[0];

            float roll = Random.Range(0f, total);
            foreach (var t in _db.customerTypes)
            {
                if (t == null || t.isSpecialGuest || t.minArc > arc) continue;
                roll -= Mathf.Max(0f, t.spawnWeight);
                if (roll <= 0f) return t;
            }
            return _db.customerTypes[0];
        }

        /// <summary>Menu yang dipesan pelanggan berikutnya, dipilih acak dari resep yang terbuka.</summary>
        public RecipeDef PickRecipe()
        {
            var pool = _game.AvailableRecipes(_plan != null ? _plan.maxRecipeComplexity : 0);
            if (pool.Count == 0) pool = _game.AvailableRecipes();
            if (pool.Count == 0) return null;
            return pool[Random.Range(0, pool.Count)];
        }

        // ------------------------------------------------------------- kursi & antrean

        Seat FindFreeSeat()
        {
            foreach (var s in seats)
                if (s != null && s.IsFree) return s;
            return null;
        }

        public void NotifySeatFreed(Seat seat)
        {
            if (_queue.Count == 0 || seat == null) return;

            var next = _queue[0];
            _queue.RemoveAt(0);
            if (next == null || !next.IsWaitingForSeat) { RaiseQueueEvent(); return; }

            next.GoToSeat(seat, PathToSeat(next.transform.position, seat));
            ShiftQueue();
            RaiseQueueEvent();
        }

        void ShiftQueue()
        {
            for (int i = 0; i < _queue.Count && i < queueSpots.Count; i++)
            {
                if (_queue[i] == null) continue;
                _queue[i].WalkTo(new[] { queueSpots[i].position });
            }
        }

        public void NotifyDespawn(Customer customer)
        {
            _active.Remove(customer);
            _queue.Remove(customer);
            RaiseQueueEvent();
        }

        void RaiseQueueEvent() => GameEvents.RaiseQueue(SeatedCount, _queue.Count);

        // ------------------------------------------------------------- jalur

        Vector3 LanePoint(Vector3 from) => new(from.x, laneY, from.z);

        IEnumerable<Vector3> PathToSeat(Vector3 from, Seat seat)
        {
            yield return LanePoint(from);
            yield return LanePoint(seat.ApproachPosition);
            yield return seat.ApproachPosition;
            yield return seat.SitPosition;
        }

        public IEnumerable<Vector3> ExitPath(Vector3 from)
        {
            var target = exitPoint != null ? exitPoint.position : doorPoint.position;
            return new[] { LanePoint(from), LanePoint(target), target };
        }

        public Sprite MoodSpriteFor(float satisfaction) =>
            satisfaction > 0.6f ? moodHappy : satisfaction > 0.25f ? moodNeutral : moodAngry;

        public Sprite HeartSprite => heartIcon != null ? heartIcon : moodHappy;
        public Sprite CoinSprite => coinIcon;
    }
}
