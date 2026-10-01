using System.Collections.Generic;
using Geprek.Core;
using Geprek.Data;
using UnityEditor;
using UnityEngine;

namespace Geprek.EditorTools
{
    public static partial class GeprekBuilder
    {
        const string DataDir = Root + "/Data";
        public static GameDatabase Database { get; private set; }

        static readonly string[] Char1Rows =
            { "Student", "OfficeWoman", "Schoolboy", "HijabWoman", "Grandpa", "Grandma", "OjolRider", "Teen" };
        static readonly string[] Char2Rows =
            { "Waiter", "Mother", "Father", "Lecturer", "Chef", "Waitress", "Cashier", "Businesswoman" };
        static readonly string[] Char1Frames =
            { "down_0", "down_1", "left_0", "left_1", "right_0", "right_1", "up_0", "up_1" };
        static readonly string[] Char2Frames =
            { "down_0", "down_1", "down_2", "left_0", "left_1", "left_2",
              "right_0", "right_1", "right_2", "up_0", "up_1", "up_2" };

        public static void BuildData()
        {
            var db = Asset<GameDatabase>(DataDir, "GameDatabase");
            Database = db;

            db.config = BuildConfig();
            db.items = BuildItems();

            // lookup lokal: cache di GameDatabase belum tentu sudah menyegarkan diri
            var itemById = new Dictionary<string, ItemDef>();
            foreach (var it in db.items) itemById[it.id] = it;

            db.recipes = BuildRecipes(itemById);
            db.upgrades = BuildUpgrades();
            var skins = BuildSkins();
            db.playerSkin = skins["Student"];
            db.motherSkin = skins["Mother"];
            db.fatherSkin = skins["Father"];
            db.lecturerSkin = skins["Lecturer"];
            db.staffSkins = new List<CharacterSkin> { skins["Chef"], skins["Waitress"], skins["Waiter"], skins["Cashier"] };
            db.customerTypes = BuildCustomerTypes(skins);

            var recipeById = new Dictionary<string, RecipeDef>();
            foreach (var r in db.recipes) recipeById[r.id] = r;
            db.arcs = BuildArcs(recipeById);

            EditorUtility.SetDirty(db);
        }

        // ---------------------------------------------------------------- config

        static GameConfig BuildConfig()
        {
            var c = Asset<GameConfig>(DataDir + "/Config", "GameConfig");
            c.dayLengthSeconds = 200f;
            c.openHour = 9; c.closeHour = 17;
            c.closingGraceSeconds = 40f;
            c.baseMoveSpeed = 4.4f;
            c.interactRadius = 1.25f;
            c.basePatience = 36f;
            c.eatDuration = 3.5f;
            c.maxQueue = 3;
            c.perfectThreshold = 0.6f;
            c.goodThreshold = 0.25f;
            c.perfectPayBonus = 1.35f;
            c.goodPayBonus = 1f;
            c.latePayBonus = 0.7f;
            c.reputationLossPerAbandon = 0.035f;
            c.reputationGainPerPerfect = 0.02f;
            c.startingMoney = 50000;
            c.dailyIngredientCost = 8000;
            c.xpForLevel2 = 55;
            c.xpGrowth = 1.4f;
            c.maxLevel = 12;
            c.promoCost = 12000;
            c.promoReputationGain = 0.12f;
            c.reputationToCustomerBonus = 0.55f;
            EditorUtility.SetDirty(c);
            return c;
        }

        // ---------------------------------------------------------------- bahan

        static ItemDef Item(string id, string label, Sprite icon, ItemStage stage, bool plating)
        {
            var it = Asset<ItemDef>(DataDir + "/Ingredients", "Item_" + id);
            it.id = id; it.displayName = label; it.icon = icon;
            it.stage = stage; it.isPlatingComponent = plating;
            it.cookResult = null; it.prepResult = null; it.burnResult = null;
            EditorUtility.SetDirty(it);
            return it;
        }

