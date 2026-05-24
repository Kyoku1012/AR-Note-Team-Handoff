using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class EnsureNoteEditPanelPrefabInScene
{
    private const string ScenePath = "Assets/Scenes/MainScene.unity";
    private const string PrefabPath = "Assets/Prefabs/UI/NoteEditPanel.prefab";
    private static readonly string[] IconIds = { "star", "finish", "inprocess", "reminder", "work", "study", "shopping" };

    public static void Run()
    {
        EnsurePrefabStaticReferences();
        EditorSceneManager.OpenScene(ScenePath);

        NoteEditPanel existing = Object.FindObjectOfType<NoteEditPanel>(true);
        if (existing == null)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
                throw new System.InvalidOperationException("Missing prefab at " + PrefabPath);

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = prefab.name;
            instance.transform.SetAsLastSibling();
            Undo.RegisterCreatedObjectUndo(instance, "Instantiate NoteEditPanel prefab");
            Debug.Log("Instantiated NoteEditPanel prefab into " + ScenePath);
        }
        else
        {
            GameObject instanceRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(existing.gameObject);
            Transform rootTransform = instanceRoot == null ? existing.transform : instanceRoot.transform;
            rootTransform.SetAsLastSibling();
            Debug.Log("Scene already contains NoteEditPanel: " + existing.name);
        }

        EnsureEventSystem();
        ValidateNoteEditPanelReferences();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("NoteEditPanel prefab scene setup complete.");
    }

    private static void EnsurePrefabStaticReferences()
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            NoteEditPanel panel = prefabRoot.GetComponentInChildren<NoteEditPanel>(true);
            if (panel == null)
                throw new System.InvalidOperationException("NoteEditPanel component is missing from " + PrefabPath);

            SerializedObject serialized = new SerializedObject(panel);
            SerializedProperty iconButtons = serialized.FindProperty("iconButtonsSerialized");
            Image[] iconSpriteImages = new Image[IconIds.Length];
            Text[] iconFallbackLabels = new Text[IconIds.Length];

            for (int i = 0; i < IconIds.Length; i++)
            {
                Button button = GetArrayObjectReference<Button>(iconButtons, i);
                if (button == null)
                    continue;

                iconSpriteImages[i] = EnsureIconSpriteImage(button.transform, IconIds[i] + "Icon");
                iconFallbackLabels[i] = button.GetComponentsInChildren<Text>(true)
                    .FirstOrDefault(text => text.gameObject.name == IconIds[i] + "Label")
                    ?? button.GetComponentInChildren<Text>(true);
            }

            SetObjectReferenceArray(serialized.FindProperty("iconSpriteImages"), iconSpriteImages);
            SetObjectReferenceArray(serialized.FindProperty("iconFallbackLabelsSerialized"), iconFallbackLabels);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
            Debug.Log("Validated NoteEditPanel prefab static icon targets.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static Image EnsureIconSpriteImage(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        GameObject iconObject = existing == null ? new GameObject(name) : existing.gameObject;
        if (existing == null)
            iconObject.transform.SetParent(parent, false);

        RectTransform rect = iconObject.GetComponent<RectTransform>();
        if (rect == null)
            rect = iconObject.AddComponent<RectTransform>();

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(24f, 24f);

        Image image = iconObject.GetComponent<Image>();
        if (image == null)
            image = iconObject.AddComponent<Image>();

        image.color = Color.white;
        image.raycastTarget = false;
        image.preserveAspect = true;
        iconObject.SetActive(false);
        return image;
    }

    private static T GetArrayObjectReference<T>(SerializedProperty arrayProperty, int index) where T : Object
    {
        if (arrayProperty == null || !arrayProperty.isArray || index >= arrayProperty.arraySize)
            return null;

        return arrayProperty.GetArrayElementAtIndex(index).objectReferenceValue as T;
    }

    private static void SetObjectReferenceArray<T>(SerializedProperty arrayProperty, T[] values) where T : Object
    {
        if (arrayProperty == null || values == null)
            return;

        arrayProperty.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            arrayProperty.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
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
            "launcherRoot",
            "iconSpriteImages",
            "iconFallbackLabelsSerialized"
        };

        string[] missing = requiredProperties
            .Where(propertyName =>
            {
                SerializedProperty property = serialized.FindProperty(propertyName);
                if (property == null)
                    return true;

                if (property.isArray)
                    return property.arraySize < IconIds.Length || Enumerable.Range(0, IconIds.Length)
                        .Any(index => property.GetArrayElementAtIndex(index).objectReferenceValue == null);

                return property.objectReferenceValue == null;
            })
            .ToArray();

        if (missing.Length > 0)
            throw new System.InvalidOperationException("NoteEditPanel prefab has missing required references: " + string.Join(", ", missing));
    }
}
