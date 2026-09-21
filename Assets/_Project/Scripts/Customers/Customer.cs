using System.Collections.Generic;
using Geprek.Core;
using Geprek.Data;
using Geprek.Economy;
using Geprek.Player;
using Geprek.Stations;
using Geprek.Utils;
using Geprek.World;
using UnityEngine;

namespace Geprek.Customers
{
    /// <summary>
    /// Seorang pelanggan dari masuk pintu sampai keluar lagi. Pelanggan juga sebuah
    /// <see cref="IInteractable"/>: pemain menghampiri lalu menekan tombol untuk mengantar pesanan.
    ///
    /// Alurnya: masuk -> antre/duduk -> pesan -> tunggu -> senang atau marah ->
    /// makan -> bayar -> pulang. Saat duduk pelanggan tidak diam kaku: sesekali
    /// menoleh, melihat pesanannya, atau melirik jam, dengan jeda acak per orang
    /// supaya tidak semuanya bergerak seragam.
    /// </summary>
    public class Customer : MonoBehaviour, IInteractable
    {
        [Header("Komponen")]
        [SerializeField] CharacterAnimator animator;
        [SerializeField] OrderBubble bubble;
        [SerializeField] SortingByY sorting;
        [Tooltip("Cincin di kaki pelanggan saat pemain cukup dekat untuk mengantar pesanan.")]
        [SerializeField] SpriteRenderer highlightGlow;
        [Tooltip("Bintang kecil di atas kepala, hanya untuk tamu istimewa.")]
        [SerializeField] SpriteRenderer specialBadge;

        [Header("Gerak")]
        [SerializeField] float walkSpeed = 2.6f;
        [SerializeField] float arriveDistance = 0.06f;
        [Tooltip("Variasi kecepatan jalan antar pelanggan, dalam persen.")]
        [SerializeField] float speedVariance = 0.12f;

        [Header("Jeda tiap tahap")]
        [SerializeField] float happyDuration = 0.7f;
        [SerializeField] float angryDuration = 0.9f;
        [SerializeField] float payDuration = 0.7f;

        public CustomerState State { get; private set; } = CustomerState.Entering;
        public RecipeDef Order { get; private set; }
        public CustomerTypeDef Type { get; private set; }
        public Seat AssignedSeat { get; private set; }

        /// <summary>Sisa kesabaran 0..1.</summary>
        public float PatienceLeft => _patienceMax <= 0f ? 1f : Mathf.Clamp01(_patience / _patienceMax);

        public Transform Transform => transform;
        public bool IsWaitingForSeat => State == CustomerState.Queueing;

        CustomerSpawner _spawner;
        GameManager _game;
        readonly Queue<Vector3> _path = new();
        Vector3 _target;
        bool _hasTarget;

        float _patience;
        float _patienceMax;
        float _stateTimer;
        float _idleTimer;
        float _speed;
        float _lastSatisfaction;

        // ------------------------------------------------------------- setup

        public void Init(CustomerSpawner spawner, CustomerTypeDef type, GameManager game)
        {
            _spawner = spawner;
            _game = game;
            Type = type;

            if (animator != null && type != null && type.skin != null) animator.SetSkin(type.skin);
            if (sorting != null) sorting.SetDynamic(true);
            if (highlightGlow != null) highlightGlow.enabled = false;
            if (specialBadge != null) specialBadge.enabled = type != null && type.isSpecialGuest;
            bubble?.Hide();

            _speed = walkSpeed * Random.Range(1f - speedVariance, 1f + speedVariance);
            _idleTimer = Random.Range(1.5f, 3.5f);

            State = CustomerState.Entering;
            _hasTarget = false;
            _path.Clear();
        }

        public void WalkTo(IEnumerable<Vector3> waypoints)
        {
            _path.Clear();
            foreach (var p in waypoints) _path.Enqueue(p);
            NextWaypoint();
        }

        public void GoToSeat(Seat seat, IEnumerable<Vector3> path)
        {
            AssignedSeat = seat;
            seat.Assign(this);
            State = CustomerState.Seating;
            WalkTo(path);
        }

        public void GoToQueue(Vector3 spot, IEnumerable<Vector3> path)
        {
            State = CustomerState.Queueing;
            WalkTo(path);
            _target = spot;
        }

        // ------------------------------------------------------------- siklus

        void BeginOrdering()
        {
            State = CustomerState.Ordering;
            _stateTimer = 0.7f;
            animator?.SetFacing(Facing.Down);
        }

        void PlaceOrder()
        {
            Order = _spawner != null ? _spawner.PickRecipe() : null;
            if (Order == null) { GoLeave(silent: true); return; }

            var cfg = _game.Config;
            _patienceMax = cfg.basePatience
                           * (Type != null ? Type.patienceMultiplier : 1f)
                           * Order.patienceMultiplier
                           * _spawner.PatienceMultiplier
                           * (1f + _game.PatienceBonus);
            _patience = _patienceMax;

            State = CustomerState.Waiting;
            bubble?.ShowOrder(Order.icon);
            Audio.AudioManager.Play(SfxId.Notify);
            JuiceDirector.DoPunch(bubble != null ? bubble.transform : transform, 0.25f);
        }

