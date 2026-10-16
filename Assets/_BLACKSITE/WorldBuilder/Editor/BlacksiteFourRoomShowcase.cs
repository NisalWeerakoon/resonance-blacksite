#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Blacksite7.WorldBuilder.Editor
{
    /// <summary>
    /// Additive, reversible cinematic room dressing. Editor-only: does NOT regenerate
    /// the scene or modify the team's World_Static, physics, doors, ECHO or gameplay.
    /// Unity 6 URP-compatible, no third-party assets or packages needed.
    /// </summary>
    public static class BlacksiteFourRoomShowcase
    {
        const string RootName = "WB_4ROOM_CINEMATIC_SHOWCASE";
        const string AssetDir = "Assets/_BLACKSITE/WorldBuilder/FourRoomGenerated";
        const float Ground = 0.035f;
        static Transform root;
        static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
        static readonly string[] roomNames = { "ENTRY", "HUB", "ACOUSTIC", "CORE" };

        struct Room
        {
            public string id;
            public float x, z, w, d;
            public Room(string n, Bounds b)
            { id = n; x = b.center.x; z = b.center.z; w = b.size.x; d = b.size.z; }
        }

        [MenuItem("BLACKSITE-7/13 - Install Four Cinematic Rooms")]
        public static void Install()
        {
            if (Application.isPlaying)
            { EditorUtility.DisplayDialog("Exit Play Mode", "Stop Unity Play Mode before decorating.", "OK"); return; }
            Transform world = GameObject.Find("World_Static")?.transform;
            if (world == null)
            { EditorUtility.DisplayDialog("Wrong scene", "Open your current BLACKSITE-7 scene with World_Static and six rooms first. Nothing was changed.", "OK"); return; }
            List<Room> rooms = new List<Room>();
            foreach (string id in roomNames)
            {
                Transform room = world.Find(id);
                if (room == null)
                { EditorUtility.DisplayDialog("Missing room", "World_Static/" + id + " is missing. Nothing was changed.", "OK"); return; }
                Renderer floor = null;
                foreach (Renderer r in room.GetComponentsInChildren<Renderer>(true))
                {
                    if (r.name == "Floor_" + id) { floor = r; break; }
                    if (floor == null && r.name.IndexOf("floor", StringComparison.OrdinalIgnoreCase) >= 0) floor = r;
                }
                if (floor == null)
                { EditorUtility.DisplayDialog("Missing floor", "No floor renderer in room " + id + ". Nothing was changed.", "OK"); return; }
                rooms.Add(new Room(id, floor.bounds));
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var existing = FindGeneratedRoot();
            if (existing != null && !EditorUtility.DisplayDialog("Rebuild generated dressing?",
                "Rebuild ONLY " + RootName + "? Your existing rooms, doors, AI, materials and earlier polish will not be deleted.", "Rebuild", "Cancel")) return;
            try
            {
                EditorUtility.DisplayProgressBar("BLACKSITE Four Rooms", "Preparing reusable materials", .05f);
                mats.Clear(); Materials();
                if (existing != null) Undo.DestroyObjectImmediate(existing);
                var go = new GameObject(RootName);
                Undo.RegisterCreatedObjectUndo(go, "Install BLACKSITE cinematic rooms");
                root = go.transform;
                foreach (Room r in rooms)
                {
                    EditorUtility.DisplayProgressBar("BLACKSITE Four Rooms", "Decorating " + r.id, .20f + rooms.IndexOf(r) * .18f);
                    Transform parent = Group(r.id + "_SHOWCASE", root);
                    switch (r.id)
                    {
                        case "ENTRY": Entry(r, parent); break;
                        case "HUB": Hub(r, parent); break;
                        case "ACOUSTIC": Acoustic(r, parent); break;
                        case "CORE": Core(r, parent); break;
                    }
                }
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                Selection.activeGameObject = go;
                SceneView.RepaintAll();
                EditorUtility.DisplayDialog("Four cinematic rooms installed",
                    "Created industrial loading bay, command hub, acoustic research lab and reactor vault.\n\n" +
                    "All NEW objects are below " + RootName + ". Existing doors/AI and physics were not modified. " +
                    "Decorative meshes initially have NO colliders to avoid breaking pathfinding.\n\n" +
                    "Inspect overlaps with older polish; optionally disable the older decorative props group. " +
                    "Save Ctrl+S and test movement, doors, and gameplay before committing.", "OK");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorUtility.DisplayDialog("Generation error", "Check Window > General > Console. " + e.Message, "OK");
            }
            finally { EditorUtility.ClearProgressBar(); }
        }

        [MenuItem("BLACKSITE-7/14 - Toggle Four Room Showcase")]
        public static void Toggle()
        {
            var obj = FindGeneratedRoot();
            if (obj == null) { EditorUtility.DisplayDialog("Missing decoration", "Run menu option 13 first.", "OK"); return; }
            Undo.RecordObject(obj, "Toggle cinematic room design");
            obj.SetActive(!obj.activeSelf);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        [MenuItem("BLACKSITE-7/15 - Remove Four Room Showcase")]
        public static void Remove()
        {
            if (Application.isPlaying) return;
            var obj = FindGeneratedRoot();
            if (obj == null) return;
            if (!EditorUtility.DisplayDialog("Remove cinematic rooms?",
                "Remove only " + RootName + " from this scene? Generated materials will remain for reuse.",
                "Remove", "Cancel")) return;
            Undo.DestroyObjectImmediate(obj);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        // Scene traversal finds disabled showcase roots too, so menu 14 toggles both ways.
        static GameObject FindGeneratedRoot()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid()) return null;
            foreach (GameObject candidate in scene.GetRootGameObjects())
                if (candidate.name == RootName) return candidate;
            return null;
        }

        static void Materials()
        {
            Directory.CreateDirectory(AssetDir + "/Materials");
            MakeMat("Hull", ColorRGB("#182734"), .52f, .22f);
            MakeMat("DarkHull", ColorRGB("#0B151E"), .57f, .16f);
            MakeMat("Alloy", ColorRGB("#405869"), .65f, .32f);
            MakeMat("GridSteel", ColorRGB("#294354"), .42f, .18f);
            MakeMat("Glass", ColorRGB("#112D3B"), .25f, .75f);
            MakeMat("Cargo", ColorRGB("#344950"), .38f, .20f);
            MakeMat("BlueScreen", ColorRGB("#125A75"), .05f, .45f, 1.15f);
            MakeMat("LightCyan", ColorRGB("#29E7FF"), .02f, .45f, 2.6f);
            MakeMat("LightBlue", ColorRGB("#459EFF"), .02f, .45f, 2.2f);
            MakeMat("LightWarm", ColorRGB("#FFD497"), .02f, .45f, 2.0f);
            MakeMat("LightRed", ColorRGB("#FF3C58"), .02f, .4f, 1.8f);
            MakeMat("LightGreen", ColorRGB("#27FFA8"), .02f, .4f, 2.8f);
            MakeMat("ReactorWhite", ColorRGB("#B2EEFF"), .07f, .75f, 3.7f);
            MakeMat("ReactorShell", ColorRGB("#50657C"), .72f, .7f);
            MakeMat("Gold", ColorRGB("#B58D42"), .46f, .36f);
            MakeMat("FloorLine", ColorRGB("#53788B"), .12f, .21f);
            MakeMat("Warn", ColorRGB("#FFBA48"), .03f, .23f, .8f);
            AssetDatabase.SaveAssets();
        }
        static Color ColorRGB(string html)
        { ColorUtility.TryParseHtmlString(html, out Color c); return c; }
        static Material MakeMat(string name, Color color, float metallic, float smooth, float glow = 0)
        {
            string path = AssetDir + "/Materials/BS7_Showcase_" + name + ".mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                if (shader == null) throw new InvalidOperationException("URP/Lit or Standard shader missing.");
                mat = new Material(shader); AssetDatabase.CreateAsset(mat, path);
            }
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smooth);
            if (glow > 0)
            {
                mat.EnableKeyword("_EMISSION");
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", color * glow);
            }
            else
            {
                mat.DisableKeyword("_EMISSION");
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", Color.black);
            }
            mat.enableInstancing = true; EditorUtility.SetDirty(mat);
            mats[name] = mat; return mat;
        }
        static Transform Group(string name, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }
        static GameObject Shape(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, string material)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name; go.transform.SetParent(parent, false);
            go.transform.position = pos; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mats[material];
            var col = go.GetComponent<Collider>();
            if (col != null) UnityEngine.Object.DestroyImmediate(col);
            var meshRenderer = go.GetComponent<Renderer>();
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = true;
            return go;
        }
        static GameObject Box(string name, Transform group, float x, float y, float z, float w, float h, float d, string mat)
        { return Shape(PrimitiveType.Cube, name, group, new Vector3(x,y,z), new Vector3(w,h,d), mat); }
        static GameObject Cyl(string name, Transform group, float x,float y,float z,float radius,float height,string mat)
        { return Shape(PrimitiveType.Cylinder, name, group, new Vector3(x,y,z), new Vector3(radius*2,height/2,radius*2), mat); }
        static GameObject Ball(string name, Transform group, float x,float y,float z,float diameter,string mat)
        { return Shape(PrimitiveType.Sphere, name, group, new Vector3(x,y,z), Vector3.one*diameter, mat); }
        static GameObject Pipe(string name, Transform group, Vector3 from, Vector3 to, float radius, string mat)
        {
            Vector3 delta = to-from;
            GameObject pipe = Cyl(name, group, (from.x+to.x)/2, (from.y+to.y)/2, (from.z+to.z)/2, radius, delta.magnitude, mat);
            pipe.transform.rotation = Quaternion.FromToRotation(Vector3.up, delta.normalized);
            return pipe;
        }
        static void Ring(string name, Transform group, Vector3 center, float radius, float thickness, string mat, int segments = 20, bool vertical = false)
        {
            for (int i=0;i<segments;i++)
            {
                float a=i*2*Mathf.PI/segments, b=(i+1)*2*Mathf.PI/segments;
                Vector3 from, to;
                if (!vertical)
                {
                    from=center+new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius);
                    to=center+new Vector3(Mathf.Cos(b)*radius,0,Mathf.Sin(b)*radius);
                }
                else
                {
                    from=center+new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,0);
                    to=center+new Vector3(Mathf.Cos(b)*radius,Mathf.Sin(b)*radius,0);
                }
                Pipe(name+"_"+i,group,from,to,thickness,mat);
            }
        }
        static void LightFill(string name,Transform group,float x,float y,float z,Color color,float intensity,float range)
        {
            var go=new GameObject(name); go.transform.SetParent(group,false); go.transform.position=new Vector3(x,y,z);
            Light light=go.AddComponent<Light>(); light.type=LightType.Point;
            light.color=color; light.intensity=intensity; light.range=range;
            light.shadows=LightShadows.None; // No extra shadow-map allocations.
        }
        static void Sign(string label,string name,Transform group,Vector3 pos,Quaternion rotation,float size)
        {
            var go=new GameObject(name); go.transform.SetParent(group,false);
            go.transform.position=pos; go.transform.rotation=rotation;
            var mesh=go.AddComponent<TextMesh>();
            mesh.text=label; mesh.fontSize=72; mesh.characterSize=size/72f;
            mesh.anchor=TextAnchor.MiddleCenter; mesh.alignment=TextAlignment.Center;
            mesh.color=ColorRGB("#99EAF9");
        }
        static void FloorLines(Room r,Transform parent,string mat)
        {
            Transform g=Group("NonBlockingFloorInlays",parent);
            for(int i=-2;i<=2;i++)
            {
                float px = r.x+i*(r.w-.9f)/5f;
                Box("FloorJointX_"+i,g,px,Ground,r.z,.014f,.008f,r.d-.42f,"FloorLine");
            }
            for(int i=-2;i<=2;i++)
            {
                float pz = r.z+i*(r.d-.9f)/5f;
                Box("FloorJointZ_"+i,g,r.x,Ground,pz,r.w-.42f,.008f,.014f,"FloorLine");
            }
            for(int s=-1;s<=1;s+=2)
                Box("PerimeterEmitter_"+s,g,r.x+s*(r.w/2-.31f),.07f,r.z,.052f,.025f,r.d-1.3f,mat);
        }
        static void WallLightBars(Room r,Transform parent,string mat)
        {
            Transform g=Group("WallLightBars",parent);
            for(int s=-1;s<=1;s+=2)
            {
                Box("NorthSouth_"+s,g,r.x+s*r.w*.30f,2.85f,r.z+r.d*.5f-.25f,.95f,.07f,.07f,mat);
                Box("SouthGlow_"+s,g,r.x+s*r.w*.30f,2.85f,r.z-r.d*.5f+.25f,.95f,.07f,.07f,mat);
                Box("SideVertical_"+s,g,r.x+s*(r.w*.5f-.25f),1.75f,r.z+r.d*.26f,.045f,2.0f,.075f,mat);
            }
        }
        static void Crate(string name,Transform group,float x,float z,float width=1f,float height=1.1f)
        {
            Transform g=Group(name,group);
            Box("Body",g,x,height/2,z,width,height,width,"Cargo");
            Box("RimTop",g,x,height,z,width+.1f,.07f,width+.1f,"Alloy");
            for(int s=-1;s<=1;s+=2)
            {
                Box("LockBuckle_"+s,g,x+s*(width/2-.09f),height*.58f,z+.011f,.065f,height*.85f,width*.88f,"Hull");
                Box("HazardLine_"+s,g,x,.12f,z+s*(width*.37f),width*.7f,.018f,.038f,"Warn");
            }
        }
        static void Screen(string name,Transform group,float x,float y,float z,float w,float h,bool front=true)
        {
            Transform g=Group(name,group);
            Box("PanelFrame",g,x,y,z,w+.14f,h+.14f,.15f,"Alloy");
            Box("LCD_Cyan_Interface",g,x,y,z+(front?-.09f:.09f),w,h,.026f,"BlueScreen");
            for(int i=0;i<3;i++)
                Box("TelemetryRow_"+i,g,x-w*.32f,y+h*.24f-i*h*.22f,z+(front?-.117f:.117f),w*.28f+i*w*.10f,.019f,.012f,"LightCyan");
        }
        static void CeilingBeam(Room r,Transform g,string accent)
        {
            Transform c=Group("OpenCeilingBeams_compatible_with_FPS_roof",g);
            for(int n=-1;n<=1;n+=2)
            {
                Box("HeavyCrossBeam_"+n,c,r.x,3.28f,r.z+n*r.d*.32f,r.w-.6f,.20f,.15f,"DarkHull");
                Box("BeamCyanTube_"+n,c,r.x,3.15f,r.z+n*r.d*.32f,r.w*.58f,.035f,.055f,accent);
            }
            // Decorative beams sit just below a 3.8m ceiling and work even with roof toggled ON.
        }
        static void Entry(Room r,Transform parent)
        {
            FloorLines(r,parent,"LightCyan"); WallLightBars(r,parent,"LightBlue"); CeilingBeam(r,parent,"LightCyan");
            Transform g=Group("01_ENTRY_INDUSTRIAL_LOADING_BAY",parent);
            // Vehicle/loading-lane paint, scanning portal and a safe free central aisle.
            for(int side=-1;side<=1;side+=2)
            {
                Box("DockLane_"+side,g,r.x+side*1.45f,.045f,r.z,.09f,.018f,r.d*.58f,"FloorLine");
                Crate("WallCargoStack_"+side,g,r.x+side*(r.w*.5f-1.2f),r.z+side*2.85f,.9f,.9f);
                Box("StorageShelfBack_"+side,g,r.x+side*(r.w*.5f-.5f),1.30f,r.z-1.8f,.24f,2.45f,2.2f,"Hull");
                for(int i=0;i<3;i++)
                    Box("ShelfDeck_"+side+"_"+i,g,r.x+side*(r.w*.5f-.8f),.4f+i*.75f,r.z-1.8f,.72f,.10f,2.2f,"Alloy");
                Box("WallWarningBar_"+side,g,r.x+side*(r.w*.5f-.32f),2.6f,r.z+1.7f,.05f,.8f,.08f,"LightRed");
            }
            // A flat visual decal: no collider and no objective logic replacement.
            Ring("ExtractionPadOutline",g,new Vector3(r.x,.08f,r.z),1.45f,.035f,"LightCyan",24);
            Box("LandingTargetLine",g,r.x,.07f,r.z,2.1f,.01f,.045f,"LightCyan");
            Sign("EXTRACTION / ENTRY","LoadingBaySign",g,
                new Vector3(r.x,2.64f,r.z+r.d*.5f-.34f),Quaternion.Euler(0,180,0),.25f);
            Screen("BayStatusMonitor",g,r.x-1.5f,1.95f,r.z+r.d*.5f-.40f,1.25f,.70f);
            LightFill("DockBlueFill_NoShadows",g,r.x,2.9f,r.z,ColorRGB("#48C6F2"),.60f,6.5f);
            LightFill("DockWarmAccent_NoShadows",g,r.x+r.w*.30f,2.6f,r.z-2,ColorRGB("#FFC16B"),.22f,3f);
        }
        static void Hub(Room r,Transform parent)
        {
            FloorLines(r,parent,"LightBlue"); WallLightBars(r,parent,"LightCyan"); CeilingBeam(r,parent,"LightCyan");
            Transform g=Group("02_HUB_COMMAND_CENTER",parent);
            // Command briefing station around edge: keeps north/south/east/west portals clear.
            Cyl("CommandStationBottom",g,r.x+1.35f,.16f,r.z+1.5f,1.10f,.30f,"DarkHull");
            Cyl("ConsoleRaisedDisk",g,r.x+1.35f,.39f,r.z+1.5f,.82f,.19f,"Alloy");
            Ring("HoloProjectionRing",g,new Vector3(r.x+1.35f,.57f,r.z+1.5f),.63f,.032f,"LightCyan",20);
            Ball("BriefingHoloDot",g,r.x+1.35f,1.19f,r.z+1.5f,.20f,"LightBlue");
            // Left/right operator consoles; center cross-route remains open.
            for(int side=-1;side<=1;side+=2)
            {
                float px=r.x+side*(r.w*.5f-1.0f);
                Box("OperatorDesk_"+side,g,px,.77f,r.z-2.3f,1.10f,1.10f,1.8f,"Hull");
                Box("OperatorScreen_"+side,g,px,1.39f,r.z-2.5f,.92f,.36f,.055f,"BlueScreen");
                Box("StatusFloorMarker_"+side,g,r.x+side*(r.w*.5f-1.4f),Ground+.014f,r.z+1.0f,.1f,.01f,2.1f,"LightCyan");
            }
            Screen("LargeMissionBriefing",g,r.x-1.3f,2.0f,r.z+r.d*.5f-.35f,2.05f,1.0f);
            Screen("TacticalRouteDisplay",g,r.x+2.2f,2.0f,r.z+r.d*.5f-.35f,1.6f,.9f);
            Sign("COMMAND / MISSION BRIEF","HubTitle",g,
                new Vector3(r.x,2.83f,r.z+r.d*.5f-.38f),Quaternion.Euler(0,180,0),.22f);
            for(int i=0;i<3;i++)
                Box("CeilingHoloRib_"+i,g,r.x-2.4f+i*2.4f,3.05f,r.z,.045f,.045f,3.2f,"LightBlue");
            LightFill("HubSoftBlueFill_NoShadows",g,r.x,2.8f,r.z,ColorRGB("#78D6F6"),.46f,7.5f);
        }
        static void Acoustic(Room r,Transform parent)
        {
            FloorLines(r,parent,"LightCyan"); WallLightBars(r,parent,"LightGreen"); CeilingBeam(r,parent,"LightCyan");
            Transform g=Group("03_ACOUSTIC_SENSING_RESEARCH_LAB",parent);
            // Glowing green research columns, as in the third supplied reference.
            for(int side=-1;side<=1;side+=2)
            for(int index=0;index<2;index++)
            {
                float x=r.x+side*(r.w*.5f-1.12f), z=r.z-1.65f+index*3.0f;
                Cyl("CryoFoot_"+side+"_"+index,g,x,.23f,z,.51f,.46f,"DarkHull");
                Cyl("CryoGlass_"+side+"_"+index,g,x,1.53f,z,.40f,2.22f,"LightGreen");
                Cyl("CryoFrameTop_"+side+"_"+index,g,x,2.72f,z,.52f,.22f,"Alloy");
                for(int rib=0;rib<3;rib++)
                    Box("SensorStatus_"+side+"_"+index+"_"+rib,g,
                        x+side*.44f,.90f+rib*.52f,z,.045f,.17f,.18f,"LightCyan");
            }
            // Acoustic emitter machine against far wall, ECHO's central navigation space stays usable.
            Box("AcousticConsoleBase",g,r.x,.54f,r.z+r.d*.5f-.98f,2.4f,1.08f,1.15f,"DarkHull");
            Screen("AnalyzerPanel",g,r.x,1.46f,r.z+r.d*.5f-.46f,1.55f,.72f);
            Ring("SignalPulseFloor",g,new Vector3(r.x,.07f,r.z),2f,.032f,"LightCyan",24);
            Ring("SignalPulseInner",g,new Vector3(r.x,.072f,r.z),1.15f,.021f,"LightGreen",20);
            for(int side=-1;side<=1;side+=2)
                Box("SoundDampeningWall_"+side,g,r.x+side*(r.w*.5f-.33f),1.5f,r.z,
                    .11f,2.1f,2.8f,"Glass");
            Sign("ACOUSTIC / SILENCE TESTING","LabSign",g,
                new Vector3(r.x,2.9f,r.z+r.d*.5f-.35f),Quaternion.Euler(0,180,0),.19f);
            LightFill("LabMintFill_NoShadows",g,r.x+1f,2.8f,r.z,ColorRGB("#38F7B0"),.43f,7.3f);
        }
        static void Core(Room r,Transform parent)
        {
            FloorLines(r,parent,"LightCyan"); WallLightBars(r,parent,"LightBlue"); CeilingBeam(r,parent,"LightBlue");
            Transform g=Group("04_CORE_VAULT_REACTOR_CHAMBER",parent);
            // Existing CorePickup_Visual_Only & gameplay objective are deliberately untouched.
            // Rings + shell panels are visual only; use team's original pickup collider.
            Ring("LowerContainmentRail",g,new Vector3(r.x,.35f,r.z),2.45f,.075f,"Alloy",28);
            Ring("LowerRailGlow",g,new Vector3(r.x,.43f,r.z),2.45f,.028f,"LightBlue",28);
            Ring("CeilingEnergyRing",g,new Vector3(r.x,3.04f,r.z),2.6f,.095f,"Alloy",28);
            Ring("CeilingCyanRing",g,new Vector3(r.x,2.95f,r.z),2.35f,.048f,"LightCyan",28);
            for(int i=0;i<8;i++)
            {
                float a=(Mathf.PI*2*i)/8f;
                float px=r.x+Mathf.Cos(a)*2.15f,pz=r.z+Mathf.Sin(a)*2.15f;
                Pipe("ReactorRadialArm_"+i,g,new Vector3(px,.38f,pz),new Vector3(px*.0f+r.x+Mathf.Cos(a)*2.4f,2.55f,r.z+Mathf.Sin(a)*2.4f),.066f,"ReactorShell");
            }
            // Four corner stabilizer pylons (not blocking the main door centerline).
            for(int xs=-1;xs<=1;xs+=2)
            for(int zs=-1;zs<=1;zs+=2)
            {
                float px=r.x+xs*(r.w*.5f-.90f),pz=r.z+zs*(r.d*.5f-.90f);
                Box("CornerSupport_"+xs+"_"+zs,g,px,1.13f,pz,.62f,2.25f,.62f,"DarkHull");
                Box("SupportLED_"+xs+"_"+zs,g,px,1.59f,pz,.68f,.85f,.055f,"LightBlue");
            }
            // If a pre-existing core orb exists, don't add another that could hide it.
            // A visual rotating ring offers visual impact without modifying the core objective.
            var gyro = RingVisual(g,r.x,1.72f,r.z);
            gyro.transform.rotation=Quaternion.Euler(0,40,19);
            Screen("VaultControlTerminal",g,r.x-2.75f,1.18f,r.z+r.d*.5f-1.05f,1.25f,.60f);
            Sign("CORE VAULT / CONTAINMENT","VaultTitle",g,
                new Vector3(r.x,2.90f,r.z+r.d*.5f-.32f),Quaternion.Euler(0,180,0),.20f);
            LightFill("ReactorBlueGlow_NoShadows",g,r.x,2.35f,r.z,ColorRGB("#39C8FF"),.95f,6.0f);
            LightFill("ReactorVioletRim_NoShadows",g,r.x-3.2f,2.2f,r.z+1.0f,ColorRGB("#8568FF"),.33f,4.3f);
        }
        static GameObject RingVisual(Transform parent,float x,float y,float z)
        {
            var obj=new GameObject("CoreOrnamentalGimbal_NOT_Objective");
            obj.transform.SetParent(parent,false);
            Ring("OrnamentalGyro",obj.transform,Vector3.zero,1.30f,.045f,"ReactorShell",24,true);
            obj.transform.position=new Vector3(x,y,z);
            return obj;
        }
    }
}
#endif
