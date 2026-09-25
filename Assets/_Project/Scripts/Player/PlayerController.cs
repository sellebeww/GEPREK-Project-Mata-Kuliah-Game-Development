using Geprek.Core;
using Geprek.World;
using UnityEngine;

namespace Geprek.Player
{
    /// <summary>Menyatukan masukan, gerak, animasi, dan interaksi pemain.</summary>
    [RequireComponent(typeof(TopDownMover))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] CharacterAnimator animator;
        [SerializeField] PlayerInteractor interactor;
        [SerializeField] PlayerCarry carry;
        [SerializeField] SortingByY sorting;

        TopDownMover _mover;
        GeprekInput _input;
        bool _frozen;

        public PlayerCarry Carry => carry;
        public PlayerInteractor Interactor => interactor;

        void Awake()
        {
            _mover = GetComponent<TopDownMover>();
            if (animator == null) animator = GetComponentInChildren<CharacterAnimator>();
            if (interactor == null) interactor = GetComponentInChildren<PlayerInteractor>();
            if (carry == null) carry = GetComponent<PlayerCarry>();
            if (sorting != null) sorting.SetDynamic(true);
        }

        void Start()
        {
            _input = GeprekInput.Instance;
            ApplyUpgrades();
        }

        /// <summary>Terapkan ulang efek upgrade, dipanggil setelah pemain membeli sesuatu.</summary>
        public void ApplyUpgrades()
        {
            var game = GameManager.Instance;
            if (game == null || game.Config == null) return;
            _mover.Speed = game.PlayerMoveSpeed;
            interactor?.SetRadius(game.Config.interactRadius);
        }

        /// <summary>Kunci gerak, misalnya saat dialog atau layar laporan terbuka.</summary>
        public void SetFrozen(bool frozen)
        {
            _frozen = frozen;
            if (frozen)
            {
                _mover.Stop();
                animator?.SetMoving(false);
                interactor?.ReleaseInteract();
            }
        }

        void Update()
        {
            if (_input == null) _input = GeprekInput.Instance;
            if (_input == null) return;

            if (_frozen)
            {
                _mover.SetInput(Vector2.zero);
                return;
            }

            Vector2 move = _input.Move;
            _mover.SetInput(move);
            animator?.Drive(_mover.Velocity, 0.12f);

            interactor?.SetFacing(move);
            interactor?.Scan();

            if (_input.InteractPressed) interactor?.PressInteract();
            interactor?.TickHold(_input.InteractHeld, Time.deltaTime);
            if (!_input.InteractHeld) interactor?.ReleaseInteract();
        }
    }
}
