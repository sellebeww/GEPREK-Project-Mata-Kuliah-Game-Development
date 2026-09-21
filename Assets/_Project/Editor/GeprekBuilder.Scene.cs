using System.Collections.Generic;
using Geprek.Audio;
using Geprek.Core;
using Geprek.Customers;
using Geprek.Data;
using Geprek.Player;
using Geprek.Progression;
using Geprek.Stations;
using Geprek.UI;
using Geprek.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace Geprek.EditorTools
{
    public static partial class GeprekBuilder
    {
        const string ScenePath = Root + "/Scenes/Main.unity";

        // Angka tata letak warung ada di GeprekBuilder.Warung.cs supaya
        // denah dan pembangunnya berada di satu berkas.
        // Ruangan dibuat sedikit lebih pendek dari lebarnya supaya seluruh isi warung
        // muat dalam satu layar 16:9 tanpa bagian penting tertutup HUD.
        static readonly Vector2 WarungMin = new(-7.9f, -4.2f);
        static readonly Vector2 WarungMax = new(7.9f, 3.9f);
        static readonly Vector2 HomeMin = new(-6.2f, -3.5f);
        static readonly Vector2 HomeMax = new(6.2f, 3.6f);

        const float WarungCameraSize = 4.4f;
        const float HomeCameraSize = 4.0f;

        public static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camera = BuildCamera();
            BuildEventSystem();

            var locationsRoot = new GameObject("Locations").transform;
            var warung = BuildBusiness(locationsRoot, WarungSpec());
            var ruko = BuildBusiness(locationsRoot, RukoSpec());
            var resto = BuildBusiness(locationsRoot, RestoranSpec());
            var home = BuildHome(locationsRoot, out var nightManager);

            var player = (GameObject)PrefabUtility.InstantiatePrefab(PlayerPrefab);
            player.name = "Player";
            player.transform.position = new Vector3(-2.0f, 1.0f, 0f);

            var follow = camera.GetComponent<CameraFollow>();
            SetField(follow, "target", player.transform);

            var managers = new GameObject("Managers").transform;
            var input = Go("Input", managers).AddComponent<GeprekInput>();
            var audio = BuildAudio(managers);
            BuildJuice(managers, follow);

            var locationManager = Go("Locations Manager", managers).AddComponent<LocationManager>();
            ConfigureLocations(locationManager, new[] { warung, ruko, resto, home }, player.transform, follow);

            var cutscene = Go("Cutscene", managers).AddComponent<CutsceneDirector>();
            SetField(cutscene, "locations", locationManager);
            SetField(cutscene, "cameraFollow", follow);
            SetField(cutscene, "database", Database);

            var gm = Go("Game Manager", managers).AddComponent<GameManager>();
            SetField(gm, "database", Database);
            SetField(gm, "locations", locationManager);
            SetField(gm, "cutscene", cutscene);

            BuildUi(player.GetComponent<PlayerController>(), nightManager, cutscene);

            EditorSceneManager.MarkSceneDirty(scene);
            EnsureFolder(Root + "/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuild(ScenePath);
        }

        // ---------------------------------------------------------------- kamera & input

        static Camera BuildCamera()
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = WarungCameraSize;
            cam.backgroundColor = new Color(0.09f, 0.07f, 0.06f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            go.AddComponent<AudioListener>();

            var follow = go.AddComponent<CameraFollow>();
            SetField(follow, "smoothTime", 0.16f);
            SetField(follow, "offset", new Vector2(0f, 0.25f));
            return cam;
        }

        static void BuildEventSystem()
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<UnityEngine.EventSystems.EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();   // wajib: project memakai Input System baru
        }

        // ---------------------------------------------------------------- audio

        static AudioManager BuildAudio(Transform parent)
        {
            var go = Go("Audio", parent);
            var music = go.AddComponent<AudioSource>();
            music.loop = true; music.playOnAwake = false; music.volume = 0.32f;

            var kitchen = go.AddComponent<AudioSource>();
            kitchen.loop = true; kitchen.playOnAwake = false; kitchen.volume = 0f;

            var crowd = go.AddComponent<AudioSource>();
            crowd.loop = true; crowd.playOnAwake = false; crowd.volume = 0f;

            var manager = go.AddComponent<AudioManager>();
            SetField(manager, "musicSource", music);
            SetField(manager, "kitchenSource", kitchen);
            SetField(manager, "crowdSource", crowd);
            SetField(manager, "kitchenAmbience", Bgm("amb_kitchen"));
            SetField(manager, "crowdAmbience", Bgm("amb_crowd"));
            SetField(manager, "streetAmbience", Bgm("amb_street"));
            SetField(manager, "ambienceVolume", 0.5f);
            SetField(manager, "dayMusic", Bgm("bgm_day"));
            SetField(manager, "nightMusic", Bgm("bgm_night"));
            SetField(manager, "menuMusic", Bgm("bgm_menu"));
            SetField(manager, "musicVolume", 0.32f);
            SetField(manager, "sfxVolume", 0.85f);

            var table = new (SfxId id, string clip, float vol)[]
            {
                (SfxId.Click, "sfx_click", 0.55f),
                (SfxId.Pickup, "sfx_pickup", 0.55f),
                (SfxId.Drop, "sfx_drop", 0.55f),
                (SfxId.Sizzle, "sfx_sizzle", 0.40f),
                (SfxId.Geprek, "sfx_geprek", 0.50f),
                (SfxId.Plate, "sfx_plate", 0.50f),
                (SfxId.Serve, "sfx_serve", 0.60f),
                (SfxId.Coin, "sfx_coin", 0.65f),
                (SfxId.Error, "sfx_error", 0.55f),
                (SfxId.Levelup, "sfx_levelup", 0.70f),
                (SfxId.Upgrade, "sfx_upgrade", 0.65f),
                (SfxId.Notify, "sfx_notify", 0.55f),
                (SfxId.CustomerAngry, "sfx_customer_angry", 0.55f),
                (SfxId.DayEnd, "sfx_day_end", 0.70f),
                (SfxId.Sleep, "sfx_sleep", 0.60f),
            };

            var so = new SerializedObject(manager);
            var list = so.FindProperty("sfx");
            list.arraySize = table.Length;
            for (int i = 0; i < table.Length; i++)
            {
                var e = list.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("id").enumValueIndex = (int)table[i].id;
                e.FindPropertyRelative("clip").objectReferenceValue = Sfx(table[i].clip);
                e.FindPropertyRelative("volume").floatValue = table[i].vol;
                e.FindPropertyRelative("pitchJitter").floatValue = 0.06f;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return manager;
        }

        // ---------------------------------------------------------------- lokasi

        static void ConfigureLocations(LocationManager manager, GameObject[] roots,
                                       Transform player, CameraFollow follow)
        {
            SetField(manager, "player", player);
            SetField(manager, "cameraFollow", follow);

            var so = new SerializedObject(manager);
            var list = so.FindProperty("locations");
            list.arraySize = roots.Length;

            for (int i = 0; i < roots.Length; i++)
            {
                var root = roots[i];
                var spec = SpecOf(root.name);

                Vector2 min = spec != null ? spec.min : HomeMin;
                Vector2 max = spec != null ? spec.max : HomeMax;
                float size = spec != null ? spec.cameraSize : HomeCameraSize;
                LocationId id = spec != null ? spec.id : LocationId.Home;

                // pemain berdiri di lantai dapur, kira-kira sepertiga dari kiri,
                // supaya ruangan lebar tetap terbaca sejak frame pertama
                Vector3 spawn = spec != null
                    ? new Vector3(Mathf.Lerp(spec.min.x, spec.max.x, 0.32f), new Layout(spec).islandY + 1.1f, 0f)
                    : new Vector3(0f, -0.8f, 0f);

                var spawnGo = Go("PlayerSpawn", root.transform, spawn);
                var e = list.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("id").enumValueIndex = (int)id;
                e.FindPropertyRelative("root").objectReferenceValue = root.transform;
                e.FindPropertyRelative("playerSpawn").objectReferenceValue = spawnGo.transform;
                e.FindPropertyRelative("cameraMin").vector2Value = min;
                e.FindPropertyRelative("cameraMax").vector2Value = max;
                e.FindPropertyRelative("cameraSize").floatValue = size;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Cari resep lokasi berdasarkan nama akar objeknya.</summary>
        static BusinessSpec SpecOf(string rootName)
        {
            if (rootName == LocationId.Warung.ToString()) return WarungSpec();
            if (rootName == LocationId.Ruko.ToString()) return RukoSpec();
            if (rootName == LocationId.Restaurant.ToString()) return RestoranSpec();
            return null;
        }

        // ---------------------------------------------------------------- resep tiap lokasi

        /// <summary>Arc 1. Kecil, sederhana, perabot kayu murah, satu baris meja.</summary>
        static BusinessSpec WarungSpec() => new()
        {
            id = LocationId.Warung, displayName = "Warung Ibu",
            min = new Vector2(-7.9f, -4.2f), max = new Vector2(7.9f, 3.9f), cameraSize = 4.4f,
            diningFloor = "floor_wood", kitchenFloor = "floor_tile",
            baseFryers = 1, upgradeFryers = 1, cobeks = 1, plates = 1,
            tablesPerRow = new[] { 5 }, upgradeSeats = 2,
            tableA = "table_wood", tableB = "table_round",
            stoolA = "stool_red", stoolB = "stool_teal",
            furnitureScale = 0.9f, stationScale = 0.85f, modernDecor = false
        };

        /// <summary>Arc 2. Lebih lebar, dapur bertambah, dua baris meja, perabot lebih rapi.</summary>
        static BusinessSpec RukoSpec() => new()
        {
            id = LocationId.Ruko, displayName = "Ruko Geprek",
            min = new Vector2(-10.6f, -5.0f), max = new Vector2(10.6f, 4.3f), cameraSize = 5.35f,
            diningFloor = "floor_terrazzo", kitchenFloor = "floor_tile",
            baseFryers = 2, upgradeFryers = 1, cobeks = 2, plates = 1,
            tablesPerRow = new[] { 4, 4 }, upgradeSeats = 2,
            tableA = "table_modern", tableB = "table_round_modern",
            stoolA = "chair_teal_a", stoolB = "chair_teal_b",
            furnitureScale = 0.95f, stationScale = 0.88f, modernDecor = true
        };

        /// <summary>Arc 3. Paling besar, dua pulau penyajian, dua belas meja, gaya restoran.</summary>
        static BusinessSpec RestoranSpec() => new()
        {
            id = LocationId.Restaurant, displayName = "Restoran Geprek",
            min = new Vector2(-13.2f, -5.8f), max = new Vector2(13.2f, 4.6f), cameraSize = 6.3f,
            diningFloor = "floor_terrazzo", kitchenFloor = "floor_tile",
            baseFryers = 3, upgradeFryers = 1, cobeks = 2, plates = 2,
            tablesPerRow = new[] { 6, 6 }, upgradeSeats = 2,
            tableA = "table_marble", tableB = "table_round_modern",
            stoolA = "chair_cream", stoolB = "booth_red",
            furnitureScale = 1f, stationScale = 0.9f, modernDecor = true
        };

        static void AddSceneToBuild(string path)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Exists(s => s.path == path)) return;
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // ---------------------------------------------------------------- efek & petunjuk

        /// <summary>Pengatur efek kecil: uap, asap, percikan, getaran kamera.</summary>
        static void BuildJuice(Transform parent, CameraFollow follow)
        {
            var go = Go("Juice", parent);
            var juice = go.AddComponent<JuiceDirector>();
            SetField(juice, "puffSprite", Ui("fx_puff"));
            SetField(juice, "sparkSprite", Ui("fx_spark"));
            SetField(juice, "dotSprite", Ui("fx_dot"));
            SetField(juice, "cameraFollow", follow);
            SetField(juice, "poolSize", 48);
        }

    }
}