        static List<ItemDef> BuildItems()
        {
            var raw    = Item("ayam_mentah", "Ayam Mentah", Food("chicken_raw"), ItemStage.Raw, false);
            var fried  = Item("ayam_goreng", "Ayam Goreng", Food("chicken_fried"), ItemStage.Cooked, false);
            var geprek = Item("ayam_geprek", "Ayam Geprek", Food("chicken_geprek"), ItemStage.Prepped, true);
            var burnt  = Item("ayam_gosong", "Ayam Gosong", Ui("chicken_burnt"), ItemStage.Burnt, false);

            raw.cookResult = fried; raw.burnResult = burnt;
            raw.cookTime = 5f; raw.burnGrace = 7f;
            fried.prepResult = geprek; fried.prepTime = 2.2f;
            EditorUtility.SetDirty(raw); EditorUtility.SetDirty(fried);

            var nasi    = Item("nasi", "Nasi Putih", Food("rice_scoop"), ItemStage.Prepped, true);
            var sMerah  = Item("sambal_merah", "Sambal Merah", Food("sambal_merah"), ItemStage.Prepped, true);
            var sIjo    = Item("sambal_ijo", "Sambal Ijo", Food("sambal_ijo"), ItemStage.Prepped, true);
            var telur   = Item("telur", "Telur Ceplok", Food("telur_ceplok"), ItemStage.Prepped, true);
            var tahu    = Item("tahu_tempe", "Tahu & Tempe", Food("tahu"), ItemStage.Prepped, true);
            var lalapan = Item("lalapan", "Lalapan", Food("timun_iris"), ItemStage.Prepped, true);
            var keju    = Item("keju", "Keju Parut", Food("kerupuk"), ItemStage.Prepped, true);

            return new List<ItemDef> { raw, fried, geprek, burnt, nasi, sMerah, sIjo, telur, tahu, lalapan, keju };
        }

        // ---------------------------------------------------------------- resep

        static RecipeDef Recipe(Dictionary<string, ItemDef> items, string id, string label, string desc, Sprite icon,
                                int price, int xp, int level, int arc, int cost, bool fromParents,
                                float patienceMul, params string[] componentIds)
        {
            var r = Asset<RecipeDef>(DataDir + "/Recipes", "Recipe_" + id);
            r.id = id; r.displayName = label; r.description = desc; r.icon = icon;
            r.basePrice = price; r.xpReward = xp;
            r.unlockLevel = level; r.unlockArc = arc; r.unlockCost = cost; r.fromParents = fromParents;
            r.patienceMultiplier = patienceMul;

            r.components = new List<ItemDef>();
            foreach (var cid in componentIds)
            {
                if (!items.TryGetValue(cid, out var item) || item == null)
                    Debug.LogWarning($"[Geprek] Bahan '{cid}' tidak ketemu untuk resep {id}");
                else r.components.Add(item);
            }
            EditorUtility.SetDirty(r);
            return r;
        }

