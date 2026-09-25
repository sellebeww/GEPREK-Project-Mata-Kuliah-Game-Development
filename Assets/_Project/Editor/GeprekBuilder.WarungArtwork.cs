using System;
using Geprek.Customers;
using Geprek.UI;
using Geprek.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Geprek.EditorTools
{
    public static partial class GeprekBuilder
    {
        static Sprite WarungArt(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(
            WarungArtworkImporter.Folder + name + ".png");

        // Also used by the builder so rebuilding the scene/prefab retains the art.
        static void ApplyCustomerArtwork(OrderBubble bubble)
        {
            if (WarungArt("order_bubble") == null) return;
            var root = bubble.transform;
            void Visual(string path, string art, Vector2 position, Vector2 scale, int order)
            {
                var sr = At(root, path).GetComponent<SpriteRenderer>();
                if (art != null) sr.sprite = WarungArt(art);
                sr.color = Color.white;
                sr.sortingOrder = order;
                sr.transform.localPosition = new Vector3(position.x, position.y, 0f);
                sr.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            }
            // A world overlay above characters/furniture, still below the Canvas.
            Visual("Bg", "order_bubble", new Vector2(0f, 0.055f), new Vector2(0.88f, 0.88f), 1000);
            Visual("Dish", null, new Vector2(-0.135f, 0.11f), new Vector2(0.31f, 0.31f), 1001);
            Visual("Mood", "mood_happy", new Vector2(0.255f, 0.18f), new Vector2(0.29f, 0.29f), 1002);
            Visual("Bar/Back", "patience_frame", Vector2.zero, new Vector2(0.64f, 0.52f), 1003);
            Visual("Bar/Fill", "patience_fill", Vector2.zero, new Vector2(0.55f, 0.48f), 1004);
            At(root, "Bar").localPosition = new Vector3(0f, -0.16f, 0f);
            var bar = At(root, "Bar").GetComponent<WorldProgressBar>();
            SetField(bar, "width", 0.55f);
            SetField(bar, "highColor", new Color(0.56f, 0.70f, 0.34f));
            SetField(bar, "midColor", new Color(0.95f, 0.70f, 0.26f));
            SetField(bar, "lowColor", new Color(0.88f, 0.32f, 0.23f));
            SetField(bubble, "moodHappy", WarungArt("mood_happy"));
            SetField(bubble, "moodNeutral", WarungArt("mood_waiting"));
            SetField(bubble, "moodAnnoyed", WarungArt("mood_angry"));
            SetField(bubble, "moodAngry", WarungArt("mood_angry"));
        }

        static void ApplySpawnerArtwork(CustomerSpawner spawner)
        {
            if (WarungArt("mood_happy") == null) return;
            SetField(spawner, "moodHappy", WarungArt("mood_happy"));
            SetField(spawner, "moodNeutral", WarungArt("mood_waiting"));
            SetField(spawner, "moodAngry", WarungArt("mood_angry"));
        }

        static void ApplyUiArtwork(UIRoot ui)
        {
            if (WarungArt("day_banner") == null) return;
            SetField(ui, "titleCardArtwork", WarungArt("day_banner"));
            var so = new SerializedObject(ui);
            var icons = so.FindProperty("icons");
            icons.FindPropertyRelative("moodHappy").objectReferenceValue = WarungArt("mood_happy");
            icons.FindPropertyRelative("moodNeutral").objectReferenceValue = WarungArt("mood_waiting");
            icons.FindPropertyRelative("moodAngry").objectReferenceValue = WarungArt("mood_angry");
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void ApplyWarungDecorArtwork(Transform root)
        {
            if (WarungArt("spice_shelf") == null) return;
            At(root, "WallDecor/RakPelengkap").GetComponent<SpriteRenderer>().sprite = WarungArt("spice_shelf");
            At(root, "WallDecor/RakPelengkap").localScale = new Vector3(0.72f, 0.72f, 1f);
            At(root, "WallDecor/MenuBoard").GetComponent<SpriteRenderer>().sprite = WarungArt("menu_board");
            // Keep the full menu below the top HUD at the normal Warung camera size.
            At(root, "WallDecor/MenuBoard").localScale = new Vector3(0.5f, 0.5f, 1f);
            Place(root, "WallDecor/MenuBoard", 6.875f, 3f);
            ApplyWarungWorkFeedback(root);
        }

        static void ApplyWarungWorkFeedback(Transform root)
        {
            // Work indicators used to sit behind the top HUD at y + 1.42.
            // Keep them on the appliance, above the player's head at its approach point.
            foreach (string name in new[] { "Penggorengan1", "Penggorengan2", "Cobek1" })
            {
                var station = At(root, "Kitchen/" + name);
                At(station, "Bar").localPosition = new Vector3(0f, 0.72f, 0f);
                At(station, "ItemIcon").localPosition = new Vector3(0.55f, 0.53f, 0f);
                var bar = At(station, "Bar").GetComponent<WorldProgressBar>();
                var back = At(station, "Bar/Back").GetComponent<SpriteRenderer>();
                var fill = At(station, "Bar/Fill").GetComponent<SpriteRenderer>();
                back.sprite = WarungArt("patience_frame");
                fill.sprite = WarungArt("patience_fill");
                back.transform.localScale = new Vector3(0.67f, 0.44f, 1f);
                fill.transform.localScale = new Vector3(0.58f, 0.44f, 1f);
                // SortingByY owns the child renderer orders. Group the meter so the
                // pestle/sizzle detail cannot cover its middle while it is working.
                var group = bar.GetComponent<UnityEngine.Rendering.SortingGroup>();
                if (group == null) group = bar.gameObject.AddComponent<UnityEngine.Rendering.SortingGroup>();
                group.sortingLayerID = back.sortingLayerID;
                group.sortingOrder = 10;
                bar.SetVisible(false);
            }
        }

        [MenuItem("Geprek/4. Pasang Artwork Warung")]
        public static void InstallWarungArtwork()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Keluar dari Play Mode sebelum memasang artwork.");
            string[] required = { "mood_happy", "mood_waiting", "mood_angry", "order_bubble",
                "patience_frame", "patience_fill", "spice_shelf", "menu_board", "day_banner" };
            foreach (string name in required)
                if (WarungArt(name) == null) throw new InvalidOperationException("Sprite belum diimpor: " + name);
            var warung = GameObject.Find("Locations/Warung");
            if (warung == null) throw new InvalidOperationException("Buka scene Main dengan Warung aktif.");

            const string path = "Assets/_Project/Prefabs/Characters/Customer.prefab";
            var prefab = PrefabUtility.LoadPrefabContents(path);
            try
            {
                ApplyCustomerArtwork(prefab.GetComponentInChildren<OrderBubble>(true));
                PrefabUtility.SaveAsPrefabAsset(prefab, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Pasang artwork Warung");
            Undo.RegisterFullObjectHierarchyUndo(warung, "Artwork dekorasi Warung");
            ApplyWarungDecorArtwork(warung.transform);
            foreach (var sceneRoot in warung.scene.GetRootGameObjects())
            {
                foreach (var spawner in sceneRoot.GetComponentsInChildren<CustomerSpawner>(true))
                {
                    Undo.RecordObject(spawner, "Artwork ekspresi pelanggan");
                    ApplySpawnerArtwork(spawner);
                }
                foreach (var ui in sceneRoot.GetComponentsInChildren<UIRoot>(true))
                {
                    Undo.RecordObject(ui, "Artwork UI Warung");
                    ApplyUiArtwork(ui);
                }
            }
            EditorSceneManager.MarkSceneDirty(warung.scene);
            Undo.CollapseUndoOperations(group);
            AssetDatabase.SaveAssets();
        }
    }
}
