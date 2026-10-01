using System.Collections.Generic;
using System.Reflection;
using Geprek.Core;
using Geprek.Data;
using Geprek.Player;
using Geprek.Stations;
using NUnit.Framework;
using UnityEngine;

namespace Geprek.EditorTools
{
    public class GeprekGameplayTests
    {
        readonly List<Object> _objects = new();

        T Component<T>(string name) where T : Component
        {
            var go = new GameObject(name);
            _objects.Add(go);
            return go.AddComponent<T>();
        }

        ItemDef Item(string id)
        {
            var item = ScriptableObject.CreateInstance<ItemDef>();
            item.id = item.displayName = id;
            _objects.Add(item);
            return item;
        }

        static void SetField(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);

        [SetUp]
        public void RequireIsolatedScene()
        {
            // These checks must not change an open game session or its economy.
            Assert.That(GameManager.Instance, Is.Null, "Run Edit Mode tests in an empty scene.");
        }

        [TearDown]
        public void Cleanup()
        {
            for (int i = _objects.Count - 1; i >= 0; i--)
                if (_objects[i] != null) Object.DestroyImmediate(_objects[i]);
            _objects.Clear();
        }

        [Test]
        public void FryerDoesNotReleaseChickenBeforeReady()
        {
            var carry = Component<PlayerCarry>("TestCarry");
            var fryer = Component<CookStation>("TestFryer");
            var raw = Item("raw");
            raw.cookResult = Item("cooked");
            raw.burnResult = Item("burnt");
            carry.TryTake(CarriedItem.FromItem(raw));
            fryer.Interact(carry);

            Assert.That(carry.IsEmpty, Is.True);
            Assert.That(fryer.IsBusy, Is.True);
            Assert.That(fryer.CanInteract(carry), Is.False);
            fryer.Interact(carry);
            Assert.That(carry.IsEmpty, Is.True, "Early interaction must not produce burnt chicken.");
            Assert.That(fryer.IsBusy, Is.True, "Cooking must continue.");
        }

        [TestCase("Ready", "cooked")]
        [TestCase("Burnt", "burnt")]
        public void FryerReleasesFinishedResult(string phase, string expected)
        {
            var carry = Component<PlayerCarry>("TestCarry");
            var fryer = Component<CookStation>("TestFryer");
            var raw = Item("raw");
            raw.cookResult = Item("cooked");
            raw.burnResult = Item("burnt");
            carry.TryTake(CarriedItem.FromItem(raw));
            fryer.Interact(carry);
            var phaseField = typeof(CookStation).GetField("_phase", BindingFlags.Instance | BindingFlags.NonPublic);
            phaseField.SetValue(fryer, System.Enum.Parse(phaseField.FieldType, phase));
            Assert.That(fryer.CanInteract(carry), Is.True);
            fryer.Interact(carry);
            Assert.That(carry.Held.def.id, Is.EqualTo(expected));
            Assert.That(fryer.IsBusy, Is.False);
        }

        [TestCase(0.45f)]
        [TestCase(0.82f)]
        [TestCase(1f)]
        public void PrepKeepsIncomingQuality(float quality)
        {
            var carry = Component<PlayerCarry>("TestCarry");
            var prep = Component<PrepStation>("TestPrep");
            var cooked = Item("cooked");
            cooked.prepResult = Item("geprek");
            cooked.prepTime = 1f;
            carry.TryTake(CarriedItem.FromItem(cooked, quality));
            prep.Interact(carry);
            prep.HoldTick(carry, 0.4f);
            prep.HoldCancelled();
            prep.HoldTick(carry, 0.7f);
            prep.Interact(carry);
            Assert.That(carry.Held.def, Is.SameAs(cooked.prepResult));
            Assert.That(carry.Held.quality, Is.EqualTo(quality).Within(0.0001f));
            Assert.That(prep.IsEmpty, Is.True);
        }

        [Test]
        public void SwitchingIdenticalHintsStillNotifiesTargetChange()
        {
            var carry = Component<PlayerCarry>("TestCarry");
            var interactor = carry.gameObject.AddComponent<PlayerInteractor>();
            SetField(interactor, "carry", carry);
            var a = Component<PrepStation>("PrepA");
            var b = Component<PrepStation>("PrepB");
            a.gameObject.AddComponent<CircleCollider2D>().isTrigger = true;
            b.gameObject.AddComponent<CircleCollider2D>().isTrigger = true;
            a.transform.position = new Vector3(0f, 0.5f);
            b.transform.position = new Vector3(10f, 0.5f);
            IInteractable notified = null;
            int changes = 0;
            interactor.TargetChanged += (target, _, __) => { notified = target; changes++; };
            Physics2D.SyncTransforms();
            interactor.Scan();
            Assert.That(notified, Is.SameAs(a));
            carry.transform.position = new Vector3(10f, 0f);
            Physics2D.SyncTransforms();
            interactor.Scan();
            Assert.That(notified, Is.SameAs(b));
            Assert.That(changes, Is.EqualTo(2));
        }

        [Test]
        public void FacingSelectsNearbyStationAndDisabledStationsAreIgnored()
        {
            var carry = Component<PlayerCarry>("TestCarry");
            var interactor = carry.gameObject.AddComponent<PlayerInteractor>();
            SetField(interactor, "carry", carry);
            var left = Component<PrepStation>("LeftPrep");
            var right = Component<PrepStation>("RightPrep");
            left.gameObject.AddComponent<CircleCollider2D>().isTrigger = true;
            right.gameObject.AddComponent<CircleCollider2D>().isTrigger = true;
            left.transform.position = Vector3.left * 0.5f;
            right.transform.position = Vector3.right * 0.5f;
            Physics2D.SyncTransforms();
            interactor.SetFacing(Vector2.right);
            interactor.Scan();
            Assert.That(interactor.Current, Is.SameAs(right));
            interactor.SetFacing(Vector2.left);
            interactor.Scan();
            Assert.That(interactor.Current, Is.SameAs(left));
            left.enabled = false;
            interactor.Scan();
            Assert.That(interactor.Current, Is.SameAs(right));
        }

        [Test]
        public void DestroyedTargetCanBeReleasedSafely()
        {
            var interactor = Component<PlayerInteractor>("TestInteractor");
            var prep = Component<PrepStation>("TestPrep");
            SetField(interactor, "_holding", prep);
            SetField(interactor, "_current", prep);
            Object.DestroyImmediate(prep.gameObject);
            Assert.DoesNotThrow(() => interactor.TickHold(true, 0.1f));
            Assert.DoesNotThrow(() => interactor.ReleaseInteract());
            Assert.DoesNotThrow(() => interactor.Scan());
        }
    }
}