        static List<RecipeDef> BuildRecipes(Dictionary<string, ItemDef> db)
        {
            return new List<RecipeDef>
            {
                Recipe(db, "geprek_original", "Geprek Original",
                    "Ayam geprek polos dengan nasi hangat. Menu andalan warung sejak hari pertama.",
                    Food("dish_original"), 15000, 8, 1, 1, 0, false, 1.15f, "ayam_geprek", "nasi"),

                Recipe(db, "geprek_sambal_merah", "Geprek Sambal Merah",
                    "Diguyur sambal bawang merah yang pedasnya nampol.",
                    Food("dish_sambal_merah"), 18000, 10, 1, 1, 0, false, 1.05f, "ayam_geprek", "nasi", "sambal_merah"),

                Recipe(db, "geprek_telur", "Geprek Telur Ceplok",
                    "Tambahan telur ceplok setengah matang, favorit anak kos.",
                    Food("dish_telur"), 22000, 13, 2, 1, 30000, false, 1f, "ayam_geprek", "nasi", "telur"),

                Recipe(db, "geprek_lalapan", "Geprek Lalapan",
                    "Disajikan dengan timun dan kol segar biar tidak eneg.",
                    Food("dish_lalapan"), 24000, 15, 3, 1, 45000, false, 1f, "ayam_geprek", "nasi", "lalapan"),

                Recipe(db, "geprek_sambal_ijo", "Geprek Sambal Ijo",
                    "Resep sambal ijo warisan Ibu. Pedasnya halus tapi bikin nagih.",
                    Food("dish_sambal_ijo"), 26000, 17, 4, 1, 0, true, 1f, "ayam_geprek", "nasi", "sambal_ijo"),

                Recipe(db, "geprek_tahu_tempe", "Geprek Tahu Tempe",
                    "Porsi lebih mengenyangkan dengan tahu dan tempe goreng.",
                    Food("dish_tahu"), 27000, 18, 5, 2, 70000, false, 0.95f, "ayam_geprek", "nasi", "tahu_tempe"),

                Recipe(db, "geprek_keju", "Geprek Keju Leleh",
                    "Ayam geprek bertabur keju leleh. Menu yang paling sering difoto pelanggan.",
                    Food("dish_mozzarella"), 32000, 22, 6, 2, 110000, false, 0.9f, "ayam_geprek", "nasi", "keju"),

                Recipe(db, "geprek_komplit", "Geprek Komplit",
                    "Sambal merah plus telur ceplok. Menu penutup arc, paling mahal dan paling ribet.",
                    Food("dish_komplit"), 38000, 28, 7, 2, 0, true, 0.85f, "ayam_geprek", "nasi", "sambal_merah", "telur"),
            };
        }

        // ---------------------------------------------------------------- upgrade

        static UpgradeDef Upgrade(string id, string label, string desc, Sprite icon, UpgradeKind kind,
                                  int baseCost, float growth, float value, int maxLevel, int arc)
        {
            var u = Asset<UpgradeDef>(DataDir + "/Upgrades", "Upgrade_" + id);
            u.id = id; u.displayName = label; u.description = desc; u.icon = icon;
            u.kind = kind; u.baseCost = baseCost; u.costGrowth = growth;
            u.valuePerLevel = value; u.maxLevel = maxLevel; u.unlockArc = arc;
            EditorUtility.SetDirty(u);
            return u;
        }

        static List<UpgradeDef> BuildUpgrades() => new()
        {
            Upgrade("sepatu", "Sepatu Kerja", "Jalan lebih cepat di dalam warung. +12% kecepatan per tingkat.",
                    Ico("growth"), UpgradeKind.MoveSpeed, 25000, 1.7f, 0.12f, 3, 1),

            Upgrade("kompor", "Kompor Tekanan Tinggi", "Menggoreng jadi lebih cepat. +15% per tingkat.",
                    House("stove_gas"), UpgradeKind.CookSpeed, 35000, 1.7f, 0.15f, 3, 1),

            Upgrade("ulekan", "Ulekan Batu Besar", "Menggeprek jadi lebih cepat. +20% per tingkat.",
                    House("cobek_station"), UpgradeKind.PrepSpeed, 30000, 1.7f, 0.20f, 3, 1),

            Upgrade("penggorengan2", "Penggorengan Kedua", "Buka satu penggorengan lagi supaya bisa masak paralel.",
                    House("fryer"), UpgradeKind.ExtraFryer, 60000, 2f, 1f, 1, 1),

            Upgrade("kursi", "Tambah Meja Pelanggan", "Satu meja tambahan, antrean jadi lebih lancar.",
                    House("table_wood"), UpgradeKind.ExtraSeat, 55000, 1.9f, 1f, 2, 1),

            Upgrade("kipas", "Kipas & Kursi Empuk", "Pelanggan betah menunggu. +10% kesabaran per tingkat.",
                    House("fan"), UpgradeKind.PatienceBonus, 40000, 1.8f, 0.10f, 3, 1),

            Upgrade("branding", "Branding Warung", "Spanduk dan kemasan rapi menaikkan harga jual. +8% per tingkat.",
                    Env("sign_chicken"), UpgradeKind.PriceBonus, 70000, 1.8f, 0.08f, 3, 1),

        };

