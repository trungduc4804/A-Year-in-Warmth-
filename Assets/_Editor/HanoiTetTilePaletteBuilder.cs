using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Generates Unity Tile assets for the Hanoi Tet palette after the PNGs import.
/// Tiles that describe architecture or props receive a Grid collider.
/// </summary>
public static class HanoiTetTilePaletteBuilder
{
    private const string TileSourceFolder = "Assets/_Sprite/HanoiTetCompleteTilemap/Tiles";
    private const string OutputFolder = "Assets/_Sprite/HanoiTetCompleteTilemap/GeneratedTiles";

    [MenuItem("Tools/A Year in Warmth/Build Hanoi Tet Tile Palette")]
    public static void BuildPalette()
    {
        if (!AssetDatabase.IsValidFolder(OutputFolder))
        {
            AssetDatabase.CreateFolder("Assets/_Sprite/HanoiTetCompleteTilemap", "GeneratedTiles");
        }

        string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { TileSourceFolder });
        int createdOrUpdated = 0;

        foreach (string guid in textureGuids)
        {
            string texturePath = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
            if (sprite == null) continue;

            string tilePath = Path.Combine(OutputFolder, sprite.name + ".asset").Replace("\\", "/");
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, tilePath);
            }

            tile.sprite = sprite;
            tile.colliderType = IsBlocking(sprite.name) ? Tile.ColliderType.Grid : Tile.ColliderType.None;
            EditorUtility.SetDirty(tile);
            createdOrUpdated++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[HanoiTetTilePaletteBuilder] Created or updated {createdOrUpdated} tiles in {OutputFolder}.");
    }

    private static bool IsBlocking(string tileName)
    {
        return tileName.Contains("MossWall") ||
               tileName.Contains("Gate") ||
               tileName.Contains("Fence") ||
               tileName.Contains("MarketStall") ||
               tileName.Contains("CourtyardProps") ||
               tileName.Contains("Blocker");
    }
}
