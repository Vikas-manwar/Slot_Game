using UnityEditor;
using UnityEngine;

namespace SlotGame.EditorTools
{
    internal static class SpriteImportConfigurator
    {
        private static readonly (string file, Vector4 border)[] SlicedSprites =
        {
            (EditorPaths.Panels + "/Panel_Blue.png", new Vector4(48, 64, 48, 48)),
            (EditorPaths.Panels + "/Panel_Orange.png", new Vector4(32, 40, 32, 32)),
        };

        public static void ConfigureAll()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { EditorPaths.UI }))
                Configure(AssetDatabase.GUIDToAssetPath(guid));
            AssetDatabase.Refresh();
        }

        private static void Configure(string path)
        {
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.spritePixelsPerUnit = 100;
            importer.maxTextureSize = 2048;

            importer.textureCompression = path.EndsWith("/Background.png")
                ? TextureImporterCompression.Compressed
                : TextureImporterCompression.Uncompressed;

            foreach (var (file, border) in SlicedSprites)
                if (path == file) importer.spriteBorder = border;

            importer.SaveAndReimport();
        }
    }
}