        // ---------------------------------------------------------------- skin karakter

        static Dictionary<string, CharacterSkin> BuildSkins()
        {
            var map = new Dictionary<string, CharacterSkin>();

            for (int i = 0; i < Char1Rows.Length; i++)
                map[Char1Rows[i]] = Skin("Character 1", Char1Rows[i], Char1Frames, 2);

            for (int i = 0; i < Char2Rows.Length; i++)
                map[Char2Rows[i]] = Skin("Character 2", Char2Rows[i], Char2Frames, 3);

            return map;
        }

        static CharacterSkin Skin(string sheet, string row, string[] frames, int perDirection)
        {
            var skin = Asset<CharacterSkin>(DataDir + "/Customers", "Skin_" + row);
            skin.displayName = row;
            skin.framesPerDirection = perDirection;
            skin.frames = new Sprite[frames.Length];
            for (int i = 0; i < frames.Length; i++)
                skin.frames[i] = Spr(sheet, $"{row}_{frames[i]}");
            EditorUtility.SetDirty(skin);
            return skin;
        }

        // ---------------------------------------------------------------- pelanggan

        static CustomerTypeDef Customer(Dictionary<string, CharacterSkin> skins, string skinKey,
                                        string id, string label, float patience, float pay, float weight, int minArc,
                                        bool special = false)
        {
            var c = Asset<CustomerTypeDef>(DataDir + "/Customers", "Customer_" + id);
            c.id = id; c.displayName = label;
            c.skin = skins.TryGetValue(skinKey, out var s) ? s : null;
            c.patienceMultiplier = patience; c.payMultiplier = pay;
            c.spawnWeight = weight; c.minArc = minArc;
            c.isSpecialGuest = special;
            c.specialReputationGain = 0.12f;
            c.specialReputationLoss = 0.08f;
            EditorUtility.SetDirty(c);
            return c;
        }

        static List<CustomerTypeDef> BuildCustomerTypes(Dictionary<string, CharacterSkin> skins) => new()
        {
            Customer(skins, "Schoolboy",   "anak_sekolah", "Anak Sekolah", 1.15f, 0.85f, 1.3f, 1),
            Customer(skins, "Teen",        "remaja",       "Remaja",       0.95f, 0.95f, 1.2f, 1),
            Customer(skins, "HijabWoman",  "ibu_muda",     "Ibu Muda",     1.10f, 1.05f, 1.0f, 1),
            Customer(skins, "OjolRider",   "driver_ojol",  "Driver Ojol",  0.65f, 1.20f, 1.1f, 1),
            Customer(skins, "OfficeWoman", "karyawan",     "Karyawan",     0.85f, 1.25f, 1.0f, 1),
            Customer(skins, "Grandpa",     "kakek",        "Kakek",        1.45f, 0.95f, 0.6f, 1),
            Customer(skins, "Grandma",     "nenek",        "Nenek",        1.45f, 0.95f, 0.6f, 1),
            Customer(skins, "Lecturer",    "dosen",        "Dosen",        1.00f, 1.40f, 0.6f, 2),

            // tamu istimewa: tidak muncul acak, hanya kalau diundang lewat media sosial
            Customer(skins, "Businesswoman", "food_vlogger", "Food Vlogger", 0.75f, 1.80f, 0f, 1, special: true),
        };

        // ---------------------------------------------------------------- hari & arc

