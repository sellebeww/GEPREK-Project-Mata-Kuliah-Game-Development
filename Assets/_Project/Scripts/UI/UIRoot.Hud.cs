using Geprek.Core;
using Geprek.Data;
using Geprek.Stations;
using Geprek.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Geprek.UI
{
    public partial class UIRoot
    {
        Text _moneyText, _levelText, _clockText, _dayText, _queueText, _hintText, _carryText, _toastText;
        Image _xpFill, _repFill, _dayFill, _carryIcon, _toastIcon, _interactImage;
        Button _interactButton;
        RectTransform _toastBox, _carryBox;
        Image _dayBarBg;
        float _toastTimer;

        // ---------------------------------------------------------------- bangun

        void BuildHud()
        {
            _hud = UIFactory.Rect("HUD", _screen);
            UIFactory.Stretch(_hud);

            BuildTopBar();
            BuildDayClock();
            BuildCarrySlot();
            BuildInteractControls();
            BuildJoystick();
            BuildToast();

            _hud.gameObject.SetActive(false);
        }

        /// <summary>Pil kecil berisi ikon + teks, dipakai untuk uang, level, reputasi, antrean.</summary>
        RectTransform Pill(Transform parent, string name, Sprite icon, string text, Color bg, Color fg,
                           out Text label, float width = 150f, float height = 42f)
        {
            var panel = UIFactory.Panel(name, parent, bg);
            var rt = panel.rectTransform;
            rt.sizeDelta = new Vector2(width, height);

            var ic = UIFactory.Icon("Icon", rt, icon, new Vector2(26f, 26f));
            UIFactory.Anchor(ic.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(9f, 0f), new Vector2(26f, 26f));

            label = UIFactory.Label("Text", rt, text, UIStyle.FontBody, fg, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Anchor(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(40f, 0f), new Vector2(width - 46f, height - 6f));
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            return rt;
        }

        void BuildTopBar()
        {
            // kiri: dua baris pil. Semua anchor kiri-atas supaya tidak saling tindih.
            var left = UIFactory.Rect("TopLeft", _hud);
            UIFactory.Anchor(left, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -14f), new Vector2(380f, 100f));

            const float pillH = 40f, gapY = -46f, colW = 182f, colX = 194f;

            var money = Pill(left, "Money", icons.coin, "Rp0", UIStyle.Panel, UIStyle.Ink, out _moneyText, colW, pillH);
            UIFactory.Anchor(money, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(colW, pillH));

            var levelPill = Pill(left, "Level", icons.chefHat, "Lv 1", UIStyle.Panel, UIStyle.Ink, out _levelText, colW, pillH);
            UIFactory.Anchor(levelPill, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(colX, 0f), new Vector2(colW, pillH));

            var repPill = Pill(left, "Reputation", icons.star, "0%", UIStyle.Panel, UIStyle.Ink, out _repLabel, colW, 34f);
            UIFactory.Anchor(repPill, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, gapY), new Vector2(colW, 34f));

            _repFill = UIFactory.Bar("RepBar", left, new Color(0.80f, 0.76f, 0.68f), UIStyle.Gold, out var repBg);
            UIFactory.Anchor(repBg.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, gapY - 36f), new Vector2(colW, 10f));

            _xpFill = UIFactory.Bar("XpBar", left, new Color(0.80f, 0.76f, 0.68f), UIStyle.Leaf, out var xpBg);
            UIFactory.Anchor(xpBg.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(colX, gapY + 4f), new Vector2(colW, 12f));

            var xpLabel = UIFactory.Label("XpHint", left, "XP", UIStyle.FontSmall - 2, UIStyle.Cream, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Anchor(xpLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(colX + 6f, gapY + 4f), new Vector2(40f, 12f));

            // kanan: antrean, buku resep, jeda
            var right = UIFactory.Rect("TopRight", _hud);
            UIFactory.Anchor(right, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -14f), new Vector2(360f, 44f));

            var queuePill = Pill(right, "Queue", icons.customer, "0 duduk", UIStyle.Panel, UIStyle.Ink, out _queueText, 205f, pillH);
            UIFactory.Anchor(queuePill, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-104f, 0f), new Vector2(205f, pillH));

            IconButton(right, "BookBtn", icons.recipeBook, new Vector2(-52f, 0f), OpenRecipeBook);
            IconButton(right, "PauseBtn", icons.pause, Vector2.zero, () => Game?.Pause());
        }

        /// <summary>Tombol kotak kecil berisi satu ikon, dipakai di bar atas.</summary>
        void IconButton(Transform parent, string name, Sprite icon, Vector2 pos, System.Action onClick)
        {
            var btn = UIFactory.Button(name, parent, "", UIStyle.Panel, UIStyle.Ink, onClick);
            UIFactory.Anchor(btn.image.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), pos, new Vector2(44f, 40f));

            var img = UIFactory.Icon("Icon", btn.transform, icon, new Vector2(24f, 24f));
            UIFactory.Anchor(img.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(24f, 24f));
        }

        Text _repLabel;

        RectTransform _clockBox;

        void BuildDayClock()
        {
            var box = UIFactory.Panel("DayClock", _hud, UIStyle.Panel);
            _clockBox = box.rectTransform;
            UIFactory.Anchor(box.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(392f, 62f));

            var sun = UIFactory.Icon("Sun", box.transform, icons.sun, new Vector2(30f, 30f));
            UIFactory.Anchor(sun.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(30f, 30f));

            _dayText = UIFactory.Label("Day", box.transform, "Hari 1", UIStyle.FontSmall, UIStyle.InkSoft, TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.Anchor(_dayText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(48f, -9f), new Vector2(236f, 20f));
            _dayText.horizontalOverflow = HorizontalWrapMode.Overflow;

            _clockText = UIFactory.Label("Clock", box.transform, "09:00", UIStyle.FontHeading, UIStyle.Ink, TextAnchor.UpperRight, FontStyle.Bold);
            UIFactory.Anchor(_clockText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-12f, -6f), new Vector2(120f, 28f));

            _dayFill = UIFactory.Bar("DayBar", box.transform, new Color(0.84f, 0.80f, 0.72f), UIStyle.Chili, out var bg);
            UIFactory.Anchor(bg.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(366f, 12f));
            _dayBarBg = bg;
        }

        void BuildCarrySlot()
        {
            var box = UIFactory.Panel("CarrySlot", _hud, new Color(0.99f, 0.96f, 0.90f, 0.92f));
            _carryBox = box.rectTransform;
            UIFactory.Anchor(_carryBox, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(200f, 74f));

            _carryIcon = UIFactory.Icon("Icon", _carryBox, null, new Vector2(52f, 52f));
            UIFactory.Anchor(_carryIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(52f, 52f));

            _carryText = UIFactory.Label("Text", _carryBox, "Tangan kosong", UIStyle.FontSmall, UIStyle.InkSoft, TextAnchor.MiddleLeft);
            UIFactory.Anchor(_carryText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(70f, 0f), new Vector2(122f, 50f));
        }

        void BuildInteractControls()
        {
            var hint = UIFactory.Panel("HintBox", _hud, new Color(0.14f, 0.11f, 0.09f, 0.82f));
            UIFactory.Anchor(hint.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 168f), new Vector2(290f, 42f));
            _hintText = UIFactory.Label("Text", hint.transform, "", UIStyle.FontBody, UIStyle.Cream, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Stretch(_hintText.rectTransform, 8f);
            hint.gameObject.SetActive(false);
            _hintBox = hint.rectTransform;

            _interactButton = UIFactory.Button("InteractBtn", _hud, "", UIStyle.Chili, UIStyle.Cream, null);
            _interactImage = _interactButton.image;
            _interactImage.sprite = UIFactory.Circle;
            _interactImage.type = Image.Type.Simple;
            UIFactory.Anchor(_interactImage.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 42f), new Vector2(116f, 116f));
            _interactButton.gameObject.AddComponent<HoldButton>();

            var icon = UIFactory.Icon("Icon", _interactButton.transform, icons.tap, new Vector2(52f, 52f));
            UIFactory.Anchor(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(52f, 52f));
        }

        RectTransform _hintBox;

        void BuildJoystick()
        {
            var area = UIFactory.Raw("JoystickArea", _hud, new Color(0f, 0f, 0f, 0f));
            UIFactory.Anchor(area.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(430f, 330f));
            area.raycastTarget = true;

            var ring = UIFactory.Raw("Ring", area.transform, new Color(1f, 1f, 1f, 0.25f), UIFactory.Circle);
            ring.rectTransform.sizeDelta = new Vector2(170f, 170f);
            ring.raycastTarget = false;

            var knob = UIFactory.Raw("Knob", area.transform, new Color(1f, 1f, 1f, 0.55f), UIFactory.Circle);
            knob.rectTransform.sizeDelta = new Vector2(76f, 76f);
            knob.raycastTarget = false;

            var stick = area.gameObject.AddComponent<TouchJoystick>();
            stick.Setup(ring.rectTransform, knob.rectTransform, 70f);
        }

        void BuildToast()
        {
            var box = UIFactory.Panel("Toast", _hud, new Color(0.16f, 0.12f, 0.10f, 0.92f));
            _toastBox = box.rectTransform;
            UIFactory.Anchor(_toastBox, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -146f), new Vector2(600f, 46f));

            _toastIcon = UIFactory.Icon("Icon", _toastBox, null, new Vector2(28f, 28f));
            UIFactory.Anchor(_toastIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(28f, 28f));

            _toastText = UIFactory.Label("Text", _toastBox, "", UIStyle.FontBody, UIStyle.Cream, TextAnchor.MiddleLeft);
            UIFactory.Stretch(_toastText.rectTransform, 8f);
            _toastText.resizeTextForBestFit = true;
            _toastText.resizeTextMinSize = 14;
            _toastText.resizeTextMaxSize = UIStyle.FontBody;

            _toastBox.gameObject.SetActive(false);
        }

        // ---------------------------------------------------------------- perbarui

        void RefreshHudForState(GameState state)
        {
            bool showDayWidgets = state is GameState.DayOperating or GameState.DayClosing;
            if (_carryBox != null) _carryBox.gameObject.SetActive(showDayWidgets);
            if (_queueText != null) _queueText.transform.parent.gameObject.SetActive(showDayWidgets);

            // jam operasional tidak relevan di rumah: panelnya disembunyikan saja
            if (_clockBox != null) _clockBox.gameObject.SetActive(showDayWidgets);
            if (_objectiveBox != null) _objectiveBox.gameObject.SetActive(false);
            SetAmbientVisible(showDayWidgets);
            if (showDayWidgets) RefreshObjectives();
        }

        void OnMoneyChanged(int total, int delta)
        {
            if (_moneyText != null) _moneyText.text = MathUtil.ToRupiah(total);
            if (delta != 0)
                ShowToast((delta > 0 ? "+" : "-") + MathUtil.ToRupiah(Mathf.Abs(delta)), icons.coin, 1.1f);
        }

        void OnXpChanged(int xp, int toNext, int level)
        {
            if (_levelText != null) _levelText.text = $"Lv {level}";
            if (_xpFill == null || Game == null) return;

            var cfg = Game.Config;
            float span = Game.Progress.XpSpanOfCurrentLevel(cfg);
            _xpFill.fillAmount = level >= cfg.maxLevel ? 1f : Mathf.Clamp01(Game.Progress.XpIntoCurrentLevel(cfg) / span);
        }

        void OnLevelUp(int level)
        {
            ShowToast($"Naik ke level {level}! Masakanmu makin rapi.", icons.chefHat, 2.4f);
            Audio.AudioManager.Play(SfxId.Levelup);
        }

        void OnReputationChanged(float rep)
        {
            if (_repFill != null) _repFill.fillAmount = Mathf.Clamp01(rep);
            if (_repLabel != null) _repLabel.text = $"{Mathf.RoundToInt(rep * 100f)}%";
        }

        void OnDayProgress(float t)
        {
            UpdateAmbient(t);
            if (_dayFill != null) _dayFill.fillAmount = t;
            if (_clockText != null && Game != null)
                _clockText.text = MathUtil.ToGameClock(t, Game.Config.openHour, Game.Config.closeHour);
        }

        void OnDayStarted(DayPlanDef plan, int dayNumber)
        {
            if (_dayText != null) _dayText.text = $"Hari {dayNumber} — {plan.title}";
            OnDayStartedDay(plan, dayNumber);
        }

        void OnQueueChanged(int seated, int queued)
        {
            if (_queueText != null) _queueText.text = queued > 0 ? $"{seated} duduk · {queued} antre" : $"{seated} duduk";
            if (_queueText != null) _queueText.fontSize = queued > 0 ? UIStyle.FontSmall : UIStyle.FontBody;
        }

        void OnCarryChanged(CarriedItem item)
        {
            if (_carryIcon != null)
            {
                _carryIcon.sprite = item?.Icon;
                _carryIcon.enabled = item?.Icon != null;
            }
            if (_carryText != null)
                _carryText.text = item == null ? "Tangan kosong" : item.DisplayName;
        }

        void OnInteractTargetChanged(IInteractable target, string hint, bool usable)
        {
            bool show = !string.IsNullOrEmpty(hint);
            if (_hintBox != null) _hintBox.gameObject.SetActive(show);
            if (_hintText != null) _hintText.text = hint;

            if (_interactImage != null)
                _interactImage.color = usable ? UIStyle.Chili : new Color(0.55f, 0.5f, 0.48f, 0.75f);
        }

        // ---------------------------------------------------------------- toast

        public void ShowToast(string message, Sprite icon) => ShowToast(message, icon, 2.2f);

        void ShowToast(string message, Sprite icon, float duration)
        {
            if (_toastBox == null) return;
            _toastText.text = message;
            // Long cashier recaps must leave room for the icon at the left edge.
            _toastText.rectTransform.offsetMin = new Vector2(icon != null ? 52f : 12f, 8f);
            _toastText.rectTransform.offsetMax = new Vector2(-12f, -8f);
            if (_toastIcon != null)
            {
                _toastIcon.sprite = icon;
                _toastIcon.enabled = icon != null;
            }
            _toastBox.gameObject.SetActive(true);
            _toastTimer = duration;
        }

        void TickToast(float dt)
        {
            if (_toastTimer <= 0f) return;
            _toastTimer -= dt;
            if (_toastTimer <= 0f && _toastBox != null) _toastBox.gameObject.SetActive(false);
        }
    }
}
