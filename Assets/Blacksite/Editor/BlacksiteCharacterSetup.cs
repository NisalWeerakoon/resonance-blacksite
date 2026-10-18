using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Blacksite.Editor
{
    public static class BlacksiteCharacterSetup
    {
        private const string Base = "Assets/Blacksite/Art/Characters";
        private const string PrefabFolder = "Assets/Blacksite/Prefabs/Agents";
        private const string ScenePath = "Assets/Blacksite/Sandboxes/TeamLead/CharacterReview.unity";

        [MenuItem("BLACKSITE/Build character prefabs and review scene")]
        public static void Build()
        {
            EnsureFolder(Base, "Materials");
            var prefabs = new List<GameObject>();
            foreach (string name in new[] { "BASTION", "ECHO", "RIFT", "TRACE" })
            {
                string sourcePath = $"{Base}/Detailed/{name}_detailed.fbx";
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
                if (source == null) throw new InvalidOperationException($"Missing model: {sourcePath}");

                var root = new GameObject(name);
                try
                {
                    var visual = (GameObject)PrefabUtility.InstantiatePrefab(source);
                    visual.name = "Visual";
                    visual.transform.SetParent(root.transform, false);
                    ApplyMaterials(name, visual);
                    if (name == "TRACE")
                    {
                        var collider = root.AddComponent<SphereCollider>();
                        collider.radius = 0.55f;
                        collider.center = new Vector3(0f, 1.55f, 0f);
                    }
                    else
                    {
                        var collider = root.AddComponent<CapsuleCollider>();
                        collider.height = name == "BASTION" ? 2.1f : 1.9f;
                        collider.radius = name == "BASTION" ? 0.52f : 0.38f;
                        collider.center = new Vector3(0f, collider.height / 2f, 0f);
                    }
                    string path = $"{PrefabFolder}/{name}.prefab";
                    prefabs.Add(PrefabUtility.SaveAsPrefabAsset(root, path));
                }
                finally { UnityEngine.Object.DestroyImmediate(root); }
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Review Floor";
            floor.transform.position = new Vector3(0f, -0.1f, 0f);
            floor.transform.localScale = new Vector3(16f, 0.2f, 9f);
            for (int i = 0; i < prefabs.Count; i++)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[i]);
                instance.transform.position = new Vector3(-5.25f + i * 3.5f, 0f, 0f);
            }
            var light = new GameObject("Review Key Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.6f;
            light.transform.rotation = Quaternion.Euler(45f, -25f, 0f);
            var camera = new GameObject("Review Camera").AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 2.6f, -11f);
            camera.transform.LookAt(new Vector3(0f, 1.1f, 0f));
            camera.fieldOfView = 55f;
            camera.gameObject.AddComponent<AudioListener>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"BLACKSITE_CHARACTER_SETUP prefabs={prefabs.Count} scene={ScenePath}");
        }

        [MenuItem("BLACKSITE/Audit character prefabs")]
        public static void Audit()
        {
            foreach (string name in new[] { "BASTION", "ECHO", "RIFT", "TRACE" })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/{name}.prefab");
                if (prefab == null) throw new InvalidOperationException($"Missing {name} prefab.");
                var renderers = prefab.GetComponentsInChildren<Renderer>(true);
                int skinned = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length;
                int colliders = prefab.GetComponentsInChildren<Collider>(true).Length;
                if (renderers.Length == 0 || skinned == 0 || colliders == 0)
                    throw new InvalidOperationException($"Incomplete {name} prefab.");
                foreach (var renderer in renderers)
                    foreach (var material in renderer.sharedMaterials)
                        if (material == null || material.shader == null ||
                            !material.shader.name.StartsWith("Universal Render Pipeline/", StringComparison.Ordinal))
                            throw new InvalidOperationException($"Invalid URP material on {name}.");
                Debug.Log($"BLACKSITE_CHARACTER_AUDIT name={name} renderers={renderers.Length} skinned={skinned} colliders={colliders}");
            }
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid() || scene.rootCount < 6)
                throw new InvalidOperationException("Character review scene is incomplete.");
            Debug.Log($"BLACKSITE_CHARACTER_AUDIT scene={ScenePath} roots={scene.rootCount}");
        }

        private static void ApplyMaterials(string name, GameObject visual)
        {
            foreach (var renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                var assigned = renderer.sharedMaterials;
                for (int i = 0; i < assigned.Length; i++)
                {
                    string sourceName = assigned[i] != null ? assigned[i].name : "";
                    assigned[i] = GetMaterial(name, sourceName);
                }
                renderer.sharedMaterials = assigned;
            }
        }

        private static Material GetMaterial(string name, string sourceName)
        {
            string key = sourceName.Contains("Accent") ? "Accent" :
                sourceName.Contains("Dark_Glass") ? "Glass" :
                sourceName.Contains("Joint_Rubber") ? "Joint" :
                sourceName.Contains("Gunmetal") ? "Gunmetal" :
                sourceName.Contains("Light_Panel") ? "Light" : "Ceramic";
            string path = $"{Base}/Materials/{name}_{key}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP/Lit shader is unavailable.");
            material = new Material(shader) { name = $"{name}_{key}" };
            var color = ColorFor(key, name);
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Metallic", key == "Gunmetal" ? 0.7f : key == "Joint" ? 0.05f : 0.25f);
            material.SetFloat("_Smoothness", key == "Joint" ? 0.2f : 0.55f);
            if (key == "Accent")
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 1.8f);
            }
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Color ColorFor(string key, string name)
        {
            switch (key)
            {
                case "Light": return new Color(0.83f, 0.87f, 0.85f);
                case "Gunmetal": return new Color(0.13f, 0.18f, 0.21f);
                case "Joint": return new Color(0.025f, 0.035f, 0.045f);
                case "Glass": return new Color(0.01f, 0.025f, 0.035f);
                case "Accent":
                    if (name == "BASTION") return new Color(1f, 0.055f, 0.04f);
                    if (name == "ECHO") return new Color(0.02f, 0.85f, 0.95f);
                    if (name == "RIFT") return new Color(0.05f, 0.28f, 1f);
                    return new Color(0.02f, 0.9f, 0.32f);
                default: return new Color(0.64f, 0.7f, 0.72f);
            }
        }

        private static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{child}"))
                AssetDatabase.CreateFolder(parent, child);
        }
    }
}
