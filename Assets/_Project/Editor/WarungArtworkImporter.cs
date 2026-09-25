using UnityEditor;
using UnityEngine;

namespace Geprek.EditorTools
{
    /// <summary>Keep the generated Warung PNGs on the same pixel grid as the main art set.</summary>
    public sealed class WarungArtworkImporter : AssetPostprocessor
    {
        public const string Folder = "Assets/Sprites/Warung/";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Folder, System.StringComparison.Ordinal)
                || !assetPath.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase)) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 128f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.crunchedCompression = false;
            importer.mipmapEnabled = false;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.sRGBTexture = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 1024;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            bool decor = assetPath.EndsWith("spice_shelf.png") || assetPath.EndsWith("menu_board.png");
            settings.spritePivot = decor ? new Vector2(0.5f, 0f) : new Vector2(0.5f, 0.5f);
            settings.spriteBorder = assetPath.EndsWith("day_banner.png")
                ? new Vector4(56f, 16f, 40f, 16f) : Vector4.zero;
            importer.SetTextureSettings(settings);
        }
    }
}
