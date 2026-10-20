using System;
using Blacksite.Agents.Bastion;
using Blacksite.Agents.Trace;
using Blacksite.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Blacksite.Editor
{
    public static class BuildSystemsEngineerSandbox
    {
        private const string Prefabs = "Assets/Blacksite/Prefabs/Systems";
        private const string ScenePath = "Assets/Blacksite/Scenes/Test_Interactions_Bastion.unity";

        [MenuItem("BLACKSITE/Build Systems Engineer sandbox")]
        public static void Build()
        {
            EnsureFolder(Prefabs);
            EnsureFolder("Assets/Blacksite/Scenes");
            var grey = MakeMaterial("Systems_Grey", new Color(.30f, .37f, .42f));
            var red = MakeMaterial("Systems_Red", new Color(.78f, .16f, .12f));
            var blue = MakeMaterial("Systems_Blue", new Color(.10f, .43f, .79f));
            var yellow = MakeMaterial("Systems_Yellow", new Color(.86f, .65f, .10f));
            ThrowableDecoy decoyPrefab = BuildDecoy(yellow);
            GameObject doorPrefab = BuildDoor(red);
            GameObject barricadePrefab = BuildBarricade(yellow);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Cube("Floor", new Vector3(0f, -.25f, 0f), new Vector3(28f, .5f, 34f), grey);
            MakeLight();

            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player_Test_WASD_E_G";
            player.transform.position = new Vector3(0f, 1f, 3f);
            UnityEngine.Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());
            player.AddComponent<CharacterController>();
            player.GetComponent<Renderer>().sharedMaterial = blue;
            var cameraObject = new GameObject("PlayerCamera");
            cameraObject.transform.SetParent(player.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, .65f, 0f);
            var camera = cameraObject.AddComponent<Camera>();
            player.AddComponent<PrototypePlayerController>();
            player.AddComponent<PlayerSystemsInteractor>().ConfigureSandbox(camera, decoyPrefab);

            var door = (GameObject)PrefabUtility.InstantiatePrefab(doorPrefab);
            door.name = "Door_D01_E_Interact";
            door.transform.position = new Vector3(0f, 1.2f, -4f);
            var doorState = door.GetComponent<DoorPortal>();

            var choke = new GameObject("Barricade_D02_ChokeTrigger");
            choke.transform.position = new Vector3(6f, 1f, 1f);
            var chokeCollider = choke.AddComponent<BoxCollider>();
            chokeCollider.size = new Vector3(2.3f, 2f, 2.3f);
            chokeCollider.isTrigger = true;
            var barricade = (GameObject)PrefabUtility.InstantiatePrefab(barricadePrefab);
            barricade.name = "Barricade_D02_E_Push";
            barricade.transform.position = choke.transform.position;
            var barricadeState = barricade.GetComponent<MovableBarricade>();
            barricadeState.ConfigureSandbox("D02", chokeCollider);

            var covers = new CoverPoint[3];
            covers[0] = MakeCover("C01", new Vector3(-6f, 1f, -1f), .8f, red);
            covers[1] = MakeCover("C02", new Vector3(6f, 1f, -1f), .6f, blue);
            covers[2] = MakeCover("C03", new Vector3(0f, 1f, -9f), .9f, yellow);

            var graphObject = new GameObject("Shared_GraphAStar_Test_Nodes");
            var graph = graphObject.AddComponent<TraceWaypointGraph>();
            ConfigureGraph(graph, new[] {
                ("B", new Vector3(0f, 1f, 9f)),
                ("J", new Vector3(0f, 1f, 0f)),
                ("C01", covers[0].transform.position),
                ("C02", covers[1].transform.position),
                ("C03", covers[2].transform.position)
            });
            var planner = graphObject.AddComponent<ExistingGraphPathPlanner>();
            planner.ConfigureSandbox(graph);

            var bastion = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            bastion.name = "BASTION_Proxy_BehaviorTree";
            bastion.transform.position = new Vector3(0f, 1f, 9f);
            bastion.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            bastion.GetComponent<Renderer>().sharedMaterial = red;
            var eye = new GameObject("Eye");
            eye.transform.SetParent(bastion.transform, false);
            eye.transform.localPosition = new Vector3(0f, .6f, 0f);
            var brain = bastion.AddComponent<BastionBrain>();
            brain.ConfigureSandbox(eye.transform, player.transform, covers, planner,
                new MonoBehaviour[] { doorState, barricadeState });
            bastion.AddComponent<BastionSandboxActuator>();

            var telemetry = new GameObject("Systems_Event_Telemetry");
            telemetry.AddComponent<SystemsSandboxTelemetry>().ConfigureSandbox(doorState, barricadeState);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"BLACKSITE_SYSTEMS_SANDBOX_BUILT scene={ScenePath} prefabs=3 covers=3 graphNodes=5");
        }

        [MenuItem("BLACKSITE/Audit Systems Engineer sandbox")]
        public static void Audit()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            int missing = 0;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                    missing += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject);
            int doors = UnityEngine.Object.FindObjectsByType<DoorPortal>().Length;
            int barriers = UnityEngine.Object.FindObjectsByType<MovableBarricade>().Length;
            int agents = UnityEngine.Object.FindObjectsByType<BastionBrain>().Length;
            int covers = UnityEngine.Object.FindObjectsByType<CoverPoint>().Length;
            Debug.Log($"BLACKSITE_SYSTEMS_SANDBOX_AUDIT missingScripts={missing} doors={doors} barricades={barriers} bastion={agents} covers={covers}");
            if (missing != 0 || doors != 1 || barriers != 1 || agents != 1 || covers != 3)
                throw new Exception("Systems Engineer sandbox failed structural audit");
        }

        private static void EnsureFolder(string path)
        {
            string current = "Assets";
            foreach (string part in path.Substring("Assets/".Length).Split('/'))
            {
                string next = current + "/" + part;
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, part);
                current = next;
            }
        }

        private static Material MakeMaterial(string name, Color color)
        {
            string path = $"{Prefabs}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader) { name = name, color = color };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static GameObject Cube(string name, Vector3 position, Vector3 scale, Material material)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.position = position;
            cube.transform.localScale = scale;
            cube.GetComponent<Renderer>().sharedMaterial = material;
            return cube;
        }

        private static ThrowableDecoy BuildDecoy(Material material)
        {
            const string path = Prefabs + "/ThrowableDecoy.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing.GetComponent<ThrowableDecoy>();
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "ThrowableDecoy";
            sphere.transform.localScale = Vector3.one * .35f;
            sphere.GetComponent<Renderer>().sharedMaterial = material;
            sphere.AddComponent<Rigidbody>();
            sphere.AddComponent<ThrowableDecoy>();
            var saved = PrefabUtility.SaveAsPrefabAsset(sphere, path);
            UnityEngine.Object.DestroyImmediate(sphere);
            return saved.GetComponent<ThrowableDecoy>();
        }

        private static GameObject BuildDoor(Material material)
        {
            const string path = Prefabs + "/DoorPortal_D01.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;
            var root = new GameObject("DoorPortal_D01");
            var panel = Cube("PanelVisual", Vector3.zero, new Vector3(2.3f, 2.4f, .3f), material);
            panel.transform.SetParent(root.transform, false);
            var handle = new GameObject("PersistentHandle_E_Raycast");
            handle.transform.SetParent(root.transform, false);
            handle.transform.localPosition = new Vector3(0f, 0f, -.38f);
            var handleCollider = handle.AddComponent<BoxCollider>();
            handleCollider.size = new Vector3(.55f, .6f, .25f);
            handleCollider.isTrigger = true;
            var portal = root.AddComponent<DoorPortal>();
            portal.ConfigureSandbox("D01", panel.GetComponent<Collider>(), panel, false);
            var saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return saved;
        }

        private static GameObject BuildBarricade(Material material)
        {
            const string path = Prefabs + "/MovableBarricade_D02.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;
            var block = Cube("MovableBarricade_D02", Vector3.zero,
                new Vector3(1.8f, 1.6f, .8f), material);
            var body = block.AddComponent<Rigidbody>();
            body.mass = 12f;
            body.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            block.AddComponent<MovableBarricade>();
            var saved = PrefabUtility.SaveAsPrefabAsset(block, path);
            UnityEngine.Object.DestroyImmediate(block);
            return saved;
        }

        private static CoverPoint MakeCover(string id, Vector3 position, float safety, Material material)
        {
            Cube("CoverObstacle_" + id, position + new Vector3(0f, 0f, .9f),
                new Vector3(2.8f, 1.8f, .55f), material);
            var point = new GameObject("CoverPoint_" + id);
            point.transform.position = position;
            var peek = new GameObject("Peek");
            peek.transform.SetParent(point.transform, false);
            peek.transform.localPosition = new Vector3(1.3f, .5f, .5f);
            var cover = point.AddComponent<CoverPoint>();
            cover.ConfigureSandbox(id, peek.transform, safety);
            return cover;
        }

        private static void ConfigureGraph(TraceWaypointGraph graph, (string id, Vector3 position)[] nodes)
        {
            var serialized = new SerializedObject(graph);
            var nodeArray = serialized.FindProperty("nodes");
            nodeArray.arraySize = nodes.Length;
            for (int i = 0; i < nodes.Length; i++)
            {
                var marker = new GameObject("Waypoint_" + nodes[i].id);
                marker.transform.SetParent(graph.transform, false);
                marker.transform.position = nodes[i].position;
                var element = nodeArray.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("id").stringValue = nodes[i].id;
                element.FindPropertyRelative("point").objectReferenceValue = marker.transform;
            }
            var links = serialized.FindProperty("links");
            links.arraySize = 4;
            SetLink(links, 0, "B", "J", null);
            SetLink(links, 1, "J", "C01", null);
            SetLink(links, 2, "J", "C02", "D02");
            SetLink(links, 3, "J", "C03", "D01");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            graph.Rebuild();
        }

        private static void SetLink(SerializedProperty array, int index, string from, string to,
            string portal)
        {
            var element = array.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("from").stringValue = from;
            element.FindPropertyRelative("to").stringValue = to;
            element.FindPropertyRelative("cost").floatValue = 1f;
            element.FindPropertyRelative("portalId").stringValue = portal;
            element.FindPropertyRelative("bidirectional").boolValue = true;
        }

        private static void MakeLight()
        {
            var lightObject = new GameObject("Directional Light");
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
        }
    }
}
