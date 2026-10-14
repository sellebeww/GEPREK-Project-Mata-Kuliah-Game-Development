using Geprek.Core;
using Geprek.Customers;
using Geprek.Data;
using Geprek.Player;
using Geprek.Stations;
using Geprek.World;
using UnityEngine;

namespace Geprek.Progression
{
    /// <summary>
    /// Karyawan di lapangan. Perannya menentukan pekerjaan yang diambil:
    ///
    /// - Pramusaji mengangkat porsi jadi dari meja penyajian lalu mengantarnya.
    /// - Juru masak menggoreng, menggeprek, lalu meracik seluruh bahan pesanan.
    /// - Kasir berjaga di tempatnya; efeknya berupa bonus harga, bukan gerakan.
    ///
    /// Klaim stasiun mencegah sesama karyawan mengambil porsi yang belum selesai.
    /// </summary>
    public class StaffWorker : MonoBehaviour
    {
        enum Job { Idle, ToPlateForDish, ToCustomer, ToSource, ToFryer, WaitFryer, ToCobek, WorkCobek, ToPlateDrop, ToIngredient, AddIngredient, ToTrash, Returning }

        [Header("Komponen")]
        [SerializeField] CharacterAnimator animator;
        [SerializeField] PlayerCarry carry;
        [SerializeField] SortingByY sorting;

        [Header("Kerja")]
        [SerializeField] PlateStation plate;
        [SerializeField] Transform idleSpot;
        [SerializeField] float walkSpeed = 3.2f;
        [SerializeField] float arriveDistance = 0.14f;
        [SerializeField] float thinkDelay = 0.45f;

        StaffMember _member;
        Job _job = Job.Idle;
        Customer _target;
        CookStation _fryer;
        PrepStation _cobek;
        IngredientSource _chickenSource;
        Vector3 _moveTo;
        float _timer;
        float _holdTimeout;
        RecipeDef _recipe;
        int _componentIndex;
        IngredientSource _ingredient;
        RecipeDef _unclaimedDish;
        float _unclaimedSince;
        static readonly System.Collections.Generic.Dictionary<Component, StaffWorker> Claims = new();
        TextMesh _nameplate;
        string _displayedStatus, _displayedName;
        readonly System.Collections.Generic.Queue<Vector3> _route = new();
        Vector3 _routeDestination = Vector3.positiveInfinity;
        public string WorkStatus => Role == StaffRole.Cashier ? "Kasir" : _job switch {
            Job.WaitFryer => "Menggoreng", Job.WorkCobek => "Menggeprek",
            Job.ToCustomer => "Mengantar", Job.ToIngredient or Job.AddIngredient => "Meracik",
            Job.Idle or Job.Returning => "Siap kerja", _ => "Menyiapkan" };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ClearClaims() => Claims.Clear();

        bool Available(Component station) => station != null && (!Claims.TryGetValue(station, out var owner) || owner == null || owner == this);
        void ReleaseClaims()
        {
            foreach (var key in new System.Collections.Generic.List<Component>(Claims.Keys))
                if (Claims[key] == this || Claims[key] == null) Claims.Remove(key);
        }
        void OnDisable() => ReleaseClaims();

        public void ResetForDay()
        {
            ReleaseClaims();
            carry?.Clear();
            _job = Job.Idle;
            _target = null;
            _recipe = null;
            _fryer = null;
            _cobek = null;
            _unclaimedDish = null;
            _route.Clear();
            _routeDestination = Vector3.positiveInfinity;
            _timer = thinkDelay;
            if (idleSpot != null) transform.position = idleSpot.position;
            animator?.SetPose(CharacterPose.Standing);
        }

        GameManager Game => GameManager.Instance;
        StaffRole Role => _member != null ? _member.role : StaffRole.Server;
        float Speed => walkSpeed * (_member != null ? _member.Speed : 1f);

        void Awake()
        {
            if (carry == null) carry = GetComponent<PlayerCarry>();
            carry?.SetBroadcast(false);
            if (sorting != null) sorting.SetDynamic(true);
        }

