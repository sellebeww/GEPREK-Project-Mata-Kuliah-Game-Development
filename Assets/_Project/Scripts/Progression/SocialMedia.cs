using System;
using Geprek.Core;
using UnityEngine;

namespace Geprek.Progression
{
    public enum SocialAction { PostFood, Promotion, Discount, InviteVlogger }

    /// <summary>
    /// Satu pilihan di layar media sosial: biaya, dampak, dan apakah masih bisa dipakai
    /// malam ini. Dipakai UI untuk menggambar tombolnya tanpa tahu isi aturannya.
    /// </summary>
    public class SocialOption
    {
        public SocialAction action;
        public string title;
        public string detail;
        public int cost;
        public bool used;
        public Sprite icon;

        public bool Affordable(int money) => money >= cost;
        public bool Available(int money) => !used && Affordable(money);
    }

    /// <summary>
    /// Aktivitas media sosial di malam hari. Efeknya tidak langsung terasa:
    /// semuanya baru berlaku pada hari berikutnya, jadi pemain harus merencanakan.
    /// </summary>
    public class SocialMediaSystem
    {
        readonly bool[] _usedTonight = new bool[4];

        public event Action Changed;

        public void ResetNight()
        {
            for (int i = 0; i < _usedTonight.Length; i++) _usedTonight[i] = false;
            Changed?.Invoke();
        }

        public bool IsUsed(SocialAction a) => _usedTonight[(int)a];

        /// <summary>Jalankan satu pilihan. Mengembalikan pesan untuk ditampilkan ke pemain.</summary>
        public string Apply(SocialAction action, GameManager game, out bool success)
        {
            success = false;
            if (game == null) return "";

            if (IsUsed(action)) return "Ini sudah kamu lakukan malam ini.";

            var cfg = game.Config;
            var p = game.Progress;

            switch (action)
            {
                case SocialAction.PostFood:
                    p.AddReputation(0.03f);
                    Mark(action);
                    success = true;
                    return "Foto ayam geprekmu diunggah. Beberapa orang menandai temannya.\n\nReputasi +3%";

                case SocialAction.Promotion:
                    if (!game.TrySpend(cfg.promoCost))
                        return "Uang belum cukup untuk pasang promosi berbayar.";
                    p.tomorrowCustomerBonus += 0.12f;
                    p.AddReputation(cfg.promoReputationGain * 0.5f);
                    Mark(action);
                    success = true;
                    return $"Promosi berbayar dipasang.\n\nPelanggan besok +12%, reputasi naik sedikit.\n(-{Utils.MathUtil.ToRupiah(cfg.promoCost)})";

                case SocialAction.Discount:
                    p.tomorrowCustomerBonus += 0.25f;
                    p.tomorrowPriceScale = 0.82f;
                    Mark(action);
                    success = true;
                    return "Diskon diumumkan.\n\nPelanggan besok +25%, tapi harga jual turun 18%.";

                case SocialAction.InviteVlogger:
                    if (!game.TrySpend(VloggerCost))
                        return "Belum cukup uang untuk mengundang food vlogger.";
                    p.vloggerTomorrow = true;
                    Mark(action);
                    success = true;
                    return "Undangan terkirim.\n\nBesok seorang food vlogger akan mampir. Layani dengan sempurna kalau mau reputasimu melonjak.";
            }

            return "";
        }

        public const int VloggerCost = 40000;

        void Mark(SocialAction a)
        {
            _usedTonight[(int)a] = true;
            Changed?.Invoke();
        }
    }
}
