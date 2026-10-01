using System.Collections.Generic;
using System.IO;
using Geprek.Data;
using UnityEditor;
using UnityEngine;

namespace Geprek.EditorTools
{
    /// <summary>
    /// Membangun seluruh isi game dari kode: aset data, prefab, dan scene.
    /// Dipisah jadi beberapa berkas partial supaya tiap bagian tetap terbaca.
    /// </summary>
    public static partial class GeprekBuilder
    {
        public const string Root = "Assets/_Project";
        const string SpriteDir = Root + "/Art/Sprites";
        const string UiDir = Root + "/Art/UI";

        static readonly Dictionary<string, Dictionary<string, Sprite>> SheetCache = new();

        [MenuItem("Geprek/2. Build Everything")]
        public static void BuildEverything()
        {
            // membangun scene mengganti scene yang terbuka, dan itu tidak boleh
            // dilakukan saat play mode berjalan
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Geprek] Keluar dari Play Mode dulu sebelum menjalankan build.");
                return;
            }

            SheetCache.Clear();
            BuildData();
            BuildPrefabs();
            BuildScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Geprek] Build selesai: data, prefab, dan scene siap.");
        }

        // ---------------------------------------------------------------- sprite

        /// <summary>Ambil satu sprite dari sheet yang sudah dipotong.</summary>
        public static Sprite Spr(string sheet, string name)
        {
            if (!SheetCache.TryGetValue(sheet, out var map))
            {
                map = new Dictionary<string, Sprite>();
                foreach (var obj in AssetDatabase.LoadAllAssetsAtPath($"{SpriteDir}/{sheet}.png"))
                    if (obj is Sprite s) map[s.name] = s;
                SheetCache[sheet] = map;
            }

            if (map.TryGetValue(name, out var sprite)) return sprite;
            Debug.LogWarning($"[Geprek] Sprite '{name}' tidak ada di {sheet}.png");
            return null;
        }

        public static Sprite Food(string n) => Spr("Foods", n);
        public static Sprite House(string n) => Spr("Household", n);
        public static Sprite Env(string n) => Spr("Environment", n);
        public static Sprite Ico(string n) => Spr("Sign", n);

        /// <summary>Sprite tunggal dari folder UI (lantai, dinding, bayangan).</summary>
        public static Sprite Ui(string fileName) =>
            AssetDatabase.LoadAssetAtPath<Sprite>($"{UiDir}/{fileName}.png");

        public static AudioClip Sfx(string n) =>
            AssetDatabase.LoadAssetAtPath<AudioClip>($"{Root}/Audio/SFX/{n}.wav");

        public static AudioClip Bgm(string n) =>
            AssetDatabase.LoadAssetAtPath<AudioClip>($"{Root}/Audio/BGM/{n}.wav");

        // ---------------------------------------------------------------- aset

        /// <summary>Buat ScriptableObject baru, atau pakai ulang yang sudah ada di path itu.</summary>
        public static T Asset<T>(string folder, string fileName) where T : ScriptableObject
        {
            EnsureFolder(folder);
            string path = $"{folder}/{fileName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;

            var created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, path);
            return created;
        }

        public static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = Path.GetDirectoryName(folder)!.Replace('\\', '/');
            string leaf = Path.GetFileName(folder);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        // ---------------------------------------------------------------- objek scene

        public static GameObject Go(string name, Transform parent, Vector3 localPos = default)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            return go;
        }

        public static SpriteRenderer Sr(string name, Transform parent, Sprite sprite, Vector3 pos,
                                        int order = 0, string layer = "Default")
        {
            var go = Go(name, parent, pos);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            sr.sortingLayerName = layer;
            return sr;
        }

        /// <summary>Latar yang di-tile, dipakai untuk lantai dan dinding.</summary>
        public static SpriteRenderer Tiled(string name, Transform parent, Sprite sprite,
                                           Vector3 center, Vector2 size, int order)
        {
            var sr = Sr(name, parent, sprite, center, order);
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            sr.size = size;
            return sr;
        }

        public static BoxCollider2D Box(GameObject go, Vector2 size, Vector2 offset, bool trigger = false)
        {
            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;
            col.offset = offset;
            col.isTrigger = trigger;
            return col;
        }

        /// <summary>Dinding tak terlihat supaya pemain tidak keluar ruangan.</summary>
        public static void Wall(Transform parent, string name, Vector2 center, Vector2 size)
        {
            var go = Go(name, parent, center);
            go.layer = LayerMask.NameToLayer("Default");
            Box(go, size, Vector2.zero);
        }
    }
}