        public void Configure(StaffMember member, PlateStation plateStation, Transform idle)
        {
            if (_member != null && _member.id != member.id)
            {
                ReleaseClaims();
                if (carry != null && carry.HasItem) { carry.Clear(); Game?.RecordWaste(); }
                _job = Job.Idle; _target = null; _recipe = null;
                _route.Clear(); _routeDestination = Vector3.positiveInfinity;
                animator?.SetPose(CharacterPose.Standing);
            }
            _member = member;
            plate = plateStation;
            idleSpot = idle;
            if (_nameplate == null)
                _nameplate = WorldLabel.Create(transform, member.displayName, new(0,1.35f,0), .025f,
                    new Color(1,.91f,.66f), 1100).GetComponent<TextMesh>();
        }

        void Update()
        {
            var game = Game;
            bool working = game != null && (game.IsOperating || game.State == GameState.DayClosing);
            if (_nameplate != null && _member != null && (_displayedStatus != WorkStatus || _displayedName != _member.displayName))
            {
                _displayedStatus = WorkStatus;
                _displayedName = _member.displayName;
                _nameplate.text = _displayedName + " · " + _displayedStatus;
            }

            if (!working || Role == StaffRole.Cashier)
            {
                bool arrived = Walk(idleSpot != null ? idleSpot.position : transform.position);
                if (arrived && working && Role == StaffRole.Cashier)
                { animator?.SetFacing(Facing.Down); animator?.SetPose(CharacterPose.Working); }
                return;
            }

            _timer -= Time.deltaTime;

            if (_job == Job.ToTrash)
            {
                var trash = transform.root.GetComponentInChildren<TrashBin>();
                if (trash != null && !Walk(trash.transform.position + Vector3.down * .8f)) return;
                if (trash != null) trash.Interact(carry);
                else if (carry.HasItem) { carry.Clear(); game.RecordWaste(); }
                ReturnHome();
                return;
            }

            if (Role == StaffRole.Server) TickServer();
            else TickCook();
        }

        // ---------------------------------------------------------------- pramusaji

        void TickServer()
        {
            switch (_job)
            {
                case Job.Idle:
                    Walk(IdlePosition());
                    if (_timer <= 0f) LookForDelivery();
                    break;

                case Job.ToPlateForDish:
                    if (Walk(_moveTo)) PickUpDish();
                    break;

                case Job.ToCustomer:
                    if (_target == null || _target.State != CustomerState.Waiting) { ReturnHome(); break; }
                    _moveTo = _target.AssignedSeat != null ? _target.AssignedSeat.ApproachPosition : _target.transform.position + Vector3.up * .7f;
                    if (Walk(_moveTo)) Deliver();
                    break;

                case Job.Returning:
                    if (Walk(IdlePosition())) { _job = Job.Idle; _timer = thinkDelay; }
                    break;
            }
        }

        void LookForDelivery()
        {
            _timer = thinkDelay;
            if (carry == null) return;
            if (carry.HasItem)
            {
                _target = carry.Held.recipe != null ? FindCustomerFor(carry.Held.recipe) : null;
                if (_target != null) _job = Job.ToCustomer;
                return;
            }
            if (!Available(plate) || plate.Match == null) return;

            _target = FindCustomerFor(plate.Match);
            if (_target == null)
            {
                if (_unclaimedDish != plate.Match) { _unclaimedDish = plate.Match; _unclaimedSince = Time.time; }
                if (Time.time - _unclaimedSince < 6f) return;
                // The customer may have left. Clear the abandoned meal so cooking can continue.
            }
            else _unclaimedDish = null;

            _moveTo = plate.transform.position + Vector3.down * 0.8f;
            _job = Job.ToPlateForDish;
        }

        void PickUpDish()
        {
            if (!Available(plate) || plate.Match == null) { ReturnHome(); return; }
            plate.Interact(carry);
            if (!carry.HasItem) { ReturnHome(); return; }
            _target = carry.Held.recipe != null ? FindCustomerFor(carry.Held.recipe) : null;
            if (_target == null) { ReturnHome(); return; }
            _job = Job.ToCustomer;
        }

        void Deliver()
        {
            if (_target != null && _target.CanInteract(carry)) _target.Interact(carry);
            _target = null;
            ReturnHome();
        }

        // ---------------------------------------------------------------- juru masak

