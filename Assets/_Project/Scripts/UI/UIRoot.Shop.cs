using System.Collections.Generic;
using System.Text;
using Geprek.Core;
using Geprek.Data;
using Geprek.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Geprek.UI
{
    public partial class UIRoot
    {
        RectTransform _shopList, _bookList;
        Text _shopMoney, _shopTabTitle;
        enum ShopTab { Upgrade, Recipe, Staff }
        ShopTab _shopTab = ShopTab.Upgrade;

        /// <summary>Area gulir standar untuk daftar panjang.</summary>
        RectTransform ScrollArea(RectTransform parent, Vector2 anchoredPos, Vector2 size)
        {
            var viewport = UIFactory.Panel("Viewport", parent, new Color(0.94f, 0.91f, 0.84f, 0.9f), UIFactory.RoundedSoft);
            UIFactory.Anchor(viewport.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), anchoredPos, size);
            viewport.gameObject.AddComponent<RectMask2D>();

            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 26f;
            scroll.viewport = viewport.rectTransform;

            var content = UIFactory.Rect("Content", viewport.transform);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = new Vector2(8f, 0f);
            content.offsetMax = new Vector2(-8f, 0f);

            var layout = UIFactory.Column(content, 8f, new RectOffset(0, 0, 8, 8), TextAnchor.UpperCenter);
            layout.childForceExpandWidth = true;

            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = content;
            return content;
        }

        /// <summary>Satu baris kartu: ikon, judul, keterangan, dan tombol di kanan.</summary>
        RectTransform Card(Transform parent, Sprite icon, string title, string body, out RectTransform actionSlot, float height = 84f)
        {
            var card = UIFactory.Panel("Card", parent, new Color(1f, 0.99f, 0.95f, 1f), UIFactory.RoundedSoft);
            UIFactory.Size(card.gameObject, -1, height);

            var ic = UIFactory.Icon("Icon", card.rectTransform, icon, new Vector2(56f, 56f));
            UIFactory.Anchor(ic.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(56f, 56f));

            var t = UIFactory.Label("Title", card.rectTransform, title, UIStyle.FontBody, UIStyle.Ink, TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.Anchor(t.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(78f, -10f), new Vector2(330f, 24f));

            var b = UIFactory.Label("Body", card.rectTransform, body, UIStyle.FontSmall, UIStyle.InkSoft, TextAnchor.UpperLeft);
            UIFactory.Anchor(b.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(78f, -34f), new Vector2(340f, height - 40f));

            actionSlot = UIFactory.Rect("Action", card.rectTransform);
            UIFactory.Anchor(actionSlot, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-12f, 0f), new Vector2(170f, 52f));
            return card.rectTransform;
        }

        // ---------------------------------------------------------------- toko

        void BuildShop()
        {
            _shopPanel = NewPanel("Shop");
            var content = Dialog(_shopPanel, "Toko & Upgrade", new Vector2(860f, 560f), out _shopTabTitle);

            _shopMoney = UIFactory.Label("Money", content, "", UIStyle.FontBody, UIStyle.Ink, TextAnchor.UpperRight, FontStyle.Bold);
            UIFactory.Anchor(_shopMoney.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0f, -4f), new Vector2(300f, 26f));

            _tabUpgrade = UIFactory.Button("TabUpgrade", content, "Upgrade", UIStyle.Chili, Color.white,
                                           () => { _shopTab = ShopTab.Upgrade; RefreshShop(); });
            UIFactory.Anchor(_tabUpgrade.image.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -2f), new Vector2(150f, 40f));

            _tabRecipe = UIFactory.Button("TabRecipe", content, "Resep", UIStyle.Wood, UIStyle.Cream,
                                          () => { _shopTab = ShopTab.Recipe; RefreshShop(); });
            UIFactory.Anchor(_tabRecipe.image.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(158f, -2f), new Vector2(150f, 40f));

            _tabStaff = UIFactory.Button("TabStaff", content, "Karyawan", UIStyle.Wood, UIStyle.Cream,
                                         () => { _shopTab = ShopTab.Staff; RefreshShop(); });
            UIFactory.Anchor(_tabStaff.image.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(316f, -2f), new Vector2(160f, 40f));

            _shopList = ScrollArea(content, new Vector2(0f, -50f), new Vector2(800f, 380f));

            var close = UIFactory.Button("Close", content, "Tutup", UIStyle.Wood, UIStyle.Cream, () => SetPanel(_shopPanel, false));
            UIFactory.Anchor(close.image.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 4f), new Vector2(240f, 48f));
        }

        Button _tabUpgrade, _tabRecipe, _tabStaff;

        void OpenShop()
        {
            RefreshShop();
            SetPanel(_shopPanel, true);
        }

        void RefreshShop()
        {
            var game = Game;
            if (game == null || _shopList == null) return;

            _shopMoney.text = $"Uang: {MathUtil.ToRupiah(game.Progress.money)}";
            _shopTabTitle.text = _shopTab switch
            {
                ShopTab.Recipe => "Toko — Resep",
                ShopTab.Staff => "Toko — Karyawan",
                _ => "Toko — Upgrade"
            };

            if (_tabUpgrade != null) _tabUpgrade.image.color = _shopTab == ShopTab.Upgrade ? UIStyle.Chili : UIStyle.Wood;
            if (_tabRecipe != null) _tabRecipe.image.color = _shopTab == ShopTab.Recipe ? UIStyle.Chili : UIStyle.Wood;
            if (_tabStaff != null) _tabStaff.image.color = _shopTab == ShopTab.Staff ? UIStyle.Chili : UIStyle.Wood;

            for (int i = _shopList.childCount - 1; i >= 0; i--) Destroy(_shopList.GetChild(i).gameObject);

            switch (_shopTab)
            {
                case ShopTab.Recipe: FillRecipeShop(game); break;
                case ShopTab.Staff: FillStaffShop(game); break;
                default: FillUpgradeShop(game); break;
            }
        }

        void FillUpgradeShop(GameManager game)
        {
            bool any = false;
            foreach (var up in game.Database.upgrades)
            {
                if (up == null || up.unlockArc > game.Progress.arcNumber) continue;
                any = true;

                int level = game.Progress.UpgradeLevel(up.id);
                bool maxed = level >= up.maxLevel;
                int cost = up.CostAt(level);

                string body = $"{up.description}\nTingkat {level}/{up.maxLevel}";
                Card(_shopList, up.icon != null ? up.icon : icons.upgrade, up.displayName, body, out var slot);

                if (maxed)
                {
                    var done = UIFactory.Label("Max", slot, "Maksimal", UIStyle.FontBody, UIStyle.Leaf, TextAnchor.MiddleCenter, FontStyle.Bold);
                    UIFactory.Stretch(done.rectTransform);
                }
                else
                {
                    bool afford = game.Progress.money >= cost;
                    var buy = UIFactory.Button("Buy", slot, MathUtil.ToRupiah(cost),
                                               afford ? UIStyle.Leaf : new Color(0.62f, 0.58f, 0.55f), Color.white,
                                               () => BuyUpgrade(up));
                    UIFactory.Stretch(buy.image.rectTransform);
                    buy.interactable = afford;
                }
            }

            if (!any) EmptyNote(_shopList, "Belum ada upgrade untuk arc ini.");
        }

        void BuyUpgrade(UpgradeDef up)
        {
            var game = Game;
            int level = game.Progress.UpgradeLevel(up.id);
            int cost = up.CostAt(level);

            if (!game.TrySpend(cost))
            {
                Audio.AudioManager.Play(SfxId.Error);
                ShowToast("Uang tidak cukup.", icons.alert);
                return;
            }

            game.Progress.SetUpgradeLevel(up.id, level + 1);
            Audio.AudioManager.Play(SfxId.Upgrade);
            GameEvents.RaiseUpgradePurchased(up, level + 1);
            ShowToast($"{up.displayName} ditingkatkan ke {level + 1}.", up.icon != null ? up.icon : icons.upgrade);

            player?.ApplyUpgrades();
            game.ActiveBusiness?.Spawner?.RefreshSeatUnlocks();
            game.ActiveBusiness?.RefreshFryers(game.ExtraFryers);
            game.SaveNow();
            RefreshShop();
        }

        void FillRecipeShop(GameManager game)
        {
            bool any = false;
            foreach (var r in game.Database.recipes)
            {
                if (r == null || r.fromParents) continue;
                if (r.unlockArc > game.Progress.arcNumber) continue;
                if (game.Progress.HasRecipe(r.id)) continue;
                if (r.unlockCost <= 0) continue;
                any = true;

                bool levelOk = game.Progress.level >= r.unlockLevel;
                string body = $"{Ingredients(r)}\nHarga jual {MathUtil.ToRupiah(r.basePrice)}" +
                              (levelOk ? "" : $"   ·   <color=#B03A2E>butuh level {r.unlockLevel}</color>");

                Card(_shopList, r.icon, r.displayName, body, out var slot);

                bool afford = game.Progress.money >= r.unlockCost && levelOk;
                var buy = UIFactory.Button("Buy", slot, MathUtil.ToRupiah(r.unlockCost),
                                           afford ? UIStyle.Leaf : new Color(0.62f, 0.58f, 0.55f), Color.white,
                                           () => BuyRecipe(r));
                UIFactory.Stretch(buy.image.rectTransform);
                buy.interactable = afford;
            }

            if (!any) EmptyNote(_shopList, "Semua resep yang dijual sudah kamu miliki.\nResep lain didapat dari Ibu di rumah.");
        }

        void BuyRecipe(RecipeDef recipe)
        {
            var game = Game;
            if (!game.TrySpend(recipe.unlockCost))
            {
                Audio.AudioManager.Play(SfxId.Error);
                ShowToast("Uang tidak cukup.", icons.alert);
                return;
            }
            game.Progress.UnlockRecipe(recipe);
            Audio.AudioManager.Play(SfxId.Upgrade);
            game.SaveNow();
            RefreshShop();
        }

        void EmptyNote(RectTransform parent, string message)
        {
            var note = UIFactory.Label("Empty", parent, message, UIStyle.FontBody, UIStyle.InkSoft, TextAnchor.MiddleCenter);
            UIFactory.Size(note.gameObject, -1, 90f);
        }

        static string Ingredients(RecipeDef r)
        {
            var sb = new StringBuilder("Bahan: ");
            for (int i = 0; i < r.components.Count; i++)
            {
                if (i > 0) sb.Append(" + ");
                sb.Append(r.components[i] != null ? r.components[i].displayName : "?");
            }
            return sb.ToString();
        }

        // ---------------------------------------------------------------- buku resep

        void BuildRecipeBook()
        {
            _bookPanel = NewPanel("RecipeBook");
            var content = Dialog(_bookPanel, "Buku Resep", new Vector2(860f, 560f), out _);

            var hint = UIFactory.Label("Hint", content,
                "Urutan memasak: ambil ayam → goreng → geprek di cobek → susun di meja penyajian → antar ke pelanggan.",
                UIStyle.FontSmall, UIStyle.InkSoft, TextAnchor.UpperLeft);
            UIFactory.Anchor(hint.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(800f, 40f));

            _bookList = ScrollArea(content, new Vector2(0f, -46f), new Vector2(800f, 382f));

            var close = UIFactory.Button("Close", content, "Tutup", UIStyle.Wood, UIStyle.Cream, () => SetPanel(_bookPanel, false));
            UIFactory.Anchor(close.image.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 4f), new Vector2(240f, 48f));
        }

        void OpenRecipeBook()
        {
            var game = Game;
            if (game == null || _bookList == null) return;

            for (int i = _bookList.childCount - 1; i >= 0; i--) Destroy(_bookList.GetChild(i).gameObject);

            var known = new List<RecipeDef>();
            var locked = new List<RecipeDef>();
            foreach (var r in game.Database.recipes)
            {
                if (r == null) continue;
                if (game.Progress.HasRecipe(r.id)) known.Add(r);
                else if (r.unlockArc <= game.Progress.arcNumber) locked.Add(r);
            }

            foreach (var r in known)
            {
                string body = $"{Ingredients(r)}\nHarga {MathUtil.ToRupiah(r.basePrice)}   ·   +{r.xpReward} XP";
                Card(_bookList, r.icon, r.displayName, body, out var slot);
                var ok = UIFactory.Label("Owned", slot, "Dikuasai", UIStyle.FontSmall, UIStyle.Leaf, TextAnchor.MiddleCenter, FontStyle.Bold);
                UIFactory.Stretch(ok.rectTransform);
            }

            foreach (var r in locked)
            {
                string how = r.fromParents ? "Tanya Ibu di rumah" : $"Beli di toko {MathUtil.ToRupiah(r.unlockCost)}";
                Card(_bookList, r.icon, "???", $"{how}\nSyarat level {r.unlockLevel}", out var slot);
                var lockLabel = UIFactory.Label("Locked", slot, "Terkunci", UIStyle.FontSmall, UIStyle.InkSoft, TextAnchor.MiddleCenter, FontStyle.Bold);
                UIFactory.Stretch(lockLabel.rectTransform);
            }

            if (known.Count == 0 && locked.Count == 0) EmptyNote(_bookList, "Belum ada resep.");
            SetPanel(_bookPanel, true);
        }
    }
}
