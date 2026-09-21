using Geprek.Core;
using Geprek.Customers;
using Geprek.Progression;
using Geprek.Stations;
using Geprek.UI;
using Geprek.World;
using UnityEditor;
using UnityEngine;

namespace Geprek.EditorTools
{
    public static partial class GeprekBuilder
    {
        static GameObject BuildHome(Transform parent, out NightManager nightManager)
        {
            var root = Go("Home", parent).transform;

            BuildHomeFloor(root);
            BuildHomeDecor(root);

            nightManager = Go("NightManager", root).AddComponent<NightManager>();
            SetField(nightManager, "motherIcon", Ico("customer"));
            SetField(nightManager, "fatherIcon", Ico("staff"));
            SetField(nightManager, "phoneIcon", Ico("social"));
            SetField(nightManager, "fatherGift", 25000);

            BuildHomeStations(root, nightManager);
            return root.gameObject;
        }

        static void BuildHomeFloor(Transform root)
        {
            var floors = Go("Floor", root).transform;

            // ruangan lebih kecil dari layar, jadi lantai sengaja dilebihkan
            // melewati batas tabrakan supaya tepi layar tidak memperlihatkan latar kosong
            const float overscanX = 3.2f, overscanY = 2.4f;
            float w = HomeMax.x - HomeMin.x + overscanX * 2f;

            const float homeWallY = 2.55f;

            Tiled("FloorTerrazzo", floors, Ui("floor_terrazzo"),
                  new Vector3(0f, (HomeMin.y - overscanY + homeWallY) * 0.5f, 0f),
                  new Vector2(w, homeWallY - HomeMin.y + overscanY), FloorOrder);

            Tiled("WallBack", floors, Ui("wall_plaster"),
                  new Vector3(0f, (homeWallY + HomeMax.y + overscanY) * 0.5f, 0f),
                  new Vector2(w, HomeMax.y - homeWallY + overscanY), WallOrder);

            var walls = Go("Walls", root).transform;
            float h = HomeMax.y - HomeMin.y;
            Wall(walls, "Top", new Vector2(0f, 2.6f), new Vector2(w, 0.4f));
            Wall(walls, "Bottom", new Vector2(0f, HomeMin.y), new Vector2(w, 0.4f));
            Wall(walls, "Left", new Vector2(HomeMin.x, 0f), new Vector2(0.4f, h));
            Wall(walls, "Right", new Vector2(HomeMax.x, 0f), new Vector2(0.4f, h));
        }

        static void BuildHomeDecor(Transform root)
        {
            var decor = Go("Decor", root).transform;

            Deco(decor, "FamilyPhoto", Env("family_photo"), new Vector3(-2.2f, 2.85f, 0f), 0.85f, DecorOrder);
            Deco(decor, "Calendar", Env("calendar"), new Vector3(2.4f, 2.85f, 0f), 0.75f, DecorOrder);
            Deco(decor, "Mirror", Env("mirror"), new Vector3(4.6f, 2.8f, 0f), 0.8f, DecorOrder);

            Deco(decor, "Rug", Env("rug_teal"), new Vector3(0f, -1.2f, 0f), 1.2f, FloorOrder + 5);
            Deco(decor, "Sofa", House("sofa_brown"), new Vector3(0f, -0.35f, 0f), 1f);
            Deco(decor, "CoffeeTable", Env("coffee_table"), new Vector3(0f, -1.85f, 0f), 0.9f);
            Deco(decor, "Tv", Env("tv"), new Vector3(-2.9f, -2.6f, 0f), 0.95f);
            Deco(decor, "Bookshelf", House("bookshelf"), new Vector3(3.4f, 2.35f, 0f), 0.95f);
            Deco(decor, "Plant", House("plant_big"), new Vector3(-5.5f, -2.3f, 0f), 1f);
            Deco(decor, "Slippers", Env("slippers"), new Vector3(5.2f, -2.6f, 0f), 0.6f);
            Deco(decor, "Lamp", House("floor_lamp"), new Vector3(5.5f, 0.9f, 0f), 0.95f);
            Deco(decor, "Door", House("door_wood"), new Vector3(-6.0f, 0.2f, 0f), 1f, DecorOrder);
        }

