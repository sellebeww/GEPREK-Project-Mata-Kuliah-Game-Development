using System.Collections.Generic;
using Geprek.Customers;
using Geprek.Stations;
using Geprek.World;
using UnityEditor;
using UnityEngine;

namespace Geprek.EditorTools
{
    public static partial class GeprekBuilder
    {
        // ---------------------------------------------------------------- rangka stasiun

        /// <summary>Rangka stasiun: badan, ikon isi, cincin sorot, bilah progres, tabrakan.</summary>
        static T NewStation<T>(Transform parent, string name, string label, Sprite body, Vector3 pos,
                               float bodyScale = 0.85f, Vector2 colliderSize = default) where T : StationBase
        {
            var go = Go(name, parent, pos);
            var station = go.AddComponent<T>();

            var sr = Sr("Body", go.transform, body, Vector3.zero);
            sr.transform.localScale = new Vector3(bodyScale, bodyScale, 1f);

            var icon = Sr("ItemIcon", go.transform, null, new Vector3(0f, 1.05f, 0f));
            icon.transform.localScale = new Vector3(0.30f, 0.30f, 1f);
            icon.enabled = false;

            var glow = Sr("Highlight", go.transform, Ui("fx_ring"), new Vector3(0f, 0.12f, 0f), -1);
            glow.transform.localScale = new Vector3(1.15f, 1.0f, 1f);
            glow.color = new Color(1f, 0.82f, 0.25f, 0.95f);
            glow.enabled = false;

            var bar = BuildBar(go.transform, new Vector3(0f, 1.42f, 0f), 0.58f, 5);

            if (colliderSize == default) colliderSize = new Vector2(1.0f, 0.44f);
            Box(go, colliderSize, new Vector2(0f, colliderSize.y * 0.5f));

            var sorting = go.AddComponent<SortingByY>();
            SetField(sorting, "isStatic", true);

            SetField(station, "bodyRenderer", sr);
            SetField(station, "itemIcon", icon);
            SetField(station, "progressBar", bar);
            SetField(station, "highlightGlow", glow);
            SetField(station, "stationName", label);
            return station;
        }

        /// <summary>Papan nama di bibir meja kerja, lebarnya mengikuti panjang teks.</summary>
        static void StationPlaque(Transform station, string text, float yOffset = -0.57f)
        {
            float width = Mathf.Max(0.52f, text.Length * 0.115f + 0.18f);
            var strip = Sr("NamePlate", station, Ui("label_strip"), new Vector3(0f, yOffset, 0f), DecorOrder + 60);
            strip.transform.localScale = new Vector3(width / 1.25f, 1.05f, 1f);

            WorldLabel.Create(strip.transform, text, new Vector3(0f, 0.004f, 0f), 0.040f,
                              new Color(0.99f, 0.95f, 0.88f), DecorOrder + 61);
        }

        static IngredientSource Source(Transform parent, BusinessSpec spec, string name, string label, Sprite body,
                                       Vector3 pos, string itemId, bool hideUntilNeeded, string plaque)
        {
            var station = NewStation<IngredientSource>(parent, name, label, body, pos, spec.stationScale);
            var item = Database.GetItem(itemId);
            SetField(station, "item", item);
            SetField(station, "hideUntilNeeded", hideUntilNeeded);

            var badge = Sr("Badge", station.transform, item != null ? item.icon : null, new Vector3(0f, 0.68f, 0f));
            badge.transform.localScale = new Vector3(0.24f, 0.24f, 1f);

            StationPlaque(station.transform, plaque);
            return station;
        }

