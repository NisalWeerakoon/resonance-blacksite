#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Blacksite7.WorldBuilder.Editor
{
    /// <summary>
    /// Reversible environment polish for BLACKSITE_MainScene or WorldBuilder_Demo.
    /// Only adds objects beneath WB_EXCELLENT_POLISH_DO_NOT_HAND_EDIT; never edits the
    /// existing player, doors, AI, spawn points, physics code or original materials.
    /// Compatible with Unity 6, URP or Built-in rendering; no optional packages required.
    /// </summary>
    public static class WorldBuilderExcellentPolish
    {
        private const string GeneratedRoot = "Assets/_BLACKSITE/WorldBuilder/ExcellentGenerated";
        private const string RootName = "WB_EXCELLENT_POLISH_DO_NOT_HAND_EDIT";
        private const string CeilingName = "07_OPTIONAL_FPS_CEILINGS";
        private static Transform _root;
        private static readonly Dictionary<string, Material> _materials = new Dictionary<string, Material>();

        private struct RoomSpec
        {
            public string Id;
            public Vector3 Center;
            public float W, D;
            public RoomSpec(string id, Vector3 c, float w, float d)
            { Id = id; Center = c; W = w; D = d; }
        }

        [MenuItem("BLACKSITE-7/9 - Add Excellent World Polish")]
        public static void Build()
        {
            if (Application.isPlaying)
            { EditorUtility.DisplayDialog("Stop Play mode", "Stop Play mode before changing scene objects.", "OK"); return; }
            GameObject staticRoot = GameObject.Find("World_Static");
            GameObject corridors = GameObject.Find("Corridors");
            if (staticRoot == null || corridors == null)
            {
                EditorUtility.DisplayDialog("Missing world hierarchy",
                    "Open your existing BLACKSITE_MainScene or WorldBuilder_Demo. It needs World_Static and Corridors objects. This tool will not build a replacement level.", "OK");
                return;
            }
            List<RoomSpec> rooms = new List<RoomSpec>();
            string[] required = { "ENTRY", "HUB", "ACOUSTIC", "SECURITY", "MAINT", "CORE" };
            foreach (string id in required)
            {
                Transform room = staticRoot.transform.Find(id);
                if (room == null)
                { EditorUtility.DisplayDialog("Missing room", "Could not find World_Static/" + id + ". No changes made.", "OK"); return; }
                Renderer floor = FindFloorRenderer(room, id);
                if (floor == null)
                { EditorUtility.DisplayDialog("Missing room floor", "Could not find floor renderer inside " + id + ". No changes made.", "OK"); return; }
                Bounds bounds = floor.bounds;
                rooms.Add(new RoomSpec(id, bounds.center, bounds.size.x, bounds.size.z));
            }
            // Older decoration pack should be removed first to avoid overlapping
            // materials, duplicate room lights and coincident floor skins.
            if (GameObject.Find("WB_DECORATION_SAFE_TO_REBUILD") != null)
            {
                EditorUtility.DisplayDialog("Older decoration found",
                    "This scene already has the earlier World Builder decoration. To avoid duplicate walls, floor skins and lights, remove it using BLACKSITE-7 > 5 - Remove Added Decoration, save, then run option 9 again. Original greybox and team scripts stay intact.", "OK");
                return;
            }
            GameObject old = GameObject.Find(RootName);
            if (old != null && !EditorUtility.DisplayDialog("Rebuild only added polish?",
                "This replaces only objects in " + RootName + ". Existing rooms, doors, AI, teammate scripts, old decoration add-on and original materials remain unchanged.\n\nDo not run on an unsaved team scene.", "Rebuild", "Cancel")) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            try
            {
                EditorUtility.DisplayProgressBar("BLACKSITE-7 Polish", "Preparing reusable assets", .08f);
                _materials.Clear();
                PrepareMaterials();
                if (old != null) Undo.DestroyObjectImmediate(old);
                GameObject go = new GameObject(RootName);
                Undo.RegisterCreatedObjectUndo(go, "Create BLACKSITE world polish");
                _root = go.transform;
                Transform floors = Group("01_FLOOR_PLATES_AND_DECALS", _root);
                Transform walls = Group("02_WALL_KIT_AND_PIPES", _root);
                Transform fixtures = Group("03_EMISSIVE_LIGHT_FIXTURES", _root);
                Transform props = Group("04_NON_INTERACTIVE_ROOM_PROPS", _root);
                Transform corridorsGroup = Group("05_CORRIDOR_TRIM", _root);
                Transform doorGroup = Group("06_DOOR_FRAME_ACCENTS", _root);
                Transform ceilings = Group(CeilingName, _root);
                Transform lights = Group("08_LOW_COST_REALTIME_LIGHTS", _root);
                for (int index = 0; index < rooms.Count; index++)
                {
                    RoomSpec r = rooms[index];
                    EditorUtility.DisplayProgressBar("BLACKSITE-7 Polish", "Dressing room " + r.Id, .18f + index * .095f);
                    DressRoom(r, floors, walls, fixtures, props, ceilings, lights);
                }
                DressCorridors(corridors.transform, corridorsGroup);
                DressDoors(doorGroup);
                ceilings.gameObject.SetActive(false); // Scene Top view still shows all rooms.
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                Selection.activeGameObject = go;
                SceneView.RepaintAll();
                EditorUtility.DisplayDialog("Polish added",
                    "Added industrial floor panels, wall bays, signs, trim, pipes, console props, room-themed emissive strips and six small non-shadow lights.\n\nFull roofs are OFF so Top view works. Use BLACKSITE-7 > 10 to toggle roofs for FPS screenshots.\n\nCheck collisions/doors/AI before submission. Save Ctrl+S.", "OK");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorUtility.DisplayDialog("Could not finish", "Open Window > General > Console and check the first red error.\n" + ex.Message, "OK");
            }
            finally { EditorUtility.ClearProgressBar(); }
        }

        [MenuItem("BLACKSITE-7/10 - Toggle FPS Ceilings")]
        public static void ToggleCeilings()
        {
            GameObject root = GameObject.Find(RootName);
            if (root == null) { EditorUtility.DisplayDialog("Polish not found", "Run BLACKSITE-7 > 9 first.", "OK"); return; }
            Transform ceiling = root.transform.Find(CeilingName);
            if (ceiling == null) return;
            Undo.RecordObject(ceiling.gameObject, "Toggle ceiling visibility");
            ceiling.gameObject.SetActive(!ceiling.gameObject.activeSelf);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("BLACKSITE ceilings: " + (ceiling.gameObject.activeSelf ? "ON (FPS view)" : "OFF (Top view)"));
        }

        [MenuItem("BLACKSITE-7/11 - Validate World Builder Scene")]
        public static void Validate()
        {
            var report = new StringBuilder();
            report.AppendLine("BLACKSITE-7 World Builder Visual / Structural Audit");
            report.AppendLine("Scene: " + SceneManager.GetActiveScene().name);
            report.AppendLine("Date: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            report.AppendLine("NOTE: Structural checks are not a substitute for actual Play Mode, profiler and full gameplay tests.");
            int good = 0, issues = 0;
            Action<string, bool> check = (description, success) =>
            {
                report.AppendLine((success ? "PASS  " : "CHECK ") + description);
                if (success) good++; else issues++;
            };
            var world = GameObject.Find("World_Static");
            var corridorRoot = GameObject.Find("Corridors");
            check("World_Static exists", world != null);
            check("Corridors exists", corridorRoot != null);
            string[] ids = { "ENTRY", "HUB", "ACOUSTIC", "SECURITY", "MAINT", "CORE" };
            foreach (string id in ids)
            {
                Transform room = world != null ? world.transform.Find(id) : null;
                Renderer floor = room != null ? FindFloorRenderer(room, id) : null;
                check("Room " + id + " has a floor", floor != null);
                check("Room " + id + " floor has original physics collider", floor != null && floor.GetComponent<Collider>() != null);
            }
            string[] links = { "ENTRY_HUB", "HUB_ACOUSTIC", "ACOUSTIC_SECURITY", "SECURITY_MAINT", "HUB_MAINT", "MAINT_CORE", "HUB_CORE_BYPASS" };
            foreach (string link in links)
                check("Corridor object " + link, corridorRoot != null && corridorRoot.transform.Find(link) != null);
            GameObject doors = GameObject.Find("Doors_Dynamic");
            for (int i = 1; i <= 7; i++)
            {
                string id = "D" + i.ToString("00");
                bool exists = false;
                if (doors != null)
                    foreach (Transform child in doors.transform)
                        if (child.name.StartsWith(id + "_", StringComparison.Ordinal) || child.name == id) exists = true;
                check("Door object " + id, exists);
            }
            check("Polish root exists", GameObject.Find(RootName) != null);
            check("RoomVolumes hierarchy exists", GameObject.Find("RoomVolumes") != null);
            check("CoverPoints hierarchy exists", GameObject.Find("CoverPoints") != null);
            check("Nav_Walkability hierarchy exists", GameObject.Find("Nav_Walkability") != null);
            var lightArray = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            int shadows = 0, pointLights = 0;
            foreach (Light light in lightArray)
            {
                if (!light.enabled || !light.gameObject.activeInHierarchy) continue;
                if (light.shadows != LightShadows.None) shadows++;
                if (light.type == LightType.Point) pointLights++;
            }
            report.AppendLine("Active point lights: " + pointLights + "; active shadow-casting lights: " + shadows);
            report.AppendLine("If Unity warns about shadow atlas: reduce shadow-casting light count/resolution, not the navigation geometry.");
            report.AppendLine("MANUAL: Walk ENTRY > HUB > ACOUSTIC > SECURITY > MAINT > CORE > return via bypass.");
            report.AppendLine("MANUAL: Test physical doors, decoy, barricades and all four agents together.");
            report.AppendLine("MANUAL: Re-bake/check NavMesh after any collision geometry change.");
            report.AppendLine("MANUAL: Capture real Unity Profiler FPS, batches/draw calls and memory.");
            report.AppendLine("MANUAL: Ensure teammates' models and animations use shared palette and matching scale.");
            report.AppendLine("Summary: " + good + " structural checks passed; " + issues + " need checking.");
            Directory.CreateDirectory(GeneratedRoot + "/Reports");
            string reportPath = GeneratedRoot + "/Reports/WorldBuilder_Audit.txt";
            File.WriteAllText(reportPath, report.ToString());
            AssetDatabase.Refresh();
            Debug.Log(report);
            EditorUtility.DisplayDialog("World audit saved", good + " passed; " + issues + " need checking.\n\nRead " + reportPath + " or open Window > General > Console.\n\nThis does NOT prove bug-free gameplay or a particular mark.", "OK");
        }

        [MenuItem("BLACKSITE-7/12 - Remove Excellent World Polish")]
        public static void Remove()
        {
            if (Application.isPlaying) { EditorUtility.DisplayDialog("Stop Play mode", "Stop Play mode first.", "OK"); return; }
            GameObject root = GameObject.Find(RootName);
            if (root == null) return;
            if (!EditorUtility.DisplayDialog("Remove polish?", "Delete only " + RootName + "? Generated material assets are retained and can be reused.", "Remove", "Cancel")) return;
            Undo.DestroyObjectImmediate(root);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        private static Renderer FindFloorRenderer(Transform parent, string id)
        {
            if (parent == null) return null;
            Renderer fallback = null;
            foreach (Renderer candidate in parent.GetComponentsInChildren<Renderer>(true))
            {
                if (candidate.name == "Floor_" + id) return candidate;
                if (fallback == null && candidate.name.IndexOf("floor", StringComparison.OrdinalIgnoreCase) >= 0)
                    fallback = candidate;
            }
            return fallback;
        }

        private static Transform Group(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static Material Mat(string name, Color baseColor, Texture2D texture = null,
            bool emission = false, float metallic = .35f, float smoothness = .25f)
        {
            Material m;
            if (_materials.TryGetValue(name, out m)) return m;
            string path = GeneratedRoot + "/Materials/M_" + name + ".mat";
            m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                if (shader == null) throw new InvalidOperationException("Could not find URP/Lit or Standard shader.");
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", baseColor);
            if (m.HasProperty("_Color")) m.SetColor("_Color", baseColor);
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", texture);
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", texture);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (emission)
            {
                m.EnableKeyword("_EMISSION");
                if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", baseColor * 2.25f);
            }
            else
            {
                m.DisableKeyword("_EMISSION");
                if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", Color.black);
            }
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            _materials[name] = m;
            return m;
        }

        private static Texture2D MakeTexture(string name, int kind)
        {
            string path = GeneratedRoot + "/Textures/T_" + name + ".png";
            if (!File.Exists(path))
            {
                const int sz = 256;
                var pixels = new Color32[sz * sz];
                var random = new System.Random(20261011 + kind * 109);
                for (int y = 0; y < sz; y++)
                    for (int x = 0; x < sz; x++)
                    {
                        int noise = random.Next(-9, 10);
                        int baseVal = 180;
                        if (kind == 0) // tiled steel floor, rivet pair and narrow seams
                        {
                            baseVal = 182 - ((x % 64 < 3 || y % 64 < 3) ? 73 : 0);
                            if (x % 64 >= 8 && x % 64 <= 11 && y % 64 >= 8 && y % 64 <= 11) baseVal = 90;
                            if (x % 64 >= 51 && x % 64 <= 54 && y % 64 >= 51 && y % 64 <= 54) baseVal = 92;
                        }
                        if (kind == 1) // wall plates, trim lines and corner bolts
                        {
                            baseVal = 169 - ((x % 128 < 4 || y % 128 < 4) ? 83 : 0);
                            if ((x % 128 == 16 || x % 128 == 112) && (y % 128 == 16 || y % 128 == 112)) baseVal = 64;
                        }
                        if (kind == 2) // brushed steel scratches
                        {
                            baseVal = 160 + ((y % 9 == 0) ? 15 : 0) + ((x % 71 == 0) ? -17 : 0);
                        }
                        if (kind == 3) // yellow/black warning stripes
                        {
                            bool yellow = ((x + y) / 30) % 2 == 0;
                            pixels[y * sz + x] = yellow ? new Color32(228, 166, 41, 255) : new Color32(31, 35, 40, 255);
                            continue;
                        }
                        byte v = (byte)Mathf.Clamp(baseVal + noise, 0, 255);
                        pixels[y * sz + x] = new Color32(v, v, v, 255);
                    }
                Texture2D texture = new Texture2D(sz, sz, TextureFormat.RGBA32, false, false);
                texture.SetPixels32(pixels);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            }
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && (importer.wrapMode != TextureWrapMode.Repeat ||
                importer.filterMode != FilterMode.Bilinear || importer.maxTextureSize != 256 ||
                importer.textureCompression != TextureImporterCompression.Compressed))
            {
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Bilinear;
                importer.maxTextureSize = 256;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static void PrepareMaterials()
        {
            Directory.CreateDirectory(GeneratedRoot + "/Materials");
            Directory.CreateDirectory(GeneratedRoot + "/Textures");
            Texture2D tiles = MakeTexture("FloorGrid", 0);
            Texture2D panels = MakeTexture("WallPanels", 1);
            Texture2D alloy = MakeTexture("BrushedAlloy", 2);
            Texture2D warning = MakeTexture("HazardStripe", 3);
            Mat("Floor_Navy", new Color(.30f, .44f, .59f), tiles, false, .36f, .16f);
            Mat("Wall_Panels", new Color(.36f, .47f, .58f), panels, false, .38f, .23f);
            Mat("Steel_Dark", new Color(.13f, .19f, .26f), alloy, false, .63f, .19f);
            Mat("Steel_Satin", new Color(.34f, .44f, .50f), alloy, false, .60f, .29f);
            Mat("Cargo", new Color(.30f, .40f, .39f), alloy, false, .22f, .17f);
            Mat("Rubber", new Color(.19f, .23f, .28f), tiles, false, .06f, .12f);
            Mat("Hazard", Color.white, warning, false, 0, .16f);
            Mat("Screen", new Color(.07f, .35f, .46f), panels, true, .13f, .45f);
            Mat("Glow_Cyan", new Color(.07f, .87f, 1f), null, true, .04f, .4f);
            Mat("Glow_Amber", new Color(1f, .50f, .09f), null, true, .04f, .4f);
            Mat("Glow_Red", new Color(1f, .10f, .17f), null, true, .04f, .4f);
            Mat("Glow_Violet", new Color(.45f, .27f, 1f), null, true, .04f, .4f);
            Mat("Sign_White", new Color(.86f,.92f,.98f), null, true, .03f, .23f);
            AssetDatabase.SaveAssets();
        }

        // Visual-only geometry by default. No colliders on floor skins, pipes, wall trims,
        // roofs or light strips: the ORIGINAL World_Static and door physics remain authoritative.
        private static GameObject Box(string name, Transform parent, Vector3 center, Vector3 size, string mat)
        {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = name;
            g.transform.SetParent(parent, false);
            g.transform.position = center;
            g.transform.localScale = size;
            g.GetComponent<Renderer>().sharedMaterial = _materials[mat];
            Collider c = g.GetComponent<Collider>();
            if (c != null) UnityEngine.Object.DestroyImmediate(c);
            Renderer renderer = g.GetComponent<Renderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return g;
        }

        private static GameObject Cylinder(string name, Transform parent, Vector3 center, float radius, float height, string mat)
        {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            g.name = name;
            g.transform.SetParent(parent, false);
            g.transform.position = center;
            g.transform.localScale = new Vector3(radius * 2, height * .5f, radius * 2);
            g.GetComponent<Renderer>().sharedMaterial = _materials[mat];
            Collider c = g.GetComponent<Collider>();
            if (c != null) UnityEngine.Object.DestroyImmediate(c);
            g.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            return g;
        }
        private static void Beam(string name, Transform parent, Vector3 a, Vector3 b, float radius, string mat)
        {
            Vector3 dir = b - a;
            if (dir.sqrMagnitude < .0001f) return;
            GameObject g = Cylinder(name, parent, (a+b)*.5f, radius, dir.magnitude, mat);
            g.transform.rotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);
        }
        private static void AddPointLight(string name, Transform parent, Vector3 pos, Color color, float intensity, float range)
        {
            GameObject g = new GameObject(name);
            g.transform.SetParent(parent, false);
            g.transform.position = pos;
            var l = g.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            l.shadows = LightShadows.None; // avoids the URP shadow atlas warning.
            l.renderMode = LightRenderMode.Auto;
        }
        private static void Lettering(string text, string name, Transform parent, Vector3 pos, float characterHeight)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0, 180, 0);
            TextMesh t = go.AddComponent<TextMesh>();
            t.text = text;
            t.fontSize = 64;
            t.characterSize = characterHeight / 64f;
            t.color = new Color(.73f,.93f,.99f);
            t.anchor = TextAnchor.MiddleCenter;
            t.alignment = TextAlignment.Center;
        }
        private static string Accent(string id)
        {
            if (id == "SECURITY" || id == "MAINT") return "Glow_Amber";
            if (id == "CORE") return "Glow_Violet";
            return "Glow_Cyan";
        }
        private static Color LightColor(string id)
        {
            if (id == "SECURITY" || id == "MAINT") return new Color(1f, .60f, .35f);
            if (id == "CORE") return new Color(.42f,.68f,1f);
            return new Color(.37f,.84f,1f);
        }
        private static void DressRoom(RoomSpec r, Transform floors, Transform walls,
            Transform fixtures, Transform props, Transform roofs, Transform lights)
        {
            string color = Accent(r.Id);
            Vector3 p = r.Center;
            float x = p.x, z = p.z, w = r.W, d = r.D;
            Transform roomFloors = Group(r.Id, floors);
            Transform roomWalls = Group(r.Id, walls);
            Transform roomFixtures = Group(r.Id, fixtures);
            Transform roomProps = Group(r.Id, props);
            Transform roomRoofs = Group(r.Id, roofs);
            // Thin decorative floor skin lies below existing glowing centerline and
            // does NOT alter walkability, floor collider or traversal height.
            Box("Grid_Panel_Floor", roomFloors, new Vector3(x,.002f,z), new Vector3(w-.54f,.004f,d-.54f), "Floor_Navy");
            for (int s = -1; s <= 1; s += 2)
            {
                // Floor perimeter illumination; center passages remain unobstructed.
                Box("Rim_Edge_EW_" + s, roomFloors, new Vector3(x,.014f,z+s*(d*.5f-.43f)),
                    new Vector3(w-.9f,.018f,.060f), color);
                Box("Rim_Edge_NS_" + s, roomFloors, new Vector3(x+s*(w*.5f-.43f),.014f,z),
                    new Vector3(.060f,.018f,d-.9f), color);
            }
            // Panel backing always near edges, never centered over portals.
            for (int i = -1; i <= 1; i += 2)
            {
                float px = x + i * w*.31f;
                Box("North_IndustrialWallBay_" + i, roomWalls,
                    new Vector3(px,1.85f,z+d*.5f-.20f), new Vector3(2.05f,2.55f,.052f), "Wall_Panels");
                Box("South_IndustrialWallBay_" + i, roomWalls,
                    new Vector3(px,1.85f,z-d*.5f+.20f), new Vector3(2.05f,2.55f,.052f), "Wall_Panels");
                Box("North_Vertical_Accent_" + i, roomWalls,
                    new Vector3(px-.75f,1.7f,z+d*.5f-.24f), new Vector3(.065f,1.6f,.065f), color);
                Box("South_Vertical_Accent_" + i, roomWalls,
                    new Vector3(px+.75f,1.7f,z-d*.5f+.24f), new Vector3(.065f,1.6f,.065f), color);
                // Structural false columns are visuals only, no new obstructions.
                Box("CornerPillar_" + i + "A", roomWalls, new Vector3(x+i*(w*.5f-.28f),1.7f,z+d*.5f-.3f), new Vector3(.26f,3.4f,.26f), "Steel_Satin");
                Box("CornerPillar_" + i + "B", roomWalls, new Vector3(x+i*(w*.5f-.28f),1.7f,z-d*.5f+.3f), new Vector3(.26f,3.4f,.26f), "Steel_Satin");
            }
            // Light fixtures look purposeful even when viewed from overhead.
            Box("Central_Strip_Housing", roomFixtures, new Vector3(x,3.34f,z), new Vector3(.42f,.14f,Mathf.Min(3.7f,d*.55f)), "Steel_Dark");
            Box("Central_Strip_Emissive", roomFixtures, new Vector3(x,3.24f,z), new Vector3(.24f,.045f,Mathf.Min(3.25f,d*.5f)), color);
            for (int i = -1; i <= 1; i += 2)
                Box("Side_Uplight_" + i, roomFixtures,
                    new Vector3(x+i*(w*.38f), 2.65f, z), new Vector3(.14f,.95f,.14f), color);
            AddPointLight("AccentFill_" + r.Id, lights, new Vector3(x+1.0f,2.7f,z+.7f), LightColor(r.Id), .58f, 6.8f);
            // Ceiling shell OFF by default. Full first-person view may enable it.
            Box("RoofPanel_" + r.Id, roomRoofs, new Vector3(x,3.79f,z), new Vector3(w,.20f,d), "Steel_Dark");
            Box("RoofServiceRib_N", roomRoofs, new Vector3(x,3.6f,z+d*.34f), new Vector3(w,.12f,.15f), "Steel_Satin");
            Box("RoofServiceRib_S", roomRoofs, new Vector3(x,3.6f,z-d*.34f), new Vector3(w,.12f,.15f), "Steel_Satin");
            // Signs mounted near the north wall, off its central doorway.
            Box("Nameplate_Backing", roomProps,
                new Vector3(x-w*.30f,2.65f,z+d*.5f-.22f), new Vector3(2.6f,.50f,.07f), "Steel_Dark");
            Box("Nameplate_Accent", roomProps,
                new Vector3(x-w*.30f,2.34f,z+d*.5f-.25f), new Vector3(2.6f,.05f,.085f), color);
            Lettering(r.Id, "SIGN_" + r.Id, roomProps,
                new Vector3(x-w*.30f,2.66f,z+d*.5f-.31f), .21f);

            // Freestanding additions are VISUAL ONLY and confined to room edges;
            // production doors, interactables and gameplay cover remain team-owned.
            if (r.Id == "ENTRY")
            {
                Cargo("CargoStack_BackLeft", roomProps, x-w*.35f, z+d*.27f, .94f);
                Machine("Cargo_Scanner", roomProps, x+w*.31f, z-d*.30f, "Glow_Cyan");
                Box("WarningLane", roomFloors,new Vector3(x,.023f,z-d*.40f), new Vector3(w*.50f,.012f,.27f), "Hazard");
            }
            else if (r.Id == "HUB")
            {
                WallDisplay("Wayfinding_Display", roomProps, x-w*.30f, z+d*.5f-.37f, color);
                Box("CentralHub_FloorCrossbar", roomFloors,new Vector3(x,.030f,z+d*.24f),new Vector3(w*.40f,.020f,.095f),color);
                Cargo("SupplyNearSide",roomProps,x+w*.34f,z-d*.36f,.70f);
            }
            else if (r.Id == "ACOUSTIC")
            {
                for (int i = 0; i < 3; i++)
                {
                    float pz = z-d*.32f+i*d*.32f;
                    Box("Acoustic_Baffle_"+i,roomProps,new Vector3(x-w*.47f,1.7f,pz),new Vector3(.12f,2.6f,1.25f),"Rubber");
                    Box("Baffle_Glow_"+i,roomProps,new Vector3(x-w*.445f,1.8f,pz),new Vector3(.025f,1.2f,.05f),color);
                }
                SensorDish("EchoSensor",roomProps,x+w*.30f,z+d*.30f);
            }
            else if (r.Id == "SECURITY")
            {
                for (int i = -1; i <= 1; i++)
                    WallDisplay("SurveillanceMonitor_"+i,roomProps,x+i*w*.22f,z+d*.5f-.37f, "Glow_Red");
                Cargo("SecurityLocker",roomProps,x+w*.40f,z-d*.30f,.75f);
                Box("Security_AlertStrip",roomFixtures,new Vector3(x,3.26f,z+d*.32f),new Vector3(2.4f,.07f,.12f),"Glow_Red");
            }
            else if (r.Id == "MAINT")
            {
                Machine("RepairStation",roomProps,x+w*.32f,z+d*.32f,"Glow_Amber");
                Cargo("MaintenanceCrate",roomProps,x-w*.36f,z-d*.31f,.78f);
                for (int i = 0; i < 3; i++)
                    Beam("ServicePipe_"+i,roomWalls,
                        new Vector3(x-w*.39f + i*.17f,3.05f,z-d*.34f),
                        new Vector3(x-w*.39f + i*.17f,3.05f,z+d*.34f),.046f,"Steel_Satin");
                Box("Maintenance_Hazard",roomFloors,new Vector3(x-w*.30f,.027f,z+d*.20f),new Vector3(.36f,.01f,2.4f),"Hazard");
            }
            else if (r.Id == "CORE")
            {
                // Do not duplicate the pickup orb or add blocking colliders near it.
                const int sides = 16;
                float radius = 2.1f;
                for (int i = 0; i < sides; i++)
                {
                    float a = i*2f*Mathf.PI/sides, b=(i+1)*2f*Mathf.PI/sides;
                    Vector3 pa = new Vector3(x+Mathf.Cos(a)*radius,.055f,z+Mathf.Sin(a)*radius);
                    Vector3 pb = new Vector3(x+Mathf.Cos(b)*radius,.055f,z+Mathf.Sin(b)*radius);
                    Beam("ContainmentRing_"+i,roomFloors,pa,pb,.047f,"Glow_Cyan");
                }
                for (int i = -1; i <= 1; i += 2)
                    for (int j = -1; j <= 1; j += 2)
                    {
                        Box("ReactorStabilizer_"+i+"_"+j,roomProps,
                            new Vector3(x+i*3.55f,.72f,z+j*3.25f),new Vector3(.65f,1.44f,.65f),"Steel_Dark");
                        Box("StabilizerGlow_"+i+"_"+j,roomProps,
                            new Vector3(x+i*3.55f,1.42f,z+j*3.25f),new Vector3(.45f,.07f,.45f),"Glow_Violet");
                    }
            }
        }

        private static void Cargo(string name, Transform parent, float x, float z, float scale)
        {
            Transform g = Group(name, parent);
            Box("ShippingBox",g,new Vector3(x,scale*.55f,z),new Vector3(scale,scale*1.1f,scale),"Cargo");
            for (int s=-1;s<=1;s+=2)
                Box("CornerReinforcement_"+s,g,new Vector3(x+s*scale*.40f,scale*.55f,z),
                    new Vector3(.07f,scale*1.1f,scale*.96f),"Steel_Satin");
            Box("InspectionStripe",g,new Vector3(x,scale*1.12f,z),new Vector3(scale*.75f,.027f,.12f),"Glow_Amber");
        }
        private static void Machine(string name, Transform parent, float x, float z, string glow)
        {
            Transform g = Group(name,parent);
            Box("Base",g,new Vector3(x,.45f,z),new Vector3(1.0f,.9f,.65f),"Steel_Dark");
            Box("Faceplate",g,new Vector3(x,1.0f,z-.33f),new Vector3(.72f,.56f,.08f),"Steel_Satin");
            Box("Readout",g,new Vector3(x,1.03f,z-.382f),new Vector3(.54f,.32f,.035f),glow);
            for (int i=0;i<3;i++)
                Box("Switch_"+i,g,new Vector3(x-.25f+i*.25f,.74f,z-.39f),new Vector3(.07f,.045f,.05f),"Glow_Cyan");
        }
        private static void WallDisplay(string name, Transform parent, float x, float z, string accent)
        {
            Transform g = Group(name,parent);
            Box("MonitorHousing",g,new Vector3(x,1.95f,z),new Vector3(1.15f,.7f,.10f),"Steel_Dark");
            Box("Display",g,new Vector3(x,1.96f,z-.065f),new Vector3(.92f,.52f,.04f),"Screen");
            Box("ActiveIndicator",g,new Vector3(x+.36f,1.65f,z-.08f),new Vector3(.1f,.045f,.045f),accent);
        }
        private static void SensorDish(string name, Transform parent, float x, float z)
        {
            Transform g=Group(name,parent);
            Cylinder("Pedestal",g,new Vector3(x,.52f,z),.34f,1.04f,"Steel_Dark");
            Cylinder("Turntable",g,new Vector3(x,1.1f,z),.44f,.16f,"Steel_Satin");
            Cylinder("AcousticCollector",g,new Vector3(x,1.43f,z),.49f,.13f,"Glow_Cyan");
            Box("RadialArms",g,new Vector3(x,1.53f,z),new Vector3(.65f,.065f,.13f),"Steel_Dark");
        }

        private static void DressCorridors(Transform source, Transform target)
        {
            // Corridor outlines use EXISTING Floor meshes as the authority, so they
            // follow the team's layout instead of guessed line segments.
            foreach (Renderer floor in source.GetComponentsInChildren<Renderer>(true))
            {
                if (!floor.name.StartsWith("Floor",StringComparison.Ordinal)) continue;
                Bounds b=floor.bounds;
                if (b.size.x < .5f || b.size.z < .5f) continue;
                Transform g=Group(floor.transform.parent.name+"_"+floor.name,target);
                Box("WalkwayGrid",g,new Vector3(b.center.x,.002f,b.center.z),
                    new Vector3(b.size.x-.10f,.004f,b.size.z-.10f),"Floor_Navy");
                bool vertical = b.size.z > b.size.x;
                for (int s=-1;s<=1;s+=2)
                {
                    if (vertical)
                        Box("EdgeGlow_"+s,g,new Vector3(b.center.x+s*(b.size.x*.5f-.20f),.016f,b.center.z),
                            new Vector3(.055f,.025f,b.size.z-.15f),"Glow_Cyan");
                    else
                        Box("EdgeGlow_"+s,g,new Vector3(b.center.x,.016f,b.center.z+s*(b.size.z*.5f-.20f)),
                            new Vector3(b.size.x-.15f,.025f,.055f),"Glow_Cyan");
                }
            }
        }
        private static void DressDoors(Transform target)
        {
            GameObject doors = GameObject.Find("Doors_Dynamic");
            if (doors == null) return;
            foreach (Transform door in doors.transform)
            {
                Vector3 pos=door.position;
                string n=door.name;
                if (!n.StartsWith("D0",StringComparison.Ordinal)) continue;
                // Fixed proposed routing: D02/D04/D06/D07 run N-S; others E-W.
                // Decoration is above the center line and has no collider.
                bool northSouth = n.StartsWith("D02") || n.StartsWith("D04") || n.StartsWith("D06") || n.StartsWith("D07");
                string color=n.StartsWith("D06") || n.StartsWith("D07") ? "Glow_Red" :
                    n.StartsWith("D04") || n.StartsWith("D05") ? "Glow_Amber" : "Glow_Cyan";
                Vector3 size=northSouth ? new Vector3(2.85f,.08f,.13f) : new Vector3(.13f,.08f,2.85f);
                Box("PortalEmissive_"+n,target,new Vector3(pos.x,2.90f,pos.z),size,color);
            }
        }
    }
}
#endif
