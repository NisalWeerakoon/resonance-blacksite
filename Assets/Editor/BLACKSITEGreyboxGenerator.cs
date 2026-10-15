// BLACKSITE-7 World Builder Greybox Generator
// Place this file at Assets/Editor/BLACKSITEGreyboxGenerator.cs
// In Unity: Tools > BLACKSITE-7 > Generate World Builder Greybox
// Creates a new scene with six rooms, seven portal markers, corridors and a basic greybox.

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public static class BLACKSITEGreyboxGenerator
{
    private const float WallHeight = 3.5f;
    private const float WallThickness = 0.35f;
    private const float CorridorWidth = 3.4f;
    private const float DoorWidth = 2.4f;
    private const float DoorHeight = 2.6f;

    private static Transform worldStatic, corridors, doors, roomVolumes, coverPoints, intercepts, lighting, spawns, interactables;

    [MenuItem("Tools/BLACKSITE-7/Generate World Builder Greybox")]
    public static void Generate()
    {
        if (!EditorUtility.DisplayDialog(
            "Generate BLACKSITE-7 Greybox",
            "This creates a NEW scene and does not overwrite your current scene. Save the generated scene as Assets/Scenes/BLACKSITE_MainScene.unity.",
            "Generate", "Cancel")) return;

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        scene.name = "BLACKSITE_MainScene";

        worldStatic = Root("World_Static");
        corridors = Root("Corridors");
        doors = Root("Doors_Dynamic");
        roomVolumes = Root("RoomVolumes");
        coverPoints = Root("CoverPoints");
        intercepts = Root("InterceptWaypoints");
        lighting = Root("Lighting");
        spawns = Root("AgentSpawns");
        interactables = Root("Interactables");
        Root("Nav_Walkability");
        Root("PlayerSpawn");
        Root("Extraction");

        // Six room footprints: centres X/Z, dimensions X/Z.
        Room("ENTRY", -29f, 0f, 10f, 10f);
        Room("HUB", -12f, 0f, 12f, 12f);
        Room("ACOUSTIC", -12f, 18f, 12f, 10f);
        Room("SECURITY", 8f, 18f, 12f, 10f);
        Room("MAINT", 8f, 0f, 12f, 12f);
        Room("CORE", 8f, -18f, 12f, 10f);

        // Physical corridor floor slabs; doors are intentionally left as placeholders/markers.
        Corridor("ENTRY_HUB", new Vector3(-21f, 0f, 0f), new Vector3(5f, 0.2f, CorridorWidth));
        Corridor("HUB_ACOUSTIC", new Vector3(-12f, 0f, 9.5f), new Vector3(CorridorWidth, 0.2f, 7f));
        Corridor("ACOUSTIC_SECURITY", new Vector3(-2f, 0f, 18f), new Vector3(8f, 0.2f, CorridorWidth));
        Corridor("SECURITY_MAINT", new Vector3(8f, 0f, 9.5f), new Vector3(CorridorWidth, 0.2f, 7f));
        Corridor("HUB_MAINT", new Vector3(-2f, 0f, 0f), new Vector3(8f, 0.2f, CorridorWidth));
        Corridor("MAINT_CORE", new Vector3(8f, 0f, -9.5f), new Vector3(CorridorWidth, 0.2f, 7f));

        // West bypass: leave the hub south, run west, then travel south/east toward CORE.
        Corridor("HUB_CORE_BYPASS_WEST", new Vector3(-12f, 0f, -8f), new Vector3(CorridorWidth, 0.2f, 4f));
        Corridor("HUB_CORE_BYPASS_SOUTH", new Vector3(-2f, 0f, -10f), new Vector3(20f, 0.2f, CorridorWidth));
        Corridor("HUB_CORE_BYPASS_EAST", new Vector3(8f, 0f, -14f), new Vector3(CorridorWidth, 0.2f, 8f));

        Portal("D01_EntryHub", "ENTRY", "HUB", new Vector3(-21f, 0f, 0f));
        Portal("D02_HubAcoustic", "HUB", "ACOUSTIC", new Vector3(-12f, 0f, 9.5f));
        Portal("D03_AcousticSecurity", "ACOUSTIC", "SECURITY", new Vector3(-2f, 0f, 18f));
        Portal("D04_SecurityMaint", "SECURITY", "MAINT", new Vector3(8f, 0f, 9.5f));
        Portal("D05_HubMaint", "HUB", "MAINT", new Vector3(-2f, 0f, 0f));
        Portal("D06_MaintCore", "MAINT", "CORE", new Vector3(8f, 0f, -9.5f));
        Portal("D07_HubCoreBypass", "HUB", "CORE", new Vector3(-12f, 0f, -8f));

        // Suggested cover points in SECURITY.
        Marker(coverPoints, "CV_SEC_01", new Vector3(5f, 0f, 16f), new Vector3(1.5f, 1.2f, 1.5f), new Color(0.35f, 0.38f, 0.4f));
        Marker(coverPoints, "CV_SEC_02", new Vector3(10f, 0f, 20f), new Vector3(1.8f, 1.2f, 1.2f), new Color(0.35f, 0.38f, 0.4f));
        Marker(coverPoints, "CV_SEC_03", new Vector3(11f, 0f, 15f), new Vector3(1.2f, 1.2f, 1.8f), new Color(0.35f, 0.38f, 0.4f));
        Marker(coverPoints, "CV_HUB_01", new Vector3(-14f, 0f, -1.5f), new Vector3(1.2f, 1.2f, 1.2f), new Color(0.35f, 0.38f, 0.4f));
        Marker(coverPoints, "CV_HUB_02", new Vector3(-10f, 0f, 1.5f), new Vector3(1.2f, 1.2f, 1.2f), new Color(0.35f, 0.38f, 0.4f));

        Marker(intercepts, "IP_CORE_NORTH", new Vector3(8f, 0f, -13f), new Vector3(0.8f, 0.05f, 0.8f), Color.green);
        Marker(intercepts, "IP_CORE_WEST", new Vector3(2f, 0f, -18f), new Vector3(0.8f, 0.05f, 0.8f), Color.green);
        Marker(intercepts, "IP_HUB_EAST", new Vector3(-6f, 0f, 0f), new Vector3(0.8f, 0.05f, 0.8f), Color.green);

        // Objective and player/extraction markers.
        Marker(spawns, "PlayerSpawnPoint", new Vector3(-32f, 0.1f, 0f), new Vector3(0.7f, 1.8f, 0.7f), Color.cyan);
        Marker(interactables, "ResonanceCore_PLACEHOLDER", new Vector3(8f, 0.8f, -18f), new Vector3(1.2f, 1.6f, 1.2f), new Color(0.2f, 0.9f, 1f));
        Marker(Root("Extraction"), "ExtractionPoint", new Vector3(-32f, 0.05f, 2.5f), new Vector3(2f, 0.05f, 2f), Color.yellow);

        // Simple lighting setup.
        GameObject sun = new GameObject("Key_Light");
        sun.transform.SetParent(lighting);
        sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        Light dir = sun.AddComponent<Light>();
        dir.type = LightType.Directional;
        dir.intensity = 0.8f;

        AddPointLight("ENTRY_CoolLight", new Vector3(-29f, 3f, 0f), new Color(0.75f, 0.85f, 1f), 5f, 10f);
        AddPointLight("HUB_CyanLight", new Vector3(-12f, 3f, 0f), new Color(0.2f, 0.8f, 1f), 4f, 12f);
        AddPointLight("ACOUSTIC_TealLight", new Vector3(-12f, 3f, 18f), new Color(0.15f, 0.9f, 0.8f), 4f, 10f);
        AddPointLight("SECURITY_AmberLight", new Vector3(8f, 3f, 18f), new Color(1f, 0.55f, 0.15f), 4f, 10f);
        AddPointLight("MAINT_AmberLight", new Vector3(8f, 3f, 0f), new Color(1f, 0.35f, 0.12f), 3f, 10f);
        AddPointLight("CORE_CyanLight", new Vector3(8f, 3f, -18f), new Color(0.15f, 0.75f, 1f), 5f, 12f);

        // Put a useful camera view over the full level.
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = new Vector3(-8f, 48f, 1f);
            cam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            cam.orthographic = true;
            cam.orthographicSize = 34f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.09f, 0.1f);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = worldStatic.gameObject;
        EditorUtility.DisplayDialog("Greybox Created",
            "BLACKSITE-7 greybox generated. Save the scene in Assets/Scenes, then inspect corridor joins and test walkability. This is a starter blockout; review geometry before using it as the final level.",
            "OK");
    }

    private static Transform Root(string name)
    {
        GameObject go = new GameObject(name);
        return go.transform;
    }

    private static void Room(string id, float x, float z, float width, float depth)
    {
        Transform room = new GameObject(id).transform;
        room.SetParent(worldStatic);
        room.position = new Vector3(x, 0f, z);

        Cube(room, id + "_Floor", new Vector3(x, -0.1f, z), new Vector3(width, 0.2f, depth), new Color(0.22f, 0.24f, 0.26f));

        // Four boundary walls with a centered opening on sides that connect to other zones.
        // Openings are intentionally broad enough for the proposed corridor connections.
        bool northOpen = id == "HUB" || id == "ACOUSTIC";
        bool southOpen = id == "HUB" || id == "MAINT" || id == "CORE";
        bool eastOpen = id == "HUB" || id == "ACOUSTIC" || id == "SECURITY" || id == "MAINT";
        bool westOpen = id == "HUB" || id == "ENTRY" || id == "CORE";

        WallSide(room, id, x, z + depth / 2f, width, true, northOpen, id == "ACOUSTIC" ? "D03" : "D02");
        WallSide(room, id, x, z - depth / 2f, width, true, southOpen, id == "CORE" ? "D06/D07" : "D07");
        WallSide(room, id, x + width / 2f, z, depth, false, eastOpen, id == "SECURITY" ? "D04" : "D05");
        WallSide(room, id, x - width / 2f, z, depth, false, westOpen, id == "ENTRY" ? "D01" : "D07");

        // Room volume trigger placeholder (BoxCollider intentionally not a trigger script).
        GameObject volume = new GameObject(id + "_RoomVolume");
        volume.transform.SetParent(roomVolumes);
        volume.transform.position = new Vector3(x, 1.5f, z);
        BoxCollider box = volume.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(width - 0.2f, 3f, depth - 0.2f);
    }

    private static void WallSide(Transform parent, string id, float x, float z, float length, bool alongX, bool open, string doorLabel)
    {
        float opening = open ? DoorWidth : 0f;
        float sideLength = (length - opening) / 2f;
        if (sideLength < 0.5f)
        {
            Cube(parent, id + "_Wall", new Vector3(x, WallHeight / 2f, z),
                alongX ? new Vector3(length, WallHeight, WallThickness) : new Vector3(WallThickness, WallHeight, length),
                new Color(0.34f, 0.36f, 0.38f));
            return;
        }

        Vector3 offset = alongX ? new Vector3((opening / 2f + sideLength / 2f), WallHeight / 2f, 0f)
                                : new Vector3(0f, WallHeight / 2f, (opening / 2f + sideLength / 2f));
        Vector3 size = alongX ? new Vector3(sideLength, WallHeight, WallThickness)
                              : new Vector3(WallThickness, WallHeight, sideLength);
        Cube(parent, id + "_Wall_A", new Vector3(x, 0f, z) - offset, size, new Color(0.34f, 0.36f, 0.38f));
        Cube(parent, id + "_Wall_B", new Vector3(x, 0f, z) + offset, size, new Color(0.34f, 0.36f, 0.38f));

        // Header above the opening.
        if (open)
        {
            float headerHeight = WallHeight - DoorHeight;
            if (headerHeight > 0.1f)
                Cube(parent, id + "_DoorHeader_" + doorLabel,
                    new Vector3(x, DoorHeight + headerHeight / 2f, z),
                    alongX ? new Vector3(DoorWidth, headerHeight, WallThickness) : new Vector3(WallThickness, headerHeight, DoorWidth),
                    new Color(0.28f, 0.3f, 0.32f));
        }
    }

    private static void Corridor(string name, Vector3 centre, Vector3 size)
    {
        Cube(corridors, name + "_Floor", new Vector3(centre.x, -0.1f, centre.z), size, new Color(0.18f, 0.2f, 0.22f));
        // Side walls only on narrow corridor spans; these are deliberately simple and need visual inspection.
        if (size.x <= CorridorWidth + 0.01f)
        {
            Cube(corridors, name + "_Wall_A", new Vector3(centre.x - size.x / 2f - WallThickness / 2f, WallHeight / 2f, centre.z),
                new Vector3(WallThickness, WallHeight, size.z), new Color(0.3f, 0.32f, 0.34f));
            Cube(corridors, name + "_Wall_B", new Vector3(centre.x + size.x / 2f + WallThickness / 2f, WallHeight / 2f, centre.z),
                new Vector3(WallThickness, WallHeight, size.z), new Color(0.3f, 0.32f, 0.34f));
        }
        else
        {
            Cube(corridors, name + "_Wall_A", new Vector3(centre.x, WallHeight / 2f, centre.z - size.z / 2f - WallThickness / 2f),
                new Vector3(size.x, WallHeight, WallThickness), new Color(0.3f, 0.32f, 0.34f));
            Cube(corridors, name + "_Wall_B", new Vector3(centre.x, WallHeight / 2f, centre.z + size.z / 2f + WallThickness / 2f),
                new Vector3(size.x, WallHeight, WallThickness), new Color(0.3f, 0.32f, 0.34f));
        }
    }

    private static void Portal(string id, string roomA, string roomB, Vector3 pos)
    {
        GameObject go = new GameObject(id);
        go.transform.SetParent(doors);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(DoorWidth, DoorHeight, 0.15f);
        BoxCollider trigger = go.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        go.AddComponent<PortalPlaceholderInfo>().Set(roomA, roomB);
        // Visible marker, not an operational door.
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        marker.name = "PORTAL_MARKER_" + id;
        marker.transform.SetParent(go.transform, false);
        marker.transform.localPosition = Vector3.zero;
        marker.transform.localScale = new Vector3(1f, 1f, 0.15f);
        marker.GetComponent<Renderer>().sharedMaterial = MakeMaterial(new Color(0.05f, 0.75f, 0.65f));
        Object.DestroyImmediate(marker.GetComponent<Collider>());
    }

    private static void Marker(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        marker.name = name;
        marker.transform.SetParent(parent);
        marker.transform.position = position;
        marker.transform.localScale = scale;
        marker.GetComponent<Renderer>().sharedMaterial = MakeMaterial(color);
        if (name.StartsWith("CV_"))
        {
            // Keep these cubes as visible cover proxies with colliders.
        }
        else
        {
            Collider c = marker.GetComponent<Collider>();
            if(c != null) Object.DestroyImmediate(c);
        }
    }

    private static void AddPointLight(string name, Vector3 position, Color color, float intensity, float range)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(lighting);
        go.transform.position = position;
        Light light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
    }

    private static GameObject Cube(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position = position;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = MakeMaterial(color);
        return go;
    }

    private static Material MakeMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material mat = new Material(shader);
        mat.color = color;
        return mat;
    }
}

// Placeholder metadata for integration; this is not a working interactive door.
public class PortalPlaceholderInfo : MonoBehaviour
{
    public string DoorId;
    public string RoomA;
    public string RoomB;
    public bool IsOpen = true;
    public bool IsLocked = false;
    public float AcousticAttenuation = 1f;

    public void Set(string roomA, string roomB)
    {
        DoorId = gameObject.name;
        RoomA = roomA;
        RoomB = roomB;
    }
}
