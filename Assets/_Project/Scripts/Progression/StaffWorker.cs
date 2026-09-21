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
    /// - Juru masak mengurus rantai ayam: ambil, goreng, geprek, taruh di meja penyajian.
    /// - Kasir berjaga di tempatnya; efeknya berupa bonus harga, bukan gerakan.
    ///
    /// Pemain tetap yang menyusun porsi dan mengambil keputusan, jadi karyawan
    /// mengurangi lari-lari tanpa mengambil alih permainan.
    /// </summary>
    public class StaffWorker : MonoBehaviour
    {
        enum Job { Idle, ToPlateForDish, ToCustomer, ToSource, ToFryer, WaitFryer, ToCobek, WorkCobek, ToPlateDrop, Returning }

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
            _member = member;
            plate = plateStation;
            idleSpot = idle;
        }

        void Update()
        {
            var game = Game;
            bool working = game != null && (game.IsOperating || game.State == GameState.DayClosing);

            if (!working || Role == StaffRole.Cashier)
            {
                Walk(idleSpot != null ? idleSpot.position : transform.position);
                return;
            }

            _timer -= Time.deltaTime;

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
                    _moveTo = _target.transform.position + Vector3.down * 0.7f;
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
            if (plate == null || carry == null || carry.HasItem || plate.Match == null) return;

            _target = FindCustomerFor(plate.Match);
            if (_target == null) return;

            _moveTo = plate.transform.position + Vector3.down * 0.8f;
            _job = Job.ToPlateForDish;
        }

        void PickUpDish()
        {
            if (plate == null || plate.Match == null) { ReturnHome(); return; }
            plate.Interact(carry);
            if (!carry.HasItem) { ReturnHome(); return; }
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
                        _job = carry.HasItem ? Job.ToFryer : Job.Returning;
                    }
                    break;

                case Job.ToFryer:
                    if (_fryer == null) { ReturnHome(); break; }
                    _moveTo = _fryer.transform.position + Vector3.down * 0.9f;
                    if (Walk(_moveTo))
                    {
                        _fryer.Interact(carry);
                        _job = Job.WaitFryer;
                        _timer = 0.5f;
                    }
                    break;

                case Job.WaitFryer:
                    Walk(_moveTo);
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
                        ReturnHome();
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

            _fryer = FindFreeFryer();
            _cobek = FindFreeCobek();
            _chickenSource = FindChickenSource();

            if (_fryer == null || _cobek == null || _chickenSource == null) return;
            if (plate != null && !plate.IsEmptyPlate) return;    // tidak perlu memasak kalau meja masih penuh

            _job = Job.ToSource;
        }

        // ---------------------------------------------------------------- pencarian

        Vector3 IdlePosition() => idleSpot != null ? idleSpot.position : transform.position;

        void ReturnHome()
        {
            _target = null;
            _job = Job.Returning;
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
                if (f != null && !f.IsBusy) return f;
            return null;
        }

        PrepStation FindFreeCobek()
        {
            var root = transform.root;
            foreach (var c in root.GetComponentsInChildren<PrepStation>(false))
                if (c != null && c.IsEmpty) return c;
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
            Vector3 delta = destination - transform.position;
            if (delta.sqrMagnitude <= arriveDistance * arriveDistance)
            {
                animator?.SetMoving(false);
                return true;
            }

            Vector3 step = delta.normalized * Speed * Time.deltaTime;
            transform.position += step;
            animator?.Drive(step / Mathf.Max(0.0001f, Time.deltaTime));
            return false;
        }
    }
}