        static DayPlanDef Day(int arc, int index, string title, string brief, int target,
                              int customers, float startInterval, float endInterval,
                              float patienceMul, int maxComplexity,
                              params DayObjective[] objectives)
        {
            var d = Asset<DayPlanDef>(DataDir + "/Levels", $"Day_A{arc}_{index}");
            d.dayInArc = index; d.title = title; d.briefing = brief;
            d.targetRevenue = target; d.totalCustomers = customers;
            d.spawnIntervalStart = startInterval; d.spawnIntervalEnd = endInterval;
            d.patienceMultiplier = patienceMul; d.maxRecipeComplexity = maxComplexity;

            d.objectives = new List<DayObjective>(objectives ?? System.Array.Empty<DayObjective>());

            // jam sibuk makin panjang dan makin padat di hari-hari akhir arc
            d.hasLunchRush = index >= 2 || arc > 1;
            d.rushStart = Mathf.Lerp(0.42f, 0.32f, (index - 1) / 4f);
            d.rushEnd = Mathf.Lerp(0.60f, 0.70f, (index - 1) / 4f);
            d.rushSpawnScale = Mathf.Lerp(0.62f, 0.45f, (index - 1) / 4f);
            d.rushPatienceScale = Mathf.Lerp(0.95f, 0.86f, (index - 1) / 4f);

            EditorUtility.SetDirty(d);
            return d;
        }

        /// <summary>Pintasan membuat target harian.</summary>
        static DayObjective Obj(ObjectiveKind kind, int amount, RecipeDef recipe = null) =>
            new() { kind = kind, amount = amount, recipe = recipe };

