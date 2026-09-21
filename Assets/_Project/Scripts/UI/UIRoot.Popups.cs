using System.Collections.Generic;
using Geprek.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Geprek.UI
{
    public partial class UIRoot
    {
        /// <summary>Satu teks mengambang yang sedang berjalan animasinya.</summary>
        class PopupSlot
        {
            public RectTransform root;
            public Text label;
            public Image icon;
            public Vector3 worldPosition;
            public float age;
            public float life;
            public bool busy;
        }

        RectTransform _popupLayer;
        readonly List<PopupSlot> _popups = new();
        const int PopupPoolSize = 10;

        void BuildPopupLayer()
        {
            _popupLayer = UIFactory.Rect("Popups", _screen);
            UIFactory.Stretch(_popupLayer);
            _popupLayer.SetAsLastSibling();

            for (int i = 0; i < PopupPoolSize; i++)
            {
                var root = UIFactory.Rect($"Popup{i}", _popupLayer);
                UIFactory.Anchor(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(260f, 40f));

                var icon = UIFactory.Icon("Icon", root, null, new Vector2(26f, 26f));
                UIFactory.Anchor(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-6f, 0f), new Vector2(26f, 26f));

                var label = UIFactory.Label("Text", root, "", UIStyle.FontHeading - 2, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
                UIFactory.Anchor(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f), new Vector2(2f, 0f), new Vector2(230f, 34f));
                UIFactory.Outline(label, new Color(0.10f, 0.07f, 0.06f, 0.95f), new Vector2(2f, -2f));

                root.gameObject.SetActive(false);
                _popups.Add(new PopupSlot { root = root, label = label, icon = icon });
            }
        }

        void OnPopup(string text, Sprite icon, Vector3 world, Color color)
        {
            PopupSlot slot = null;
            for (int i = 0; i < _popups.Count; i++)
                if (!_popups[i].busy) { slot = _popups[i]; break; }
            if (slot == null) return;                 // semua terpakai: lewati saja

            slot.busy = true;
            slot.age = 0f;
            slot.life = 1.25f;
            slot.worldPosition = world;
            slot.label.text = text;
            slot.label.color = color;

            slot.icon.sprite = icon;
            slot.icon.enabled = icon != null;
            // teks digeser sedikit kalau ada ikon, supaya tetap seimbang
            slot.label.rectTransform.anchoredPosition = new Vector2(icon != null ? 18f : 0f, 0f);

            slot.root.gameObject.SetActive(true);
            slot.root.SetAsLastSibling();
            PositionPopup(slot, 0f);
        }

        void TickPopups(float dt)
        {
            var cam = Camera.main;
            for (int i = 0; i < _popups.Count; i++)
            {
                var s = _popups[i];
                if (!s.busy) continue;

                s.age += dt;
                float t = s.age / s.life;
                if (t >= 1f || cam == null)
                {
                    s.busy = false;
                    s.root.gameObject.SetActive(false);
                    continue;
                }

                PositionPopup(s, t);
            }
        }

        void PositionPopup(PopupSlot s, float t)
        {
            var cam = Camera.main;
            if (cam == null) return;

            // naik perlahan sambil memudar, membesar sebentar di awal
            Vector3 world = s.worldPosition + new Vector3(0f, 0.30f + t * 0.60f, 0f);
            Vector3 screen = cam.WorldToScreenPoint(world);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _screen, screen, _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : cam, out var local);

            // bar atas dipakai HUD, jadi teks mengambang ditahan di bawahnya
            float ceiling = _screen.rect.height * 0.5f - 132f;
            local.y = Mathf.Min(local.y, ceiling);
            s.root.anchoredPosition = local;

            float pop = t < 0.18f ? Mathf.Lerp(0.6f, 1.12f, t / 0.18f)
                      : t < 0.32f ? Mathf.Lerp(1.12f, 1f, (t - 0.18f) / 0.14f)
                      : 1f;
            s.root.localScale = Vector3.one * pop;

            float alpha = t < 0.7f ? 1f : 1f - (t - 0.7f) / 0.3f;
            var c = s.label.color; c.a = alpha; s.label.color = c;
            if (s.icon.enabled) { var ic = s.icon.color; ic.a = alpha; s.icon.color = ic; }
        }
    }
}
