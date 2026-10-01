using Geprek.Customers;
using Geprek.Player;
using Geprek.Progression;
using Geprek.World;
using UnityEditor;
using UnityEngine;

namespace Geprek.EditorTools
{
    public static partial class GeprekBuilder
    {
        const string PrefabDir = Root + "/Prefabs";

        public static GameObject PlayerPrefab { get; private set; }
        public static Customer CustomerPrefab { get; private set; }
        public static GameObject StaffPrefab { get; private set; }

        public static void BuildPrefabs()
        {
            EnsureFolder(PrefabDir + "/Characters");
            PlayerPrefab = SavePrefab(MakePlayer(), PrefabDir + "/Characters/Player.prefab");
            var customerGo = SavePrefab(MakeCustomer(), PrefabDir + "/Characters/Customer.prefab");
            CustomerPrefab = customerGo.GetComponent<Customer>();
            StaffPrefab = SavePrefab(MakeStaff(), PrefabDir + "/Characters/Staff.prefab");
        }

        static GameObject SavePrefab(GameObject source, string path)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(source, path);
            Object.DestroyImmediate(source);
            return prefab;
        }

        // ---------------------------------------------------------------- bagian bersama

        /// <summary>Badan karakter: bayangan, sprite beranimasi, dan urutan gambar per-y.</summary>
        static CharacterAnimator BuildBody(GameObject root, out SpriteRenderer bodySr)
        {
            var shadow = Sr("Shadow", root.transform, Ui("shadow_blob"), new Vector3(0f, 0.06f, 0f));
            shadow.color = new Color(1f, 1f, 1f, 0.55f);
            shadow.transform.localScale = new Vector3(0.9f, 0.9f, 1f);

            var sprite = Go("Sprite", root.transform);
            bodySr = sprite.AddComponent<SpriteRenderer>();

            var anim = sprite.AddComponent<CharacterAnimator>();
            SetField(anim, "spriteRenderer", bodySr);
            SetField(anim, "framesPerSecond", 7f);

            var sorting = root.AddComponent<SortingByY>();
            SetField(sorting, "isStatic", false);
            SetField(sorting, "renderers", new[] { shadow, bodySr });

            return anim;
        }

        // ---------------------------------------------------------------- pemain

        static GameObject MakePlayer()
        {
            var root = new GameObject("Player");

            var rb = root.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            var col = root.AddComponent<CapsuleCollider2D>();
            col.size = new Vector2(0.52f, 0.34f);
            col.offset = new Vector2(0f, 0.17f);
            col.direction = CapsuleDirection2D.Horizontal;

            var anim = BuildBody(root, out var bodySr);
            bodySr.sprite = Database != null && Database.playerSkin != null
                ? Database.playerSkin.GetIdle(Core.Facing.Down) : null;
            SetField(anim, "skin", Database != null ? Database.playerSkin : null);

            // ikon barang bawaan di atas kepala
            var held = Sr("HeldIcon", root.transform, null, new Vector3(0f, 1.42f, 0f), 50);
            held.transform.localScale = new Vector3(0.42f, 0.42f, 1f);
            held.enabled = false;

            var carry = root.AddComponent<PlayerCarry>();
            SetField(carry, "heldIcon", held);
            SetField(carry, "heldAnchor", held.transform);

            var mover = root.AddComponent<TopDownMover>();
            SetField(mover, "speed", 4.4f);

            var interactorGo = Go("Interactor", root.transform, new Vector3(0f, 0.35f, 0f));
            var interactor = interactorGo.AddComponent<PlayerInteractor>();
            SetField(interactor, "carry", carry);
            SetField(interactor, "radius", 1.25f);

            var controller = root.AddComponent<PlayerController>();
            SetField(controller, "animator", anim);
            SetField(controller, "interactor", interactor);
            SetField(controller, "carry", carry);
            SetField(controller, "sorting", root.GetComponent<SortingByY>());

            return root;
        }

        // ---------------------------------------------------------------- pelanggan

        static GameObject MakeCustomer()
        {
            var root = new GameObject("Customer");

            var col = root.AddComponent<CircleCollider2D>();
            col.radius = 0.46f;
            col.offset = new Vector2(0f, 0.45f);
            col.isTrigger = true;                       // tidak menghalangi pemain

            var anim = BuildBody(root, out _);

            var glow = Sr("Highlight", root.transform, Ui("fx_ring"), new Vector3(0f, 0.10f, 0f), -1);
            glow.transform.localScale = new Vector3(0.78f, 0.78f, 1f);
            glow.color = new Color(1f, 0.82f, 0.25f, 0.95f);
            glow.enabled = false;

            var badge = Sr("SpecialBadge", root.transform, Ico("star"), new Vector3(0.30f, 1.20f, 0f), 70);
            badge.transform.localScale = new Vector3(0.22f, 0.22f, 1f);
            badge.enabled = false;

            var bubble = BuildBubble(root.transform);

            var customer = root.AddComponent<Customer>();
            SetField(customer, "animator", anim);
            SetField(customer, "bubble", bubble);
            SetField(customer, "sorting", root.GetComponent<SortingByY>());
            SetField(customer, "highlightGlow", glow);
            SetField(customer, "specialBadge", badge);
            SetField(customer, "walkSpeed", 2.7f);

            return root;
        }

