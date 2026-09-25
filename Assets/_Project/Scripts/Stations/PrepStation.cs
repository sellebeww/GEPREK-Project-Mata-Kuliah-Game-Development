using Geprek.Core;
using Geprek.Data;
using Geprek.Player;
using Geprek.World;
using UnityEngine;

namespace Geprek.Stations
{
    /// <summary>
    /// Cobek. Ayam goreng ditaruh di sini lalu tombol interaksi ditahan untuk
    /// menggeprek sampai selesai. Melepas tombol menghentikan progres, bukan mereset.
    /// </summary>
    public class PrepStation : StationBase
    {
        [Header("Efek")]
        [SerializeField] Transform pestle;              // ulekan yang bergerak naik-turun
        [SerializeField] float pestleBounce = 0.08f;
        [SerializeField] Color workingColor = new(0.95f, 0.5f, 0.35f);

        ItemDef _input;
        float _inputQuality = 1f;
        float _progress;
        float _sfxTimer;
        Vector3 _pestleHome;

        bool HasInput => _input != null;

        /// <summary>Cobek sedang kosong dan siap dipakai.</summary>
        public bool IsEmpty => _input == null;
        bool IsDone => HasInput && _progress >= _input.prepTime;

        protected override void Awake()
        {
            base.Awake();
            if (pestle != null) _pestleHome = pestle.localPosition;
        }

        public override bool CanInteract(PlayerCarry carry)
        {
            if (carry == null) return false;
            if (!HasInput) return carry.HasItem && carry.Held.def != null && carry.Held.def.CanPrep;
            if (IsDone) return carry.IsEmpty;
            return carry.IsEmpty;   // perlu tangan kosong untuk mengulek
        }

        public override string Hint(PlayerCarry carry)
        {
            if (!HasInput)
            {
                if (carry != null && carry.HasItem && carry.Held.def != null && carry.Held.def.CanPrep)
                    return $"Taruh {carry.Held.def.displayName}";
                return "Butuh ayam goreng";
            }
            if (IsDone) return $"Ambil {_input.prepResult.displayName}";
            return "Tahan untuk geprek";
        }

        public override bool UsesHold(PlayerCarry carry) =>
            HasInput && !IsDone && carry != null && carry.IsEmpty;

        public override void Interact(PlayerCarry carry)
        {
            if (!CanInteract(carry)) return;

            if (!HasInput)
            {
                var held = carry.Release();
                _input = held.def;
                _inputQuality = held.quality;
                _progress = 0f;
                ShowIcon(_input.icon);
                ShowProgress(0f);
                if (progressBar != null) progressBar.SetColor(workingColor);
                Sfx(SfxId.Drop);
                return;
            }

            if (IsDone)
            {
                carry.TryTake(CarriedItem.FromItem(_input.prepResult, _inputQuality));
                Sfx(SfxId.Pickup);
                Clear();
            }
        }

        public override void HoldTick(PlayerCarry carry, float deltaTime)
        {
            if (!HasInput || IsDone || carry == null || !carry.IsEmpty) return;

            float speed = Game != null ? Game.PrepSpeedMultiplier : 1f;
            _progress += deltaTime * speed;
            ShowProgress(Mathf.Clamp01(_progress / _input.prepTime));

            if (pestle != null)
            {
                float bob = Mathf.Abs(Mathf.Sin(Time.time * 18f)) * pestleBounce;
                pestle.localPosition = _pestleHome + new Vector3(0f, bob, 0f);
            }

            _sfxTimer -= deltaTime;
            if (_sfxTimer <= 0f)
            {
                Sfx(SfxId.Geprek);
                JuiceDirector.DoImpact(transform.position + Vector3.up * 0.55f);
                JuiceDirector.DoPunch(transform, 0.05f, 0.1f);
                _sfxTimer = 0.22f;
            }

            if (_progress >= _input.prepTime)
            {
                ShowIcon(_input.prepResult != null ? _input.prepResult.icon : _input.icon);
                ShowProgress(1f);
                Sfx(SfxId.Notify);
                JuiceDirector.DoSparkle(transform.position + Vector3.up * 0.8f, new Color(1f, 0.72f, 0.35f), 5);
                JuiceDirector.DoPopup("Geprek siap", null, transform.position + Vector3.up * 1.1f,
                                      new Color(1f, 0.82f, 0.45f));
                HoldCancelled();
            }
        }

        public override void HoldCancelled()
        {
            if (pestle != null) pestle.localPosition = _pestleHome;
        }

        void Clear()
        {
            _input = null;
            _inputQuality = 1f;
            _progress = 0f;
            ShowIcon(null);
            HideProgress();
            HoldCancelled();
        }
    }
}
