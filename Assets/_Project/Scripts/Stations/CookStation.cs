using Geprek.Core;
using Geprek.Data;
using Geprek.Player;
using Geprek.World;
using UnityEngine;

namespace Geprek.Stations
{
    /// <summary>
    /// Penggorengan. Bahan ditaruh, matang sendiri setelah beberapa detik, lalu
    /// gosong kalau ditinggal terlalu lama. Ini sumber tekanan waktu utama Arc 1.
    ///
    /// Setelah matang ada jendela singkat "pas": mengangkat di jendela itu memberi
    /// hasil PERFECT, agak telat jadi GOOD, kelewat batas jadi gosong. Jadi menggoreng
    /// bukan cuma menunggu timer, tapi soal memilih waktu kembali ke penggorengan.
    /// </summary>
    public class CookStation : StationBase
    {
        enum Phase { Empty, Cooking, Ready, Burnt }

        [Header("Efek")]
        [SerializeField] GameObject sizzleEffect;
        [SerializeField] Transform steamOrigin;
        [SerializeField] Color cookingColor = new(1f, 0.85f, 0.5f);
        [SerializeField] Color readyColor = new(0.45f, 0.85f, 0.45f);
        [SerializeField] Color perfectColor = new(1f, 0.82f, 0.25f);
        [SerializeField] Color burntColor = new(0.45f, 0.35f, 0.3f);

        [Header("Penilaian")]
        [Tooltip("Bagian awal masa aman yang dihitung sebagai hasil sempurna.")]
        [Range(0.2f, 0.8f)] [SerializeField] float perfectWindow = 0.45f;
        [Tooltip("Pengali kualitas kalau pengambilan sudah lewat jendela sempurna.")]
        [SerializeField] float goodQualityScale = 0.82f;

        Phase _phase = Phase.Empty;
        ItemDef _input;
        float _timer;
        float _sfxTimer;
        float _fxTimer;
        bool _burnAnnounced;

        public bool IsBusy => _phase != Phase.Empty;

        Vector3 SteamPoint => steamOrigin != null ? steamOrigin.position : transform.position + Vector3.up * 0.9f;

        protected override void Awake()
        {
            base.Awake();
            SetEffect(false);
        }

        void Update()
        {
            if (_phase == Phase.Empty || _input == null) return;

            float speed = Game != null ? Game.CookSpeedMultiplier : 1f;
            _timer += Time.deltaTime * speed;
            _fxTimer -= Time.deltaTime;

            switch (_phase)
            {
                case Phase.Cooking: TickCooking(); break;
                case Phase.Ready: TickReady(); break;
                case Phase.Burnt: TickBurnt(); break;
            }
        }

        void TickCooking()
        {
            ShowProgress(Mathf.Clamp01(_timer / Mathf.Max(0.1f, _input.cookTime)));
            if (progressBar != null) progressBar.SetColor(cookingColor);

            if (_sfxTimer <= 0f) { Sfx(SfxId.Sizzle); _sfxTimer = 1.6f; }
            _sfxTimer -= Time.deltaTime;

            if (_fxTimer <= 0f)
            {
                JuiceDirector.DoSteam(SteamPoint);
                _fxTimer = 0.28f;
            }

            if (_timer >= _input.cookTime)
            {
                _phase = Phase.Ready;
                _timer = 0f;
                ShowIcon(_input.cookResult != null ? _input.cookResult.icon : _input.icon);
                Sfx(SfxId.Notify);
                JuiceDirector.DoSparkle(SteamPoint, perfectColor, 4);
                JuiceDirector.DoPunch(transform, 0.12f);
            }
        }

        void TickReady()
        {
            float grace = Mathf.Max(0.5f, _input.burnGrace);
            float left = 1f - Mathf.Clamp01(_timer / grace);
            ShowProgress(left);

            bool inPerfect = _timer <= grace * perfectWindow;
            if (progressBar != null) progressBar.SetColor(inPerfect ? perfectColor : readyColor);

            // uap makin jarang saat mendekati gosong, diganti asap tipis
            if (_fxTimer <= 0f)
            {
                if (left < 0.3f) JuiceDirector.DoSmoke(SteamPoint);
                else JuiceDirector.DoSteam(SteamPoint);
                _fxTimer = left < 0.3f ? 0.22f : 0.45f;
            }

            if (_timer >= grace) EnterBurnt();
        }

