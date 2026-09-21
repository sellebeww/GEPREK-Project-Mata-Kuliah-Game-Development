using UnityEngine;

namespace Geprek.Player
{
    /// <summary>Gerak top-down berbasis Rigidbody2D supaya tabrakan dengan perabot tetap benar.</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class TopDownMover : MonoBehaviour
    {
        [SerializeField] float speed = 4.2f;
        [SerializeField] float acceleration = 40f;

        Rigidbody2D _rb;
        Vector2 _input;

        public Vector2 Velocity => _rb != null ? _rb.linearVelocity : Vector2.zero;
        public float Speed { get => speed; set => speed = Mathf.Max(0f, value); }

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        /// <summary>Arah gerak yang diinginkan, panjang 0..1.</summary>
        public void SetInput(Vector2 input)
        {
            _input = Vector2.ClampMagnitude(input, 1f);
        }

        public void Stop()
        {
            _input = Vector2.zero;
            if (_rb != null) _rb.linearVelocity = Vector2.zero;
        }

        void FixedUpdate()
        {
            Vector2 target = _input * speed;
            _rb.linearVelocity = Vector2.MoveTowards(_rb.linearVelocity, target, acceleration * Time.fixedDeltaTime);
        }
    }
}
