using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Arash.Editor.Content
{
    /// <summary>
    /// Applies the art manifest written by tools/art (pivot, pixels per unit, 9-slice border) to
    /// the texture importers of the sprites under Resources/Art. Only importers whose settings
    /// differ are changed, so this is quick after the first run.
    /// </summary>
    static class ArtImport
    {
        const string ManifestPath = "Assets/_Project/Art/art-manifest.json";

#pragma warning disable 0649 // filled in by JsonUtility
        [Serializable]
        class Entry
        {
            public string path;
            public float ppu;
            public float pivotX;
            public float pivotY;
            public float[] border;
            public bool tiled;
        }

        [Serializable]
        class Manifest
        {
            public List<Entry> sprites;
        }
#pragma warning restore 0649

        public static bool Apply()
        {
            if (!File.Exists(ManifestPath))
            {
                Debug.LogError("[Arash Setup] Art manifest not found at " + ManifestPath);
                return false;
            }
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(ManifestPath));
            if (manifest == null || manifest.sprites == null)
                return false;

            var changed = 0;
            var missing = 0;
            foreach (var entry in manifest.sprites)
            {
                var importer = AssetImporter.GetAtPath(entry.path) as TextureImporter;
                if (importer == null)
                {
                    missing++;
                    continue;
                }
                if (Configure(importer, entry))
                {
                    importer.SaveAndReimport();
                    changed++;
                }
            }
            if (changed > 0 || missing > 0)
                Debug.Log($"[Arash Setup] Art: {manifest.sprites.Count} sprites, {changed} reimported, {missing} missing.");
            return missing == 0;
        }

        /// <summary>Sets the importer up for the entry; true if anything changed.</summary>
        static bool Configure(TextureImporter importer, Entry entry)
        {
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            var pivot = new Vector2(entry.pivotX, entry.pivotY);
            var border = entry.border != null && entry.border.Length == 4
                ? new Vector4(entry.border[0], entry.border[1], entry.border[2], entry.border[3])
                : Vector4.zero;

            var same = importer.textureType == TextureImporterType.Sprite &&
                       importer.spriteImportMode == SpriteImportMode.Single &&
                       Mathf.Approximately(importer.spritePixelsPerUnit, entry.ppu) &&
                       !importer.mipmapEnabled &&
                       importer.alphaIsTransparency &&
                       settings.spriteAlignment == (int)SpriteAlignment.Custom &&
                       Vector2.Distance(settings.spritePivot, pivot) < 0.0005f &&
                       settings.spriteMeshType == SpriteMeshType.FullRect &&
                       settings.spriteBorder == border &&
                       importer.wrapMode == TextureWrapMode.Clamp;
            if (same)
                return false;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = entry.ppu;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            // Full-rect meshes are required for sliced and tiled drawing.
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteBorder = border;
            importer.SetTextureSettings(settings);
            return true;
        }
    }
}
