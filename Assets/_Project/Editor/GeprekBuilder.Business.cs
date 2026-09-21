using System.Collections.Generic;
using Geprek.Core;
using Geprek.Customers;
using Geprek.Stations;
using Geprek.World;
using UnityEditor;
using UnityEngine;

namespace Geprek.EditorTools
{
    /// <summary>
    /// Resep sebuah tempat usaha. Warung, ruko, dan restoran memakai pembangun yang
    /// sama; yang berbeda hanya ukuran ruangan, jumlah alat, jumlah meja, dan gaya
    /// perabotnya. Tata letaknya dihitung dari batas ruangan supaya semua lokasi
    /// tetap punya alur kerja yang sama: BAHAN -> MASAK -> SAJI -> ruang makan.
    /// </summary>
    public class BusinessSpec
    {
        public LocationId id = LocationId.Warung;
        public string displayName = "Warung";
        public Vector2 min = new(-7.9f, -4.2f);
        public Vector2 max = new(7.9f, 3.9f);
        public float cameraSize = 4.4f;

        public string diningFloor = "floor_wood";
        public string kitchenFloor = "floor_tile";

        public int baseFryers = 1;
        public int upgradeFryers = 1;
        public int cobeks = 1;
        public int plates = 1;

        public int[] tablesPerRow = { 5 };
        public int upgradeSeats = 2;

        public string tableA = "table_wood";
        public string tableB = "table_round";
        public string stoolA = "stool_red";
        public string stoolB = "stool_teal";
        public float furnitureScale = 0.9f;

        public float stationScale = 0.85f;
        public bool modernDecor;

        public int TotalFryers => baseFryers + upgradeFryers;
        public int TotalTables { get { int n = 0; foreach (var r in tablesPerRow) n += r; return n; } }
    }

    public static partial class GeprekBuilder
    {
        const int FloorOrder = -2000;
        const int WallOrder = -1900;
        const int DecorOrder = -1800;

        // ---- angka turunan tata letak, dihitung dari batas ruangan ----
        class Layout
        {
            public float wallBandY, counterY, counterStripY, kitchenFloorBottom, islandY, laneY;
            public float[] tableY, seatY, approachY;

            public Layout(BusinessSpec s)
            {
                wallBandY = s.max.y - 0.80f;
                counterY = wallBandY - 1.05f;
                counterStripY = counterY - 0.25f;
                kitchenFloorBottom = counterY - 2.40f;
                islandY = kitchenFloorBottom + 0.70f;
                laneY = kitchenFloorBottom - 0.80f;

                int rows = s.tablesPerRow.Length;
                tableY = new float[rows]; seatY = new float[rows]; approachY = new float[rows];
                for (int r = 0; r < rows; r++)
                {
                    tableY[r] = laneY - 1.75f - r * 2.05f;
                    seatY[r] = tableY[r] + 0.55f;
                    approachY[r] = tableY[r] + 1.05f;
                }
            }
        }

        static readonly (string name, Color tint)[] ZoneStyle =
        {
            ("BAHAN",  new Color(0.98f, 0.74f, 0.30f, 0.26f)),
            ("MASAK",  new Color(0.95f, 0.42f, 0.28f, 0.24f)),
            ("SAMBAL", new Color(0.38f, 0.76f, 0.44f, 0.24f)),
        };

