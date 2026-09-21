using System.Collections.Generic;
using Geprek.Core;
using Geprek.Customers;
using Geprek.Data;
using Geprek.Player;
using Geprek.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace Geprek.UI
{
    /// <summary>
    /// Seluruh antarmuka game dibangun dan dikendalikan dari sini. UI dibuat lewat kode
    /// supaya tata letaknya terbaca dalam satu berkas dan tidak bergantung penataan manual.
    /// </summary>
    public partial class UIRoot : MonoBehaviour
    {
        [Header("Referensi")]
        [SerializeField] PlayerController player;
        [SerializeField] NightManager nightManager;
        [SerializeField] GameDatabase database;
        [SerializeField] CutsceneDirector cutscene;

        [Header("Ikon")]
        [SerializeField] UIIcons icons = new();

        Canvas _canvas;
        RectTransform _screen;

        // panel
        RectTransform _hud, _menuPanel, _briefPanel, _pausePanel, _reportPanel,
                      _arcPanel, _shopPanel, _bookPanel, _dialoguePanel, _nightPanel, _overPanel;
        Image _dimmer, _nightTint;

        readonly List<RectTransform> _modals = new();

        GameManager Game => GameManager.Instance;

        // ---------------------------------------------------------------- daur hidup

        void Awake()
        {
            BuildCanvas();
            BuildHud();
            BuildMainMenu();
            BuildBriefing();
            BuildPause();
            BuildDayReport();
            BuildArcReport();
            BuildShop();
            BuildRecipeBook();
            BuildDialogue();
            BuildSocialPanel();
            BuildBranchPanel();
            BuildNightOverlay();
            BuildGameOver();
            BuildDayLayer();
            BuildAmbientTint();
            BuildPopupLayer();
            BuildCutsceneLayer();
            HideAllModals();
        }

        void OnEnable()
        {
            GameEvents.StateChanged += OnStateChanged;
            GameEvents.MoneyChanged += OnMoneyChanged;
            GameEvents.XpChanged += OnXpChanged;
            GameEvents.LevelUp += OnLevelUp;
            GameEvents.ReputationChanged += OnReputationChanged;
            GameEvents.DayProgressChanged += OnDayProgress;
            GameEvents.LunchRushChanged += OnLunchRushChanged;
            GameEvents.DayStarted += OnDayStarted;
            GameEvents.DayEnded += OnDayEnded;
            GameEvents.ArcEnded += OnArcEnded;
            GameEvents.QueueChanged += OnQueueChanged;
            GameEvents.CarryChanged += OnCarryChanged;
            GameEvents.Toast += ShowToast;
            GameEvents.Popup += OnPopup;
            GameEvents.RecipeUnlocked += OnRecipeUnlocked;

            if (player != null && player.Interactor != null)
                player.Interactor.TargetChanged += OnInteractTargetChanged;

            if (cutscene != null)
            {
                cutscene.LineShown += OnCutsceneLine;
                cutscene.TitleShown += OnCutsceneTitle;
                cutscene.TitleHidden += OnCutsceneTitleHidden;
                cutscene.FadeRequested += OnCutsceneFade;
                cutscene.CutsceneActiveChanged += OnCutsceneActiveChanged;
            }

            if (nightManager != null)
            {
                nightManager.DialogueRequested += ShowDialogue;
                nightManager.ShopRequested += OpenShop;
                nightManager.SleepRequested += ConfirmSleep;
                nightManager.SocialMediaRequested += OpenSocialMedia;
                nightManager.BranchesRequested += OpenBranches;
            }
        }

        void OnDisable()
        {
            GameEvents.StateChanged -= OnStateChanged;
            GameEvents.MoneyChanged -= OnMoneyChanged;
            GameEvents.XpChanged -= OnXpChanged;
            GameEvents.LevelUp -= OnLevelUp;
            GameEvents.ReputationChanged -= OnReputationChanged;
            GameEvents.DayProgressChanged -= OnDayProgress;
            GameEvents.LunchRushChanged -= OnLunchRushChanged;
            GameEvents.DayStarted -= OnDayStarted;
            GameEvents.DayEnded -= OnDayEnded;
            GameEvents.ArcEnded -= OnArcEnded;
            GameEvents.QueueChanged -= OnQueueChanged;
            GameEvents.CarryChanged -= OnCarryChanged;
            GameEvents.Toast -= ShowToast;
            GameEvents.Popup -= OnPopup;
            GameEvents.RecipeUnlocked -= OnRecipeUnlocked;

            if (player != null && player.Interactor != null)
                player.Interactor.TargetChanged -= OnInteractTargetChanged;

            if (cutscene != null)
            {
                cutscene.LineShown -= OnCutsceneLine;
                cutscene.TitleShown -= OnCutsceneTitle;
                cutscene.TitleHidden -= OnCutsceneTitleHidden;
                cutscene.FadeRequested -= OnCutsceneFade;
                cutscene.CutsceneActiveChanged -= OnCutsceneActiveChanged;
            }

            if (nightManager != null)
            {
                nightManager.DialogueRequested -= ShowDialogue;
                nightManager.ShopRequested -= OpenShop;
                nightManager.SleepRequested -= ConfirmSleep;
                nightManager.SocialMediaRequested -= OpenSocialMedia;
                nightManager.BranchesRequested -= OpenBranches;
            }
        }

        void Start()
        {
            // pengaman: kalau event state pertama terjadi sebelum panel siap, samakan sekarang
            if (Game != null) OnStateChanged(GameState.Boot, Game.State);
        }

        void Update()
        {
            TickToast(Time.unscaledDeltaTime);
            TickPopups(Time.unscaledDeltaTime);
            TickDayLayer(Time.unscaledDeltaTime);
            TickCutscene(Time.unscaledDeltaTime);

            var input = GeprekInput.Instance;
            if (input != null && input.PausePressed) OnEscape();
        }

        void OnEscape()
        {
            var game = Game;
            if (game == null) return;
            if (cutscene != null && cutscene.IsPlaying) return;

            if (IsAnyModalOpen()) { CloseTopModal(); return; }
            if (game.State == GameState.Paused) { game.Resume(); return; }
            if (game.State is GameState.DayOperating or GameState.DayClosing or GameState.NightHome)
                game.Pause();
        }

        // ---------------------------------------------------------------- kanvas

        void BuildCanvas()
        {
            _canvas = gameObject.GetComponent<Canvas>();
            if (_canvas == null) _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // sprite dunia memakai sortingOrder berbasis posisi y yang bisa mencapai ratusan,
            // jadi kanvas harus jauh di atasnya supaya UI tidak tertutup meja atau perabot
            _canvas.sortingOrder = 10000;

            var scaler = gameObject.GetComponent<CanvasScaler>() ?? gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            if (gameObject.GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();

            _screen = (RectTransform)transform;

            _nightTint = UIFactory.Raw("NightTint", _screen, UIStyle.NightTint);
            UIFactory.Stretch(_nightTint.rectTransform);
            _nightTint.raycastTarget = false;
            _nightTint.gameObject.SetActive(false);

            _dimmer = UIFactory.Raw("Dimmer", _screen, UIStyle.Dim);
            UIFactory.Stretch(_dimmer.rectTransform);
            _dimmer.gameObject.SetActive(false);
        }

        // ---------------------------------------------------------------- panel

        RectTransform NewPanel(string name, bool modal = true)
        {
            var rt = UIFactory.Rect(name, _screen);
            UIFactory.Stretch(rt);
            if (modal) _modals.Add(rt);
            rt.gameObject.SetActive(false);
            return rt;
        }

        void HideAllModals()
        {
            foreach (var m in _modals) if (m != null) m.gameObject.SetActive(false);
            RefreshDimmer();
        }

        bool IsAnyModalOpen()
        {
            foreach (var m in _modals) if (m != null && m.gameObject.activeSelf) return true;
            return false;
        }

        void CloseTopModal()
        {
            for (int i = _modals.Count - 1; i >= 0; i--)
            {
                if (_modals[i] != null && _modals[i].gameObject.activeSelf)
                {
                    // panel alur utama tidak boleh ditutup sembarangan
                    if (_modals[i] == _menuPanel || _modals[i] == _briefPanel ||
                        _modals[i] == _reportPanel || _modals[i] == _arcPanel || _modals[i] == _overPanel) return;
                    SetPanel(_modals[i], false);
                    return;
                }
            }
        }

        void SetPanel(RectTransform panel, bool on)
        {
            if (panel == null) return;
            panel.gameObject.SetActive(on);
            RefreshDimmer();
            if (on) panel.SetAsLastSibling();
            RefreshPlayerFreeze();
        }

        /// <summary>
        /// Latar gelap harus tepat di bawah panel teratas. Dipindahkan dengan dua kali
        /// SetAsLastSibling supaya urutannya pasti benar tanpa hitung-hitungan indeks.
        /// </summary>
        void RefreshDimmer()
        {
            if (_dimmer == null) return;

            RectTransform top = null;
            foreach (var m in _modals)
            {
                if (m == null || !m.gameObject.activeSelf) continue;
                if (top == null || m.GetSiblingIndex() > top.GetSiblingIndex()) top = m;
            }

            if (top == null) { _dimmer.gameObject.SetActive(false); _popupLayer?.SetAsLastSibling(); return; }

            _dimmer.gameObject.SetActive(true);
            _dimmer.rectTransform.SetAsLastSibling();
            top.SetAsLastSibling();
            _popupLayer?.SetAsLastSibling();
        }

        void RefreshPlayerFreeze()
        {
            if (player == null) return;
            bool freeze = IsAnyModalOpen()
                          || (Game != null && Game.State == GameState.Paused)
                          || (cutscene != null && cutscene.IsPlaying);
            player.SetFrozen(freeze);
        }

        // ---------------------------------------------------------------- state

        void OnStateChanged(GameState from, GameState to)
        {
            HideAllModals();

            bool hudVisible = to is GameState.DayOperating or GameState.DayClosing or GameState.NightHome or GameState.Paused;
            if (to == GameState.Cutscene) hudVisible = false;
            if (_hud != null) _hud.gameObject.SetActive(hudVisible);
            if (_nightTint != null) _nightTint.gameObject.SetActive(to == GameState.NightHome);
            if (_nightPanel != null) _nightPanel.gameObject.SetActive(to == GameState.NightHome);

            switch (to)
            {
                case GameState.MainMenu: RefreshMainMenu(); SetPanel(_menuPanel, true); break;
                case GameState.DayBriefing: RefreshBriefing(); SetPanel(_briefPanel, true); break;
                case GameState.DayReport: SetPanel(_reportPanel, true); break;
                case GameState.ArcReport: SetPanel(_arcPanel, true); break;
                case GameState.Paused: RefreshPause(); SetPanel(_pausePanel, true); break;
                case GameState.GameOver: RefreshGameOver(); SetPanel(_overPanel, true); break;
            }

            RefreshHudForState(to);
            RefreshPlayerFreeze();
        }
    }
}
