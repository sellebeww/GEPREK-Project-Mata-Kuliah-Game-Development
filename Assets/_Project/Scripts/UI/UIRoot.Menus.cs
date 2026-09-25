using Geprek.Core;
using Geprek.Data;
using Geprek.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Geprek.UI
{
    public partial class UIRoot
    {
        Button _continueButton;
        Text _briefTitle, _briefBody, _reportTitle, _reportBody, _reportGrade, _reportStars,
             _arcTitle, _arcBody, _dialogueSpeaker, _dialogueBody, _overBody;
        Image _dialogueIcon, _reportGradeBg;

        /// <summary>Kotak tengah layar dengan judul dan area isi. Mengembalikan tempat menaruh isi.</summary>
        RectTransform Dialog(RectTransform parent, string title, Vector2 size, out Text titleText)
        {
            var box = UIFactory.Panel("Box", parent, UIStyle.Panel);
            UIFactory.Anchor(box.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);

            var header = UIFactory.Panel("Header", box.transform, UIStyle.Wood);
            UIFactory.Anchor(header.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(size.x - 20f, 54f));

            titleText = UIFactory.Label("Title", header.transform, title, UIStyle.FontHeading, UIStyle.Cream, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Stretch(titleText.rectTransform, 10f);

            var content = UIFactory.Rect("Content", box.transform);
            UIFactory.Anchor(content, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -74f), new Vector2(size.x - 44f, size.y - 90f));
            return content;
        }

        // ---------------------------------------------------------------- menu utama

        void BuildMainMenu()
        {
            _menuPanel = NewPanel("MainMenu");

            var bg = UIFactory.Raw("Bg", _menuPanel, new Color(0.12f, 0.07f, 0.04f));
            UIFactory.Stretch(bg.rectTransform);

            var artwork = Resources.Load<Texture2D>("Art/geprek_menu_warung");
            if (artwork != null)
            {
                var artRect = UIFactory.Rect("WarungIllustration", bg.transform);
                UIFactory.Stretch(artRect);
                var art = artRect.gameObject.AddComponent<RawImage>();
                art.texture = artwork;
                art.raycastTarget = false;
                var fit = artRect.gameObject.AddComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fit.aspectRatio = (float)artwork.width / artwork.height;
            }

            var footer = UIFactory.Raw("FooterShade", _menuPanel, new Color(0.10f, 0.06f, 0.03f, 0.82f));
            footer.rectTransform.anchorMin = Vector2.zero;
            footer.rectTransform.anchorMax = new Vector2(1f, 0f);
            footer.rectTransform.pivot = Vector2.zero;
            footer.rectTransform.sizeDelta = new Vector2(0f, 48f);
            footer.raycastTarget = false;

            var card = UIFactory.Panel("MenuCard", _menuPanel, new Color(0.12f, 0.075f, 0.045f, 0.90f));
            UIFactory.Anchor(card.rectTransform, new Vector2(0.055f, 0.53f), new Vector2(0f, 0.5f),
                             Vector2.zero, new Vector2(430f, 568f));

            var badge = UIFactory.Panel("Badge", card.transform, UIStyle.Gold);
            UIFactory.Anchor(badge.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                             new Vector2(32f, -30f), new Vector2(252f, 29f));
            var badgeText = UIFactory.Label("Text", badge.transform, "DARI WARUNG, JADI CERITA", 12,
                                            UIStyle.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Stretch(badgeText.rectTransform);

            var title = UIFactory.Label("Title", card.transform, "GEPREK!", 76, UIStyle.Gold,
                                         TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Anchor(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                             new Vector2(28f, -66f), new Vector2(378f, 94f));
            UIFactory.Outline(title, UIStyle.ChiliDark, new Vector2(2f, -3f));

            var sub = UIFactory.Label("Sub", card.transform,
                "Racik sambal. Layani pelanggan.\nBesarkan warung ibu dengan caramu.",
                19, UIStyle.Cream, TextAnchor.UpperLeft);
            UIFactory.Anchor(sub.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                             new Vector2(34f, -171f), new Vector2(362f, 66f));

            var rule = UIFactory.Raw("GoldRule", card.transform, new Color(0.97f, 0.76f, 0.24f, 0.45f));
            UIFactory.Anchor(rule.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                             new Vector2(34f, -247f), new Vector2(362f, 2f));
            rule.raycastTarget = false;

            var newBtn = UIFactory.Button("NewGame", card.transform, "Mulai Baru", UIStyle.Chili, UIStyle.Cream, () =>
            {
                Audio.AudioManager.Play(SfxId.Click);
                if (Game.HasSave) SetPanel(_newGameConfirmation, true);
                else Game.NewGame();
            }, 24);
            UIFactory.Anchor(newBtn.image.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                             new Vector2(0f, -274f), new Vector2(362f, 62f));

            _continueButton = UIFactory.Button("Continue", card.transform, "Lanjutkan", UIStyle.Wood, UIStyle.Cream, () =>
            {
                Audio.AudioManager.Play(SfxId.Click);
                Game.ContinueGame();
            }, 22);
            UIFactory.Anchor(_continueButton.image.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                             new Vector2(0f, -348f), new Vector2(362f, 58f));

            var quit = UIFactory.Button("Quit", card.transform, "Keluar", new Color(0.26f, 0.19f, 0.13f),
                                        UIStyle.Cream, QuitGame);
            UIFactory.Anchor(quit.image.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                             new Vector2(0f, -418f), new Vector2(362f, 44f));

            var controls = UIFactory.Label("Controls", card.transform,
                "WASD / PANAH   Gerak     •     E / SPASI   Interaksi\nTahan untuk geprek  •  Kontrol sentuh tersedia", 13,
                new Color(0.83f, 0.76f, 0.64f), TextAnchor.MiddleCenter);
            UIFactory.Anchor(controls.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                             new Vector2(0f, 22f), new Vector2(382f, 54f));

            var credit = UIFactory.Label("Credit", _menuPanel, "Kelompok 4 — Gading · Naufal · Vassel · Farrell",
                                         13, UIStyle.Cream, TextAnchor.MiddleCenter);
            UIFactory.Anchor(credit.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                             new Vector2(0f, 12f), new Vector2(700f, 24f));

            BuildNewGameConfirmation();
        }

        RectTransform _newGameConfirmation;

        void BuildNewGameConfirmation()
        {
            _newGameConfirmation = NewPanel("ConfirmNewGame");
            var content = Dialog(_newGameConfirmation, "Mulai cerita baru?", new Vector2(560f, 300f), out _);
            var body = UIFactory.Label("Warning", content,
                "Progres yang tersimpan akan diganti.\nYakin ingin memulai kembali dari warung ibu?",
                20, UIStyle.Ink, TextAnchor.MiddleCenter);
            UIFactory.Anchor(body.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                             Vector2.zero, new Vector2(480f, 100f));
            var cancel = UIFactory.Button("Cancel", content, "Kembali", UIStyle.Wood, UIStyle.Cream,
                () => SetPanel(_newGameConfirmation, false));
            UIFactory.Anchor(cancel.image.rectTransform, Vector2.zero, Vector2.zero,
                             new Vector2(0f, 12f), new Vector2(225f, 52f));
            var confirm = UIFactory.Button("Confirm", content, "Mulai Baru", UIStyle.Chili, UIStyle.Cream, () =>
            {
                SetPanel(_newGameConfirmation, false);
                Game.NewGame();
            });
            UIFactory.Anchor(confirm.image.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f),
                             new Vector2(0f, 12f), new Vector2(225f, 52f));
        }

        void RefreshMainMenu()
        {
            if (_continueButton == null || Game == null) return;
            bool has = Game.HasSave;
            _continueButton.interactable = has;
            var img = _continueButton.image;
            if (img != null) img.color = has ? UIStyle.Wood : new Color(0.35f, 0.31f, 0.28f);
        }

        static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ---------------------------------------------------------------- briefing

        void BuildBriefing()
        {
            _briefPanel = NewPanel("Briefing");
            var content = Dialog(_briefPanel, "Hari Baru", new Vector2(680f, 500f), out _briefTitle);

            _briefBody = UIFactory.Label("Body", content, "", UIStyle.FontBody, UIStyle.Ink, TextAnchor.UpperLeft);
            UIFactory.Anchor(_briefBody.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(600f, 290f));

            var open = UIFactory.Button("Open", content, "Buka Warung", UIStyle.Leaf, Color.white, () =>
            {
                Audio.AudioManager.Play(SfxId.Click);
                Game.BeginDay();
            }, UIStyle.FontHeading);
            UIFactory.Anchor(open.image.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(300f, 58f));

            var shop = UIFactory.Button("Shop", content, "Toko & Upgrade", UIStyle.Wood, UIStyle.Cream, OpenShop);
            UIFactory.Anchor(shop.image.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 84f), new Vector2(300f, 48f));

            var book = UIFactory.Button("Book", content, "Buku Resep", UIStyle.Sky, Color.white, OpenRecipeBook);
            UIFactory.Anchor(book.image.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0f, 84f), new Vector2(300f, 48f));
        }

        void RefreshBriefing()
        {
            var game = Game;
            if (game == null) return;
            var arc = game.CurrentArc;
            var plan = game.CurrentDay;
            if (arc == null || plan == null) return;

            _briefTitle.text = $"{arc.title} — Hari {game.Progress.dayInArc} dari {arc.DayCount}";

            int repPercent = Mathf.RoundToInt(game.Progress.reputation * 100f);
            var extra = new System.Text.StringBuilder();
            foreach (var o in plan.objectives) extra.Append($"• {o.Describe()}\n");
            if (plan.hasLunchRush) extra.Append("• Bersiap saat <color=#C2452F><b>jam sibuk</b></color> di tengah hari\n");

            _briefBody.text =
                $"<b>{plan.title}</b>\n{plan.briefing}\n\n" +
                $"<b>Target omzet hari ini:</b> {MathUtil.ToRupiah(plan.targetRevenue)}\n" +
                (extra.Length > 0 ? $"<b>Target tambahan:</b>\n{extra}" : "") +
                $"<b>Perkiraan pelanggan:</b> {plan.totalCustomers} orang\n" +
                $"<b>Uang sekarang:</b> {MathUtil.ToRupiah(game.Progress.money)}\n" +
                $"<b>Reputasi:</b> {repPercent}%   ·   <b>Level masak:</b> {game.Progress.level}\n\n" +
                $"<i>Target arc: {MathUtil.ToRupiah(arc.arcRevenueTarget)} " +
                $"(terkumpul {MathUtil.ToRupiah(game.Progress.arcStats.revenue)})</i>";
        }

        // ---------------------------------------------------------------- jeda

        void BuildPause()
        {
            _pausePanel = NewPanel("Pause");
            var content = Dialog(_pausePanel, "Jeda", new Vector2(520f, 400f), out _);

            var resume = UIFactory.Button("Resume", content, "Lanjut Main", UIStyle.Leaf, Color.white, () =>
            {
                Audio.AudioManager.Play(SfxId.Click);
                Game.Resume();
            }, UIStyle.FontHeading);
            UIFactory.Anchor(resume.image.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(340f, 56f));

            var book = UIFactory.Button("Book", content, "Buku Resep", UIStyle.Sky, Color.white, OpenRecipeBook);
            UIFactory.Anchor(book.image.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -76f), new Vector2(340f, 48f));

            var save = UIFactory.Button("Save", content, "Simpan Sekarang", UIStyle.Wood, UIStyle.Cream, () =>
            {
                Game.SaveNow();
                ShowToast("Kemajuan tersimpan.", icons.check);
            });
            UIFactory.Anchor(save.image.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -134f), new Vector2(340f, 48f));

            var menu = UIFactory.Button("Menu", content, "Simpan & Keluar ke Menu", new Color(0.55f, 0.28f, 0.24f), UIStyle.Cream, () =>
            {
                Game.SaveNow();
                Game.Resume();
                Game.BackToMenu();
            });
            UIFactory.Anchor(menu.image.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -192f), new Vector2(340f, 48f));

            _pauseHint = UIFactory.Label("Hint", content, "Tekan Esc untuk menutup.", UIStyle.FontSmall, UIStyle.InkSoft, TextAnchor.MiddleCenter);
            UIFactory.Anchor(_pauseHint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(400f, 24f));
        }

        Text _pauseHint;

        void RefreshPause()
        {
            if (_pauseHint == null || Game == null) return;
            _pauseHint.text = $"Uang {MathUtil.ToRupiah(Game.Progress.money)} · Level {Game.Progress.level} · Esc untuk menutup";
        }

        // ---------------------------------------------------------------- laporan harian

        void BuildDayReport()
        {
            _reportPanel = NewPanel("DayReport");
            var content = Dialog(_reportPanel, "Laporan Hari Ini", new Vector2(700f, 540f), out _reportTitle);

            _reportStars = UIFactory.Label("Stars", content, "", UIStyle.FontTitle - 6, UIStyle.Gold,
                                           TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Anchor(_reportStars.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                             new Vector2(0f, -2f), new Vector2(320f, 46f));
            UIFactory.Outline(_reportStars, new Color(0.45f, 0.30f, 0.08f, 0.9f), new Vector2(2f, -2f));

            _reportGradeBg = UIFactory.Panel("GradeBg", content, UIStyle.Gold, UIFactory.Circle);
            _reportGradeBg.type = Image.Type.Simple;
            UIFactory.Anchor(_reportGradeBg.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-6f, -52f), new Vector2(72f, 72f));

            _reportGrade = UIFactory.Label("Grade", _reportGradeBg.transform, "A", UIStyle.FontTitle, UIStyle.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Stretch(_reportGrade.rectTransform);

            _reportBody = UIFactory.Label("Body", content, "", UIStyle.FontBody, UIStyle.Ink, TextAnchor.UpperLeft);
            UIFactory.Anchor(_reportBody.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -52f), new Vector2(540f, 330f));

            var next = UIFactory.Button("Next", content, "Pulang ke Rumah", UIStyle.Leaf, Color.white, () =>
            {
                Audio.AudioManager.Play(SfxId.Click);
                Game.GoHomeForNight();
            }, UIStyle.FontHeading);
            UIFactory.Anchor(next.image.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(320f, 58f));
        }

        void OnDayEnded(DayStats s)
        {
            if (_reportTitle == null) return;
            _reportTitle.text = $"Laporan Hari {s.dayNumber}";
            _reportGrade.text = s.Grade;
            _reportGradeBg.color = s.TargetMet ? UIStyle.Gold : new Color(0.80f, 0.55f, 0.50f);

            // bintang penuh sebanyak yang dicapai, sisanya bintang kosong
            _reportStars.text = new string('\u2605', s.stars) + new string('\u2606', 3 - s.stars);

            string status = s.TargetMet
                ? "<color=#3E8E41><b>Target tercapai</b></color>"
                : "<color=#B03A2E><b>Target belum tercapai</b></color>";

            _reportBody.text =
                $"{status}\n" + ObjectiveLines(s) + "\n" +
                $"Omzet            : <b>{MathUtil.ToRupiah(s.revenue)}</b>  (target {MathUtil.ToRupiah(s.targetRevenue)})\n" +
                (s.branchIncome > 0 ? $"  (termasuk setoran cabang {MathUtil.ToRupiah(s.branchIncome)})\n" : "") +
                $"Biaya operasional : -{MathUtil.ToRupiah(s.expenses)}\n" +
                $"Laba bersih      : <b>{MathUtil.ToRupiah(s.Profit)}</b>\n\n" +
                $"Pelanggan dilayani : {s.served} dari {s.TotalCustomers}\n" +
                $"Pelayanan sempurna : {s.perfect}   ·   Telat: {s.late}\n" +
                $"Kabur / tak kebagian tempat : {s.abandoned} / {s.turnedAway}\n" +
                $"Salah menu : {s.wrongOrders}   ·   Bahan terbuang: {s.wasted}\n\n" +
                $"Kepuasan rata-rata : {Mathf.RoundToInt(s.AverageSatisfaction * 100f)}%\n" +
                $"XP didapat : +{s.xpGained}";
        }


        /// <summary>Daftar target tambahan beserta status tercapainya, untuk laporan harian.</summary>
        string ObjectiveLines(DayStats stats)
        {
            var game = Game;
            if (game == null) return "";
            var list = game.TodayObjectives;
            if (list == null || list.Count == 0) return "";

            var sb = new System.Text.StringBuilder("\n");
            foreach (var o in list)
            {
                bool met = o.IsMet(stats);
                string mark = met ? "<color=#3E8E41>\u2714</color>" : "<color=#B03A2E>\u2718</color>";
                sb.Append($"{mark} {o.Describe()}  <b>{o.Progress(stats)}</b>\n");
            }
            return sb.ToString();
        }

        // ---------------------------------------------------------------- laporan arc

        void BuildArcReport()
        {
            _arcPanel = NewPanel("ArcReport");
            var content = Dialog(_arcPanel, "Laporan ke Dosen", new Vector2(680f, 470f), out _arcTitle);

            _arcBody = UIFactory.Label("Body", content, "", UIStyle.FontBody, UIStyle.Ink, TextAnchor.UpperLeft);
            UIFactory.Anchor(_arcBody.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(600f, 300f));

            _arcButton = UIFactory.Button("Next", content, "Lanjut", UIStyle.Leaf, Color.white, () =>
            {
                Audio.AudioManager.Play(SfxId.Click);
                Game.AdvanceAfterArcReport(_arcPassed);
            }, UIStyle.FontHeading);
            UIFactory.Anchor(_arcButton.image.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(320f, 58f));
        }

        Button _arcButton;
        bool _arcPassed;

        void OnArcEnded(ArcDef arc, ArcStats stats, bool passed)
        {
            _arcPassed = passed;
            if (_arcTitle == null) return;

            _arcTitle.text = $"{arc.title} — Evaluasi";
            string verdict = passed
                ? "<color=#3E8E41><b>Lulus. Bisnismu layak dikembangkan.</b></color>"
                : "<color=#B03A2E><b>Belum lulus. Dosen minta arc ini diulang.</b></color>";

            _arcBody.text =
                $"{verdict}\n\n" +
                $"Total omzet arc : <b>{MathUtil.ToRupiah(stats.revenue)}</b> " +
                $"(target {MathUtil.ToRupiah(arc.arcRevenueTarget)})\n" +
                $"Pelanggan dilayani : {stats.served}\n" +
                $"Pelanggan hilang : {stats.lost}\n" +
                $"Kepuasan rata-rata : {Mathf.RoundToInt(stats.AverageSatisfaction * 100f)}% " +
                $"(minimal {Mathf.RoundToInt(arc.minSatisfaction * 100f)}%)\n" +
                $"Hari dijalani : {stats.daysPlayed}\n\n" +
                $"<i>{(passed ? arc.outro : "Perbaiki kecepatan pelayanan dan tambah kapasitas tempat duduk.")}</i>";

            if (_arcButton != null)
            {
                var label = _arcButton.GetComponentInChildren<Text>();
                if (label != null) label.text = passed ? "Lanjut ke Arc Berikutnya" : "Ulangi Arc Ini";
            }
        }

        // ---------------------------------------------------------------- malam & tamat

        void BuildNightOverlay()
        {
            // ditaruh di bawah: bar atas sudah dipakai uang, level, dan reputasi
            _nightPanel = UIFactory.Rect("NightHint", _screen);
            UIFactory.Anchor(_nightPanel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(720f, 52f));

            var plate = UIFactory.Panel("Bg", _nightPanel, new Color(0.14f, 0.12f, 0.22f, 0.88f));
            UIFactory.Stretch(plate.rectTransform);

            var label = UIFactory.Label("Text", plate.transform,
                "Malam hari — ngobrol dengan orang tua, buka toko, pasang promosi, lalu tidur untuk mengakhiri hari.",
                UIStyle.FontSmall, UIStyle.Cream, TextAnchor.MiddleCenter);
            UIFactory.Stretch(label.rectTransform, 12f);

            _nightPanel.gameObject.SetActive(false);
        }

        void BuildGameOver()
        {
            _overPanel = NewPanel("GameOver");
            var content = Dialog(_overPanel, "Tamat", new Vector2(620f, 420f), out _);

            _overBody = UIFactory.Label("Body", content, "", UIStyle.FontBody, UIStyle.Ink, TextAnchor.UpperLeft);
            UIFactory.Anchor(_overBody.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(540f, 250f));

            var menu = UIFactory.Button("Menu", content, "Kembali ke Menu", UIStyle.Leaf, Color.white, () => Game.BackToMenu(), UIStyle.FontHeading);
            UIFactory.Anchor(menu.image.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(320f, 58f));
        }

        void RefreshGameOver()
        {
            if (_overBody == null || Game == null) return;
            var p = Game.Progress;
            _overBody.text =
                "Dari warung kecil milik ibu, bisnis ayam geprekmu tumbuh sampai punya cabang sendiri. " +
                "Tugas kuliahmu selesai dengan nilai penuh.\n\n" +
                $"Total omzet sepanjang permainan : <b>{MathUtil.ToRupiah(p.lifetimeRevenue)}</b>\n" +
                $"Pelanggan dilayani : {p.lifetimeServed}\n" +
                $"Level memasak akhir : {p.level}\n" +
                $"Reputasi akhir : {Mathf.RoundToInt(p.reputation * 100f)}%";
        }

        // ---------------------------------------------------------------- dialog

        void BuildDialogue()
        {
            _dialoguePanel = NewPanel("Dialogue");

            var box = UIFactory.Panel("Box", _dialoguePanel, UIStyle.Panel);
            UIFactory.Anchor(box.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(820f, 220f));

            _dialogueIcon = UIFactory.Icon("Icon", box.transform, null, new Vector2(96f, 96f));
            UIFactory.Anchor(_dialogueIcon.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -18f), new Vector2(96f, 96f));

            _dialogueSpeaker = UIFactory.Label("Speaker", box.transform, "", UIStyle.FontHeading, UIStyle.Chili, TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.Anchor(_dialogueSpeaker.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(130f, -16f), new Vector2(600f, 32f));

            _dialogueBody = UIFactory.Label("Body", box.transform, "", UIStyle.FontBody, UIStyle.Ink, TextAnchor.UpperLeft);
            UIFactory.Anchor(_dialogueBody.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(130f, -52f), new Vector2(660f, 110f));

            var close = UIFactory.Button("Close", box.transform, "Tutup", UIStyle.Wood, UIStyle.Cream, () => SetPanel(_dialoguePanel, false));
            UIFactory.Anchor(close.image.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-18f, 16f), new Vector2(170f, 44f));
        }

        void ShowDialogue(string speaker, string body, Sprite icon)
        {
            if (_dialoguePanel == null) return;
            _dialogueSpeaker.text = speaker;
            _dialogueBody.text = body;
            if (_dialogueIcon != null)
            {
                _dialogueIcon.sprite = icon;
                _dialogueIcon.enabled = icon != null;
            }
            // tombol tidur hanya muncul lewat ConfirmSleep
            if (_sleepButton != null) _sleepButton.gameObject.SetActive(false);
            SetPanel(_dialoguePanel, true);
        }

        void ConfirmSleep()
        {
            ShowDialogue("Kasur", "Tidur sekarang dan lanjut ke hari berikutnya?", icons.sleep);
            if (_sleepButton == null)
            {
                _sleepButton = UIFactory.Button("Sleep", _dialoguePanel.GetChild(0), "Tidur", UIStyle.Sky, Color.white, () =>
                {
                    Audio.AudioManager.Play(SfxId.Sleep);
                    SetPanel(_dialoguePanel, false);
                    Game.Sleep();
                });
                UIFactory.Anchor(_sleepButton.image.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-198f, 16f), new Vector2(170f, 44f));
            }
            _sleepButton.gameObject.SetActive(true);
        }

        Button _sleepButton;

        void OnRecipeUnlocked(RecipeDef recipe)
        {
            ShowToast($"Resep baru terbuka: {recipe.displayName}", recipe.icon, 3f);
            RefreshIngredientStations();
        }

        void RefreshIngredientStations()
        {
            var sources = FindObjectsByType<Stations.IngredientSource>(FindObjectsInactive.Include);
            foreach (var s in sources) s.RefreshAvailability();
        }
    }
}