        void TickCook()
        {
            switch (_job)
            {
                case Job.Idle:
                    Walk(IdlePosition());
                    if (_timer <= 0f) LookForCooking();
                    break;

                case Job.ToSource:
                    if (_chickenSource == null) { ReturnHome(); break; }
                    _moveTo = _chickenSource.transform.position + Vector3.down * 0.9f;
                    if (Walk(_moveTo))
                    {
                        _chickenSource.Interact(carry);
                        if (carry.HasItem) _job = Job.ToFryer; else ReturnHome();
                    }
                    break;

                case Job.ToFryer:
                    if (_fryer == null) { ReturnHome(); break; }
                    _moveTo = _fryer.transform.position + Vector3.down * 0.9f;
                    if (Walk(_moveTo))
                    {
                        _fryer.Interact(carry);
                        if (carry.HasItem) { ReturnHome(); break; }
                        _holdTimeout = 30f;
                        _job = Job.WaitFryer;
                        _timer = 0.5f;
                    }
                    break;

                case Job.WaitFryer:
                    _holdTimeout -= Time.deltaTime;
                    if (_fryer == null || !_fryer.IsBusy || _holdTimeout <= 0f) { ReturnHome(); break; }
                    Walk(_moveTo);
                    animator?.SetPose(CharacterPose.Working);
                    // tunggu di dekat penggorengan sampai matang, lalu langsung angkat
                    if (_timer <= 0f)
                    {
                        _timer = 0.35f;
                        if (_fryer != null && _fryer.CanInteract(carry))
                        {
                            _fryer.Interact(carry);
                            if (carry.HasItem) { _job = Job.ToCobek; break; }
                        }
                    }
                    break;

                case Job.ToCobek:
                    if (_cobek == null) { ReturnHome(); break; }
                    _moveTo = _cobek.transform.position + Vector3.down * 0.9f;
                    if (Walk(_moveTo))
                    {
                        _cobek.Interact(carry);           // taruh ayam goreng
                        _job = Job.WorkCobek;
                        _holdTimeout = 12f;
                    }
                    break;

                case Job.WorkCobek:
                    Walk(_moveTo);
                    animator?.SetPose(CharacterPose.Working);
                    _holdTimeout -= Time.deltaTime;
                    if (_cobek == null || _holdTimeout <= 0f) { ReturnHome(); break; }

                    _cobek.HoldTick(carry, Time.deltaTime * (_member != null ? _member.Speed : 1f));
                    if (_cobek.CanInteract(carry))
                    {
                        _cobek.Interact(carry);           // angkat hasil kalau sudah jadi
                        if (carry.HasItem) { _job = Job.ToPlateDrop; _holdTimeout = 10f; }
                    }
                    break;

                case Job.ToPlateDrop:
                    if (plate == null || !carry.HasItem) { ReturnHome(); break; }
                    _holdTimeout -= Time.deltaTime;

                    // meja penyajian harus kosong supaya susunan porsi pemain tidak kacau
                    if (!plate.IsEmptyPlate)
                    {
                        Walk(IdlePosition());
                        if (_holdTimeout <= 0f) ReturnHome();
                        break;
                    }

                    _moveTo = plate.transform.position + Vector3.down * 0.8f;
                    if (Walk(_moveTo))
                    {
                        plate.Interact(carry);
                        if (!carry.HasItem) { _componentIndex = 0; NextIngredient(); }
                        else ReturnHome();
                    }
                    break;

                case Job.ToIngredient:
                    if (_ingredient == null) { ReturnHome(); break; }
                    if (Walk(_ingredient.transform.position + Vector3.down * .75f))
                    {
                        _ingredient.Interact(carry);
                        if (carry.HasItem) _job = Job.AddIngredient; else ReturnHome();
                    }
                    break;

                case Job.AddIngredient:
                    if (plate == null) { ReturnHome(); break; }
                    if (Walk(plate.transform.position + Vector3.down * .75f))
                    {
                        plate.Interact(carry);
                        NextIngredient();
                    }
                    break;

                case Job.Returning:
                    if (Walk(IdlePosition())) { _job = Job.Idle; _timer = thinkDelay; }
                    break;
            }
        }

        void LookForCooking()
        {
            _timer = thinkDelay;
            if (carry == null || carry.HasItem) return;
            if (!Available(plate)) return;

            _recipe = null;
            float mostTime = -1f;
            foreach (var customer in transform.root.GetComponentsInChildren<Customer>())
                if (customer.State == CustomerState.Waiting && customer.Order != null && customer.PatienceLeft > mostTime)
                { _recipe = customer.Order; mostTime = customer.PatienceLeft; }
            if (_recipe == null) return;

            _fryer = FindFreeFryer();
            _cobek = FindFreeCobek();
            _chickenSource = FindChickenSource();

            if (_fryer == null || _cobek == null || _chickenSource == null) return;
            if (plate != null && !plate.IsEmptyPlate) return;    // tidak perlu memasak kalau meja masih penuh

            Claims[_fryer] = this; Claims[_cobek] = this; Claims[plate] = this;

            _job = Job.ToSource;
        }

