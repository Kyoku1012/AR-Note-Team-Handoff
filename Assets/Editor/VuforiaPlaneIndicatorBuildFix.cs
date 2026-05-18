using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using Vuforia;

public class VuforiaPlaneIndicatorBuildFix : IPreprocessBuildWithReport
{
    private const string LocalPlaneIndicatorPath = "Assets/Prefabs/Vuforia/DefaultPlaneIndicator.prefab";

    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        GameObject localPlaneIndicator = AssetDatabase.LoadAssetAtPath<GameObject>(LocalPlaneIndicatorPath);
        if (localPlaneIndicator == null)
        {
            Debug.LogWarning("Local Vuforia plane indicator prefab was not found at " + LocalPlaneIndicatorPath);
            return;
        }

        foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
        {
            if (!buildScene.enabled || string.IsNullOrWhiteSpace(buildScene.path))
                continue;

            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene(buildScene.path, OpenSceneMode.Single);
            bool changed = AssignLocalPlaneIndicator(localPlaneIndicator);
            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("Updated Vuforia PlaneFinder PlaneIndicator reference in " + buildScene.path);
            }
        }
    }

    private static bool AssignLocalPlaneIndicator(GameObject localPlaneIndicator)
    {
        bool changed = false;
        PlaneFinderBehaviour[] planeFinders = Object.FindObjectsOfType<PlaneFinderBehaviour>(true);

        foreach (PlaneFinderBehaviour planeFinder in planeFinders)
        {
            SerializedObject serializedObject = new SerializedObject(planeFinder);
            SerializedProperty planeIndicator = serializedObject.FindProperty("PlaneIndicator");
            if (planeIndicator == null)
                continue;

            if (planeIndicator.objectReferenceValue == localPlaneIndicator)
                continue;

            planeIndicator.objectReferenceValue = localPlaneIndicator;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(planeFinder);
            changed = true;
        }

        return changed;
    }
}
