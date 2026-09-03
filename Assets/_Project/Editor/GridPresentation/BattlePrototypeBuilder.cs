using System;
using System.IO;
using ProjectResonance.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace ProjectResonance.Presentation.Editor
{
    // One-shot authoring only. The scene uses saved PNGs/Tiles, never procedural runtime textures.
    public static class BattlePrototypeBuilder
    {
        private const string ArtRoot = "Assets/_Project/Art/Environment/Tiles/Prototype";
        private const string SourceRoot = "Assets/_Project/Editor/GridPresentation/ArtSource";
        public const string ScenePath = "Assets/_Project/Scenes/BattlePrototype.unity";

        [MenuItem("ProjectResonance/Prototype/Rebuild Grid Assets and Scene")]
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
            var grass = LoadSource("GrassTexture.png");
            var cliff = LoadSource("CliffTexture.png");
            try
            {
                WriteTop(grass, "GrassTop", false);
                WriteTop(grass, "GrassTopVariation", true);
                WriteSide(cliff, "CliffLeft", true, false);
                WriteSide(cliff, "CliffRight", false, true);
                WriteSide(cliff, "CliffBoth", true, true);
            }
            finally
            {
                Object.DestroyImmediate(grass);
                Object.DestroyImmediate(cliff);
            }
            AssetDatabase.Refresh();

            var top = CreateTile("GrassTop", new Vector2(0.5f, 0.5f));
            var variation = CreateTile("GrassTopVariation", new Vector2(0.5f, 0.5f));
            var left = CreateTile("CliffLeft", new Vector2(0.5f, 1f));
            var right = CreateTile("CliffRight", new Vector2(0.5f, 1f));
            var both = CreateTile("CliffBoth", new Vector2(0.5f, 1f));
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
            var topMap = CreateTilemap("TerrainTopTilemap", gridObject.transform, material);
            var sideMap = CreateTilemap("TerrainSideTilemap", gridObject.transform, material);
            var overlay = CreateTilemap("OverlayTilemap", gridObject.transform, material);
            overlay.GetComponent<TilemapRenderer>().sortingOrder = 10;
            var presenter = gridObject.AddComponent<IsometricGridPresenter>();
            Assign(presenter, "terrainTop", topMap);
            Assign(presenter, "terrainSide", sideMap);
            Assign(presenter, "grassTop", top);
            Assign(presenter, "grassTopVariation", variation);
            Assign(presenter, "cliffLeft", left);
            Assign(presenter, "cliffRight", right);
            Assign(presenter, "cliffBoth", both);
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

            // Validate Unity's actual projection against the 32-pixel cliff step before saving.
            var heightStep = grid.CellToWorld(new Vector3Int(0, 0, 1)) - grid.CellToWorld(Vector3Int.zero);
            if (Mathf.Abs(heightStep.y - 0.25f) > 0.0001f)
                throw new InvalidOperationException($"Unexpected height projection: {heightStep}.");
            if (topMap.GetUsedTilesCount() == 0 || sideMap.GetUsedTilesCount() == 0)
                throw new InvalidOperationException("Prototype terrain was not rendered.");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"BattlePrototype saved: 10x8, Height 0/1/2, height step {heightStep}; top/side populated, overlay empty.");
        }

        private static Texture2D LoadSource(string name)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes($"{SourceRoot}/{name}")))
                throw new InvalidOperationException($"Cannot load source {name}.");
            return texture;
        }

        private static Color Sample(Texture2D source, float u, float v)
        {
            // Sample a small pixel grid, not a bilinear/blurred rescale of the generated source.
            var x = (Mathf.FloorToInt(Mathf.Repeat(u, 1f) * 64) + 0.5f) / 64f;
            var y = (Mathf.FloorToInt(Mathf.Repeat(v, 1f) * 64) + 0.5f) / 64f;
            return source.GetPixel((int)(x * source.width), (int)(y * source.height));
        }

        private static void WriteTop(Texture2D source, string name, bool variation)
        {
            var pixels = new Color[128 * 64];
            for (var y = 0; y < 64; y++)
            {
                for (var x = 0; x < 128; x++)
                {
                    var dx = (x + 0.5f - 64f) / 64f;
                    var dy = (y + 0.5f - 32f) / 32f;
                    var edge = Mathf.Abs(dx) + Mathf.Abs(dy);
                    if (edge > 1f) continue;
                    var color = Sample(source, (dx + dy + 1f) * 0.16f + (variation ? 0.37f : 0f),
                        (dy - dx + 1f) * 0.16f + (variation ? 0.19f : 0f));
                    color = Color.Lerp(color, new Color(0.45f, 0.60f, 0.29f), 0.25f);
                    if (variation) color *= new Color(0.95f, 1.02f, 1.05f, 1f);
                    if (edge > 0.965f) color *= dy >= 0 ? 1.13f : 0.68f;
                    else if (edge > 0.925f && dy < 0) color *= 0.9f;
                    color.a = 1f;
                    pixels[y * 128 + x] = color;
                }
            }
            WritePng(name, pixels);
        }

        private static void WriteSide(Texture2D source, string name, bool left, bool right)
        {
            var pixels = new Color[128 * 64];
            for (var y = 0; y < 64; y++)
            {
                for (var x = 0; x < 128; x++)
                {
                    var isLeft = x < 64;
                    if (isLeft ? !left : !right) continue;
                    var upperEdge = 32f + Mathf.Abs(x + 0.5f - 64f) * 0.5f;
                    var depth = upperEdge - (y + 0.5f);
                    if (depth < 0f || depth >= 32f) continue;
                    var u = isLeft ? (x + 0.5f) / 64f : (x - 63.5f) / 64f;
                    var color = Sample(source, u * 0.5f, depth / 128f);
                    color *= isLeft ? 1.18f : 0.84f;
                    if (depth < 2f) color *= 0.75f;
                    if (x == 63 || x == 64) color *= 0.85f;
                    color.a = 1f;
                    pixels[y * 128 + x] = color;
                }
            }
            WritePng(name, pixels);
        }

        private static void WritePng(string name, Color[] pixels)
        {
            var texture = new Texture2D(128, 64, TextureFormat.RGBA32, false);
            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes($"{ArtRoot}/{name}.png", texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
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