        static CookStation Fryer(Transform parent, BusinessSpec spec, string name, Vector3 pos, string plaque)
        {
            var station = NewStation<CookStation>(parent, name, "Penggorengan", House("fryer"), pos, spec.stationScale);

            var origin = Go("SteamOrigin", station.transform, new Vector3(0f, 0.85f, 0f)).transform;
            SetField(station, "steamOrigin", origin);

            var glow = Sr("Sizzle", station.transform, Ui("glow"), new Vector3(0f, 0.48f, 0f), 6);
            glow.transform.localScale = new Vector3(0.7f, 0.45f, 1f);
            glow.color = new Color(1f, 0.62f, 0.2f, 0.55f);
            glow.gameObject.SetActive(false);
            SetField(station, "sizzleEffect", glow.gameObject);

            StationPlaque(station.transform, plaque);
            return station;
        }

        static PrepStation BuildCobek(Transform parent, BusinessSpec spec, string name, Vector3 pos, string plaque)
        {
            var station = NewStation<PrepStation>(parent, name, "Cobek Geprek", House("cobek_station"), pos, spec.stationScale);
            var pestle = Sr("Pestle", station.transform, Food("cobek_small"), new Vector3(0.05f, 0.55f, 0f));
            pestle.transform.localScale = new Vector3(0.36f, 0.36f, 1f);
            SetField(station, "pestle", pestle.transform);
            StationPlaque(station.transform, plaque);
            return station;
        }

        static PlateStation BuildPlate(Transform parent, BusinessSpec spec, string name, Vector3 pos)
        {
            var station = NewStation<PlateStation>(parent, name, "Meja Penyajian",
                                                   House("warming_table"), pos, 1.0f, new Vector2(2.2f, 0.30f));

            var dish = Sr("Dish", station.transform, null, new Vector3(0f, 0.88f, 0f));
            dish.transform.localScale = new Vector3(0.42f, 0.42f, 1f);
            dish.enabled = false;

            var slots = new SpriteRenderer[4];
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = Sr($"Slot{i}", station.transform, null, new Vector3(-0.42f + i * 0.28f, 0.84f, 0f));
                slots[i].transform.localScale = new Vector3(0.21f, 0.21f, 1f);
                slots[i].enabled = false;
            }

            SetField(station, "matchedDishIcon", dish);
            SetField(station, "componentSlots", slots);

            Deco(station.transform, "OrderRail", Env("order_rail"), new Vector3(0f, 1.62f, 0f), 0.55f, DecorOrder);
            StationPlaque(station.transform, "MEJA SAJI", -0.40f);
            return station;
        }

        static CashierStation BuildCashier(Transform parent, BusinessSpec spec, Vector3 pos)
        {
            var station = NewStation<CashierStation>(parent, "Kasir", "Kasir",
                                                     House("counter"), pos, 0.95f, new Vector2(1.2f, 0.30f));
            SetField(station, "reportIcon", Ico("report"));

            var register = Sr("Register", station.transform,
                              House(spec.modernDecor ? "pos_terminal" : "cash_register"), new Vector3(0f, 0.55f, 0f));
            register.transform.localScale = new Vector3(0.62f, 0.62f, 1f);

            StationPlaque(station.transform, "KASIR", -0.40f);
            return station;
        }

