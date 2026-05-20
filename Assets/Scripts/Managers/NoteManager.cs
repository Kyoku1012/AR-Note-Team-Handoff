using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class NoteManager : MonoBehaviour
{
    public static NoteManager Instance { get; private set; }

    public List<NoteData> allNotes = new List<NoteData>();
    public DatabaseManager databaseManager;

    public NoteView SelectedNote { get; private set; }
    public event Action<NoteView> NoteSelected;

    private readonly Dictionary<string, NoteView> activeViews = new Dictionary<string, NoteView>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureDatabaseManager();
        LoadNotes();
        EnsureRuntimeServices();
        ReminderManager.Instance?.RescheduleAll(allNotes);
    }

    public void RegisterView(NoteView view)
    {
        if (view == null || view.Data == null || string.IsNullOrWhiteSpace(view.Data.id))
            return;

        if (activeViews.TryGetValue(view.Data.id, out NoteView existingView) && existingView != null && existingView != view)
        {
            Debug.LogWarning("Duplicate note id detected at runtime. Assigning a new id to keep note views independent.");
            view.Data.id = Guid.NewGuid().ToString();
        }

        activeViews[view.Data.id] = view;
    }

    public void UnregisterView(NoteView view)
    {
        if (view == null || view.Data == null || string.IsNullOrWhiteSpace(view.Data.id))
            return;

        if (activeViews.TryGetValue(view.Data.id, out NoteView current) && current == view)
            activeViews.Remove(view.Data.id);
    }

    public void SelectNote(NoteView view)
    {
        if (SelectedNote != null && SelectedNote != view)
            SelectedNote.SetSelectedVisual(false);

        SelectedNote = view;
        SelectedNote?.SetSelectedVisual(true);
        NoteSelected?.Invoke(view);
    }

    public void AddNote(NoteData note)
    {
        if (note == null) return;

        note.ApplyDefaults();
        if (allNotes.Any(n => n.id == note.id))
        {
            UpdateNote(note);
            return;
        }

        allNotes.Add(note);
        ReminderManager.Instance?.ScheduleOrCancel(note);
        SaveNotes();
    }

    public NoteData GetNote(string noteID)
    {
        if (string.IsNullOrWhiteSpace(noteID)) return null;
        return allNotes.FirstOrDefault(n => n.id == noteID);
    }

    public List<NoteData> GetAllNotes()
    {
        return allNotes;
    }

    public NoteView GetView(string noteID)
    {
        if (string.IsNullOrWhiteSpace(noteID)) return null;
        activeViews.TryGetValue(noteID, out NoteView view);
        return view;
    }

    public NoteView GetFirstView()
    {
        return activeViews.Values.FirstOrDefault(view => view != null);
    }

    public void OpenNoteFromNotification(string noteID)
    {
        if (string.IsNullOrWhiteSpace(noteID))
            return;

        RuntimeNotificationOpenOverlay.Show();
        StartCoroutine(OpenNoteFromNotificationWhenReady(noteID));
    }

    public void UpdateNote(NoteData note)
    {
        if (note == null) return;

        note.ApplyDefaults();
        int index = allNotes.FindIndex(n => n.id == note.id);
        if (index < 0)
        {
            AddNote(note);
            return;
        }

        allNotes[index] = note;
        if (activeViews.TryGetValue(note.id, out NoteView view))
            view.RefreshFromData();

        ReminderManager.Instance?.ScheduleOrCancel(note);
        SaveNotes();
    }

    public void RemoveNote(string noteID)
    {
        if (string.IsNullOrWhiteSpace(noteID)) return;

        NoteData note = allNotes.FirstOrDefault(n => n.id == noteID);
        if (note == null) return;

        ReminderManager.Instance?.Cancel(note);

        if (activeViews.TryGetValue(noteID, out NoteView view) && view != null)
            Destroy(view.AnchorRoot != null ? view.AnchorRoot : view.gameObject);

        activeViews.Remove(noteID);
        allNotes.Remove(note);
        SaveNotes();
    }

    public void ToggleCompleted(string noteID, bool isCompleted)
    {
        NoteData note = GetNote(noteID);
        if (note == null) return;

        note.isCompleted = isCompleted;
        UpdateNote(note);
    }

    public void ClearAllNotesAndData()
    {
        foreach (NoteData note in allNotes)
        {
            if (note != null)
                ReminderManager.Instance?.Cancel(note);
        }

        foreach (NoteView view in activeViews.Values.ToList())
        {
            if (view == null) continue;
            Destroy(view.AnchorRoot != null ? view.AnchorRoot : view.gameObject);
        }

        activeViews.Clear();
        allNotes.Clear();
        SelectedNote = null;

        EnsureDatabaseManager();
        databaseManager?.ClearAllSavedData();
    }

    public void SaveNotes()
    {
        EnsureDatabaseManager();
        if (databaseManager == null)
        {
            Debug.LogWarning("DatabaseManager not assigned, cannot save notes.");
            return;
        }

        databaseManager.SaveNotes(allNotes);
    }

    public void LoadNotes()
    {
        EnsureDatabaseManager();
        if (databaseManager == null)
        {
            Debug.LogWarning("DatabaseManager not assigned, cannot load notes.");
            return;
        }

        List<NoteData> loaded = databaseManager.LoadNotes();
        allNotes = loaded ?? new List<NoteData>();
        HashSet<string> seenIds = new HashSet<string>();
        foreach (NoteData note in allNotes)
        {
            note.ApplyDefaults();
            if (!seenIds.Add(note.id))
            {
                Debug.LogWarning("Duplicate saved note id detected. Assigning a new id while loading.");
                note.id = Guid.NewGuid().ToString();
                seenIds.Add(note.id);
            }
        }
    }

    private void EnsureDatabaseManager()
    {
        if (databaseManager != null) return;

        databaseManager = FindObjectOfType<DatabaseManager>();
        if (databaseManager != null) return;

        GameObject managerObject = gameObject.name == "Managers" ? gameObject : new GameObject("Managers");
        databaseManager = managerObject.AddComponent<DatabaseManager>();
    }

    private void EnsureRuntimeServices()
    {
        if (FindObjectOfType<ReminderManager>() == null)
            gameObject.AddComponent<ReminderManager>();

        if (FindObjectOfType<SpeechToTextManager>() == null)
            gameObject.AddComponent<SpeechToTextManager>();

        NoteEditPanel.EnsureExists();
    }

    private IEnumerator OpenNoteFromNotificationWhenReady(string noteID)
    {
        const int maxFramesToWait = 90;

        for (int i = 0; i < maxFramesToWait; i++)
        {
            NoteView view = GetView(noteID);
            if (view != null)
            {
                view.OpenEditor();
                RuntimeNotificationOpenOverlay.Hide();
                Debug.Log("Opened note from reminder notification: " + noteID);
                yield break;
            }

            yield return null;
        }

        NoteData note = GetNote(noteID);
        if (note != null)
        {
            SelectNote(null);
            RuntimeNotificationOpenOverlay.Hide();
            Debug.LogWarning("Reminder notification matched a saved note, but its view is not restored yet: " + noteID);
            yield break;
        }

        RuntimeNotificationOpenOverlay.Hide();
        Debug.LogWarning("Reminder notification referenced a note that no longer exists: " + noteID);
    }
}

