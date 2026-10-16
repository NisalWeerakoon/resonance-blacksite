using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;

public static class BLACKSITERealisticBunkerSetup
{
    const string Root="Assets/BLACKSITE_RealisticBunker";
    const string Tex=Root+"/Textures";
    const string Mats=Root+"/Materials";

    [MenuItem("Tools/BLACKSITE-7/Apply Realistic Bunker Materials")]
    static void Apply()
    {
        if(!EditorUtility.DisplayDialog("BLACKSITE-7 Realistic Bunker",
          "Save a backup first. This creates textured materials, assigns them by object names, and adds industrial lights. It does not replace geometry. Continue?","Apply","Cancel")) return;
        EnsureFolder(Root); EnsureFolder(Mats); AssetDatabase.Refresh();
        Material concrete=Make("M_Weathered_Concrete","T_Concrete_Albedo.png",.18f,.05f);
        Material floor=Make("M_Grimy_Bunker_Floor","T_Floor_Albedo.png",.12f,.1f);
        Material rust=Make("M_Rusted_Steel","T_Rust_Albedo.png",.32f,.48f);
        Material wood=Make("M_Military_Crate_Wood","T_Wood_Albedo.png",.2f,.05f);
        Material sand=Make("M_Sandbag_Canvas","T_Sandbag_Albedo.png",.15f,0);
        Material hazard=Make("M_Hazard_Stripes","T_Warning_Albedo.png",.25f,.12f);
        int count=0;
        foreach(GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if(!go) continue;
            Renderer r=go.GetComponent<Renderer>(); if(!r) continue;
            string n=go.name.ToLowerInvariant(); Material m=null;
            if(n.Contains("floor")||n.Contains("ground")||n.Contains("walkable")) m=floor;
            else if(n.Contains("wall")||n.Contains("ceiling")||n.Contains("room")||n.Contains("corridor")) m=concrete;
            else if(n.Contains("door")||n.Contains("pipe")||n.Contains("barrel")||n.Contains("metal")||n.Contains("vent")) m=rust;
            else if(n.Contains("crate")||n.Contains("box")||n.Contains("wood")) m=wood;
            else if(n.Contains("sandbag")) m=sand;
            else if(n.Contains("hazard")||n.Contains("warning")||n.Contains("stripe")) m=hazard;
            if(m){r.sharedMaterial=m;count++;}
            if(n.Contains("floor")||n.Contains("wall")||n.Contains("ceiling")||n.Contains("corridor"))
                GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.BatchingStatic|StaticEditorFlags.OccluderStatic|StaticEditorFlags.OccludeeStatic|StaticEditorFlags.ContributeGI);
        }
        LightAt("ENTRY_WarmIndustrialLight",new Vector3(-29,2.7f,0),new Color(1,.70f,.42f),2.5f,9);
        LightAt("HUB_CoolIndustrialLight",new Vector3(-12,2.7f,0),new Color(.55f,.72f,1),2,10);
        LightAt("ACOUSTIC_TealLight",new Vector3(-12,2.7f,18),new Color(.28f,.78f,.72f),1.8f,9);
        LightAt("SECURITY_AmberLight",new Vector3(8,2.7f,18),new Color(1,.42f,.18f),1.8f,9);
        LightAt("MAINT_RedWarningLight",new Vector3(8,2.7f,0),new Color(1,.18f,.1f),1.4f,8);
        LightAt("CORE_CyanLight",new Vector3(8,2.7f,-18),new Color(.15f,.7f,1),2.5f,10);
        RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight=new Color(.16f,.17f,.18f);
        AssetDatabase.SaveAssets();
        var scene=SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        EditorUtility.DisplayDialog("Applied","Created materials and assigned "+count+" renderers. Inspect assignments, then bake lighting manually and verify NavMesh separately.","OK");
    }
    static Material Make(string name,string textureFile,float smooth,float metallic)
    {
        string path=Mats+"/"+name+".mat"; Material m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){Shader s=Shader.Find("Universal Render Pipeline/Lit");if(!s)s=Shader.Find("Standard");m=new Material(s);AssetDatabase.CreateAsset(m,path);}
        Texture2D t=AssetDatabase.LoadAssetAtPath<Texture2D>(Tex+"/"+textureFile);
        if(t){if(m.HasProperty("_BaseMap"))m.SetTexture("_BaseMap",t);if(m.HasProperty("_MainTex"))m.SetTexture("_MainTex",t);}
        if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",smooth);
        if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",metallic);
        EditorUtility.SetDirty(m);return m;
    }
    static void LightAt(string name,Vector3 pos,Color color,float intensity,float range)
    {
        GameObject g=GameObject.Find(name);
        if(!g){g=new GameObject(name);GameObject p=GameObject.Find("Lighting");if(p)g.transform.SetParent(p.transform);}
        g.transform.position=pos; Light l=g.GetComponent<Light>();if(!l)l=g.AddComponent<Light>();
        l.type=LightType.Point;l.color=color;l.intensity=intensity;l.range=range;l.shadows=LightShadows.Soft;
    }
    static void EnsureFolder(string path)
    {
        if(AssetDatabase.IsValidFolder(path))return;
        string parent=Path.GetDirectoryName(path).Replace("\\","/");
        if(!AssetDatabase.IsValidFolder(parent))EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent,Path.GetFileName(path));
    }
}