        /// <summary>
        /// Detail non-interaktif di lantai dapur (di celah antara sampah, meja saji,
        /// dan kasir): meja prep, area cuci piring, dan perlengkapan makan. Bukan
        /// stasiun baru -- cuma supaya tahapan siapkan -> masak -> saji -> cuci
        /// terbaca, sesuai masukan bahwa dapurnya kurang terasa alurnya.
        /// </summary>
        static void BuildKitchenFlavor(Transform kitchen, BusinessSpec spec, Layout L, float[] zoneEdges)
        {
            float trashX = zoneEdges[1] + 0.4f;
            float kasirX = spec.max.x - 2.2f;

            // posisi meja saji PERTAMA dan TERAKHIR, dihitung dengan rumus yang sama
            // persis dengan BuildKitchen -- lokasi berplate 1 (Warung/Ruko) punya celah
            // besar di sisi sampah, tapi lokasi berplate 2 (Restoran) meja saji pertamanya
            // duduk dekat sekali dengan sampah, jadi celahnya nyaris tidak ada di sana.
            // Taruh dekorasi baru relatif terhadap celah yang BENAR-BENAR ada di tiap
            // lokasi, bukan jarak tetap, supaya tidak ikut menabrak papan nama meja saji.
            float islandSpan = (spec.max.x - spec.min.x) * 0.5f;
            float firstPlateX = spec.plates == 1
                ? spec.min.x + (spec.max.x - spec.min.x) * 0.66f
                : spec.min.x + islandSpan * 0.55f;
            float lastPlateX = spec.plates == 1
                ? firstPlateX
                : spec.min.x + islandSpan * 0.55f + (spec.plates - 1) * islandSpan * 0.8f;

            float earlyGap = firstPlateX - trashX;
            float lateGap = kasirX - lastPlateX;

            var prep = Go("MejaPrep", kitchen, new Vector3(trashX + earlyGap * 0.4f, L.islandY, 0f)).transform;
            var prepTable = Sr("Meja", prep, House("prep_table"), Vector3.zero);
            prepTable.transform.localScale = new Vector3(0.85f, 0.85f, 1f);
            var board = Sr("Talenan", prep, House("cutting_board"), new Vector3(0f, 0.40f, 0f));
            board.transform.localScale = new Vector3(0.46f, 0.46f, 1f);
            var sortPrep = prep.gameObject.AddComponent<SortingByY>();
            SetField(sortPrep, "renderers", new[] { prepTable, board });
            StationPlaque(prep, "PREP", -0.42f);

            var wash = Go("AreaCuci", kitchen, new Vector3(lastPlateX + lateGap * 0.5f, L.islandY, 0f)).transform;
            var sink = Sr("Wastafel", wash, House("sink_double"), Vector3.zero);
            sink.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
            var dirty = Sr("PiringKotor", wash, Env("plates_dirty"), new Vector3(-0.5f, 0.28f, 0f));
            dirty.transform.localScale = new Vector3(0.38f, 0.38f, 1f);
            var clean = Sr("PiringBersih", wash, Env("plates_clean"), new Vector3(0.5f, 0.28f, 0f));
            clean.transform.localScale = new Vector3(0.38f, 0.38f, 1f);
            var sortWash = wash.gameObject.AddComponent<SortingByY>();
            SetField(sortWash, "renderers", new[] { sink, dirty, clean });
            StationPlaque(wash, "CUCI PIRING", -0.42f);

            var cutlery = Go("AlatMakan", kitchen, new Vector3(lastPlateX + lateGap * 0.80f, L.islandY, 0f)).transform;
            var plate = Sr("PiringKosong", cutlery, Food("plate_empty"), new Vector3(-0.18f, 0f, 0f));
            plate.transform.localScale = new Vector3(0.40f, 0.40f, 1f);
            var spoon = Sr("Sendok", cutlery, Food("cutlery"), new Vector3(0.26f, 0.05f, 0f));
            spoon.transform.localScale = new Vector3(0.30f, 0.30f, 1f);
            var sortCutlery = cutlery.gameObject.AddComponent<SortingByY>();
            SetField(sortCutlery, "renderers", new[] { plate, spoon });

            var kasirDeco = Go("DetailKasir", kitchen, new Vector3(kasirX + 0.85f, L.islandY, 0f)).transform;
            var clipboard = Sr("Nota", kasirDeco, Food("clipboard"), new Vector3(0f, 0.15f, 0f));
            clipboard.transform.localScale = new Vector3(0.26f, 0.26f, 1f);
            var coins = Sr("Receh", kasirDeco, Food("coin_stack"), new Vector3(0f, -0.05f, 0f));
            coins.transform.localScale = new Vector3(0.22f, 0.22f, 1f);
            var sortKasir = kasirDeco.gameObject.AddComponent<SortingByY>();
            SetField(sortKasir, "renderers", new[] { clipboard, coins });
        }

        // ---------------------------------------------------------------- hiasan

