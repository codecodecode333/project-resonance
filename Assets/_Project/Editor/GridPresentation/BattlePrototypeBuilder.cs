using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace Riftchord.Presentation.Editor
{
    // One-shot authoring only. The scene uses saved PNGs/Tiles, never procedural runtime textures.
    public static class BattlePrototypeBuilder
    {
        private const string ArtRoot = "Assets/_Project/Art/Environment/Tiles/Prototype";
        private const string SourceRoot = "Assets/_Project/Editor/GridPresentation/ArtSource";
        public const string ScenePath = "Assets/_Project/Scenes/BattlePrototype.unity";

        [MenuItem("RIFTCHORD/Prototype/Rebuild Grid Assets and Scene")]
        private static void RebuildFromMenu()
        {
            if (!EditorUtility.DisplayDialog("Rebuild prototype",
                "Replace the generated prototype tiles and BattlePrototype scene?", "Rebuild", "Cancel")
                || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build();
        }

        public static void Build()
        {
            Directory.CreateDirectory(ArtRoot);
            var grass = LoadSource("GrassBlockSource.png");
            var alternate = LoadSource("GrassBlockVariationSource.png");
            try
            {
                MatchVariationAlpha(grass, alternate);
                WriteBlock(grass, "GrassBlock");
                WriteBlock(alternate, "GrassBlockVariation");
            }
            finally
            {
                Object.DestroyImmediate(grass);
                Object.DestroyImmediate(alternate);
            }
            AssetDatabase.Refresh();

            // Top diamond center is (64, 96) in bottom-origin sprite pixels.
            var block = CreateTile("GrassBlock", new Vector2(0.5f, 0.75f));
            var variation = CreateTile("GrassBlockVariation", new Vector2(0.5f, 0.75f));
            var materialPath = "Assets/_Project/Materials/PrototypeTerrain.mat";
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) throw new InvalidOperationException("URP 2D unlit sprite shader is missing.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, materialPath);
            }
            material.shader = shader;
            EditorUtility.SetDirty(material);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("BattlePrototypeRoot");
            var gridObject = Child("BattleGrid", root.transform);
            var grid = gridObject.AddComponent<UnityEngine.Grid>();
            grid.cellLayout = GridLayout.CellLayout.IsometricZAsY;
            grid.cellSize = new Vector3(1f, 0.5f, 1f);
            var blockMap = CreateTilemap("TerrainBlockTilemap", gridObject.transform, material);
            var overlay = CreateTilemap("OverlayTilemap", gridObject.transform, material);
            overlay.GetComponent<TilemapRenderer>().sortingOrder = 10;
            var presenter = gridObject.AddComponent<IsometricBlockGridPresenter>();
            Assign(presenter, "terrainBlocks", blockMap);
            Assign(presenter, "grassBlock", block);
            Assign(presenter, "grassBlockVariation", variation);
            var bootstrap = Child("PresentationBootstrap", root.transform).AddComponent<BattlePrototypeBootstrap>();
            Assign(bootstrap, "presenter", presenter);
            bootstrap.RenderDemo();

            var cameraObject = Child("Main Camera", root.transform);
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 3.125f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(29, 38, 48, 255);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.allowHDR = false;
            camera.allowMSAA = false;
            camera.transparencySortMode = TransparencySortMode.CustomAxis;
            camera.transparencySortAxis = new Vector3(0, 1, -0.26f);
            cameraObject.transform.position = new Vector3(0.5f, 2f, -10f);

            // Validate Unity's actual projection against the 32-pixel block step before saving.
            var heightStep = grid.CellToWorld(new Vector3Int(0, 0, 1)) - grid.CellToWorld(Vector3Int.zero);
            if (Mathf.Abs(heightStep.y - 0.25f) > 0.0001f)
                throw new InvalidOperationException($"Unexpected height projection: {heightStep}.");
            if (blockMap.GetUsedTilesCount() != 2)
                throw new InvalidOperationException("Prototype terrain was not rendered.");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"BattlePrototype saved: 10x8, Height 0/1/2, height step {heightStep}; block columns populated, overlay empty.");
        }

        private static Texture2D LoadSource(string name)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes($"{SourceRoot}/{name}")))
                throw new InvalidOperationException($"Cannot load source {name}.");
            return texture;
        }

        private static void WriteBlock(Texture2D source, string name)
        {
            // Crop transparent margins and point-resize the COMPLETE authored block.
            // No face projection, texture synthesis, recoloring, or runtime generation.
            var sourcePixels = source.GetPixels32();
            var minX = source.width;
            var minY = source.height;
            var maxX = -1;
            var maxY = -1;
            var hasTransparency = false;
            for (var y = 0; y < source.height; y++)
            {
                for (var x = 0; x < source.width; x++)
                {
                    var alpha = sourcePixels[y * source.width + x].a;
                    hasTransparency |= alpha == 0;
                    if (alpha < 128) continue;
                    minX = Mathf.Min(minX, x);
                    maxX = Mathf.Max(maxX, x);
                    minY = Mathf.Min(minY, y);
                    maxY = Mathf.Max(maxY, y);
                }
            }
            if (!hasTransparency || maxX < minX || maxY < minY)
                throw new InvalidOperationException($"{name} needs a nonempty block with genuine transparent alpha.");

            // A 128x64 diamond plus a 32px wall occupies 128x96; bottom 32px stay empty.
            var pixels = new Color32[128 * 128];
            for (var y = 0; y < 96; y++)
            {
                for (var x = 0; x < 128; x++)
                {
                    var sx = minX + (int)((x + 0.5f) * (maxX - minX + 1) / 128);
                    var sy = minY + (int)((y + 0.5f) * (maxY - minY + 1) / 96);
                    pixels[(y + 32) * 128 + x] = sourcePixels[sy * source.width + sx];
                }
            }
            var texture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            try
            {
                texture.SetPixels32(pixels);
                texture.Apply();
                File.WriteAllBytes($"{ArtRoot}/{name}.png", texture.EncodeToPNG());
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        private static void MatchVariationAlpha(Texture2D source, Texture2D variation)
        {
            // The generated variant shares the source framing but arrived as RGB.
            // Reuse the authored silhouette so both adjacent tile variants fit identically.
            if (source.width != variation.width || source.height != variation.height)
                throw new InvalidOperationException("Block variation must match the base source canvas.");
            var silhouette = source.GetPixels32();
            var pixels = variation.GetPixels32();
            for (var i = 0; i < pixels.Length; i++) pixels[i].a = silhouette[i].a;
            // LoadImage may change an RGB PNG's texture format, so restore alpha storage.
            variation.Reinitialize(source.width, source.height, TextureFormat.RGBA32, false);
            variation.SetPixels32(pixels);
            variation.Apply();
        }

        private static Tile CreateTile(string name, Vector2 pivot)
        {
            var pngPath = $"{ArtRoot}/{name}.png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(pngPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 128;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaIsTransparency = true;
            importer.isReadable = false;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            var tilePath = $"{ArtRoot}/{name}.asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, tilePath);
            }
            tile.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
            tile.colliderType = Tile.ColliderType.None;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        private static GameObject Child(string name, Transform parent)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static Tilemap CreateTilemap(string name, Transform parent, Material material)
        {
            var go = Child(name, parent);
            var tilemap = go.AddComponent<Tilemap>();
            tilemap.tileAnchor = Vector3.zero;
            var renderer = go.AddComponent<TilemapRenderer>();
            renderer.mode = TilemapRenderer.Mode.Individual;
            renderer.sharedMaterial = material;
            return tilemap;
        }

        private static void Assign(Object target, string property, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(property).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