        void Update()
        {
            MoveAlongPath();

            switch (State)
            {
                case CustomerState.Ordering:
                    if (Countdown()) PlaceOrder();
                    break;

                case CustomerState.Waiting:
                    _patience -= Time.deltaTime;
                    bubble?.SetPatience(PatienceLeft);
                    IdleLookAround();
                    if (_patience <= 0f) EnterAngry();
                    break;

                case CustomerState.Happy:
                    if (Countdown()) EnterEating();
                    break;

                case CustomerState.Eating:
                    IdleLookAround(faceDownBias: true);
                    if (Countdown()) EnterPaying();
                    break;

                case CustomerState.Paying:
                    if (Countdown()) GoLeave(silent: true);
                    break;

                case CustomerState.Angry:
                    if (Countdown()) GoLeave(silent: true);
                    break;
            }
        }

        bool Countdown()
        {
            _stateTimer -= Time.deltaTime;
            return _stateTimer <= 0f;
        }

        /// <summary>Gerak kecil saat duduk supaya ruangan tidak terasa mati.</summary>
        void IdleLookAround(bool faceDownBias = false)
        {
            if (_hasTarget) return;

            _idleTimer -= Time.deltaTime;
            if (_idleTimer > 0f) return;
            _idleTimer = Random.Range(1.8f, 4.2f);

            // sebagian besar waktu menghadap depan (ke arah dapur), sesekali menoleh
            float roll = Random.value;
            Facing next = faceDownBias || roll < 0.55f ? Facing.Down
                        : roll < 0.78f ? Facing.Left
                        : roll < 0.95f ? Facing.Right
                        : Facing.Up;
            animator?.SetFacing(next);
        }

        // ------------------------------------------------------------- jalan

        void MoveAlongPath()
        {
            if (!_hasTarget)
            {
                animator?.SetMoving(false);
                return;
            }

            Vector3 pos = transform.position;
            Vector3 delta = _target - pos;
            if (delta.sqrMagnitude <= arriveDistance * arriveDistance)
            {
                transform.position = _target;
                NextWaypoint();
                return;
            }

            Vector3 step = delta.normalized * _speed * Time.deltaTime;
            transform.position = pos + step;
            animator?.Drive(step / Mathf.Max(0.0001f, Time.deltaTime));
        }

        void NextWaypoint()
        {
            if (_path.Count > 0)
            {
                _target = _path.Dequeue();
                _hasTarget = true;
                return;
            }

            _hasTarget = false;
            animator?.SetMoving(false);
            OnPathFinished();
        }

        void OnPathFinished()
        {
            switch (State)
            {
                case CustomerState.Seating: BeginOrdering(); break;
                case CustomerState.Leaving: Despawn(); break;
                case CustomerState.Queueing: animator?.SetFacing(Facing.Up); break;
            }
        }

        // ------------------------------------------------------------- interaksi

        public bool CanInteract(PlayerCarry carry) =>
            State == CustomerState.Waiting && carry != null && carry.HasItem && carry.Held.IsDish;

        public string Hint(PlayerCarry carry)
        {
            if (State != CustomerState.Waiting) return "";
            if (carry == null || !carry.HasItem) return $"Minta {Order?.displayName}";
            if (!carry.Held.IsDish) return "Bukan porsi siap saji";
            return carry.Held.recipe == Order ? $"Antar {Order.displayName}" : "Menu tidak sesuai";
        }

        public bool UsesHold(PlayerCarry carry) => false;
        public void HoldTick(PlayerCarry carry, float deltaTime) { }
        public void HoldCancelled() { }
        public void SetHighlighted(bool highlighted)
        {
            if (highlightGlow != null) highlightGlow.enabled = highlighted;
        }

        public void Interact(PlayerCarry carry)
        {
            if (!CanInteract(carry)) return;

            var dish = carry.Release();
            Vector3 head = transform.position + Vector3.up * 1.7f;

            if (dish.recipe != Order)
            {
                _patience -= _patienceMax * 0.3f;
                bubble?.SetPatience(PatienceLeft);
                _game.RecordWrongOrder();
                _game.RecordWaste();
                Audio.AudioManager.Play(SfxId.Error);
                JuiceDirector.DoPopup("Salah menu!", null, head, new Color(0.88f, 0.34f, 0.28f));
                GameEvents.RaiseToast($"Pelanggan minta {Order.displayName}!");
                return;
            }

            var cfg = _game.Config;
            int pay = Payout.Compute(cfg, Order, Type, PatienceLeft, dish.quality, _game.PriceBonus,
                                     out var outcome, out float satisfaction, _game.TodayPriceScale);

            _lastSatisfaction = satisfaction;
            _game.RecordServed(outcome, pay, satisfaction, Order.xpReward, Order);
            Audio.AudioManager.Play(SfxId.Coin);

            JuiceDirector.DoPopup($"+{MathUtil.ToRupiah(pay)}", _spawner != null ? _spawner.CoinSprite : null,
                                  head, new Color(1f, 0.83f, 0.32f));

            if (outcome == ServeOutcome.Perfect)
            {
                JuiceDirector.DoSparkle(head, new Color(1f, 0.85f, 0.4f), 7);
                JuiceDirector.DoPopup("PERFECT", null, head + Vector3.up * 0.45f, new Color(1f, 0.9f, 0.45f));
            }

            if (Type != null && Type.isSpecialGuest) ResolveSpecialGuest(outcome);

            EnterHappy();
        }