        static void BuildHomeStations(Transform root, NightManager night)
        {
            var stations = Go("NightStations", root).transform;

            Night(stations, night, NightAction.Sleep, "Kasur", "Tidur", House("bed"),
                  new Vector3(-4.3f, 1.85f, 0f), 1.05f);

            var mother = Night(stations, night, NightAction.TalkMother, "Ibu", "Ngobrol dengan Ibu",
                               Database.motherSkin != null ? Database.motherSkin.GetIdle(Facing.Down) : null,
                               new Vector3(-1.4f, 1.95f, 0f), 1f);
            AddPersonShadow(mother.transform);

            var father = Night(stations, night, NightAction.TalkFather, "Ayah", "Ngobrol dengan Ayah",
                               Database.fatherSkin != null ? Database.fatherSkin.GetIdle(Facing.Down) : null,
                               new Vector3(0.6f, 1.95f, 0f), 1f);
            AddPersonShadow(father.transform);

            Night(stations, night, NightAction.Shop, "MejaBelajar", "Buka toko & upgrade",
                  Env("desk_study"), new Vector3(2.6f, 0.4f, 0f), 1f);

            Night(stations, night, NightAction.SocialMedia, "Handphone", "Pasang promosi medsos",
                  Env("laptop_chart"), new Vector3(-3.2f, 0.3f, 0f), 0.9f);

            Night(stations, night, NightAction.Branches, "MonitorCabang", "Pantau cabang",
                  Env("monitor_branch"), new Vector3(4.6f, 0.4f, 0f), 0.9f);
        }

        static NightStation Night(Transform parent, NightManager manager, NightAction action,
                                  string name, string hint, Sprite body, Vector3 pos, float scale)
        {
            var station = NewStation<NightStation>(parent, name, name, body, pos, scale, new Vector2(1.0f, 0.5f));
            SetField(station, "nightManager", manager);
            SetField(station, "action", (int)action);
            SetField(station, "hintText", hint);
            return station;
        }

        static void AddPersonShadow(Transform person)
        {
            var shadow = Sr("Shadow", person, Ui("shadow_blob"), new Vector3(0f, 0.06f, 0f));
            shadow.color = new Color(1f, 1f, 1f, 0.5f);
            shadow.transform.localScale = new Vector3(0.85f, 0.85f, 1f);
            shadow.transform.SetAsFirstSibling();
        }

        // ---------------------------------------------------------------- UI

        static void BuildUi(Geprek.Player.PlayerController player, NightManager night,
                            Geprek.Core.CutsceneDirector cutscene)
        {
            var go = new GameObject("UI", typeof(RectTransform));
            var ui = go.AddComponent<UIRoot>();

            SetField(ui, "player", player);
            SetField(ui, "nightManager", night);
            SetField(ui, "database", Database);
            SetField(ui, "cutscene", cutscene);

            var so = new SerializedObject(ui);
            var icons = so.FindProperty("icons");

            void Icon(string field, string sprite) =>
                icons.FindPropertyRelative(field).objectReferenceValue = Ico(sprite);

            Icon("coin", "coin");             Icon("clock", "clock");
            Icon("star", "star");             Icon("chefHat", "chef_hat");
            Icon("heart", "heart");           Icon("recipeBook", "recipe_book");
            Icon("sun", "sun");               Icon("moon", "moon");
            Icon("customer", "customer");     Icon("report", "report");
            Icon("growth", "growth");         Icon("upgrade", "upgrade");
            Icon("locked", "locked");         Icon("check", "check");
            Icon("cross", "cross");           Icon("alert", "alert");
            Icon("sleep", "sleep");           Icon("social", "social");
            Icon("promo", "promo");           Icon("graduation", "graduation");
            Icon("warung", "build_warung");   Icon("ruko", "build_ruko");
            Icon("restaurant", "build_resto"); Icon("branches", "build_branch");
            Icon("play", "play");             Icon("pause", "pause");
            Icon("home", "home");             Icon("exit", "exit");
            Icon("tap", "tap");          Icon("staffPlus", "hire");
            Icon("moodHappy", "mood_happy");  Icon("moodNeutral", "mood_neutral");
            Icon("moodAngry", "mood_angry");

            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