        static SpriteRenderer Deco(Transform parent, string name, Sprite sprite, Vector3 pos,
                                   float scale = 1f, int order = int.MinValue)
        {
            var sr = Sr(name, parent, sprite, pos);
            sr.transform.localScale = new Vector3(scale, scale, 1f);
            if (order != int.MinValue)
            {
                sr.sortingOrder = order;
            }
            else
            {
                var sorting = sr.gameObject.AddComponent<SortingByY>();
                SetField(sorting, "renderers", new[] { sr });
            }
            return sr;
        }

        /// <summary>
        /// Dekorasi dinding dibuat mengikuti tiga zona dapur di bawahnya (rak bahan di
        /// atas BAHAN, cerobong di atas MASAK, rak pelengkap di atas SAMBAL) supaya
        /// dinding terasa menyatu dengan meja kerja, bukan ikon acak yang mengambang
        /// di tembok kosong.
        /// </summary>
        static void BuildWallDecor(Transform root, BusinessSpec spec, Layout L, float[] zoneEdges)
        {
            var decor = Go("WallDecor", root).transform;
            float left = spec.min.x, right = spec.max.x;
            float wallY = L.wallBandY;

            Deco(decor, "RakBahan", House("shelf_ingredients"),
                 new Vector3((zoneEdges[0] + zoneEdges[1]) * 0.5f, wallY + 0.05f, 0f), 0.62f, DecorOrder);

            // satu cerobong per ~dua alat masak, merata di atas zona MASAK -- dapur
            // yang lebih besar (Ruko/Restoran) otomatis dapat cerobong lebih banyak
            int hoods = Mathf.Max(1, Mathf.RoundToInt((spec.TotalFryers + spec.cobeks) / 2f));
            var hoodX = SlotsIn(zoneEdges[1], zoneEdges[2], hoods);
            foreach (var x in hoodX)
                Deco(decor, "Hood", House("range_hood"), new Vector3(x, wallY + 0.05f, 0f), 0.68f, DecorOrder);

            Deco(decor, "RakPelengkap", Env("pantry_shelf"),
                 new Vector3(zoneEdges[2] + (zoneEdges[3] - zoneEdges[2]) * 0.32f, wallY + 0.03f, 0f), 0.62f, DecorOrder);

            Deco(decor, "Fan", House("fan"), new Vector3(right - 0.7f, wallY + 0.1f, 0f), 0.55f, DecorOrder);

            // identitas warung ditempel di sisi kiri dan kanan yang selalu terlihat
            Deco(decor, "BannerAyam", Env("sign_chicken"), new Vector3(left + 0.55f, L.islandY + 0.2f, 0f), 0.85f, DecorOrder + 10);
            Deco(decor, "JamDinding", House("wall_clock"), new Vector3(left + 0.6f, L.islandY + 1.4f, 0f), 0.55f, DecorOrder + 10);
            Deco(decor, "MenuBoard", Env("menu_board"), new Vector3(right - 0.75f, L.kitchenFloorBottom + 0.15f, 0f), 0.82f, DecorOrder + 10);
            Deco(decor, "Kalender", Env("calendar"), new Vector3(right - 0.65f, L.counterY + 0.55f, 0f), 0.48f, DecorOrder + 10);

            BuildSpicePoster(decor, new Vector3(right - 0.85f, L.islandY + 1.05f, 0f));

            // kenangan warung ibu -- selalu ada di setiap lokasi (dulu cuma muncul di
            // lokasi modern; besarnya sekarang tumbuh mengikuti kemewahan tempatnya)
            Deco(decor, "FotoKeluarga", Env("family_photo"),
                 new Vector3(left + 0.6f, wallY + 0.05f, 0f),
                 spec.modernDecor ? 0.62f : 0.42f, DecorOrder);

            if (spec.modernDecor)
                Deco(decor, "Trofi", Env("trophy"), new Vector3(left + 2.2f, L.islandY + 0.1f, 0f), 0.6f);
        }

