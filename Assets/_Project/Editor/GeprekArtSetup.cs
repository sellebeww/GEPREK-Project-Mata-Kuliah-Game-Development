using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace Geprek.EditorTools
{
    /// <summary>
    /// Mengatur import setting dan memotong keenam sprite sheet memakai daftar
    /// kotak yang dihitung di luar Unity (Tools/slices.json), lengkap dengan namanya.
    /// </summary>
    public static class GeprekArtSetup
    {
        const string SpriteFolder = "Assets/_Project/Art/Sprites";
        const int PixelsPerUnit = 128;

        [Serializable] public class SliceRect { public string name; public int x, y, w, h; }
        [Serializable] public class SheetSlices { public string file; public float pivotX, pivotY; public SliceRect[] sprites; }
        [Serializable] public class SliceFile { public SheetSlices[] sheets; }

        [MenuItem("Geprek/1. Setup Sprite Sheets")]
        public static void ApplySlices()
        {
            string jsonPath = Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, "Tools", "slices.json");
            if (!File.Exists(jsonPath))
            {
                Debug.LogError($"[Geprek] slices.json tidak ditemukan di {jsonPath}");
                return;
            }

            var data = JsonUtility.FromJson<SliceFile>(File.ReadAllText(jsonPath));
            int totalSprites = 0;

            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (var sheet in data.sheets)
                {
                    string path = $"{SpriteFolder}/{sheet.file}.png";
                    if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                    {
                        Debug.LogWarning($"[Geprek] Tekstur tidak ditemukan: {path}");
                        continue;
                    }

                    ConfigureImporter(importer);
                    totalSprites += ApplyRects(importer, sheet);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            foreach (var sheet in data.sheets)
                AssetDatabase.ImportAsset($"{SpriteFolder}/{sheet.file}.png", ImportAssetOptions.ForceUpdate);

            SetupSingleSprites();

            AssetDatabase.Refresh();
            Debug.Log($"[Geprek] Selesai memotong {totalSprites} sprite dari {data.sheets.Length} sheet.");
        }

        static void ConfigureImporter(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;          // gaya pixel art, jangan diburamkan
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;   // lebih aman untuk UI dan ikon
            settings.spriteExtrude = 1;
            importer.SetTextureSettings(settings);
        }

        static int ApplyRects(TextureImporter importer, SheetSlices sheet)
        {
            var factories = new SpriteDataProviderFactories();
            factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();

            var rects = new List<SpriteRect>(sheet.sprites.Length);
            var pairs = new List<SpriteNameFileIdPair>(sheet.sprites.Length);
            var pivot = new Vector2(sheet.pivotX, sheet.pivotY);

            foreach (var s in sheet.sprites)
            {
                var id = GUID.Generate();
                rects.Add(new SpriteRect
                {
                    name = s.name,
                    spriteID = id,
                    rect = new Rect(s.x, s.y, s.w, s.h),
                    alignment = SpriteAlignment.Custom,
                    pivot = pivot,
                    border = Vector4.zero
                });
                pairs.Add(new SpriteNameFileIdPair(s.name, id));
            }

            provider.SetSpriteRects(rects.ToArray());
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>()?.SetNameFileIdPairs(pairs);
            provider.Apply();

            EditorUtility.SetDirty(importer);
            return rects.Count;
        }

        /// <summary>Tekstur lantai, dinding, dan bayangan: sprite tunggal, tidak dipotong.</summary>
        static void SetupSingleSprites()
        {
            const string folder = "Assets/_Project/Art/UI";
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer) continue;

                bool tileable = System.IO.Path.GetFileName(path).StartsWith("floor_")
                             || System.IO.Path.GetFileName(path).StartsWith("wall_");

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = PixelsPerUnit;
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.wrapMode = tileable ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 512;

                var st = new TextureImporterSettings();
                importer.ReadTextureSettings(st);
                st.spriteMeshType = SpriteMeshType.FullRect;
                st.spriteAlignment = (int)SpriteAlignment.Center;
                importer.SetTextureSettings(st);

                importer.SaveAndReimport();
            }
        }
    }
}
