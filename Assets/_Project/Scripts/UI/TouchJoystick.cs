using Geprek.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Geprek.UI
{
    /// <summary>
    /// Joystick layar untuk kontrol sentuh sesuai GDD. Muncul hanya saat disentuh
    /// supaya tidak menutupi tampilan di perangkat kecil.
    /// </summary>
    public class TouchJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [SerializeField] RectTransform ring;
        [SerializeField] RectTransform knob;
        [SerializeField] float radius = 70f;
        [SerializeField] bool hideWhenIdle = true;

        RectTransform _self;
        Canvas _canvas;
        Vector2 _origin;
        bool _active;

        public void Setup(RectTransform ringRt, RectTransform knobRt, float r)
        {
            ring = ringRt; knob = knobRt; radius = r;
            _self = (RectTransform)transform;
            _canvas = GetComponentInParent<Canvas>();
            SetVisible(!hideWhenIdle);
        }

        void Awake()
        {
            _self = (RectTransform)transform;
            _canvas = GetComponentInParent<Canvas>();
        }

        void SetVisible(bool on)
        {
            if (ring != null) ring.gameObject.SetActive(on);
            if (knob != null) knob.gameObject.SetActive(on);
        }

        public void OnPointerDown(PointerEventData e)
        {
            _active = true;
            SetVisible(true);
            _origin = ToLocal(e);
            if (ring != null) ring.anchoredPosition = _origin;
            if (knob != null) knob.anchoredPosition = _origin;
        }

        public void OnDrag(PointerEventData e)
        {
            if (!_active) return;

            Vector2 local = ToLocal(e);
            Vector2 delta = Vector2.ClampMagnitude(local - _origin, radius);
            if (knob != null) knob.anchoredPosition = _origin + delta;

            GeprekInput.Instance?.SetVirtualMove(delta / radius);
        }

        public void OnPointerUp(PointerEventData e)
        {
            _active = false;
            GeprekInput.Instance?.SetVirtualMove(Vector2.zero);
            if (knob != null && ring != null) knob.anchoredPosition = ring.anchoredPosition;
            if (hideWhenIdle) SetVisible(false);
        }

        Vector2 ToLocal(PointerEventData e)
        {
            var cam = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? _canvas.worldCamera : null;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_self, e.position, cam, out var local);
            return local;
        }
    }
}
