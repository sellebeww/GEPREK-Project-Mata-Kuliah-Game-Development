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
        float[] _arrivals = System.Array.Empty<float>();   // waktu datang tiap pelanggan (progres hari 0..1)
        int _nextArrival;
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
            _arrivals = BuildSchedule(_plannedCount, _game.Config.dayLengthSeconds, plan.hasLunchRush,
                                      plan.rushStart, plan.rushEnd, plan.rushSpawnScale, new System.Random());
            _nextArrival = 0;
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
            if (_nextArrival >= _arrivals.Length) return;

            // kedatangan mengikuti jam warung, bukan timer acak: pagi sepi, makan siang ramai
            if (_game.DayProgress < _arrivals[_nextArrival]) return;
            _nextArrival++;
            SpawnOne();
        }

        // ------------------------------------------------------------- jadwal

        /// <summary>Pelanggan pertama boleh datang setelah warung buka sebentar.</summary>
        public const float FirstArrival = 0.03f;
        /// <summary>Pelanggan terakhir datang sebelum tutup supaya masih sempat dilayani.</summary>
        public const float LastArrival = 0.86f;
        /// <summary>Jeda minimum antar pelanggan (detik) supaya pemain sempat membagi waktu.</summary>
        public const float MinGapSeconds = 2.2f;

        /// <summary>Kerapatan kedatangan relatif pada saat t (progres hari).</summary>
        public static float ArrivalDensity(float t, bool hasRush, float rushStart, float rushEnd, float rushScale)
        {
            // pagi: pelan lalu naik
            float d = Mathf.Lerp(0.55f, 1f, Mathf.SmoothStep(0f, 1f, t / 0.25f));
            if (hasRush)
            {
                float boost = Mathf.Clamp(1f / Mathf.Max(0.2f, rushScale), 1f, 2.5f);
                const float edge = 0.04f;
                float inside = Mathf.SmoothStep(0f, 1f, (t - (rushStart - edge)) / (2f * edge))
                             * (1f - Mathf.SmoothStep(0f, 1f, (t - (rushEnd - edge)) / (2f * edge)));
                d *= Mathf.Lerp(1f, boost, inside);
                if (t > rushEnd + edge) d *= 0.75f;       // sore melandai
            }
            return d;
        }

        /// <summary>Waktu datang (progres hari, terurut) untuk <paramref name="count"/> pelanggan.</summary>
        public static float[] BuildSchedule(int count, float dayLengthSeconds, bool hasRush,
                                            float rushStart, float rushEnd, float rushScale, System.Random rng)
        {
            if (count <= 0) return System.Array.Empty<float>();

            const int steps = 256;
            var cdf = new float[steps + 1];
            for (int i = 1; i <= steps; i++)
            {
                float t = Mathf.Lerp(FirstArrival, LastArrival, (i - 0.5f) / steps);
                cdf[i] = cdf[i - 1] + ArrivalDensity(t, hasRush, rushStart, rushEnd, rushScale);
            }

            var result = new float[count];
            for (int n = 0; n < count; n++)
            {
                // satu pelanggan per irisan kerapatan, digeser sedikit supaya tidak terlalu rapi
                float jitter = (float)(rng.NextDouble() - 0.5) * 0.7f;
                float q = Mathf.Clamp01((n + 0.5f + jitter) / count) * cdf[steps];
                int k = 1;
                while (k < steps && cdf[k] < q) k++;
                float span = Mathf.Max(1e-5f, cdf[k] - cdf[k - 1]);
                float frac = Mathf.Clamp01((q - cdf[k - 1]) / span);
                result[n] = Mathf.Lerp(FirstArrival, LastArrival, (k - 1 + frac) / steps);
            }
            System.Array.Sort(result);

            // jeda minimum: dorong ke depan, lalu tarik mundur dari batas akhir bila meluap
            float window = LastArrival - FirstArrival;
            float gap = Mathf.Min(MinGapSeconds / Mathf.Max(1f, dayLengthSeconds), window / count * 0.9f);
            for (int i = 1; i < count; i++) result[i] = Mathf.Max(result[i], result[i - 1] + gap);
            for (int i = count - 1; i >= 0; i--)
            {
                float limit = LastArrival - (count - 1 - i) * gap;
                if (result[i] > limit) result[i] = limit;
            }
            return result;
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
