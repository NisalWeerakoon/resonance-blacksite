#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Blacksite7.WorldBuilder.Editor
{
    /// <summary>
    /// Additive, reversible decoration of the team's existing BLACKSITE-7 greybox.
    /// Does not edit any existing geometry, spawn, RoomVolume, DoorPortal or AI component.
    /// Open the WorldBuilder_Demo scene and run menu BLACKSITE-7 > 3 - Decorate Current Scene.
    /// </summary>
    public static class WorldBuilderDecorator
    {
        private const string AssetRoot = "Assets/_BLACKSITE/WorldBuilder/DecorGenerated";
        private const string DecorationName = "WB_DECORATION_SAFE_TO_REBUILD";
        private static Transform decoration;
        private static Material wall, floor, dark, trim, cyan, amber, red, screen, hazard, crate, white;

        private struct Room
        {
            public string id, label;
            public float x, z, w, d;
            public Room(string id, string label, float x, float z, float w, float d)
            { this.id = id; this.label = label; this.x = x; this.z = z; this.w = w; this.d = d; }
        }
        private static readonly Room[] rooms =
        {
            new Room("ENTRY", "LOADING BAY", -29, 0, 10, 10),
            new Room("HUB", "CENTRAL HUB", -12, 0, 12, 12),
            new Room("ACOUSTIC", "ACOUSTIC LAB", -12, 18, 12, 10),
            new Room("SECURITY", "SECURITY OPS", 8, 18, 12, 10),
            new Room("MAINT", "MAINTENANCE", 8, 0, 12, 12),
            new Room("CORE", "CORE VAULT", 8, -18, 12, 10)
        };

        [MenuItem("BLACKSITE-7/3 - Decorate Current Scene")]
        public static void Decorate()
        {
            if (Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Exit Play Mode", "Stop Play Mode before decorating.", "OK");
                return;
            }
            if (GameObject.Find("BLACKSITE7_WorldBuilder_Demo") == null ||
                GameObject.Find("World_Static") == null || GameObject.Find("Doors_Dynamic") == null)
            {
                EditorUtility.DisplayDialog("Open the demo scene first",
                    "Open Assets/_BLACKSITE/Scenes/WorldBuilder_Demo.unity, then try again. This add-on deliberately refuses to decorate an unrelated or production scene.", "OK");
                return;
            }
            Transform parent = GameObject.Find("BLACKSITE7_WorldBuilder_Demo").transform;
            Transform old = parent.Find(DecorationName);
            if (old != null && !EditorUtility.DisplayDialog("Rebuild decorations?",
                "This will delete ONLY the existing " + DecorationName + " objects and build fresh decoration. Existing rooms, doors, scripts, spawns and AI are untouched. Continue?", "Rebuild", "Cancel")) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            try
            {
                EditorUtility.DisplayProgressBar("BLACKSITE-7", "Creating procedural materials...", .1f);
                CreateMaterials();
                if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
                var go = new GameObject(DecorationName);
                Undo.RegisterCreatedObjectUndo(go, "Decorate BLACKSITE-7");
                decoration = go.transform;
                decoration.SetParent(parent, false);
                Transform roofGroup = Group("01_Roofs_And_Ceiling", decoration);
                Transform architecture = Group("02_Architectural_Trim", decoration);
                Transform roomsGroup = Group("03_Room_Decor", decoration);
                Transform lighting = Group("04_Additional_Lighting", decoration);
                Transform corridors = Group("05_Corridors_And_Signs", decoration);
                Transform doors = Group("06_Door_Visual_Trim", decoration);
                Transform skins = Group("00_Textured_Floor_And_Wall_Skins", decoration);
                SkinExistingArchitecture(skins);
                foreach (Room r in rooms)
                {
                    Transform rg = Group(r.id, roomsGroup);
                    CreateRoomShell(r, roofGroup, architecture);
                    DecorateRoom(r, rg, lighting);
                }
                EditorUtility.DisplayProgressBar("BLACKSITE-7", "Adding corridors and door highlights...", .7f);
                CorridorCeilings(corridors);
                DecorateDoors(doors);
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                Selection.activeGameObject = go;
                EditorUtility.DisplayDialog("Decoration created!",
                    "Added ceiling roofs, textured materials, pipes, machinery, neon panels, warning signs, corridor details and accent lights.\n\n" +
                    "Original level and ECHO scripts were NOT altered.\n\n" +
                    "Now press Play to inspect, then save the scene (Ctrl+S). Use menu BLACKSITE-7 > 4 to tune the original daylight if the scene looks too bright.", "OK");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorUtility.DisplayDialog("Decoration error", "Look at the first red error in Window > General > Console. The original world has not been rebuilt.\n\n" + e.Message, "OK");
            }
            finally { EditorUtility.ClearProgressBar(); }
        }

        [MenuItem("BLACKSITE-7/4 - Set Indoor Lighting Mood (Optional)")]
        public static void IndoorMood()
        {
            if (Application.isPlaying) { EditorUtility.DisplayDialog("Exit Play Mode", "Stop Play Mode first.", "OK"); return; }
            GameObject sun = GameObject.Find("FillDirectionalLight");
            if (sun == null || GameObject.Find(DecorationName) == null)
            {
                EditorUtility.DisplayDialog("Decorate first", "Open the demo, run menu BLACKSITE-7 > 3 first, then enable indoor lighting.", "OK");
                return;
            }
            Light light = sun.GetComponent<Light>();
            if (light != null)
            {
                Undo.RecordObject(light, "BLACKSITE interior light mood");
                light.intensity = 0.14f;
                light.color = new Color(.48f,.64f,.86f);
                EditorUtility.SetDirty(light);
            }
            // This scene-level ambient setting is intentional. Prefer an indoors feel but retain playable visibility.
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.19f, .24f, .31f);
            RenderSettings.fog = false;
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("Indoor lighting updated",
                "Reduced the demo sunlight and set a cool ambient tone. Save the scene. The original light can be adjusted manually under Lighting > FillDirectionalLight.", "OK");
        }

        [MenuItem("BLACKSITE-7/5 - Remove Added Decoration")]
        public static void RemoveDecoration()
        {
            if (Application.isPlaying) { EditorUtility.DisplayDialog("Exit Play Mode", "Stop Play Mode first.", "OK"); return; }
            GameObject root = GameObject.Find(DecorationName);
            if (root == null) { EditorUtility.DisplayDialog("Nothing to remove", "No add-on decoration is in this scene.", "OK"); return; }
            if (!EditorUtility.DisplayDialog("Remove decoration?", "Delete ONLY add-on decoration objects? Your original greybox and teammate scripts stay intact.", "Remove", "Cancel")) return;
            Undo.DestroyObjectImmediate(root);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        private static void CreateMaterials()
        {
            Directory.CreateDirectory(AssetRoot + "/Materials");
            Directory.CreateDirectory(AssetRoot + "/Textures");
            Texture2D metalTex = Texture("T_BrushedAlloy", 1);
            Texture2D floorTex = Texture("T_IndustrialFloor", 2);
            Texture2D wallTex = Texture("T_WallPanels", 3);
            Texture2D hazardTex = Texture("T_HazardStripes", 4);
            wall = Mat("M_WallPanels", new Color(.38f,.48f,.55f), wallTex, false);
            floor = Mat("M_FloorPlates", new Color(.43f,.50f,.56f), floorTex, false);
            dark = Mat("M_DarkMetal", new Color(.17f,.23f,.30f), metalTex, false);
            trim = Mat("M_BrightTrim", new Color(.57f,.65f,.69f), metalTex, false);
            cyan = Mat("M_EmissiveCyan", new Color(.05f,.92f,1f), null, true);
            amber = Mat("M_EmissiveAmber", new Color(1f,.54f,.13f), null, true);
            red = Mat("M_EmissiveRed", new Color(.96f,.11f,.16f), null, true);
            screen = Mat("M_DisplayBlue", new Color(.06f,.34f,.42f), wallTex, true);
            hazard = Mat("M_Hazard", Color.white, hazardTex, false);
            crate = Mat("M_CargoGreen", new Color(.40f,.48f,.40f), metalTex, false);
            white = Mat("M_LabelWhite", new Color(.85f,.96f,1f), null, false);
            AssetDatabase.SaveAssets();
        }

        private static Texture2D Texture(string name, int mode)
        {
            string path = AssetRoot + "/Textures/" + name + ".png";
            if (!File.Exists(path))
            {
                const int sz = 256;
                var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
                var colors = new Color32[sz * sz];
                var rng = new System.Random(187 + mode * 91);
                for (int y = 0; y < sz; y++)
                for (int x = 0; x < sz; x++)
                {
                    int noise = rng.Next(-14, 15);
                    int baseShade = 150;
                    if (mode == 1) baseShade = 144 + ((y % 17 == 0) ? 24 : 0);
                    if (mode == 2)
                    {
                        baseShade = 148 + (Mathf.Abs(x - 128) < 3 || Mathf.Abs(y - 128) < 3 ? -66 : 0);
                        if ((x % 64) < 2 || (y % 64) < 2) baseShade -= 16;
                        if ((x % 128) == 13 && (y % 128) == 13) baseShade = 80;
                    }
                    if (mode == 3)
                    {
                        baseShade = 151;
                        if (x % 128 < 5 || y % 128 < 5) baseShade = 64;
                        if ((x % 128 == 18 || x % 128 == 110) && (y % 128 == 18 || y % 128 == 110)) baseShade = 54;
                    }
                    if (mode == 4)
                    {
                        bool yellow = ((x + y) / 28) % 2 == 0;
                        colors[y * sz + x] = yellow ? new Color32(225,171,49,255) : new Color32(23,26,29,255);
                        continue;
                    }
                    byte v = (byte)Mathf.Clamp(baseShade + noise, 0, 255);
                    colors[y * sz + x] = new Color32(v, v, v, 255);
                }
                tex.SetPixels32(colors);
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && (importer.wrapMode != TextureWrapMode.Repeat || importer.filterMode != FilterMode.Bilinear))
            {
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Material Mat(string name, Color color, Texture2D texture, bool glow)
        {
            string path = AssetRoot + "/Materials/" + name + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                if (shader == null) throw new Exception("No supported Lit shader found. Use a Unity 6 URP project.");
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }
            m.color = color;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", texture);
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", texture);
            if (texture != null)
            {
                if (m.HasProperty("_BaseMap")) m.SetTextureScale("_BaseMap", name.Contains("Floor") ? new Vector2(4,4) : new Vector2(2,2));
                if (m.HasProperty("_MainTex")) m.SetTextureScale("_MainTex", name.Contains("Floor") ? new Vector2(4,4) : new Vector2(2,2));
            }
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", glow ? .08f : .55f);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", glow ? .52f : .23f);
            if (glow)
            {
                m.EnableKeyword("_EMISSION");
                if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", color * 2.0f);
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        private static Transform Group(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static GameObject Box(string name, Transform parent, Vector3 position, Vector3 scale, Material mat, bool collider = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            else go.isStatic = true;
            return go;
        }

        private static GameObject Cylinder(string name, Transform parent, Vector3 pos, float radius, float height, Material mat, bool collider = false)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.position = pos;
            obj.transform.localScale = new Vector3(radius * 2f, height * .5f, radius * 2f);
            obj.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider) UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());
            else obj.isStatic = true;
            return obj;
        }

        private static void Tube(string name, Transform p, Vector3 a, Vector3 b, float radius, Material m)
        {
            Vector3 delta = b-a;
            if (delta.sqrMagnitude < .001f) return;
            GameObject obj = Cylinder(name, p, (a+b)*.5f, radius, delta.magnitude, m);
            obj.transform.rotation = Quaternion.FromToRotation(Vector3.up, delta.normalized);
        }

        private static void LightAt(string name, Transform parent, Vector3 pos, Color color, float intensity, float range)
        {
            var g = new GameObject(name);
            g.transform.SetParent(parent, false);
            g.transform.position = pos;
            Light l = g.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            l.shadows = LightShadows.None;
            l.renderMode = LightRenderMode.Auto;
        }

        private static void TextAt(string name, string message, Transform parent, Vector3 pos, float size, Color color, float angle = 0)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0, angle, 0);
            TextMesh tm = go.AddComponent<TextMesh>();
            tm.text = message;
            tm.fontSize = 64;
            tm.characterSize = size / 64f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
        }

        private static void SkinExistingArchitecture(Transform skins)
        {
            // Thin renderer-only overlays: actual greybox geometry/colliders/materials stay unchanged.
            string[] roots = { "World_Static", "Corridors" };
            foreach (string rootName in roots)
            {
                GameObject sourceRoot = GameObject.Find(rootName);
                if (sourceRoot == null) continue;
                foreach (Transform child in sourceRoot.GetComponentsInChildren<Transform>(true))
                {
                    if (child == sourceRoot.transform) continue;
                    if (child.GetComponent<MeshRenderer>() == null || child.GetComponent<BoxCollider>() == null) continue;
                    string n = child.name;
                    bool isFloor = n.StartsWith("Floor_") && !n.Contains("Stripe") || n == "Floor";
                    bool isWall = n.Contains("_Segment") || n.StartsWith("Wall_") || n.Contains("OuterWestWall") ||
                                  n.Contains("InnerEastWall") || n.Contains("InnerNorthWall") || n.Contains("OuterSouthWall");
                    if (!isFloor && !isWall) continue;
                    Vector3 sourceScale = child.lossyScale;
                    Vector3 scale = isFloor ? new Vector3(sourceScale.x, .012f, sourceScale.z) :
                        new Vector3(sourceScale.x + .018f, sourceScale.y, sourceScale.z + .018f);
                    Vector3 pos = child.position;
                    if (isFloor) pos.y = .008f;
                    GameObject skin = Box("Textured_" + rootName + "_" + n, skins, pos, scale, isFloor ? floor : wall);
                    skin.transform.rotation = child.rotation;
                }
            }
        }

        private static void CreateRoomShell(Room r, Transform roofGroup, Transform trims)
        {
            // All ceilings are above the 3.5m walls. No colliders: navigation and physics unaffected.
            Box("Roof_" + r.id, roofGroup, new Vector3(r.x, 3.75f, r.z), new Vector3(r.w, .24f, r.d), dark);
            // Four structural piers just inside the corners, visually attached to existing greybox walls.
            float x0 = r.x-r.w*.5f+.32f, x1 = r.x+r.w*.5f-.32f;
            float z0 = r.z-r.d*.5f+.32f, z1 = r.z+r.d*.5f-.32f;
            Box("PillarA_"+r.id, trims, new Vector3(x0, 1.7f, z0), new Vector3(.34f,3.4f,.34f), trim);
            Box("PillarB_"+r.id, trims, new Vector3(x1, 1.7f, z0), new Vector3(.34f,3.4f,.34f), trim);
            Box("PillarC_"+r.id, trims, new Vector3(x0, 1.7f, z1), new Vector3(.34f,3.4f,.34f), trim);
            Box("PillarD_"+r.id, trims, new Vector3(x1, 1.7f, z1), new Vector3(.34f,3.4f,.34f), trim);
            for (int i = -1; i <= 1; i++)
            {
                float px = r.x+i*(r.w*.26f);
                Box("CeilingRib_"+r.id+"_"+i, roofGroup, new Vector3(px,3.53f,r.z), new Vector3(.18f,.20f,r.d-.36f), wall);
            }
            Box("FloorEdgeN_"+r.id, trims, new Vector3(r.x,.02f,r.z+r.d/2-.37f), new Vector3(r.w-.9f,.025f,.11f), floor);
            Box("FloorEdgeS_"+r.id, trims, new Vector3(r.x,.02f,r.z-r.d/2+.37f), new Vector3(r.w-.9f,.025f,.11f), floor);
            // Wall panel 'backs': thin surfaces within the room perimeter, NOT across door openings.
            for (int i = 0; i < 2; i++)
            {
                float px = i == 0 ? r.x-r.w*.35f : r.x+r.w*.35f;
                Box("NWall_Accent_"+r.id+"_"+i, trims, new Vector3(px,2.05f,r.z+r.d/2-.16f), new Vector3(1.45f,1.45f,.04f), wall);
                Box("SWall_Accent_"+r.id+"_"+i, trims, new Vector3(px,2.05f,r.z-r.d/2+.16f), new Vector3(1.45f,1.45f,.04f), wall);
            }
        }

        private static void DecorateRoom(Room r, Transform rg, Transform lights)
        {
            Material accent = r.id == "SECURITY" || r.id == "MAINT" ? amber : r.id == "CORE" ? red : cyan;
            Color lightColor = r.id == "SECURITY" || r.id == "MAINT" ? new Color(1f,.66f,.33f) : new Color(.25f,.82f,1f);
            // Emissive-only fluorescent housings plus ONE supplementary light per room; existing lights remain.
            for (int i = -1; i <= 1; i += 2)
            {
                float px = r.x+i*(r.w*.20f);
                Box("CeilingLuminaireFrame_"+i, rg, new Vector3(px,3.38f,r.z), new Vector3(.46f,.12f,3.1f), dark);
                Box("CeilingLuminaireGlow_"+i, rg, new Vector3(px,3.29f,r.z), new Vector3(.25f,.045f,2.7f), i == -1 ? white : accent);
            }
            LightAt("ExtraFill_" + r.id, lights, new Vector3(r.x+1.2f, 2.95f, r.z-1.1f), lightColor, 1.2f, 8.2f);
            // Sci-fi room ID wall placard placed to the LEFT of a wall corner, avoiding centerline doorways.
            float placardX = r.x - r.w*.32f;
            Box("RoomLabel_Back", rg, new Vector3(placardX,2.65f,r.z+r.d*.5f-.20f), new Vector3(2.25f,.46f,.08f), dark);
            Box("RoomLabel_Bar", rg, new Vector3(placardX,2.37f,r.z+r.d*.5f-.20f), new Vector3(2.25f,.055f,.095f), accent);
            // Face toward the interior (-Z) from the north wall.
            TextAt("ROOM_SIGN_"+r.id, r.label, rg, new Vector3(placardX,2.68f,r.z+r.d*.5f-.29f), .21f, Color.white, 180);

            if (r.id == "ENTRY")
            {
                CargoCrate("CARGO_STACK", rg, -26.9f, -3.15f, .9f);
                CargoCrate("CARGO_STACK_SMALL", rg, -27.1f, 3.5f, .64f);
                WallConsole("ENTRY_MANIFEST", rg, new Vector3(-33.7f,1.7f,-1.0f), false);
                Box("LOADING_HAZARD_ZONE", rg, new Vector3(-29,.017f,-3.9f), new Vector3(5,.012f,.4f), hazard);
                Tube("CeilingCable_West", rg, new Vector3(-33,3.30f,-3), new Vector3(-33,3.30f,3), .075f, dark);
            }
            if (r.id == "HUB")
            {
                WallConsole("HUB_TERMINAL", rg, new Vector3(-16.4f,1.7f,3.2f), false);
                CargoCrate("HUB_CARGO", rg, -8.2f, -4.3f, .72f);
                Box("HUB_DirectionLight", rg, new Vector3(-12,3.32f,2.8f), new Vector3(3.8f,.06f,.18f), cyan);
                TextAt("HUB_WAYFINDING", "ACOUSTIC  ^     CORE  v", rg, new Vector3(-12f,2.95f,5.77f), .18f, new Color(.66f,1f,1f),180);
            }
            if (r.id == "ACOUSTIC")
            {
                // Prototype ECHO stays in the room; new props live around room edges.
                for (int i = 0; i < 3; i++)
                {
                    float z = 14.4f + i*2.6f;
                    Box("SoundDamp_Panel_"+i, rg, new Vector3(-17.76f,1.8f,z), new Vector3(.09f,2.6f,1.8f), wall);
                    Box("SoundDamp_Stripe_"+i, rg, new Vector3(-17.7f,1.8f,z), new Vector3(.04f,1.6f,.07f), cyan);
                }
                Cylinder("SensorBase", rg, new Vector3(-8.5f,.42f,21.4f), .48f,.84f,dark);
                Cylinder("SensorRotor", rg, new Vector3(-8.5f,1.36f,21.4f), .57f,.12f,trim);
                Box("SensorFace", rg, new Vector3(-8.5f,1.45f,20.92f), new Vector3(.8f,.28f,.1f), cyan);
                TextAt("SILENCE_TEST", "SILENCE TESTING", rg, new Vector3(-12f,2.42f,22.74f), .22f, new Color(.7f,.96f,.98f), 180);
            }
            if (r.id == "SECURITY")
            {
                for (int i = 0; i < 3; i++)
                {
                    float x = 5.4f + 2.25f*i;
                    WallScreen("CAM_MONITOR_"+i, rg, new Vector3(x,2.15f,22.72f), amber);
                }
                Box("SecurityWarningStrip", rg, new Vector3(8,.015f,14.6f),new Vector3(5.8f,.015f,.34f), hazard);
                Tube("CCTV_Arm", rg,new Vector3(12.7f,2.95f,21.3f),new Vector3(11.85f,2.95f,21.3f),.06f,trim);
                Box("CCTV_Camera",rg,new Vector3(11.8f,2.9f,21.3f),new Vector3(.45f,.22f,.25f),dark);
            }
            if (r.id == "MAINT")
            {
                for (int i = 0; i < 3; i++)
                {
                    float z = -3.8f + 2.4f*i;
                    Tube("Cable_Rail_"+i, rg,new Vector3(3.6f,2.9f,z),new Vector3(3.6f,2.9f,z+1.7f),.075f,trim);
                }
                Tube("MainOverheadPipe",rg,new Vector3(11.9f,3.05f,-4.5f),new Vector3(11.9f,3.05f,3.9f),.12f,crate);
                Box("MaintMachinery",rg,new Vector3(11.5f,.9f,-3.4f),new Vector3(1.4f,1.8f,1.2f),dark);
                Box("MaintMachineryLight",rg,new Vector3(11.5f,1.35f,-4.05f),new Vector3(.85f,.12f,.055f),amber);
                WallConsole("MAINT_POWER",rg,new Vector3(3.8f,1.6f,-4.0f),true);
            }
            if (r.id == "CORE")
            {
                // Do not cover the existing interactable placeholder at (8,-18).
                for (int i = 0; i < 4; i++)
                {
                    float x = i%2==0 ? 4.1f : 11.9f;
                    float z = i<2 ? -21.6f : -14.4f;
                    Box("ContainmentPylon_"+i,rg,new Vector3(x,1.05f,z),new Vector3(.54f,2.1f,.54f),dark);
                    Box("PylonGlow_"+i,rg,new Vector3(x,1.55f,z),new Vector3(.58f,.09f,.58f),cyan);
                }
                Box("VaultFloorRing_E",rg,new Vector3(10.3f,.016f,-18),new Vector3(.10f,.014f,3.3f),red);
                Box("VaultFloorRing_W",rg,new Vector3(5.7f,.016f,-18),new Vector3(.10f,.014f,3.3f),red);
                Box("VaultFloorRing_N",rg,new Vector3(8,.016f,-16.35f),new Vector3(4.6f,.014f,.10f),red);
                Box("VaultFloorRing_S",rg,new Vector3(8,.016f,-19.65f),new Vector3(4.6f,.014f,.10f),red);
                WallScreen("CORE_STATUS",rg,new Vector3(11.4f,2.3f,-22.72f),cyan);
                TextAt("VAULT_WARNING", "CONTAINMENT  /  AUTHORIZED ACCESS",rg,new Vector3(8,2.65f,-22.70f),.18f,Color.white,0);
            }
        }

        private static void CargoCrate(string id, Transform p, float x, float z, float width)
        {
            Box(id+"_Body",p,new Vector3(x,width*.48f,z),new Vector3(width,width*.96f,width),crate, true);
            Box(id+"_BandA",p,new Vector3(x,width*.56f,z),new Vector3(width+.03f,.11f,width+.03f),dark);
            Box(id+"_Top",p,new Vector3(x,width*.99f,z),new Vector3(width+.03f,.06f,width+.03f),trim);
            Box(id+"_Label",p,new Vector3(x,width*.49f,z-width*.52f),new Vector3(width*.55f,.16f,.03f),hazard);
        }

        private static void WallConsole(string id, Transform p, Vector3 loc, bool faceEast)
        {
            Vector3 housing = faceEast ? new Vector3(.18f,1.25f,.9f) : new Vector3(.18f,1.25f,.9f);
            Box(id+"_Chassis",p,loc,housing,dark);
            Box(id+"_Display",p,loc+new Vector3(faceEast ? .12f:.12f,.06f,0),new Vector3(.055f,.75f,.67f),screen);
            Box(id+"_LED",p,loc+new Vector3(.16f,-.47f,.21f),new Vector3(.06f,.09f,.1f),cyan);
        }

        private static void WallScreen(string id, Transform p, Vector3 loc, Material accent)
        {
            Box(id+"_Frame",p,loc,new Vector3(1.4f,.92f,.12f),dark);
            Box(id+"_Glass",p,loc+new Vector3(0,0,-.078f),new Vector3(1.2f,.7f,.022f),screen);
            Box(id+"_Signal",p,loc+new Vector3(-.38f,-.22f,-.098f),new Vector3(.17f,.065f,.02f),accent);
        }

        private static void CorridorCeilings(Transform p)
        {
            StraightRoof("ENTRY_HUB",p,-24,-18,0,false);
            StraightRoof("HUB_ACOUSTIC",p,6,13,-12,true);
            StraightRoof("ACOUSTIC_SECURITY",p,-6,2,18,false);
            StraightRoof("SECURITY_MAINT",p,6,13,8,true);
            StraightRoof("HUB_MAINT",p,-6,2,0,false);
            StraightRoof("MAINT_CORE",p,-13,-6,8,true);
            Box("WestBypass_NorthSouthRoof",p,new Vector3(-12,3.75f,-12),new Vector3(3.4f,.24f,12),dark);
            Box("WestBypass_EastWestRoof",p,new Vector3(-5,3.75f,-18),new Vector3(14,.24f,3.4f),dark);
            LightAt("Bypass_GuideLight_A", p, new Vector3(-12,3.1f,-13.2f), new Color(.35f,.73f,1f), .95f, 7f);
            LightAt("Bypass_GuideLight_B", p, new Vector3(-5.6f,3.1f,-18), new Color(.42f,.78f,1f), .95f, 7f);
            // Thin emissive housings decorate the bypass, with a pair of real point lights above.
            for (int i=0;i<2;i++)
            {
                Box("BypassGlow_NS_"+i,p,new Vector3(-12,3.44f,-11-i*3.2f),new Vector3(.3f,.07f,1.0f),red);
                Box("BypassGlow_EW_"+i,p,new Vector3(-8+i*3.6f,3.44f,-18),new Vector3(1f,.07f,.3f),cyan);
            }
        }

        private static void StraightRoof(string id, Transform p, float from, float to, float axis, bool vertical)
        {
            float length = Mathf.Abs(to-from);
            float mid = (from+to)*.5f;
            Box(id+"_Roof",p,vertical ? new Vector3(axis,3.75f,mid):new Vector3(mid,3.75f,axis),
                vertical ? new Vector3(3.4f,.24f,length):new Vector3(length,.24f,3.4f),dark);
            Box(id+"_CeilingStrip",p,vertical ? new Vector3(axis,3.48f,mid):new Vector3(mid,3.48f,axis),
                vertical ? new Vector3(.23f,.06f,length*.5f):new Vector3(length*.5f,.06f,.23f),cyan);
            // Note: existing corridor wall colliders remain unchanged.
        }

        private static void DecorateDoors(Transform p)
        {
            var points = new (string id, float x, float z, bool vertical, Material m)[]
            {
                ("D01",-21,0,false,cyan), ("D02",-12,9.5f,true,cyan),
                ("D03",-2,18,false,cyan), ("D04",8,9.5f,true,amber),
                ("D05",-2,0,false,amber), ("D06",8,-9.5f,true,red),
                ("D07",-12,-8,true,red)
            };
            foreach (var door in points)
            {
                // Above passable part. Collider-free, purely cosmetic and separable from future door prefabs.
                Vector3 loc = new Vector3(door.x,3.03f,door.z);
                Vector3 size = door.vertical ? new Vector3(3.5f,.065f,.31f) : new Vector3(.31f,.065f,3.5f);
                Box(door.id+"_StatusBar",p,loc,size,door.m);
                if (door.vertical)
                {
                    Box(door.id+"_ThresholdHazard",p,new Vector3(door.x,.018f,door.z),new Vector3(3.4f,.013f,.38f),hazard);
                }
                else
                {
                    Box(door.id+"_ThresholdHazard",p,new Vector3(door.x,.018f,door.z),new Vector3(.38f,.013f,3.4f),hazard);
                }
            }
        }
    }
}
#endif
