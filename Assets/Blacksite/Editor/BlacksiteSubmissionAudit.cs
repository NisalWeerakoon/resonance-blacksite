using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Blacksite.Editor
{
    public static class BlacksiteSubmissionAudit
    {
        private const string ScenePath = "Assets/Blacksite/Scenes/Integration/SubmissionPlayable.unity";

        [MenuItem("BLACKSITE/Audit submission scene")]
        public static void Audit()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            int objects = 0;
            int missing = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Debug.Log($"SUBMISSION_ROOT {root.name} children={root.transform.childCount}");
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                {
                    objects++;
                    missing += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject);
                    if (child.name == "AgentWaypointGraph" || child.parent != null &&
                        child.parent.name == "AgentWaypointGraph")
                        Debug.Log($"SUBMISSION_GRAPH_OBJECT {child.name} pos={child.position}");
                    if (child.name == "ECHO_World_Visual" || child.name == "BASTION_World_Visual" ||
                        child.name == "RIFT_World_Visual" || child.name == "TRACE_World_Visual" ||
                        child.name == "DemoPlayer_FPS" || child.name == "AgentWaypointGraph")
                    {
                        foreach (MonoBehaviour component in child.GetComponents<MonoBehaviour>())
                        {
                            if (component == null) continue;
                            var serialized = new SerializedObject(component);
                            var property = serialized.GetIterator();
                            while (property.NextVisible(true))
                            {
                                if (property.propertyType == SerializedPropertyType.ObjectReference)
                                    Debug.Log($"SUBMISSION_REF {child.name}/{component.GetType().Name}.{property.propertyPath}={property.objectReferenceValue?.name ?? "null"}");
                                else if (property.isArray && property.propertyType != SerializedPropertyType.String)
                                    Debug.Log($"SUBMISSION_ARRAY {child.name}/{component.GetType().Name}.{property.propertyPath}={property.arraySize}");
                            }
                        }
                    }
                    if (child.name.Contains("D0") || child.name.Contains("Core") ||
                        child.name.Contains("Player") || child.name.Contains("ECHO") ||
                        child.name.Contains("BASTION") || child.name.Contains("RIFT") ||
                        child.name.Contains("TRACE") || child.name.Contains("AgentSpawns"))
                    {
                        string scripts = string.Join(",", child.GetComponents<MonoBehaviour>()
                            .Where(x => x != null).Select(x => x.GetType().Name));
                        Debug.Log($"SUBMISSION_OBJECT {child.name} pos={child.position} active={child.gameObject.activeSelf} scripts={scripts}");
                    }
                }
            }
            Debug.Log($"SUBMISSION_AUDIT objects={objects} missingScripts={missing}");
        }
    }
}