        void EnterBurnt()
        {
            _phase = Phase.Burnt;
            _timer = 0f;
            ShowIcon(_input.burnResult != null ? _input.burnResult.icon : null);
            if (progressBar != null) progressBar.SetColor(burntColor);
            ShowProgress(1f);
            SetEffect(false);
            Sfx(SfxId.Error);

            if (!_burnAnnounced)
            {
                _burnAnnounced = true;
                JuiceDirector.DoPopup("GOSONG!", null, SteamPoint, new Color(0.88f, 0.34f, 0.28f));
                JuiceDirector.DoShake(0.10f, 0.22f);
                GameEvents.RaiseToast("Ayam gosong! Buang ke tempat sampah.");
            }
        }

        void TickBurnt()
        {
            if (_fxTimer > 0f) return;
            JuiceDirector.DoSmoke(SteamPoint);
            _fxTimer = 0.3f;
        }

        // ---------------------------------------------------------------- interaksi

        public override bool CanInteract(PlayerCarry carry)
        {
            if (carry == null) return false;
            if (_phase == Phase.Empty)
                return carry.HasItem && carry.Held.def != null && carry.Held.def.CanCook;
            return carry.IsEmpty;   // ambil hasil
        }

        public override string Hint(PlayerCarry carry)
        {
            switch (_phase)
            {
                case Phase.Empty:
                    if (carry != null && carry.HasItem && carry.Held.def != null && carry.Held.def.CanCook)
                        return $"Goreng {carry.Held.def.displayName}";
                    return "Butuh ayam mentah";
                case Phase.Cooking: return "Sedang menggoreng...";
                case Phase.Ready: return "Angkat ayam goreng";
                default: return "Buang yang gosong";
            }
        }

        public override void Interact(PlayerCarry carry)
        {
            if (!CanInteract(carry)) return;

            if (_phase == Phase.Empty)
            {
                var held = carry.Release();
                _input = held.def;
                _phase = Phase.Cooking;
                _timer = 0f;
                _sfxTimer = 0f;
                _fxTimer = 0f;
                _burnAnnounced = false;
                ShowIcon(_input.icon);
                SetEffect(true);
                Sfx(SfxId.Drop);
                JuiceDirector.DoOil(SteamPoint, 7);
                JuiceDirector.DoPunch(transform, 0.10f);
                return;
            }

            TakeResult(carry);
        }

        void TakeResult(PlayerCarry carry)
        {
            float baseQuality = Game != null ? Game.Progress.CookQuality(Game.Config) : 1f;

            if (_phase == Phase.Ready)
            {
                float grace = Mathf.Max(0.5f, _input.burnGrace);
                bool perfect = _timer <= grace * perfectWindow;

                float quality = perfect ? baseQuality : baseQuality * goodQualityScale;
                carry.TryTake(CarriedItem.FromItem(_input.cookResult, quality));
                Sfx(SfxId.Pickup);

                if (perfect)
                {
                    JuiceDirector.DoPopup("PAS!", null, SteamPoint, perfectColor);
                    JuiceDirector.DoSparkle(SteamPoint, perfectColor, 6);
                }
                else
                {
                    JuiceDirector.DoPopup("Lumayan", null, SteamPoint, new Color(0.55f, 0.78f, 0.52f));
                }
            }
            else
            {
                carry.TryTake(CarriedItem.FromItem(_input.burnResult, baseQuality));
                Sfx(SfxId.Error);
            }

            ClearStation();
        }

        void ClearStation()
        {
            _phase = Phase.Empty;
            _input = null;
            _timer = 0f;
            ShowIcon(null);
            HideProgress();
            SetEffect(false);
        }

        void SetEffect(bool on) { if (sizzleEffect != null) sizzleEffect.SetActive(on); }
    }
}
