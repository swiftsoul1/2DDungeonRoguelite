#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Only affects this new prototype folder; leaves existing art import settings alone.
public class PrototypeArtImporter : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Resources/DungeonPrototype/Art/")) return;
        TextureImporter importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        // These are detailed, high-resolution pixel-style images, not native 32px sprites.
        // Prefilter smaller mip levels instead of sampling scattered source pixels at game scale.
        importer.filterMode = FilterMode.Trilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.mipmapEnabled = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.maxTextureSize = 2048;
        importer.spritePixelsPerUnit = 100;
        importer.npotScale = TextureImporterNPOTScale.None;
        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
    }

    [MenuItem("Dungeon/Reimport Prototype Art")]
    private static void ReimportPrototypeArt()
    {
        AssetDatabase.Refresh();
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/DungeonPrototype/Art" }))
            AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);
    }

    public static void PreparePrototypeArt()
    {
        AssetDatabase.Refresh();
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/DungeonPrototype/Art" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && (importer.filterMode != FilterMode.Trilinear || !importer.mipmapEnabled ||
                importer.textureCompression != TextureImporterCompression.Uncompressed))
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }
    }
}
#endif
