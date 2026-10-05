using UnityEditor;
using UnityEngine;

namespace Siavosh.Editor.Setup
{
    /// <summary>
    /// Imports the game art (PNG files rendered from the SVG sources in Siavosh/Design) as sprites:
    /// 200 pixels per world unit, no mipmaps, high-quality compression that keeps the flat
    /// manuscript colours clean.
    /// </summary>
    class ArtImporter : AssetPostprocessor
    {
        const string ArtFolder = "Assets/_Project/Resources/Art/";
        public const float PixelsPerUnit = 200f; // matches Siavosh/Design/tools/export_game.py

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtFolder))
                return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
            importer.filterMode = UnityEngine.FilterMode.Bilinear;
            importer.maxTextureSize = 4096; // backgrounds are 2304 px wide
            importer.textureCompression = TextureImporterCompression.CompressedHQ;

            // Full-rect meshes so ground tiles and ledges can use the Tiled draw mode.
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
        }
    }
}
