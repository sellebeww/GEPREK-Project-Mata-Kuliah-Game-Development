using Geprek.Core;
using Geprek.Progression;
using Geprek.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Geprek.UI
{
    public partial class UIRoot
    {
        RectTransform _branchPanel, _branchList;
        Text _branchSummary;

        const int MaxBranches = 4;

        static int OpenBranchCost(int current) => Mathf.RoundToInt(260000 * Mathf.Pow(1.75f, current) / 5000f) * 5000;

        static readonly string[] BranchNames =
        { "Cabang Kampus", "Cabang Pasar", "Cabang Stasiun", "Cabang Mall" };

        void BuildBranchPanel()
        {
            _branchPanel = NewPanel("Branches");
            var content = Dialog(_branchPanel, "Manajemen Cabang", new Vector2(800f, 596f), out _);

            var hint = UIFactory.Label("Hint", content,
                "Cabang menyetor hasil tiap hari tanpa kamu jalankan langsung. Hasilnya ikut naik bersama reputasi.",
                UIStyle.FontSmall, UIStyle.InkSoft, TextAnchor.UpperLeft);
            UIFactory.Anchor(hint.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                             new Vector2(0f, -2f), new Vector2(740f, 24f));

            _branchList = ScrollArea(content, new Vector2(0f, -30f), new Vector2(740f, 372f));

            _branchSummary = UIFactory.Label("Summary", content, "", UIStyle.FontSmall, UIStyle.Ink, TextAnchor.MiddleCenter);
            UIFactory.Anchor(_branchSummary.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                             new Vector2(0f, 62f), new Vector2(740f, 22f));

            var close = UIFactory.Button("Close", content, "Tutup", UIStyle.Wood, UIStyle.Cream,
                                         () => SetPanel(_branchPanel, false));
            UIFactory.Anchor(close.image.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                             new Vector2(0f, 6f), new Vector2(240f, 44f));
        }

        void OpenBranches()
        {
            RefreshBranches();
            SetPanel(_branchPanel, true);
        }

        void RefreshBranches()
        {
            var game = Game;
            if (game == null || _branchList == null) return;

            for (int i = _branchList.childCount - 1; i >= 0; i--) Destroy(_branchList.GetChild(i).gameObject);

            if (game.Progress.arcNumber < 3)
            {
                EmptyNote(_branchList, "Membuka cabang tersedia mulai Arc 3,\nsetelah restoran utama berjalan stabil.");
                _branchSummary.text = "";
                return;
            }

            var list = game.Progress.branches;

            foreach (var b in list)
            {
                string body = $"Tingkat {b.level}/{BranchInfo.MaxLevel}  ·  {b.staffAssigned} karyawan\n" +
                              $"Setoran {MathUtil.ToRupiah(b.DailyIncome(game.Progress.reputation))}/hari  ·  " +
                              $"biaya {MathUtil.ToRupiah(b.DailyCost)}/hari";
                Card(_branchList, icons.branches, b.name, body, out var slot, 96f);

                var row = UIFactory.Rect("Actions", slot);
                UIFactory.Stretch(row);
                UIFactory.Row(row, 6f, null, TextAnchor.MiddleRight);

                bool canLevel = b.level < BranchInfo.MaxLevel;
                var up = UIFactory.Button("Up", row, canLevel ? MathUtil.ToRupiah(b.UpgradeCost) : "Maks",
                                          canLevel ? UIStyle.Leaf : new Color(0.62f, 0.58f, 0.55f), Color.white,
                                          () => UpgradeBranch(game, b));
                UIFactory.Size(up.gameObject, 104f, 42f);
                up.interactable = canLevel && game.Progress.money >= b.UpgradeCost;

                var add = UIFactory.Button("Staff", row, "+Staf", UIStyle.Sky, Color.white,
                                           () => AddBranchStaff(game, b));
                UIFactory.Size(add.gameObject, 76f, 42f);
                add.interactable = b.staffAssigned < 4 && game.Progress.money >= b.StaffCost;
            }

            if (list.Count < MaxBranches)
            {
                int cost = OpenBranchCost(list.Count);
                string name = BranchNames[Mathf.Min(list.Count, BranchNames.Length - 1)];
                Card(_branchList, icons.restaurant, $"Buka {name}",
                     "Cabang baru dengan satu karyawan.\nSetorannya bertambah seiring tingkat dan reputasi.",
                     out var slot, 96f);

                bool afford = game.Progress.money >= cost;
                var open = UIFactory.Button("Open", slot, MathUtil.ToRupiah(cost),
                                            afford ? UIStyle.Leaf : new Color(0.62f, 0.58f, 0.55f), Color.white,
                                            () => OpenBranch(game, cost, name));
                UIFactory.Stretch(open.image.rectTransform);
                open.interactable = afford;
            }
            else
            {
                EmptyNote(_branchList, $"Sudah mencapai batas {MaxBranches} cabang.");
            }

            int income = game.Progress.BranchIncome();
            int cost2 = game.Progress.BranchCost();
            _branchSummary.text = list.Count == 0
                ? "<i>Belum ada cabang.</i>"
                : $"<b>Total per hari:</b> setoran {MathUtil.ToRupiah(income)} · biaya {MathUtil.ToRupiah(cost2)} · " +
                  $"bersih <b>{MathUtil.ToRupiah(income - cost2)}</b>";
        }

        void OpenBranch(GameManager game, int cost, string name)
        {
            if (!game.TrySpend(cost))
            {
                Audio.AudioManager.Play(SfxId.Error);
                ShowToast("Uang tidak cukup untuk membuka cabang.", icons.alert);
                return;
            }

            game.Progress.branches.Add(new BranchInfo
            {
                id = System.Guid.NewGuid().ToString("N")[..6],
                name = name,
                level = 1,
                staffAssigned = 1
            });

            Audio.AudioManager.Play(SfxId.Upgrade);
            ShowToast($"{name} dibuka!", icons.restaurant);
            game.SaveNow();
            RefreshBranches();
        }

        void UpgradeBranch(GameManager game, BranchInfo branch)
        {
            if (!game.TrySpend(branch.UpgradeCost))
            {
                Audio.AudioManager.Play(SfxId.Error);
                ShowToast("Uang tidak cukup.", icons.alert);
                return;
            }
            branch.level++;
            Audio.AudioManager.Play(SfxId.Levelup);
            game.SaveNow();
            RefreshBranches();
        }

        void AddBranchStaff(GameManager game, BranchInfo branch)
        {
            if (!game.TrySpend(branch.StaffCost))
            {
                Audio.AudioManager.Play(SfxId.Error);
                ShowToast("Uang tidak cukup.", icons.alert);
                return;
            }
            branch.staffAssigned++;
            Audio.AudioManager.Play(SfxId.Upgrade);
            game.SaveNow();
            RefreshBranches();
        }
    }
}
