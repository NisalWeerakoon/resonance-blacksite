using System;
using System.Collections.Generic;
using Blacksite.Agents.Trace;
using Blacksite.Gameplay;
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
                        root.AddComponent<TraceBrain>();
                        root.AddComponent<TraceSightSensor>();
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
            CreateTraceSandbox(prefabs[3]);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"BLACKSITE_CHARACTER_SETUP prefabs={prefabs.Count} scene={ScenePath}");
        }

        private static void CreateTraceSandbox(GameObject tracePrefab)
        {
            const string path = "Assets/Blacksite/Sandboxes/TeamLead/TraceDecisionSandbox.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Test Floor";
            floor.transform.position = new Vector3(0f, -0.1f, 0f);
            floor.transform.localScale = new Vector3(12f, 0.2f, 16f);

            var player = new GameObject("Sighted Player");
            player.transform.position = new Vector3(0f, 1f, -6f);
            var controller = player.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.center = Vector3.zero;
            player.AddComponent<PrototypePlayerController>();
            var camera = new GameObject("Player Camera").AddComponent<Camera>();
            camera.transform.SetParent(player.transform, false);
            camera.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            camera.gameObject.AddComponent<AudioListener>();

            var trace = (GameObject)PrefabUtility.InstantiatePrefab(tracePrefab);
            trace.transform.position = new Vector3(0f, 0f, 3f);
            var waypointRoot = new GameObject("TRACE Test Waypoints");
            var graph = waypointRoot.AddComponent<TraceWaypointGraph>();
            var ids = new[] { "START", "LEFT", "RIGHT", "CHOKE" };
            var positions = new[]
            {
                new Vector3(0f, 1.55f, 3f), new Vector3(-2.5f, 1.55f, 0f),
                new Vector3(2.5f, 1.55f, 0f), new Vector3(0f, 1.55f, -2f)
            };
            var points = new Transform[ids.Length];
            for (int i = 0; i < ids.Length; i++)
            {
                points[i] = new GameObject(ids[i]).transform;
                points[i].SetParent(waypointRoot.transform);
                points[i].position = positions[i];
            }
            var graphData = new SerializedObject(graph);
            var nodes = graphData.FindProperty("nodes");
            nodes.arraySize = ids.Length;
            for (int i = 0; i < ids.Length; i++)
            {
                nodes.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue = ids[i];
                nodes.GetArrayElementAtIndex(i).FindPropertyRelative("point").objectReferenceValue = points[i];
            }
            var links = graphData.FindProperty("links");
            var ends = new[] { ("START", "LEFT"), ("START", "RIGHT"),
                ("LEFT", "CHOKE"), ("RIGHT", "CHOKE") };
            links.arraySize = ends.Length;
            for (int i = 0; i < ends.Length; i++)
            {
                var link = links.GetArrayElementAtIndex(i);
                link.FindPropertyRelative("from").stringValue = ends[i].Item1;
                link.FindPropertyRelative("to").stringValue = ends[i].Item2;
                link.FindPropertyRelative("cost").floatValue = 1f;
                link.FindPropertyRelative("bidirectional").boolValue = true;
            }
            graphData.ApplyModifiedPropertiesWithoutUndo();

            var intercepts = new TraceInterceptPoint[3];
            for (int i = 0; i < intercepts.Length; i++)
            {
                intercepts[i] = points[i + 1].gameObject.AddComponent<TraceInterceptPoint>();
                var data = new SerializedObject(intercepts[i]);
                data.FindProperty("nodeId").stringValue = ids[i + 1];
                data.FindProperty("chokeImportance").floatValue = i == 2 ? 0.9f : 0.6f;
                data.FindProperty("historicalUse").floatValue = 0.5f;
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            var brainData = new SerializedObject(trace.GetComponent<TraceBrain>());
            brainData.FindProperty("graph").objectReferenceValue = graph;
            var candidates = brainData.FindProperty("interceptPoints");
            candidates.arraySize = intercepts.Length;
            for (int i = 0; i < intercepts.Length; i++)
                candidates.GetArrayElementAtIndex(i).objectReferenceValue = intercepts[i];
            brainData.ApplyModifiedPropertiesWithoutUndo();
            var sensorData = new SerializedObject(trace.GetComponent<TraceSightSensor>());
            sensorData.FindProperty("playerTarget").objectReferenceValue = player.transform;
            sensorData.ApplyModifiedPropertiesWithoutUndo();

            var light = new GameObject("Sun").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(45f, -25f, 0f);
            EditorSceneManager.SaveScene(scene, path);
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
            var traceScene = EditorSceneManager.OpenScene(
                "Assets/Blacksite/Sandboxes/TeamLead/TraceDecisionSandbox.unity",
                OpenSceneMode.Single);
            TraceBrain brain = null;
            TraceWaypointGraph graph = null;
            TraceSightSensor sensor = null;
            foreach (var root in traceScene.GetRootGameObjects())
            {
                if (brain == null) brain = root.GetComponentInChildren<TraceBrain>();
                if (graph == null) graph = root.GetComponentInChildren<TraceWaypointGraph>();
                if (sensor == null) sensor = root.GetComponentInChildren<TraceSightSensor>();
            }
            if (brain == null || graph == null || sensor == null)
                throw new InvalidOperationException("TRACE sandbox is missing its brain, graph or sight sensor.");
            var brainData = new SerializedObject(brain);
            var sensorData = new SerializedObject(sensor);
            if (brainData.FindProperty("graph").objectReferenceValue != graph ||
                brainData.FindProperty("interceptPoints").arraySize != 3 ||
                sensorData.FindProperty("playerTarget").objectReferenceValue == null)
                throw new InvalidOperationException("TRACE sandbox references are incomplete.");
            Debug.Log($"BLACKSITE_CHARACTER_AUDIT traceSandbox={traceScene.path} roots={traceScene.rootCount} candidates=3");
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