public class RuntimeNotificationOpenOverlay : MonoBehaviour
{
    private static RuntimeNotificationOpenOverlay instance;
    private CanvasGroup canvasGroup;

    public static void Show()
    {
        EnsureExists();
        if (instance.canvasGroup == null)
            return;

        instance.canvasGroup.alpha = 1f;
        instance.canvasGroup.gameObject.SetActive(true);
    }

    public static void Hide()
    {
        if (instance == null || instance.canvasGroup == null)
            return;

        instance.canvasGroup.gameObject.SetActive(false);
    }

    private static void EnsureExists()
    {
        if (instance != null)
            return;

        GameObject root = new GameObject("RuntimeNotificationOpenOverlay");
        instance = root.AddComponent<RuntimeNotificationOpenOverlay>();
        instance.BuildUi(root);
        DontDestroyOnLoad(root);
    }

    private void BuildUi(GameObject root)
    {
        Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;
        root.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        root.AddComponent<GraphicRaycaster>();

        GameObject panel = new GameObject("OpenReminderPanel");
        panel.transform.SetParent(root.transform, false);
        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image background = panel.AddComponent<Image>();
        background.color = new Color(0.08f, 0.08f, 0.08f, 0.62f);

        canvasGroup = panel.AddComponent<CanvasGroup>();

        GameObject labelObject = new GameObject("OpeningReminderText");
        labelObject.transform.SetParent(panel.transform, false);
        RectTransform labelRect = labelObject.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 0.5f);
        labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        labelRect.anchoredPosition = Vector2.zero;
        labelRect.sizeDelta = new Vector2(280, 40);

        Text label = labelObject.AddComponent<Text>();
        label.text = "Opening reminder...";
        label.font = font;
        label.fontSize = 18;
        label.color = Color.white;
        label.alignment = TextAnchor.MiddleCenter;
        label.raycastTarget = false;

        panel.SetActive(false);
    }
}
