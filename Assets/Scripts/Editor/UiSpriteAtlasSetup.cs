using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

[InitializeOnLoad]
public static class UiSpriteAtlasSetup
{
    private const string AtlasPath = "Assets/UI/UI.spriteatlasv2";
    private const string SpritesFolder = "Assets/UI/Sprites";

    static UiSpriteAtlasSetup()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(AtlasPath) && AssetDatabase.IsValidFolder(SpritesFolder))
                CreateAtlas();
        };
    }

    [MenuItem("Última Luz/Crear o actualizar Sprite Atlas UI")]
    public static void CreateAtlas()
    {
        List<Object> sprites = new();

        foreach (string guid in AssetDatabase.FindAssets("t:Sprite", new[] { SpritesFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string name = Path.GetFileNameWithoutExtension(path);

            if (name.StartsWith("bg_") || name.StartsWith("sky_"))
                continue;

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

            if (sprite != null)
                sprites.Add(sprite);
        }

        if (sprites.Count == 0)
        {
            Debug.LogWarning($"[UiSpriteAtlasSetup] No hay sprites en {SpritesFolder}.");
            return;
        }

        SpriteAtlasAsset atlas = new();
        atlas.Add(sprites.ToArray());
        SpriteAtlasAsset.Save(atlas, AtlasPath);
        AssetDatabase.ImportAsset(AtlasPath, ImportAssetOptions.ForceUpdate);

        if (AssetImporter.GetAtPath(AtlasPath) is SpriteAtlasImporter importer)
        {
            importer.includeInBuild = true;

            importer.packingSettings = new SpriteAtlasPackingSettings
            {
                blockOffset = 1,
                padding = 4,
                enableRotation = false,
                enableTightPacking = false,
                enableAlphaDilation = true
            };

            importer.textureSettings = new SpriteAtlasTextureSettings
            {
                readable = false,
                generateMipMaps = false,
                sRGB = true,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 1
            };

            TextureImporterPlatformSettings platform = importer.GetPlatformSettings("DefaultTexturePlatform");
            platform.maxTextureSize = 2048;
            platform.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SetPlatformSettings(platform);

            importer.SaveAndReimport();
        }

        if (EditorSettings.spritePackerMode == SpritePackerMode.Disabled)
            EditorSettings.spritePackerMode = SpritePackerMode.SpriteAtlasV2;

        Debug.Log($"[UiSpriteAtlasSetup] Sprite Atlas UI creado con {sprites.Count} sprites.");
    }
}
