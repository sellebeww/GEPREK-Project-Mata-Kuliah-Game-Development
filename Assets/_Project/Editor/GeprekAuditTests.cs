using System;
using System.IO;
using System.Reflection;
using Geprek.Core;
using Geprek.Player;
using Geprek.Progression;
using Geprek.Save;
using Geprek.Stations;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Geprek.EditorTools
{
    public class GeprekAuditTests
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        string _oldPath, _path;

        [SetUp] public void Setup()
        {
            _oldPath = SaveSystem.EditorOverridePath;
            _path = Path.Combine(Path.GetTempPath(), "geprek-audit-" + Guid.NewGuid() + ".json");
            SaveSystem.EditorOverridePath = _path;
        }
        [TearDown] public void Cleanup()
        {
            SaveSystem.Delete();
            SaveSystem.EditorOverridePath = _oldPath;
            Time.timeScale = 1f;
        }
        [Test] public void SaveRetainsPreviousSnapshotAndRecoversCorruption()
        {
            SaveSystem.Save(new PlayerProgress { money = 12345, dayCompleted = true });
            SaveSystem.Save(new PlayerProgress { money = 23456 });
            Assert.That(SaveSystem.Load().money, Is.EqualTo(23456));
            File.WriteAllText(_path, "broken json");
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Gagal memuat save"));
            var restored = SaveSystem.Load();
            Assert.That(restored.money, Is.EqualTo(12345));
            Assert.That(restored.dayCompleted, Is.True);
            Assert.That(File.ReadAllText(_path), Is.EqualTo("broken json"));
        }
        [Test] public void InvalidProgressDoesNotLoad()
        {
            File.WriteAllText(_path, "{\"version\":1,\"progress\":null}");
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Gagal memuat save"));
            Assert.That(SaveSystem.Load(), Is.Null);
            Assert.That(File.Exists(_path), Is.True);
        }
        [Test] public void ResetInputClearsHeldAndPendingPress()
        {
            var go = new GameObject("InputTest");
            try
            {
                var input = go.AddComponent<GeprekInput>();
                input.PressInteract(); input.SetVirtualMove(Vector2.one); input.ResetVirtualInput();
                Assert.That(typeof(GeprekInput).GetField("_virtualMove", Private).GetValue(input), Is.EqualTo(Vector2.zero));
                Assert.That(typeof(GeprekInput).GetField("_virtualInteractDown", Private).GetValue(input), Is.False);
                Assert.That(typeof(GeprekInput).GetField("_virtualInteractHeld", Private).GetValue(input), Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
        [Test] public void QueueFollowsFirstWaypointInsteadOfCuttingAcrossRoom()
        {
            var go = new GameObject("QueueTest");
            try
            {
                var customer = go.AddComponent<Geprek.Customers.Customer>();
                var first = new Vector3(2, 0, 0);
                var spot = new Vector3(2, 4, 0);
                customer.GoToQueue(spot, new[] { first, spot });
                Assert.That(typeof(Geprek.Customers.Customer).GetField("_target", Private).GetValue(customer), Is.EqualTo(first));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
        [Test] public void NewShiftClearsCookingAndPrepState()
        {
            var go = new GameObject("KitchenResetTest");
            var raw = ScriptableObject.CreateInstance<Geprek.Data.ItemDef>();
            var cooked = ScriptableObject.CreateInstance<Geprek.Data.ItemDef>();
            var geprek = ScriptableObject.CreateInstance<Geprek.Data.ItemDef>();
            try
            {
                raw.cookResult = cooked; cooked.prepResult = geprek;
                var carry = go.AddComponent<PlayerCarry>();
                var fryer = go.AddComponent<CookStation>();
                var prep = go.AddComponent<PrepStation>();
                carry.TryTake(Geprek.Data.CarriedItem.FromItem(raw)); fryer.Interact(carry);
                carry.TryTake(Geprek.Data.CarriedItem.FromItem(cooked)); prep.Interact(carry);
                Assert.That(fryer.IsBusy, Is.True); Assert.That(prep.IsEmpty, Is.False);
                fryer.ResetForDay(); prep.ResetForDay();
                Assert.That(fryer.IsBusy, Is.False); Assert.That(prep.IsEmpty, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
                UnityEngine.Object.DestroyImmediate(raw); UnityEngine.Object.DestroyImmediate(cooked);
                UnityEngine.Object.DestroyImmediate(geprek);
            }
        }
        [Test] public void CookRecoversWhenPlayerEmptiesClaimedFryer()
        {
            var go = new GameObject("CookTest"); var fryerGo = new GameObject("FryerTest");
            try
            {
                go.AddComponent<PlayerCarry>();
                var worker = go.AddComponent<StaffWorker>();
                var fryer = fryerGo.AddComponent<CookStation>();
                typeof(StaffWorker).GetField("_fryer", Private).SetValue(worker, fryer);
                typeof(StaffWorker).GetField("_holdTimeout", Private).SetValue(worker, 20f);
                var job = typeof(StaffWorker).GetField("_job", Private);
                job.SetValue(worker, Enum.Parse(job.FieldType, "WaitFryer"));
                typeof(StaffWorker).GetMethod("TickCook", Private).Invoke(worker, null);
                Assert.That(job.GetValue(worker).ToString(), Is.EqualTo("Returning"));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(fryerGo); }
        }
    }
}
