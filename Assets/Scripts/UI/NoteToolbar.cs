using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;

public class NoteToolbar : MonoBehaviour
{
    private GameObject currentNote;

    public void SetCurrentNote(GameObject note)
    {
        currentNote = note;
        Debug.Log("Current note selected: " + note.name);
    }

    public void SetYellow()
    {
        SetNoteColor(Color.yellow);
    }

    public void SetPink()
    {
        SetNoteColor(new Color(1f, 0.6f, 0.8f));
    }

    public void SetBlue()
    {
        SetNoteColor(new Color(0.5f, 0.8f, 1f));
    }

    private void SetNoteColor(Color color)
    {
        if (currentNote == null)
        {
            Debug.LogWarning("No current note selected.");
            return;
        }

        Image img = currentNote.GetComponentInChildren<Image>();

        if (img != null)
        {
            img.color = color;
            Debug.Log("Note color changed.");
        }
        else
        {
            Debug.LogWarning("No Image component found in current note.");
        }
    }
}
