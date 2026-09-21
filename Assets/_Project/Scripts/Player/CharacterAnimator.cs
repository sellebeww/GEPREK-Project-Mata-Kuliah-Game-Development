using Geprek.Core;
using Geprek.Data;
using UnityEngine;

namespace Geprek.Player
{
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

        public Facing Facing => _facing;

        void Reset() => spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
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
            var sprite = skin.Get(_facing, _frame);
            if (sprite != null) spriteRenderer.sprite = sprite;
        }
    }
}