        static List<ArcDef> BuildArcs(Dictionary<string, RecipeDef> recipes)
        {
            RecipeDef R(string id) => recipes.TryGetValue(id, out var r) ? r : null;

            var arc1 = Asset<ArcDef>(DataDir + "/Levels", "Arc_1");
            arc1.arcNumber = 1;
            arc1.title = "Arc 1: Perintis";
            arc1.intro = "Warung ibu jadi tempat usaha pertamamu. Semua dikerjakan sendiri.";
            arc1.outro = "Omzetmu sudah cukup untuk menyewa ruko. Saatnya naik kelas.";
            arc1.businessLocation = LocationId.Warung;
            arc1.arcRevenueTarget = 420000;
            arc1.minSatisfaction = 0.45f;
            arc1.days = new List<DayPlanDef>
            {
                Day(1, 1, "Hari Pertama", "Buka warung ibu. Kenali dulu alurnya: ambil ayam, goreng, geprek, susun, antar.", 55000, 7, 11f, 8f, 1.3f, 2,
                    Obj(ObjectiveKind.ServeCustomers, 5)),
                Day(1, 2, "Mulai Dikenal", "Beberapa tetangga mulai penasaran. Layani tanpa ada yang kabur.", 80000, 9, 10f, 7f, 1.2f, 3,
                    Obj(ObjectiveKind.MaxAbandon, 1)),
                Day(1, 3, "Jam Makan Siang", "Antrean mulai muncul di jam sibuk. Atur urutan kerjamu.", 110000, 12, 9f, 6f, 1.1f, 3,
                    Obj(ObjectiveKind.PerfectOrders, 4)),
                Day(1, 4, "Ramai Betul", "Warung makin padat. Pertimbangkan tambah meja atau penggorengan.", 140000, 14, 8f, 5f, 1f, 3,
                    Obj(ObjectiveKind.Satisfaction, 65)),
                Day(1, 5, "Ujian Pertama", "Hari terakhir sebelum lapor ke dosen. Kejar target arc.", 170000, 17, 7f, 4.5f, 1f, 4,
                    Obj(ObjectiveKind.ServeCustomers, 14),
                    Obj(ObjectiveKind.SellRecipe, 5, R("geprek_sambal_merah"))),
            };
            EditorUtility.SetDirty(arc1);

            var arc2 = Asset<ArcDef>(DataDir + "/Levels", "Arc_2");
            arc2.arcNumber = 2;
            arc2.title = "Arc 2: Pengembangan";
            arc2.intro = "Warung pindah ke ruko. Kapasitas bertambah dan kamu bisa merekrut karyawan.";
            arc2.outro = "Bisnismu stabil. Sudah waktunya memikirkan cabang.";
            arc2.businessLocation = LocationId.Ruko;
            arc2.arcRevenueTarget = 1150000;
            arc2.minSatisfaction = 0.5f;
            arc2.days = new List<DayPlanDef>
            {
                Day(2, 1, "Pindah ke Ruko", "Tempat baru, pelanggan baru. Menu premium mulai laku.", 180000, 18, 7f, 5f, 1.1f, 0,
                    Obj(ObjectiveKind.ServeCustomers, 15)),
                Day(2, 2, "Karyawan Pertama", "Rekrut karyawan di toko supaya pengantaran tidak menumpuk.", 210000, 20, 6.5f, 4.5f, 1.05f, 0,
                    Obj(ObjectiveKind.SellRecipe, 6, R("geprek_telur"))),
                Day(2, 3, "Jam Sibuk Ganda", "Dua gelombang pelanggan dalam sehari.", 245000, 23, 6f, 4f, 1f, 0,
                    Obj(ObjectiveKind.Satisfaction, 70)),
                Day(2, 4, "Pelanggan Rewel", "Pengusaha dan dosen mulai mampir. Mereka bayar mahal tapi tidak sabar.", 280000, 25, 5.5f, 3.8f, 0.95f, 0,
                    Obj(ObjectiveKind.MaxAbandon, 3),
                    Obj(ObjectiveKind.PerfectOrders, 10)),
                Day(2, 5, "Evaluasi Ruko", "Hari terakhir arc 2. Jaga kepuasan pelanggan.", 320000, 28, 5f, 3.5f, 0.95f, 0,
                    Obj(ObjectiveKind.ServeCustomers, 24),
                    Obj(ObjectiveKind.Satisfaction, 72)),
            };
            EditorUtility.SetDirty(arc2);

            var arc3 = Asset<ArcDef>(DataDir + "/Levels", "Arc_3");
            arc3.arcNumber = 3;
            arc3.title = "Arc 3: Restoran";
            arc3.intro = "Restoran penuh sesak. Fokusmu sekarang menjaga ritme dan mengatur karyawan.";
            arc3.outro = "Tugas kuliahmu selesai dengan bisnis yang benar-benar jalan.";
            arc3.businessLocation = LocationId.Restaurant;
            arc3.arcRevenueTarget = 2200000;
            arc3.minSatisfaction = 0.55f;
            arc3.days = new List<DayPlanDef>
            {
                Day(3, 1, "Buka Restoran", "Skala baru, ritme baru.", 360000, 30, 5f, 3.4f, 1f, 0,
                    Obj(ObjectiveKind.ServeCustomers, 26)),
                Day(3, 2, "Ulasan Viral", "Postingan pelangganmu viral. Bersiaplah.", 410000, 33, 4.6f, 3.1f, 0.95f, 0,
                    Obj(ObjectiveKind.PerfectOrders, 15)),
                Day(3, 3, "Akhir Pekan", "Hari terpadat dalam seminggu.", 460000, 36, 4.2f, 2.9f, 0.95f, 0,
                    Obj(ObjectiveKind.Satisfaction, 75)),
                Day(3, 4, "Cabang Kedua", "Kamu mulai memantau dua dapur sekaligus.", 520000, 39, 4f, 2.7f, 0.9f, 0,
                    Obj(ObjectiveKind.MaxAbandon, 3),
                    Obj(ObjectiveKind.SellRecipe, 8, R("geprek_komplit"))),
                Day(3, 5, "Presentasi Akhir", "Hari terakhir. Tunjukkan bisnismu benar-benar berjalan.", 600000, 42, 3.8f, 2.5f, 0.9f, 0,
                    Obj(ObjectiveKind.ServeCustomers, 36),
                    Obj(ObjectiveKind.Satisfaction, 78)),
            };
            EditorUtility.SetDirty(arc3);

            return new List<ArcDef> { arc1, arc2, arc3 };
        }
    }
}
