using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ARNoteReadinessCheck
{
    private const string MainScenePath = "Assets/Scenes/MainScene.unity";
    private const string ManifestPath = "Packages/manifest.json";

    [MenuItem("AR Note/Run Readiness Check")]
    public static void Run()
    {
        List<string> issues = new List<string>();

        CheckBuildScene(issues);
        CheckScriptAssets(issues);
        CheckMainScene(issues);
        CheckNotificationPackage(issues);

        if (issues.Count == 0)
        {
            Debug.Log("AR Note readiness check passed. Main scene, core scripts, Vuforia placement, persistence, styling, reminders, and voice managers are present.");
            EditorUtility.DisplayDialog("AR Note Readiness", "Passed. The project is ready for member testing.", "OK");
            return;
        }

        string message = string.Join("\n", issues);
        Debug.LogWarning("AR Note readiness check found issues:\n" + message);
        EditorUtility.DisplayDialog("AR Note Readiness", message, "OK");
    }

    private static void CheckBuildScene(List<string> issues)
    {
        bool hasMainScene = false;
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled && scene.path == MainScenePath)
            {
                hasMainScene = true;
                break;
            }
        }

        if (!hasMainScene)
            issues.Add("- Add Assets/Scenes/MainScene.unity to Build Settings.");
    }

    private static void CheckScriptAssets(List<string> issues)
    {
        CheckType<PlaceNote>(issues, "PlaceNote");
        CheckType<NoteView>(issues, "NoteView");
        CheckType<NoteManager>(issues, "NoteManager");
        CheckType<DatabaseManager>(issues, "DatabaseManager");
        CheckType<StylePanelController>(issues, "StylePanelController");
        CheckType<ReminderManager>(issues, "ReminderManager");
        CheckType<VoiceNoteManager>(issues, "VoiceNoteManager");
        CheckType<NoteEditPanel>(issues, "NoteEditPanel");
    }

    private static void CheckType<T>(List<string> issues, string typeName) where T : Object
    {
        string[] guids = AssetDatabase.FindAssets(typeName + " t:script");
        if (guids.Length == 0)
            issues.Add("- Missing script: " + typeName + ".");
    }

    private static void CheckMainScene(List<string> issues)
    {
        if (!File.Exists(MainScenePath))
        {
            issues.Add("- Missing MainScene at " + MainScenePath + ".");
            return;
        }

        string sceneText = File.ReadAllText(MainScenePath);
        if (!sceneText.Contains("m_Name: Plane Finder"))
            issues.Add("- MainScene is missing the Vuforia Plane Finder object.");

        if (!sceneText.Contains("m_Name: ARCamera"))
            issues.Add("- MainScene is missing ARCamera.");

        if (!sceneText.Contains("m_Name: EventSystem"))
            issues.Add("- MainScene is missing EventSystem, so UI taps may not work correctly.");

        if (!sceneText.Contains("m_MethodName: AnchorCreated"))
            issues.Add("- Plane Finder is not wired to PlaceNote.AnchorCreated.");

        if (!sceneText.Contains("m_Name: Managers"))
            issues.Add("- MainScene is missing a Managers object. NoteManager can still self-create services, but the scene should keep one for clarity.");
    }

    private static void CheckNotificationPackage(List<string> issues)
    {
        if (!File.Exists(ManifestPath))
        {
            issues.Add("- Missing Packages/manifest.json.");
            return;
        }

        string manifest = File.ReadAllText(ManifestPath);
        if (!manifest.Contains("\"com.unity.mobile.notifications\""))
            issues.Add("- Missing com.unity.mobile.notifications package for Android reminders.");
    }
}