        static void BuildSpicePoster(Transform parent, Vector3 pos)
        {
            var board = Sr("PosterPedas", parent, Ui("plaque"), pos, DecorOrder + 40);
            board.transform.localScale = new Vector3(0.88f, 1.30f, 1f);

            WorldLabel.Create(board.transform, "LEVEL PEDAS", new Vector3(0f, 0.06f, 0f), 0.024f,
                              new Color(0.99f, 0.95f, 0.87f), DecorOrder + 41);

            string[] chilis = { "spice_1", "spice_2", "spice_3" };
            for (int i = 0; i < chilis.Length; i++)
            {
                var c = Sr($"Cabai{i}", board.transform, Ico(chilis[i]),
                           new Vector3(-0.24f + i * 0.24f, -0.045f, 0f), DecorOrder + 42);
                c.transform.localScale = new Vector3(0.16f, 0.12f, 1f);
            }
        }

        static void BuildEntrance(Transform root, BusinessSpec spec, Layout L)
        {
            var entrance = Go("Entrance", root).transform;
            float x = spec.min.x;

            Deco(entrance, "Pintu", House(spec.modernDecor ? "door_glass" : "door_glass"),
                 new Vector3(x + 0.45f, L.laneY, 0f), 1.0f);
            Deco(entrance, "Keset", Env("rug_red"), new Vector3(x + 1.3f, L.laneY, 0f), 0.95f, FloorOrder + 6);
            Deco(entrance, "BannerBuka", Env("sign_red"), new Vector3(x + 0.6f, L.laneY + 1.55f, 0f), 0.7f, DecorOrder);

            var label = Sr("PapanMasuk", entrance, Ui("plaque"), new Vector3(x + 1.3f, L.laneY + 1.0f, 0f), DecorOrder + 40);
            label.transform.localScale = new Vector3(0.62f, 0.95f, 1f);
            WorldLabel.Create(label.transform, "MASUK", new Vector3(0f, 0.005f, 0f), 0.044f,
                              new Color(0.99f, 0.95f, 0.87f), DecorOrder + 41);

            Deco(entrance, "PapanMenuBerdiri", House("chalkboard"), new Vector3(x + 0.7f, spec.min.y + 1.7f, 0f), 0.8f);
        }

        static void BuildDiningDecor(Transform root, BusinessSpec spec, Layout L)
        {
            var decor = Go("DiningDecor", root).transform;
            float left = spec.min.x, right = spec.max.x, bottom = spec.min.y;

            Deco(decor, "Dispenser", House("dispenser"), new Vector3(right - 0.65f, L.laneY - 0.3f, 0f), 0.85f);
            Deco(decor, "RakMinum", House("display_cabinet"), new Vector3(right - 1.1f, L.kitchenFloorBottom - 0.3f, 0f), 0.78f);
            Deco(decor, "TempatSampahTamu", House("bin_green"), new Vector3(right - 0.65f, L.laneY - 1.65f, 0f), 0.7f);
            Deco(decor, "TanamanKanan", House("plant_big"), new Vector3(right - 1.0f, bottom + 0.3f, 0f), 0.85f);
            Deco(decor, "TanamanKiri", House("plant_big"), new Vector3(left + 0.8f, bottom + 0.3f, 0f), 0.85f);
            Deco(decor, "KardusSupplier", Food("veg_crate"), new Vector3(left + 1.7f, bottom + 0.35f, 0f), 0.65f);
            Deco(decor, "Trolley", Env("service_cart"), new Vector3(left + 1.3f, L.laneY + 0.4f, 0f), 0.72f);
            Deco(decor, "RakPiring", Env("dish_rack"), new Vector3(right - 1.6f, bottom + 0.35f, 0f), 0.7f);

            if (spec.modernDecor)
            {
                Deco(decor, "Locker", Env("locker"), new Vector3(left + 3.0f, bottom + 0.4f, 0f), 0.75f);
                Deco(decor, "Corkboard", Env("corkboard"), new Vector3(left + 4.4f, bottom + 0.4f, 0f), 0.7f);
            }
        }

        // ---------------------------------------------------------------- ruang makan

