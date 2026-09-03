using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace ProjectResonance.Presentation.Editor
{
    public static class BattlePrototypePreview
    {
        public static void RebuildAndCapture()
        {
            BattlePrototypeBuilder.Build();
            Capture();
        }

        [MenuItem("ProjectResonance/Prototype/Capture Grid Preview")]
        public static void Capture()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(BattlePrototypeBuilder.ScenePath);
            var bootstrap = Object.FindFirstObjectByType<BattlePrototypeBootstrap>();
            bootstrap.RenderDemo();
            var camera = Camera.main;
            var target = new RenderTexture(1280, 800, 24, RenderTextureFormat.ARGB32);
            var pixels = new Texture2D(1280, 800, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                target.Create();
                RenderPipeline.SubmitRenderRequest(camera,
                    new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 1280, 800), 0, 0);
                pixels.Apply();
                Directory.CreateDirectory("Logs");
                File.WriteAllBytes("Logs/BattlePrototype-preview.png", pixels.EncodeToPNG());
                Debug.Log("BattlePrototype preview captured at Logs/BattlePrototype-preview.png (1280x800).");
            }
            finally
            {
                RenderTexture.active = previous;
                target.Release();
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(pixels);
            }
        }
    }
}
