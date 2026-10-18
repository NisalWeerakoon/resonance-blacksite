using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Blacksite.Agents.Trace;

namespace Blacksite.Editor
{
    public static class BlacksiteWorldIntegration
    {
        private const string ScenePath = "Assets/Scenes/final.unity";
        private const string IntegrationPath =
            "Assets/Blacksite/Scenes/Integration/WorldCharacterIntegration.unity";

        [MenuItem("BLACKSITE/Inspect world spawn markers")]
        public static void Inspect()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var spawns = GameObject.Find("AgentSpawns");
            if (spawns == null) throw new System.Exception("AgentSpawns root not found.");
            Debug.Log($"BLACKSITE_WORLD_SPAWNS scene={scene.path} count={spawns.transform.childCount}");
            foreach (Transform marker in spawns.transform)
                Debug.Log($"BLACKSITE_WORLD_SPAWN name={marker.name} position={marker.position}");
            foreach (var root in scene.GetRootGameObjects())
                foreach (var point in root.GetComponentsInChildren<Transform>(true))
                    if (point.name.Contains("Spawn") || point.name.Contains("ECHO") ||
                        point.name.Contains("Nav_Walkability") || point.name.Contains("RoomVolumes"))
                        Debug.Log($"BLACKSITE_WORLD_OBJECT name={point.name} parent={point.parent?.name} position={point.position}");
            var rooms = GameObject.Find("RoomVolumes");
            if (rooms != null)
                foreach (Transform room in rooms.transform)
                    Debug.Log($"BLACKSITE_WORLD_ROOM name={room.name} position={room.position} scale={room.localScale}");
            foreach (var root in scene.GetRootGameObjects())
                foreach (var point in root.GetComponentsInChildren<Transform>(true))
                {
                    if (!point.name.StartsWith("D0", System.StringComparison.Ordinal)) continue;
                    Debug.Log($"BLACKSITE_WORLD_DOOR name={point.name} position={point.position} scale={point.lossyScale}");
                    foreach (var collider in point.GetComponentsInChildren<Collider>(true))
                        Debug.Log($"BLACKSITE_WORLD_DOOR_COLLIDER name={collider.name} size={collider.bounds.size}");
                    var left = point.Find("Frame_Left")?.GetComponent<Collider>();
                    var right = point.Find("Frame_Right")?.GetComponent<Collider>();
                    if (left != null && right != null)
                    {
                        bool acrossX = Mathf.Abs(left.bounds.center.x - right.bounds.center.x) >
                            Mathf.Abs(left.bounds.center.z - right.bounds.center.z);
                        float gap = acrossX
                            ? Mathf.Abs(left.bounds.center.x - right.bounds.center.x) -
                              left.bounds.extents.x - right.bounds.extents.x
                            : Mathf.Abs(left.bounds.center.z - right.bounds.center.z) -
                              left.bounds.extents.z - right.bounds.extents.z;
                        Debug.Log($"BLACKSITE_WORLD_DOOR_GAP name={point.name} width={gap:F2} axis={(acrossX ? "X" : "Z")}");
                    }
                }
        }

        [MenuItem("BLACKSITE/Build integrated world character scene")]
        public static void Build()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var spawnRoot = GameObject.Find("AgentSpawns");
            if (spawnRoot == null) throw new System.Exception("AgentSpawns root not found.");
            var placed = new GameObject("IntegratedCharacters");
            placed.transform.SetParent(spawnRoot.transform, false);
            var names = new[] { "ECHO", "BASTION", "RIFT", "TRACE" };
            var roomNames = new[] { "ACOUSTIC_Volume", "SECURITY_Volume",
                "MAINT_Volume", "CORE_Volume" };
            for (int i = 0; i < names.Length; i++)
            {
                var room = GameObject.Find(roomNames[i]);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    $"Assets/Blacksite/Prefabs/Agents/{names[i]}.prefab");
                if (room == null || prefab == null)
                    throw new System.Exception($"Missing {names[i]} room marker or prefab.");
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.name = names[i] + "_World_Visual";
                instance.transform.SetParent(placed.transform);
                var position = room.transform.position;
                instance.transform.position = new Vector3(position.x, 0.05f, position.z);
                if (names[i] == "TRACE")
                {
                    var player = GameObject.Find("DemoPlayer_FPS");
                    var sensor = instance.GetComponent<TraceSightSensor>();
                    if (player != null && sensor != null)
                    {
                        var data = new SerializedObject(sensor);
                        data.FindProperty("playerTarget").objectReferenceValue = player.transform;
                        data.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
                Debug.Log($"BLACKSITE_WORLD_PLACEMENT name={names[i]} room={room.name} position={instance.transform.position}");
            }
            var prototype = GameObject.Find("ECHO_Prototype_Stationary");
            if (prototype != null) prototype.SetActive(false);
            EditorSceneManager.SaveScene(scene, IntegrationPath, true);
            AssetDatabase.SaveAssets();
            Debug.Log($"BLACKSITE_WORLD_INTEGRATION scene={IntegrationPath} characters={names.Length} originalPreserved={ScenePath}");
        }

        [MenuItem("BLACKSITE/Audit integrated world scene")]
        public static void Audit()
        {
            var scene = EditorSceneManager.OpenScene(IntegrationPath, OpenSceneMode.Single);
            var placed = GameObject.Find("IntegratedCharacters");
            if (placed == null || placed.transform.childCount != 4)
                throw new System.Exception("Integrated scene needs four character instances.");
            int objects = 0;
            int missingScripts = 0;
            int colliders = 0;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var point in root.GetComponentsInChildren<Transform>(true))
                {
                    objects++;
                    missingScripts += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(point.gameObject);
                    colliders += point.GetComponents<Collider>().Length;
                }
            foreach (Transform character in placed.transform)
            {
                var meshes = character.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                if (!PrefabUtility.IsAnyPrefabInstanceRoot(character.gameObject) ||
                    meshes.Length == 0 ||
                    character.GetComponent<Collider>() == null)
                    throw new System.Exception($"Incomplete integrated character: {character.name}");
                var bounds = meshes[0].bounds;
                foreach (var mesh in meshes) bounds.Encapsulate(mesh.bounds);
                Debug.Log($"BLACKSITE_WORLD_CHARACTER name={character.name} visualSize={bounds.size} collider={character.GetComponent<Collider>().GetType().Name}");
            }
            if (missingScripts > 0 || objects < 500)
                throw new System.Exception("Integrated scene has missing scripts or geometry.");
            Debug.Log($"BLACKSITE_WORLD_AUDIT scene={scene.path} objects={objects} colliders={colliders} missingScripts={missingScripts} characters={placed.transform.childCount}");
        }
    }
}
