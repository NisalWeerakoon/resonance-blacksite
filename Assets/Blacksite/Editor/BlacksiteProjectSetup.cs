using Blacksite.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Blacksite.Editor
{
    public static class BlacksiteProjectSetup
    {
        [MenuItem("BLACKSITE/Initialize URP and prototype scenes")]
        public static void Initialize()
        {
            const string settingsFolder = "Assets/Blacksite/Settings";
            if (!AssetDatabase.IsValidFolder(settingsFolder)) AssetDatabase.CreateFolder("Assets/Blacksite", "Settings");
            const string rendererPath = settingsFolder + "/BlacksiteRenderer.asset";
            const string pipelinePath = settingsFolder + "/BlacksiteURP.asset";
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, rendererPath);
            }
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, pipelinePath);
            }
            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
            CreateScene("Assets/Blacksite/Scenes/Integration/BlacksitePrototype.unity", true);
            CreateScene("Assets/Blacksite/Sandboxes/TeamLead/TraceSandbox.unity", false);
            CreateScene("Assets/Blacksite/Sandboxes/WorldBuilder/EchoSandbox.unity", false);
            CreateScene("Assets/Blacksite/Sandboxes/SystemsEngineer/BastionSandbox.unity", false);
            CreateScene("Assets/Blacksite/Sandboxes/AgentController/RiftSandbox.unity", false);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            UnityEngine.Debug.Log("BLACKSITE URP assets and prototype scenes initialized.");
        }

        private static void CreateScene(string path, bool integration)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Prototype Ground";
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(30f, 1f, 30f);
            var player = new GameObject("Player Prototype");
            player.transform.position = new Vector3(0f, 1.1f, -6f);
            player.AddComponent<CharacterController>();
            var cameraObject = new GameObject("View Camera");
            cameraObject.transform.SetParent(player.transform);
            cameraObject.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            player.AddComponent<PrototypePlayerController>();
            var lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            lightObject.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
            if (integration)
            {
                for (int i = 0; i < 4; i++)
                {
                    var proxy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    proxy.name = new[] { "ECHO Proxy", "BASTION Proxy", "RIFT Proxy", "TRACE Proxy" }[i];
                    proxy.transform.position = new Vector3(-6f + 4f * i, 1f, 4f);
                }
            }
            EditorSceneManager.SaveScene(scene, path);
        }
    }
}
