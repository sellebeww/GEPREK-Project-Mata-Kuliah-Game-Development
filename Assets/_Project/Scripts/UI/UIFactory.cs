using System;
using UnityEngine;
using UnityEngine.UI;

namespace Geprek.UI
{
    /// <summary>
    /// Pembantu membuat elemen UI lewat kode. Seluruh antarmuka game dibangun di sini
    /// supaya tata letaknya ada di satu tempat dan gampang diubah.
    /// </summary>
    public static class UIFactory
    {
        static Sprite _rounded, _roundedSoft, _circle, _white;

        // ---------------------------------------------------------------- sprite prosedural

        /// <summary>Kotak sudut tumpul untuk panel dan tombol, dibuat sekali lalu dipakai ulang.</summary>
        public static Sprite RoundedBox
        {
            get
            {
                if (_rounded == null) _rounded = MakeRounded(48, 14, 0);
                return _rounded;
            }
        }

        public static Sprite RoundedSoft
        {
            get
            {
                if (_roundedSoft == null) _roundedSoft = MakeRounded(48, 8, 0);
                return _roundedSoft;
            }
        }

        public static Sprite Circle
        {
            get
            {
                if (_circle == null) _circle = MakeCircle(96);
                return _circle;
            }
        }

        public static Sprite White
        {
            get
            {
                if (_white == null)
                {
                    var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                    var px = new Color32[16];
                    for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
                    tex.SetPixels32(px); tex.Apply();
                    tex.hideFlags = HideFlags.HideAndDontSave;
                    _white = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                    _white.hideFlags = HideFlags.HideAndDontSave;
                }
                return _white;
            }
        }

        static Sprite MakeRounded(int size, int radius, int border)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(radius - x, x - (size - 1 - radius), 0f);
                float dy = Mathf.Max(radius - y, y - (size - 1 - radius), 0f);
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(radius - dist + 0.5f);
                if (dx == 0f && dy == 0f) a = 1f;
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px); tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f,
                                       0, SpriteMeshType.FullRect,
                                       new Vector4(radius + 2, radius + 2, radius + 2, radius + 2));
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        static Sprite MakeCircle(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            var px = new Color32[size * size];
            float r = size * 0.5f - 1f, c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                float a = Mathf.Clamp01(r - d + 0.5f);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px); tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        // ---------------------------------------------------------------- dasar

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static RectTransform Stretch(RectTransform rt, float pad = 0f)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(pad, pad);
            rt.offsetMax = new Vector2(-pad, -pad);
            return rt;
        }

        public static RectTransform Anchor(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static Image Panel(string name, Transform parent, Color color, Sprite sprite = null)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite != null ? sprite : RoundedBox;
            img.type = Image.Type.Sliced;
            img.color = color;
            return img;
        }

        public static Image Raw(string name, Transform parent, Color color, Sprite sprite = null)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite != null ? sprite : White;
            img.type = Image.Type.Simple;
            img.color = color;
            return img;
        }

        public static Text Label(string name, Transform parent, string content, int size, Color color,
                                 TextAnchor align = TextAnchor.MiddleLeft, FontStyle style = FontStyle.Normal)
        {
            var rt = Rect(name, parent);
            var text = rt.gameObject.AddComponent<Text>();
            text.font = UIStyle.Font;
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.alignment = align;
            text.fontStyle = style;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        public static Button Button(string name, Transform parent, string caption, Color bg, Color fg,
                                    Action onClick, int fontSize = UIStyle.FontBody)
        {
            var img = Panel(name, parent, bg);
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;

            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f);
            colors.pressedColor = new Color(0.86f, 0.86f, 0.86f);
            colors.disabledColor = new Color(0.65f, 0.65f, 0.65f, 0.6f);
            colors.fadeDuration = 0.06f;
            btn.colors = colors;

            var label = Label("Label", img.transform, caption, fontSize, fg, TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch(label.rectTransform, 6f);

            if (onClick != null) btn.onClick.AddListener(() => onClick());
            return btn;
        }

        /// <summary>Bilah isi sederhana. Kembalikan Image bagian isinya supaya bisa diatur fillAmount.</summary>
        public static Image Bar(string name, Transform parent, Color back, Color fill, out Image background)
        {
            background = Panel(name, parent, back, RoundedSoft);
            var fillImg = Panel("Fill", background.transform, fill, RoundedSoft);
            Stretch(fillImg.rectTransform, 2f);
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = 0;
            fillImg.fillAmount = 1f;
            return fillImg;
        }

        public static Image Icon(string name, Transform parent, Sprite sprite, Vector2 size)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.enabled = sprite != null;
            rt.sizeDelta = size;
            return img;
        }

        public static VerticalLayoutGroup Column(RectTransform rt, float spacing, RectOffset padding = null,
                                                 TextAnchor align = TextAnchor.UpperCenter)
        {
            var g = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            g.spacing = spacing;
            g.padding = padding ?? new RectOffset(0, 0, 0, 0);
            g.childAlignment = align;
            g.childForceExpandWidth = true;
            g.childForceExpandHeight = false;
            g.childControlWidth = true;
            g.childControlHeight = true;
            return g;
        }

        public static HorizontalLayoutGroup Row(RectTransform rt, float spacing, RectOffset padding = null,
                                                TextAnchor align = TextAnchor.MiddleLeft)
        {
            var g = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            g.spacing = spacing;
            g.padding = padding ?? new RectOffset(0, 0, 0, 0);
            g.childAlignment = align;
            g.childForceExpandWidth = false;
            g.childForceExpandHeight = false;
            g.childControlWidth = true;
            g.childControlHeight = true;
            return g;
        }

        public static LayoutElement Size(GameObject go, float w = -1, float h = -1)
        {
            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            if (w >= 0) { le.preferredWidth = w; le.minWidth = w; }
            if (h >= 0) { le.preferredHeight = h; le.minHeight = h; }
            return le;
        }

        public static Shadow Outline(Graphic g, Color color, Vector2 distance)
        {
            var o = g.gameObject.AddComponent<Outline>();
            o.effectColor = color;
            o.effectDistance = distance;
            return o;
        }
    }
}