        // ------------------------------------------------------------- tahap reaksi

        void EnterHappy()
        {
            State = CustomerState.Happy;
            _stateTimer = happyDuration;
            bubble?.ShowReaction(_spawner != null ? _spawner.HeartSprite : null);
            JuiceDirector.DoPunch(bubble != null ? bubble.transform : transform, 0.3f);
        }

        void EnterEating()
        {
            State = CustomerState.Eating;
            _stateTimer = _game != null ? _game.Config.eatDuration : 3.5f;
            bubble?.ShowReaction(Order != null ? Order.icon : null);
        }

        void EnterPaying()
        {
            State = CustomerState.Paying;
            _stateTimer = payDuration;
            bubble?.ShowReaction(_spawner != null ? _spawner.CoinSprite : null);
            Audio.AudioManager.Play(SfxId.Coin);
        }

        /// <summary>
        /// Tamu istimewa seperti food vlogger memberi lonjakan reputasi kalau puas,
        /// dan pukulan balik kalau kecewa. Inilah taruhan dari mengundangnya.
        /// </summary>
        void ResolveSpecialGuest(ServeOutcome outcome)
        {
            if (_game == null || Type == null) return;

            if (outcome == ServeOutcome.Perfect)
            {
                _game.Progress.AddReputation(Type.specialReputationGain);
                GameEvents.RaiseToast($"{Type.displayName} terkesan! Reputasi melonjak.", Order != null ? Order.icon : null);
                JuiceDirector.DoSparkle(transform.position + Vector3.up * 1.8f, new Color(0.6f, 0.9f, 1f), 12, 0.8f);
                JuiceDirector.DoPopup("VIRAL!", null, transform.position + Vector3.up * 2.3f, new Color(0.6f, 0.9f, 1f));
            }
            else
            {
                GameEvents.RaiseToast($"{Type.displayName}: \"Biasa saja...\"");
            }
        }

        void EnterAngry()
        {
            if (State == CustomerState.Angry) return;

            State = CustomerState.Angry;
            _stateTimer = angryDuration;

            _game?.RecordLost(ServeOutcome.Abandoned);
            if (Type != null && Type.isSpecialGuest)
            {
                _game?.Progress.AddReputation(-Type.specialReputationLoss);
                GameEvents.RaiseToast($"{Type.displayName} pulang kecewa. Reputasi turun.");
            }
            Audio.AudioManager.Play(SfxId.CustomerAngry);
            bubble?.ShowReaction(_spawner != null ? _spawner.MoodSpriteFor(0f) : null);
            JuiceDirector.DoPopup("Kelamaan!", null, transform.position + Vector3.up * 1.7f,
                                  new Color(0.9f, 0.4f, 0.32f));
            JuiceDirector.DoPunch(bubble != null ? bubble.transform : transform, 0.25f);
        }

        // ------------------------------------------------------------- keluar

        /// <summary>Paksa pulang saat warung tutup.</summary>
        public void ForceLeave()
        {
            if (State is CustomerState.Leaving or CustomerState.Done) return;
            if (State == CustomerState.Waiting) { EnterAngry(); return; }
            GoLeave(silent: true);
        }

        void GoLeave(bool silent)
        {
            if (State is CustomerState.Leaving or CustomerState.Done) return;

            if (!silent) _game?.RecordLost(ServeOutcome.Abandoned);
            bubble?.Hide();

            State = CustomerState.Leaving;
            if (AssignedSeat != null)
            {
                AssignedSeat.Free();
                _spawner?.NotifySeatFreed(AssignedSeat);
                AssignedSeat = null;
            }

            var exit = _spawner != null ? _spawner.ExitPath(transform.position) : null;
            if (exit != null) WalkTo(exit); else Despawn();
        }

        /// <summary>Pelanggan pulang sebelum sempat masuk karena tempat penuh.</summary>
        public void TurnAway()
        {
            _game?.RecordLost(ServeOutcome.NoSeat);
            bubble?.ShowReaction(_spawner != null ? _spawner.MoodSpriteFor(0f) : null);
            JuiceDirector.DoPopup("Penuh...", null, transform.position + Vector3.up * 1.7f,
                                  new Color(0.85f, 0.55f, 0.3f));

            State = CustomerState.Leaving;
            var exit = _spawner != null ? _spawner.ExitPath(transform.position) : null;
            if (exit != null) WalkTo(exit); else Despawn();
        }

        void Despawn()
        {
            State = CustomerState.Done;
            _spawner?.NotifyDespawn(this);
            Destroy(gameObject);
        }
    }
}
