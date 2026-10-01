using System;
using System.Collections.Generic;
using Geprek.Core;
using Geprek.Data;
using UnityEngine;

namespace Geprek.Progression
{
    public enum NightAction { Sleep, TalkMother, TalkFather, SocialMedia, Shop, Branches }

    /// <summary>
    /// Isi aktivitas malam di rumah: mengobrol dengan orang tua, promosi medsos,
    /// belanja upgrade, lalu tidur untuk menutup hari.
    /// </summary>
    public class NightManager : MonoBehaviour
    {
        [Header("Dialog ibu")]
        [TextArea] [SerializeField] string[] motherIdleLines =
        {
            "Gimana jualannya hari ini? Ibu lihat pelanggannya mulai ramai.",
            "Jangan lupa istirahat. Besok warung buka pagi lagi.",
            "Sambal itu kuncinya di ulekan, bukan di cabainya saja."
        };
        [TextArea] [SerializeField] string motherRecipeIntro = "Ibu punya resep baru buat kamu:";
        [TextArea] [SerializeField] string motherNotReady = "Latihan dulu ya. Kalau masakanmu sudah lebih rapi, Ibu kasih resep baru.";

        [Header("Dialog ayah")]
        [TextArea] [SerializeField] string[] fatherLines =
        {
            "Catat pemasukan dan pengeluaran. Usaha itu soal angka, bukan perasaan.",
            "Pelanggan yang puas akan kembali lagi. Itu iklan paling murah.",
            "Kalau antre terlalu panjang, tambah kursi dulu sebelum tambah menu."
        };
        [Tooltip("Uang saku dari ayah, sekali per arc.")]
        [SerializeField] int fatherGift = 25000;

        /// <summary>UI berlangganan ini untuk menampilkan dialog.</summary>
        public event Action<string, string, Sprite> DialogueRequested;   // pembicara, isi, ikon
        public event Action ShopRequested;
        public event Action SleepRequested;
        public event Action SocialMediaRequested;
        public event Action BranchesRequested;

        /// <summary>Aturan media sosial. UI membaca daftar pilihannya dari sini.</summary>
        public SocialMediaSystem Social { get; } = new();

        [Header("Ikon")]
        [SerializeField] Sprite motherIcon;
        [SerializeField] Sprite fatherIcon;
        [SerializeField] Sprite phoneIcon;

        bool _talkedToMotherTonight;
        readonly List<string> _giftsThisArc = new();

        GameManager Game => GameManager.Instance;

        void OnEnable() => GameEvents.StateChanged += OnStateChanged;
        void OnDisable() => GameEvents.StateChanged -= OnStateChanged;

        void OnStateChanged(GameState from, GameState to)
        {
            if (to == GameState.NightHome)
            {
                _talkedToMotherTonight = false;
                Social.ResetNight();
            }
            if (to == GameState.ArcReport) _giftsThisArc.Clear();
        }

        public void Do(NightAction action)
        {
            switch (action)
            {
                case NightAction.TalkMother: TalkMother(); break;
                case NightAction.TalkFather: TalkFather(); break;
                case NightAction.SocialMedia: SocialMediaRequested?.Invoke(); break;
                case NightAction.Branches: BranchesRequested?.Invoke(); break;
                case NightAction.Shop: ShopRequested?.Invoke(); break;
                case NightAction.Sleep: SleepRequested?.Invoke(); break;
            }
        }

        // ---------------------------------------------------------------- ibu

        void TalkMother()
        {
            var game = Game;
            if (game == null) return;

            if (!_talkedToMotherTonight)
            {
                var recipe = FindGiftRecipe(game);
                if (recipe != null)
                {
                    _talkedToMotherTonight = true;
                    game.Progress.UnlockRecipe(recipe);
                    Audio.AudioManager.Play(SfxId.Levelup);
                    DialogueRequested?.Invoke("Ibu", $"{motherRecipeIntro}\n\n<b>{recipe.displayName}</b>\n{recipe.description}", motherIcon);
                    return;
                }
            }

            bool anyGiftLeft = FindLockedGiftRecipe(game) != null;
            string line = anyGiftLeft && !_talkedToMotherTonight
                ? motherNotReady
                : motherIdleLines[UnityEngine.Random.Range(0, motherIdleLines.Length)];
            DialogueRequested?.Invoke("Ibu", line, motherIcon);
        }

        /// <summary>Resep hadiah yang syaratnya sudah terpenuhi.</summary>
        RecipeDef FindGiftRecipe(GameManager game)
        {
            foreach (var r in game.Database.recipes)
            {
                if (r == null || !r.fromParents) continue;
                if (game.Progress.HasRecipe(r.id)) continue;
                if (r.unlockArc > game.Progress.arcNumber) continue;
                if (r.unlockLevel > game.Progress.level) continue;
                return r;
            }
            return null;
        }

        /// <summary>Resep hadiah yang masih terkunci, tanpa melihat syaratnya.</summary>
        RecipeDef FindLockedGiftRecipe(GameManager game)
        {
            foreach (var r in game.Database.recipes)
                if (r != null && r.fromParents && !game.Progress.HasRecipe(r.id)) return r;
            return null;
        }

        // ---------------------------------------------------------------- ayah

        void TalkFather()
        {
            var game = Game;
            if (game == null) return;

            string key = $"arc{game.Progress.arcNumber}";
            if (!_giftsThisArc.Contains(key) && fatherGift > 0)
            {
                _giftsThisArc.Add(key);
                game.AddMoney(fatherGift);
                Audio.AudioManager.Play(SfxId.Coin);
                DialogueRequested?.Invoke("Ayah",
                    $"Ini modal tambahan dari Ayah, {Utils.MathUtil.ToRupiah(fatherGift)}. Pakai yang benar ya.", fatherIcon);
                return;
            }

            DialogueRequested?.Invoke("Ayah", fatherLines[UnityEngine.Random.Range(0, fatherLines.Length)], fatherIcon);
        }

        // ---------------------------------------------------------------- medsos

        /// <summary>Jalankan satu pilihan medsos dan tampilkan hasilnya sebagai dialog.</summary>
        public void RunSocial(SocialAction action)
        {
            var game = Game;
            if (game == null) return;

            string message = Social.Apply(action, game, out bool success);
            Audio.AudioManager.Play(success ? SfxId.Notify : SfxId.Error);
            if (!string.IsNullOrEmpty(message))
                DialogueRequested?.Invoke("Handphone", message, phoneIcon);
        }

        public Sprite PhoneIcon => phoneIcon;
    }
}
