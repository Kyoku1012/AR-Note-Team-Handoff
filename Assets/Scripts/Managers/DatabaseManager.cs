using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class DatabaseManager : MonoBehaviour
{
    private const string SaveFileName = "ar_notes.json";

    private string SaveFilePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    [System.Serializable]
    private class NoteDataCollection
    {
        public List<NoteData> notes = new List<NoteData>();
    }

    public void SaveNote(NoteData note)
    {
        if (note == null) return;
        note.ApplyDefaults();

        List<NoteData> notes = LoadNotes() ?? new List<NoteData>();
        int index = notes.FindIndex(n => n.id == note.id);

        if (index >= 0)
        {
            notes[index] = note;
        }
        else
        {
            notes.Add(note);
        }

        SaveNotes(notes);
    }

    public void SaveNotes(List<NoteData> notes)
    {
        if (notes == null)
        {
            Debug.LogWarning("SaveNotes called with null notes list.");
            return;
        }

        try
        {
            foreach (NoteData note in notes)
                note?.ApplyDefaults();

            NoteDataCollection collection = new NoteDataCollection { notes = notes };
            string json = JsonUtility.ToJson(collection, true);

            string folder = Path.GetDirectoryName(SaveFilePath);
            if (!string.IsNullOrEmpty(folder))
            {
                Directory.CreateDirectory(folder);
            }

            File.WriteAllText(SaveFilePath, json);
            Debug.Log($"Saved {notes.Count} notes to {SaveFilePath}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to save notes: {ex.Message}");
        }
    }

    public List<NoteData> LoadNotes()
    {
        if (!File.Exists(SaveFilePath))
        {
            Debug.Log($"No saved notes found at {SaveFilePath}");
            return new List<NoteData>();
        }

        try
        {
            string json = File.ReadAllText(SaveFilePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<NoteData>();
            }

            NoteDataCollection collection = JsonUtility.FromJson<NoteDataCollection>(json);
            List<NoteData> notes = collection?.notes ?? new List<NoteData>();
            foreach (NoteData note in notes)
                note?.ApplyDefaults();

            return notes;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to load notes: {ex.Message}");
            return new List<NoteData>();
        }
    }

    public void DeleteNote(string noteID)
    {
        if (string.IsNullOrWhiteSpace(noteID)) return;

        List<NoteData> notes = LoadNotes() ?? new List<NoteData>();
        int index = notes.FindIndex(n => n.id == noteID);
        if (index < 0) return;

        notes.RemoveAt(index);
        SaveNotes(notes);
    }

    public void ClearAllSavedData()
    {
        try
        {
            if (File.Exists(SaveFilePath))
                File.Delete(SaveFilePath);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to clear saved note data: {ex.Message}");
        }
    }
}
