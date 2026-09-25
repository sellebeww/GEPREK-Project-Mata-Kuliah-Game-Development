using Geprek.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Geprek.UI
{
    public partial class UIRoot
    {
        [SerializeField] Sprite titleCardArtwork;
        RectTransform _cutsceneLayer, _cutsceneBox, _titleCard;
        Image _cutFade, _barTop, _barBottom, _cutPortrait;
        Text _cutSpeaker, _cutText, _cutHint, _titleMain, _titleSub;

        float _fadeTarget, _fadeSpeed;
        CanvasGroup _titleGroup;
        float _titleElapsed, _titleDuration;
        const float TitleSlideSeconds = 0.28f;

        /// <summary>
        /// Lapisan adegan cerita: dua bilah hitam di atas-bawah, kotak dialog besar,
        /// kartu judul, dan lapisan gelap untuk transisi. Selalu di atas panel lain.
        /// </summary>
        void BuildCutsceneLayer()
        {
            _cutsceneLayer = UIFactory.Rect("Cutscene", _screen);
            UIFactory.Stretch(_cutsceneLayer);

            // bilah sinematik
            _barTop = UIFactory.Raw("BarTop", _cutsceneLayer, Color.black);
            UIFactory.Anchor(_barTop.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                             Vector2.zero, new Vector2(1600f, 78f));
            _barBottom = UIFactory.Raw("BarBottom", _cutsceneLayer, Color.black);
            UIFactory.Anchor(_barBottom.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                             Vector2.zero, new Vector2(1600f, 78f));

            // kotak dialog
            var box = UIFactory.Panel("Box", _cutsceneLayer, new Color(0.99f, 0.96f, 0.90f, 0.98f));
            _cutsceneBox = box.rectTransform;
            UIFactory.Anchor(_cutsceneBox, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                             new Vector2(0f, 104f), new Vector2(880f, 178f));

            _cutPortrait = UIFactory.Icon("Portrait", _cutsceneBox, null, new Vector2(104f, 104f));
            UIFactory.Anchor(_cutPortrait.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                             new Vector2(18f, -16f), new Vector2(104f, 104f));

            _cutSpeaker = UIFactory.Label("Speaker", _cutsceneBox, "", UIStyle.FontHeading, UIStyle.Chili,
                                          TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.Anchor(_cutSpeaker.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                             new Vector2(136f, -14f), new Vector2(600f, 30f));

            _cutText = UIFactory.Label("Text", _cutsceneBox, "", UIStyle.FontBody + 1, UIStyle.Ink, TextAnchor.UpperLeft);
            UIFactory.Anchor(_cutText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                             new Vector2(136f, -48f), new Vector2(714f, 92f));

            _cutHint = UIFactory.Label("Hint", _cutsceneBox, "Klik atau tekan apa saja untuk lanjut",
                                       UIStyle.FontSmall - 1, UIStyle.InkSoft, TextAnchor.MiddleRight);
            UIFactory.Anchor(_cutHint.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f),
                             new Vector2(-16f, 8f), new Vector2(320f, 20f));

            // seluruh layar bisa diklik untuk lanjut
            var clickArea = UIFactory.Raw("ClickArea", _cutsceneLayer, new Color(0f, 0f, 0f, 0f));
            UIFactory.Stretch(clickArea.rectTransform);
            clickArea.raycastTarget = true;
            var btn = clickArea.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => cutscene?.Advance());
            clickArea.rectTransform.SetAsFirstSibling();

            // The banner fits inside the upper cinematic band, clear of the work area.
            var titlePanel = UIFactory.Panel("TitleCard", _cutsceneLayer, UIStyle.Cream, UIFactory.RoundedSoft);
            if (titleCardArtwork != null)
            {
                titlePanel.sprite = titleCardArtwork;
                titlePanel.type = Image.Type.Sliced;
                titlePanel.color = Color.white;
                titlePanel.pixelsPerUnitMultiplier = 1.5f;
            }
            titlePanel.raycastTarget = false;
            _titleCard = titlePanel.rectTransform;
            UIFactory.Anchor(_titleCard, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                             new Vector2(0f, 80f), new Vector2(560f, 64f));
            _titleGroup = _titleCard.gameObject.AddComponent<CanvasGroup>();
            _titleGroup.blocksRaycasts = false;
            _titleGroup.interactable = false;
            _titleGroup.alpha = 0f;

            _titleMain = UIFactory.Label("Main", _titleCard, "", 26, UIStyle.Chili,
                                         TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Anchor(_titleMain.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                             new Vector2(0f, -8f), new Vector2(470f, 30f));

            _titleSub = UIFactory.Label("Sub", _titleCard, "", 15, UIStyle.Ink,
                                        TextAnchor.MiddleCenter);
            UIFactory.Anchor(_titleSub.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                             new Vector2(0f, -34f), new Vector2(470f, 18f));
            var accent = UIFactory.Raw("Accent", _titleCard, UIStyle.Gold);
            accent.gameObject.SetActive(titleCardArtwork == null);
            accent.raycastTarget = false;
            UIFactory.Anchor(accent.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                             new Vector2(0f, 1f), new Vector2(526f, 3f));

            // lapisan gelap untuk transisi. Ditaruh di bawah kotak dialog dan kartu judul
            // supaya teks tetap terbaca saat layar sedang gelap.
            _cutFade = UIFactory.Raw("Fade", _cutsceneLayer, new Color(0f, 0f, 0f, 0f));
            UIFactory.Stretch(_cutFade.rectTransform);
            _cutFade.raycastTarget = false;
            _cutFade.rectTransform.SetSiblingIndex(1);   // tepat di atas area klik

            _cutsceneBox.gameObject.SetActive(false);
            _titleCard.gameObject.SetActive(false);
            SetCinematicBars(false);
            _cutsceneLayer.gameObject.SetActive(false);
        }

        void SetCinematicBars(bool on)
        {
            if (_barTop != null) _barTop.gameObject.SetActive(on);
            if (_barBottom != null) _barBottom.gameObject.SetActive(on);
        }

        // ---------------------------------------------------------------- dipanggil director

        void OnCutsceneActiveChanged(bool active)
        {
            if (_cutsceneLayer == null) return;

            _cutsceneLayer.gameObject.SetActive(active);
            _cutsceneLayer.SetAsLastSibling();
            _popupLayer?.SetAsLastSibling();
            SetCinematicBars(active);

            if (!active)
            {
                _cutsceneBox.gameObject.SetActive(false);
                OnCutsceneTitleHidden();
                _fadeTarget = 0f;
                _fadeSpeed = 4f;
            }
            RefreshPlayerFreeze();
        }

        void OnCutsceneLine(string speaker, string text, Sprite portrait)
        {
            if (_cutsceneBox == null) return;

            OnCutsceneTitleHidden();
            _cutsceneBox.gameObject.SetActive(true);
            _cutSpeaker.text = speaker;
            _cutText.text = text;

            if (_cutPortrait != null)
            {
                _cutPortrait.sprite = portrait;
                _cutPortrait.enabled = portrait != null;
            }
            // tanpa potret, teks memakai lebar penuh
            _cutSpeaker.rectTransform.anchoredPosition = new Vector2(portrait != null ? 136f : 24f, -14f);
            _cutText.rectTransform.anchoredPosition = new Vector2(portrait != null ? 136f : 24f, -48f);
        }

        void OnCutsceneTitle(string title, string subtitle, float duration)
        {
            if (_titleCard == null) return;
            _cutsceneBox.gameObject.SetActive(false);
            _titleCard.gameObject.SetActive(true);
            _titleMain.text = title;
            _titleSub.text = subtitle;
            _titleElapsed = 0f;
            _titleDuration = Mathf.Max(TitleSlideSeconds * 2f, duration);
            UpdateTitleBanner(0f);
        }

        void OnCutsceneTitleHidden()
        {
            if (_titleCard != null) _titleCard.gameObject.SetActive(false);
            if (_titleGroup != null) _titleGroup.alpha = 0f;
            _titleElapsed = _titleDuration = 0f;
        }

        void UpdateTitleBanner(float dt)
        {
            if (_titleCard == null || !_titleCard.gameObject.activeSelf) return;
            _titleElapsed += dt;
            float enter = Mathf.Clamp01(_titleElapsed / TitleSlideSeconds);
            float leave = Mathf.Clamp01((_titleDuration - _titleElapsed) / TitleSlideSeconds);
            float visible = Mathf.SmoothStep(0f, 1f, Mathf.Min(enter, leave));
            _titleGroup.alpha = visible;
            _titleCard.anchoredPosition = new Vector2(0f, Mathf.Lerp(80f, -7f, visible));
            if (_titleElapsed >= _titleDuration) OnCutsceneTitleHidden();
        }

        void OnCutsceneFade(bool toBlack, float duration)
        {
            _fadeTarget = toBlack ? 1f : 0f;
            _fadeSpeed = 1f / Mathf.Max(0.05f, duration);
        }

        void TickCutscene(float dt)
        {
            UpdateTitleBanner(dt);
            if (_cutFade == null) return;

            var c = _cutFade.color;
            float a = Mathf.MoveTowards(c.a, _fadeTarget, _fadeSpeed * dt);
            if (!Mathf.Approximately(a, c.a)) _cutFade.color = new Color(0f, 0f, 0f, a);

            // tombol apa saja juga melanjutkan dialog
            if (cutscene != null && cutscene.IsPlaying && _cutsceneBox.gameObject.activeSelf)
            {
                var k = UnityEngine.InputSystem.Keyboard.current;
                if (k != null && k.anyKey.wasPressedThisFrame) cutscene.Advance();
            }
        }
    }
}
