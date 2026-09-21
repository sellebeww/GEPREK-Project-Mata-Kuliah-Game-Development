using System.Collections.Generic;
using Geprek.Core;
using Geprek.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Geprek.UI
{
    public partial class UIRoot
    {
        // ---- target harian ----
        RectTransform _objectiveBox;
        readonly List<(Text label, Image tick)> _objectiveRows = new();

        // ---- jam sibuk ----
        RectTransform _rushBanner;
        Image _rushWindow;
        float _rushTimer;
        bool _rushActive;

        // ---- warna suasana sepanjang hari ----
        Image _ambientTint;

        const int MaxObjectiveRows = 2;

        void BuildDayLayer()
        {
            BuildObjectiveBox();
            BuildRushBanner();
        }

        /// <summary>Kotak target harian, tepat di bawah jam. Sengaja ringkas supaya tidak menutupi dapur.</summary>
        void BuildObjectiveBox()
        {
            var box = UIFactory.Panel("Objectives", _hud, new Color(0.16f, 0.12f, 0.10f, 0.80f));
            _objectiveBox = box.rectTransform;
            UIFactory.Anchor(_objectiveBox, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                             new Vector2(0f, -80f), new Vector2(392f, 30f));

            for (int i = 0; i < MaxObjectiveRows; i++)
            {
                var row = UIFactory.Rect($"Row{i}", _objectiveBox);
                UIFactory.Anchor(row, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                                 new Vector2(0f, -3f - i * 26f), new Vector2(376f, 24f));

                var tick = UIFactory.Icon("Tick", row, icons.check, new Vector2(18f, 18f));
                UIFactory.Anchor(tick.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                                 new Vector2(2f, 0f), new Vector2(18f, 18f));

                var label = UIFactory.Label("Text", row, "", UIStyle.FontSmall, UIStyle.Cream, TextAnchor.MiddleLeft);
                UIFactory.Anchor(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                                 new Vector2(24f, 0f), new Vector2(348f, 22f));

                row.gameObject.SetActive(false);
                _objectiveRows.Add((label, tick));
            }

            _objectiveBox.gameObject.SetActive(false);
        }

        void BuildRushBanner()
        {
            var box = UIFactory.Panel("RushBanner", _hud, UIStyle.ChiliDark);
            _rushBanner = box.rectTransform;
            UIFactory.Anchor(_rushBanner, new Vector2(0.5f, 1f), new Vector2(0.5f, 0f),
                             new Vector2(0f, -250f), new Vector2(420f, 58f));

            var label = UIFactory.Label("Text", _rushBanner, "JAM SIBUK!", UIStyle.FontHeading, Color.white,
                                        TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Stretch(label.rectTransform, 8f);
            UIFactory.Outline(label, new Color(0.35f, 0.06f, 0.05f, 0.95f), new Vector2(2f, -2f));

            _rushBanner.gameObject.SetActive(false);
        }

        /// <summary>Lapisan warna tipis yang mengikuti jam, dipasang di bawah semua panel.</summary>
        void BuildAmbientTint()
        {
            _ambientTint = UIFactory.Raw("AmbientTint", _screen, new Color(0f, 0f, 0f, 0f));
            UIFactory.Stretch(_ambientTint.rectTransform);
            _ambientTint.raycastTarget = false;
            _ambientTint.rectTransform.SetSiblingIndex(0);
        }

        // ---------------------------------------------------------------- pembaruan

        void OnDayStartedDay(DayPlanDef plan, int dayNumber)
        {
            RefreshObjectives();
            RefreshRushWindow(plan);
        }

        /// <summary>Tandai rentang jam sibuk di bilah jam supaya pemain bisa bersiap.</summary>
        void RefreshRushWindow(DayPlanDef plan)
        {
            if (_dayBarBg == null) return;

            if (_rushWindow == null)
            {
                _rushWindow = UIFactory.Raw("RushWindow", _dayBarBg.transform, new Color(1f, 0.55f, 0.25f, 0.55f));
                _rushWindow.raycastTarget = false;
                _rushWindow.rectTransform.SetAsFirstSibling();
            }

            bool on = plan != null && plan.hasLunchRush;
            _rushWindow.gameObject.SetActive(on);
            if (!on) return;

            float w = _dayBarBg.rectTransform.rect.width;
            float x0 = plan.rushStart * w;
            float x1 = plan.rushEnd * w;
            UIFactory.Anchor(_rushWindow.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                             new Vector2(x0, 0f), new Vector2(Mathf.Max(4f, x1 - x0), 8f));
        }

        void RefreshObjectives()
        {
            var game = Game;
            if (_objectiveBox == null || game == null) return;

            var list = game.TodayObjectives;
            bool any = list != null && list.Count > 0;
            _objectiveBox.gameObject.SetActive(any && game.State is GameState.DayOperating or GameState.DayClosing);
            if (!any) return;

            int rows = Mathf.Min(list.Count, MaxObjectiveRows);
            _objectiveBox.sizeDelta = new Vector2(392f, 6f + rows * 26f);

            for (int i = 0; i < _objectiveRows.Count; i++)
            {
                bool show = i < rows;
                _objectiveRows[i].label.transform.parent.gameObject.SetActive(show);
                if (!show) continue;

                var obj = list[i];
                bool met = obj.IsMet(game.Today);
                _objectiveRows[i].label.text = $"{obj.Describe()}  <b>{obj.Progress(game.Today)}</b>";
                _objectiveRows[i].label.color = met ? new Color(0.62f, 0.94f, 0.62f) : UIStyle.Cream;
                _objectiveRows[i].tick.sprite = met ? icons.check : icons.alert;
                _objectiveRows[i].tick.color = met ? new Color(0.62f, 0.94f, 0.62f) : new Color(0.85f, 0.8f, 0.7f, 0.8f);
            }
        }

        void OnLunchRushChanged(bool active)
        {
            _rushActive = active;
            if (!active) return;

            _rushTimer = 2.6f;
            if (_rushBanner != null)
            {
                _rushBanner.gameObject.SetActive(true);
                _rushBanner.localScale = Vector3.one;
            }
            Audio.AudioManager.Play(SfxId.Notify);
        }

        void TickDayLayer(float dt)
        {
            // spanduk jam sibuk turun, menahan sebentar, lalu naik lagi
            if (_rushBanner != null && _rushBanner.gameObject.activeSelf)
            {
                _rushTimer -= dt;
                float slideIn = Mathf.Clamp01((2.6f - _rushTimer) / 0.28f);
                float slideOut = Mathf.Clamp01(_rushTimer / 0.3f);
                float k = Mathf.Min(slideIn, slideOut);
                _rushBanner.anchoredPosition = new Vector2(0f, Mathf.Lerp(40f, -250f, k));
                if (_rushTimer <= 0f) _rushBanner.gameObject.SetActive(false);
            }

            if (Game != null && Game.State is GameState.DayOperating or GameState.DayClosing)
                RefreshObjectives();
        }

        /// <summary>
        /// Warna suasana bergerak dari pagi yang netral, ke siang terang, lalu sore
        /// yang menghangat. Dibuat tipis supaya isi layar tetap mudah dibaca.
        /// </summary>
        void UpdateAmbient(float dayProgress)
        {
            if (_ambientTint == null) return;

            Color morning = new(0.55f, 0.62f, 0.85f, 0.10f);
            Color noon = new(1f, 0.98f, 0.90f, 0.03f);
            Color evening = new(1f, 0.62f, 0.28f, 0.17f);

            Color c = dayProgress < 0.45f
                ? Color.Lerp(morning, noon, dayProgress / 0.45f)
                : Color.Lerp(noon, evening, (dayProgress - 0.45f) / 0.55f);

            _ambientTint.color = c;
        }

        void SetAmbientVisible(bool on)
        {
            if (_ambientTint != null) _ambientTint.gameObject.SetActive(on);
        }
    }
}
