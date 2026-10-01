// In-memory Unity MCP run_script. Lives outside Assets; no runtime code is added to the game.
// Drives the same virtual joystick/button entry points as the touch UI. All movement,
// collision, target selection, cooking, prep, customers and payouts use normal Update loops.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Geprek.Core;
using Geprek.Customers;
using Geprek.Player;
using Geprek.Stations;
using Geprek.World;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

namespace Geprek.VisualVerification
{
    public sealed class Phase3Gameplay
    {
        readonly Stack<IEnumerator> routines = new();
        readonly List<object> events = new();
        readonly List<object> checks = new();
        readonly List<object> samples = new();
        readonly HashSet<string> renderIssues = new();
        readonly Dictionary<string, Vector3> stationPositions = new();
        GameManager game;
        PlayerController player;
        GeprekInput input;
        Rigidbody2D body;
        Transform warung;
        Customer customer;
        MethodInfo encoder;
        string directory, savePath, saveHash, step = "initializing", cashierToast;
        float started, lastSample, stepStarted;
        double realStarted;
        int lastFrame = -1, baselineMoney, payment, expectedPayment;
        bool stopped, paidAnimationSeen, naturalQueueSeen, naturalAngrySeen;

        public static string Start(string runName)
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
            var runner = new Phase3Gameplay();
            runner.Initialize(runName);
            return "Started normal gameplay input run: " + runner.directory;
        }

