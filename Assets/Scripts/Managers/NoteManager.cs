using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
        AlarmManager.Instance?.ScheduleOrCancel(note);
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

        AlarmManager.Instance?.ScheduleOrCancel(note);
        SaveNotes();
    }

    public void RemoveNote(string noteID)
    {
        if (string.IsNullOrWhiteSpace(noteID)) return;

        NoteData note = allNotes.FirstOrDefault(n => n.id == noteID);
        if (note == null) return;

        AlarmManager.Instance?.Cancel(note);

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
                AlarmManager.Instance?.Cancel(note);
        }

        foreach (NoteView view in activeViews.Values.ToList())
        {
            if (view == null) continue;
            Destroy(view.AnchorRoot != null ? view.AnchorRoot : view.gameObject);
        }

        activeViews.Clear();
        allNotes.Clear();
        SelectedNote = null;

        VoiceNoteManager.Instance?.ClearAllVoiceFiles();
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

        if (FindObjectOfType<AlarmManager>() == null)
            gameObject.AddComponent<AlarmManager>();

        if (FindObjectOfType<VoiceNoteManager>() == null)
            gameObject.AddComponent<VoiceNoteManager>();

        if (FindObjectOfType<SpeechToTextManager>() == null)
            gameObject.AddComponent<SpeechToTextManager>();

        NoteEditPanel.EnsureExists();
    }
}
