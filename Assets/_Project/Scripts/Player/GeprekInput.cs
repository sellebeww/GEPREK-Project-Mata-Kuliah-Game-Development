using UnityEngine;
using UnityEngine.InputSystem;

namespace Geprek.Player
{
    /// <summary>
    /// Menggabungkan masukan keyboard dan sentuhan layar menjadi satu sumber.
    /// Joystick layar dan tombol interaksi mendaftarkan nilainya ke sini.
    /// </summary>
    public class GeprekInput : MonoBehaviour
    {
        public static GeprekInput Instance { get; private set; }

        Vector2 _virtualMove;
        bool _virtualInteractDown;
        bool _virtualInteractHeld;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        void LateUpdate() => _virtualInteractDown = false;   // tombol layar hanya sekali per frame

        // ---- dipanggil UI ----
        public void SetVirtualMove(Vector2 v) => _virtualMove = Vector2.ClampMagnitude(v, 1f);
        public void PressInteract() { _virtualInteractDown = true; _virtualInteractHeld = true; }
        public void ReleaseInteract() => _virtualInteractHeld = false;

        // ---- dibaca gameplay ----

        public Vector2 Move
        {
            get
            {
                Vector2 kb = Vector2.zero;
                var k = Keyboard.current;
                if (k != null)
                {
                    if (k.aKey.isPressed || k.leftArrowKey.isPressed) kb.x -= 1f;
                    if (k.dKey.isPressed || k.rightArrowKey.isPressed) kb.x += 1f;
                    if (k.sKey.isPressed || k.downArrowKey.isPressed) kb.y -= 1f;
                    if (k.wKey.isPressed || k.upArrowKey.isPressed) kb.y += 1f;
                }
                Vector2 combined = kb + _virtualMove;
                return Vector2.ClampMagnitude(combined, 1f);
            }
        }

        public bool InteractPressed
        {
            get
            {
                var k = Keyboard.current;
                bool kb = k != null && (k.eKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame);
                return kb || _virtualInteractDown;
            }
        }

        public bool InteractHeld
        {
            get
            {
                var k = Keyboard.current;
                bool kb = k != null && (k.eKey.isPressed || k.spaceKey.isPressed);
                return kb || _virtualInteractHeld;
            }
        }

        public bool PausePressed
        {
            get
            {
                var k = Keyboard.current;
                return k != null && k.escapeKey.wasPressedThisFrame;
            }
        }
    }
}