        static OrderBubble BuildBubble(Transform parent)
        {
            var root = Go("Bubble", parent, new Vector3(0f, 1.42f, 0f));
            var bubble = root.AddComponent<OrderBubble>();

            var bgSr = Sr("Bg", root.transform, Ui("bubble"), Vector3.zero, 60);
            bgSr.transform.localScale = new Vector3(0.62f, 0.62f, 1f);

            var dish = Sr("Dish", root.transform, null, new Vector3(-0.09f, 0.12f, 0f), 61);
            dish.transform.localScale = new Vector3(0.30f, 0.30f, 1f);

            var mood = Sr("Mood", root.transform, null, new Vector3(0.21f, 0.24f, 0f), 62);
            mood.transform.localScale = new Vector3(0.16f, 0.16f, 1f);

            var bar = BuildBar(root.transform, new Vector3(0f, -0.14f, 0f), 0.52f, 63, colorByValue: true);

            SetField(bubble, "root", root);
            SetField(bubble, "background", bgSr);
            SetField(bubble, "dishIcon", dish);
            SetField(bubble, "moodIcon", mood);
            SetField(bubble, "patienceBar", bar);
            SetField(bubble, "moodHappy", Ico("mood_happy"));
            SetField(bubble, "moodNeutral", Ico("mood_neutral"));
            SetField(bubble, "moodAnnoyed", Ico("mood_annoyed"));
            SetField(bubble, "moodAngry", Ico("mood_angry"));
            ApplyCustomerArtwork(bubble);
            return bubble;
        }

        /// <summary>Bilah progres kecil. Sprite isi selebar 1 unit supaya skala = nilai.</summary>
        public static WorldProgressBar BuildBar(Transform parent, Vector3 localPos, float width,
                                                int order, bool colorByValue = false)
        {
            var root = Go("Bar", parent, localPos);
            var bar = root.AddComponent<WorldProgressBar>();

            var back = Sr("Back", root.transform, Ui("bar_bg"), Vector3.zero, order);
            back.transform.localScale = new Vector3(width * 1.06f, 0.55f, 1f);

            var fill = Sr("Fill", root.transform, Ui("bar_fill"), Vector3.zero, order + 1);
            fill.color = new Color(0.36f, 0.82f, 0.42f);
            fill.transform.localScale = new Vector3(width, 0.38f, 1f);

            SetField(bar, "background", back);
            SetField(bar, "fill", fill);
            SetField(bar, "width", width);
            SetField(bar, "colorByValue", colorByValue);
            return bar;
        }

        // ---------------------------------------------------------------- karyawan

        static GameObject MakeStaff()
        {
            var root = new GameObject("Staff");

            var col = root.AddComponent<CircleCollider2D>();
            col.radius = 0.4f;
            col.offset = new Vector2(0f, 0.4f);
            col.isTrigger = true;

            var anim = BuildBody(root, out var sr);
            var skin = Database != null && Database.staffSkins.Count > 0 ? Database.staffSkins[0] : null;
            SetField(anim, "skin", skin);
            if (skin != null) sr.sprite = skin.GetIdle(Core.Facing.Down);

            var held = Sr("HeldIcon", root.transform, null, new Vector3(0f, 1.35f, 0f), 50);
            held.transform.localScale = new Vector3(0.38f, 0.38f, 1f);
            held.enabled = false;

            var carry = root.AddComponent<PlayerCarry>();
            SetField(carry, "heldIcon", held);
            SetField(carry, "broadcastEvents", false);

            var worker = root.AddComponent<StaffWorker>();
            SetField(worker, "animator", anim);
            SetField(worker, "carry", carry);
            SetField(worker, "sorting", root.GetComponent<SortingByY>());

            return root;
        }

        // ---------------------------------------------------------------- utilitas

        /// <summary>Isi field privat ber-[SerializeField] lewat SerializedObject.</summary>
        public static void SetField(Object target, string fieldName, object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[Geprek] Field '{fieldName}' tidak ada di {target.GetType().Name}");
                return;
            }
            AssignProperty(prop, value);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AssignProperty(SerializedProperty prop, object value)
        {
            switch (prop.propertyType)
            {
                case SerializedPropertyType.ObjectReference: prop.objectReferenceValue = (Object)value; break;
                case SerializedPropertyType.Float: prop.floatValue = System.Convert.ToSingle(value); break;
                case SerializedPropertyType.Integer: prop.intValue = System.Convert.ToInt32(value); break;
                case SerializedPropertyType.Boolean: prop.boolValue = System.Convert.ToBoolean(value); break;
                case SerializedPropertyType.String: prop.stringValue = (string)value; break;
                case SerializedPropertyType.Color: prop.colorValue = (Color)value; break;
                case SerializedPropertyType.Vector2: prop.vector2Value = (Vector2)value; break;
                case SerializedPropertyType.Vector3: prop.vector3Value = (Vector3)value; break;
                case SerializedPropertyType.Enum: prop.enumValueIndex = System.Convert.ToInt32(value); break;
                default:
                    if (prop.isArray && value is System.Array arr)
                    {
                        prop.arraySize = arr.Length;
                        for (int i = 0; i < arr.Length; i++)
                            AssignProperty(prop.GetArrayElementAtIndex(i), arr.GetValue(i));
                    }
                    break;
            }
        }
    }
}
