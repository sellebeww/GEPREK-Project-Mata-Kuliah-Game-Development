using Geprek.Progression;
using Geprek.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Geprek.UI
{
    public partial class UIRoot
    {
        RectTransform _socialPanel, _socialList;
        Text _socialPlanText;

        void BuildSocialPanel()
        {
            _socialPanel = NewPanel("SocialMedia");
            var content = Dialog(_socialPanel, "Media Sosial", new Vector2(770f, 596f), out _);

            var hint = UIFactory.Label("Hint", content,
                "Semua efeknya baru terasa besok. Tiap pilihan hanya bisa sekali per malam.",
                UIStyle.FontSmall, UIStyle.InkSoft, TextAnchor.UpperLeft);
            UIFactory.Anchor(hint.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                             new Vector2(0f, -2f), new Vector2(712f, 24f));

            _socialList = ScrollArea(content, new Vector2(0f, -30f), new Vector2(712f, 372f));

            _socialPlanText = UIFactory.Label("Plan", content, "", UIStyle.FontSmall, UIStyle.Ink, TextAnchor.MiddleCenter);
            UIFactory.Anchor(_socialPlanText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                             new Vector2(0f, 62f), new Vector2(712f, 22f));

            var close = UIFactory.Button("Close", content, "Tutup", UIStyle.Wood, UIStyle.Cream,
                                         () => SetPanel(_socialPanel, false));
            UIFactory.Anchor(close.image.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                             new Vector2(0f, 6f), new Vector2(240f, 44f));
        }

        void OpenSocialMedia()
        {
            RefreshSocial();
            SetPanel(_socialPanel, true);
        }

        void RefreshSocial()
        {
            var game = Game;
            if (game == null || nightManager == null || _socialList == null) return;

            for (int i = _socialList.childCount - 1; i >= 0; i--) Destroy(_socialList.GetChild(i).gameObject);

            AddSocialCard(SocialAction.PostFood, "Posting Foto Makanan",
                          "Unggah foto ayam geprek hari ini. Reputasi +3%.", 0, icons.social);

            AddSocialCard(SocialAction.Promotion, "Promosi Berbayar",
                          $"Pelanggan besok +12%, reputasi naik sedikit.",
                          game.Config.promoCost, icons.promo);

            AddSocialCard(SocialAction.Discount, "Umumkan Diskon",
                          "Pelanggan besok +25%, tapi harga jual turun 18%.",
                          0, icons.growth);

            AddSocialCard(SocialAction.InviteVlogger, "Undang Food Vlogger",
                          "Besok food vlogger mampir. Pelayanan sempurna = reputasi melonjak,\ngagal = reputasi turun.",
                          SocialMediaSystem.VloggerCost, icons.star);

            var p = game.Progress;
            var parts = new System.Collections.Generic.List<string>();
            if (p.tomorrowCustomerBonus > 0f) parts.Add($"pelanggan +{Mathf.RoundToInt(p.tomorrowCustomerBonus * 100f)}%");
            if (p.tomorrowPriceScale < 1f) parts.Add($"harga -{Mathf.RoundToInt((1f - p.tomorrowPriceScale) * 100f)}%");
            if (p.vloggerTomorrow) parts.Add("food vlogger datang");

            _socialPlanText.text = parts.Count > 0
                ? $"<b>Rencana besok:</b> {string.Join(", ", parts)}"
                : "<i>Belum ada rencana untuk besok.</i>";
        }

        void AddSocialCard(SocialAction action, string title, string detail, int cost, Sprite icon)
        {
            var game = Game;
            var social = nightManager.Social;

            bool used = social.IsUsed(action);
            bool afford = game.Progress.money >= cost;

            Card(_socialList, icon, title, detail, out var slot, 84f);

            if (used)
            {
                var done = UIFactory.Label("Done", slot, "Sudah dipakai", UIStyle.FontSmall, UIStyle.Leaf,
                                           TextAnchor.MiddleCenter, FontStyle.Bold);
                UIFactory.Stretch(done.rectTransform);
                return;
            }

            string caption = cost > 0 ? MathUtil.ToRupiah(cost) : "Gratis";
            var buy = UIFactory.Button("Do", slot, caption,
                                       afford ? UIStyle.Leaf : new Color(0.62f, 0.58f, 0.55f), Color.white,
                                       () =>
                                       {
                                           nightManager.RunSocial(action);
                                           RefreshSocial();
                                       });
            UIFactory.Stretch(buy.image.rectTransform);
            buy.interactable = afford;
        }
    }
}
