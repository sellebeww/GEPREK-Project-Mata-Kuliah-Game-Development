using Geprek.Core;
using Geprek.Progression;
using Geprek.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Geprek.UI
{
    public partial class UIRoot
    {
        const int MaxStaff = 4;

        /// <summary>Biaya merekrut karyawan berikutnya, naik tiap kali menambah orang.</summary>
        static int HireCost(int current) => Mathf.RoundToInt(90000 * Mathf.Pow(1.55f, current) / 5000f) * 5000;

        /// <summary>Isi tab karyawan di dalam toko.</summary>
        void FillStaffShop(GameManager game)
        {
            if (game.Progress.arcNumber < 2)
            {
                EmptyNote(_shopList, "Perekrutan karyawan terbuka mulai Arc 2,\nsaat usaha pindah ke ruko.");
                return;
            }

            var staff = game.Progress.staff;

            // daftar karyawan yang sudah ada
            foreach (var m in staff)
            {
                string body = $"{m.RoleDetail}\nTingkat {m.level}  ·  gaji {MathUtil.ToRupiah(m.salary * m.level)}/hari";
                Card(_shopList, RoleIcon(m.role), $"{m.displayName} — {m.RoleName}", body, out var slot, 92f);

                var box = UIFactory.Rect("Actions", slot);
                UIFactory.Stretch(box);
                UIFactory.Row(box, 6f, null, TextAnchor.MiddleRight);

                var up = UIFactory.Button("Up", box, MathUtil.ToRupiah(m.UpgradeCost), UIStyle.Leaf, Color.white,
                                          () => UpgradeStaff(game, m));
                UIFactory.Size(up.gameObject, 104f, 44f);
                up.interactable = game.Progress.money >= m.UpgradeCost && m.level < 5;

                var fire = UIFactory.Button("Fire", box, "Lepas", new Color(0.62f, 0.32f, 0.28f), Color.white,
                                            () => FireStaff(game, m));
                UIFactory.Size(fire.gameObject, 76f, 44f);
            }

            if (staff.Count >= MaxStaff)
            {
                EmptyNote(_shopList, $"Sudah mencapai batas {MaxStaff} karyawan.");
                return;
            }

            // pilihan rekrut
            int cost = HireCost(staff.Count);
            AddHireCard(game, StaffRole.Cook, "Juru Masak",
                        "Mengurus ayam dari kulkas sampai jadi geprek di meja penyajian.", cost);
            AddHireCard(game, StaffRole.Server, "Pramusaji",
                        "Mengantar porsi yang sudah jadi ke pelanggan yang paling mendesak.", cost);
            AddHireCard(game, StaffRole.Cashier, "Kasir",
                        "Berjaga di kasir. Pembayaran jadi lebih royal, harga jual naik.", cost);
        }

        void AddHireCard(GameManager game, StaffRole role, string title, string detail, int cost)
        {
            Card(_shopList, RoleIcon(role), $"Rekrut {title}", $"{detail}\nGaji harian mulai {MathUtil.ToRupiah(15000)}",
                 out var slot, 92f);

            bool afford = game.Progress.money >= cost;
            var hire = UIFactory.Button("Hire", slot, MathUtil.ToRupiah(cost),
                                        afford ? UIStyle.Leaf : new Color(0.62f, 0.58f, 0.55f), Color.white,
                                        () => HireStaff(game, role, cost));
            UIFactory.Stretch(hire.image.rectTransform);
            hire.interactable = afford;
        }

        Sprite RoleIcon(StaffRole role) => role switch
        {
            StaffRole.Cook => icons.chefHat,
            StaffRole.Cashier => icons.coin,
            _ => icons.staffPlus
        };

        void HireStaff(GameManager game, StaffRole role, int cost)
        {
            if (!game.TrySpend(cost))
            {
                Audio.AudioManager.Play(SfxId.Error);
                ShowToast("Uang tidak cukup untuk merekrut.", icons.alert);
                return;
            }

            var member = new StaffMember
            {
                id = System.Guid.NewGuid().ToString("N")[..6],
                displayName = NextStaffName(game),
                role = role,
                level = 1,
                salary = 15000
            };
            game.Progress.staff.Add(member);

            Audio.AudioManager.Play(SfxId.Upgrade);
            ShowToast($"{member.displayName} bergabung sebagai {member.RoleName}.", RoleIcon(role));
            GameEvents.RaiseStaffChanged();
            game.SaveNow();
            RefreshShop();
        }

        void UpgradeStaff(GameManager game, StaffMember member)
        {
            int cost = member.UpgradeCost;
            if (!game.TrySpend(cost))
            {
                Audio.AudioManager.Play(SfxId.Error);
                ShowToast("Uang tidak cukup.", icons.alert);
                return;
            }

            member.level++;
            Audio.AudioManager.Play(SfxId.Levelup);
            ShowToast($"{member.displayName} naik ke tingkat {member.level}.", RoleIcon(member.role));
            GameEvents.RaiseStaffChanged();
            game.SaveNow();
            RefreshShop();
        }

        void FireStaff(GameManager game, StaffMember member)
        {
            game.Progress.staff.Remove(member);
            Audio.AudioManager.Play(SfxId.Click);
            ShowToast($"{member.displayName} berhenti bekerja.", null);
            GameEvents.RaiseStaffChanged();
            game.SaveNow();
            RefreshShop();
        }

        static readonly string[] StaffNames =
        { "Rina", "Bayu", "Sari", "Dimas", "Tika", "Yoga", "Nadia", "Farid" };

        static string NextStaffName(GameManager game)
        {
            int used = game.Progress.staff.Count;
            return StaffNames[used % StaffNames.Length];
        }
    }
}