        void NextIngredient()
        {
            if (_recipe == null || plate == null || plate.Match == _recipe) { ReturnHome(); return; }
            while (_componentIndex < _recipe.components.Count)
            {
                var item = _recipe.components[_componentIndex++];
                if (item.id == "ayam_geprek") continue;
                _ingredient = null;
                foreach (var source in transform.root.GetComponentsInChildren<IngredientSource>())
                    if (source.ItemId == item.id) { _ingredient = source; break; }
                if (_ingredient == null) { ReturnHome(); return; }
                _job = Job.ToIngredient;
                return;
            }
            ReturnHome();
        }

        // ---------------------------------------------------------------- pencarian

        Vector3 IdlePosition() => idleSpot != null ? idleSpot.position : transform.position;

        void ReturnHome()
        {
            ReleaseClaims();
            animator?.SetPose(CharacterPose.Standing);
            _target = null;
            _job = carry != null && carry.HasItem ? Job.ToTrash : Job.Returning;
            _timer = thinkDelay;
        }

        Customer FindCustomerFor(RecipeDef recipe)
        {
            var all = FindObjectsByType<Customer>(FindObjectsInactive.Exclude);
            Customer best = null;
            float worst = float.MaxValue;

            foreach (var c in all)
            {
                if (c == null || c.State != CustomerState.Waiting || c.Order != recipe) continue;
                if (c.PatienceLeft < worst) { worst = c.PatienceLeft; best = c; }
            }
            return best;
        }

        CookStation FindFreeFryer()
        {
            var root = transform.root;
            foreach (var f in root.GetComponentsInChildren<CookStation>(false))
                if (f != null && !f.IsBusy && Available(f) && f.GetComponent<Collider2D>().enabled) return f;
            return null;
        }

        PrepStation FindFreeCobek()
        {
            var root = transform.root;
            foreach (var c in root.GetComponentsInChildren<PrepStation>(false))
                if (c != null && c.IsEmpty && Available(c)) return c;
            return null;
        }

        IngredientSource FindChickenSource()
        {
            var root = transform.root;
            foreach (var s in root.GetComponentsInChildren<IngredientSource>(false))
                if (s != null && s.ItemId == "ayam_mentah") return s;
            return null;
        }

        /// <summary>Jalan menuju titik. Mengembalikan true saat sudah sampai.</summary>
        bool Walk(Vector3 destination)
        {
            if ((_routeDestination - destination).sqrMagnitude > .02f)
            {
                _routeDestination = destination;
                _route.Clear();
                Vector3 from = transform.position;
                // Cross the service islands only through the gaps between worktops.
                if ((from.y > .65f && destination.y < 0f) || (from.y < 0f && destination.y > .65f))
                {
                    float gap = -5.6f, best = float.MaxValue;
                    foreach (float x in new[] { -5.6f, -2.5f, .6f, 3.55f, 6.3f })
                    {
                        float cost = Mathf.Abs(from.x - x) + Mathf.Abs(destination.x - x);
                        if (cost < best) { best = cost; gap = x; }
                    }
                    _route.Enqueue(new(gap, from.y > .65f ? .85f : -.6f, from.z));
                    _route.Enqueue(new(gap, destination.y > .65f ? .85f : -.6f, from.z));
                }
                _route.Enqueue(destination);
            }
            while (_route.Count > 0 && (_route.Peek() - transform.position).sqrMagnitude <= arriveDistance * arriveDistance)
                _route.Dequeue();
            Vector3 delta = (_route.Count > 0 ? _route.Peek() : destination) - transform.position;
            if (delta.sqrMagnitude <= arriveDistance * arriveDistance)
            {
                animator?.SetMoving(false);
                return true;
            }

            animator?.SetPose(CharacterPose.Standing);
            Vector3 step = Vector3.ClampMagnitude(delta, Speed * Time.deltaTime);
            transform.position += step;
            animator?.Drive(step / Mathf.Max(0.0001f, Time.deltaTime));
            return false;
        }
    }
}