        void Initialize(string runName)
        {
            game = GameManager.Instance;
            if (game == null || game.State != GameState.MainMenu)
                throw new InvalidOperationException("Use a fresh Main Play Mode session at the main menu.");
            if ((bool)typeof(GameManager).GetField("_sessionStarted", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game))
                throw new InvalidOperationException("Do not attach to a user's loaded game session.");
            if (runName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || runName.Contains(".."))
                throw new ArgumentException("Invalid run name.");
            directory = Path.GetFullPath("Screenshots/VisualAudit_Phase3/runs/" + runName);
            Directory.CreateDirectory(directory);
            player = Object.FindAnyObjectByType<PlayerController>();
            body = player.GetComponent<Rigidbody2D>();
            input = GeprekInput.Instance;
            warung = GameObject.Find("Locations/Warung").transform;
            savePath = Path.Combine(Application.persistentDataPath, "geprek_save.json");
            saveHash = HashSave();
            var captureType = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("Unity.Pipeline.Editor.Commands.Capture.CaptureCommands"))
                .First(t => t != null);
            encoder = captureType.GetMethod("EncodeScreenToPng", BindingFlags.Static | BindingFlags.NonPublic);
            foreach (var station in warung.GetComponentsInChildren<StationBase>(true))
                stationPositions[station.name] = station.transform.localPosition;
            GameEvents.CarryChanged += CarryChanged;
            GameEvents.CustomerResolved += Resolved;
            GameEvents.Toast += Toast;
            EditorApplication.playModeStateChanged += PlayModeChanged;
            UnityEngine.Random.InitState(43026);
            game.Progress.tutorialSeen = true;
            foreach (var recipe in game.Database.recipes)
                if (recipe != null && recipe.unlockArc <= 1 && recipe.unlockCost <= 0
                    && !recipe.fromParents && recipe.unlockLevel <= 1 && !game.Progress.HasRecipe(recipe.id))
                    game.Progress.unlockedRecipes.Add(recipe.id);
            baselineMoney = game.Progress.money;
            game.BeginDay();
            Time.timeScale = 1f;
            Camera.main.transform.position = new Vector3(-0.07777786f, -0.149999857f, -10f);
            started = Time.time;
            realStarted = EditorApplication.timeSinceStartup;
            routines.Push(Run());
            InputSystem.onAfterUpdate += Tick;
            WriteStatus("running");
        }

        void PlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode) Finish(false, "Play Mode stopped during run.");
        }

        void Tick()
        {
            if (stopped || InputState.currentUpdateType != InputUpdateType.Dynamic || lastFrame == Time.frameCount) return;
            lastFrame = Time.frameCount;
            try
            {
                if (Time.time - started > 125f || EditorApplication.timeSinceStartup - realStarted > 240)
                    throw new TimeoutException("Run deadline reached before day closing.");
                Monitor();
                for (int guard = 0; guard < 30; guard++)
                {
                    if (routines.Count == 0) { Finish(true, null); return; }
                    var iterator = routines.Peek();
                    if (!iterator.MoveNext()) { routines.Pop(); continue; }
                    if (iterator.Current is IEnumerator child) { routines.Push(child); continue; }
                    break;
                }
            }
            catch (Exception e)
            {
                try { Capture("failure"); } catch { }
                Finish(false, e.ToString());
            }
        }

        IEnumerator Run()
        {
            Stage("Baseline overview");
            yield return Delay(.3f);
            Capture("00_overview_final");
            var source = warung.Find("Kitchen/SumberAyam").GetComponent<IngredientSource>();
            var rice = warung.Find("Kitchen/SumberNasi").GetComponent<IngredientSource>();
            var fryer = warung.Find("Kitchen/Penggorengan1").GetComponent<CookStation>();
            var prep = warung.Find("Kitchen/Cobek1").GetComponent<PrepStation>();
            var plate = warung.Find("Kitchen/MejaPenyajian").GetComponent<PlateStation>();
            var cashier = warung.Find("Kitchen/Kasir").GetComponent<CashierStation>();

            Stage("Walk to AYAM and collect raw chicken");
            yield return Upper(source.transform.position.x);
            yield return Press(source);
            Check("Raw chicken obtained from source", HeldId() == "ayam_mentah");
            Capture("01_raw_chicken");

            Stage("Walk to GORENG and cook on the real timer");
            yield return Upper(fryer.transform.position.x);
            yield return Press(fryer);
            Check("Fryer accepts chicken and empties hands", fryer.IsBusy && player.Carry.IsEmpty);
            yield return Delay(1f);
            Capture("02_cooking");
            yield return Until(() => fryer.CanInteract(player.Carry), 8f, "Fryer ready");
            yield return Press(fryer);
            Check("Cooked chicken obtained, not burnt", HeldId() == "ayam_goreng");
            Capture("03_fried_chicken");

            Stage("Walk to GEPREK and hold the input button");
            yield return Upper(prep.transform.position.x);
            yield return Press(prep);
            Check("Cobek accepts cooked chicken", !prep.IsEmpty && player.Carry.IsEmpty);
            yield return Until(() => ReferenceEquals(player.Interactor.Current, prep), 2f, "Target cobek");
            input.PressInteract();
            yield return Delay(.75f);
            Capture("04_geprek_in_progress");
            yield return Until(() => !prep.UsesHold(player.Carry), 5f, "Geprek finishes through held input");
            input.ReleaseInteract();
            yield return Delay(.12f);
            yield return Press(prep);
            Check("Geprek result obtained", HeldId() == "ayam_geprek" && prep.IsEmpty);
            Capture("05_geprek_ready");

            Stage("Add chicken to MEJA SAJI");
            yield return ToPlate();
            yield return Press(plate);
            Check("Chicken deposited on plate", player.Carry.IsEmpty && !plate.IsEmptyPlate);
            Stage("Collect NASI and complete the recipe");
            yield return Upper(rice.transform.position.x);
            yield return Press(rice);
            Check("Rice obtained from source", HeldId() == "nasi");
            yield return ToPlate();
            yield return Press(plate);
            Check("Two ingredients match Geprek Original", plate.Match != null && plate.Match.id == "geprek_original");
            Capture("06_plated_recipe");
            yield return Press(plate);
            Check("Finished dish picked up", player.Carry.Held?.recipe?.id == "geprek_original" && plate.IsEmptyPlate);
            Capture("07_dish_in_hand");

            Stage("Deliver to a naturally spawned customer");
            yield return Until(() => FindWaiting() != null, 8f, "A customer orders Geprek Original");
            customer = FindWaiting();
            var assignedSeat = customer.AssignedSeat;
            Event("target_customer", new { type = customer.Type.id, order = customer.Order.id, patience = customer.PatienceLeft });
            yield return Move(new Vector2(body.position.x, -1.35f));
            yield return Move(new Vector2(customer.transform.position.x, -1.35f));
            yield return Move(new Vector2(customer.transform.position.x, -1.70f));
            expectedPayment = Geprek.Economy.Payout.Compute(game.Config, customer.Order, customer.Type,
                customer.PatienceLeft, player.Carry.Held.quality, game.PriceBonus, out _, out _, game.TodayPriceScale);
            yield return Press(customer);
            Check("Customer receives the correct dish", player.Carry.IsEmpty && customer.State == CustomerState.Happy);
            Check("One sale and payout recorded", game.Today.served == 1 && payment > 0 && payment == expectedPayment
                && game.Today.revenue == payment && game.Progress.money == baselineMoney + payment);
            Capture("08_served_happy");
            yield return Until(() => customer.State == CustomerState.Eating, 2f, "Customer eating");
            yield return Delay(.15f);
            Capture("09_eating");
            yield return Until(() => customer.State == CustomerState.Paying, 5f, "Customer paying");
            paidAnimationSeen = true;
            yield return Delay(.15f);
            Capture("10_paying");
            yield return Until(() => customer.State == CustomerState.Leaving, 2f, "Customer leaves");
            Check("Customer frees the seat after paying", assignedSeat.Occupant == null || assignedSeat.Occupant != customer);

            Stage("Walk to KASIR and inspect sales recap");
            yield return Move(new Vector2(body.position.x, -.65f));
            yield return Move(new Vector2(cashier.transform.position.x, -.65f));
            yield return Move(new Vector2(cashier.transform.position.x, -.275f));
            yield return Press(cashier);
            Check("Cashier recap includes one completed sale", cashierToast != null && cashierToast.Contains("1 dilayani"));
            yield return Delay(.3f);
            var toast = Object.FindAnyObjectByType<Geprek.UI.UIRoot>().transform.Find("HUD/Toast");
            var toastText = toast.Find("Text").GetComponent<UnityEngine.UI.Text>();
            var toastIcon = toast.Find("Icon").GetComponent<UnityEngine.UI.Image>();
            Canvas.ForceUpdateCanvases();
            var textCorners = new Vector3[4]; var iconCorners = new Vector3[4];
            toastText.rectTransform.GetWorldCorners(textCorners); toastIcon.rectTransform.GetWorldCorners(iconCorners);
            Check("Cashier toast text clear of icon", textCorners[0].x > iconCorners[2].x);
            Check("Cashier recap fits one readable line", toastText.cachedTextGenerator.lineCount == 1
                && toastText.cachedTextGenerator.fontSizeUsedForBestFit / toastText.pixelsPerUnit >= 14);
            Capture("11_cashier_recap");
            yield return Until(() => customer == null, 10f, "Paid customer exits and despawns");
            Check("Full served/eating/paying/leaving lifecycle", paidAnimationSeen);

            // Observe normal arrivals and patience decay as well as the successful sale.
            // No customer state, timer, order, money or station field is forced.
            Stage("Observe natural queue and mood changes");
            float until = Time.time + 65f;
            bool neutralCapture = false, angryCapture = false, queueCapture = false;
            while (Time.time < until)
            {
                var all = Object.FindObjectsByType<Customer>();
                if (!neutralCapture && all.Any(c => c.State == CustomerState.Waiting && c.PatienceLeft <= .55f && c.PatienceLeft > .35f))
                { Capture("12_natural_waiting"); neutralCapture = true; }
                if (!angryCapture && all.Any(c => c.State == CustomerState.Waiting && c.PatienceLeft <= .30f && c.PatienceLeft > .05f))
                { Capture("13_natural_impatient"); angryCapture = true; naturalAngrySeen = true; }
                if (!queueCapture && all.Any(c => c.State == CustomerState.Queueing))
                { Capture("14_natural_queue"); queueCapture = true; naturalQueueSeen = true; }
                if (neutralCapture && angryCapture && queueCapture) break;
                yield return null;
            }
            Check("Neutral and impatient icons observed with natural patience decay", neutralCapture && angryCapture);
            Check("Queue observed during normal arrivals", naturalQueueSeen);
            Check("Station positions stable throughout gameplay", renderIssues.Count == 0);
            Check("Save unchanged", HashSave() == saveHash);
            Stage("Complete");
        }

        Customer FindWaiting() => Object.FindObjectsByType<Customer>()
            .Where(c => c.State == CustomerState.Waiting && c.Order != null && c.Order.id == "geprek_original")
            .OrderBy(c => c.PatienceLeft).FirstOrDefault();

        IEnumerator Upper(float x)
        {
            if (body.position.y < 1.1f)
            {
                float gap = body.position.x < -3.5f ? -4.125f : -1.625f;
                yield return Move(new Vector2(body.position.x, -.65f));
                yield return Move(new Vector2(gap, -.65f));
                yield return Move(new Vector2(gap, 1.4125f));
            }
            yield return Move(new Vector2(x, 1.4125f));
        }

        IEnumerator ToPlate()
        {
            yield return Move(new Vector2(-1.625f, 1.4125f));
            yield return Move(new Vector2(-1.625f, -.65f));
            yield return Move(new Vector2(0f, -.65f));
            yield return Move(new Vector2(0f, -.275f));
        }

        IEnumerator Move(Vector2 target)
        {
            float deadline = Time.time + 9f;
            var from = body.position;
            while (Vector2.Distance(body.position, target) > .035f)
            {
                if (Time.time > deadline) throw new TimeoutException("Movement blocked: " + from + " -> " + target + ", at " + body.position);
                Vector2 delta = target - body.position;
                input.SetVirtualMove(delta.normalized * Mathf.Clamp01(delta.magnitude / .28f));
                yield return null;
            }
            input.SetVirtualMove(Vector2.zero);
            yield return Delay(.10f);
            Event("walk", new { from = Vec(from), to = Vec(body.position), target = Vec(target) });
        }

        IEnumerator Press(IInteractable target)
        {
            input.SetVirtualMove(Vector2.zero);
            input.ReleaseInteract();
            yield return Until(() => ReferenceEquals(player.Interactor.Current, target) && target.CanInteract(player.Carry),
                2f, "Input target: " + target.Transform.name);
            Event("press", new { target = target.Transform.name, hint = target.Hint(player.Carry), position = Vec(body.position) });
            input.PressInteract();
            yield return null;
            input.ReleaseInteract();
            yield return Delay(.14f);
        }

        IEnumerator Delay(float seconds) { float until = Time.time + seconds; while (Time.time < until) yield return null; }
        IEnumerator Until(Func<bool> condition, float seconds, string label)
        {
            float deadline = Time.time + seconds;
            while (!condition())
            {
                if (Time.time > deadline) throw new TimeoutException(label + "; current target=" + player.Interactor.Current?.Transform.name);
                yield return null;
            }
        }

        void Monitor()
        {
            foreach (var station in warung.GetComponentsInChildren<StationBase>(true))
                if ((station.transform.localPosition - stationPositions[station.name]).sqrMagnitude > .000001f)
                    renderIssues.Add("Station moved: " + station.name);
            if (Time.time - lastSample < .25f) return;
            lastSample = Time.time;
            samples.Add(new { t = Time.time - started, step, player = Vec(body.position), held = HeldId(),
                target = player.Interactor.Current?.Transform.name, revenue = game.Today.revenue,
                customers = Customers() });
            WriteStatus("running");
        }

        object[] Customers() => Object.FindObjectsByType<Customer>().Select(c => (object)new {
            id = c.GetEntityId().ToString(), type = c.Type?.id, state = c.State.ToString(), position = Vec(c.transform.position),
            patience = c.PatienceLeft, mood = c.transform.Find("Bubble/Mood")?.GetComponent<SpriteRenderer>().sprite?.name,
            bodyOrder = c.transform.Find("Sprite").GetComponent<SpriteRenderer>().sortingOrder,
            bubbleOrder = c.transform.Find("Bubble/Bg").GetComponent<SpriteRenderer>().sortingOrder
        }).ToArray();

        void Capture(string name)
        {
            File.WriteAllBytes(Path.Combine(directory, name + ".png"), (byte[])encoder.Invoke(null, new object[] { 1600, 900 }));
            var cam = Camera.main;
            var sprites = warung.GetComponentsInChildren<SpriteRenderer>(true)
                .Where(s => s.enabled && s.gameObject.activeInHierarchy && s.sprite != null)
                .Select(s => new { path = PathOf(s.transform), sprite = s.sprite.name, order = s.sortingOrder,
                    position = Vec(s.transform.position), scale = Vec(s.transform.lossyScale) }).ToArray();
            Write(name + ".json", new { t = Time.time - started, step, camera = Vec(cam.transform.position), size = cam.orthographicSize,
                aspect = cam.aspect, player = Vec(body.position), held = HeldId(), revenue = game.Today.revenue,
                customers = Customers(), sprites });
            Event("screenshot", name);
        }

        void Stage(string name) { step = name; stepStarted = Time.time; Event("step", name); }
        void Event(string kind, object detail) => events.Add(new { t = Time.time - started, kind, detail });
        void Check(string name, bool pass)
        {
            checks.Add(new { name, passed = pass });
            if (!pass) throw new InvalidOperationException("Check failed: " + name);
        }
        string HeldId() => player.Carry.Held?.recipe?.id ?? player.Carry.Held?.def?.id;
        void CarryChanged(Geprek.Data.CarriedItem item) => Event("carry", item?.recipe?.id ?? item?.def?.id ?? "empty");
        void Resolved(ServeOutcome outcome, int pay) { if (pay > 0) payment += pay; Event("customer_resolved", new { outcome = outcome.ToString(), pay }); }
        void Toast(string text, Sprite icon) { if (text.StartsWith("Omzet ")) cashierToast = text; Event("toast", text); }
        string HashSave()
        {
            if (!File.Exists(savePath)) return "absent";
            using var sha = System.Security.Cryptography.SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(savePath))).Replace("-", "");
        }
        static float[] Vec(Vector3 v) => new[] { v.x, v.y, v.z };
        static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;
        void Write(string name, object data) => File.WriteAllText(Path.Combine(directory, name), JsonConvert.SerializeObject(data, Formatting.Indented));
        void WriteStatus(string state) => Write("status.json", new { state, step, elapsed = Time.time - started,
            position = Vec(body.position), held = HeldId(), served = game.Today.served, revenue = game.Today.revenue });

        void Finish(bool success, string error)
        {
            if (stopped) return;
            stopped = true;
            InputSystem.onAfterUpdate -= Tick;
            EditorApplication.playModeStateChanged -= PlayModeChanged;
            GameEvents.CarryChanged -= CarryChanged;
            GameEvents.CustomerResolved -= Resolved;
            GameEvents.Toast -= Toast;
            if (input != null) { input.ReleaseInteract(); input.SetVirtualMove(Vector2.zero); }
            if (body != null) body.linearVelocity = Vector2.zero;
            Time.timeScale = 0f;
            bool saveUnchanged = HashSave() == saveHash;
            Write("result.json", new { success = success && saveUnchanged, error, elapsed = Time.time - started,
                controls = "GeprekInput virtual joystick/button; unmodified runtime physics and timers",
                paidAnimationSeen, naturalQueueSeen, naturalAngrySeen, baselineMoney, payment, expectedPayment,
                money = game.Progress.money, served = game.Today.served, revenue = game.Today.revenue,
                saveBefore = saveHash, saveAfter = HashSave(), saveUnchanged, checks, renderIssues, events, samples });
            WriteStatus(success ? "completed" : "failed");
        }
    }
}
