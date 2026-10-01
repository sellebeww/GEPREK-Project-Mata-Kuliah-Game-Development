using System;
using Geprek.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Geprek.EditorTools
{
    public static partial class GeprekBuilder
    {
        // One grid for the whole counter, including slots reserved for upgrades.
        const float WarungGrid = 1f / 16f;
        const float WarungCounterY = 33f * WarungGrid;
        const float WarungCounterStep = 22f * WarungGrid;
        const float WarungIslandY = 6f * WarungGrid;
        const float WarungIslandStep = 44f * WarungGrid;

        static readonly string[] WarungCounterStations =
        {
            "SumberAyam", "SumberNasi", "Penggorengan1", "Penggorengan2", "Cobek1",
            "SumberSambalMerah", "SumberSambalIjo", "SumberTelur", "SumberTahu",
            "SumberLalapan", "SumberKeju"
        };

        [MenuItem("Geprek/3. Rapikan Visual Warung")]
        public static void PolishOpenWarung()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Keluar dari Play Mode sebelum menata Warung.");
            var root = GameObject.Find("Locations/Warung");
            if (root == null) throw new InvalidOperationException("Buka scene Main dengan Locations/Warung aktif.");

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Rapikan visual Warung");
            Undo.RegisterFullObjectHierarchyUndo(root, "Rapikan visual Warung");
            try
            {
                ApplyWarungVisualLayout(root.transform);
                var player = GameObject.Find("Player");
                if (player != null)
                {
                    Undo.RecordObject(player.transform, "Posisi awal di lorong Warung");
                    player.transform.position = root.transform.TransformPoint(BusinessPlayerSpawn(WarungSpec()));
                }
                EditorSceneManager.MarkSceneDirty(root.scene);
                Undo.CollapseUndoOperations(group);
            }
            catch
            {
                Undo.RevertAllDownToGroup(group);
                throw;
            }
        }

        /// <summary>
        /// Shared by the scene builder and the in-place editor action. Changes only
        /// presentation; station, seat and queue references keep their existing objects.
        /// </summary>
        public static void ApplyWarungVisualLayout(Transform root)
        {
            var spawn = root.Find("PlayerSpawn");
            if (spawn != null) spawn.localPosition = BusinessPlayerSpawn(WarungSpec());
            for (int i = 0; i < WarungCounterStations.Length; i++)
            {
                string stationPath = "Kitchen/" + WarungCounterStations[i];
                Place(root, stationPath, -7f + i * WarungCounterStep, WarungCounterY);
                At(root, stationPath + "/Body").localScale = new Vector3(0.85f, 0.85f, 1f);
            }
            Place(root, "Floor/CounterTop", 0f, WarungCounterY - 0.25f);
            Place(root, "Walls/CounterBlock", 0f, WarungCounterY + 0.01f);

            // Zone boundaries now fall halfway between slots on the same grid.
            float[] edges = { -7.6875f, -4.9375f, -0.8125f, 7.4375f };
            var floor = At(root, "Floor");
            for (int i = 0; i < ZoneStyle.Length; i++)
            {
                var band = At(floor, "Zona_" + ZoneStyle[i].name);
                band.localPosition = new Vector3((edges[i] + edges[i + 1]) * 0.5f,
                                                band.localPosition.y, 0f);
                band.localScale = new Vector3(edges[i + 1] - edges[i], band.localScale.y, 1f);
                foreach (var label in floor.GetComponentsInChildren<TextMesh>(true))
                    if (label.text == ZoneStyle[i].name)
                        label.transform.localPosition = new Vector3(edges[i] + 0.35f, -0.23f, 0f);
            }

            string[] island = { "TempatSampah", "MejaPrep", "MejaPenyajian", "AreaCuci", "Kasir" };
            for (int i = 0; i < island.Length; i++)
                Place(root, "Kitchen/" + island[i], (i - 2) * WarungIslandStep, WarungIslandY);

            // Loose props were floating between work surfaces. Keep the cashier details
            // on its counter; unused cutlery/rail decorations can stay hidden in Hierarchy.
            Hide(root, "Kitchen/AlatMakan");
            Hide(root, "Kitchen/MejaPenyajian/OrderRail");
            Place(root, "Kitchen/DetailKasir", 5.5f, WarungIslandY);
            Place(root, "Kitchen/DetailKasir/Nota", 0.38f, 0.75f);
            Place(root, "Kitchen/DetailKasir/Receh", -0.38f, 0.70f);
            SetField(At(root, "Kitchen/DetailKasir").GetComponent<SortingByY>(), "orderBias", 10);

            Hide(root, "WallDecor/JamDinding");
            Place(root, "WallDecor/BannerAyam", -7f, WarungIslandY);
            Place(root, "WallDecor/RakBahan", -6.375f, 3.1875f);
            Place(root, "WallDecor/RakPelengkap", 2.75f, 3.1875f);
            Place(root, "WallDecor/FotoKeluarga", -7.375f, 3.1875f);
            Place(root, "WallDecor/PosterPedas", 0.125f, 3.4375f);
            Place(root, "WallDecor/Kalender", 4.25f, 3.1875f);
            Place(root, "WallDecor/Fan", 5.375f, 3.1875f);
            Place(root, "WallDecor/MenuBoard", 6.875f, 3.0625f);
            At(root, "WallDecor/MenuBoard").localScale = new Vector3(0.70f, 0.70f, 1f);
            int hood = 0;
            foreach (Transform child in At(root, "WallDecor"))
                if (child.name == "Hood")
                    child.localPosition = new Vector3(-3.5625f + hood++ * 1.625f, 3.1875f, 0f);

            Place(root, "DiningDecor/RakMinum", 6.875f, -0.75f);
            Place(root, "DiningDecor/Dispenser", 6.875f, -2f);
            Place(root, "DiningDecor/TempatSampahTamu", 7.25f, -3.75f);
            Place(root, "DiningDecor/TanamanKanan", 6.25f, -3.75f);
            Place(root, "DiningDecor/RakPiring", 5.25f, -3.75f);
            Place(root, "DiningDecor/TanamanKiri", -7f, -3.75f);
            var crate = At(root, "DiningDecor/KardusSupplier");
            var sprite = crate.GetComponent<SpriteRenderer>().sprite;
            float pivotLift = sprite.pivot.y / sprite.pixelsPerUnit * crate.localScale.y;
            Place(root, "DiningDecor/KardusSupplier", -6f, -3.75f + pivotLift);
            SetField(crate.GetComponent<SortingByY>(), "pivotOffset", -pivotLift);

            foreach (Transform table in At(root, "Dining"))
            {
                var left = At(table, "StoolL").GetComponent<SpriteRenderer>();
                var right = At(table, "StoolR").GetComponent<SpriteRenderer>();
                right.sprite = left.sprite;
                right.color = left.color;
                right.transform.localScale = left.transform.localScale;
                ConfigureTableNumber(At(table, "NomorMeja").GetComponent<SpriteRenderer>());
            }

            // Waypoint transforms are siblings, not children of these visual markers.
            Hide(root, "CustomerSpawner/PembatasAwal");
            Hide(root, "CustomerSpawner/PembatasAkhir");
            Hide(root, "Entrance/PapanMasuk");
            Place(root, "CustomerSpawner/PapanAntre", -4.875f, -1.625f);
            foreach (Transform child in At(root, "CustomerSpawner"))
            {
                if (!child.name.StartsWith("QueueMark", StringComparison.Ordinal)) continue;
                var mark = child.GetComponent<SpriteRenderer>();
                mark.sprite = Ui("fx_ring");
                mark.color = new Color(0.96f, 0.85f, 0.63f, 0.18f);
                mark.sortingOrder = FloorOrder + 7;
                child.localScale = new Vector3(0.42f, 0.42f, 1f);
            }

            foreach (var bar in root.GetComponentsInChildren<WorldProgressBar>(true)) bar.SetVisible(false);
            foreach (var sorting in root.GetComponentsInChildren<SortingByY>(true)) sorting.Apply();
            foreach (var label in root.GetComponentsInChildren<WorldLabel>(true)) label.RefreshSorting();
            ApplyWarungDecorArtwork(root);
        }

        static Transform At(Transform root, string path) => root.Find(path)
            ?? throw new InvalidOperationException("Objek layout tidak ditemukan: " + root.name + "/" + path);

        static void Place(Transform root, string path, float x, float y)
        {
            var target = At(root, path);
            target.localPosition = new Vector3(x, y, target.localPosition.z);
        }

        static void Hide(Transform root, string path) => At(root, path).gameObject.SetActive(false);
    }
}
