using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System;
using System.Reflection;
using System.IO;

public static class BLACKSITEWorldBuilderFinishPass
{
    const string Root = "Assets/BLACKSITE_WorldBuilder";
    const string Mats = Root + "/Materials";

    [MenuItem("Tools/BLACKSITE-7/Finish World Builder Pass")]
    static void Run()
    {
        if (!EditorUtility.DisplayDialog("BLACKSITE-7 Finish Pass",
            "Save a backup first. This applies industrial materials and lighting to the open scene and attempts to build a NavMesh. Continue?", "Continue", "Cancel")) return;

        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded) { EditorUtility.DisplayDialog("No scene", "Open BLACKSITE_MainScene first.", "OK"); return; }
        EnsureFolder(Root); EnsureFolder(Mats);

        Material concrete = Mat("M_Concrete_Dark", new Color(.29f,.31f,.33f), .12f, .08f);
        Material floor = Mat("M_Industrial_Floor", new Color(.18f,.20f,.22f), .10f, .12f);
        Material metal = Mat("M_Painted_Metal", new Color(.39f,.43f,.46f), .38f, .42f);
        Material cyan = Mat("M_Portal_Cyan", new Color(.08f,.78f,.68f), .65f, .18f);
        Material core = Mat("M_Resonance_Core", new Color(.12f,.82f,1f), .7f, .15f);
        Material cover = Mat("M_Cover_Gunmetal", new Color(.25f,.29f,.31f), .28f, .3f);

        int count=0;
        foreach (GameObject go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (!go || !go.GetComponent<Renderer>()) continue;
            string n=go.name.ToLowerInvariant();
            Material m=concrete;
            if(n.Contains("floor")) m=floor;
            else if(n.Contains("portal_marker") || n.StartsWith("d0")) m=cyan;
            else if(n.Contains("resonancecore")) m=core;
            else if(n.StartsWith("cv_") || n.Contains("cover")) m=cover;
            else if(n.Contains("doorheader") || n.Contains("metal") || n.Contains("pipe") || n.Contains("crate")) m=metal;
            else if(n.Contains("wall")) m=concrete;
            go.GetComponent<Renderer>().sharedMaterial=m; count++;
            if(n.Contains("floor") || n.Contains("wall") || n.Contains("header"))
                GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ContributeGI);
        }

        LightAt("ENTRY_CoolLight",new Vector3(-29,3,0),new Color(.72f,.84f,1f),3.5f,11);
        LightAt("HUB_CyanLight",new Vector3(-12,3,0),new Color(.22f,.78f,1f),3.2f,13);
        LightAt("ACOUSTIC_TealLight",new Vector3(-12,3,18),new Color(.18f,.88f,.78f),3.2f,11);
        LightAt("SECURITY_AmberLight",new Vector3(8,3,18),new Color(1f,.58f,.2f),3f,11);
        LightAt("MAINT_RedAmberLight",new Vector3(8,3,0),new Color(1f,.28f,.14f),2.5f,10);
        LightAt("CORE_CyanLight",new Vector3(8,3,-18),new Color(.12f,.78f,1f),4f,13);
        RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight=new Color(.18f,.20f,.23f);

        bool built=TryBuildNavMesh();
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        EditorUtility.DisplayDialog("Finish Pass complete",
            "Materials assigned to "+count+" renderers. Room lights updated. Static flags set for walls/floors. NavMesh build attempted: "+(built?"YES — inspect the blue NavMesh visualization to verify it":"NO — follow Documentation/NavMesh_Bake_Guide.md")+
            ".\n\nThis is still a greybox: inspect joins, bake lighting manually, and test walkability before submission.", "OK");
    }

    static Material Mat(string name, Color c, float smooth, float metallic)
    {
        string path=Mats+"/"+name+".mat";
        Material m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){ Shader s=Shader.Find("Universal Render Pipeline/Lit"); if(s==null)s=Shader.Find("Standard"); m=new Material(s); AssetDatabase.CreateAsset(m,path); }
        m.color=c;
        if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",smooth);
        if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",metallic);
        if(name.Contains("Cyan")||name.Contains("Core")){
            if(m.HasProperty("_EmissionColor"))m.SetColor("_EmissionColor",c*.25f);
            m.EnableKeyword("_EMISSION");
        }
        EditorUtility.SetDirty(m); return m;
    }

    static void LightAt(string name, Vector3 p, Color c, float intensity, float range)
    {
        GameObject g=GameObject.Find(name);
        if(g==null){g=new GameObject(name); var parent=GameObject.Find("Lighting"); if(parent)g.transform.SetParent(parent.transform);}
        g.transform.position=p;
        Light l=g.GetComponent<Light>(); if(!l)l=g.AddComponent<Light>();
        l.type=LightType.Point; l.color=c; l.intensity=intensity; l.range=range; l.shadows=LightShadows.Soft;
    }

    static bool TryBuildNavMesh()
    {
        GameObject root=GameObject.Find("Nav_Walkability"); if(!root)root=new GameObject("Nav_Walkability");
        Type t=Type.GetType("Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation");
        if(t==null) foreach(Assembly a in AppDomain.CurrentDomain.GetAssemblies()){t=a.GetType("Unity.AI.Navigation.NavMeshSurface");if(t!=null)break;}
        if(t==null)return false;
        Component c=root.GetComponent(t); if(!c)c=root.AddComponent(t);
        MethodInfo build=t.GetMethod("BuildNavMesh",BindingFlags.Public|BindingFlags.Instance);
        if(build==null)return false;
        try{build.Invoke(c,null);return true;}catch(Exception e){Debug.LogWarning("NavMesh build failed: "+e.Message);return false;}
    }

    static void EnsureFolder(string path)
    {
        if(AssetDatabase.IsValidFolder(path))return;
        string parent=Path.GetDirectoryName(path).Replace("\\","/");
        if(!AssetDatabase.IsValidFolder(parent))EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent,Path.GetFileName(path));
    }
}
