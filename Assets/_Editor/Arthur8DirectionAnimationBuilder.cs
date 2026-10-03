using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class Arthur8DirectionAnimationBuilder
{
    private const string Root = "Assets/_Sprite/Arthur8DirectionV2";
    private const string FrameFolder = Root + "/Frames";
    private const string OutputFolder = Root + "/GeneratedAnimation";

    private static readonly string[] Directions =
    {
        "Down", "DownRight", "Right", "UpRight",
        "Up", "UpLeft", "Left", "DownLeft"
    };

    [MenuItem("Tools/A Year in Warmth/Build Arthur 8 Direction Animator")]
    public static void Build()
    {
        EnsureFolder(OutputFolder);

        foreach (string direction in Directions)
        {
            BuildClip("Idle", direction, 4, 4f);
            BuildClip("Walk", direction, 6, 10f);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Arthur: rebuilt 16 seamless looping sprite clips.");
    }

    private static void BuildClip(string action, string direction, int frameCount, float fps)
    {
        var sprites = new List<Sprite>(frameCount);
        for (int i = 0; i < frameCount; i++)
        {
            string path = $"{FrameFolder}/Arthur8_{action}_{direction}_{i:00}.png";
            ConfigureTexture(path);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                throw new System.IO.FileNotFoundException($"Arthur sprite was not imported: {path}");
            sprites.Add(sprite);
        }

        string clipPath = $"{OutputFolder}/Arthur8_{action}_{direction}.anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, clipPath);
        }

        clip.name = $"Arthur8_{action}_{direction}";
        clip.frameRate = fps;

        // Add the first sprite again exactly at the loop endpoint. The last
        // visible pose therefore transitions into the first pose before Unity
        // wraps time to zero, removing the one-frame seam/jump.
        var keys = new ObjectReferenceKeyframe[frameCount + 1];
        for (int i = 0; i < frameCount; i++)
        {
            keys[i] = new ObjectReferenceKeyframe
            {
                time = i / fps,
                value = sprites[i]
            };
        }
        keys[frameCount] = new ObjectReferenceKeyframe
        {
            time = frameCount / fps,
            value = sprites[0]
        };

        var binding = new EditorCurveBinding
        {
            path = string.Empty,
            type = typeof(SpriteRenderer),
            propertyName = "m_Sprite"
        };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        settings.loopBlend = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
    }

    private static void ConfigureTexture(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            return;

        bool changed = importer.textureType != TextureImporterType.Sprite
            || importer.spriteImportMode != SpriteImportMode.Single
            || importer.spritePixelsPerUnit != 16f
            || importer.filterMode != FilterMode.Point
            || importer.textureCompression != TextureImporterCompression.Uncompressed
            || importer.mipmapEnabled;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 16f;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;

        if (changed)
            importer.SaveAndReimport();
    }

    private static void EnsureFolder(string folder)
    {
        string[] parts = folder.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
