using System.Collections.Generic;
using System.Reflection;
using Geprek.Core;
using Geprek.Data;
using Geprek.Player;
using Geprek.Progression;
using Geprek.Stations;
using NUnit.Framework;
using UnityEngine;

namespace Geprek.EditorTools
{
    public class GeprekReferenceTests
    {
        readonly List<Object> created = new();
        static void Field(object obj, string name, object value) => obj.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(obj,value);
        T Data<T>() where T : ScriptableObject { var obj = ScriptableObject.CreateInstance<T>(); created.Add(obj); return obj; }
        GameObject Go(string name) { var go = new GameObject(name); created.Add(go); return go; }

        [TearDown] public void Cleanup()
        {
            for (int i = created.Count - 1; i >= 0; i--) if (created[i] != null) Object.DestroyImmediate(created[i]);
            created.Clear();
        }

        [Test]
        public void CookFinishesRequestedToppingBeforeReleasingBaseDish()
        {
            var root = Go("TestKitchen");
            var workerGo = Go("Cook"); workerGo.transform.SetParent(root.transform);
            workerGo.AddComponent<PlayerCarry>();
            var cook = workerGo.AddComponent<StaffWorker>();
            var plate = Go("Plate").AddComponent<PlateStation>();
            var chicken = Data<ItemDef>(); chicken.id = "ayam_geprek";
            var rice = Data<ItemDef>(); rice.id = "nasi";
            var topping = Data<ItemDef>(); topping.id = "sambal_merah";
            var basic = Data<RecipeDef>(); basic.components = new() { chicken, rice };
            var requested = Data<RecipeDef>(); requested.components = new() { chicken, rice, topping };
            var source = Go("Sambal").AddComponent<IngredientSource>(); source.transform.SetParent(root.transform);
            Field(source,"item",topping);
            Field(plate,"_match",basic);
            Field(cook,"plate",plate); Field(cook,"_recipe",requested); Field(cook,"_componentIndex",2);
            typeof(StaffWorker).GetMethod("NextIngredient",BindingFlags.Instance | BindingFlags.NonPublic).Invoke(cook,null);
            Assert.That(typeof(StaffWorker).GetField("_ingredient",BindingFlags.Instance | BindingFlags.NonPublic).GetValue(cook), Is.SameAs(source));
            Assert.That(typeof(StaffWorker).GetField("_job",BindingFlags.Instance | BindingFlags.NonPublic).GetValue(cook).ToString(), Is.EqualTo("ToIngredient"));
        }

        [Test]
        public void SeatedCustomerFacesTableAndStandingRestoresDirection()
        {
            var skin = Data<CharacterSkin>(); skin.framesPerDirection = 1;
            var texture = new Texture2D(4,4); created.Add(texture);
            var standing = Sprite.Create(texture,new(0,0,2,2),new(.5f,0)); created.Add(standing);
            var sitting = Sprite.Create(texture,new(2,0,2,2),new(.5f,0)); created.Add(sitting);
            var ordering = Sprite.Create(texture,new(0,2,2,2),new(.5f,0)); created.Add(ordering);
            skin.frames = new[] { standing,standing,standing,standing }; skin.seatedRight = sitting; skin.orderingRight = ordering;
            var go = Go("Customer"); var renderer = go.AddComponent<SpriteRenderer>();
            var animator = go.AddComponent<CharacterAnimator>(); Field(animator,"spriteRenderer",renderer);
            animator.SetSkin(skin); animator.SetFacing(Facing.Left); animator.SetPose(CharacterPose.Seated);
            Assert.That(renderer.sprite, Is.SameAs(sitting)); Assert.That(renderer.flipX, Is.True);
            animator.SetPose(CharacterPose.Ordering); Assert.That(renderer.sprite, Is.SameAs(ordering));
            animator.SetPose(CharacterPose.Eating); Assert.That(renderer.sprite, Is.SameAs(sitting));
            animator.SetPose(CharacterPose.Standing); Assert.That(renderer.sprite, Is.SameAs(standing)); Assert.That(renderer.flipX, Is.False);
        }
    }
}
