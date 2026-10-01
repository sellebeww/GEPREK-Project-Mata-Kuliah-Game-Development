using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Geprek.Core;
using Geprek.Customers;
using Geprek.Player;
using Geprek.Progression;
using Geprek.Save;
using UnityEditor;
using UnityEngine;

namespace Geprek.EditorTools
{
    /// <summary>Reproducible, disposable play sessions. Never writes the player's save.</summary>
    [InitializeOnLoad]
    public static class GeprekPreview
    {
        [Serializable] public class Report
        {
            public int tier, served, revenue, wrongOrders, seated;
            public string state, plate;
            public string[] customerStates, poses, jobs, workers;
        }
        static readonly HashSet<string> States = new(), Poses = new(), Jobs = new();
        static bool observing;
        static int tier;
        static Geprek.Data.GameConfig config;
        static float originalDayLength;

        static GeprekPreview()
        {
            EditorApplication.update += Observe;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingPlayMode)
                {
                    if (observing) WriteReport();
                    if (config != null) config.dayLengthSeconds = originalDayLength;
                    Time.timeScale = 1;
                }
                if (state == PlayModeStateChange.EnteredEditMode)
                { observing = false; SaveSystem.EditorOverridePath = null; }
            };
        }

        [MenuItem("Geprek/Preview/1. Warung (Play Mode)")]
        static void Warung() => Begin(1);
        [MenuItem("Geprek/Preview/2. Ruko + Karyawan (Play Mode)")]
        static void Ruko() => Begin(2);
        [MenuItem("Geprek/Preview/3. Restoran + Karyawan (Play Mode)")]
        static void Restaurant() => Begin(3);

        public static void IsolateSave()
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Masuk Play Mode terlebih dahulu.");
            SaveSystem.EditorOverridePath ??= Path.Combine(Application.temporaryCachePath, "geprek-preview-" + Guid.NewGuid().ToString("N") + ".json");
        }

        public static void Begin(int businessTier)
        {
            IsolateSave();
            var game = GameManager.Instance;
            if (game == null) throw new InvalidOperationException("Buka Main.unity.");
            if (UnityEngine.Object.FindFirstObjectByType<CutsceneDirector>().IsPlaying)
                throw new InvalidOperationException("Selesaikan cutscene atau mulai ulang Play Mode.");
            if (observing) WriteReport();
            game.BackToMenu();
            tier = Mathf.Clamp(businessTier, 1, 3);
            States.Clear(); Poses.Clear(); Jobs.Clear(); observing = true;
            var p = game.Progress;
            p.arcNumber = tier; p.dayInArc = 1; p.tutorialSeen = true;
            p.money = 500000; p.level = 10; p.reputation = .5f;
            p.unlockedRecipes.Clear();
            foreach (var recipe in game.Database.recipes)
                if (recipe.unlockArc <= tier) p.unlockedRecipes.Add(recipe.id);
            p.staff.Clear();
            if (tier >= 2)
            {
                p.staff.Add(new StaffMember { id = "preview-cook", displayName = "Bimo", role = StaffRole.Cook, level = 3 });
                p.staff.Add(new StaffMember { id = "preview-server", displayName = "Sari", role = StaffRole.Server, level = 3 });
                p.staff.Add(new StaffMember { id = "preview-cashier", displayName = "Dimas", role = StaffRole.Cashier, level = 2 });
            }
            if (config == null) { config = game.Config; originalDayLength = config.dayLengthSeconds; }
            config.dayLengthSeconds = 600;
            game.BeginDay();
            GameEvents.RaiseMoney(p.money, 0);
        }

        static void Observe()
        {
            if (!observing || !EditorApplication.isPlaying || GameManager.Instance == null) return;
            foreach (var c in UnityEngine.Object.FindObjectsByType<Customer>(FindObjectsInactive.Exclude))
            {
                States.Add(c.State.ToString());
                var animator = c.GetComponentInChildren<CharacterAnimator>();
                if (animator != null) Poses.Add(animator.Pose.ToString());
            }
            foreach (var worker in UnityEngine.Object.FindObjectsByType<StaffWorker>(FindObjectsInactive.Exclude)) Jobs.Add(worker.WorkStatus);
        }

        public static string Status()
        {
            var game = GameManager.Instance;
            return JsonUtility.ToJson(new Report {
                tier = tier, state = game.State.ToString(), served = game.Today.served,
                revenue = game.Today.revenue, wrongOrders = game.Today.wrongOrders,
                seated = game.ActiveBusiness != null ? game.ActiveBusiness.Spawner.SeatedCount : 0,
                plate = game.ActiveBusiness?.Plate.Match?.id,
                customerStates = States.OrderBy(x => x).ToArray(), poses = Poses.OrderBy(x => x).ToArray(), jobs = Jobs.OrderBy(x => x).ToArray(),
                workers = UnityEngine.Object.FindObjectsByType<StaffWorker>(FindObjectsInactive.Exclude)
                    .Select(x => x.name + ": " + x.WorkStatus + " / " + x.GetComponent<PlayerCarry>().Held?.DisplayName).ToArray()
            }, true);
        }

        public static void WriteReport()
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/reference-playtest-tier" + tier + ".json", Status());
        }
    }
}