        static List<Seat> BuildDining(Transform root, BusinessSpec spec, Layout L)
        {
            var dining = Go("Dining", root).transform;
            var seats = new List<Seat>();

            const float margin = 1.4f;
            float availWidth = (spec.max.x - spec.min.x) - margin * 2f;
            int number = 1;
            var ordered = new List<Seat>();

            for (int row = 0; row < spec.tablesPerRow.Length; row++)
            {
                int count = spec.tablesPerRow[row];
                float spacing = availWidth / count;
                // baris kedua digeser setengah langkah supaya jalur turun tidak menembus meja depan
                float offset = row % 2 == 1 ? spacing * 0.5f : 0f;

                for (int i = 0; i < count; i++)
                {
                    float x = spec.min.x + margin + spacing * (i + 0.5f) + offset;
                    if (x > spec.max.x - margin * 0.5f) x -= spacing;

                    var seat = BuildTable(dining, spec, L, row, x, number++);
                    ordered.Add(seat);
                }
            }

            // kursi hasil upgrade diambil dari yang paling belakang
            for (int i = 0; i < ordered.Count; i++)
            {
                bool upgrade = i >= ordered.Count - spec.upgradeSeats;
                SetField(ordered[i], "isUpgradeSeat", upgrade);
                seats.Add(ordered[i]);
            }

            return seats;
        }

        static Seat BuildTable(Transform dining, BusinessSpec spec, Layout L, int row, float x, int number)
        {
            var group = Go($"Meja{number}", dining, new Vector3(x, L.tableY[row], 0f)).transform;

            var table = Sr("Table", group, House(number % 2 == 1 ? spec.tableA : spec.tableB), Vector3.zero);
            table.transform.localScale = new Vector3(spec.furnitureScale, spec.furnitureScale, 1f);

            var stoolL = Sr("StoolL", group, House(spec.stoolA), new Vector3(-0.66f, 0.12f, 0f));
            var stoolR = Sr("StoolR", group, House(spec.stoolB), new Vector3(0.66f, 0.12f, 0f));
            stoolL.transform.localScale = stoolR.transform.localScale =
                new Vector3(spec.furnitureScale * 0.8f, spec.furnitureScale * 0.8f, 1f);

            var tissue = Sr("Tisu", group, House("tissue_box"), new Vector3(0.26f, 0.52f, 0f));
            tissue.transform.localScale = new Vector3(0.28f, 0.28f, 1f);

            var numberPlate = Sr("NomorMeja", group, Ui("label_strip"), new Vector3(-0.28f, 0.56f, 0f));
            numberPlate.transform.localScale = new Vector3(0.22f, 0.62f, 1f);
            WorldLabel.Create(numberPlate.transform, number.ToString(), new Vector3(0f, 0.004f, 0f), 0.055f,
                              new Color(0.99f, 0.95f, 0.87f), 0);

            var sorting = group.gameObject.AddComponent<SortingByY>();
            SetField(sorting, "isStatic", true);
            SetField(sorting, "renderers", new[] { stoolL, stoolR, table, tissue, numberPlate });

            Box(group.gameObject, new Vector2(1.4f, 0.36f), new Vector2(0f, 0.18f));

            var seatGo = Go("Seat", group);
            var seat = seatGo.AddComponent<Seat>();
            var sit = Go("Sit", seatGo.transform, new Vector3(0f, L.seatY[row] - L.tableY[row], 0f)).transform;
            var approach = Go("Approach", seatGo.transform, new Vector3(0f, L.approachY[row] - L.tableY[row], 0f)).transform;

            SetField(seat, "sitPoint", sit);
            SetField(seat, "approachPoint", approach);

            var so = new SerializedObject(seat);
            var visuals = so.FindProperty("visuals");
            visuals.arraySize = 5;
            visuals.GetArrayElementAtIndex(0).objectReferenceValue = table.gameObject;
            visuals.GetArrayElementAtIndex(1).objectReferenceValue = stoolL.gameObject;
            visuals.GetArrayElementAtIndex(2).objectReferenceValue = stoolR.gameObject;
            visuals.GetArrayElementAtIndex(3).objectReferenceValue = tissue.gameObject;
            visuals.GetArrayElementAtIndex(4).objectReferenceValue = numberPlate.gameObject;
            so.ApplyModifiedPropertiesWithoutUndo();

            return seat;
        }

