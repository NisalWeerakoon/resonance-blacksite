using System;
using System.Collections.Generic;
using Blacksite.Agents.Echo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Blacksite.Agents.Echo.Editor
{
    /// <summary>
    /// Builds an independent ECHO test scene; never overwrites the team's RiftSandbox,
    /// main scene, or existing EchoSandbox. Run once from the BLACKSITE/ECHO menu.
    /// </summary>
    public static class EchoFullSandboxBuilder
    {
        private const string ScenePath =
            "Assets/Blacksite/Sandboxes/WorldBuilder/EchoSandbox_FullAI.unity";
        private const string MaterialsFolder = "Assets/Blacksite/Art/Environment/EchoDemo";
        private static Material floor, wall, trim, cyan, amber, red, green, purple;

        [MenuItem("BLACKSITE/ECHO/Create Full AI Sandbox")]
        public static void Create()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                EditorUtility.DisplayDialog("ECHO scene already exists",
                    "EchoSandbox_FullAI.unity exists. Open it rather than overwriting your edits.", "OK");
                return;
            }
            EnsureFolder("Assets/Blacksite/Art/Environment");
            EnsureFolder(MaterialsFolder);
            EnsureFolder("Assets/Blacksite/Sandboxes/WorldBuilder");
            floor = MaterialAsset("Echo_Floor", new Color(0.07f, 0.12f, 0.18f));
            wall = MaterialAsset("Echo_Wall", new Color(0.09f, 0.16f, 0.20f));
            trim = MaterialAsset("Echo_Trim", new Color(0.18f, 0.29f, 0.36f));
            cyan = MaterialAsset("Echo_Cyan", new Color(0.07f, 0.91f, 1f), true);
            amber = MaterialAsset("Echo_Amber", new Color(1f, 0.61f, 0.10f), true);
            red = MaterialAsset("Echo_Red", new Color(0.95f, 0.13f, 0.18f), true);
            green = MaterialAsset("Echo_Green", new Color(0.1f, 0.90f, 0.57f), true);
            purple = MaterialAsset("Echo_Violet", new Color(0.55f, 0.31f, 0.92f), true);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("ECHO_SANDBOX_FULL_AI");
            var worldObject = new GameObject("WorldContext");
            worldObject.transform.SetParent(root.transform);
            EchoWorldContext worldContext = worldObject.AddComponent<EchoWorldContext>();
            Transform rooms = Group("Rooms", root.transform);
            Transform corridors = Group("Corridors", root.transform);
            Transform doors = Group("Doors_Demo", root.transform);
            Transform props = Group("Props_NoCollision", root.transform);

            Room(rooms, "ENTRY", -29f, 0f, 10f, 10f, "E");
            Room(rooms, "HUB", -12f, 0f, 12f, 12f, "WNES");
            Room(rooms, "ACOUSTIC", -12f, 18f, 12f, 10f, "SE");
            Room(rooms, "SECURITY", 8f, 18f, 12f, 10f, "WS");
            Room(rooms, "MAINT", 8f, 0f, 12f, 12f, "WNS");
            Room(rooms, "CORE", 8f, -18f, 12f, 10f, "NW");

            Corridor(corridors, "ENTRY_HUB", -21f, 0f, 6f, true);
            Corridor(corridors, "HUB_ACOUSTIC", -12f, 9.5f, 7f, false);
            Corridor(corridors, "ACOUSTIC_SECURITY", -2f, 18f, 8f, true);
            Corridor(corridors, "SECURITY_MAINT", 8f, 9.5f, 7f, false);
            Corridor(corridors, "HUB_MAINT", -2f, 0f, 8f, true);
            Corridor(corridors, "MAINT_CORE", 8f, -9.5f, 7f, false);
            Corridor(corridors, "HUB_CORE_BYPASS_VERTICAL", -12f, -12f, 12f, false);
            Corridor(corridors, "HUB_CORE_BYPASS_HORIZONTAL", -5f, -18f, 14f, true);

            Door(doors, worldContext, "D01", -21f, 0f, true);
            Door(doors, worldContext, "D02", -12f, 9.5f, false);
            Door(doors, worldContext, "D03", -2f, 18f, true);
            Door(doors, worldContext, "D04", 8f, 9.5f, false);
            Door(doors, worldContext, "D05", -2f, 0f, true);
            Door(doors, worldContext, "D06", 8f, -9.5f, false);
            Door(doors, worldContext, "D07", -12f, -8f, false);

            // Nonblocking props. Retain a wide center aisle for the capsule/motor.
            PropsForRoom(props, -29f, 0f, cyan);
            PropsForRoom(props, -12f, 0f, cyan);
            PropsForRoom(props, -12f, 18f, green);
            PropsForRoom(props, 8f, 18f, red);
            PropsForRoom(props, 8f, 0f, amber);
            PropsForRoom(props, 8f, -18f, purple);

            var echo = new GameObject("ECHO_Proxy_Agent");
            echo.transform.SetParent(root.transform);
            echo.transform.position = new Vector3(-12f, 1.1f, 18f);
            var body = Box("Slim_Hooded_Body", echo.transform, Vector3.zero,
                new Vector3(0.8f, 1.7f, 0.65f), trim, false);
            body.transform.localPosition = Vector3.zero;
            Sphere("Acoustic_Dish_Left", echo.transform, new Vector3(-0.65f, 0.7f, 0.28f), 0.48f, cyan, false);
            Sphere("Acoustic_Dish_Right", echo.transform, new Vector3(0.65f, 0.7f, 0.28f), 0.48f, cyan, false);
            echo.AddComponent<EchoDemoPathFollower>();
            echo.AddComponent<EchoBrain>();

            var controls = new GameObject("ECHO_Clickable_Test_Controls");
            controls.transform.SetParent(root.transform);
            controls.AddComponent<EchoDemoControlPanel>();

            var cameraObject = new GameObject("Main Camera");
            cameraObject.transform.SetParent(root.transform);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 42f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.015f, 0.025f, 0.045f);
            cameraObject.AddComponent<AudioListener>();
            cameraObject.transform.position = new Vector3(-8f, 75f, -9f);
            cameraObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            AddLight(root.transform, new Vector3(-12f, 5f, 18f), new Color(0.15f, 0.78f, 0.94f));
            AddLight(root.transform, new Vector3(-12f, 5f, 0f), new Color(0.18f, 0.68f, 0.9f));
            AddLight(root.transform, new Vector3(8f, 5f, 18f), new Color(0.9f, 0.38f, 0.38f));
            AddLight(root.transform, new Vector3(8f, 5f, -18f), new Color(0.29f, 0.7f, 1f));
            RenderSettings.ambientLight = new Color(0.23f, 0.31f, 0.4f);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeGameObject = echo;
            EditorUtility.DisplayDialog("ECHO sandbox created",
                "Open EchoSandbox_FullAI.unity and click Play. Use the on-screen buttons to test sounds, D02/D03 doors, memory, and scores.", "OK");
        }

        [MenuItem("BLACKSITE/ECHO/Open Full AI Sandbox")]
        public static void Open()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                EditorUtility.DisplayDialog("Missing scene", "Use BLACKSITE/ECHO/Create Full AI Sandbox first.", "OK");
                return;
            }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = path.Substring(0, path.LastIndexOf('/'));
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(path.LastIndexOf('/') + 1));
        }

        private static Material MaterialAsset(string name, Color color, bool glow = false)
        {
            string path = MaterialsFolder + "/" + name + ".mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) throw new InvalidOperationException("No supported Lit shader found");
            Material material = new Material(shader);
            material.color = color;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (glow)
            {
                material.EnableKeyword("_EMISSION");
                if (material.HasProperty("_EmissionColor"))
                    material.SetColor("_EmissionColor", color * 1.8f);
            }
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Transform Group(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            return go.transform;
        }

        private static GameObject Box(string name, Transform parent, Vector3 pos,
            Vector3 scale, Material material, bool collider)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static GameObject Sphere(string name, Transform parent, Vector3 pos,
            float diameter, Material material, bool collider)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.localPosition = pos;
            go.transform.localScale = Vector3.one * diameter;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static void Room(Transform parent, string name, float x, float z, float w, float d, string openings)
        {
            Transform group = Group(name, parent);
            group.position = new Vector3(x, 0f, z);
            Box("WalkableFloor", group, new Vector3(0f, -0.18f, 0f), new Vector3(w, 0.36f, d), floor, true);
            foreach (char side in "WNES") WallSide(group, side, w, d, openings.IndexOf(side) >= 0);
            Box("CenterLine", group, new Vector3(0f, 0.008f, 0f), new Vector3(0.08f, 0.008f, d * 0.68f), cyan, false);
        }

        private static void WallSide(Transform parent, char side, float w, float d, bool hasOpening)
        {
            bool eastWest = side == 'E' || side == 'W';
            float width = eastWest ? d : w;
            Vector3 center = eastWest ? new Vector3(side == 'E' ? w / 2f : -w / 2f, 1.75f, 0f)
                : new Vector3(0f, 1.75f, side == 'N' ? d / 2f : -d / 2f);
            if (!hasOpening)
            {
                Box("Wall_" + side, parent, center,
                    eastWest ? new Vector3(0.3f, 3.5f, width) : new Vector3(width, 3.5f, 0.3f), wall, true);
                return;
            }
            float section = (width - 3.4f) / 2f;
            for (int k = -1; k <= 1; k += 2)
            {
                Vector3 offset = eastWest ? new Vector3(0f, 0f, k * (3.4f + section) / 2f)
                    : new Vector3(k * (3.4f + section) / 2f, 0f, 0f);
                Box("Wall_" + side + "_" + k, parent, center + offset,
                    eastWest ? new Vector3(0.3f, 3.5f, section) : new Vector3(section, 3.5f, 0.3f), wall, true);
            }
            Vector3 top = new Vector3(center.x, 3.05f, center.z);
            Box("DoorHeader_" + side, parent, top,
                eastWest ? new Vector3(0.36f, 0.9f, 3.4f) : new Vector3(3.4f, 0.9f, 0.36f), trim, true);
        }

        private static void Corridor(Transform parent, string name, float x, float z, float length, bool horizontal)
        {
            Transform group = Group(name, parent);
            group.position = new Vector3(x, 0f, z);
            Box("CorridorFloor", group, new Vector3(0f, -0.18f, 0f),
                horizontal ? new Vector3(length, 0.36f, 3.4f) : new Vector3(3.4f, 0.36f, length), floor, true);
            for (int sign = -1; sign <= 1; sign += 2)
                Box("CyanRail", group,
                    horizontal ? new Vector3(0f, 0.04f, sign * 1.58f) : new Vector3(sign * 1.58f, 0.04f, 0f),
                    horizontal ? new Vector3(length, 0.05f, 0.08f) : new Vector3(0.08f, 0.05f, length), cyan, false);
        }

        private static void Door(Transform parent, EchoWorldContext context,
            string id, float x, float z, bool horizontalPassage)
        {
            var root = new GameObject(id + "_DemoDoor");
            root.transform.SetParent(parent);
            root.transform.position = new Vector3(x, 0f, z);
            // Horizontal passage -> passage runs on X, blocker spans Z.
            GameObject blocker = Box("ClosedDoorPhysicalBlocker", root.transform,
                new Vector3(0f, 1.32f, 0f), horizontalPassage
                    ? new Vector3(0.3f, 2.6f, 3.35f)
                    : new Vector3(3.35f, 2.6f, 0.3f), red, true);
            GameObject indicatorObject = Box("DoorColorStatus", root.transform,
                new Vector3(0f, 2.75f, 0f), horizontalPassage
                    ? new Vector3(0.35f, 0.16f, 3.4f)
                    : new Vector3(3.4f, 0.16f, 0.35f), cyan, false);
            EchoDemoDoor component = root.AddComponent<EchoDemoDoor>();
            component.Setup(id, blocker, indicatorObject.GetComponent<Renderer>());
            context.RegisterDoor(component);
        }

        private static void PropsForRoom(Transform parent, float x, float z, Material color)
        {
            var group = Group("RoomProps_" + x + "_" + z, parent);
            group.position = new Vector3(x, 0f, z);
            Box("NorthConsole", group, new Vector3(-3.0f, 0.55f, 3.5f),
                new Vector3(1.6f, 1.1f, 0.7f), trim, false);
            Box("NorthScreen", group, new Vector3(-3.0f, 1.13f, 3.5f),
                new Vector3(1.2f, 0.05f, 0.45f), color, false);
            Box("FloorPanel", group, new Vector3(3f, 0.05f, -3f),
                new Vector3(1.4f, 0.08f, 1.4f), color, false);
        }

        private static void AddLight(Transform parent, Vector3 pos, Color color)
        {
            GameObject lightObject = new GameObject("NonShadowRoomLight");
            lightObject.transform.SetParent(parent);
            lightObject.transform.position = pos;
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = 1.4f;
            light.range = 14f;
            light.shadows = LightShadows.None;
        }
    }
}
