using UnityEditor;
using UnityEngine;

namespace Geprek.EditorTools
{
    // Keep the menu illustration sharp without applying pixel-sheet slicing rules.
    public class MenuArtworkImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (assetPath != "Assets/_Project/Resources/Art/geprek_menu_warung.png") return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
        }
    }
}