        // ---------------------------------------------------------------- pelanggan

        static CustomerSpawner BuildSpawner(Transform root, BusinessSpec spec, Layout L, List<Seat> seats)
        {
            var go = Go("CustomerSpawner", root);
            var spawner = go.AddComponent<CustomerSpawner>();

            float doorX = spec.min.x - 0.7f;
            var door = Go("DoorPoint", go.transform, new Vector3(doorX, L.laneY, 0f)).transform;
            var exit = Go("ExitPoint", go.transform, new Vector3(doorX - 0.5f, L.laneY, 0f)).transform;
            var parentGo = Go("Customers", go.transform).transform;

            int queueCount = Mathf.Clamp(spec.TotalTables / 2, 3, 5);
            var queue = new Transform[queueCount];
            for (int i = 0; i < queueCount; i++)
            {
                float x = spec.min.x + 2.2f + i * 0.85f;
                queue[i] = Go($"Queue{i}", go.transform, new Vector3(x, L.laneY, 0f)).transform;

                var mark = Sr($"QueueMark{i}", go.transform, Ui("floor_queue"), new Vector3(x, L.laneY - 0.06f, 0f),
                              FloorOrder + 7);
                mark.transform.localScale = new Vector3(0.48f, 0.36f, 1f);
                mark.color = new Color(1f, 1f, 1f, 0.55f);
            }

            // pembatas + papan kecil supaya jalur antrean kelihatan sebagai jalur,
            // bukan cuma pola lantai yang samar
            float queueStart = spec.min.x + 2.2f, queueEnd = queueStart + (queueCount - 1) * 0.85f;
            Deco(go.transform, "PembatasAwal", Env("bollard"), new Vector3(queueStart - 0.55f, L.laneY, 0f), 0.5f);
            Deco(go.transform, "PembatasAkhir", Env("bollard"), new Vector3(queueEnd + 0.55f, L.laneY, 0f), 0.5f);

            var antreSign = Sr("PapanAntre", go.transform, Ui("plaque"),
                               new Vector3(queueStart - 0.15f, L.laneY + 1.15f, 0f), DecorOrder + 40);
            antreSign.transform.localScale = new Vector3(0.62f, 0.6f, 1f);
            WorldLabel.Create(antreSign.transform, "ANTRE SINI", new Vector3(0f, 0.005f, 0f), 0.028f,
                              new Color(0.99f, 0.95f, 0.87f), DecorOrder + 41);

            SetField(spawner, "customerPrefab", CustomerPrefab);
            SetField(spawner, "doorPoint", door);
            SetField(spawner, "exitPoint", exit);
            SetField(spawner, "customerParent", parentGo);
            SetField(spawner, "laneY", L.laneY);
            SetField(spawner, "moodHappy", Ico("mood_happy"));
            SetField(spawner, "moodNeutral", Ico("mood_neutral"));
            SetField(spawner, "moodAngry", Ico("mood_angry"));
            SetField(spawner, "heartIcon", Ico("heart"));
            SetField(spawner, "coinIcon", Ico("coin"));

            var so = new SerializedObject(spawner);
            var seatList = so.FindProperty("seats");
            seatList.arraySize = seats.Count;
            for (int i = 0; i < seats.Count; i++)
                seatList.GetArrayElementAtIndex(i).objectReferenceValue = seats[i];

            var queueList = so.FindProperty("queueSpots");
            queueList.arraySize = queue.Length;
            for (int i = 0; i < queue.Length; i++)
                queueList.GetArrayElementAtIndex(i).objectReferenceValue = queue[i];

            so.ApplyModifiedPropertiesWithoutUndo();
            return spawner;
        }
    }
}
