using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Blacksite7.WorldBuilder;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Blacksite7.WorldBuilder.Editor
{
    /// <summary>One-click generation of the standalone WORLD BUILDER demonstration scene.</summary>
    public static class WorldBuilderSceneGenerator
    {
        private const string Base = "Assets/_BLACKSITE/WorldBuilder";
        private static Transform root;
        private static Transform staticRoot, corridorRoot, doorRoot, volumeRoot, coverRoot;
        private static Transform interceptRoot, lightingRoot, waypointRoot, propsRoot;
        private static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
        private const float WallHeight = 3.5f;
        private const float WallThickness = 0.22f;
        private const float PassageWidth = 3.4f;

        private struct RoomDef
        {
            public string id, label;
            public float x, z, w, d;
            public Color floorColor;
            public RoomDef(string id, string label, float x, float z, float w, float d, Color floorColor)
            {
                this.id = id; this.label = label; this.x = x; this.z = z; this.w = w; this.d = d;
                this.floorColor = floorColor;
            }
        }

        private static readonly RoomDef[] Rooms =
        {
            new RoomDef("ENTRY", "LOADING BAY", -29, 0, 10, 10, new Color(.19f,.24f,.28f)),
            new RoomDef("HUB", "CENTRAL HUB", -12, 0, 12, 12, new Color(.15f,.24f,.28f)),
            new RoomDef("ACOUSTIC", "ACOUSTIC LAB", -12, 18, 12, 10, new Color(.10f,.26f,.29f)),
            new RoomDef("SECURITY", "SECURITY OPS", 8, 18, 12, 10, new Color(.30f,.24f,.20f)),
            new RoomDef("MAINT", "MAINTENANCE", 8, 0, 12, 12, new Color(.29f,.28f,.18f)),
            new RoomDef("CORE", "CORE VAULT", 8, -18, 12, 10, new Color(.29f,.14f,.19f)),
        };

        [MenuItem("BLACKSITE-7/1 - Build World Builder Scene")]
        public static void BuildScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EnsureDirectory(Base + "/Generated/Materials");
            EnsureDirectory("Assets/_BLACKSITE/Scenes");
            mats.Clear();
            Material steel = Mat("Steel", new Color(.29f,.34f,.39f));
            Material trim = Mat("TrimDark", new Color(.09f,.12f,.16f));
            Material hall = Mat("HallFloor", new Color(.20f,.26f,.30f));
            Material glowCyan = Mat("CyanAccent", new Color(.05f,.72f,.75f), true);
            Material amber = Mat("AmberAccent", new Color(.98f,.59f,.14f), true);
            Material red = Mat("RedAccent", new Color(.92f,.15f,.16f), true);
            Material lightMetal = Mat("Panel", new Color(.47f,.54f,.56f));
            Material cover = Mat("CoverCrates", new Color(.30f,.39f,.38f));
            Material core = Mat("CoreGlow", new Color(.19f,.90f,.95f), true);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            root = new GameObject("BLACKSITE7_WorldBuilder_Demo").transform;
            staticRoot = Group("World_Static", root);
            corridorRoot = Group("Corridors", root);
            doorRoot = Group("Doors_Dynamic", root);
            volumeRoot = Group("RoomVolumes", root);
            waypointRoot = Group("Nav_Walkability", root);
            coverRoot = Group("CoverPoints", root);
            interceptRoot = Group("InterceptWaypoints", root);
            propsRoot = Group("Props_Static", root);
            Group("Interactables_Integration", root);
            lightingRoot = Group("Lighting", root);
            Group("AgentSpawns", root);

            foreach (RoomDef r in Rooms)
            {
                Transform group = Group(r.id, staticRoot);
                Material floorMat = Mat("Floor_" + r.id, r.floorColor);
                Cube("Floor_" + r.id, group, new Vector3(r.x, -0.18f, r.z), new Vector3(r.w, .36f, r.d), floorMat, true);
                Cube("FloorStripe_" + r.id, group, new Vector3(r.x, .008f, r.z), new Vector3(.12f, .015f, r.d * .66f), r.id == "CORE" ? red : glowCyan, false);
                BuildRoomWalls(r, group, steel);
                Label(r.label, group, new Vector3(r.x, 3.7f, r.z), 0.46f, Color.white);
                // Visual wall pilasters (collider-free) establish a consistent modular kit.
                for (float px = r.x - r.w/2f + 1.5f; px < r.x + r.w/2f - 1; px += 3f)
                    Cube("North_WallRib", group, new Vector3(px, 1.75f, r.z + r.d/2f - .1f), new Vector3(.18f, 3.45f, .32f), trim, false);
                Transform marker = Group(r.id + "_Volume", volumeRoot);
                marker.position = new Vector3(r.x, 1.7f, r.z);
                var col = marker.gameObject.AddComponent<BoxCollider>();
                col.isTrigger = true;
                col.size = new Vector3(r.w-.25f, 3.4f, r.d-.25f);
                var volume = marker.gameObject.AddComponent<RoomVolumeData>();
                volume.roomId = r.id;
                volume.displayName = r.label;
                volume.footprint = new Vector2(r.w, r.d);
                PointLight("Light_" + r.id, new Vector3(r.x, 3.1f, r.z),
                    r.id == "CORE" ? new Color(.37f,.85f,.98f) : (r.id == "MAINT" || r.id == "SECURITY" ? new Color(1f,.73f,.45f) : new Color(.52f,.87f,.96f)),
                    r.id == "CORE" ? 7 : 4, 15);
            }

            // Straight corridors: true 3.4m wide physical space, not lines drawn through solid walls.
            Straight("ENTRY_HUB", -24, -18, 0, false, corridorRoot, hall, steel);
            Straight("HUB_ACOUSTIC", 6, 13, -12, true, corridorRoot, hall, steel);
            Straight("ACOUSTIC_SECURITY", -6, 2, 18, false, corridorRoot, hall, steel);
            Straight("SECURITY_MAINT", 6, 13, 8, true, corridorRoot, hall, steel);
            Straight("HUB_MAINT", -6, 2, 0, false, corridorRoot, hall, steel);
            Straight("MAINT_CORE", -13, -6, 8, true, corridorRoot, hall, steel);
            BuildBypass(hall, steel);

            AddDoor("D01", "ENTRY", "HUB", -21, 0, false, lightMetal, glowCyan);
            AddDoor("D02", "HUB", "ACOUSTIC", -12, 9.5f, true, lightMetal, glowCyan);
            AddDoor("D03", "ACOUSTIC", "SECURITY", -2, 18, false, lightMetal, glowCyan);
            AddDoor("D04", "SECURITY", "MAINT", 8, 9.5f, true, lightMetal, amber);
            AddDoor("D05", "HUB", "MAINT", -2, 0, false, lightMetal, amber);
            AddDoor("D06", "MAINT", "CORE", 8, -9.5f, true, lightMetal, red);
            AddDoor("D07", "HUB", "CORE", -12, -8, true, lightMetal, red);

            // Architectural decoration is deliberately simple. Keep AI lanes open.
            Props(steel, trim, amber, glowCyan, red, cover, core);
            AddCover("CV_HUB_01", -15, 2);
            AddCover("CV_HUB_02", -9, -3);
            AddCover("CV_SEC_01", 5.4f, 20.5f);
            AddCover("CV_SEC_02", 10.0f, 20.5f);
            AddCover("CV_SEC_03", 11.0f, 15.5f);
            AddCover("CV_SEC_04", 5.3f, 15.1f);
            AddIntercept("IP_CORE_NORTH", 8, -13.7f);
            AddIntercept("IP_CORE_WEST", 2.8f, -18);
            AddIntercept("IP_HUB_EAST", -6.9f, 0);
            AddWaypoint("WP_ENTRY", -29, 0);
            AddWaypoint("WP_HUB", -12, 0);
            AddWaypoint("WP_ACOUSTIC", -12, 18);
            AddWaypoint("WP_SECURITY", 8, 18);
            AddWaypoint("WP_MAINT", 8, 0);
            AddWaypoint("WP_CORE", 8, -18);
            AddWaypoint("WP_BYPASS_TURN", -12, -18);

            // Demo-only controller, never the team's production player.
            Transform demo = Group("Demo_Only_DELETE_AFTER_INTEGRATION", root);
            Transform playerSpawn = Group("PlayerSpawn", root);
            playerSpawn.position = new Vector3(-31f, .1f, 0);
            GameObject player = new GameObject("DemoPlayer_FPS");
            player.transform.SetParent(demo);
            player.transform.position = new Vector3(-31f, 1.1f, 0);
            player.transform.rotation = Quaternion.Euler(0, 90, 0);
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.9f;
            cc.radius = .34f;
            cc.center = new Vector3(0, .02f, 0);
            cc.stepOffset = .25f;
            GameObject camObj = new GameObject("FirstPersonCamera");
            camObj.transform.SetParent(player.transform, false);
            camObj.transform.localPosition = new Vector3(0, .65f, 0);
            var camera = camObj.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.nearClipPlane = .05f;
            camObj.AddComponent<AudioListener>();
            var fps = player.AddComponent<BeginnerFPSController>();
            fps.cameraPivot = camObj.transform;

            GameObject echo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            echo.name = "ECHO_Prototype_Stationary";
            echo.transform.SetParent(demo);
            echo.transform.position = new Vector3(-12, 1f, 19.5f);
            echo.GetComponent<Renderer>().sharedMaterial = glowCyan;
            echo.GetComponent<Collider>().enabled = false;
            var brain = echo.AddComponent<EchoAcousticPrototype>();
            brain.demoPlayer = player.transform;
            brain.echoRoomId = "ACOUSTIC";
            Cube("ECHO_DishLeft", echo.transform, new Vector3(-.55f, .75f, 0f), new Vector3(.5f,.15f,.6f), glowCyan, false, true);
            Cube("ECHO_DishRight", echo.transform, new Vector3(.55f, .75f, 0f), new Vector3(.5f,.15f,.6f), glowCyan, false, true);

            RenderSettings.ambientLight = new Color(.31f,.37f,.44f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            GameObject sun = new GameObject("FillDirectionalLight");
            sun.transform.SetParent(lightingRoot);
            sun.transform.rotation = Quaternion.Euler(45, -25, 0);
            var directional = sun.AddComponent<Light>();
            directional.type = LightType.Directional;
            directional.intensity = .65f;
            directional.color = new Color(.72f,.85f,1f);

            SceneView.lastActiveSceneView?.Frame(new Bounds(new Vector3(-11, 0, 1), new Vector3(60, 12, 65)), false);
            EditorSceneManager.MarkSceneDirty(scene);
            string scenePath = "Assets/_BLACKSITE/Scenes/WorldBuilder_Demo.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("BLACKSITE-7 World Builder demo created at " + scenePath + ". Press Play, click the Game view, and walk with WASD.");
            EditorUtility.DisplayDialog("BLACKSITE-7 ready", "WorldBuilder_Demo scene has been generated and saved.\n\nPress Play, click inside the Game window and use WASD.\nN = make demo noise; T = toggle D02 acoustic state; F1 = debug.\n\nNEXT: Install AI Navigation and bake NavMesh (menu BLACKSITE-7/2).", "OK");
        }

        private static void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }

        private static Material Mat(string name, Color color, bool emission = false)
        {
            if (mats.TryGetValue(name, out var existing)) return existing;
            string path = Base + "/Generated/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                if (shader == null) shader = Shader.Find("Unlit/Color");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = color;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .28f);
            if (emission)
            {
                material.EnableKeyword("_EMISSION");
                if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", color * 1.5f);
            }
            EditorUtility.SetDirty(material);
            mats[name] = material;
            return material;
        }

        private static Transform Group(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            return go.transform;
        }

        private static GameObject Cube(string name, Transform parent, Vector3 center, Vector3 size, Material material, bool collider = true, bool local = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            if (local) go.transform.localPosition = center;
            else go.transform.position = center;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material;
            var col = go.GetComponent<BoxCollider>();
            if (!collider && col != null) UnityEngine.Object.DestroyImmediate(col);
            if (collider) go.isStatic = true;
            return go;
        }

        private static void Label(string s, Transform parent, Vector3 position, float size, Color tint)
        {
            GameObject g = new GameObject("Label_" + s.Replace(' ', '_'));
            g.transform.SetParent(parent);
            g.transform.position = position;
            g.transform.rotation = Quaternion.Euler(90, 0, 0);
            var t = g.AddComponent<TextMesh>();
            t.text = s;
            t.fontSize = 60;
            t.characterSize = size / 12f;
            t.color = tint;
            t.anchor = TextAnchor.MiddleCenter;
            t.alignment = TextAlignment.Center;
        }

        private static void BuildRoomWalls(RoomDef r, Transform roomGroup, Material wall)
        {
            // Doors break wall segments, making physically valid openings.
            var north = new List<float>(); var south = new List<float>();
            var east = new List<float>(); var west = new List<float>();
            switch (r.id)
            {
                case "ENTRY": east.Add(0); break;
                case "HUB": west.Add(0); north.Add(-12); east.Add(0); south.Add(-12); break;
                case "ACOUSTIC": south.Add(-12); east.Add(18); break;
                case "SECURITY": west.Add(18); south.Add(8); break;
                case "MAINT": west.Add(0); north.Add(8); south.Add(8); break;
                case "CORE": north.Add(8); west.Add(-18); break;
            }
            WallSegments(r.id + "_N", roomGroup, true, r.z + r.d/2, r.x-r.w/2, r.x+r.w/2, north, wall);
            WallSegments(r.id + "_S", roomGroup, true, r.z - r.d/2, r.x-r.w/2, r.x+r.w/2, south, wall);
            WallSegments(r.id + "_E", roomGroup, false, r.x + r.w/2, r.z-r.d/2, r.z+r.d/2, east, wall);
            WallSegments(r.id + "_W", roomGroup, false, r.x - r.w/2, r.z-r.d/2, r.z+r.d/2, west, wall);
        }

        private static void WallSegments(string name, Transform p, bool horizontal, float fixedCoord, float low, float high, List<float> openingCenters, Material m)
        {
            openingCenters.Sort();
            float cursor = low;
            foreach (float opening in openingCenters)
            {
                float edge = opening - PassageWidth/2;
                WallPiece(name, p, horizontal, fixedCoord, cursor, edge, m);
                cursor = opening + PassageWidth/2;
            }
            WallPiece(name, p, horizontal, fixedCoord, cursor, high, m);
        }

        private static void WallPiece(string name, Transform p, bool horizontal, float constant, float from, float to, Material m)
        {
            float len = to - from;
            if (len <= .01f) return;
            Vector3 c = horizontal ? new Vector3((from+to)/2, WallHeight/2, constant) : new Vector3(constant, WallHeight/2, (from+to)/2);
            Vector3 s = horizontal ? new Vector3(len, WallHeight, WallThickness) : new Vector3(WallThickness, WallHeight, len);
            Cube(name + "_Segment", p, c, s, m);
        }

        private static void Straight(string name, float from, float to, float axis, bool vertical, Transform parent, Material floor, Material wall)
        {
            Transform g = Group(name, parent);
            float mid = (from+to)/2, length = Mathf.Abs(to-from);
            Vector3 center = vertical ? new Vector3(axis, -.18f, mid) : new Vector3(mid, -.18f, axis);
            Vector3 dimensions = vertical ? new Vector3(PassageWidth,.36f,length) : new Vector3(length,.36f,PassageWidth);
            Cube("Floor", g, center, dimensions, floor);
            if (vertical)
            {
                Cube("Wall_W", g, new Vector3(axis-PassageWidth/2,WallHeight/2,mid), new Vector3(WallThickness,WallHeight,length), wall);
                Cube("Wall_E", g, new Vector3(axis+PassageWidth/2,WallHeight/2,mid), new Vector3(WallThickness,WallHeight,length), wall);
            }
            else
            {
                Cube("Wall_N", g, new Vector3(mid,WallHeight/2,axis+PassageWidth/2), new Vector3(length,WallHeight,WallThickness), wall);
                Cube("Wall_S", g, new Vector3(mid,WallHeight/2,axis-PassageWidth/2), new Vector3(length,WallHeight,WallThickness), wall);
            }
        }

        private static void BuildBypass(Material floor, Material wall)
        {
            Transform g = Group("HUB_CORE_BYPASS", corridorRoot);
            // L-shaped, physically open junction. Inner walls end before the corner.
            Cube("Floor_NorthSouth", g, new Vector3(-12,-.18f,-12), new Vector3(PassageWidth,.36f,12), floor);
            Cube("Floor_EastWest", g, new Vector3(-5,-.18f,-18), new Vector3(14,.36f,PassageWidth), floor);
            Cube("OuterWestWall", g, new Vector3(-13.7f,WallHeight/2,-12.85f), new Vector3(WallThickness,WallHeight,13.7f), wall);
            Cube("InnerEastWall", g, new Vector3(-10.3f,WallHeight/2,-11.15f), new Vector3(WallThickness,WallHeight,10.3f), wall);
            Cube("InnerNorthWall", g, new Vector3(-4.15f,WallHeight/2,-16.3f), new Vector3(12.3f,WallHeight,WallThickness), wall);
            Cube("OuterSouthWall", g, new Vector3(-5.85f,WallHeight/2,-19.7f), new Vector3(15.7f,WallHeight,WallThickness), wall);
        }

        private static void AddDoor(string id, string a, string b, float x, float z, bool northSouthTravel, Material frame, Material color)
        {
            // This is a passable FRAME only; teammate's physics door replaces it.
            Transform g = Group(id + "_" + a + "_" + b + "_Placeholder", doorRoot);
            g.position = new Vector3(x, 0, z);
            var meta = g.gameObject.AddComponent<DoorPortalData>();
            meta.doorId = id;
            meta.roomA = a;
            meta.roomB = b;
            float offset = PassageWidth / 2 + .1f;
            if (northSouthTravel)
            {
                Cube("Frame_Left", g, new Vector3(x-offset, 1.3f, z), new Vector3(.25f, 2.6f, .3f), frame);
                Cube("Frame_Right", g, new Vector3(x+offset, 1.3f, z), new Vector3(.25f, 2.6f, .3f), frame);
                Cube("Frame_Top", g, new Vector3(x, 2.8f, z), new Vector3(PassageWidth+.5f, .4f, .3f), color, false);
            }
            else
            {
                Cube("Frame_Left", g, new Vector3(x, 1.3f, z-offset), new Vector3(.3f, 2.6f, .25f), frame);
                Cube("Frame_Right", g, new Vector3(x, 1.3f, z+offset), new Vector3(.3f, 2.6f, .25f), frame);
                Cube("Frame_Top", g, new Vector3(x, 2.8f, z), new Vector3(.3f,.4f,PassageWidth+.5f), color, false);
            }
            Label(id, g, new Vector3(x, 3.3f, z), .30f, Color.white);
        }

        private static void Props(Material steel, Material dark, Material amber, Material cyan, Material red, Material crate, Material core)
        {
            // Keep critical routes, spawns and doorways clear.
            Cube("Loading_Cargo_A", propsRoot, new Vector3(-32, .65f, -3), new Vector3(1.5f,1.3f,1.5f), crate);
            Cube("Loading_Cargo_B", propsRoot, new Vector3(-32.5f, .6f, 3), new Vector3(1.4f,1.2f,1.4f), crate);
            Cube("Hub_Console", propsRoot, new Vector3(-11, .55f, 2.0f), new Vector3(1.8f,1.1f,1), steel);
            Cube("Hub_Screen", propsRoot, new Vector3(-11,1.15f,2f), new Vector3(1.3f,.15f,.85f), cyan, false);
            Cube("Acoustic_SensorBank", propsRoot, new Vector3(-16.8f,.9f,20.5f), new Vector3(.7f,1.8f,2.7f), steel);
            Cube("Acoustic_Panel", propsRoot, new Vector3(-16.35f,1.5f,20.5f), new Vector3(.1f,.65f,1.9f), cyan, false);
            Cube("Security_Desk_A", propsRoot, new Vector3(4.1f,.7f,20), new Vector3(1.2f,1.4f,1.8f), crate);
            Cube("Security_Desk_B", propsRoot, new Vector3(11.5f,.7f,20), new Vector3(1.2f,1.4f,1.8f), crate);
            Cube("Security_Locker_C", propsRoot, new Vector3(12.2f,.9f,15.5f), new Vector3(.8f,1.8f,1.5f), crate);
            Cube("Security_Locker_D", propsRoot, new Vector3(4.1f,.9f,15.1f), new Vector3(.8f,1.8f,1.5f), crate);
            Cube("Security_Status", propsRoot, new Vector3(10.6f,2.1f,22.5f), new Vector3(2.2f,1f,.2f), red, false);
            Cube("Maint_Workbench", propsRoot, new Vector3(11.6f,.55f,3.3f), new Vector3(2.6f,1.1f,1.3f), steel);
            Cube("Maint_Status", propsRoot, new Vector3(11.6f,1.17f,3.3f), new Vector3(1.7f,.08f,.9f), amber, false);
            Cube("Core_Pedestal", propsRoot, new Vector3(8,.6f,-18), new Vector3(2.2f,1.2f,2.2f), dark);
            GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orb.name = "CorePickup_Visual_Only";
            orb.transform.SetParent(propsRoot);
            orb.transform.position = new Vector3(8,1.6f,-18);
            orb.transform.localScale = new Vector3(1.25f,1.25f,1.25f);
            orb.GetComponent<Renderer>().sharedMaterial = core;
            UnityEngine.Object.DestroyImmediate(orb.GetComponent<Collider>());
            // Empty extraction marker only; win/loss interaction comes from another teammate.
            var extract = Group("Extraction", root);
            extract.position = new Vector3(-32,0,-1);
        }

        private static void AddCover(string id, float x, float z)
        {
            Transform p = Group(id, coverRoot);
            p.position = new Vector3(x,0,z);
            p.gameObject.AddComponent<CoverPointData>().coverId = id;
        }
        private static void AddIntercept(string id, float x, float z)
        {
            Transform p = Group(id, interceptRoot);
            p.position = new Vector3(x,0,z);
            p.gameObject.AddComponent<InterceptPointData>().pointId = id;
        }
        private static void AddWaypoint(string id, float x, float z)
        {
            Transform p = Group(id, waypointRoot);
            p.position = new Vector3(x,0,z);
        }
        private static void PointLight(string name, Vector3 position, Color color, float intensity, float range)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(lightingRoot);
            go.transform.position = position;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
        }

        [MenuItem("BLACKSITE-7/2 - Add and Bake NavMesh (AI Navigation package)")]
        public static void TryBakeNavMesh()
        {
            // Reflection avoids an uncompilable project when AI Navigation isn't installed.
            Type surfaceType = Type.GetType("Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation");
            if (surfaceType == null)
            {
                EditorUtility.DisplayDialog("AI Navigation missing", "Install 'AI Navigation' from Window > Package Manager > Unity Registry, then try this menu command again.\n\nNote: Your team's assessed custom A* remains separate from this NavMesh reference.", "OK");
                return;
            }
            GameObject owner = GameObject.Find("Nav_Walkability");
            if (owner == null)
            {
                EditorUtility.DisplayDialog("Build world first", "Use BLACKSITE-7 > 1 - Build World Builder Scene first.", "OK");
                return;
            }
            Component component = owner.GetComponent(surfaceType);
            if (component == null) component = owner.AddComponent(surfaceType);
            MethodInfo build = surfaceType.GetMethod("BuildNavMesh", BindingFlags.Instance | BindingFlags.Public);
            if (build == null)
            {
                EditorUtility.DisplayDialog("Cannot bake", "NavMeshSurface API differs in this package version. Select Nav_Walkability > NavMeshSurface and click Bake in the Inspector.", "OK");
                return;
            }
            try
            {
                build.Invoke(component, null);
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                EditorSceneManager.SaveOpenScenes();
                EditorUtility.DisplayDialog("NavMesh build requested", "A NavMeshSurface bake was requested. Select Nav_Walkability to inspect it and check the Navigation overlay for blue walkable areas.\n\nIf paths are missing, adjust Surface settings and bake manually.", "OK");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorUtility.DisplayDialog("Bake failed", "Select Nav_Walkability, inspect NavMeshSurface settings and bake manually. See Console for error details.", "OK");
            }
        }
    }
}
