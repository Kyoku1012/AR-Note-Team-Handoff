using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

public static class EnsureNoteEditPanelPrefabInScene
{
    private const string ScenePath = "Assets/Scenes/MainScene.unity";
    private const string PrefabPath = "Assets/Prefabs/UI/NoteEditPanel.prefab";

    public static void Run()
    {
        EditorSceneManager.OpenScene(ScenePath);

        NoteEditPanel existing = Object.FindObjectOfType<NoteEditPanel>(true);
        if (existing == null)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
                throw new System.InvalidOperationException("Missing prefab at " + PrefabPath);

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = prefab.name;
            Undo.RegisterCreatedObjectUndo(instance, "Instantiate NoteEditPanel prefab");
            Debug.Log("Instantiated NoteEditPanel prefab into " + ScenePath);
        }
        else
        {
            Debug.Log("Scene already contains NoteEditPanel: " + existing.name);
        }

        EnsureEventSystem();
        ValidateNoteEditPanelReferences();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("NoteEditPanel prefab scene setup complete.");
    }

    private static void EnsureEventSystem()
    {
        EventSystem eventSystem = Object.FindObjectOfType<EventSystem>(true);
        if (eventSystem != null)
            return;

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
        Debug.Log("Created EventSystem for NoteEditPanel prefab UI.");
    }

    private static void ValidateNoteEditPanelReferences()
    {
        NoteEditPanel panel = Object.FindObjectOfType<NoteEditPanel>(true);
        if (panel == null)
            throw new System.InvalidOperationException("NoteEditPanel is missing after prefab setup.");

        SerializedObject serialized = new SerializedObject(panel);
        string[] requiredProperties =
        {
            "noteInput",
            "titleInput",
            "reminderSummaryText",
            "reminderToggle",
            "speechStatusText",
            "completeCheckmark",
            "completeButtonBackground",
            "completeCheckboxBackground",
            "launcherRoot"
        };

        string[] missing = requiredProperties
            .Where(propertyName =>
            {
                SerializedProperty property = serialized.FindProperty(propertyName);
                return property == null || property.objectReferenceValue == null;
            })
            .ToArray();

        if (missing.Length > 0)
            throw new System.InvalidOperationException("NoteEditPanel prefab has missing required references: " + string.Join(", ", missing));
    }
}
