using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Blacksite.Agents.Bastion;
using Blacksite.Agents.Echo;
using Blacksite.Agents.Movement;
using Blacksite.Agents.Rift;
using Blacksite.Agents.Trace;
using Blacksite.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Blacksite.Editor
{
    public static class BlacksiteSubmissionBuilder
    {
        private const string Source = "Assets/Blacksite/Scenes/Integration/WorldCharacterIntegration.unity";
        private const string Target = "Assets/Blacksite/Scenes/Integration/SubmissionPlayable.unity";

        [MenuItem("BLACKSITE/Build submission playable scene")]
        public static void Build()
        {
            var scene = EditorSceneManager.OpenScene(Source, OpenSceneMode.Single);
            var runtime = new GameObject("BLACKSITE_Submission_Runtime");
            Transform player = Find("DemoPlayer_FPS");
            Transform echoRoot = Find("ECHO_World_Visual");
            Transform bastionRoot = Find("BASTION_World_Visual");
            Transform riftRoot = Find("RIFT_World_Visual");
            Transform traceRoot = Find("TRACE_World_Visual");
            Transform core = Find("CorePickup_Visual_Only");
            var graph = Find("AgentWaypointGraph").GetComponent<TraceWaypointGraph>();
            if (player == null || echoRoot == null || bastionRoot == null || riftRoot == null ||
                traceRoot == null || core == null || graph == null)
                throw new Exception("Required player, character, core or graph missing from integration scene.");

            ConfigureGraph(graph);
            var door02 = AddDoor(runtime.transform, "D02", new Vector3(-12f, 1.2f, 9.5f));
            var door06 = AddDoor(runtime.transform, "D06", new Vector3(8f, 1.2f, -9.5f));
            var choke = new GameObject("D05_Barricade_Choke");
            choke.transform.SetParent(runtime.transform);
            choke.transform.position = new Vector3(-2f, 1f, 0f);
            var chokeCollider = choke.AddComponent<BoxCollider>();
            chokeCollider.size = new Vector3(2.7f, 2.2f, 2.7f);
            chokeCollider.isTrigger = true;
            var barrierPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Blacksite/Prefabs/Systems/MovableBarricade_D02.prefab");
            if (barrierPrefab == null) throw new Exception("Systems barricade prefab missing");
            var barrier = (GameObject)PrefabUtility.InstantiatePrefab(barrierPrefab);
            barrier.name = "D05_Movable_Barricade";
            barrier.transform.SetParent(runtime.transform);
            barrier.transform.position = choke.transform.position;
            var barricade = barrier.GetComponent<MovableBarricade>();
            barricade.ConfigureSandbox("D05", chokeCollider);
            MonoBehaviour[] portals = { door02, door06, barricade };

            var world = runtime.AddComponent<EchoWorldContext>();
            runtime.AddComponent<EchoWorldSignalBridge>();
            var echoMotor = EnsureMotor(echoRoot, graph);
            EnsureNavigationCollider(echoRoot, .35f, 1.8f);
            var echo = echoRoot.gameObject.AddComponent<EchoBrain>();
            SetReference(echo, "world", world);
            SetReference(echo, "sharedMotor", echoMotor);
            AddSight(echoRoot, echo, player);

            var coverPoints = AddCovers(runtime.transform);
            var planner = graph.gameObject.AddComponent<ExistingGraphPathPlanner>();
            planner.ConfigureSandbox(graph);
            var bastionMotor = EnsureMotor(bastionRoot, graph);
            EnsureNavigationCollider(bastionRoot, .55f, 2f);
            var bastion = bastionRoot.gameObject.AddComponent<BastionBrain>();
            var eye = new GameObject("BASTION_Eye");
            eye.transform.SetParent(bastionRoot, false);
            eye.transform.localPosition = new Vector3(0f, 1.8f, .15f);
            bastion.ConfigureSandbox(eye.transform, player, coverPoints, planner, portals);
            bastion.enabled = false;
            bastion.enabled = true;
            bastionRoot.gameObject.AddComponent<BastionRouteMotorBridge>();
            AddPortals(bastionRoot, bastion, portals);

            var rift = riftRoot.GetComponent<RiftBrain>();
            var riftMotor = EnsureMotor(riftRoot, graph);
            EnsureNavigationCollider(riftRoot, .35f, 1.8f);
            SetReference(rift, "motor", riftMotor);
            SetReference(rift, "flankPoint", Find("SUB_WP_SECURITY"));
            AddPortals(riftRoot, rift, portals);

            var traceMotor = EnsureMotor(traceRoot, graph);
            EnsureNavigationCollider(traceRoot, .35f, 1.8f);
            var trace = traceRoot.GetComponent<TraceBrain>();
            SetReference(trace, "graph", graph);
            var intercepts = AddIntercepts(runtime.transform);
            SetArray(trace, "interceptPoints", intercepts);
            traceRoot.gameObject.AddComponent<TraceRouteMotorBridge>();
            AddPortals(traceRoot, trace, portals);

            var camera = player.GetComponentInChildren<Camera>();
            var decoyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Blacksite/Prefabs/Systems/ThrowableDecoy.prefab");
            if (camera == null || decoyPrefab == null) throw new Exception("Player camera or decoy prefab missing");
            player.gameObject.AddComponent<PlayerSystemsInteractor>()
                .ConfigureSandbox(camera, decoyPrefab.GetComponent<ThrowableDecoy>());
            player.gameObject.AddComponent<PlayerFootstepEmitter>();
            player.gameObject.AddComponent<PlayerPulseWeapon>();
            player.gameObject.AddComponent<PlayerVitals>();
            AddCarbineViewmodel(camera.transform);

            var extraction = new GameObject("Extraction_Point_ENTRY");
            extraction.transform.SetParent(runtime.transform);
            extraction.transform.position = new Vector3(-29f, 1f, 0f);
            var relays = new[] {
                AddRelay(runtime.transform, "ACOUSTIC", new Vector3(-9f, 1f, 18f)),
                AddRelay(runtime.transform, "SECURITY", new Vector3(11f, 1f, 18f)),
                AddRelay(runtime.transform, "MAINT", new Vector3(11f, 1f, 0f))
            };
            var mission = runtime.AddComponent<BlacksiteMission>();
            var agents = new[] { echoRoot, bastionRoot, riftRoot, traceRoot };
            var health = new SubmissionAgentHealth[agents.Length];
            for (int i = 0; i < agents.Length; i++)
            {
                health[i] = agents[i].gameObject.AddComponent<SubmissionAgentHealth>();
                AddAgentIdentifier(agents[i], new[] { "ECHO", "BASTION", "RIFT", "TRACE" }[i],
                    new[] { Color.cyan, new Color(1f, .45f, .1f), Color.red, Color.magenta }[i]);
            }
            mission.Configure(player, core.gameObject, extraction.transform, health, relays);
            runtime.AddComponent<SubmissionCombatDirector>().Configure(
                player.GetComponent<PlayerVitals>(), health, bastion);
            runtime.AddComponent<BlacksiteGameFlow>().Configure(mission);

            int staleComponents = 0;
            foreach (Transform item in UnityEngine.Object.FindObjectsByType<Transform>())
            {
                staleComponents += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(item.gameObject);
                var components = new SerializedObject(item.gameObject).FindProperty("m_Component");
                if (components == null) continue;
                for (int index = components.arraySize - 1; index >= 0; index--)
                {
                    var slot = components.GetArrayElementAtIndex(index)
                        .FindPropertyRelative("component");
                    if (slot == null || slot.objectReferenceValue != null) continue;
                    var owner = new SerializedObject(item.gameObject);
                    owner.FindProperty("m_Component").DeleteArrayElementAtIndex(index);
                    owner.ApplyModifiedPropertiesWithoutUndo();
                    staleComponents++;
                }
            }

            EditorSceneManager.SaveScene(scene, Target);
            // Older World Builder data components contain local script file IDs, not
            // portable GUID references. Unity can display them in the editor but
            // reports missing scripts when the scene is loaded in Play Mode.
            int legacyData = RemoveNonportableSceneScripts(Target);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Target, true) };
            AssetDatabase.SaveAssets();
            Debug.Log($"BLACKSITE_SUBMISSION_BUILT scene={Target} agents=4 doors=2 barricades=1 covers=3 graphNodes=12 removedStaleScripts={staleComponents} removedNonportableData={legacyData}");
        }

        private static int RemoveNonportableSceneScripts(string path)
        {
            string yaml = File.ReadAllText(path);
            var blocks = Regex.Matches(yaml, @"(?ms)^--- !u!114 &(?<id>\d+)\r?\n.*?(?=^--- !u!|\z)");
            int removed = 0;
            var ids = new List<string>();
            for (int i = blocks.Count - 1; i >= 0; i--)
            {
                var block = blocks[i];
                if (!Regex.IsMatch(block.Value, @"(?m)^  m_Script: \{fileID: [1-9]\d+\}\r?$")) continue;
                string id = block.Groups["id"].Value;
                yaml = yaml.Remove(block.Index, block.Length);
                ids.Add(id);
                removed++;
            }
            if (removed > 0)
            {
                foreach (string id in ids)
                    yaml = Regex.Replace(yaml, @"(?m)^  - component: \{fileID: " + id + @"\}\r?\n", "");
                File.WriteAllText(path, yaml);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
            return removed;
        }

        private static Transform Find(string name)
        {
            foreach (Transform transform in UnityEngine.Object.FindObjectsByType<Transform>())
                if (transform.name == name) return transform;
            return null;
        }

        private static void AddCarbineViewmodel(Transform camera)
        {
            var root = new GameObject("Pulse_Carbine_Viewmodel");
            root.transform.SetParent(camera, false);
            var shell = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Blacksite/Prefabs/Systems/Systems_Grey.mat");
            var glow = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/_BLACKSITE/WorldBuilder/DecorGenerated/Materials/M_EmissiveCyan.mat");
            ViewPart("Receiver", PrimitiveType.Cube, root.transform,
                new Vector3(.37f, -.31f, .72f), new Vector3(.22f, .19f, .54f), shell);
            ViewPart("Barrel", PrimitiveType.Cylinder, root.transform,
                new Vector3(.37f, -.28f, 1.05f), new Vector3(.075f, .29f, .075f), shell)
                .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ViewPart("EnergyCell", PrimitiveType.Cube, root.transform,
                new Vector3(.37f, -.19f, .71f), new Vector3(.13f, .035f, .30f), glow);
            ViewPart("Muzzle", PrimitiveType.Sphere, root.transform,
                new Vector3(.37f, -.28f, 1.33f), new Vector3(.11f, .11f, .11f), glow);
            ViewPart("Grip", PrimitiveType.Cube, root.transform,
                new Vector3(.37f, -.48f, .53f), new Vector3(.12f, .27f, .18f), shell);
        }

        private static GameObject ViewPart(string name, PrimitiveType type, Transform parent,
            Vector3 position, Vector3 scale, Material material)
        {
            var part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            if (material != null) part.GetComponent<Renderer>().sharedMaterial = material;
            var collider = part.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
            return part;
        }

        private static void AddAgentIdentifier(Transform agent, string label, Color tint)
        {
            var marker = new GameObject(label + "_Identity");
            marker.transform.SetParent(agent, false);
            marker.transform.localPosition = new Vector3(0f, 2.7f, 0f);
            var text = marker.AddComponent<TextMesh>();
            text.text = label;
            text.fontSize = 72;
            text.characterSize = .014f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = tint;
            marker.AddComponent<AgentWorldLabel>();
        }

        private static SubmissionRelay AddRelay(Transform parent, string label, Vector3 position)
        {
            var root = new GameObject("Relay_" + label);
            root.transform.SetParent(parent);
            root.transform.position = position;
            ViewPart("Console", PrimitiveType.Cube, root.transform,
                new Vector3(0f, -.35f, 0f), new Vector3(.8f, .7f, .7f),
                AssetDatabase.LoadAssetAtPath<Material>("Assets/Blacksite/Prefabs/Systems/Systems_Grey.mat"));
            var indicator = ViewPart("Indicator", PrimitiveType.Cube, root.transform,
                new Vector3(0f, .17f, 0f), new Vector3(.65f, .3f, .45f),
                AssetDatabase.LoadAssetAtPath<Material>(
                    "Assets/_BLACKSITE/WorldBuilder/DecorGenerated/Materials/M_EmissiveCyan.mat"));
            var relay = root.AddComponent<SubmissionRelay>();
            relay.Configure(label, indicator.GetComponent<Renderer>());
            var sign = new GameObject("Relay_Sign");
            sign.transform.SetParent(root.transform, false);
            sign.transform.localPosition = new Vector3(0f, 1f, 0f);
            var text = sign.AddComponent<TextMesh>();
            text.text = "[E] " + label + " RELAY";
            text.fontSize = 60;
            text.characterSize = .012f;
            text.anchor = TextAnchor.MiddleCenter;
            text.color = Color.cyan;
            sign.AddComponent<AgentWorldLabel>();
            return relay;
        }

        private static WaypointAgentMotor EnsureMotor(Transform root, TraceWaypointGraph graph)
        {
            var motor = root.GetComponent<WaypointAgentMotor>();
            if (motor == null) motor = root.gameObject.AddComponent<WaypointAgentMotor>();
            SetReference(motor, "graphComponent", graph);
            return motor;
        }

        private static void EnsureNavigationCollider(Transform root, float radius, float height)
        {
            if (root.GetComponent<CharacterController>() != null) return;
            var capsule = root.GetComponent<CapsuleCollider>();
            if (capsule == null) capsule = root.gameObject.AddComponent<CapsuleCollider>();
            capsule.radius = radius;
            capsule.height = height;
            capsule.center = Vector3.up * (height * .5f);
        }

        private static void AddSight(Transform root, MonoBehaviour brain, Transform player)
        {
            var sensor = root.gameObject.AddComponent<AgentSightSensor>();
            SetReference(sensor, "brainComponent", brain);
            SetReference(sensor, "playerTarget", player);
        }

        private static void AddPortals(Transform root, MonoBehaviour brain, MonoBehaviour[] portals)
        {
            var bridge = root.gameObject.AddComponent<AgentPortalStateBridge>();
            SetReference(bridge, "agentBrain", brain);
            SetArray(bridge, "portalComponents", portals);
            bridge.enabled = false;
            bridge.enabled = true;
        }

        private static DoorPortal AddDoor(Transform parent, string id, Vector3 position)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Blacksite/Prefabs/Systems/DoorPortal_D01.prefab");
            if (prefab == null) throw new Exception("Systems door prefab missing");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = id + "_Interactive_Door";
            instance.transform.SetParent(parent);
            instance.transform.position = position;
            var panel = instance.transform.Find("PanelVisual");
            var door = instance.GetComponent<DoorPortal>();
            door.ConfigureSandbox(id, panel.GetComponent<Collider>(), panel.gameObject, true);
            return door;
        }

        private static CoverPoint[] AddCovers(Transform parent)
        {
            var positions = new[] { new Vector3(5.3f, 1f, 15f),
                new Vector3(10.3f, 1f, 16f), new Vector3(8.8f, 1f, 22f) };
            var obstacles = new[] { new Vector3(4.2f, .75f, 15f),
                new Vector3(9.1f, .75f, 16f), new Vector3(7.7f, .75f, 22f) };
            var material = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Blacksite/Prefabs/Systems/Systems_Grey.mat");
            var result = new CoverPoint[3];
            for (int i = 0; i < result.Length; i++)
            {
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = $"BASTION_CoverObstacle_{i + 1}";
                cube.transform.SetParent(parent);
                cube.transform.position = obstacles[i];
                cube.transform.localScale = new Vector3(1.5f, 1.5f, 2f);
                if (material != null) cube.GetComponent<Renderer>().sharedMaterial = material;
                var point = new GameObject($"BASTION_CoverPoint_{i + 1}");
                point.transform.SetParent(parent);
                point.transform.position = positions[i];
                var peek = new GameObject("Peek");
                peek.transform.SetParent(point.transform, false);
                peek.transform.localPosition = new Vector3(0f, .7f, 1f);
                result[i] = point.AddComponent<CoverPoint>();
                result[i].ConfigureSandbox($"B_C0{i + 1}", peek.transform, .6f + i * .1f);
            }
            return result;
        }

        private static TraceInterceptPoint[] AddIntercepts(Transform parent)
        {
            string[] ids = { "HUB", "MAINT", "CORE" };
            Vector3[] positions = { new Vector3(-12f, 1f, 0f),
                new Vector3(8f, 1f, 0f), new Vector3(8f, 1f, -18f) };
            var result = new TraceInterceptPoint[ids.Length];
            for (int i = 0; i < ids.Length; i++)
            {
                var marker = new GameObject("TRACE_Intercept_" + ids[i]);
                marker.transform.SetParent(parent);
                marker.transform.position = positions[i];
                result[i] = marker.AddComponent<TraceInterceptPoint>();
                SetString(result[i], "nodeId", ids[i]);
            }
            return result;
        }

        private static void ConfigureGraph(TraceWaypointGraph graph)
        {
            var nodes = new (string id, Vector3 point)[] {
                ("ENTRY", new Vector3(-29f, 0f, 0f)),
                ("HUB", new Vector3(-12f, 0f, 0f)),
                ("ACOUSTIC", new Vector3(-12f, 0f, 18f)),
                ("SECURITY", new Vector3(8f, 0f, 18f)),
                ("MAINT", new Vector3(8f, 0f, 0f)),
                ("CORE", new Vector3(8f, 0f, -18f)),
                ("HALL", new Vector3(-2f, 0f, 0f)),
                ("BYPASS_TOP", new Vector3(-12f, 0f, -9f)),
                ("BYPASS_CORNER", new Vector3(-12f, 0f, -18f)),
                ("B_C01", new Vector3(5.3f, 0f, 15f)),
                ("B_C02", new Vector3(10.3f, 0f, 16f)),
                ("B_C03", new Vector3(8.8f, 0f, 22f))
            };
            var serialized = new SerializedObject(graph);
            var array = serialized.FindProperty("nodes");
            array.arraySize = nodes.Length;
            for (int i = 0; i < nodes.Length; i++)
            {
                var marker = new GameObject("SUB_WP_" + nodes[i].id);
                marker.transform.SetParent(graph.transform);
                marker.transform.position = nodes[i].point;
                var element = array.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("id").stringValue = nodes[i].id;
                element.FindPropertyRelative("point").objectReferenceValue = marker.transform;
            }
            var links = new (string a, string b, string portal)[] {
                ("ENTRY", "HUB", "D01"), ("HUB", "ACOUSTIC", "D02"),
                ("ACOUSTIC", "SECURITY", "D03"), ("SECURITY", "MAINT", "D04"),
                ("HUB", "HALL", "D05"), ("HALL", "MAINT", null),
                ("MAINT", "CORE", "D06"), ("HUB", "BYPASS_TOP", "D07"),
                ("BYPASS_TOP", "BYPASS_CORNER", null),
                ("BYPASS_CORNER", "CORE", null),
                ("SECURITY", "B_C01", null), ("SECURITY", "B_C02", null),
                ("SECURITY", "B_C03", null)
            };
            var linkArray = serialized.FindProperty("links");
            linkArray.arraySize = links.Length;
            for (int i = 0; i < links.Length; i++)
            {
                var element = linkArray.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("from").stringValue = links[i].a;
                element.FindPropertyRelative("to").stringValue = links[i].b;
                element.FindPropertyRelative("portalId").stringValue = links[i].portal;
                element.FindPropertyRelative("cost").floatValue = 1f;
                element.FindPropertyRelative("bidirectional").boolValue = true;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            graph.Rebuild();
        }

        private static void SetReference(UnityEngine.Object target, string name, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(name);
            if (property == null) throw new Exception($"Missing field {target.name}.{name}");
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetString(UnityEngine.Object target, string name, string value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(name).stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetArray<T>(UnityEngine.Object target, string name, T[] values)
            where T : UnityEngine.Object
        {
            var serialized = new SerializedObject(target);
            var array = serialized.FindProperty(name);
            if (array == null) throw new Exception($"Missing array {target.name}.{name}");
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