        /// <summary>Bangun satu tempat usaha lengkap dan kembalikan akarnya.</summary>
        static GameObject BuildBusiness(Transform parent, BusinessSpec spec)
        {
            var root = Go(spec.id.ToString(), parent).transform;
            var L = new Layout(spec);

            var zoneEdges = BuildBusinessFloor(root, spec, L);
            BuildBusinessWalls(root, spec, L);

            var plate = BuildKitchen(root, spec, L, out var upgradeFryers, zoneEdges);
            BuildWallDecor(root, spec, L);
            BuildEntrance(root, spec, L);
            var seats = BuildDining(root, spec, L);
            BuildDiningDecor(root, spec, L);

            var staffIdle = Go("StaffIdle", root, new Vector3(spec.min.x + 3.2f, L.islandY + 0.8f, 0f)).transform;
            var spawner = BuildSpawner(root, spec, L, seats);

            var staffRoot = Go("Staff", root, Vector3.zero).transform;
            var director = staffRoot.gameObject.AddComponent<Progression.StaffDirector>();
            SetField(director, "staffPrefab", StaffPrefab);
            SetField(director, "plate", plate);
            SetField(director, "idleSpot", staffIdle);

            var biz = root.gameObject.AddComponent<BusinessLocation>();
            SetField(biz, "id", (int)spec.id);
            SetField(biz, "displayName", spec.displayName);
            SetField(biz, "spawner", spawner);
            SetField(biz, "plateStation", plate);
            SetField(biz, "staffDirector", director);

            var so = new SerializedObject(biz);
            var list = so.FindProperty("upgradeFryers");
            list.arraySize = upgradeFryers.Count;
            for (int i = 0; i < upgradeFryers.Count; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = upgradeFryers[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            return root.gameObject;
        }

        // ---------------------------------------------------------------- lantai & dinding

        /// <summary>Gambar lantai, dinding, pita zona, dan meja kerja. Mengembalikan batas x tiap zona.</summary>
        static float[] BuildBusinessFloor(Transform root, BusinessSpec spec, Layout L)
        {
            var floors = Go("Floor", root).transform;

            const float overscan = 2.2f;
            float w = spec.max.x - spec.min.x + overscan * 2f;

            Tiled("FloorDining", floors, Ui(spec.diningFloor),
                  new Vector3(0f, (spec.min.y - overscan + L.kitchenFloorBottom) * 0.5f, 0f),
                  new Vector2(w, L.kitchenFloorBottom - spec.min.y + overscan), FloorOrder);

            Tiled("FloorKitchen", floors, Ui(spec.kitchenFloor),
                  new Vector3(0f, (L.kitchenFloorBottom + L.wallBandY) * 0.5f, 0f),
                  new Vector2(w, L.wallBandY - L.kitchenFloorBottom), FloorOrder + 1);

            Tiled("WallBack", floors, Ui("wall_plaster"),
                  new Vector3(0f, (L.wallBandY + spec.max.y + overscan) * 0.5f, 0f),
                  new Vector2(w, spec.max.y - L.wallBandY + overscan), WallOrder);

            var edges = ZoneEdges(spec);
            for (int z = 0; z < 3; z++)
            {
                float x0 = edges[z], x1 = edges[z + 1];
                float top = L.counterStripY - 0.46f;

                var band = Sr($"Zona_{ZoneStyle[z].name}", floors, Ui("bar_fill"),
                              new Vector3((x0 + x1) * 0.5f, (L.kitchenFloorBottom + top) * 0.5f, 0f), FloorOrder + 2);
                band.transform.localScale = new Vector3(x1 - x0, top - L.kitchenFloorBottom, 1f);
                band.color = ZoneStyle[z].tint;

                WorldLabel.Create(floors, ZoneStyle[z].name,
                                  new Vector3(x0 + 0.35f, L.kitchenFloorBottom + 0.30f, 0f),
                                  0.042f, new Color(0.30f, 0.21f, 0.14f, 0.95f),
                                  FloorOrder + 3, TextAnchor.MiddleLeft);
            }

            var divider = Sr("ZoneLine", floors, Ui("bar_bg"), new Vector3(0f, L.kitchenFloorBottom, 0f), FloorOrder + 4);
            divider.transform.localScale = new Vector3(w / 1.06f, 0.15f, 1f);
            divider.color = new Color(0.42f, 0.30f, 0.21f, 0.55f);

            Tiled("CounterTop", floors, Ui("counter_top"),
                  new Vector3(0f, L.counterStripY, 0f), new Vector2(w, 0.92f), FloorOrder + 5);

            return edges;
        }

        /// <summary>Batas x untuk tiga zona dapur, proporsional terhadap jumlah alat di tiap zona.</summary>
        static float[] ZoneEdges(BusinessSpec spec)
        {
            int z1 = 2, z2 = spec.TotalFryers + spec.cobeks, z3 = 6;
            int total = z1 + z2 + z3;
            float x0 = spec.min.x, x1 = spec.max.x, span = x1 - x0;
            return new[]
            {
                x0,
                x0 + span * z1 / total,
                x0 + span * (z1 + z2) / total,
                x1
            };
        }

        static void BuildBusinessWalls(Transform root, BusinessSpec spec, Layout L)
        {
            var walls = Go("Walls", root).transform;
            float w = spec.max.x - spec.min.x;
            float h = spec.max.y - spec.min.y;

            Wall(walls, "Top", new Vector2(0f, L.wallBandY + 0.1f), new Vector2(w, 0.4f));
            Wall(walls, "Bottom", new Vector2(0f, spec.min.y), new Vector2(w, 0.4f));
            Wall(walls, "Left", new Vector2(spec.min.x, 0f), new Vector2(0.4f, h));
            Wall(walls, "Right", new Vector2(spec.max.x, 0f), new Vector2(0.4f, h));
            Wall(walls, "CounterBlock", new Vector2(0f, L.counterY + 0.01f), new Vector2(w, 0.44f));
        }

        // ---------------------------------------------------------------- dapur

        static PlateStation BuildKitchen(Transform root, BusinessSpec spec, Layout L,
                                         out List<UpgradeToggle> upgradeFryers, float[] zoneEdges)
        {
            var kitchen = Go("Kitchen", root).transform;
            upgradeFryers = new List<UpgradeToggle>();

            // posisi setiap alat dihitung merata di dalam zonanya
            var zone1 = SlotsIn(zoneEdges[0], zoneEdges[1], 2);
            var zone2 = SlotsIn(zoneEdges[1], zoneEdges[2], spec.TotalFryers + spec.cobeks);
            var zone3 = SlotsIn(zoneEdges[2], zoneEdges[3], 6);

            Source(kitchen, spec, "SumberAyam", "Kulkas Ayam", House("fridge_small"),
                   new Vector3(zone1[0], L.counterY, 0f), "ayam_mentah", false, "AYAM");
            Source(kitchen, spec, "SumberNasi", "Rice Cooker", House("rice_cooker"),
                   new Vector3(zone1[1], L.counterY, 0f), "nasi", false, "NASI");

            int slot = 0;
            for (int i = 0; i < spec.TotalFryers; i++, slot++)
            {
                bool isUpgrade = i >= spec.baseFryers;
                var fryer = Fryer(kitchen, spec, $"Penggorengan{i + 1}", new Vector3(zone2[slot], L.counterY, 0f),
                                  i == 0 ? "GORENG" : $"GORENG {i + 1}");
                if (!isUpgrade) continue;

                var toggle = fryer.gameObject.AddComponent<UpgradeToggle>();
                SetField(toggle, "startActive", false);
                upgradeFryers.Add(toggle);
            }

            for (int i = 0; i < spec.cobeks; i++, slot++)
                BuildCobek(kitchen, spec, $"Cobek{i + 1}", new Vector3(zone2[slot], L.counterY, 0f),
                           i == 0 ? "GEPREK" : $"GEPREK {i + 1}");

            var toppings = new (string name, string label, Sprite body, string item, bool hide, string plaque)[]
            {
                ("SumberSambalMerah", "Sambal Merah", Env("counter_sambal"), "sambal_merah", false, "SAMBAL"),
                ("SumberSambalIjo",   "Sambal Ijo",   Env("cobek_stand"),   "sambal_ijo",   true,  "S. IJO"),
                ("SumberTelur",       "Rak Telur",    House("prep_table"),  "telur",        true,  "TELUR"),
                ("SumberTahu",        "Rak Tahu",     Env("cabinet_meat"),  "tahu_tempe",   true,  "TAHU"),
                ("SumberLalapan",     "Sayur Segar",  Env("veg_basket"),    "lalapan",      true,  "LALAPAN"),
                ("SumberKeju",        "Kotak Keju",   House("display_case"),"keju",         true,  "KEJU"),
            };
            for (int i = 0; i < toppings.Length; i++)
            {
                var t = toppings[i];
                Source(kitchen, spec, t.name, t.label, t.body, new Vector3(zone3[i], L.counterY, 0f),
                       t.item, t.hide, t.plaque);
            }

            // lantai dapur: pulau penyajian, kasir, tempat sampah
            PlateStation first = null;
            float islandSpan = (spec.max.x - spec.min.x) * 0.5f;
            for (int i = 0; i < spec.plates; i++)
            {
                float x = spec.plates == 1
                    ? spec.min.x + (spec.max.x - spec.min.x) * 0.66f
                    : spec.min.x + islandSpan * 0.55f + i * islandSpan * 0.8f;
                var p = BuildPlate(kitchen, spec, $"MejaPenyajian{(i == 0 ? "" : (i + 1).ToString())}",
                                   new Vector3(x, L.islandY, 0f));
                first ??= p;
            }

            BuildCashier(kitchen, spec, new Vector3(spec.max.x - 2.2f, L.islandY, 0f));

            var trash = NewStation<TrashBin>(kitchen, "TempatSampah", "Tempat Sampah",
                                             House("bin_blue"), new Vector3(zoneEdges[1] + 0.4f, L.islandY, 0f),
                                             spec.stationScale * 1.05f);
            StationPlaque(trash.transform, "SAMPAH", -0.40f);

            return first;
        }

        /// <summary>Bagi rentang x menjadi n titik yang jaraknya sama.</summary>
        static float[] SlotsIn(float x0, float x1, int n)
        {
            var result = new float[Mathf.Max(1, n)];
            float inner = (x1 - x0) * 0.86f;          // sisakan sedikit napas di tepi zona
            float start = x0 + (x1 - x0 - inner) * 0.5f;
            float step = n > 1 ? inner / n : 0f;
            for (int i = 0; i < n; i++)
                result[i] = n > 1 ? start + step * (i + 0.5f) : (x0 + x1) * 0.5f;
            return result;
        }
    }
}
