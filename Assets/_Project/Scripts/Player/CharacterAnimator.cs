using Geprek.Core;
using Geprek.Data;
using UnityEngine;

namespace Geprek.Player
{
    public enum CharacterPose { Standing, Seated, Ordering, Eating, Working }
    /// <summary>
    /// Animasi jalan 4 arah tanpa AnimatorController: sprite ditukar langsung dari
    /// <see cref="CharacterSkin"/>. Dipakai pemain, pelanggan, dan karyawan.
    /// </summary>
    public class CharacterAnimator : MonoBehaviour
    {
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] CharacterSkin skin;
        [SerializeField] float framesPerSecond = 7f;

        Facing _facing = Facing.Down;
        bool _moving;
        float _timer;
        int _frame;
        CharacterPose _pose;
        Vector3 _restPosition, _restScale;
        float _phase, _poseBlend;
        public CharacterPose Pose => _pose;

        public Facing Facing => _facing;

        void Reset() => spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            _restPosition = transform.localPosition;
            _restScale = transform.localScale;
            _phase = Random.Range(0f, 6.28f);
            ApplySprite();
        }

        public void SetPose(CharacterPose pose)
        {
            if (_pose == pose) return;
            _pose = pose;
            _poseBlend = 0f;
            ApplySprite();
        }

        public void SetSkin(CharacterSkin newSkin)
        {
            skin = newSkin;
            _frame = 0;
            ApplySprite();
        }

        public void SetFacing(Facing facing)
        {
            if (_facing == facing) return;
            _facing = facing;
            ApplySprite();
        }

        public void SetMoving(bool moving)
        {
            if (_moving == moving) return;
            _moving = moving;
            if (!moving) { _frame = 0; _timer = 0f; ApplySprite(); }
        }

        /// <summary>Atur arah dan status gerak sekaligus dari vektor kecepatan.</summary>
        public void Drive(Vector2 velocity, float moveThreshold = 0.05f)
        {
            bool moving = velocity.sqrMagnitude > moveThreshold * moveThreshold;
            if (moving) SetFacing(Utils.MathUtil.ToFacing(velocity, _facing));
            SetMoving(moving);
        }

        void Update()
        {
            _poseBlend = Mathf.MoveTowards(_poseBlend, 1f, Time.deltaTime * 5f);
            if (skin != null && skin.illustrated)
            {
                float time = Time.time + _phase;
                bool seated = _pose is CharacterPose.Seated or CharacterPose.Ordering or CharacterPose.Eating;
                float bounce = _moving ? Mathf.Abs(Mathf.Sin(time * 12f)) * 0.035f : Mathf.Sin(time * 2.5f) * 0.009f;
                float tilt = _moving ? Mathf.Sin(time * 12f) * 2f : 0f;
                if (_pose == CharacterPose.Ordering) tilt = Mathf.Sin(time * 7f) * 3f;
                if (_pose == CharacterPose.Eating) tilt = (1f + Mathf.Sin(time * 5f)) * -3f;
                if (_pose == CharacterPose.Working) { tilt = Mathf.Sin(time * 10f) * 5f; bounce += Mathf.Abs(Mathf.Sin(time * 10f)) * .035f; }
                transform.localPosition = _restPosition + Vector3.up * (bounce + (seated ? (1f - _poseBlend) * .12f : 0f));
                transform.localRotation = Quaternion.Euler(0f, 0f, tilt);
                transform.localScale = new Vector3(_restScale.x, _restScale.y * (1f - (seated ? .025f * Mathf.Sin(time * 3f) : 0f)), _restScale.z);
            }
            if (!_moving || skin == null || skin.framesPerDirection <= 1) return;

            _timer += Time.deltaTime;
            float step = 1f / Mathf.Max(0.1f, framesPerSecond);
            while (_timer >= step)
            {
                _timer -= step;
                _frame = (_frame + 1) % skin.framesPerDirection;
                ApplySprite();
            }
        }

        void ApplySprite()
        {
            if (spriteRenderer == null || skin == null || !skin.IsValid) return;
            bool seated = _pose is CharacterPose.Seated or CharacterPose.Ordering or CharacterPose.Eating;
            if (seated && skin.seatedRight != null)
            {
                spriteRenderer.sprite = _pose == CharacterPose.Ordering && skin.orderingRight != null
                    ? skin.orderingRight : skin.seatedRight;
                spriteRenderer.flipX = _facing == Facing.Left;
                return;
            }
            spriteRenderer.flipX = false;
            var sprite = skin.Get(_facing, _frame);
            if (sprite != null) spriteRenderer.sprite = sprite;
        }
    }
}
