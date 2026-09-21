using System;
using System.Collections.Generic;
using Geprek.Core;
using Geprek.Stations;
using UnityEngine;

namespace Geprek.Player
{
    /// <summary>
    /// Mencari benda terdekat yang bisa dipakai, menyorotnya, lalu meneruskan
    /// tekan / tahan tombol ke benda tersebut.
    /// </summary>
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] PlayerCarry carry;
        [SerializeField] float radius = 1.15f;
        [SerializeField] LayerMask interactableMask = ~0;

        /// <summary>Kandidat aktif dan teks petunjuknya. UI mendengarkan ini.</summary>
        public event Action<IInteractable, string, bool> TargetChanged;

        readonly List<Collider2D> _hits = new(16);
        ContactFilter2D _filter;
        bool _filterReady;
        IInteractable _current;
        IInteractable _holding;
        string _lastHint = "";
        bool _lastUsable;

        public IInteractable Current => _current;

        void Awake()
        {
            if (carry == null) carry = GetComponentInParent<PlayerCarry>();
        }

        public void SetRadius(float r) => radius = Mathf.Max(0.2f, r);

        public void Scan()
        {
            IInteractable best = null;
            float bestScore = float.MaxValue;

            if (!_filterReady)
            {
                _filter = new ContactFilter2D { useTriggers = true };
                _filter.SetLayerMask(interactableMask);
                _filterReady = true;
            }

            Physics2D.OverlapCircle((Vector2)transform.position, radius, _filter, _hits);
            for (int i = 0; i < _hits.Count; i++)
            {
                var col = _hits[i];
                if (col == null) continue;

                var candidate = col.GetComponentInParent<IInteractable>();
                if (candidate == null) continue;

                float dist = Vector2.SqrMagnitude((Vector2)candidate.Transform.position - (Vector2)transform.position);
                // yang bisa dipakai selalu menang dari yang cuma dekat
                float score = candidate.CanInteract(carry) ? dist : dist + 100f;
                if (score < bestScore) { bestScore = score; best = candidate; }
            }

            if (!ReferenceEquals(best, _current))
            {
                _current?.SetHighlighted(false);
                _current = best;
                _current?.SetHighlighted(true);
            }

            string hint = _current != null ? _current.Hint(carry) : "";
            bool usable = _current != null && _current.CanInteract(carry);
            if (hint != _lastHint || usable != _lastUsable)
            {
                _lastHint = hint;
                _lastUsable = usable;
                TargetChanged?.Invoke(_current, hint, usable);
            }
        }

        public void PressInteract()
        {
            if (_current == null) return;
            if (_current.UsesHold(carry)) { _holding = _current; return; }
            if (_current.CanInteract(carry)) _current.Interact(carry);
        }

        public void TickHold(bool held, float deltaTime)
        {
            if (_holding == null) return;

            // lepas tombol atau menjauh dari stasiun -> berhenti mengulek
            bool stillNear = ReferenceEquals(_holding, _current);
            if (!held || !stillNear)
            {
                _holding.HoldCancelled();
                _holding = null;
                return;
            }
            _holding.HoldTick(carry, deltaTime);
        }

        public void ReleaseInteract()
        {
            if (_holding == null) return;
            _holding.HoldCancelled();
            _holding = null;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.85f, 0.3f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
