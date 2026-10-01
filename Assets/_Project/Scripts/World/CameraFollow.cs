using UnityEngine;

namespace Geprek.World
{
    /// <summary>Kamera mengikuti pemain dengan pelembutan dan dibatasi kotak lokasi.</summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] float smoothTime = 0.14f;
        [SerializeField] Vector2 offset = new(0f, 0.4f);

        [SerializeField] Vector2 boundsMin = new(-8f, -6f);
        [SerializeField] Vector2 boundsMax = new(8f, 6f);

        Camera _cam;
        Vector3 _velocity;
        Vector3 _shakeOffset;
        float _shakeAmount, _shakeLeft, _shakeTotal;

        void Awake() => _cam = GetComponent<Camera>();

        public void SetTarget(Transform t) => target = t;

        Vector3? _focusPoint;

        /// <summary>
        /// Arahkan kamera ke satu titik, mengabaikan pemain. Dipakai adegan cerita.
        /// Kirim null untuk mengembalikannya mengikuti pemain.
        /// </summary>
        public void FocusOn(Vector3? worldPoint) => _focusPoint = worldPoint;

        public void SetBounds(Vector2 min, Vector2 max)
        {
            boundsMin = min; boundsMax = max;
        }

        /// <summary>Atur zoom. Ruangan yang lebih kecil dipandang lebih dekat.</summary>
        public void SetSize(float orthographicSize)
        {
            if (_cam == null) _cam = GetComponent<Camera>();
            if (_cam != null) _cam.orthographicSize = Mathf.Max(1f, orthographicSize);
        }

        /// <summary>Getar sesaat. Dipakai untuk kejadian keras seperti ayam gosong.</summary>
        public void Shake(float amount, float duration)
        {
            _shakeAmount = Mathf.Max(_shakeAmount, amount);
            _shakeTotal = Mathf.Max(0.05f, duration);
            _shakeLeft = _shakeTotal;
        }

        void LateUpdate()
        {
            if (target == null && _focusPoint == null) return;

            Vector2 anchor = _focusPoint.HasValue ? (Vector2)_focusPoint.Value : (Vector2)target.position;
            Vector3 desired = Clamp(anchor + offset);
            desired.z = transform.position.z;
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, smoothTime);

            if (_shakeLeft > 0f)
            {
                _shakeLeft -= Time.unscaledDeltaTime;
                float falloff = Mathf.Clamp01(_shakeLeft / _shakeTotal);
                _shakeOffset = (Vector3)Random.insideUnitCircle * (_shakeAmount * falloff);
                transform.position += _shakeOffset;
                if (_shakeLeft <= 0f) _shakeAmount = 0f;
            }
        }

        public void SnapToTarget()
        {
            if (target == null && _focusPoint == null) return;
            Vector2 anchor = _focusPoint.HasValue ? (Vector2)_focusPoint.Value : (Vector2)target.position;
            Vector3 p = Clamp(anchor + offset);
            p.z = transform.position.z;
            transform.position = p;
            _velocity = Vector3.zero;
        }

        Vector2 Clamp(Vector2 p)
        {
            if (_cam == null) _cam = GetComponent<Camera>();
            float halfH = _cam.orthographicSize;
            float halfW = halfH * _cam.aspect;

            float minX = boundsMin.x + halfW, maxX = boundsMax.x - halfW;
            float minY = boundsMin.y + halfH, maxY = boundsMax.y - halfH;

            // kalau ruangan lebih kecil dari layar, pusatkan saja
            p.x = minX > maxX ? (boundsMin.x + boundsMax.x) * 0.5f : Mathf.Clamp(p.x, minX, maxX);
            p.y = minY > maxY ? (boundsMin.y + boundsMax.y) * 0.5f : Mathf.Clamp(p.y, minY, maxY);
            return p;
        }
    }
}
